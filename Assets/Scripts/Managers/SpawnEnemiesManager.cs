using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SpawnEnemiesManager : NetworkBehaviour
{
    // NOVO: Criamos uma struct para organizar os dados de cada spawn.
    [System.Serializable]
    public struct SpawnInfo
    {
        public EnemiesSO enemyData;
        public Transform[] spawnPoints;
    }

    // AGORA TEMOS UMA LISTA, você pode adicionar quantos tipos de inimigos quiser
    [Header("Lista de Spawns")]
    public List<SpawnInfo> enemiesToSpawn;

    #region Ciclo de Jogo (Single-Player e Multiplayer)

    // No SINGLE-PLAYER, é chamado pelo SpawnLevelManager
    public void SpawnForSinglePlayer()
    {
        Debug.Log("Spawning todos os inimigos localmente (Single-Player)...");
        foreach (var info in enemiesToSpawn)
        {
            if (info.enemyData == null || info.spawnPoints.Length == 0) continue;

            foreach (Transform point in info.spawnPoints)
            {
                // Instanciação simples para single-player
                Addressables.InstantiateAsync(info.enemyData.prefabEnemieRef, point.position, point.rotation);
            }
        }
    }

    // No MULTIPLAYER, este método é chamado automaticamente no servidor
    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        Debug.Log("Servidor está spawnando todos os inimigos na rede...");
        await SpawnAllEnemiesNetworked();
    }

    #endregion

    #region Lógica de Spawning Unificada

    /// <summary>
    /// MÉTODO ÚNICO E UNIFICADO que agora spawna TODOS os inimigos da lista, incluindo o boss.
    /// </summary>
    private async Task SpawnAllEnemiesNetworked()
    {
        // Itera sobre a lista de "coisas a spawnar"
        foreach (var info in enemiesToSpawn)
        {
            // Validações
            if (info.enemyData == null)
            {
                Debug.LogWarning("Um SpawnInfo na lista não tem EnemyData atribuído.", this);
                continue;
            }
            if (info.spawnPoints == null || info.spawnPoints.Length == 0)
            {
                Debug.LogWarning($"Nenhum ponto de spawn definido para {info.enemyData.name}.", this);
                continue;
            }

            // Itera sobre os pontos de spawn para este tipo de inimigo
            foreach (Transform point in info.spawnPoints)
            {
                // A LÓGICA DE SPAWN QUE SABEMOS QUE FUNCIONA
                AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(info.enemyData.prefabEnemieRef, point.position, point.rotation);
                GameObject instance = await handle.Task;

                if (instance == null) continue;

                NetworkObject instanceNetwork = instance.GetComponent<NetworkObject>();
                if (instanceNetwork != null)
                {
                    instanceNetwork.Spawn();
                }
                else
                {
                    Debug.LogError($"O prefab '{instance.name}' para o inimigo '{info.enemyData.name}' não possui um componente NetworkObject!", instance);
                    Addressables.ReleaseInstance(instance);
                }
            }
        }
    }

    #endregion
}