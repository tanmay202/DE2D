using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Attach to the shopItemPrefab.
/// Prefab needs: Image (icon), TMP (name), TMP (price), Button (buy)
public class ShopItemEntry : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private ItemData _item;
    private ShopUI _shopUI;

    public void Setup(ItemData item, ShopUI shopUI)
    {
        _item = item;
        _shopUI = shopUI;
        iconImage.sprite = item.icon;
        nameText.text = item.itemName;
        priceText.text = $"$ {item.buyPrice}";
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    void OnBuyClicked() => _shopUI.TryBuy(_item);
}