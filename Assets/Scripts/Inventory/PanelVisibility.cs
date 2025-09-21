using UnityEngine;

public class PanelVisibility : MonoBehaviour
{
    [SerializeField] private Canvas inventoryCanvas; // Canvas do invent�rio
    
    // Vari�vel para armazenar o estado do invent�rio
    private bool isInventoryOpen = false;

    private void Start()
    {
        // Inicialmente, desativa os Canvas
        inventoryCanvas.enabled = false;
        
    }

    private void Update()
    {
        // Alterna o estado dos Canvas ao pressionar a tecla "E"
        if (Input.GetKeyDown(KeyCode.E))
        {
            isInventoryOpen = !isInventoryOpen;
            ToggleInventory(isInventoryOpen);
        }
    }

    private void ToggleInventory(bool open)
    {
        // Ativa ou desativa os Canvas
        inventoryCanvas.enabled = open;
       
    }
}
