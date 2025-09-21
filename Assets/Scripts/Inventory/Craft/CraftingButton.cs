// CraftingButton.cs (MODIFICADO)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // <<-- ADICIONE ESTA LINHA

// Implemente as duas interfaces para detectar o mouse
public class CraftingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Recipe recipe;
    private Button button;
    private Image icon;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        button = GetComponent<Button>();
        icon = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();

        button.onClick.AddListener(OnClick);
    }

    public void Setup(Recipe newRecipe)
    {
        recipe = newRecipe;
        icon.sprite = recipe.result.sprite;

        // Lógica de "CanCraft" para deixar o botão semi-transparente
        bool canCraft = Inventory.Singleton.CanCraft(recipe);
        canvasGroup.alpha = canCraft ? 1f : 0.5f;
        button.interactable = canCraft;
    }

    void OnClick()
    {
        Inventory.Singleton.Craft(recipe);
        // Não é ideal usar FindAnyObjectByType, mas para o seu caso atual, funciona.
        // O melhor seria o CraftingUI se inscrever no evento OnInventoryChanged.
        FindAnyObjectByType<CraftingUI>().RefreshCraftingUI();
    }

    // --- LÓGICA DO TOOLTIP ADICIONADA AQUI ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Quando o mouse entra, mostra o tooltip com a receita deste botão
        if (recipe != null)
        {
            TooltipManager.Instance.MostrarTooltip(recipe);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Quando o mouse sai, esconde o tooltip
        TooltipManager.Instance.EsconderTooltip();
    }
}