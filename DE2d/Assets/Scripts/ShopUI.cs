using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Items sold in this shop")]
    public ItemData[] itemsForSale;

    [Header("UI References")]
    public GameObject panel;
    public Transform itemListParent;     // ScrollView Content
    public GameObject shopItemPrefab;    // See layout below
    public TextMeshProUGUI feedbackText; // "Not enough money!" etc.

    void Start() => panel.SetActive(false);

    public void Open()
    {
        panel.SetActive(true);
        BuildList();
        Time.timeScale = 0f; // Pause game while shopping (optional)
    }

    public void Close()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void BuildList()
    {
        // Clear old entries
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (var item in itemsForSale)
        {
            var go = Instantiate(shopItemPrefab, itemListParent);
            var entry = go.GetComponent<ShopItemEntry>();
            entry.Setup(item, this);
        }
    }

    /// <summary>Called by ShopItemEntry buttons.</summary>
    public void TryBuy(ItemData item)
    {
        if (GameManager.Instance.TrySpend(item.buyPrice))
        {
            Inventory.Instance.AddItem(item);
            ShowFeedback($"Bought {item.itemName}!");
        }
        else
        {
            ShowFeedback("Not enough money!");
        }
    }

    void ShowFeedback(string msg)
    {
        if (feedbackText)
        {
            feedbackText.text = msg;
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 2f);
        }
    }

    void ClearFeedback() { if (feedbackText) feedbackText.text = ""; }
}