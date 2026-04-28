// ============================================================================
// DeviceData.cs — Device ScriptableObject Definition
// Responsibility: Data container for a device TYPE (not instance).
//                 Defines base economics, identity, and condition multipliers.
//                 Lives in /ScriptableObjects/Devices/.
// Dependencies: None — pure data.
// Usage: Create via Assets → Create → DeviceEmpire → Device Data
// ============================================================================

using UnityEngine;

namespace DeviceEmpire.Inventory
{
    /// <summary>
    /// ScriptableObject defining a device archetype (e.g., "Used Android Phone").
    /// Each physical device in the game references one of these for its base stats.
    /// 
    /// WHY SCRIPTABLEOBJECT?
    /// - You'll rebalance these numbers 100+ times during development
    /// - Changes are instant — no recompilation needed
    /// - Designers (even if that's just future-you) can tweak in the Inspector
    /// - Easy to create variants: duplicate .asset, change a few numbers
    /// </summary>
    [CreateAssetMenu(fileName = "Device_New", menuName = "DeviceEmpire/Device Data", order = 0)]
    public class DeviceData : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────
        [Header("Identity")]
        [Tooltip("Display name shown to the player")]
        public string DeviceName = "New Device";

        [Tooltip("Device category — determines which customers want it")]
        public DeviceCategory Category = DeviceCategory.Phone;

        [Tooltip("Icon shown in inventory, shop, and customer UI")]
        public Sprite Icon;

        [Tooltip("Short flavor text for the device (shown in tooltips)")]
        [TextArea(2, 4)]
        public string Description = "A standard electronic device.";

        // ── Economics ──────────────────────────────────────────────────────
        [Header("Economics")]
        [Tooltip("What you pay suppliers for this device (base cost in NEW condition)")]
        [Min(1f)]
        public float BaseWholesalePrice = 100f;

        [Tooltip("Starting suggested retail price — player's initial pricing reference")]
        [Min(1f)]
        public float SuggestedRetailPrice = 150f;

        [Tooltip("Value lost per in-game day (simulates market depreciation)")]
        [Min(0f)]
        public float DepreciationPerDay = 0.5f;

        [Tooltip("Minimum value floor as percentage of wholesale (prevents $0 devices)")]
        [Range(0.05f, 0.5f)]
        public float MinValueFloor = 0.1f;

        // ── Condition Multipliers ──────────────────────────────────────────
        [Header("Condition Value Multipliers")]
        [Tooltip("Value multiplier when device is in NEW condition")]
        [Range(0.5f, 1.5f)]
        public float ConditionMultiplier_New = 1.0f;

        [Tooltip("Value multiplier when device is in GOOD condition")]
        [Range(0.3f, 1.2f)]
        public float ConditionMultiplier_Good = 0.85f;

        [Tooltip("Value multiplier when device is in FAIR condition")]
        [Range(0.2f, 1.0f)]
        public float ConditionMultiplier_Fair = 0.65f;

        [Tooltip("Value multiplier when device is in POOR condition")]
        [Range(0.1f, 0.8f)]
        public float ConditionMultiplier_Poor = 0.40f;

        // ── Computed Properties ────────────────────────────────────────────

        /// <summary>
        /// Get the condition multiplier for a given condition level.
        /// </summary>
        public float GetConditionMultiplier(DeviceCondition condition)
        {
            return condition switch
            {
                DeviceCondition.New  => ConditionMultiplier_New,
                DeviceCondition.Good => ConditionMultiplier_Good,
                DeviceCondition.Fair => ConditionMultiplier_Fair,
                DeviceCondition.Poor => ConditionMultiplier_Poor,
                _ => 1f
            };
        }

        /// <summary>
        /// Calculate what this device costs from suppliers at a given condition.
        /// Used by SupplierPanel.
        /// </summary>
        public float GetWholesalePrice(DeviceCondition condition)
        {
            float condFactor = condition switch
            {
                DeviceCondition.New  => 1.0f,
                DeviceCondition.Good => 0.7f,
                DeviceCondition.Fair => 0.5f,
                DeviceCondition.Poor => 0.3f,
                _ => 1f
            };
            return Mathf.Round(BaseWholesalePrice * condFactor * 100f) / 100f;
        }

        /// <summary>
        /// Calculate expected profit margin at suggested retail price (NEW condition).
        /// Useful for editor validation.
        /// </summary>
        public float GetExpectedMarginPercent()
        {
            if (BaseWholesalePrice <= 0) return 0;
            return ((SuggestedRetailPrice - BaseWholesalePrice) / BaseWholesalePrice) * 100f;
        }

        // ── Editor Validation ──────────────────────────────────────────────
        private void OnValidate()
        {
            // Ensure SRP is at least wholesale price (warn, don't force)
            if (SuggestedRetailPrice < BaseWholesalePrice)
            {
                Debug.LogWarning($"[DeviceData] '{DeviceName}': SuggestedRetailPrice (${SuggestedRetailPrice}) " +
                                 $"is below BaseWholesalePrice (${BaseWholesalePrice}). Player will lose money at default price!");
            }

            // Ensure condition multipliers are in descending order
            if (ConditionMultiplier_Good > ConditionMultiplier_New ||
                ConditionMultiplier_Fair > ConditionMultiplier_Good ||
                ConditionMultiplier_Poor > ConditionMultiplier_Fair)
            {
                Debug.LogWarning($"[DeviceData] '{DeviceName}': Condition multipliers should decrease: New > Good > Fair > Poor");
            }
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Device categories — determines customer interest and shop layout.
    /// V1 uses Phone, Laptop, Tablet, Accessory.
    /// </summary>
    public enum DeviceCategory
    {
        Phone,
        Laptop,
        Tablet,
        Accessory
    }

    /// <summary>
    /// Physical condition of a device instance.
    /// Affects value, customer willingness to pay, and visual presentation.
    /// </summary>
    public enum DeviceCondition
    {
        New,    // Factory sealed, full value
        Good,   // Light use, minor cosmetic wear
        Fair,   // Visible wear, fully functional
        Poor    // Heavy wear, possible hidden defects
    }
}
