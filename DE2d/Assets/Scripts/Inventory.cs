using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    // item → quantity owned
    public Dictionary<ItemData, int> _items = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(ItemData item, int qty = 1)
    {
        if (_items.ContainsKey(item)) _items[item] += qty;
        else _items[item] = qty;
        Debug.Log($"[Inventory] +{qty} {item.itemName} (total: {_items[item]})");
    }

    /// <summary>Returns false if not enough stock.</summary>
    public bool RemoveItem(ItemData item, int qty = 1)
    {
        if (!_items.ContainsKey(item) || _items[item] < qty) return false;
        _items[item] -= qty;
        if (_items[item] <= 0) _items.Remove(item);
        return true;
    }

    public int GetQuantity(ItemData item) =>
        _items.ContainsKey(item) ? _items[item] : 0;

    /// <summary>Returns a snapshot list for UI display.</summary>
    public List<(ItemData item, int qty)> GetAll()
    {
        var list = new List<(ItemData, int)>();
        foreach (var kv in _items) list.Add((kv.Key, kv.Value));
        return list;
    }
}