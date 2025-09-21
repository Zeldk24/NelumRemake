using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;


public class EnemyController : NetworkBehaviour
{
    public EnemiesSO enemiesData;

    [Header("Componentes")]
    private NavMeshAgent agent;
    private Rigidbody2D rb; // <- NOVO: Refer�ncia para o Rigidbody2D
    private SpriteRenderer spriteRender;
    private NetworkObject enemiesNO;
    private Material materialInstance;
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    [Header("Knockback")]
    public float knockbackStrength = 5f; // For�a da repuls�o
    public float knockbackDuration = 0.2f; // Dura��o em segundos
    public bool isKnockedBack; // Flag para controlar o estado

    public int currentHealh;
    private int damage;

    private void Start()
    {
        // Pega os componentes
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>(); // <- NOVO: Pegar o Rigidbody2D
        spriteRender = GetComponent<SpriteRenderer>();
        enemiesNO = GetComponent<NetworkObject>();

        // Configura��o de Material/Shader
        materialInstance = new Material(spriteRender.material);
        spriteRender.material = materialInstance;

        // Configura��es do inimigo
        currentHealh = enemiesData.health;
        damage = enemiesData.damage;

        // Configura��es do NavMeshAgent para 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    // NOVO: Adicione um Update para pausar a IA durante o knockback
    private void Update()
    {
        // Se estiver sofrendo knockback, a IA (movimento do NavMeshAgent) n�o deve ser executada
        // Isso impede o NavMeshAgent de lutar contra a for�a do knockback
        if (isKnockedBack)
        {
            // O NavMeshAgent j� estar� desabilitado, mas � uma boa pr�tica
            return;
        }

        
    }


    /*[] Coroutine de feedback de dano ... (c�digo existente)*/
    private IEnumerator FeedbackDamage()
    {
        materialInstance.SetFloat(FlashAmountID, 1f);
        yield return new WaitForSeconds(0.1f);
        materialInstance.SetFloat(FlashAmountID, 0f);
        yield return new WaitForSeconds(0.1f);
        materialInstance.SetFloat(FlashAmountID, 0f);
    }

    // --- L�GICA DE DANO E KNOCKBACK ---

    // MODIFICADO: Agora precisamos da posi��o de quem causou o dano
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageEnemyServerRpc(int damage, Vector3 damageSourcePosition)
    {
        // Se j� estiver em knockback ou morto, n�o faz nada
        if (isKnockedBack || currentHealh <= 0) return;

        TakeDamageInternal(damage, damageSourcePosition);
    }

    // MODIFICADO: Par�metro damageSourcePosition adicionado
    public void TakeDamageInternal(int damage, Vector3 damageSourcePosition)
    {
        currentHealh -= damage;

        // Chama a coroutine de Knockback
        StartCoroutine(KnockbackCoroutine(damageSourcePosition));

        if (!IsSingleplayer())
        {
            FeedbackDamageClientRpc();
        }
        else
        {
            StartCoroutine(FeedbackDamage());
        }

        if (currentHealh <= 0)
        {
            if (!string.IsNullOrEmpty(enemiesData.deathSoundAddress))
            {
                SoundManager.Instance.PlaySound(enemiesData.deathSoundAddress, enemiesData.deathSoundVolume);
            }
            DeathEnemy();
        }
    }

    // NOVO: A coroutine que aplica a for�a do knockback
    private IEnumerator KnockbackCoroutine(Vector3 damageSourcePosition)
    {
        isKnockedBack = true;

        // 1. Desativa o NavMeshAgent para que a f�sica possa assumir
        if (agent.isOnNavMesh) // Checagem de seguran�a
        {
            agent.enabled = false;
        }

        // 2. Calcula a dire��o da for�a
        Vector2 direction = (transform.position - damageSourcePosition).normalized;

        // 3. Zera a velocidade atual para que a nova for�a seja aplicada de forma limpa
        rb.linearVelocity = Vector2.zero;

        // 4. Aplica a for�a de repuls�o
        rb.AddForce(direction * knockbackStrength, ForceMode2D.Impulse);

        // 5. Espera a dura��o do knockback
        yield return new WaitForSeconds(knockbackDuration);

        // 6. Para o movimento do Rigidbody
        rb.linearVelocity = Vector2.zero;

        // 7. Reativa o NavMeshAgent
        agent.enabled = true;

        isKnockedBack = false;
    }


    [ClientRpc]
    private void FeedbackDamageClientRpc()
    {
        StartCoroutine(FeedbackDamage());
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    private void DeathEnemy()
    {
        if (IsServer)
        {
            enemiesNO.Despawn();
        }
        if (IsSingleplayer())
        {
            Destroy(this.gameObject);
        }
    }

    private bool IsSingleplayer()
    {
        return !NetworkManager.Singleton.IsListening;
    }
}