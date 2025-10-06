using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;
    private bool isSinglePlayerStarting = false;


    [Header("Spawn")]
    public Transform[] spawnPoints;

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        FindAllSpawnPoints();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void FindAllSpawnPoints()
    {
        GameObject[] spawnObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints = new Transform[spawnObjects.Length];

        for (int i = 0; i < spawnObjects.Length; i++)
        {
            spawnPoints[i] = spawnObjects[i].transform;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("Nenhum ponto de spawn encontrado com a tag 'SpawnPoint'.");
        }
    }

    public Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || index < 0 || index >= spawnPoints.Length)
        {
            Debug.LogWarning($"Spawn point inválido: {index}");
            return null;
        }

        return spawnPoints[index];
    }

    public void StartSinglePlayer()
    {
        isSinglePlayerStarting = true;
        SceneManager.LoadScene(1); // Substitua pelo nome ou índice da sua cena
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAllSpawnPoints();

        if (isSinglePlayerStarting && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            isSinglePlayerStarting = false;
            SpawnLocalPlayer();
        }
    }


    private void SpawnLocalPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Prefab do jogador não está atribuído!");
            return;
        }

        Transform spawnPoint = GetSpawnPoint(0);
        Vector3 spawnPosition = spawnPoint ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);

        CameraController cameraController = Object.FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetupCameraOnSceneLoad(playerInstance);
        }

        Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
        HandsController handsController = FindAnyObjectByType<HandsController>();

        if (handsController != null)
        {
            handsController.playerController = playerInstance.GetComponent<PlayerController>();
            handsController.SetCamera(playerCamera != null ? playerCamera : Camera.main);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        NetworkObject playerNetworkObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerNetworkObject == null) return;

        GameObject playerInstance = playerNetworkObject.gameObject;

        Transform spawnPoint = GetSpawnPoint((int)clientId);
        if (spawnPoint != null)
        {
            playerInstance.transform.position = spawnPoint.position;
            playerInstance.transform.rotation = spawnPoint.rotation;
        }

        CameraController cameraController = Object.FindAnyObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetupCamera(playerInstance);
        }
    }

    public void RespawnPlayer(GameObject player)
    {
        Transform spawnPoint = GetSpawnPoint(0);

        if (spawnPoint != null) 
        {
           player.transform.position = spawnPoint.position;
        }

        
    }
}
