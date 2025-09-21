using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyCode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static LobbyCode instance;
    public TextMeshProUGUI textCode;
  
  

    private void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        textCode.alpha = 0.1f;
       
       
    }

    public void LastLobbyCode()
    {
        string code = GameManager.lastLobbyCode;
        textCode.text = "Código da sala: " + code;

    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        textCode.alpha = 1f;
        

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        textCode.alpha = 0.1f;
    }
}
