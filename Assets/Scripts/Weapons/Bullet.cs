using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Bullet : NetworkBehaviour
{
    public Item itemData;
    private int damage;
    private Rigidbody2D rb2d;
    private Collider2D collider2d;

    private Transform followTarget;
    private HashSet<Collider2D> damagedEnemies = new HashSet<Collider2D>();
    [SerializeField] private float bulletRotationSpeed = 30;


    private void Start()
    {
        damage = itemData.damage;

        rb2d = GetComponent<Rigidbody2D>();
        collider2d = GetComponent<Collider2D>();

        if (IsSingleplayer())
        {
            // Destrói o NetworkObject para evitar chamadas de rede
            Destroy(GetComponent<NetworkObject>());
            Destroy(gameObject, 3f);
        }
        else
        {
         
            Debug.Log("No servidor a bala");
            StartCoroutine(BulletDestroyServer(3f));
        }

    }

    private bool IsSingleplayer()
    {
        return !NetworkManager.Singleton.IsListening;
    }



    void Update()
    {
        transform.Rotate(0, 0, bulletRotationSpeed * Time.deltaTime);

        if (followTarget != null)
        {
            transform.position = followTarget.position;
            transform.rotation = followTarget.rotation;
        }

    }

    private void TryDamageEnemy(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemies") && !damagedEnemies.Contains(other))
        {
            EnemyController inimigo = other.GetComponent<EnemyController>();
            if (inimigo != null)
            {

                Vector3 damageSource = transform.position;

                inimigo.TakeDamageInternal(damage, damageSource);

                damagedEnemies.Add(other); //[] Adiciona o inimigo ao hashset

            }
        }

        if (!IsSingleplayer())
        {
            var otherNetObj = other.GetComponent<NetworkObject>();

            if (otherNetObj != null && otherNetObj.IsSpawned && IsServer)
            {

               
                transform.parent = other.transform;
            }
            else
            {
                // Simula grudando ao seguir a posição do outro objeto
                followTarget = other.transform;
            }
        }
        else
        {
            // No singleplayer pode parentear normalmente
            transform.SetParent(other.transform, true);
        }




        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            collider2d.enabled = false;
        }

        this.enabled = false;



    }

    private IEnumerator BulletDestroyServer(float timer)
    {

        yield return new WaitForSeconds(timer);

        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    

    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamageEnemy(other);
    }
}
