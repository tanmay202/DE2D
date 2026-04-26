using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TableInventoryEntry : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI qtyText;
    public Button placeButton;

    public void Setup(ItemData item, int qty, TableUI tableUI)
    {
        iconImage.sprite = item.icon;
        nameText.text = item.itemName;
        qtyText.text = $"x{qty}";
        placeButton.onClick.AddListener(() => tableUI.OnPlaceItem(item));
    }
}