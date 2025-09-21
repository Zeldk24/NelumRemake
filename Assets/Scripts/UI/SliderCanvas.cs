using UnityEngine;

public class SliderCanvas : MonoBehaviour
{
    [Header("Configura��es do Movimento")]
    public RectTransform panel;      // O painel que cont�m todo o menu
    public float speed = 5f;

    [Header("Apenas para Debug")]
    [SerializeField] // Mostra a vari�vel privada no Inspector para debug
    private Vector2 targetPosition; // Posi��o alvo

    private bool moving = false;

    void Update()
    {
        if (moving)
        {
            // Move o painel em dire��o ao alvo
            panel.anchoredPosition = Vector2.Lerp(
                panel.anchoredPosition,
                targetPosition,
                Time.deltaTime * speed
            );

            // Opcional: Para o movimento quando estiver muito perto do alvo
            if (Vector2.Distance(panel.anchoredPosition, targetPosition) < 0.1f)
            {
                panel.anchoredPosition = targetPosition; // Garante a posi��o final exata
                moving = false;
            }
        }
    }

    // --- MUDAN�A PRINCIPAL ---
    // Esta fun��o agora aceita um Vector2 como argumento.
    // Vamos dar um nome mais descritivo a ela.
    public void MovePanelTo()
    {
        // Define a nova posi��o alvo com base no que o bot�o enviou
        this.targetPosition = new Vector2(-1920, 0);
        // Inicia o movimento
        this.moving = true;
    }

    public void MovePanelToBackMenu()
    {
        this.targetPosition = new Vector2(0, 0);

        this.moving = true;
}
}