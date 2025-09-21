// SoundManager.cs - VERSÃO ATUALIZADA PARA ADDRESSABLES
using UnityEngine;
using System.Collections.Generic;
// Adicione os usings para Addressables
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    private AudioSource audioSource;

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
        }
    }


    void Start()
    {
        // Apenas precisamos garantir que temos um AudioSource.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // Adiciona um AudioSource se não houver um.
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }


    public async void PlaySound(string soundAddress, float volume = 1.0f)
    {
        if (string.IsNullOrEmpty(soundAddress)) return;

        AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(soundAddress);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            AudioClip clip = handle.Result;
            audioSource.PlayOneShot(clip, volume);

            // espera a duração do som para liberar
            await System.Threading.Tasks.Task.Delay(Mathf.CeilToInt(clip.length * 1000));
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        else
        {
            Debug.LogError($"Falha ao carregar o som do endereço: {soundAddress}");
        }
    }

}