// TooltipManager.cs (NOVO SCRIPT)

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("Componentes do Tooltip")]
    public GameObject painelTooltip;
    public TextMeshProUGUI tituloTexto;
    public Transform containerDeIngredientes;
    public GameObject prefabLinhaIngrediente; // Prefab com Image e TextMeshPro

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (painelTooltip != null) painelTooltip.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        RectTransform rt = painelTooltip.GetComponent<RectTransform>();
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform,
            Input.mousePosition,
            null,
            out mousePos
        );
        Vector2 offset = new Vector2(rt.rect.width / 2 + 35, -rt.rect.height / 2 - 35);
        rt.localPosition = mousePos + offset;


    }

    // Adaptado para usar suas classes 'Recipe' e 'Ingredient'
    public void MostrarTooltip(Recipe receita)
    {
        if (painelTooltip == null) return;

        // Limpa os ingredientes antigos
        foreach (Transform child in containerDeIngredientes)
        {
            Destroy(child.gameObject);
        }

        // Define o título com o nome do item resultado
        tituloTexto.text = receita.result.itemName;

        // Preenche a lista de ingredientes
        foreach (var ingrediente in receita.ingredients)
        {
            GameObject linhaObj = Instantiate(prefabLinhaIngrediente, containerDeIngredientes);

            // Supondo que seu prefab tenha os componentes abaixo.
            // Para mais robustez, crie um script "LinhaIngredienteUI.cs" para o prefab.
            Image iconeIngrediente = linhaObj.transform.Find("Icon").GetComponent<Image>(); // Substitua "Icon" pelo nome do seu objeto de imagem
            TextMeshProUGUI textoDaLinha = linhaObj.GetComponentInChildren<TextMeshProUGUI>();

            iconeIngrediente.sprite = ingrediente.item.sprite;

            // Checa se o jogador tem os itens (você precisa implementar Inventory.Singleton.GetItemCount)
            int quantidadeNoInventario = Inventory.Singleton.GetItemCountByID(ingrediente.item.itemID);
            bool temItensSuficientes = quantidadeNoInventario >= ingrediente.quantity;

            // Formata o texto e colore de vermelho se não tiver recursos
            string textoQuantidade = $"<color={(temItensSuficientes ? "white" : "red")}>{quantidadeNoInventario}</color>/{ingrediente.quantity}";
            textoDaLinha.text = textoQuantidade;
        }

        painelTooltip.SetActive(true);
    }

    public void EsconderTooltip()
    {
        if (painelTooltip != null) painelTooltip.SetActive(false);
    }
}