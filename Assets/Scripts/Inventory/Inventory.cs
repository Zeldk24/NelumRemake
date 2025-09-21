using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
//using UnityEditor;
//using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Inventory : NetworkBehaviour
{
    public static Inventory Singleton;
    public static InventoryItem carriedItem;

    [SerializeField] InventorySlot[] inventorySlots;
    [SerializeField] InventorySlot[] equipmentSlots;

    public Transform draggablesTransform;
    [SerializeField] InventoryItem itemPrefab;

    [SerializeField] Item[] items;

    [SerializeField] Button giveItemBtn;

    public PlayerController playerController;

    public event System.Action OnInventoryChanged;


    int selectedSlot = -1;
    private int scrollLimit;




    /*if (OnInventoryChanged != null) OnInventoryChanged.Invoke();

     OnInventoryChanged?.Invoke();

     Esse If ou método deve ser chamado sempre que o inventário precisar mudar
     (Craft, Pegar Item, Dropar Item e etc...
     */






    private void Start()
    {
        scrollLimit = Mathf.Min(9, inventorySlots.Length);

        if (playerController == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
        {
            playerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        }

        //ChangeSelectedSlot(0);
    }


    void ChangeSelectedSlot(int newValue)
    {
        // --- Lógica para selecionar/deselecionar slots na UI ---
        if (selectedSlot >= 0 && selectedSlot < inventorySlots.Length)
        {
            inventorySlots[selectedSlot].Deselect();
        }
        inventorySlots[newValue].Select();
        selectedSlot = newValue;

        // --- Lógica para encontrar o PlayerController ---
        PlayerController playerController = GetLocalPlayerController();
        if (playerController == null)
        {
            Debug.LogError("PlayerController local não foi encontrado.");
            return;
        }
        PlayerAttack playerAttack = playerController.GetComponentInChildren<PlayerAttack>();

        InventorySlot currentSlot = inventorySlots[newValue];

        // --- A NOVA LÓGICA DE COMUNICAÇÃO ---

        // Se o slot atual tem um item...
        if (currentSlot.myItem != null && currentSlot.myItem.myItemScriptable != null)
        {
            Item itemData = currentSlot.myItem.myItemScriptable;

            // ======================= A ÚNICA CHAMADA QUE VOCÊ PRECISA =======================
            // Simplesmente diga ao jogador para equipar este item.
            // O PlayerController vai cuidar de TUDO:
            // - Ativar a camada correta da animação
            // - Sincronizar pela rede
            // - Mostrar o objeto visual na mão
            playerController.EquipItem(itemData);

            playerAttack.SETSOUND(itemData);
            // ==============================================================================
        }
        else
        {
            // Se o slot está vazio, diga ao jogador para desequipar tudo.
            playerController.EquipItem(null);
        }
    }

    public int GetItemCount(Item itemToCount)
    {
        int totalCount = 0;

        // Varre os slots do inventário principal
        foreach (InventorySlot slot in inventorySlots)
        {
            // Se o slot tem um item e é do tipo que procuramos...
            if (slot.myItem != null && slot.myItem.myItemScriptable == itemToCount)
            {
                // ...soma 1 ao total, pois cada item é único.
                totalCount++;
            }
        }

        // Opcional: faça o mesmo para os slots de equipamento, se eles contarem.
        foreach (InventorySlot slot in equipmentSlots)
        {
            if (slot.myItem != null && slot.myItem.myItemScriptable == itemToCount)
            {
                totalCount++;
            }
        }

        return totalCount;
    }

    private void Awake()
    {
        Singleton = this;
        giveItemBtn.onClick.AddListener(delegate { SpawnInventoryItem(); });


    }



    private void Update()
    {
        if (Input.inputString != null)
        {

            bool isNumber = int.TryParse(Input.inputString, out int number);
            if (isNumber && number > 0 && number < 10)
            {
                ChangeSelectedSlot(number - 1);
            }


        }




        if (Input.GetKeyDown(KeyCode.Q)) // Tecla para dropar o item selecionado
        {
            DropSelectedItem(); // Chama o método correto!
        }

        /* Verifica se alguma tecla é pressionada, após isso verifica se ela é um número,
    e somente se o número estiver entre o array troca a cor do slot do Inventário.
    */


        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) // Scroll para cima
        {
            ChangeSelectedSlot((selectedSlot - 1 + scrollLimit) % scrollLimit);
        }
        else if (scroll < 0f) // Scroll para baixo
        {
            ChangeSelectedSlot((selectedSlot + 1) % scrollLimit);
        }


        if (carriedItem == null)
        {
            return;
        }

        carriedItem.transform.position = Input.mousePosition;

    }

    public void SetCarriedItem(InventoryItem item)
    {
        // Não permite redefinir um item que já foi dropado
        if (carriedItem == null)
        {
            carriedItem = item;
            carriedItem.canvasGroup.blocksRaycasts = false;
            item.transform.SetParent(draggablesTransform);
            return;
        }

        // Validações adicionais
        if (item.activeSlot.myTag != SlotTag.None && item.activeSlot.myTag != carriedItem.myItemScriptable.itemTag)
        {
            return;
        }

        if (item.activeSlot.myTag != SlotTag.None)
        {
            EquipEquipment(item.activeSlot.myTag, null);
        }

        carriedItem = item;
        carriedItem.canvasGroup.blocksRaycasts = false;
        item.transform.SetParent(draggablesTransform);
    }



    public void EquipEquipment(SlotTag tag, InventoryItem item = null)
    {

        switch (tag)
        {

            case SlotTag.Head:

                if (item == null)
                {
                    Debug.Log("Removeu um item da head");
                }
                else
                {
                    Debug.Log("Equipou um item da head");
                }
                break;
            case SlotTag.Chest:
                break;
            case SlotTag.Legs:
                break;
            case SlotTag.Feet:
                break;

        }

    }

    public void SpawnInventoryItem(Item item = null)
    {

        Item _item = item;
        if (item == null)
        {

            _item = PickRandomItem();

        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {

            if (inventorySlots[i].myItem == null)
            {

                Instantiate(itemPrefab, inventorySlots[i].transform).Initialize(_item, inventorySlots[i]);
                OnInventoryChanged?.Invoke();
                break;

            }



        }
    }


    Item PickRandomItem()
    {



        int random = Random.Range(0, items.Length);
        OnInventoryChanged?.Invoke();
        return items[random];
    }

    Item PickItem(Item pickItem)
    {




        Item selectedItem = null;
        foreach (var item in items)
        {

            if (item.name == pickItem.name)
            {
                selectedItem = item;
                break;
            }
        }
        return selectedItem;
    }

    public bool PickUpItem(Item item)
    {


        Item _item = item;
        if (item == null)
        {
            _item = PickItem(item);
        }

        bool hasFreeSlot = false;


        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].myItem == null)
            {
                hasFreeSlot = true; // Achou espaço disponível
                break;
            }
        }

        if (!hasFreeSlot)
        {
            Debug.Log("Inventário cheio! Item não foi coletado.");
            return false; // Retorna falso, indicando que o item não foi coletado
        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].myItem == null)
            {
                Instantiate(itemPrefab, inventorySlots[i].transform).Initialize(_item, inventorySlots[i]);

                Debug.Log("Item Adicionado");

                //Chamando atualização de inventário
                OnInventoryChanged?.Invoke();
                return true;
            }

            else
            {

                Debug.Log("Item Não Adicionado");


            }
        }
        return false;
    }




    #region Drag and Drop Logic

    public void StartDraggingItem(InventoryItem item)
    {
        // Se o item clicado estava na mão, limpa a mão.
        if (item.activeSlot != null && selectedSlot >= 0 && inventorySlots[selectedSlot] == item.activeSlot)
        {
            PlayerController localPlayer = FindFirstObjectByType<PlayerController>();
            if (localPlayer != null)
            {
                localPlayer.ClearHandItem();
            }
        }

        // Configura o item como "sendo carregado"
        carriedItem = item;
        carriedItem.transform.SetParent(draggablesTransform); // Move para a camada de "arrastáveis"
        carriedItem.canvasGroup.blocksRaycasts = false; // Permite que o mouse detecte o que está por baixo
    }

    public void EndDraggingItem()
    {
        if (carriedItem == null) return;

        // Se, após o EndDrag, o item não encontrou um novo slot (ou seja, ainda é filho do draggablesTransform)
        if (carriedItem.transform.parent == draggablesTransform)
        {
            // Se o seu `InventorySlot` não moveu o item, significa que ele foi solto "no nada".
            // Cenário 1: Dropar no mundo
            Debug.Log("Item solto fora de um slot. Dropando no mundo.");
            DropCarriedItemInWorld();

            // Cenário 2: Retornar ao slot original (se você não quer dropar ao arrastar para fora)
            // carriedItem.MoveToSlot(carriedItem.activeSlot); 
        }

        // Limpa a referência do item carregado.
        if (carriedItem != null)
        {
            carriedItem.canvasGroup.blocksRaycasts = true;
        }
        carriedItem = null;
    }

    #endregion


    #region Drop Logic

    // Este método é chamado pelo clique direito
    public void DropItemOnClick(InventoryItem item)
    {
        if (item == null) return;

        // <<< MUDANÇA: Usa a nova função auxiliar >>>
        PlayerController localPlayer = GetLocalPlayerController();
        if (localPlayer == null)
        {
            Debug.LogError("Player local não encontrado para dropar item.");
            return;
        }

        // A lógica de limpeza da mão VEM PRIMEIRO
        if (item.activeSlot != null && selectedSlot >= 0 && selectedSlot < inventorySlots.Length && inventorySlots[selectedSlot] == item.activeSlot)
        {
            localPlayer.ClearHandItem();
        }

        // Agora, spawnamos o item no mundo
        DropItemInWorld(item);

        // E finalmente destruímos o item da UI
        Destroy(item.gameObject);
    }


    // Este método é chamado pela tecla 'Q'
    public void DropSelectedItem()
    {
        if (selectedSlot < 0 || selectedSlot >= inventorySlots.Length || inventorySlots[selectedSlot].myItem == null) return;

        // <<< MUDANÇA: Usa a nova função auxiliar >>>
        PlayerController localPlayer = GetLocalPlayerController();
        if (localPlayer == null)
        {
            Debug.LogError("Player local não encontrado para dropar item.");
            return;
        }

        InventoryItem itemToDrop = inventorySlots[selectedSlot].myItem;

        localPlayer.ClearHandItem();

        DropItemInWorld(itemToDrop);
        Destroy(itemToDrop.gameObject);
    }


    // Método auxiliar para dropar um item que estava sendo arrastado
    private void DropCarriedItemInWorld()
    {
        if (carriedItem == null) return;
        DropItemInWorld(carriedItem);
        Destroy(carriedItem.gameObject); // Destrói o item da UI
        carriedItem = null;
    }

    // Método centralizado para spawnar o item no mundo
    private void DropItemInWorld(InventoryItem item)
    {
        // <<< MUDANÇA: Usa a nova função auxiliar >>>
        PlayerController player = GetLocalPlayerController();
        string itemAddress = item.myItemScriptable.prefabItemRef.RuntimeKey.ToString();

        if (player == null || string.IsNullOrEmpty(itemAddress))
        {
            Debug.LogError("Não foi possível dropar o item. Player ou endereço do item nulo.");
            return;
        }

        Vector3 playerPosition = player.transform.position;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            SpawnObjectMultiplayerServerRpc(itemAddress, playerPosition);
        }
        else
        {
            SpawnObjectLocal(itemAddress, playerPosition);
        }
    }

    #endregion



    private PlayerController GetLocalPlayerController()
    {
        // Se estiver em modo single-player, a busca simples funciona.
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            return FindFirstObjectByType<PlayerController>();
        }

        // Em multiplayer, SEMPRE use o PlayerObject do cliente local.
        if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            return NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        }

        // Retorna nulo se não encontrar nada (caso de segurança)
        return null;
    }




    //Spawna o Objeto localmente
    public async void SpawnObjectLocal(string itemAddress, Vector3 playerPosition)
    {


        // Carrega o prefab do Addressable
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(itemAddress);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject prefabToSpawn = handle.Result;

            // <<< MUDANÇA: Usa a posição recebida, que é 100% confiável.
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 spawnPosition = (Vector2)playerPosition + randomDirection * Random.Range(0.5f, 1f);

            GameObject droppedItem = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

            droppedItem.layer = LayerMask.NameToLayer("PickupItens");

            FireWeapon fireWeapon = droppedItem.GetComponent<FireWeapon>();
            if (fireWeapon != null)
            {
                fireWeapon.enabled = false;

            }

            // ... O resto da sua lógica de animator e rigidbody está correta ...
            Animator animator = droppedItem.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.AddForce(randomDirection * 3f, ForceMode2D.Impulse);
            }


            Addressables.Release(handle);
        }
        else
        {
            Debug.LogError($"Falha ao carregar o item para dropar (local): {itemAddress}");
        }
    }


    //Spawna o Objeto em redes
    [ServerRpc(RequireOwnership = false)]
    private void SpawnObjectMultiplayerServerRpc(string itemAddress, Vector3 playerPosition, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // <<< MUDANÇA: Obtém o clientId a partir dos parâmetros do RPC.
        ulong clientId = rpcParams.Receive.SenderClientId;

        var handle = Addressables.LoadAssetAsync<GameObject>(itemAddress);
        handle.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject prefabToSpawn = op.Result;

                // <<< MUDANÇA: Usa a posição recebida do cliente. Não busca mais o player object.
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                Vector2 spawnPosition = (Vector2)playerPosition + randomDirection * Random.Range(0.5f, 1f);

                GameObject droppedItem = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

                droppedItem.layer = LayerMask.NameToLayer("PickupItens");

                // O resto do seu código já está correto
                NetworkObject networkObjectDrop = droppedItem.GetComponent<NetworkObject>();
                if (networkObjectDrop == null) { /* ... */ }
                networkObjectDrop.Spawn(true);

                FireWeapon fireWeapon = droppedItem.GetComponent<FireWeapon>();
                if (fireWeapon != null)
                {
                    fireWeapon.enabled = false;

                }
                // ... resto do seu código de configuração de animator, rigidbody, etc. ...
                Animator animator = droppedItem.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;

                var netTransform = droppedItem.GetComponent<NetworkTransform>();
                if (netTransform != null) netTransform.enabled = true;

                Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
                Vector2 initialVelocity = Vector2.zero;
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.AddForce(randomDirection * 3f, ForceMode2D.Impulse);
                    initialVelocity = rb.linearVelocity;
                }

                ApplyInitialVelocityClientRpc(networkObjectDrop.NetworkObjectId, initialVelocity);
                ClearHandItemClientRpc(clientId);

                Addressables.Release(op);
            }
            else
            {
                Debug.LogError($"[Server] Falha ao carregar o item para dropar: {itemAddress}");
            }
        };
    }

    // ClientRpc para aplicar a velocidade inicial nos clientes
    [ClientRpc]
    private void ApplyInitialVelocityClientRpc(ulong networkObjectId, Vector2 initialVelocity)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            GameObject itemObject = netObj.gameObject;

            // PASSO IMPORTANTE 1: Desativa o Animator na cópia LOCAL do cliente.
            Animator animator = itemObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }



            Rigidbody2D rb = netObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = initialVelocity; // Aplica a velocidade inicial
            }
        }
    }


    [ClientRpc]
    private void ClearHandItemClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            var controller = player.GetComponent<PlayerController>();
            controller.ClearHandItem();
        }
    }

    #region Craft


    public bool CanCraft(Recipe recipe)
    {
        foreach (Ingredient ingredient in recipe.ingredients)
        {
            // Verifica se a contagem de itens no inventário é menor que a necessária.
            if (GetItemCount(ingredient.item) < ingredient.quantity)
            {
                // Se faltar qualquer ingrediente, não pode craftar.
                return false;
            }
        }
        // Se passou por todos os ingredientes, pode craftar.
        return true;
    }

    public void Craft(Recipe recipe)
    {
        // 1. Verifica se é possível craftar
        if (!CanCraft(recipe))
        {
            Debug.Log("Faltam itens para criar " + recipe.result.itemName);
            return;
        }

        // 2. Remove os ingredientes do inventário
        foreach (Ingredient ingredient in recipe.ingredients)
        {
            RemoveItem(ingredient.item, ingredient.quantity);
        }

        // 3. Adiciona o item resultante ao inventário
        SpawnInventoryItem(recipe.result);

        // 4. Dispara o evento para atualizar a UI (você já tem isso!)
        OnInventoryChanged?.Invoke();
    }


    public void RemoveItem(Item itemToRemove, int quantityToRemove)
    {
        int quantityRemoved = 0;

        // Percorre todos os slots para encontrar os itens
        foreach (InventorySlot slot in inventorySlots)
        {
            // Se já removemos o suficiente, podemos parar.
            if (quantityRemoved >= quantityToRemove) break;

            if (slot.myItem != null && slot.myItem.myItemScriptable == itemToRemove)
            {
                // Destroi o item visual do slot
                Destroy(slot.myItem.gameObject);
                slot.myItem = null; // Limpa a referência no slot

                quantityRemoved++;
            }
        }
    }

    #endregion
}