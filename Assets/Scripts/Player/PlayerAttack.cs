using Unity.Netcode;
using UnityEngine;

public class PlayerAttack : NetworkBehaviour
{

    private Item itemData;
 
    

    [SerializeField]
    private Transform[] attackPoint;

    [SerializeField]
    private float rangeAttack;

    [SerializeField] private LayerMask enemyLayer;

   
    public void SETSOUND(Item newItem)
    {
        itemData = newItem;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
          
            Atacar();



            if (itemData != null && !string.IsNullOrEmpty(itemData.attackSoundAddress))
            {
                SoundManager.Instance.PlaySound(itemData.attackSoundAddress, itemData.attackVolume);
            }
        }

    }

    private void OnDrawGizmos()
    {
        if (attackPoint != null)
        {
            foreach (Transform point in attackPoint)
            {

                Gizmos.DrawWireSphere(point.position, rangeAttack);
            }
               
        }

    }




    private void Atacar()
    {
        

        foreach (Transform point in attackPoint)
        {
            // Use OverlapCircleAll para encontrar TODOS os inimigos no raio
            Collider2D[] colliders = Physics2D.OverlapCircleAll(point.position, rangeAttack, enemyLayer);

            // Se encontrou algum inimigo...
            if (colliders.Length > 0)
            {
                Debug.Log($"Ataque encontrou {colliders.Length} objetos na layer de inimigos.");
            }

            // Itera sobre cada inimigo encontrado
            foreach (Collider2D colliderEnemy in colliders)
            {
                Debug.Log($"Tentando aplicar dano em: {colliderEnemy.name}");

                EnemyController enemyController = colliderEnemy.GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    Debug.Log($"Inimigo VÁLIDO encontrado: {colliderEnemy.name}. Aplicando dano...");


                    int danoParaAplicar = itemData.damage;

                    Vector3 damageSource = transform.position;

                    if (IsSinglePlayer())
                    {
                        enemyController.TakeDamageInternal(danoParaAplicar, damageSource);
                    }
                    else
                    {
                        enemyController.TakeDamageEnemyServerRpc(danoParaAplicar, damageSource);
                    }
                }
            }
        }
    }


    private bool IsSinglePlayer()
    {
        return NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
    }

}
