// Create via: Right Click > Create > ShopSim > ItemData
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "ShopSim/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName = "Item";
    public Sprite icon;
    public int buyPrice = 10;   // Cost to buy from shop
    public int sellPrice = 15;  // Price you sell it for at your table
    [TextArea] public string description;
}