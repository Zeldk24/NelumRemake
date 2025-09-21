using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    // Vari�veis p�blicas para f�cil acesso e atribui��o
    public Item myItemScriptable { get; set; }
    public InventorySlot activeSlot { get; set; }

    // Componentes cacheados para performance
    private Image itemIcon;
    public CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        itemIcon = GetComponent<Image>();
    }

    public void Initialize(Item item, InventorySlot parentSlot)
    {
        myItemScriptable = item;
        itemIcon.sprite = item.sprite;

        // Atribui o item ao novo slot
        MoveToSlot(parentSlot);
    }

    // M�todo auxiliar para mover o item para um novo slot
    public void MoveToSlot(InventorySlot newSlot)
    {
        // Se j� est�vamos em um slot, limpa a refer�ncia dele
        if (activeSlot != null)
        {
            activeSlot.myItem = null;
        }

        // Atualiza para o novo slot
        activeSlot = newSlot;

        // Informa ao novo slot que este item agora pertence a ele
        if (newSlot != null)
        {
            newSlot.myItem = this;
            transform.SetParent(newSlot.transform);
            transform.localPosition = Vector3.zero;
        }
    }

    // --- L�gica de Arrastar e Soltar (Drag and Drop) ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // Arrastar apenas com bot�o esquerdo

        Debug.Log("Begin Drag");

        // Informa ao Singleton do Invent�rio para come�ar a arrastar este item
        Inventory.Singleton.StartDraggingItem(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Esta fun��o precisa existir para a interface IDragHandler, mas n�o precisa fazer nada.
        // O Inventory.Singleton.Update() j� est� movendo o item que est� sendo carregado.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Debug.Log("End Drag");

        // Informa ao Singleton do Invent�rio que o arraste terminou
        Inventory.Singleton.EndDraggingItem();
    }

    // --- L�gica de Clique Direito (Right-Click Drop) ---

    public void OnPointerDown(PointerEventData eventData)
    {
        // Apenas reage ao bot�o direito do mouse
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("Right-Click detectado para dropar item.");

            // Chama o m�todo no invent�rio, passando este item como refer�ncia.
            // N�O alteramos o `activeSlot` aqui. O invent�rio cuidar� disso.
            Inventory.Singleton.DropItemOnClick(this);
        }
    }
}