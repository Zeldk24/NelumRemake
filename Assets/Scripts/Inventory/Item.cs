using UnityEngine;
using UnityEngine.AddressableAssets;
public enum SlotTag { None, Head, Chest, Legs, Feet, Weapon, FireWeapon }

[CreateAssetMenu(menuName = "InventoryScriptableObjects /Item")]
public class Item : ScriptableObject
{
    public string itemID;
    public AssetReferenceGameObject prefabItemRef;
    public Sprite sprite;
    public SlotTag itemTag;

    [Header("ItemMeeleInfo")]
    public int damage;
    public bool stackable = true;
    public string itemName;
    public float attackCooldown;

    [Header("Sounds")]
    public string attackVFXAddress;
    public string attackSoundAddress;
    [Range(0f,1f)]
    public float attackVolume;

}

