// ============================================================================
// SupplierPanel.cs — V1 Supplier System (Phone-Enhanced)
// Responsibility: Let the player buy devices with full phone spec visibility.
//                 Shows brand, key specs, color variants, and condition.
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
    /// V1 supplier interface — phone-enhanced.
    /// Shows brand, key specs, color variants, and price per condition.
    /// </summary>
    public class SupplierPanel : MonoBehaviour
    {
        // ── Configuration ──────────────────────────────────────────────────
        [Header("Available Devices")]
        [Tooltip("Drag all DeviceData ScriptableObjects the supplier offers")]
        [SerializeField] private DeviceData[] availableDevices;

        [Header("UI References")]
        [SerializeField] private Transform itemListContainer;
        [SerializeField] private GameObject supplierItemPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI cashDisplay;
        [SerializeField] private TextMeshProUGUI slotsDisplay;

        // ── Events ─────────────────────────────────────────────────────────
        public event Action<DeviceInstance> OnDevicePurchased;
        public event Action<string> OnPurchaseFailed;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCashChanged += UpdateCashDisplay;

            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

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
                GameManager.Instance.OnCashChanged -= UpdateCashDisplay;
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Buy a device at a specific condition and color.
        /// </summary>
        public bool BuyDevice(DeviceData data, DeviceCondition condition, int colorIndex = 0)
        {
            if (data == null)
            {
                Debug.LogError("[SupplierPanel] Attempted to buy null device data.");
                return false;
            }

            // Calculate cost (includes brand premium + color premium)
            float cost = data.GetWholesalePrice(condition);

            // Apply color premium if applicable
            if (data.AvailableColors != null && colorIndex >= 0 && colorIndex < data.AvailableColors.Length)
            {
                cost *= data.AvailableColors[colorIndex].PriceMultiplier;
                cost = Mathf.Round(cost * 100f) / 100f;
            }

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

            // Create device instance with color selection
            int currentDay = GameManager.Instance.Clock?.CurrentDay ?? 1;
            var instance = new DeviceInstance(data, condition, cost, currentDay, colorIndex);

            // Add to inventory
            if (!GameManager.Instance.Inventory.AddDevice(instance))
            {
                GameManager.Instance.AddCash(cost);
                OnPurchaseFailed?.Invoke("Failed to add device to inventory.");
                return false;
            }

            string brandName = data.Brand != null ? data.Brand.BrandName : "";
            string colorName = instance.GetColorName();
            Debug.Log($"[SupplierPanel] Purchased: {brandName} {data.DeviceName} ({condition}, {colorName}) for ${cost:F2}");

            OnDevicePurchased?.Invoke(instance);
            UpdateDisplays();
            return true;
        }

        public void TogglePanel() => gameObject.SetActive(!gameObject.activeSelf);
        public void OpenPanel() { gameObject.SetActive(true); UpdateDisplays(); }
        public void ClosePanel() => gameObject.SetActive(false);

        // ── Internal ───────────────────────────────────────────────────────

        private void PopulateSupplierList()
        {
            if (itemListContainer == null || supplierItemPrefab == null)
            {
                Debug.LogWarning("[SupplierPanel] Missing UI references.");
                return;
            }

            foreach (Transform child in itemListContainer)
                Destroy(child.gameObject);

            DeviceCondition[] conditions = { DeviceCondition.New, DeviceCondition.Good, DeviceCondition.Fair, DeviceCondition.Poor };

            foreach (var device in availableDevices)
            {
                foreach (var condition in conditions)
                {
                    // For V1, create one item per condition with default color (index 0)
                    CreateSupplierItem(device, condition, 0);
                }
            }
        }

        private void CreateSupplierItem(DeviceData data, DeviceCondition condition, int colorIndex)
        {
            var item = Instantiate(supplierItemPrefab, itemListContainer);
            item.name = $"Supplier_{data.DeviceName}_{condition}";

            // Find UI elements
            var nameText = item.transform.Find("DeviceName")?.GetComponent<TextMeshProUGUI>();
            var condText = item.transform.Find("Condition")?.GetComponent<TextMeshProUGUI>();
            var priceText = item.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
            var buyButton = item.transform.Find("BuyButton")?.GetComponent<Button>();

            // New phone-specific fields (optional in prefab — gracefully skip if missing)
            var brandText = item.transform.Find("Brand")?.GetComponent<TextMeshProUGUI>();
            var specsText = item.transform.Find("Specs")?.GetComponent<TextMeshProUGUI>();
            var tierText = item.transform.Find("Tier")?.GetComponent<TextMeshProUGUI>();
            var brandIcon = item.transform.Find("BrandIcon")?.GetComponent<Image>();

            float cost = data.GetWholesalePrice(condition);

            // Apply color premium
            if (data.AvailableColors != null && colorIndex >= 0 && colorIndex < data.AvailableColors.Length)
                cost = Mathf.Round(cost * data.AvailableColors[colorIndex].PriceMultiplier * 100f) / 100f;

            // Populate text fields
            if (nameText != null)
            {
                string brandPrefix = data.Brand != null ? $"{data.Brand.BrandName} " : "";
                nameText.text = $"{brandPrefix}{data.DeviceName}";
            }

            if (condText != null)
                condText.text = condition.ToString();

            if (priceText != null)
                priceText.text = $"${cost:F2}";

            // Phone-specific fields
            if (brandText != null && data.Brand != null)
            {
                brandText.text = data.Brand.GetTierLabel();
            }

            if (specsText != null && data.Category == DeviceCategory.Phone)
            {
                var s = data.Specs;
                specsText.text = $"{s.RAM_GB}GB | {s.Storage_GB}GB | {s.MainCamera_MP}MP | {s.BatteryCapacity_mAh}mAh";
            }

            if (tierText != null)
                tierText.text = data.GetSpecTier();

            if (brandIcon != null && data.Brand?.BrandLogo != null)
                brandIcon.sprite = data.Brand.BrandLogo;

            // Buy button
            if (buyButton != null)
            {
                var deviceData = data;
                var deviceCondition = condition;
                var cIdx = colorIndex;
                buyButton.onClick.AddListener(() => BuyDevice(deviceData, deviceCondition, cIdx));
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
                headerText.text = "Street Supplier";
        }

        private void UpdateCashDisplay(float cash)
        {
            if (cashDisplay != null)
                cashDisplay.text = $"Cash: ${cash:F2}";
        }

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
