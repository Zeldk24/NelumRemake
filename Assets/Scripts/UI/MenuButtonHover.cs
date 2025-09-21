using UnityEngine;
using UnityEngine.EventSystems;

// ALTERADO: Adicionamos a interface IPointerExitHandler
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler
{
    public bool useCustomOffset = false;
    public Vector2 customOffset = Vector2.zero;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameManager.Instance == null) return;

        // A lógica que você já tem está perfeita.
        if (useCustomOffset)
        {
            GameManager.Instance.MoveTo(transform, customOffset);
        }
        else
        {
            // Se não usar offset custom, ele chama a versão de 1 argumento
            // que usa o defaultOffset do GameManager.
            GameManager.Instance.MoveTo(transform);
        }
    }

   
}