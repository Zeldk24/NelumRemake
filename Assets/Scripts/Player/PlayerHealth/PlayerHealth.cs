using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlayerHealth : NetworkBehaviour
{
    private SpawnManager spawnManager;
    public ulong playerID;

    public int maxHealth = 6;
    public int currentHealth;

    public GameObject heartsUIPrefab;
    private HeartUIManager heartUIManager;
    private bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private int playerCount;
    private InputManager inputs;
    private bool isDead = false; // <<< PASSO 1: Adicionar a flag de estado de morte

    [Header("Knockback")]
    public float knockbackDuration = 0.2f;
    public bool isKnockedBack;
    private Rigidbody2D rb;



    private SpriteRenderer spriteRender;
    private Material materialInstance;
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");


    private bool isInvincible = false;
    [Tooltip("Duração da invencibilidade em segundos após levar dano.")]
    public float invincibilityDuration = 1f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        playerID = OwnerClientId;
    }

    void Start()
    {
        inputs = GetComponent<InputManager>();
        rb = GetComponent<Rigidbody2D>();
        spawnManager = SpawnManager.Instance;
        spriteRender = GetComponent<SpriteRenderer>();

        materialInstance = new Material(spriteRender.material);
        spriteRender.material = materialInstance;


        if (isMultiplayer)
        {
            if (!IsOwner) return;
        }

        currentHealth = maxHealth;

        GameObject uiInstance = Instantiate(heartsUIPrefab);
        DontDestroyOnLoad(uiInstance);

        heartUIManager = uiInstance.GetComponent<HeartUIManager>();
        heartUIManager.UpdateHearts(currentHealth, maxHealth);
    }


    public void TakeDamage(int damage, Vector3 damageSourcePosition)
    {
        if (isDead || isInvincible) return;

        isInvincible = true;

        // Inicia as corrotinas de física (knockback) e estado (invencibilidade)
        StartCoroutine(KnockbackCoroutine(damageSourcePosition));
        StartCoroutine(InvincibilityCoroutine()); // <<< Inicia a nova corrotina aqui

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        heartUIManager?.UpdateHearts(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            if (IsOwner || IsSingleplayer())
            {
                StartCoroutine(CooldownRespawn(1));
            }
        }
    }
    private IEnumerator InvincibilityCoroutine()
    {
        // 1. Flash branco inicial para dar impacto (sua lógica de FeedbackDamage)
        materialInstance.SetFloat(FlashAmountID, 1f);
        yield return new WaitForSeconds(0.1f);

        // 2. Período de invencibilidade com o jogador piscando sutilmente
        float remainingTime = invincibilityDuration - 0.1f;
        float timer = 0f;
        bool isFlashed = false;

        while (timer < remainingTime)
        {
            // Alterna entre normal (0) e um flash leve (0.5)
            materialInstance.SetFloat(FlashAmountID, isFlashed ? 0f : 0.5f);
            isFlashed = !isFlashed;

            yield return new WaitForSeconds(0.15f); // Velocidade da piscada
            timer += 0.15f;
        }

        // 3. Garante que o jogador volte à aparência normal no final
        materialInstance.SetFloat(FlashAmountID, 0f);

        // 4. Desativa a invencibilidade
        isInvincible = false;
    }


    private bool IsSingleplayer()
    {
        return !NetworkManager.Singleton.IsListening;
    }

    private IEnumerator CooldownRespawn(int time)
    {
        inputs.DisableMovement();
        rb.linearVelocity = Vector2.zero; // Zera a velocidade para evitar que deslize enquanto morto

        yield return new WaitForSeconds(time);

        spawnManager.RespawnPlayer(gameObject);
        currentHealth = maxHealth;
        heartUIManager.UpdateHearts(currentHealth, maxHealth);

        isDead = false; // <<< PASSO 4: Revive o jogador
        inputs.EnableMovement();
    }

    private IEnumerator KnockbackCoroutine(Vector3 damageSourcePosition)
    {
        inputs.DisableMovement();
        isKnockedBack = true;

        Vector2 direction = (transform.position - damageSourcePosition).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * 10f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector2.zero;

        if (!isDead)
        {
            inputs.EnableMovement();
        }

        isKnockedBack = false;
    }

    public void Heal(int amount)
    {
        if (isDead) return; // Um jogador morto não pode se curar

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        heartUIManager?.UpdateHearts(currentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMax)
    {
        maxHealth = newMax;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        heartUIManager?.UpdateHearts(currentHealth, maxHealth);
    }

    private void Update()
    {
        // Pode ser mantido vazio
    }
}