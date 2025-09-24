using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class TriggerBossMersilas : MonoBehaviour
{
    private BossController boss;

    [Header("Requerimento")]
    [Tooltip("Arraste o ScriptableObject do item necessário que o jogador deve ter no inventário.")]
    public Item requiredItem;

    [Header("Cutscene")]
    public float cutsceneDuration = 7f;

    private bool isActivated = false;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    public void RegisterBoss(BossController bossController)
    {
        boss = bossController;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // Só funciona para o player
        if (isActivated || !other.gameObject.CompareTag("Player") || boss == null)
            return;

        if (other.gameObject.TryGetComponent<NetworkObject>(out var networkObject) && !networkObject.IsOwner)
            return;

        Inventory playerInventory = Inventory.Singleton;

        if (playerInventory == null)
        {
            Debug.LogError("Não foi possível encontrar o Singleton do Inventário!");
            return;
        }

        // 🚫 Se não tiver o item → NÃO deixa passar (colisor continua sólido)
        if (requiredItem != null && !playerInventory.HasItem(requiredItem))
        {
            Debug.Log($"O jogador não possui o item '{requiredItem.itemName}' no inventário.");
            return;
        }

        // ✅ Se tiver o item → libera passagem
        isActivated = true;
        Debug.Log("Item encontrado! Iniciando a cutscene do boss.");

        if (col != null)
            col.enabled = false; // desativa a barreira

        Camera playerCam = other.gameObject.GetComponentInChildren<Camera>();
        if (playerCam != null)
        {
            StartCoroutine(Cutscene(other.transform, playerCam));
        }
    }

    private IEnumerator Cutscene(Transform player, Camera cam)
    {
        CameraController camController = player.GetComponentInChildren<CameraController>();
        if (camController != null)
            camController.canFollow = false;

        InputManager inputManager = player.GetComponent<InputManager>();
        if (inputManager != null)
            inputManager.DisableMovement();

        Vector3 startPos = cam.transform.position;
        Vector3 targetPos = new Vector3(boss.transform.position.x,
                                        boss.transform.position.y,
                                        cam.transform.position.z);

        float t = 0f;
        float moveDuration = 1.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        boss.PlayRoar();

        if (camController != null)
            StartCoroutine(camController.CameraShake(3f, 0.2f));

        yield return new WaitForSeconds(3f);

        startPos = cam.transform.position;
        targetPos = player.position + camController.offset;
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (camController != null)
            camController.canFollow = true;
        if (inputManager != null)
            inputManager.EnableMovement();

        Destroy(gameObject);
    }
}
