using UnityEngine;
using UnityEngine.AI; // Necessário para o NavMeshAgent

// Enum para definir os estados possíveis da nossa IA
public enum EstadoIA
{
    Patrulhando,
    Perseguindo
}

public class InimigoStateMachine : MonoBehaviour
{
    [Header("Componentes")]
    public NavMeshAgent agente;
    private Transform jogador;
    private EnemyController enemyController;

    private float speed = 2f;

    [Header("Configurações de Patrulha")]
    public Transform[] pontosDePatrulha;
    private int indicePontoAtual = 0;

    [Header("Configurações de Detecção")]
    public float raioDeVisao = 10f;
    public float raioDePerdaDeVisao = 15f; // Um raio maior para não perder o alvo tão fácil
    public LayerMask camadaDoJogador; // Para o Physics.CheckSphere só detectar o jogador

    [Header("Estado Atual")]
    [SerializeField] // Mostra a variável privada no Inspector para debug
    private EstadoIA estadoAtual;

    [Header("StatesPermissions")]
    [SerializeField] private bool canPatrol;
    [SerializeField] private bool canPursuit;


    public bool isPaused = false;

    void Start()
    {
        // Pega o componente NavMeshAgent automaticamente
        agente = GetComponent<NavMeshAgent>();
        enemyController = GetComponent<EnemyController>();

        // Encontra o jogador pela tag "Player"
        jogador = GameObject.FindGameObjectWithTag("Player").transform;

        // Garante que o inimigo não rotacione nos eixos X e Z (essencial para 2D)
        agente.updateRotation = false;
        agente.updateUpAxis = false;

        agente.speed = speed;

        // Inicia no estado de patrulha
        MudarParaEstado(EstadoIA.Patrulhando);
    }

    void Update()
    {

        if (isPaused) return;
        // Se o inimigo está sofrendo knockback, não executa a IA
        if (enemyController != null && enemyController.isKnockedBack) return;
        // --- Lógica normal de IA ---
        switch (estadoAtual)
        {
            case EstadoIA.Patrulhando:
                if(canPatrol) ExecutarEstadoPatrulha();
                break;
            case EstadoIA.Perseguindo:
                if(canPursuit) ExecutarEstadoPerseguicao();
                break;
        }
    }

    private void MudarParaEstado(EstadoIA novoEstado)
    {
        estadoAtual = novoEstado;

        // Ações de entrada para cada estado
        switch (estadoAtual)
        {
            case EstadoIA.Patrulhando:
                // Define a velocidade para patrulha
                agente.speed = 2f;
                // Inicia a patrulha do ponto atual
                if (pontosDePatrulha.Length > 0)
                {
                    agente.SetDestination(pontosDePatrulha[indicePontoAtual].position);
                }
                break;
            case EstadoIA.Perseguindo:
                // Aumenta a velocidade ao perseguir
                agente.speed = 2.5f;
                break;
        }
    }

    // --- LÓGICA DOS ESTADOS ---

    void ExecutarEstadoPatrulha()
    {
        // 1. AÇÃO: Patrulhar
        // Se o inimigo chegou perto do seu destino (ponto de patrulha)
        if (!agente.pathPending && agente.remainingDistance < 0.5f)
        {
            IrParaProximoPonto();
        }

        // 2. TRANSIÇÃO: Verificar se o jogador está no raio de visão
        if (JogadorEstaNoRaio(raioDeVisao))
        {
            MudarParaEstado(EstadoIA.Perseguindo);
        }
    }

    void ExecutarEstadoPerseguicao()
    {
        if (enemyController != null && enemyController.isKnockedBack) return;

        agente.SetDestination(jogador.position);

        // Opcional: fazer o inimigo olhar para o jogador
        Vector2 direcao = (jogador.position - transform.position).normalized;
        // (Aqui você poderia usar a 'direcao' para mudar o sprite ou rotação do inimigo)

        // 2. TRANSIÇÃO: Verificar se o jogador saiu do raio de visão
        if (!JogadorEstaNoRaio(raioDePerdaDeVisao))
        {
            MudarParaEstado(EstadoIA.Patrulhando);
        }
    }

    // --- MÉTODOS AUXILIARES ---

    void IrParaProximoPonto()
    {
        // Se não houver pontos de patrulha, não faz nada
        if (pontosDePatrulha.Length == 0) return;

        // Define o destino para o ponto atual
        agente.SetDestination(pontosDePatrulha[indicePontoAtual].position);

        // Atualiza o índice para o próximo ponto, voltando ao início se chegar no fim
        indicePontoAtual = (indicePontoAtual + 1) % pontosDePatrulha.Length;
    }

    bool JogadorEstaNoRaio(float raio)
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, raio, camadaDoJogador);

        return hit != null;
    }


  
    private void OnDrawGizmosSelected()
    {
        // Desenha o raio de visão
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioDeVisao);

        // Desenha o raio de perda de visão
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioDePerdaDeVisao);
    }
}