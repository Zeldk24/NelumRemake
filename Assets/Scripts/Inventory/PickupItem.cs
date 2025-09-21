using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PickupItem : NetworkBehaviour
{
    public Item item;

    [SerializeField] private float collectableDelay = 1f;

    // --- Variáveis de Controle ---
    // Usada APENAS em modo multiplayer
    private NetworkVariable<bool> isCollectable_Network = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Usada APENAS em modo single-player
    private bool isCollectable_Local = false;

      // Distância máxima para o item começar a voar até o player
    [SerializeField] private float magnetSpeed = 6f;    // Velocidade com que o item vai em direção ao player

    private Transform targetPlayer;


    #region Lógica de Inicialização (Start/OnNetworkSpawn)

    // Este método é chamado para TODOS os objetos, em qualquer modo.
    void Start()
    {
        // Se NÃO estivermos em uma sessão de rede, inicie a lógica de single-player.
        if (!IsNetworkActive())
        {
            StartCoroutine(MakeItemCollectable_Local());
        }
    }

    // Este método é chamado APENAS quando o objeto é spawnado na rede.
    public override void OnNetworkSpawn()
    {
        // Apenas o servidor deve controlar quando o item se torna coletável.
        if (IsServer)
        {
            StartCoroutine(MakeItemCollectable_Network());
        }
    }

    // Corotina para modo multiplayer
    private IEnumerator MakeItemCollectable_Network()
    {
        yield return new WaitForSeconds(collectableDelay);
        isCollectable_Network.Value = true;
    }

    // Corotina para modo single-player
    private IEnumerator MakeItemCollectable_Local()
    {
        yield return new WaitForSeconds(collectableDelay);
        isCollectable_Local = true;
    }

    #endregion


    #region Lógica de Coleta (OnTriggerEnter2D)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verificação inicial que vale para ambos os modos
        if (!collision.CompareTag("Player")) return;

        // Verifica em qual modo estamos e executa a lógica apropriada
        if (IsNetworkActive())
        {
            // --- LÓGICA MULTIPLAYER ---
            // Apenas o servidor processa a coleta. Se não for o servidor, ou se o item não for coletável, ignora.
            if (!IsServer || !isCollectable_Network.Value) return;

            PlayerController playerController = collision.GetComponent<PlayerController>();
            if (playerController == null) return;

            ulong ownerClientId = playerController.OwnerClientId;

            // Marca como não coletável imediatamente para evitar coleta dupla
            isCollectable_Network.Value = false;

            if (ownerClientId == NetworkManager.Singleton.LocalClientId) // O Host pegou
            {
                Inventory.Singleton.PickUpItem(item);
            }
            else // Um cliente pegou
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { ownerClientId } }
                };
                ConfirmPickupClientRpc(clientRpcParams);
            }

            GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            // --- LÓGICA SINGLE-PLAYER ---
            // Se o item não for coletável, ignora.
            if (!isCollectable_Local) return;

            // Em single-player, a lógica é simples: pegue o item e se destrua.
            bool pickedUp = Inventory.Singleton.PickUpItem(item);
            if (pickedUp)
            {
                // Simplesmente destrói o objeto, já que não há rede.
                Destroy(gameObject);
            }
        }
    }

    [ClientRpc]
    private void ConfirmPickupClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Inventory.Singleton.PickUpItem(item);
    }

    #endregion

    /// <summary>
    /// Função auxiliar para verificar se o jogo está rodando em uma sessão de rede.
    /// </summary>
    private bool IsNetworkActive()
    {
        // Retorna true se o NetworkManager existir e estiver escutando (como Host, Servidor ou Cliente).
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    void Update()
    {
        // --- Só ativa o ímã se o item for coletável ---
        if (IsNetworkActive())
        {
            if (!IsServer || !isCollectable_Network.Value) return;
        }
        else
        {
            if (!isCollectable_Local) return;
        }

        // Se já tem um player alvo, voa até ele
        if (targetPlayer != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, magnetSpeed * Time.deltaTime);
        }
        else
        {
            // Se não tem alvo, procura algum player no range
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    targetPlayer = hit.transform;
                    break;
                }
            }
        }
    }



    private void OnDrawGizmosSelected()
{
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, 1f);
}
}