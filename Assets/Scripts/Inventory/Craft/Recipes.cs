using UnityEngine;

[CreateAssetMenu(menuName = "InventoryScriptableObjects /Recipe")]
public class Recipe : ScriptableObject
{
    public Ingredient[] ingredients;
    public Item result;

}

[System.Serializable]
public class Ingredient
{
    public Item item;
    public int quantity;
}