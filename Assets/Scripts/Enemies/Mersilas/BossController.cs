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
    public float dashCooldown = 1f;
    public float dashDistance = 10f;
    public GameObject dashIndicatorPrefab;

    private bool isInDashPhase = false;
    private bool dashAvailable = true;
    private bool isDashing = false;

    private Vector3 dashStartPosition; // posição inicial do dash

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

        // Dash interrompível
        if (isInDashPhase && dashAvailable && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        dashAvailable = false;
        isDashing = true;

        dashStartPosition = transform.position; // salva posição inicial

        InimigoStateMachine statemachine = GetComponent<InimigoStateMachine>();
        statemachine.isPaused = true;
        agent.isStopped = true;

        Vector2 direction = (player.position - dashStartPosition).normalized;

        float dashOffset = 0.3f;

        // Raycast para detectar parede
        RaycastHit2D hit = Physics2D.Raycast(dashStartPosition, direction, dashDistance, LayerMask.GetMask("Wall"));
        float actualDashDistance = dashDistance;

        if (hit.collider != null)
        {
            actualDashDistance = Mathf.Max(0f, hit.distance - dashOffset);
        }

        Vector3 dashTarget = dashStartPosition + (Vector3)direction * actualDashDistance;

        // Instancia o indicador
        if (dashIndicatorPrefab != null)
        {
            Vector3 indicatorPos = dashStartPosition + (Vector3)direction * (actualDashDistance / 2);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angle + 90f);

            GameObject indicator = Instantiate(dashIndicatorPrefab, indicatorPos, rotation);
            indicator.transform.localScale = new Vector3(
                indicator.transform.localScale.x,
                actualDashDistance,
                indicator.transform.localScale.z
            );
            Destroy(indicator, 1f);
        }

        // Aviso do dash
        yield return new WaitForSeconds(1f);

        // Move o boss até o dashTarget
        float distanceToTarget = Vector3.Distance(transform.position, dashTarget);
        while (distanceToTarget > 0.01f && isDashing)
        {
            Vector3 nextPosition = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);

            // Checa colisão no próximo passo (com parede)
            RaycastHit2D stepHit = Physics2D.Raycast(transform.position, direction, dashSpeed * Time.deltaTime, LayerMask.GetMask("Wall"));
            if (stepHit.collider != null)
                break;

            transform.position = nextPosition;
            distanceToTarget = Vector3.Distance(transform.position, dashTarget);
            yield return null;
        }

        isDashing = false;
        statemachine.isPaused = false;
        agent.isStopped = false;
        yield return new WaitForSeconds(dashCooldown);
        dashAvailable = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing && other.CompareTag("Player"))
        {
            // Cancela dash
            StopAllCoroutines();
            isDashing = false;

            // Aplica dano no jogador
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(1);
            }

            // Aplica knockback no jogador
            Rigidbody2D rbPlayer = other.GetComponent<Rigidbody2D>();
            if (rbPlayer != null)
            {
                Vector2 knockDir = (other.transform.position - transform.position).normalized;
                float knockForce = 8f;
                rbPlayer.AddForce(knockDir * knockForce, ForceMode2D.Impulse);
            }

            // Inicia recuo suave do boss
            StartCoroutine(SmoothReturnAfterHit());
        }
    }

    private IEnumerator SmoothReturnAfterHit()
    {
        Vector3 recuoTarget = dashStartPosition - (player.position - dashStartPosition).normalized * 1f;
        // recua um pouco atrás do dash inicial para evitar que fique colado no player
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

        // Espera alguns segundos antes de liberar o dash novamente
        yield return new WaitForSeconds(2f);
        dashAvailable = true;
    }


    // Funções existentes
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
