using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.AI;

public class BossController : NetworkBehaviour
{
    [Header("Componentes")]
    private Animator animator;
    public ParticleSystem particleSystems;
    private InimigoStateMachine stateMachine;

    [Header("Referências")]
    public EnemyController enemyController;
    private Transform player;
    private NavMeshAgent agent;

    [Header("Dash Config")]
    public float dashSpeed = 12f;
    public float dashCooldown = 1.5f;
    public GameObject dashIndicatorPrefab;

    public bool isInDashPhase = false;
    private bool dashAvailable = true;
    private bool isDashing = false;

    private Collider2D dashCollider;
    // --- Layers ---
    private string dashLayerName = "Wall";
    private bool _hasHitPlayerThisDash = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        particleSystems = GetComponentInChildren<ParticleSystem>();
        stateMachine = GetComponent<InimigoStateMachine>();
        dashCollider = GetComponent<Collider2D>();
        Debug.Log("Collider encontrado: " + dashCollider);

    }

    private void Start()
    {
        TriggerBossMersilas trigger = FindObjectOfType<TriggerBossMersilas>();
        if (trigger != null)
        {
            trigger.RegisterBoss(this);
        }

        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    private void Update()
    {
      
        if (enemyController == null || player == null) return;

        // Controle do dash apenas na fase 2
        if (isInDashPhase && dashAvailable && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        dashAvailable = false;
        isDashing = false;
        _hasHitPlayerThisDash = false;
        agent.enabled = false;

        try
        {
            if (player == null) yield break;

            Camera cam = Camera.main;
            if (cam == null) yield break;

            float targetY = player.position.y;

            // Decide se o recuo será pra esquerda ou direita (aleatório)
            // Decide se o recuo será pra esquerda ou direita
            bool recuaEsquerda = Random.value > 0.5f;

            // decide também se sobe ou desce (pra não cruzar o player no meio)
            bool sobe = Random.value > 0.5f;

            float prepDistanceX = 5f;
            float prepDistanceY = 3f;

            // ponto alvo do recuo em arco
            Vector3 prepTarget = player.position
                + (recuaEsquerda ? Vector3.left : Vector3.right) * prepDistanceX
                + (sobe ? Vector3.up : Vector3.down) * prepDistanceY;

            // move até esse ponto (não cruza o player, porque tem offset em Y)
            yield return MoveToPosition(prepTarget, Vector3.Distance(transform.position, prepTarget) / dashSpeed);


            // --- Agora define o lado do dash (oposto ao recuo) ---
            bool fromLeft = !recuaEsquerda;

            float camHeight = 2f * cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            Vector3 offscreenPos = fromLeft ?
                new Vector3(cam.transform.position.x - camWidth / 2 - 4f, targetY, 0f) :
                new Vector3(cam.transform.position.x + camWidth / 2 + 4f, targetY, 0f);

            // Vai até fora da tela
            yield return MoveToPosition(offscreenPos, Vector3.Distance(transform.position, offscreenPos) / dashSpeed);

            // Cria o indicador
            if (dashIndicatorPrefab != null)
            {
                Vector3 indicatorPos = new Vector3(cam.transform.position.x, targetY, 0f);
                Quaternion rotation = Quaternion.Euler(0, 0, 90f);
                GameObject indicator = Instantiate(dashIndicatorPrefab, indicatorPos, rotation);

                float indicatorWidth = camWidth + 8f;
                indicator.transform.localScale = new Vector3(3.4f, indicatorWidth, indicator.transform.localScale.z);

                Destroy(indicator, 1f);
            }

            yield return new WaitForSeconds(1f);

            // --- DASH REAL ---
            isDashing = true;

            Vector3 dashTarget = fromLeft ?
                new Vector3(cam.transform.position.x + camWidth / 2 + 4f, targetY, 0f) :
                new Vector3(cam.transform.position.x - camWidth / 2 - 4f, targetY, 0f);

            while (Vector3.Distance(transform.position, dashTarget) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);
                yield return null;
            }
        }
        finally
        {
            isDashing = false;
        }

        yield return new WaitForSeconds(dashCooldown);
        dashAvailable = true;
    }


    private IEnumerator MoveToPosition(Vector3 target, float duration)
    {
        float time = 0;
        Vector3 startPosition = transform.position;
        while (time < duration)
        {
            transform.position = Vector3.Lerp(startPosition, target, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = target;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing && !_hasHitPlayerThisDash && other.CompareTag("Player"))
        {

            _hasHitPlayerThisDash = true;

            isDashing = false;
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(1, transform.position);

            }

           
        }
    }

    private IEnumerator waitroar()
    {
        animator.SetTrigger("Roar");
        if (particleSystems != null) particleSystems.Play();
        yield return new WaitForSeconds(3f);
        if (particleSystems != null) particleSystems.Stop();
    }

    public void PlayRoar()
    {
        StartCoroutine(waitroar());
    }

    // --- NOVO: método para entrar na fase 2 ---
    public void EnterDashPhase()
    {
        if (isInDashPhase) return; // evita desativar no Start
        isInDashPhase = true;

        enemyController.isAiPermanentlyDisabled = true;
        if (stateMachine != null)
            stateMachine.enabled = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        

        if (dashCollider != null)
        {
            dashCollider.isTrigger = true;
        }
        Debug.Log("BOSS ENTROU NA FASE 2! NAVMESH DESATIVADO.");
    }
}
