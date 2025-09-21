using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class FireWeapon : NetworkBehaviour
{
    public Transform bulletSpawn;
    public GameObject bulletPrefab;
    public float bulletSpeed;
    private Camera cam;


    private Vector3 direcao;

    private void Start()
    {
        this.cam = Camera.main;

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Vector2 posicaoMouse = Input.mousePosition;
            Vector3 posicaoMouseNoMundo = this.cam.ScreenToWorldPoint(posicaoMouse);
            posicaoMouseNoMundo.z = 0;

            direcao = (posicaoMouseNoMundo - this.transform.position);


            if (IsSingleplayer())
            {
                Shoot(direcao); // Instancia localmente sem rede
            }
            else if (IsOwner)
            {
                ShootServerRpc(direcao); // Pede ao servidor para instanciar
            }



        }
    }

    private bool IsSingleplayer()
    {
        return !NetworkManager.Singleton.IsListening;
    }

    [ServerRpc]
    private void ShootServerRpc(Vector2 direction)
    {

        if (bulletPrefab == null)
        {
            Debug.LogError("bulletPrefab está NULL no servidor!");
        }

        if (bulletSpawn == null)
        {
            Debug.LogError("bulletSpawn está NULL no servidor!");
        }


        var bullet = Instantiate(bulletPrefab, bulletSpawn.transform.position, bulletSpawn.transform.rotation);
        var networkBullet = bullet.GetComponent<NetworkObject>();
        direction = direction.normalized;
        bullet.GetComponent<Rigidbody2D>().linearVelocity = direction * bulletSpeed;
        networkBullet.Spawn();



    }

    private void Shoot(Vector2 direction)
    {

        var bullet = Instantiate(bulletPrefab, bulletSpawn.transform.position, bulletSpawn.transform.rotation);
        direction = direction.normalized;
        bullet.GetComponent<Rigidbody2D>().linearVelocity = direction * bulletSpeed;
    
    }

}
