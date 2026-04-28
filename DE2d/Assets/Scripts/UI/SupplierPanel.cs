// ============================================================================
// SupplierPanel.cs — V1 Hardcoded Supplier System
// Responsibility: Let the player "buy" devices from a hardcoded supplier list.
//                 No supplier simulation yet — just enough to stock inventory.
//                 Replaced in V2 with a proper SupplierSystem with tiers/trust.
// Dependencies: GameManager, DeviceData, DeviceInstance, InventoryManager
// Scene Placement: UI/SupplierPanel (Canvas panel, toggled on/off)
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeviceEmpire.Core;
using DeviceEmpire.Inventory;

namespace DeviceEmpire.UI
{
    /// <summary>
    /// V1 supplier interface. Displays a list of available devices at various conditions.
    /// Player clicks to buy — device is added to inventory, cash is deducted.
    /// 
    /// In V2, this is replaced by:
    /// - Multiple supplier tiers (street vendor, wholesaler, distributor)
    /// - Trust/relationship system
    /// - Bulk order discounts
    /// - Supply availability fluctuation
    /// </summary>
    public class SupplierPanel : MonoBehaviour
    {
        // ── Configuration ──────────────────────────────────────────────────
        [Header("Available Devices")]
        [Tooltip("Drag all DeviceData ScriptableObjects the supplier offers here")]
        [SerializeField] private DeviceData[] availableDevices;

        [Header("UI References")]
        [Tooltip("Parent transform for supplier item buttons")]
        [SerializeField] private Transform itemListContainer;

        [Tooltip("Prefab for a single supplier item row")]
        [SerializeField] private GameObject supplierItemPrefab;

        [Tooltip("Close button for the supplier panel")]
        [SerializeField] private Button closeButton;

        [Tooltip("Header text showing supplier info")]
        [SerializeField] private TextMeshProUGUI headerText;

        [Tooltip("Player cash display in supplier panel")]
        [SerializeField] private TextMeshProUGUI cashDisplay;

        [Tooltip("Inventory slots remaining display")]
        [SerializeField] private TextMeshProUGUI slotsDisplay;

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired when a device is purchased. Param = the purchased device instance.</summary>
        public event Action<DeviceInstance> OnDevicePurchased;

        /// <summary>Fired when purchase fails (no cash or no space).</summary>
        public event Action<string> OnPurchaseFailed;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            // Subscribe to cash changes to update display
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCashChanged += UpdateCashDisplay;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            // Build the supplier item list
            PopulateSupplierList();
            UpdateDisplays();
        }

