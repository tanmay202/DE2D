using UnityEngine;

public class PlayerTable : MonoBehaviour, IInteractable
{
    [Header("How many item slots this table has")]
    public int slotCount = 4;

    [Header("Link to TableUI canvas")]
    public TableUI tableUI;

    // Tracks what's placed on each slot (null = empty)
    public ItemData[] slots { get; private set; }

    void Awake() => slots = new ItemData[slotCount];

    public void Interact() => tableUI.Open(this);

    public string GetHint() => "[E] Set Up Table";

    /// <summary>Place an item into the first free slot.</summary>
    public bool PlaceItem(ItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                Debug.Log($"[Table] Placed {item.itemName} on slot {i}");
                return true;
            }
        }
        return false; // No empty slot
    }

    /// <summary>Remove an item from a specific slot back to inventory.</summary>
    public void RemoveItem(int slotIndex)
    {
        if (slots[slotIndex] == null) return;
        Inventory.Instance.AddItem(slots[slotIndex]);
        slots[slotIndex] = null;
    }
}