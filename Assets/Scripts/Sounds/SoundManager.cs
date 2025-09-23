using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections; // Necessário para Corrotinas
using System.Threading.Tasks;
using UnityEngine.Rendering;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private AsyncOperationHandle<AudioClip> currentMusicHandle;
    private string currentMusicAddress;

    // --- NOVO ---
    // Variável para guardar a referência da nossa corrotina de fade
    private Coroutine musicFadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
    }

    private void InitializeAudioSources()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0.5f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = 1.0f;
    }


    #region Music Management

    // --- MODIFICADO ---
    // Adicionamos o parâmetro fadeDuration. O valor padrão é 0 para não quebrar chamadas antigas.
    public async void PlayMusic(string musicAddress, float targetVolume = 0.5f, float fadeDuration = 0f)
    {
        if (string.IsNullOrEmpty(musicAddress) || (musicAddress == currentMusicAddress && musicSource.isPlaying))
        {
            return;
        }

        // --- NOVO ---
        // Se uma corrotina de fade já estiver rodando, nós a paramos.
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        if (currentMusicHandle.IsValid())
        {
            Addressables.Release(currentMusicHandle);
        }

        currentMusicAddress = musicAddress;

        AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(musicAddress);
        currentMusicHandle = handle;
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            AudioClip clip = handle.Result;
            musicSource.clip = clip;

            // --- LÓGICA MODIFICADA ---
            if (fadeDuration > 0f)
            {
                // Se a duração do fade for positiva, iniciamos a corrotina
                musicFadeCoroutine = StartCoroutine(FadeInMusic(targetVolume, fadeDuration));
            }
            else
            {
                // Senão, tocamos a música instantaneamente no volume alvo
                musicSource.volume = targetVolume;
                musicSource.Play();
            }
        }
        else
        {
            Debug.LogError($"Falha ao carregar a música: {musicAddress}");
            currentMusicAddress = string.Empty;
        }
    }

    // --- NOVA CORROTINA ---
    private IEnumerator FadeInMusic(float targetVolume, float duration)
    {
        musicSource.volume = 0f; // Começa com o volume em zero
        musicSource.Play();

        float timer = 0f;

        while (timer < duration)
        {
            // A cada frame, calculamos o volume atual usando uma interpolação linear (Lerp)
            musicSource.volume = Mathf.Lerp(0f, targetVolume, timer / duration);

            // Avança o tempo e espera até o próximo frame
            timer += Time.deltaTime;
            yield return null;
        }

        // Ao final, garante que o volume seja exatamente o valor alvo
        musicSource.volume = targetVolume;
        musicFadeCoroutine = null; // Limpa a referência da corrotina
    }


    public void StopMusic()
    {
        // --- NOVO ---
        // Para qualquer corrotina de fade que possa estar rodando
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        if (musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        if (currentMusicHandle.IsValid())
        {
            Addressables.Release(currentMusicHandle);
        }
        currentMusicAddress = string.Empty;
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = Mathf.Clamp01(volume);
    }

    #endregion

    #region SFX Management

    // O código de SFX permanece o mesmo
    public async void PlaySfx(string sfxAddress, float volume = 1.0f)
    {
        if (string.IsNullOrEmpty(sfxAddress)) return;
        AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(sfxAddress);
        await handle.Task;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            AudioClip clip = handle.Result;
            sfxSource.PlayOneShot(clip, volume);
            await Task.Delay(Mathf.CeilToInt(clip.length * 1000));
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        else { Debug.LogError($"Falha ao carregar o SFX: {sfxAddress}"); }
    }

    public void PlaySound(string soundAddress, float volume = 1.0f)
    {
        PlaySfx(soundAddress, volume);
    }

    public void SetSfxVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }

    #endregion
}