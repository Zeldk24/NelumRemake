using UnityEngine;
using Unity.Netcode;

public class SpawnLevelManager : MonoBehaviour
{
    [Header("Gerenciadores de Spawn")]
    [Tooltip("Arraste aqui o componente que gerencia o spawn de inimigos.")]
    [SerializeField]
    private SpawnEnemiesManager spawnEnemiesManager;

    [Tooltip("Arraste aqui o componente que gerencia o spawn de itens.")]
    [SerializeField]
    private SpawnItemsManager spawnItemsManager; // Adicionamos a referência para o spawner de itens

    void Start()
    {
        // A lógica de decisão continua a mesma!
        if (IsSinglePlayer())
        {
            Debug.Log("Modo Single-Player detectado. Spawning tudo localmente.");

            // Chama o spawner de inimigos, se existir
            if (spawnEnemiesManager != null)
            {
                spawnEnemiesManager.SpawnForSinglePlayer();
            
            }
            else
            {
                Debug.LogWarning("SpawnEnemiesManager não foi configurado no SpawnLevelManager.");
            }

            // Chama o spawner de itens, se existir
            if (spawnItemsManager != null)
            {
                spawnItemsManager.SpawnForSinglePlayer();
                
                
            }
            else
            {
                Debug.LogWarning("SpawnItemsManager não foi configurado no SpawnLevelManager.");
            }
        }
        else
        {
            // Em modo multiplayer, não fazemos nada aqui.
            // Os OnNetworkSpawn de cada gerenciador serão chamados automaticamente pelo Netcode no servidor.
            Debug.Log("Modo Multiplayer detectado. Spawning será gerenciado pelo servidor.");
        }
    }

    private bool IsSinglePlayer()
    {
        return NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
    }
}