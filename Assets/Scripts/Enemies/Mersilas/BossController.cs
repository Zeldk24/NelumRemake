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
    public float dashCooldown = 3f;
    public float dashDistance = 10f;
    public GameObject dashIndicatorPrefab;

    private bool isInDashPhase = false;
    private bool dashAvailable = true;
    private bool isDashing = false;

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

        Vector3 startPosition = transform.position;
        Vector2 direction = (player.position - startPosition).normalized;

        float dashOffset = 0.3f; // Quanto queremos recuar do hit para evitar colisão

        // Raycast para detectar parede
        RaycastHit2D hit = Physics2D.Raycast(startPosition, direction, dashDistance, LayerMask.GetMask("Wall"));
        float actualDashDistance = dashDistance;

        if (hit.collider != null)
        {
            actualDashDistance = Mathf.Max(0f, hit.distance - dashOffset);
            // Garante que não fique negativo
        }

        Vector3 dashTarget = startPosition + (Vector3)direction * actualDashDistance;

        // Instancia o indicador
        if (dashIndicatorPrefab != null)
        {
            Vector3 indicatorPos = startPosition + (Vector3)direction * (actualDashDistance / 2);
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
        while (distanceToTarget > 0.01f)
        {
            Vector3 nextPosition = Vector3.MoveTowards(transform.position, dashTarget, dashSpeed * Time.deltaTime);

            // Checa colisão no próximo passo
            RaycastHit2D stepHit = Physics2D.Raycast(transform.position, direction, dashSpeed * Time.deltaTime, LayerMask.GetMask("Wall"));
            if (stepHit.collider != null)
                break; // bateu na parede

            transform.position = nextPosition;
            distanceToTarget = Vector3.Distance(transform.position, dashTarget);
            yield return null;
        }

        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
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
