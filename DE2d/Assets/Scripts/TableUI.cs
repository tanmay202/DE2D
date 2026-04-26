using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TableUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Transform inventoryListParent;   // Left side: inventory items
    public Transform tableSlotsParent;      // Right side: table slots
    public GameObject inventoryEntryPrefab; // Button per inventory item
    public GameObject tableSlotPrefab;      // Slot display per table slot
    public TextMeshProUGUI feedbackText;

    private PlayerTable _table;

    void Start() => panel.SetActive(false);

    public void Open(PlayerTable table)
    {
        _table = table;
        panel.SetActive(true);
        RefreshAll();
        Time.timeScale = 0f;
    }

    public void Close()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void RefreshAll()
    {
        BuildInventoryList();
        BuildTableSlots();
    }

    // ── Left Side: Inventory items the player can place ──────────────────

    void BuildInventoryList()
    {
        foreach (Transform c in inventoryListParent) Destroy(c.gameObject);

        var items = Inventory.Instance.GetAll();
        if (items.Count == 0)
        {
            // Show "empty" label
            var go = new GameObject("EmptyLabel", typeof(TextMeshProUGUI));
            go.transform.SetParent(inventoryListParent, false);
            go.GetComponent<TextMeshProUGUI>().text = "Inventory empty";
            return;
        }

        foreach (var (item, qty) in items)
        {
            var go = Instantiate(inventoryEntryPrefab, inventoryListParent);
            var entry = go.GetComponent<TableInventoryEntry>();
            entry.Setup(item, qty, this);
        }
    }

    // ── Right Side: Current table slots ──────────────────────────────────

    void BuildTableSlots()
    {
        foreach (Transform c in tableSlotsParent) Destroy(c.gameObject);

        for (int i = 0; i < _table.slots.Length; i++)
        {
            var go = Instantiate(tableSlotPrefab, tableSlotsParent);
            var slot = go.GetComponent<TableSlotEntry>();
            int capturedIndex = i; // capture for lambda
            slot.Setup(_table.slots[i], () => OnRemoveSlot(capturedIndex));
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────

    /// Called by inventory entry button.
    public void OnPlaceItem(ItemData item)
    {
        if (!Inventory.Instance.RemoveItem(item))
        {
            ShowFeedback("No stock!");
            return;
        }

        if (!_table.PlaceItem(item))
        {
            // Table full — give it back
            Inventory.Instance.AddItem(item);
            ShowFeedback("Table is full!");
            return;
        }

        RefreshAll();
        ShowFeedback($"{item.itemName} placed on table!");
    }

    void OnRemoveSlot(int index)
    {
        var item = _table.slots[index];
        if (item == null) return;
        _table.RemoveItem(index);
        RefreshAll();
        ShowFeedback($"{item.itemName} returned to inventory.");
    }

    void ShowFeedback(string msg)
    {
        if (!feedbackText) return;
        feedbackText.text = msg;
        CancelInvoke(nameof(ClearFeedback));
        Invoke(nameof(ClearFeedback), 2f);
    }

    void ClearFeedback() { if (feedbackText) feedbackText.text = ""; }
}