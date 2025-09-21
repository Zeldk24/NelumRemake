using UnityEngine;
using UnityEngine.UI;

public class CraftingUI : MonoBehaviour
{
    public GameObject craftingButtonPrefab;
    public Transform craftingPanel;
    public Recipe[] allRecipes;



    public void RefreshCraftingUI()
    {
        foreach (Transform child in craftingPanel)
            Destroy(child.gameObject);

        foreach (var recipe in allRecipes)
        {
            GameObject obj = Instantiate(craftingButtonPrefab, craftingPanel);
            var button = obj.GetComponent<CraftingButton>();
            button.Setup(recipe);
        }
    }

    void Start()
    {
        RefreshCraftingUI();
        Inventory.Singleton.OnInventoryChanged += RefreshCraftingUI;
    }


    void OnDestroy()
    {
        if (Inventory.Singleton != null)
            Inventory.Singleton.OnInventoryChanged -= RefreshCraftingUI;
    }
}
