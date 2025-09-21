using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class InventorySlot : MonoBehaviour, IDropHandler
{

    public Image image;
    public Color selectedColor, notSelectedColor;
    
    //Representa instância do item no slot
    public InventoryItem myItem { get; set; }

    public SlotTag myTag;

    public void Awake()
    {
        Deselect();
    }

    public void Select()
    {
        image.color = selectedColor;
    }

    public void Deselect() 
    {
        image.color = notSelectedColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Item Dropado");


        if (myTag != SlotTag.None && Inventory.carriedItem.myItemScriptable.itemTag != myTag)
        {

            return;

        }

        if (myItem != null)
        {
            Debug.Log("O slot já está ocupado.");
            return;
        }


        SetItem(Inventory.carriedItem);

    }
    
    public void SetItem(InventoryItem item)
    {
        if (item.activeSlot != null)
        {
            // Libera o slot anterior
            item.activeSlot.myItem = null;
        }

        Inventory.carriedItem = null;

        myItem = item;
        myItem.activeSlot = this;
        myItem.transform.SetParent(transform);
        myItem.canvasGroup.blocksRaycasts = true;

        if (myTag != SlotTag.None)
        {
            Inventory.Singleton.EquipEquipment(myTag, myItem);
        }
    }

   
}
