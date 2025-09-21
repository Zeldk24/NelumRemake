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
    private SpawnItemsManager spawnItemsManager; // Adicionamos a refer�ncia para o spawner de itens

    void Start()
    {
        // A l�gica de decis�o continua a mesma!
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
                Debug.LogWarning("SpawnEnemiesManager n�o foi configurado no SpawnLevelManager.");
            }

            // Chama o spawner de itens, se existir
            if (spawnItemsManager != null)
            {
                spawnItemsManager.SpawnForSinglePlayer();
                
                
            }
            else
            {
                Debug.LogWarning("SpawnItemsManager n�o foi configurado no SpawnLevelManager.");
            }
        }
        else
        {
            // Em modo multiplayer, n�o fazemos nada aqui.
            // Os OnNetworkSpawn de cada gerenciador ser�o chamados automaticamente pelo Netcode no servidor.
            Debug.Log("Modo Multiplayer detectado. Spawning ser� gerenciado pelo servidor.");
        }
    }

    private bool IsSinglePlayer()
    {
        return NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
    }
}