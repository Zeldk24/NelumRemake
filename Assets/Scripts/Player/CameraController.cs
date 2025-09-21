using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;
using System.Collections;
public class CameraController : NetworkBehaviour
{
    public GameObject cameraPrefab;  // Prefab da câmera
    private GameObject currentCamera;  // Câmera instanciada
    private GameObject player;        // Jogador que a câmera deve seguir

    public Vector3 offset = new Vector3(0, 0, -7);  // Deslocamento desejado para a câmera

    [HideInInspector] public bool canFollow = true; // NOVO

    private Vector3 originalPos;


    public override void OnNetworkSpawn()
    {
        // Configura a câmera apenas para o cliente local (o dono do jogador)
        if (IsOwner)
        {
            SetupCamera(gameObject);  // Passa o próprio objeto do jogador
        }
    }

    public IEnumerator CameraShake(float duration, float magnitude)
    {
        if (currentCamera == null) yield break;

        Vector3 originalPos = currentCamera.transform.localPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            currentCamera.transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        currentCamera.transform.localPosition = originalPos;
    }


    public void SetupCamera(GameObject playerInstance)
    {
        player = playerInstance;

        if (cameraPrefab != null)
        {
            // Instancia a câmera, caso ainda não tenha sido instanciada
            if (currentCamera == null)
            {
                currentCamera = Instantiate(cameraPrefab);
                currentCamera.tag = "MainCamera";

                // --- CÓDIGO ANTIGO, SEM MUDANÇAS ---
                Camera camComponent = currentCamera.GetComponent<Camera>();
                if (camComponent != null)
                {
                    camComponent.orthographic = true;
                    camComponent.orthographicSize = 4f;
                }
                AudioListener existingAudioListener = Object.FindAnyObjectByType<AudioListener>();
                if (existingAudioListener != null && existingAudioListener != currentCamera.GetComponent<AudioListener>())
                {
                    if (currentCamera.GetComponent<AudioListener>() != null)
                        currentCamera.GetComponent<AudioListener>().enabled = false;
                }

                // --- INÍCIO DA NOVA LÓGICA DE STACK ---

                // 1. Obter o componente de dados da câmera principal
                var mainCameraData = currentCamera.GetComponent<UniversalAdditionalCameraData>();
                if (mainCameraData == null)
                {
                    Debug.LogError("Câmera principal não tem UniversalAdditionalCameraData. Verifique se o URP está instalado e configurado.");
                    return;
                }

                // 2. Encontrar a câmera de overlay (que é filha no prefab)
                Transform overlayCamTransform = currentCamera.transform.Find("Camera3D"); // Use o nome que você deu
                if (overlayCamTransform != null)
                {
                    Camera overlayCamera = overlayCamTransform.GetComponent<Camera>();

                    // 3. Adicionar a câmera de overlay ao stack da câmera principal
                    if (overlayCamera != null)
                    {
                        mainCameraData.cameraStack.Clear(); // Limpa o stack para garantir
                        mainCameraData.cameraStack.Add(overlayCamera);
                        Debug.Log("Câmera de Overlay adicionada ao stack da câmera principal.");
                    }
                }
                else
                {
                    Debug.LogWarning("Não foi possível encontrar a OverlayCamera_3D como filha do prefab da câmera.");
                }

                // --- FIM DA NOVA LÓGICA DE STACK ---

                // --- CÓDIGO ANTIGO, SEM MUDANÇAS ---
                currentCamera.transform.SetParent(playerInstance.transform);
                currentCamera.transform.localPosition = offset;
                currentCamera.transform.localRotation = Quaternion.identity;

                Debug.Log("Câmera configurada para seguir o jogador.");
            }
        }
        else
        {
            Debug.LogWarning("Prefab da câmera não está atribuído.");
        }
    }

    // Atualiza a posição da câmera para seguir o jogador
    void Update()
    {
        if (player != null && currentCamera != null && canFollow) // só segue se permitido
        {
            currentCamera.transform.position = player.transform.position + offset;
        }
    }

    // Método que será chamado quando a cena for carregada para configurar a câmera
    public void SetupCameraOnSceneLoad(GameObject playerInstance)
    {
        SetupCamera(playerInstance);
    }
}
