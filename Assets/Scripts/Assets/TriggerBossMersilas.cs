using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class TriggerBossMersilas : MonoBehaviour
{
    private BossController boss;
    

    [Header("Cutscene")]
    public float cutsceneDuration = 7f;

    public void RegisterBoss(BossController bossController)
    {
        boss = bossController;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || boss == null)
            return;

        // Pega a câmera dentro do player
        Camera playerCam = other.GetComponentInChildren<Camera>();
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
        float moveDuration = 1.5f; // tempo que a camera leva até o boss

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        boss.PlayRoar();

        camController = player.GetComponentInChildren<CameraController>();
        if (camController != null)
        {
            // tremor de 0.6 segundos enquanto o boss ruge
            StartCoroutine(camController.CameraShake(3f, 0.2f));
        }

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
