using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Enemies / EnemiesSO")] 
public class EnemiesSO : ScriptableObject
{
    public AssetReferenceGameObject prefabEnemieRef;
    public int health;
    public int damage;

    public string takeDamageSoundAddress;
    [Range(0f, 1f)]
    public float takeDamageVolume;

    public string deathSoundAddress;
    [Range(0f, 1f)]
    public float deathSoundVolume;

    public float speed;
    public float chaseSpeed;
}
