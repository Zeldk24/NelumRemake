using UnityEngine;

public class SliderCanvas : MonoBehaviour
{
    [Header("Configurações do Movimento")]
    public RectTransform panel;      // O painel que contém todo o menu
    public float speed = 5f;

    [Header("Apenas para Debug")]
    [SerializeField] // Mostra a variável privada no Inspector para debug
    private Vector2 targetPosition; // Posição alvo

    private bool moving = false;

    void Update()
    {
        if (moving)
        {
            // Move o painel em direção ao alvo
            panel.anchoredPosition = Vector2.Lerp(
                panel.anchoredPosition,
                targetPosition,
                Time.deltaTime * speed
            );

            // Opcional: Para o movimento quando estiver muito perto do alvo
            if (Vector2.Distance(panel.anchoredPosition, targetPosition) < 0.1f)
            {
                panel.anchoredPosition = targetPosition; // Garante a posição final exata
                moving = false;
            }
        }
    }

    // --- MUDANÇA PRINCIPAL ---
    // Esta função agora aceita um Vector2 como argumento.
    // Vamos dar um nome mais descritivo a ela.
    public void MovePanelTo()
    {
        // Define a nova posição alvo com base no que o botão enviou
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