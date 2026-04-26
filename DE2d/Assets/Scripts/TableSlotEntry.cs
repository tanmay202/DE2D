using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TableSlotEntry : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public Button removeButton;
    public GameObject emptyLabel;   // A "Empty Slot" text shown when null

    public void Setup(ItemData item, Action onRemove)
    {
        bool hasItem = item != null;
        iconImage.gameObject.SetActive(hasItem);
        nameText.gameObject.SetActive(hasItem);
        removeButton.gameObject.SetActive(hasItem);
        if (emptyLabel) emptyLabel.SetActive(!hasItem);

        if (hasItem)
        {
            iconImage.sprite = item.icon;
            nameText.text = item.itemName;
            removeButton.onClick.AddListener(() => onRemove());
        }
    }
}