using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;
using System.Collections;
public class CameraController : NetworkBehaviour
{
    public GameObject cameraPrefab;  // Prefab da c�mera
    private GameObject currentCamera;  // C�mera instanciada
    private GameObject player;        // Jogador que a c�mera deve seguir

    public Vector3 offset = new Vector3(0, 0, -7);  // Deslocamento desejado para a c�mera

    [HideInInspector] public bool canFollow = true; // NOVO

    private Vector3 originalPos;


    public override void OnNetworkSpawn()
    {
        // Configura a c�mera apenas para o cliente local (o dono do jogador)
        if (IsOwner)
        {
            SetupCamera(gameObject);  // Passa o pr�prio objeto do jogador
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
            // Instancia a c�mera, caso ainda n�o tenha sido instanciada
            if (currentCamera == null)
            {
                currentCamera = Instantiate(cameraPrefab);
                currentCamera.tag = "MainCamera";

                // --- C�DIGO ANTIGO, SEM MUDAN�AS ---
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

                // --- IN�CIO DA NOVA L�GICA DE STACK ---

                // 1. Obter o componente de dados da c�mera principal
                var mainCameraData = currentCamera.GetComponent<UniversalAdditionalCameraData>();
                if (mainCameraData == null)
                {
                    Debug.LogError("C�mera principal n�o tem UniversalAdditionalCameraData. Verifique se o URP est� instalado e configurado.");
                    return;
                }

                // 2. Encontrar a c�mera de overlay (que � filha no prefab)
                Transform overlayCamTransform = currentCamera.transform.Find("Camera3D"); // Use o nome que voc� deu
                if (overlayCamTransform != null)
                {
                    Camera overlayCamera = overlayCamTransform.GetComponent<Camera>();

                    // 3. Adicionar a c�mera de overlay ao stack da c�mera principal
                    if (overlayCamera != null)
                    {
                        mainCameraData.cameraStack.Clear(); // Limpa o stack para garantir
                        mainCameraData.cameraStack.Add(overlayCamera);
                        Debug.Log("C�mera de Overlay adicionada ao stack da c�mera principal.");
                    }
                }
                else
                {
                    Debug.LogWarning("N�o foi poss�vel encontrar a OverlayCamera_3D como filha do prefab da c�mera.");
                }

                // --- FIM DA NOVA L�GICA DE STACK ---

                // --- C�DIGO ANTIGO, SEM MUDAN�AS ---
                currentCamera.transform.SetParent(playerInstance.transform);
                currentCamera.transform.localPosition = offset;
                currentCamera.transform.localRotation = Quaternion.identity;

                Debug.Log("C�mera configurada para seguir o jogador.");
            }
        }
        else
        {
            Debug.LogWarning("Prefab da c�mera n�o est� atribu�do.");
        }
    }

    // Atualiza a posi��o da c�mera para seguir o jogador
    void Update()
    {
        if (player != null && currentCamera != null && canFollow) // s� segue se permitido
        {
            currentCamera.transform.position = player.transform.position + offset;
        }
    }

    // M�todo que ser� chamado quando a cena for carregada para configurar a c�mera
    public void SetupCameraOnSceneLoad(GameObject playerInstance)
    {
        SetupCamera(playerInstance);
    }
}
