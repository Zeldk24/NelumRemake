using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.AI;

public class BossController : NetworkBehaviour
{
    [Header("Componentes")]
    private Animator animator;
    public ParticleSystem particleSystems;

    [Header("Referências")]
    public EnemyController enemyController; // Vida do boss
    private Transform player;
    private NavMeshAgent agent;

    [Header("Dash Config")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 0.5f;
    public float dashDistance = 10f;
    public GameObject dashIndicatorPrefab;

    private bool isInDashPhase = false;
    private bool dashAvailable = true;
    private bool isDashing = false;

    private Vector3 dashStartPosition; // posição inicial do dash

    // === Layers ===
       // layer normal do boss
    private string dashLayerName = "Wall";   // layer que criamos só pra colidir com player

    private void Awake()
    {
        animator = GetComponent<Animator>();
        particleSystems = GetComponentInChildren<ParticleSystem>();
    }

    private void Start()
    {
        // Trigger de boss
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
        if (enemyController == null || player == null || agent == null) return;

        // Ativa fase de dash quando vida <=50%
        if (!isInDashPhase && enemyController.currentHealh <= enemyController.enemiesData.health / 2)
        {
            isInDashPhase = true;
        }

        if (isInDashPhase && dashAvailable && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        dashAvailable = false;
        isDashing = true;

        InimigoStateMachine statemachine = GetComponent<InimigoStateMachine>();
        statemachine.isPaused = true;

        // === 🔹 MUDA PARA LAYER DO DASH ===
        gameObject.layer = LayerMask.NameToLayer(dashLayerName);

        // Desativa NavMesh enquanto dasha
        if (agent != null) agent.enabled = false;

        // === 1. Captura a altura do jogador no momento do ataque ===
        float targetY = player.position.y;

        bool fromLeft = Random.value > 0.5f;
        Vector3 recuoDir = fromLeft ? Vector3.right : Vector3.left;

        // === 2. Passo para trás antes de sair da tela ===
        Vector3 recuoTarget = transform.position + recuoDir * 2f;
        float recuoTime = 0.3f;
        float t = 0f;
        Vector3 recuoStart = transform.position;

        while (t < recuoTime)
        {
            transform.position = Vector3.Lerp(recuoStart, recuoTarget, t / recuoTime);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = recuoTarget;

        // === 3. Calcula posições offscreen ===
        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Vector3 offscreenPos;
        if (fromLeft)
            offscreenPos = new Vector3(cam.transform.position.x - camWidth / 2 - 4f, targetY, 0f);
        else
            offscreenPos = new Vector3(cam.transform.position.x + camWidth / 2 + 4f, targetY, 0f);

        // === 4. Move o boss até fora da tela ===
        while (Vector3.Distance(transform.position, offscreenPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, offscreenPos, dashSpeed * Time.deltaTime);
            yield return null;
        }

        // === 5. Cria indicador horizontal ===
        if (dashIndicatorPrefab != null)
        {
            Vector3 indicatorPos = new Vector3(cam.transform.position.x, targetY, 0f);
            Quaternion rotation = Quaternion.Euler(0, 0, 90f);
            GameObject indicator = Instantiate(dashIndicatorPrefab, indicatorPos, rotation);

            indicator.transform.localScale = new Vector3(
                indicator.transform.localScale.x,
                camWidth + 8f,
                indicator.transform.localScale.z
            );

            Destroy(indicator, 1f);
        }

        yield return new WaitForSeconds(1f);

        // === 6. Define alvo final do dash ===
        Vector3 dashTarget;
        if (fromLeft)
            dashTarget = new Vector3(cam.transform.position.x + camWidth / 2 + 4f, targetY, 0f);
        else
            dashTarget = new Vector3(cam.transform.position.x - camWidth / 2 - 4f, targetY, 0f);

        // === 7. Dash em linha reta ===
        while (Vector3.Distance(transform.position, dashTarget) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);
            yield return null;
        }

        // === 8. Reseta estado ===
        isDashing = false;
        statemachine.isPaused = false;

        // 🔹 IMPORTANTE: mantém na layer "BossDash" pra SEMPRE atravessar cenário
        // Se quisesse voltar ao normal: gameObject.layer = LayerMask.NameToLayer(defaultLayerName);

        yield return new WaitForSeconds(dashCooldown);
        dashAvailable = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing && other.CompareTag("Player"))
        {
            StopAllCoroutines();
            isDashing = false;

            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(1, transform.position);
            }

            Rigidbody2D rbPlayer = other.GetComponent<Rigidbody2D>();
            if (rbPlayer != null)
            {
                Vector2 knockDir = (other.transform.position - transform.position).normalized;
                float knockForce = 8f;
                rbPlayer.AddForce(knockDir * knockForce, ForceMode2D.Impulse);
            }

            StartCoroutine(SmoothReturnAfterHit());
        }
    }

    private IEnumerator SmoothReturnAfterHit()
    {
        Vector3 recuoTarget = dashStartPosition - (player.position - dashStartPosition).normalized * 1f;
        float recuoTime = 1f;
        float t = 0f;
        Vector3 recuoStart = transform.position;

        while (t < recuoTime)
        {
            transform.position = Vector3.Lerp(recuoStart, recuoTarget, t / recuoTime);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = recuoTarget;

        yield return new WaitForSeconds(2f);
        dashAvailable = true;
    }

    private IEnumerator waitroar()
    {
        animator.SetTrigger("Roar");

        if (particleSystems != null)
            particleSystems.Play();

        yield return new WaitForSeconds(3f);
        particleSystems.Stop();
    }

    public void PlayRoar()
    {
        StartCoroutine(waitroar());
    }
}
