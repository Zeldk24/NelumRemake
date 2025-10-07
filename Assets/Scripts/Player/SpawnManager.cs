using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
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

        // NOVA LÓGICA: Para single-player, spawna local
        if (isSinglePlayerStarting && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            isSinglePlayerStarting = false;
            SpawnLocalPlayer();
        }
        // NOVA LÓGICA: Para host em multiplayer, garante spawn/teleport após cena carregar
        else if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            // Pega o clientId do host (geralmente 0) e teleporta se o PlayerObject existir
            ulong hostClientId = NetworkManager.Singleton.LocalClientId;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(hostClientId, out var hostClient))
            {
                if (hostClient.PlayerObject != null)
                {
                    TeleportPlayerToSpawn(hostClient.PlayerObject, (int)hostClientId);
                }
            }
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

        // Setup câmera e hands (mesmo código)
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

    // FUNÇÃO ATUALIZADA: Lógica centralizada para teleport com verificações de autoridade (corrige o erro em single-player)
    private void TeleportPlayerToSpawn(NetworkObject playerNetworkObject, int spawnIndex)
    {
        if (playerNetworkObject == null) return;

        Transform spawnPoint = GetSpawnPoint(spawnIndex);
        if (spawnPoint == null) return;

        // NOVA VERIFICAÇÃO: Se rede não está ativa (single-player), usa fallback local
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isNetworkActive)
        {
            // Single-player: Seta posição diretamente (sem rede)
            playerNetworkObject.transform.position = spawnPoint.position;
            playerNetworkObject.transform.rotation = spawnPoint.rotation;
            Debug.Log("Teleport em single-player: Usando fallback local.");
            return;
        }

        // Rede ativa: Verifica autoridade antes de usar NetworkTransform
        NetworkTransform networkTransform = playerNetworkObject.GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            bool isAuthoritative = NetworkManager.Singleton.IsServer ||
                                   (playerNetworkObject.IsOwner && playerNetworkObject.IsPlayerObject);

            if (isAuthoritative)
            {
                // Teleport syncado para rede (só se autoritativo)
                networkTransform.Teleport(spawnPoint.position, spawnPoint.rotation, Vector3.one);
                Debug.Log($"Player teleportado para spawn {spawnIndex} via NetworkTransform (autoritativo).");
            }
            else
            {
                // Não autoritativo: Usa fallback local (ex: client remoto não pode teleportar seu próprio player assim)
                playerNetworkObject.transform.position = spawnPoint.position;
                playerNetworkObject.transform.rotation = spawnPoint.rotation;
                Debug.LogWarning($"Teleport para spawn {spawnIndex}: Não autoritativo, usando fallback local.");
            }
        }
        else
        {
            // Sem NetworkTransform: Fallback sempre
            playerNetworkObject.transform.position = spawnPoint.position;
            playerNetworkObject.transform.rotation = spawnPoint.rotation;
            Debug.LogWarning("NetworkTransform não encontrado! Usando fallback local.");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return; // Só servidor lida com spawns

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        // NOVA LÓGICA: Se o PlayerObject não existir, SPAWNE o player no servidor
        var client = NetworkManager.Singleton.ConnectedClients[clientId];
        if (client.PlayerObject == null)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Prefab do jogador não está atribuído para spawn!");
                return;
            }

            // Pega spawn point baseado no clientId (ou use um sistema de round-robin se quiser distribuir)
            int spawnIndex = (int)clientId % spawnPoints.Length; // Exemplo: distribui por clientId
            Transform spawnPoint = GetSpawnPoint(spawnIndex);
            if (spawnPoint == null)
            {
                spawnPoint = GetSpawnPoint(0); // Fallback para primeiro spawn
            }

            // Instancia o prefab no servidor, na posição do spawn
            GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // Pega o NetworkObject do player e spawna na rede, atribuindo ao client
            NetworkObject playerNetworkObject = playerInstance.GetComponent<NetworkObject>();
            if (playerNetworkObject != null)
            {
                playerNetworkObject.SpawnAsPlayerObject(clientId); // Isso atribui ao client e synca para todos
                client.PlayerObject = playerNetworkObject; // Garante atribuição

                Debug.Log($"Player spawnado para client {clientId} no spawn {spawnIndex}.");
            }
            else
            {
                Debug.LogError("PlayerPrefab não tem NetworkObject!");
                Destroy(playerInstance);
                return;
            }
        }
        else
        {
            // Se já existir (ex: reconexão ou host), só teleporta
            int spawnIndex = (int)clientId % spawnPoints.Length;
            TeleportPlayerToSpawn(client.PlayerObject, spawnIndex);
        }

        // Setup câmera (para todos, mas só localmente no client)
        if (NetworkManager.Singleton.IsClient)
        {
            CameraController cameraController = Object.FindAnyObjectByType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetupCamera(client.PlayerObject.gameObject);
            }
        }
    }

    // FUNÇÃO ATUALIZADA: Respawn agora com verificação explícita para single-player
    public void RespawnPlayer(GameObject player)
    {
        if (player == null) return;

        if (!player.TryGetComponent<NetworkObject>(out var networkObject))
        {
            Debug.LogError("Player não tem NetworkObject para respawn!");
            return;
        }

        // NOVA VERIFICAÇÃO: Detecta single-player e usa fallback direto
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isNetworkActive)
        {
            // Single-player: Respawn local simples
            Transform spawnPoint = GetSpawnPoint(0); // Ou use índice fixo para single
            if (spawnPoint != null)
            {
                player.transform.position = spawnPoint.position;
                player.transform.rotation = spawnPoint.rotation;
                Debug.Log("Respawn em single-player: Posição local setada.");
            }
            return;
        }

        // Multiplayer: Usa a lógica de rede
        ulong clientId = networkObject.OwnerClientId;
        int spawnIndex = (int)clientId % spawnPoints.Length;

        TeleportPlayerToSpawn(networkObject, spawnIndex);

        // Opcional: Reativa o player se estiver inativo
        if (!networkObject.IsSpawned)
        {
            networkObject.Spawn();
        }

        Debug.Log($"Player {clientId} respawnado no spawn {spawnIndex}.");
    }
}
