using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    [SerializeField] private TMP_InputField lobbyCodeInput; // Referência ao InputField para o código do lobby
    private Lobby hostLobby; // Referência ao lobby criado
    private Lobby joinedLobby; // Referência ao lobby conectado
    private float heartbeartTimer; // Timer para manter o lobby ativo

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject SelectorController;


    [Header("ArrowConfigurations")]
    public RectTransform selector;
    private Vector2 targetPosition;
    public float lerpSpeed = 10f;
    [SerializeField] private Animator selectorAnimator;

    [SerializeField] private Button singlePlayerBtn;
    [SerializeField] private Button multiplayerBtn;
  

    private bool unityServicesInitialized = false;

    [Header("TextMeshConfigurations")]
    public TextMeshProUGUI textMeshPro;
    public static string lastLobbyCode;
    public Animator errorCodeAnim;
    private void Awake()
    {
        // Garante que só exista uma instância do GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Impede que o GameManager seja destruído ao trocar de cena
        }
        else
        {
            Destroy(gameObject); // Destrói a instância duplicada
        }

        targetPosition = selector.anchoredPosition;

        singlePlayerBtn.onClick.AddListener(SpawnManager.Instance.StartSinglePlayer);
        multiplayerBtn.onClick.AddListener(GameManager.Instance.StartHostGame);
    }

    #region ArrowCode

    public void MoveTo(Transform target)
    {
        // Esta função simplesmente chama a outra versão, passando o offset padrão
        MoveTo(target);
    }

    // Versão 2: A "principal", que permite um offset personalizado (usada pelo seu script)
    public void MoveTo(Transform target, Vector2 offset)
    {
        // Esta é a lógica central, agora em um único lugar
        RectTransform targetRect = target.GetComponent<RectTransform>();
        targetPosition = targetRect.anchoredPosition + offset;

        // Ativa a animação como antes
        if (selectorAnimator != null)
        {
            selectorAnimator.SetBool("isHovering", true);
        }
    }

    public void PlaySelectorClickAnimation()
    {
        if (selectorAnimator != null)
        {
            selectorAnimator.SetTrigger("Click");
        }
    }

    public void AnimateButtonOnClick(GameObject buttonObject)
    {
        // 1. Tenta pegar o componente Animator no botão que foi clicado
        Animator buttonAnimator = buttonObject.GetComponent<Animator>();

        // 2. Se o Animator for encontrado, dispara o trigger
        if (buttonAnimator != null)
        {
            buttonAnimator.SetTrigger("PlayBtn");
        }
        else
        {
            // Opcional: Um aviso caso você esqueça de adicionar o Animator em um botão
            Debug.LogWarning("O botão '" + buttonObject.name + "' não tem um componente Animator para a animação de clique.");
        }
    }


    #endregion

    #region ConnectionUnityServices

    private async void Start()
    {
        // Aguarda até que o NetworkManager esteja disponível
        await WaitForNetworkManager();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnNetworkReady;
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                OnNetworkReady();
            }
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton não encontrado mesmo após espera!");
            return;
        }

        await UnityServices.InitializeAsync();
        unityServicesInitialized = true;

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        lobbyCodeInput.onSubmit.AddListener(delegate { StartClientGameCode(); });
    }

    private async Task WaitForNetworkManager()
    {
        int attempts = 0;
        const int maxAttempts = 100; // Limite de 10 segundos (100 * 0.1s)
        while (NetworkManager.Singleton == null && attempts < maxAttempts)
        {
            Debug.Log("Aguardando NetworkManager... Tentativa " + attempts);
            await Task.Delay(100); // Aguarda 0.1 segundo
            attempts++;
        }
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager não inicializado após " + maxAttempts + " tentativas!");
        }
    }

    private void OnNetworkReady()
    {
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            Debug.Log("Evento OnLoadEventCompleted registrado com sucesso.");
        }
        else
        {
            Debug.LogError("SceneManager do NetworkManager é nulo!");
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            NetworkManager.Singleton.OnServerStarted -= OnNetworkReady;
        }
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == "Gameplay" && clientsCompleted.Contains(NetworkManager.Singleton.LocalClientId))
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
                Debug.Log("Cena Gameplay carregada para o cliente local. LoadingPanel desativado.");
            }
            else
            {
                Debug.LogWarning("loadingPanel foi destruído ou é nulo ao tentar desativá-lo.");
            }
        }
    }
    private async void HandleLobbyHeartbeat()
    {

        if (hostLobby != null)
        {
            heartbeartTimer -= Time.deltaTime;
            if (heartbeartTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeartTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    #endregion

    #region Relay

    public async Task<string> CreateRelay()
    {
        try
        {

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joinCode);



            //Método antigo para criar Relay

            // RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);


            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }


    public async Task JoinRelay(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Join Code inválido.");
            return;
        }

        try
        {
            Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            //Método antigo para entrar no Relay                                                       // joinAllocation, "dtls"

            //  RelayServerData relayServerData = new RelayServerData();
            // NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

            Debug.Log("Relay Server Data configurado com sucesso.");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Erro ao entrar no Relay: {e.Message}\n{e}");
        }
    }

    #endregion

    #region ButtonsStartGameMultiplayer

    public async void StartHostGame()
    {

        if (!unityServicesInitialized)
        {
            Debug.LogWarning("Aguardando inicialização dos Unity Services...");
            while (!unityServicesInitialized)
            {
                await Task.Delay(100);
            }
        }



        loadingPanel.SetActive(true);
        SelectorController.SetActive(false);

        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 3;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = true
            };


            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            Debug.Log($"Lobby criado: Nome = {hostLobby.Name}, Código = {hostLobby.LobbyCode}");

            lastLobbyCode = hostLobby.LobbyCode;

            // Cria o Relay e atualiza os dados no lobby
            string joinCode = await CreateRelay();
            if (!string.IsNullOrEmpty(joinCode))
            {

                Debug.Log($"Relay Join Code gerado: {joinCode}");
                hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                {
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
                });
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Erro ao criar o lobby: {e}");
        }

        // Inicia o Host
        NetworkManager.Singleton.StartHost();
        if (NetworkManager.Singleton.IsHost)
        {
            LobbyCode.instance.LastLobbyCode();
            LoadScenes("Gameplay");

        }
    }

    private IEnumerator WaitErrorCode(float time)
    {
      
        if (errorCodeAnim != null) 
        {
            errorCodeAnim.SetBool("PlayErrorAnim", true);
            yield return new WaitForSeconds(time);
            errorCodeAnim.SetBool("PlayErrorAnim", false);
        }

    }

    public async void StartClientGame(string lobbyCode)
    {
       
        try
        {
            // Conecta ao lobby pelo código
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            Debug.Log($"Entrou no lobby: Nome = {joinedLobby.Name}, Código = {joinedLobby.LobbyCode}, Jogadores = {joinedLobby.Players.Count}/{joinedLobby.MaxPlayers}");

            loadingPanel.SetActive(true);

            // Obtém o joinCode do Relay armazenado no lobby
            if (joinedLobby.Data.TryGetValue("joinCode", out var joinCodeData))
            {
                string joinCode = joinCodeData.Value;
                Debug.Log($"Usando Join Code do Relay: {joinCode}");

                // Configura o Relay antes de iniciar o cliente
                await JoinRelay(joinCode);

                // Inicia o cliente após configurar o Relay
                NetworkManager.Singleton.StartClient();
                LoadScenes("Gameplay");
            }
            else
            {
             
                Debug.LogError("Erro: joinCode não encontrado no lobby.");
            }
        }
        catch (LobbyServiceException e)
        {
            loadingPanel.SetActive(false);
            StartCoroutine(WaitErrorCode(1.5f));
            Debug.LogError($"Erro ao entrar no lobby com código {lobbyCode}: {e}");
         
        }
        catch (RelayServiceException e)
        {
          
            Debug.LogError($"Erro ao configurar o Relay: {e}");
        }
    }

    public void StartClientGameCode()
    {
        // Obtém o código do InputField e tenta entrar no lobby
        string lobbyCode = lobbyCodeInput.text;
        if (!string.IsNullOrEmpty(lobbyCode))
        {
            StartClientGame(lobbyCode);
        }
        else
        {
            Debug.LogWarning("Por favor, insira um código de lobby válido!");
        }
    }

    #endregion

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }

        HandleLobbyHeartbeat();

        if (selector != null)
        {
            selector.anchoredPosition = Vector2.Lerp(selector.anchoredPosition, targetPosition, Time.deltaTime * lerpSpeed);
        }

    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void LoadScenes(string scene)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }

        else 
        {

            SceneManager.LoadScene(scene);

        }
    }
}