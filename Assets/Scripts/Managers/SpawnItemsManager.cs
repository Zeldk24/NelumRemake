using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SpawnItemsManager : NetworkBehaviour
{
    [Header("Dados do Item")]
    public Item itemData; // Refer�ncia para os dados do item a ser spawnado.

    [Header("Pontos de Spawn")]
    public Transform[] spawnPoints; // Pontos espec�ficos para os itens.

    /// <summary>
    /// Chamado automaticamente no servidor quando este objeto � spawnado na rede.
    /// </summary>
    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            return;
        }

        Debug.Log("Servidor est� spawnando itens para todos os clientes...");
        await SpawnAllItemsNetworked();
    }

    /// <summary>
    /// M�todo p�blico para ser chamado no modo Single-Player.
    /// </summary>
    public async void SpawnForSinglePlayer()
    {
        if (!ValidateSpawnPoints()) return;

        foreach (Transform point in spawnPoints)
        {
            // <<< MUDAN�A: Obtemos o handle para poder acessar a inst�ncia
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(itemData.prefabItemRef, point.position, point.rotation);
            GameObject instance = await handle.Task;

            instance.layer = LayerMask.NameToLayer("PickupItens");
        }



    }

    /// <summary>
    /// L�gica de spawn de itens na rede para o servidor.
    /// </summary>
    private async Task SpawnAllItemsNetworked()
    {
        if (!ValidateSpawnPoints()) return;

        foreach (Transform point in spawnPoints)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(itemData.prefabItemRef, point.position, point.rotation);
            GameObject instance = await handle.Task;

            instance.layer = LayerMask.NameToLayer("PickupItens");

            if (instance == null)
            {
                Debug.LogError($"Falha ao instanciar o Addressable do item: {itemData.prefabItemRef.AssetGUID}", this);
                continue;
            }

            NetworkObject instanceNetwork = instance.GetComponent<NetworkObject>();
            if (instanceNetwork == null)
            {
                Debug.LogError($"O prefab de item '{instance.name}' n�o possui um componente NetworkObject!", instance);
                Addressables.ReleaseInstance(handle);
                continue;
            }

            instanceNetwork.Spawn();
        }
    }

    /// <summary>
    /// Valida se os dados de spawn est�o corretos.
    /// </summary>
    private bool ValidateSpawnPoints()
    {
        if (itemData == null)
        {
            Debug.LogError("O ScriptableObject 'itemData' n�o foi atribu�do no SpawnItemsManager!", this);
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Nenhum ponto de spawn de item foi definido no SpawnItemsManager!", this);
            return false;
        }
        return true;
    }




}