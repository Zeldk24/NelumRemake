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




    public override void OnNetworkSpawn()
    {
      
        base.OnNetworkSpawn();

        if (!IsOwner) return;
        
        playerID = OwnerClientId;
        
    }


    void Start()
    {
        inputs = GetComponent<InputManager>();

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



    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        heartUIManager?.UpdateHearts(currentHealth, maxHealth);

        playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        if (currentHealth <= 0 && IsOwner && !IsSingleplayer())
        {
            StartCoroutine(CooldownRespawn(1));
        }
        else if(currentHealth <= 0 && IsSingleplayer())
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
