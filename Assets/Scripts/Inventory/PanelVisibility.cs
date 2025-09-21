using UnityEngine;

public class PanelVisibility : MonoBehaviour
{
    [SerializeField] private Canvas inventoryCanvas; // Canvas do inventário
    
    // Variável para armazenar o estado do inventário
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
