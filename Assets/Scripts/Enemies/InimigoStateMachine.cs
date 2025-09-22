using UnityEngine;
using UnityEngine.AI; // Necess�rio para o NavMeshAgent

// Enum para definir os estados poss�veis da nossa IA
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

    [Header("Configura��es de Patrulha")]
    public Transform[] pontosDePatrulha;
    private int indicePontoAtual = 0;

    [Header("Configura��es de Detec��o")]
    public float raioDeVisao = 10f;
    public float raioDePerdaDeVisao = 15f; // Um raio maior para n�o perder o alvo t�o f�cil
    public LayerMask camadaDoJogador; // Para o Physics.CheckSphere s� detectar o jogador

    [Header("Estado Atual")]
    [SerializeField] // Mostra a vari�vel privada no Inspector para debug
    private EstadoIA estadoAtual;

    [Header("StatesPermissions")]
    public bool canPatrol;
    public bool canPursuit;


    public bool isPaused = false;

    void Start()
    {
        // Pega o componente NavMeshAgent automaticamente
        agente = GetComponent<NavMeshAgent>();
        enemyController = GetComponent<EnemyController>();

        // Encontra o jogador pela tag "Player"
        jogador = GameObject.FindGameObjectWithTag("Player").transform;

        // Garante que o inimigo n�o rotacione nos eixos X e Z (essencial para 2D)
        agente.updateRotation = false;
        agente.updateUpAxis = false;

        agente.speed = speed;

        // Inicia no estado de patrulha
        MudarParaEstado(EstadoIA.Patrulhando);
    }

    void Update()
    {

        if (isPaused) return;
        // Se o inimigo est� sofrendo knockback, n�o executa a IA
        if (enemyController != null && enemyController.isKnockedBack) return;
        // --- L�gica normal de IA ---
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

        // A��es de entrada para cada estado
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

    // --- L�GICA DOS ESTADOS ---

    void ExecutarEstadoPatrulha()
    {
        if (agente == null || !agente.enabled || !agente.isOnNavMesh) return;

        // 1. A��O: Patrulhar
        if (!agente.pathPending && agente.remainingDistance < 0.5f)
        {
            IrParaProximoPonto();
        }

        // 2. TRANSI��O: Verificar se o jogador est� no raio de vis�o
        if (JogadorEstaNoRaio(raioDeVisao))
        {
            MudarParaEstado(EstadoIA.Perseguindo);
        }
    }


    void ExecutarEstadoPerseguicao()
    {
        if (enemyController != null && enemyController.isKnockedBack) return;

        if (agente.enabled && agente.isOnNavMesh) // s� executa se o agente estiver ativo
        {
            agente.SetDestination(jogador.position);
        }

        if (!JogadorEstaNoRaio(raioDePerdaDeVisao))
        {
            MudarParaEstado(EstadoIA.Patrulhando);
        }
    }

    // --- M�TODOS AUXILIARES ---

    void IrParaProximoPonto()
    {
        if (pontosDePatrulha.Length == 0) return;

        if (agente.enabled && agente.isOnNavMesh)
        {
            agente.SetDestination(pontosDePatrulha[indicePontoAtual].position);
        }

        indicePontoAtual = (indicePontoAtual + 1) % pontosDePatrulha.Length;
    }

    bool JogadorEstaNoRaio(float raio)
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, raio, camadaDoJogador);

        return hit != null;
    }


  
    private void OnDrawGizmosSelected()
    {
        // Desenha o raio de vis�o
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioDeVisao);

        // Desenha o raio de perda de vis�o
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioDePerdaDeVisao);
    }
}