        private void OnEnable()
        {
            UpdateDisplays();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCashChanged -= UpdateCashDisplay;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Buy a device at a specific condition. Called by supplier item buttons.
        /// </summary>
        /// <param name="data">The DeviceData type to buy</param>
        /// <param name="condition">The condition to buy at</param>
        /// <returns>True if purchase succeeded</returns>
        public bool BuyDevice(DeviceData data, DeviceCondition condition)
        {
            if (data == null)
            {
                Debug.LogError("[SupplierPanel] Attempted to buy null device data.");
                return false;
            }

            // Calculate cost
            float cost = data.GetWholesalePrice(condition);

            // Check cash
            if (!GameManager.Instance.CanAfford(cost))
            {
                string msg = $"Not enough cash! Need ${cost:F2}, have ${GameManager.Instance.PlayerCash:F2}";
                Debug.Log($"[SupplierPanel] {msg}");
                OnPurchaseFailed?.Invoke(msg);
                return false;
            }

            // Check inventory space
            if (GameManager.Instance.Inventory.IsFull)
            {
                string msg = $"Inventory full! ({GameManager.Instance.Inventory.CurrentCount}/{GameManager.Instance.Inventory.MaxCapacity})";
                Debug.Log($"[SupplierPanel] {msg}");
                OnPurchaseFailed?.Invoke(msg);
                return false;
            }

            // Deduct cash
            if (!GameManager.Instance.SpendCash(cost))
            {
                OnPurchaseFailed?.Invoke("Transaction failed.");
                return false;
            }

            // Create device instance
            int currentDay = GameManager.Instance.Clock?.CurrentDay ?? 1;
            var instance = new DeviceInstance(data, condition, cost, currentDay);

            // Add to inventory
            if (!GameManager.Instance.Inventory.AddDevice(instance))
            {
                // Refund if inventory add fails
                GameManager.Instance.AddCash(cost);
                OnPurchaseFailed?.Invoke("Failed to add device to inventory.");
                return false;
            }

            Debug.Log($"[SupplierPanel] Purchased: {data.DeviceName} ({condition}) for ${cost:F2}");

            OnDevicePurchased?.Invoke(instance);
            UpdateDisplays();
            return true;
        }

        /// <summary>Toggle panel visibility.</summary>
        public void TogglePanel()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        /// <summary>Open the supplier panel.</summary>
        public void OpenPanel()
        {
            gameObject.SetActive(true);
            UpdateDisplays();
        }

        /// <summary>Close the supplier panel.</summary>
        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        // ── Internal ───────────────────────────────────────────────────────

        /// <summary>
        /// Build the supplier item list UI. Called once at Start.
        /// Creates one row per device × condition combination.
        /// </summary>
        private void PopulateSupplierList()
        {
            if (itemListContainer == null || supplierItemPrefab == null)
            {
                Debug.LogWarning("[SupplierPanel] Missing UI references. Assign itemListContainer and supplierItemPrefab.");
                return;
            }

            // Clear existing children
            foreach (Transform child in itemListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create a row for each device × condition
            DeviceCondition[] conditions = { DeviceCondition.New, DeviceCondition.Good, DeviceCondition.Fair, DeviceCondition.Poor };

            foreach (var device in availableDevices)
            {
                foreach (var condition in conditions)
                {
                    CreateSupplierItem(device, condition);
                }
            }
        }

        /// <summary>
        /// Create a single supplier item row in the UI.
        /// </summary>
        private void CreateSupplierItem(DeviceData data, DeviceCondition condition)
        {
            var item = Instantiate(supplierItemPrefab, itemListContainer);
            item.name = $"Supplier_{data.DeviceName}_{condition}";

            // Try to find and configure UI elements in the prefab
            var nameText = item.transform.Find("DeviceName")?.GetComponent<TextMeshProUGUI>();
            var condText = item.transform.Find("Condition")?.GetComponent<TextMeshProUGUI>();
            var priceText = item.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
            var buyButton = item.transform.Find("BuyButton")?.GetComponent<Button>();

            float cost = data.GetWholesalePrice(condition);

            if (nameText != null) nameText.text = data.DeviceName;
            if (condText != null) condText.text = condition.ToString();
            if (priceText != null) priceText.text = $"${cost:F2}";

            if (buyButton != null)
            {
                // Capture locals for the lambda
                var deviceData = data;
                var deviceCondition = condition;
                buyButton.onClick.AddListener(() => BuyDevice(deviceData, deviceCondition));
            }
        }

        private void UpdateDisplays()
        {
            if (GameManager.Instance == null) return;

            UpdateCashDisplay(GameManager.Instance.PlayerCash);

            if (slotsDisplay != null && GameManager.Instance.Inventory != null)
            {
                int current = GameManager.Instance.Inventory.CurrentCount;
                int max = GameManager.Instance.Inventory.MaxCapacity;
                slotsDisplay.text = $"Inventory: {current}/{max}";
            }

            if (headerText != null)
            {
                headerText.text = "Street Supplier"; // V1 — single supplier
            }
        }

        private void UpdateCashDisplay(float cash)
        {
            if (cashDisplay != null)
            {
                cashDisplay.text = $"Cash: ${cash:F2}";
            }
        }

        // ── Condition Cost Factor (V1 simple) ──────────────────────────────

        /// <summary>
        /// Get the cost multiplier for a given condition. Used internally.
        /// Moved to DeviceData.GetWholesalePrice() for cleaner access.
        /// </summary>
        public static float GetConditionCostFactor(DeviceCondition condition)
        {
            return condition switch
            {
                DeviceCondition.New  => 1.0f,
                DeviceCondition.Good => 0.7f,
                DeviceCondition.Fair => 0.5f,
                DeviceCondition.Poor => 0.3f,
                _ => 1f
            };
        }
    }
}
