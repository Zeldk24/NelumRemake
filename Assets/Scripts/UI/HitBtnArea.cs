using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HitBtnArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Botão alvo")]
    public Image targetImage; // O botão real que vai trocar sprite

    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite hoverSprite;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage != null)
            targetImage.sprite = hoverSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetImage != null)
            targetImage.sprite = normalSprite;
    }

}
