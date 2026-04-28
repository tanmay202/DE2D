// ============================================================================
// InventoryPanelView.cs — Inventory Display Panel
// Responsibility: Show list of devices in inventory. Clicking selects a device
//                 for the ShopController. Shows condition, cost, and set price.
// Dependencies: GameManager, ShopController, InventoryManager
// Scene Placement: UI/InventoryPanel (Canvas panel, toggleable)
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeviceEmpire.Core;
using DeviceEmpire.Inventory;

namespace DeviceEmpire.UI
{
    /// <summary>
    /// Displays all devices currently in the player's inventory.
    /// Each device is a clickable button that selects it for sale.
    /// 
    /// PASSIVE VIEW: Reads from InventoryManager, calls ShopController.SelectDevice().
    /// No game logic lives here.
    /// </summary>
    public class InventoryPanelView : MonoBehaviour
    {
        // ── UI References ──────────────────────────────────────────────────
        [Header("UI References")]
        [Tooltip("Parent transform for inventory item buttons")]
        [SerializeField] private Transform itemListContainer;

        [Tooltip("Prefab for a single inventory item row")]
        [SerializeField] private GameObject inventoryItemPrefab;

        [Tooltip("Text shown when inventory is empty")]
        [SerializeField] private TextMeshProUGUI emptyText;

        [Tooltip("Header showing inventory count")]
        [SerializeField] private TextMeshProUGUI headerText;

        // ── State ──────────────────────────────────────────────────────────
        private List<GameObject> _itemButtons = new();
        private DeviceInstance _highlightedDevice;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.Inventory != null)
                {
                    gm.Inventory.OnDeviceAdded += (_) => RefreshList();
                    gm.Inventory.OnDeviceRemoved += (_) => RefreshList();
                }

                if (gm.Shop != null)
                {
                    gm.Shop.OnFlowReset += () => ClearHighlight();
                }
            }

            RefreshList();
        }

        private void OnEnable()
        {
            RefreshList();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Rebuild the entire inventory list. Called when inventory changes.
        /// </summary>
        public void RefreshList()
        {
            // Clear existing buttons
            foreach (var btn in _itemButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _itemButtons.Clear();

            var inventory = GameManager.Instance?.Inventory;
            if (inventory == null) return;

            // Update header
            if (headerText != null)
            {
                headerText.text = $"Inventory ({inventory.CurrentCount}/{inventory.MaxCapacity})";
            }

            // Show/hide empty text
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(inventory.CurrentCount == 0);
                if (inventory.CurrentCount == 0)
                {
                    emptyText.text = "No devices in stock.\nVisit the Supplier to buy inventory!";
                }
            }

            if (itemListContainer == null || inventoryItemPrefab == null) return;

            // Create a button for each device
            int currentDay = GameManager.Instance.Clock?.CurrentDay ?? 1;

            foreach (var device in inventory.Stock)
            {
                CreateInventoryItem(device, currentDay);
            }
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void CreateInventoryItem(DeviceInstance device, int currentDay)
        {
            var item = Instantiate(inventoryItemPrefab, itemListContainer);
            item.name = $"InvItem_{device.Data.DeviceName}_{device.InstanceId}";
            _itemButtons.Add(item);

            // Find UI elements in the prefab
            var nameText = item.transform.Find("DeviceName")?.GetComponent<TextMeshProUGUI>();
            var condText = item.transform.Find("Condition")?.GetComponent<TextMeshProUGUI>();
            var costText = item.transform.Find("Cost")?.GetComponent<TextMeshProUGUI>();
            var valueText = item.transform.Find("Value")?.GetComponent<TextMeshProUGUI>();
            var categoryText = item.transform.Find("Category")?.GetComponent<TextMeshProUGUI>();
            var selectButton = item.GetComponent<Button>();
            if (selectButton == null)
                selectButton = item.transform.Find("SelectButton")?.GetComponent<Button>();

            // Populate text fields
            if (nameText != null)
                nameText.text = device.Data.DeviceName;

            if (condText != null)
                condText.text = device.GetConditionLabelPlain();

            if (costText != null)
                costText.text = $"Cost: ${device.PurchasePrice:F2}";

            if (valueText != null)
            {
                float marketValue = device.GetCurrentValue(currentDay);
                valueText.text = $"Value: ${marketValue:F2}";
            }

            if (categoryText != null)
                categoryText.text = device.Data.Category.ToString();

            // Icon
            var iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && device.Data.Icon != null)
                iconImage.sprite = device.Data.Icon;

            // Click handler — select this device for sale
            if (selectButton != null)
            {
                var capturedDevice = device;
                selectButton.onClick.AddListener(() => OnDeviceClicked(capturedDevice, item));
            }
        }

        private void OnDeviceClicked(DeviceInstance device, GameObject buttonObj)
        {
            // Deselect previous highlight
            ClearHighlight();

            // Select this device in the ShopController
            GameManager.Instance?.Shop?.SelectDevice(device);

            // Highlight this button
            _highlightedDevice = device;
            var outline = buttonObj.GetComponent<Outline>();
            if (outline == null)
                outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.8f, 1f, 1f);
            outline.effectDistance = new Vector2(3, 3);

            Debug.Log($"[InventoryPanelView] Selected: {device.Data.DeviceName} ({device.GetConditionLabelPlain()})");
        }

        private void ClearHighlight()
        {
            _highlightedDevice = null;
            // Remove outlines from all items
            foreach (var btn in _itemButtons)
            {
                if (btn == null) continue;
                var outline = btn.GetComponent<Outline>();
                if (outline != null) Destroy(outline);
            }
        }
    }
}
