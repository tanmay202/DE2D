// ============================================================================
// DeviceInstance.cs — Runtime Device Instance
// Responsibility: Represents a SINGLE physical device in the player's inventory.
//                 Wraps DeviceData (the type) with runtime state like condition,
//                 purchase price, age, and hidden defects.
// Dependencies: DeviceData ScriptableObject
// Scene Placement: None — this is a plain C# class, not a MonoBehaviour.
// ============================================================================

using UnityEngine;

namespace DeviceEmpire.Inventory
{
    /// <summary>
    /// A single physical device the player owns. Created when buying from suppliers.
    /// Destroyed (removed from list) when sold to a customer.
    /// 
    /// KEY DISTINCTION:
    /// - DeviceData = "what kind of device is this?" (shared archetype)
    /// - DeviceInstance = "this specific device I bought on Day 3 for $80" (unique)
    /// </summary>
    [System.Serializable]
    public class DeviceInstance
    {
        // ── Core Data ──────────────────────────────────────────────────────
        [Tooltip("Reference to the device type ScriptableObject")]
        public DeviceData Data;

        [Tooltip("Physical condition of this specific device")]
        public DeviceCondition Condition;

        [Tooltip("What the player paid for this device")]
        public float PurchasePrice;

        [Tooltip("In-game day when this device was acquired")]
        public int DayPurchased;

        [Tooltip("Whether this device has a hidden defect (revealed by tech-savvy customers)")]
        public bool HasHiddenDefect;

        // ── Player-Set Price ───────────────────────────────────────────────
        [Tooltip("The price the player has set for this device (defaults to SRP)")]
        public float PlayerSetPrice;

        // ── Unique ID ──────────────────────────────────────────────────────
        private static int _nextId = 0;
        public int InstanceId { get; private set; }

        // ── Constructor ────────────────────────────────────────────────────

        /// <summary>
        /// Create a new device instance. Called by SupplierPanel when buying stock.
        /// </summary>
        public DeviceInstance(DeviceData data, DeviceCondition condition, float purchasePrice, int dayPurchased)
        {
            Data = data;
            Condition = condition;
            PurchasePrice = purchasePrice;
            DayPurchased = dayPurchased;
            HasHiddenDefect = false;
            PlayerSetPrice = data.SuggestedRetailPrice;
            InstanceId = _nextId++;

            // Small chance of hidden defect on non-new items
            if (condition != DeviceCondition.New)
            {
                float defectChance = condition switch
                {
                    DeviceCondition.Good => 0.05f,  // 5% chance
                    DeviceCondition.Fair => 0.15f,   // 15% chance
                    DeviceCondition.Poor => 0.30f,   // 30% chance
                    _ => 0f
                };
                HasHiddenDefect = Random.value < defectChance;
            }
        }

        /// <summary>
        /// Parameterless constructor for serialization. Don't use directly.
        /// </summary>
        public DeviceInstance()
        {
            InstanceId = _nextId++;
        }

        // ── Value Calculation ──────────────────────────────────────────────

        /// <summary>
        /// Calculate the current market value of this device based on age and condition.
        /// This represents what a "fair" customer would consider a reasonable price.
        /// 
        /// Formula:
        ///   baseValue = wholesale - (depreciation × daysOwned)
        ///   currentValue = baseValue × conditionMultiplier
        ///   floor = wholesale × minValueFloor (prevents $0 devices)
        /// </summary>
        /// <param name="currentDay">The current in-game day number</param>
        /// <returns>Current market value in dollars</returns>
        public float GetCurrentValue(int currentDay)
        {
            float daysOwned = Mathf.Max(0, currentDay - DayPurchased);
            float depreciated = Data.BaseWholesalePrice - (Data.DepreciationPerDay * daysOwned);
            float condMultiplier = Data.GetConditionMultiplier(Condition);
            float value = depreciated * condMultiplier;

            // Apply floor — device never drops below X% of wholesale
            float floor = Data.BaseWholesalePrice * Data.MinValueFloor;
            value = Mathf.Max(value, floor);

            // Hidden defects reduce value by 25% (if known)
            // In V1, this is always hidden from player but affects WTP of tech-savvy customers
            // We don't apply it here — TransactionEngine handles it

            return Mathf.Round(value * 100f) / 100f; // Round to cents
        }

        /// <summary>
        /// Calculate the profit/loss if sold at the player's set price.
        /// Negative = selling at a loss.
        /// </summary>
        public float GetExpectedProfit()
        {
            return PlayerSetPrice - PurchasePrice;
        }

        /// <summary>
        /// Calculate the profit margin percentage at the player's set price.
        /// </summary>
        public float GetExpectedMarginPercent()
        {
            if (PurchasePrice <= 0f) return 0f;
            return ((PlayerSetPrice - PurchasePrice) / PurchasePrice) * 100f;
        }

        /// <summary>
        /// How many days this device has been in inventory.
        /// </summary>
        public int GetDaysInStock(int currentDay)
        {
            return Mathf.Max(0, currentDay - DayPurchased);
        }

        /// <summary>
        /// Get a color-coded condition label for UI display.
        /// </summary>
        public string GetConditionLabel()
        {
            return Condition switch
            {
                DeviceCondition.New  => "<color=#4CAF50>NEW</color>",
                DeviceCondition.Good => "<color=#8BC34A>GOOD</color>",
                DeviceCondition.Fair => "<color=#FFC107>FAIR</color>",
                DeviceCondition.Poor => "<color=#FF5722>POOR</color>",
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Get a plain condition label (no rich text).
        /// </summary>
        public string GetConditionLabelPlain()
        {
            return Condition switch
            {
                DeviceCondition.New  => "NEW",
                DeviceCondition.Good => "GOOD",
                DeviceCondition.Fair => "FAIR",
                DeviceCondition.Poor => "POOR",
                _ => "UNKNOWN"
            };
        }

        public override string ToString()
        {
            return $"[Device #{InstanceId}] {Data.DeviceName} ({GetConditionLabelPlain()}) " +
                   $"Bought: ${PurchasePrice:F2} on Day {DayPurchased}";
        }
    }
}
