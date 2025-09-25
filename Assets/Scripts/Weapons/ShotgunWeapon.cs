using UnityEngine;
using Unity.Netcode;

public class PlayerShooting : NetworkBehaviour
{
    private Camera cam;
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletSpeed = 10f;

    [Header("Shotgun settings")]
    public int pelletCount = 8;               // quantos proj�teis por disparo
    [Tooltip("�ngulo total de dispers�o em graus (ex: 30)")]
    public float spreadAngle = 30f;           // �ngulo total de spread em graus
    public float speedVariation = 0.1f;       // +/- varia��o percentual na velocidade

    private void Start()
    {

        this.cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
            mouseWorld.z = 0f;

            Vector2 direcao = (mouseWorld - transform.position).normalized;

            if (IsSingleplayer())
            {
                Shoot(direcao);
            }
            else if (IsOwner)
            {
                // opcional: spawn local para responsividade (comentado)
                // Shoot(direcao);

                ShootServerRpc(direcao);
            }
        }
    }

    private bool IsSingleplayer()
    {
        return !NetworkManager.Singleton.IsListening;
    }

    // SERVER: instancia os pellets no servidor (autoridade)
    [ServerRpc]
    private void ShootServerRpc(Vector2 direction)
    {
        SpawnShot(direction);
    }

    // Fun��o que cria os pellets (pode ser usada tanto no servidor quanto local)
    private void SpawnShot(Vector2 direction)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("bulletPrefab est� NULL!");
            return;
        }

        if (bulletSpawn == null)
        {
            Debug.LogError("bulletSpawn est� NULL!");
            return;
        }

        // �ngulo inicial para centrar os pellets em torno da dire��o base
        float halfSpread = spreadAngle * 0.5f;

        for (int i = 0; i < pelletCount; i++)
        {
            // calculamos um �ngulo para este pellet
            float t = (pelletCount == 1) ? 0.5f : (float)i / (pelletCount - 1); // 0..1
            float angle = Mathf.Lerp(-halfSpread, halfSpread, t);

            // rotaciona a dire��o pelo �ngulo em torno do eixo Z (2D)
            Vector2 pelletDir = RotateVector(direction, angle).normalized;

            // varia��o de velocidade pequena e aleat�ria
            float speed = bulletSpeed * (1f + Random.Range(-speedVariation, speedVariation));

            // instancia
            var bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);

            // garante que tem Rigidbody2D
            var rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = pelletDir * speed;
            }
            else
            {
                Debug.LogWarning("bulletPrefab n�o tem Rigidbody2D!");
            }

            // se estivermos no servidor (temos autoridade), spawn network
            var net = bullet.GetComponent<NetworkObject>();
            if (NetworkManager.Singleton.IsServer)
            {
                if (net != null)
                {
                    net.Spawn();
                }
                else
                {
                    Debug.LogWarning("bulletPrefab n�o tem NetworkObject � n�o ser� spawnado na rede.");
                }
            }
            else
            {
                // se for singleplayer, n�o precisa do NetworkObject
                // caso este cliente queira prever (client-side prediction), poderia instanciar efeitos visuais aqui
            }
        }
    }

    // Fun��o utilit�ria para rotacionar um Vector2 por 'angleDeg' graus em torno do eixo Z
    private Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Vers�o local (singleplayer) � apenas chama SpawnShot diretamente
    private void Shoot(Vector2 direction)
    {
        SpawnShot(direction);
    }
}
