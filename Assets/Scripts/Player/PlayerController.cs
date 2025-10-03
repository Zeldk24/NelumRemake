using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
//using UnityEditor.Experimental.GraphView;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerController : NetworkBehaviour
{
    
    private Rigidbody2D _playerRigidbody2d;
    
    public float _playerSpeed = 3f;
   
    public GameObject currentHandItem;  
    
    private Camera mainCamera;

    public GameObject playerHandsPrefab;

    private GameObject playerHandsObj;

    private int facingDirection;

    [HideInInspector] public Animator playerAnim;

    public Item currentHandItemData;

    public SpriteRenderer playerSprite;

    private InputManager inputs;

    private PlayerHealth playerHealth;

    private NetworkVariable<int> activeItemType = new NetworkVariable<int>(0,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
   );

    

    // 2. Mapeamento para configurar no Inspector
    [System.Serializable]
    public class ItemLayerMapping
    {
        public SlotTag itemTag; // Ex: Weapon, FireWeapon
        public int layerIndex;  // O número da camada no Animator
    }
    public List<ItemLayerMapping> itemLayers = new List<ItemLayerMapping>();
    private Dictionary<SlotTag, int> _itemLayerDict = new Dictionary<SlotTag, int>();



    private bool isChangingItem = false;
    private string pendingItemAddress = null;

    private void Awake()
    {
        inputs = GetComponent<InputManager>();
        _playerRigidbody2d = GetComponent<Rigidbody2D>();
        playerAnim = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();

        foreach (var mapping in itemLayers)
        {
            if (!_itemLayerDict.ContainsKey(mapping.itemTag))
            {
                _itemLayerDict.Add(mapping.itemTag, mapping.layerIndex);
            }
        }
    }

    private void Andar()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector2 movimento = new Vector3(horizontal, vertical).normalized;
        _playerRigidbody2d.linearVelocity = movimento;
       


    }

    void Start()
    {
       

        playerSprite = GetComponent<SpriteRenderer>();
     

       

        FindOrAssignCamera();

        if (IsSinglePlayer())
        {
            SpawnHandsLocal();
        }


    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // --- NOVA LÓGICA DE OnNetworkSpawn ---
        activeItemType.OnValueChanged += OnActiveItemChanged;
        // Aplica o estado atual para todos
        OnActiveItemChanged(0, activeItemType.Value);

        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        if (!IsSinglePlayer()) SpawnHands();
    }

    public void EquipItem(Item itemData)
    {
        this.currentHandItemData = itemData;

        // 1. Determina qual camada de animação usar
        int itemTypeIndex = 0; // 0 = Desarmado (camada base)
        if (itemData != null && _itemLayerDict.ContainsKey(itemData.itemTag))
        {
            itemTypeIndex = _itemLayerDict[itemData.itemTag];
        }

        // 2. Sincroniza a animação
        if (IsSinglePlayer())
        {
            OnActiveItemChanged(0, itemTypeIndex);
        }
        else if (IsOwner)
        {
            SetEquippedItemTypeServerRpc(itemTypeIndex);
        }

        // 3. Sincroniza o objeto visual na mão
        HandleVisualItemChange(itemData);
    }

    [ServerRpc]
    private void SetEquippedItemTypeServerRpc(int newItemTypeIndex)
    {
        activeItemType.Value = newItemTypeIndex;
    }

    private void OnActiveItemChanged(int previousValue, int newValue)
    {
        foreach (var mapping in itemLayers)
        {
            if (mapping.layerIndex > 0 && playerAnim != null)
                playerAnim.SetLayerWeight(mapping.layerIndex, 0f);
        }

        if (newValue > 0 && playerAnim != null)
        {
            playerAnim.SetLayerWeight(newValue, 1f);
        }
    }





    void FindOrAssignCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // procura pela Main Camera na cena
            if (mainCamera == null)
            {
                mainCamera = FindAnyObjectByType<Camera>(); // fallback, caso não tenha tag "MainCamera"
            }
        }
    }


    //Codigo original
    private bool IsSinglePlayer()
    {
        return NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
    }


    private void HandleVisualItemChange(Item itemData)
    {

        bool showVisual = (itemData != null && itemData.itemTag == SlotTag.FireWeapon && itemData.prefabItemRef.RuntimeKeyIsValid());

        // Lógica para pedir para spawnar ou despawnar o item
        if (showVisual)
        {
            string prefabAddress = itemData.prefabItemRef.RuntimeKey.ToString();

            if (IsSinglePlayer())
            {
                SetVisualItemLocal(prefabAddress);
            }
            else if (IsOwner)
            { // O dono do player pede para spawnar o item
                SetVisualItemServerRpc(prefabAddress);
            }
        }
        else // Se não é pra mostrar, pede para limpar.
        {
            if (IsSinglePlayer())
            {
                // Limpa localmente em modo single player
                if (currentHandItem != null)
                {
                    Destroy(currentHandItem);
                    currentHandItem = null;
                }
            }
            else if (IsOwner)
            { // O dono do player pede para limpar o item
                ClearVisualItemServerRpc();
            }
        }
    }

    private void NeutralizeHeldItemComponents(GameObject heldItem)
    {
        if (heldItem == null) return;

        // 1. Desativa o script de coleta para que ele não execute a lógica de OnTriggerEnter2D
        if (heldItem.TryGetComponent<PickupItem>(out var pickupScript))
        {
            pickupScript.enabled = false;
        }

        // 2. Desativa o collider para evitar quaisquer interações de trigger/colisão
        if (heldItem.TryGetComponent<Collider2D>(out var collider))
        {
            collider.enabled = false;
        }
    }

    private async void SetVisualItemLocal(string itemAddress)
    {
        if (isChangingItem)
        {
            pendingItemAddress = itemAddress; // salva o próximo pedido
            return;
        }

        isChangingItem = true;

        if (currentHandItem != null)
            Destroy(currentHandItem);

        var handle = Addressables.LoadAssetAsync<GameObject>(itemAddress);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            currentHandItem = Instantiate(handle.Result, playerHandsObj.transform);
            currentHandItem.transform.localPosition = Vector3.zero;
            currentHandItem.transform.localRotation = Quaternion.identity;

            // --- ADICIONE ESTA LINHA ---
            NeutralizeHeldItemComponents(currentHandItem);
        }

        Addressables.Release(handle);

        isChangingItem = false;

        // Se houve outro pedido durante o processo, chama de novo
        if (pendingItemAddress != null)
        {
            string next = pendingItemAddress;
            pendingItemAddress = null;
            SetVisualItemLocal(next);
        }
    }

    [ServerRpc]
    private void SetVisualItemServerRpc(string itemAddress)
    {
        if (currentHandItem != null) currentHandItem.GetComponent<NetworkObject>().Despawn(true);

        var handle = Addressables.LoadAssetAsync<GameObject>(itemAddress);
        handle.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                currentHandItem = Instantiate(op.Result, playerHandsObj.transform);

                // --- ADICIONE ESTA LINHA ---
                NeutralizeHeldItemComponents(currentHandItem);

                var netObj = currentHandItem.GetComponent<NetworkObject>();
                netObj.SpawnWithOwnership(OwnerClientId);
                netObj.TrySetParent(playerHandsObj.GetComponent<NetworkObject>());
            }
            Addressables.Release(op);
        };
    }


    [ServerRpc]
    private void ClearVisualItemServerRpc()
    {
        // A lógica de Despawn AGORA acontece 100% no servidor.
        // O cliente só "pede".
        if (currentHandItem != null)
        {
            var netObj = currentHandItem.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true); // O Servidor SEMPRE tem autoridade para despawnar.
            }
            else
            {
                // Fallback caso algo dê errado e o objeto não seja de rede
                Destroy(currentHandItem);
            }
            currentHandItem = null;

            // Informa o cliente que o objeto foi destruído, para limpar a referência dele.
            // Isso previne "objetos fantasmas" no cliente.
            ClearVisualItemClientRpc();
        }
    }

    [ClientRpc]
    private void ClearVisualItemClientRpc()
    {
        // Apenas os clientes (não o servidor, para evitar dupla destruição) limpam a referência
        // caso o objeto já não tenha sido destruído pelo Despawn.
        if (!IsServer && currentHandItem != null)
        {
            // Se o Despawn não destruiu o objeto (o que é raro), força a destruição
            Destroy(currentHandItem);
            currentHandItem = null;
        }
    }


    #region ClearHandItem


    public void ClearHandItem()
    {
      
        currentHandItemData = null;


        if (currentHandItem != null)
        {
            if (IsSinglePlayer())
            {
                // O ClearHandItemLocally vai destruir o objeto
                ClearHandItemLocally();
            }
            else
            {
                // O ServerRpc vai despawnar o objeto
                ClearHandItemServerRpc();
            }
        }
    }

    private void ClearHandItemLocally()
    {
        //[] Destroi Item na Hotbar ao trocar de Slot se o mesmo estiver vazio


        if (currentHandItem != null)
        {
            Destroy(currentHandItem);
            currentHandItem = null;

            Debug.Log("As mãos do jogador foram limpas localmente.");
        }
        else
        {
            Debug.Log("Nenhum item a ser limpo");
        }
    }

    [ServerRpc]
    public void ClearHandItemServerRpc()
    {
        if (currentHandItem != null)
        {
            var networkObject = currentHandItem.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn(true);
            }
            else
            {
                Destroy(currentHandItem);
            }
            currentHandItem = null;
            Debug.Log("Item despawnado no servidor.");
        }

        ClearHandItemClientRpc();
    }

    [ClientRpc]
    public void ClearHandItemClientRpc()
    {
        if (currentHandItem != null)
        {
            Destroy(currentHandItem);
            currentHandItem = null;
            Debug.Log("As mãos do jogador foram limpas no cliente.");
        }
    }

    #endregion

    #region SpawnHands

    private void SpawnHands()
    {
        if (playerHandsObj != null)
            return;

        if (playerHandsPrefab == null)
        {
            Debug.LogError("playerHandsPrefab não está atribuído!");
            return;
        }

        SpawnHandsServerRpc();

    }

    public void SetHandsObject(GameObject hands)
    {
        playerHandsObj = hands;
    }

    private void SpawnHandsLocal()
    {
        if (playerHandsObj != null) return;

        // Criação local sem NetworkObject
        playerHandsObj = Instantiate(playerHandsPrefab, transform);
        playerHandsObj.transform.localPosition = Vector3.zero;
        
       

    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnHandsServerRpc(ServerRpcParams rpcParams = default)
    {
        if (playerHandsPrefab == null)
        {
            Debug.LogError("playerHandsPrefab está nulo no ServerRpc!");
            return;
        }

        // Only spawn if it doesn't exist yet
        if (playerHandsObj != null)
        {
            return;
        }

        GameObject hands = Instantiate(playerHandsPrefab, transform.position, Quaternion.identity);
        var handsNetworkObj = hands.GetComponent<NetworkObject>();

        // Spawn with ownership
        handsNetworkObj.SpawnWithOwnership(OwnerClientId);

        // Set parent on the server
        hands.transform.SetParent(transform);
        hands.transform.localPosition = Vector3.zero;

        // Assign the reference on server
        playerHandsObj = hands;

        // Sync to client
        AssignHandsToPlayerClientRpc(handsNetworkObj.NetworkObjectId);
    }

    [ClientRpc]
    private void AssignHandsToPlayerClientRpc(ulong handsNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(handsNetworkId, out NetworkObject handsNetObj))
        {
            playerHandsObj = handsNetObj.gameObject;
            playerHandsObj.transform.SetParent(transform);
            playerHandsObj.transform.localPosition = Vector3.zero;
            Debug.Log($"playerHandsObj atribuído no cliente: {playerHandsObj.name}, Pai: {playerHandsObj.transform.parent.name}", this);
        }
        else
        {
            Debug.LogError($"Hands com NetworkObjectId {handsNetworkId} não encontrado no cliente!");
        }
    }


    #endregion

    void Update()
    {

        if (mainCamera == null)
        {
            FindOrAssignCamera(); // tenta encontrar a câmera de novo
            if (mainCamera == null || !mainCamera.gameObject)
            {
                Debug.Log("Cameramain não encontrada");
                return;
            }
        }


        if (!GetComponent<NetworkObject>().IsOwner)
        {
            return;
        }


        PickupMouseDir();

        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }

        Andar();

    }


    private void PickupMouseDir()
    {
        //[]Pega a posição do mouse na tela
        Vector3 mousePos = Input.mousePosition;

        /*
          []De forma resumida esse código pega a posição do objeto no mundo através do transform.position, 
          e converte nas coordenadas da tela em relação a câmera setada na variável mainCamera.
        */

        Vector3 playerPos = mainCamera.WorldToScreenPoint(transform.position);

        float deltaX = mousePos.x - playerPos.x;
        float deltaY = mousePos.y - playerPos.y;

        Vector2 lookDir = new Vector2(deltaX, deltaY).normalized;

        playerAnim.SetFloat("LookX", lookDir.x);
        playerAnim.SetFloat("LookY", lookDir.y);

        if (Mathf.Abs(lookDir.x) > Mathf.Abs(lookDir.y))
        {
            facingDirection = (lookDir.x > 0) ? 4 : 3; // 4=Direita, 3=Esquerda
        }
        else
        {
            facingDirection = (lookDir.y > 0) ? 1 : 2; // 1=Cima, 2=Baixo
        }

    }



    void FixedUpdate()
    {
        if (GetComponent<NetworkObject>().IsOwner)
        {
            // Se o jogador estiver sofrendo knockback, interrompa a execução do movimento.
            if (playerHealth != null && playerHealth.isKnockedBack)
            {
                return;
            }

            Vector2 move = inputs.MovementInput().normalized;
            _playerRigidbody2d.linearVelocity = move * _playerSpeed;
        }
    }
    private void Attack()
    {

        if (currentHandItemData == null)
        {
            Debug.Log("Nenhum item equipado → não pode atacar.");
            return;
        }
        // O cliente NÃO ativa o trigger diretamente.
        // Em vez disso, ele pede ao servidor.
        if (IsOwner && !IsSinglePlayer()) // Apenas o dono pode pedir para atacar
        {
            AttackServerRpc(facingDirection);
        }
        else
        {
            AttackSingleplayer(facingDirection);
        }
    }

    [ServerRpc]
    private void AttackServerRpc(int direction)
    {
        playerAnim.SetInteger("AttackDir", direction);
        playerAnim.SetTrigger("Attack");

        // Opcional: Para garantir que o próprio servidor veja a animação
        // (às vezes o Netcode precisa de um empurrãozinho)
        // AttackClientRpc(direction);
    }

    private void AttackSingleplayer(int direction)
    {
        playerAnim.SetInteger("AttackDir", direction);
        playerAnim.SetTrigger("Attack");
    }


}