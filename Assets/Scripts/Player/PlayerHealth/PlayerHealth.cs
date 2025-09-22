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

    public GameObject heartsUIPrefab; // UI de corações
    private HeartUIManager heartUIManager;
    private bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private int playerCount;

   // [SerializeField] private GameObject panel;
   // [SerializeField] private TextMeshProUGUI textCount;

    //private bool bloquearContador = false;
    //public bool IsDead = false;
    private InputManager inputs;


    [Header("Knockback")]
    
    public float knockbackDuration = 0.2f; // Duração em segundos
    public bool isKnockedBack; // Flag para controlar o estado
    private Rigidbody2D rb;

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
       // panel.SetActive(false);

        // Se for multiplayer, só o dono instancia o UI
        if (isMultiplayer)
        {
            
            Debug.Log("Quantidade de Jogadores: " + playerID);
            if (!IsOwner) return;
        }

        currentHealth = maxHealth;

        // Instancia o UI
        GameObject uiInstance = Instantiate(heartsUIPrefab);
        DontDestroyOnLoad(uiInstance); // mantém o UI entre cenas

        heartUIManager = uiInstance.GetComponent<HeartUIManager>();
        heartUIManager.UpdateHearts(currentHealth, maxHealth);
    }



    public void TakeDamage(int damage, Vector3 damageSourcePosition)
    {
        if (isKnockedBack) return; // <--- ADICIONE ESTA LINHA PARA EVITAR MÚLTIPLOS KNOCKBACKS

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        heartUIManager?.UpdateHearts(currentHealth, maxHealth);

        StartCoroutine(KnockbackCoroutine(damageSourcePosition));

        playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        if (currentHealth <= 0 && IsOwner && !IsSingleplayer())
        {
            StartCoroutine(CooldownRespawn(1));
        }
        else if (currentHealth <= 0 && IsSingleplayer())
        {
            StartCoroutine(CooldownRespawn(1));
        }
    }


    private bool IsSingleplayer()
    {
        return !NetworkManager.Singleton.IsListening;
    }

    private IEnumerator CooldownRespawn(int time)
    {
        inputs.DisableMovement();

        yield return new WaitForSeconds(time);

        spawnManager.RespawnPlayer(gameObject);

        currentHealth = maxHealth;

        heartUIManager.UpdateHearts(currentHealth, maxHealth);

        inputs.EnableMovement();
    }

    private IEnumerator KnockbackCoroutine(Vector3 damageSourcePosition)
    {
        inputs.DisableMovement();
        isKnockedBack = true;

        // 2. Calcula a direção da força
        Vector2 direction = (transform.position - damageSourcePosition).normalized;

        // 3. Zera a velocidade atual para que a nova força seja aplicada de forma limpa
        rb.linearVelocity = Vector2.zero;

        // 4. Aplica a força de repulsão
        rb.AddForce(direction * 20f, ForceMode2D.Impulse);

        // 5. Espera a duração do knockback
        yield return new WaitForSeconds(knockbackDuration);

        // 6. Para o movimento do Rigidbody
        rb.linearVelocity = Vector2.zero;

        inputs.EnableMovement();
        isKnockedBack = false;
    }


    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        heartUIManager?.UpdateHearts(currentHealth, maxHealth);
    }

    public void SetMaxHealth(int newMax)
    {
        maxHealth = newMax;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        heartUIManager?.UpdateHearts(currentHealth, maxHealth);
    }

  

   // public IEnumerator ContadorDeMorte()
   // {

   //     bloquearContador = true;
   //     IsDead = true;

   //     panel.SetActive(true);

   //     int contador = 11;

   //     inputs.DisableMovement();


   //     while (contador > 0)
   //     {
   //         contador--;
   //         textCount.text = contador.ToString();
   //         Debug.Log(contador);
   //         yield return new WaitForSeconds(1);

           

   //     }

   //     inputs.EnableMovement();

   //     panel.SetActive(false);

   //     IsDead = false;
   //     bloquearContador = false;

   // }


    private void Update()
    {
        

      
    }
}
