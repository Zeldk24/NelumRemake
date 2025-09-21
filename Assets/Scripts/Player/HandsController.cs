using UnityEngine;
using Unity.Netcode;

public class HandsController : NetworkBehaviour
{
    private Camera mainCamera;
    public float maxDistance = 0.8f;
    public float offsetDistance = 0.3f;

    public PlayerController playerController;
 

 
    private bool IsSinglePlayer => NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;



 

    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController não encontrado pelo HandsController!", this);
                return;
            }
        }

     
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            StartCoroutine(WaitForCamera());
        }

        // Se for singleplayer, remove componentes de rede
        if (IsSinglePlayer)
        {
            var networkComponents = GetComponents<NetworkBehaviour>();
            foreach (var component in networkComponents)
            {
                if (component != this) // Não remove o próprio HandsController
                {
                    Destroy(component);
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsSinglePlayer) return;

        if (IsOwner)
        {
            StartCoroutine(WaitForCamera());
        }
        
    }

    private System.Collections.IEnumerator WaitForCamera()
    {
        int attempts = 0;
        while (mainCamera == null && attempts < 10)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindAnyObjectByType<Camera>();
            }

            if (mainCamera != null) yield break;

            attempts++;
            yield return new WaitForSeconds(0.1f);
        }

        if (mainCamera == null)
        {
            Debug.LogError("Não foi possível encontrar uma câmera após 10 tentativas", this);
        }
    }

    void Update()
    {
        if (playerController == null || mainCamera == null) return;

        Debug.DrawLine(playerController.transform.position, transform.position, Color.red);

        // Em singleplayer ou se for o dono no multiplayer
        if (IsSinglePlayer || IsOwner)
        {
            if (ShouldFollowMouse())
            {
                Vector3 playerPos = playerController.transform.position;
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0;

                Vector3 direction = (mouseWorldPos - playerPos).normalized;
                float distance = Vector3.Distance(playerPos, mouseWorldPos);

                if (distance > maxDistance)
                {
                    mouseWorldPos = playerPos + direction * maxDistance;
                }

                Vector3 newHandPos = playerPos + direction * offsetDistance;
                transform.position = newHandPos;

                LookAtMouse(mouseWorldPos - playerPos);

               
            }
            
        }
    }

    private bool ShouldFollowMouse()
    {
        if (playerController.currentHandItem == null) return true;

        var itemHandler = playerController.currentHandItem.GetComponent<ItemHandler>();
        if (itemHandler == null || itemHandler.item == null) return true;

        return itemHandler.item.itemTag != SlotTag.Weapon; // Mantém armas com posição baseada no sprite
    }

    private void LookAtMouse(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if (direction.x < 0)
            transform.localScale = new Vector3(1, -1, 1);
        else
            transform.localScale = Vector3.one;
    }



    public void SetCamera(Camera cam)
    {
        if (cam != null)
        {
            mainCamera = cam;
            Debug.Log($"Camera atribuída: {cam.name}", this);
        }
        else
        {
            Debug.LogWarning("Tentativa de atribuir câmera nula ao HandsController", this);
            StartCoroutine(WaitForCamera());
        }
    }
}

