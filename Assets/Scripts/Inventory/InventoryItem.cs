using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    // Variáveis públicas para fácil acesso e atribuição
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

    // Método auxiliar para mover o item para um novo slot
    public void MoveToSlot(InventorySlot newSlot)
    {
        // Se já estávamos em um slot, limpa a referência dele
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

    // --- Lógica de Arrastar e Soltar (Drag and Drop) ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // Arrastar apenas com botão esquerdo

        Debug.Log("Begin Drag");

        // Informa ao Singleton do Inventário para começar a arrastar este item
        Inventory.Singleton.StartDraggingItem(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Esta função precisa existir para a interface IDragHandler, mas não precisa fazer nada.
        // O Inventory.Singleton.Update() já está movendo o item que está sendo carregado.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Debug.Log("End Drag");

        // Informa ao Singleton do Inventário que o arraste terminou
        Inventory.Singleton.EndDraggingItem();
    }

    // --- Lógica de Clique Direito (Right-Click Drop) ---

    public void OnPointerDown(PointerEventData eventData)
    {
        // Apenas reage ao botão direito do mouse
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("Right-Click detectado para dropar item.");

            // Chama o método no inventário, passando este item como referência.
            // NÃO alteramos o `activeSlot` aqui. O inventário cuidará disso.
            Inventory.Singleton.DropItemOnClick(this);
        }
    }
}