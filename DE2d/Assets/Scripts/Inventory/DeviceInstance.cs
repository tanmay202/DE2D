// ============================================================================
// DeviceInstance.cs — Runtime Device Instance (Phone-Enhanced)
// Responsibility: A SINGLE physical device in inventory. Wraps DeviceData with
//                 runtime state: condition, color choice, purchase price, age,
//                 defects, battery health degradation.
// Dependencies: DeviceData, PhoneBrandData, PhoneColorVariant
// Scene Placement: None — plain C# class.
// ============================================================================

using UnityEngine;

namespace DeviceEmpire.Inventory
{
    /// <summary>
    /// A single physical device the player owns. Phone-enhanced with color
    /// selection, battery health tracking, and brand-aware value calculations.
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

        [Tooltip("Whether this device has a hidden defect")]
        public bool HasHiddenDefect;

        // ── Phone-Specific Runtime State ───────────────────────────────────
        [Tooltip("Index into DeviceData.AvailableColors for this unit's color")]
        public int SelectedColorIndex;

        [Tooltip("Battery health percentage (100 = new, degrades over time for used)")]
        [Range(0f, 100f)]
        public float BatteryHealth = 100f;

        [Tooltip("Whether the screen has scratches (cosmetic, affects perceived value)")]
        public bool HasScreenScratches;

        [Tooltip("Whether the device has been repaired/refurbished")]
        public bool IsRefurbished;

        [Tooltip("IMEI-like unique serial number (flavor, used for future warranty system)")]
        public string SerialNumber;

        // ── Player-Set Price ───────────────────────────────────────────────
        [Tooltip("The price the player has set for this device")]
        public float PlayerSetPrice;

        // ── Unique ID ──────────────────────────────────────────────────────
        private static int _nextId = 0;
        public int InstanceId { get; private set; }

        // ── Constructor ────────────────────────────────────────────────────

        /// <summary>
        /// Create a new device instance with full phone state.
        /// </summary>
        public DeviceInstance(DeviceData data, DeviceCondition condition, float purchasePrice,
                              int dayPurchased, int colorIndex = 0)
        {
            Data = data;
            Condition = condition;
            PurchasePrice = purchasePrice;
            DayPurchased = dayPurchased;
            SelectedColorIndex = colorIndex;
            PlayerSetPrice = data.SuggestedRetailPrice;
            InstanceId = _nextId++;
            HasHiddenDefect = false;
            HasScreenScratches = false;
            IsRefurbished = false;

            // Generate serial number
            SerialNumber = GenerateSerial(data);

            // Battery health based on condition
            BatteryHealth = condition switch
            {
                DeviceCondition.New  => 100f,
                DeviceCondition.Good => Random.Range(85f, 95f),
                DeviceCondition.Fair => Random.Range(70f, 85f),
                DeviceCondition.Poor => Random.Range(45f, 70f),
                _ => 100f
            };

            // Screen scratches probability
            HasScreenScratches = condition switch
            {
                DeviceCondition.New  => false,
                DeviceCondition.Good => Random.value < 0.1f,
                DeviceCondition.Fair => Random.value < 0.4f,
                DeviceCondition.Poor => Random.value < 0.75f,
                _ => false
            };

            // Hidden defect chance — influenced by brand defect rate
            float brandDefectRate = data.Brand != null ? data.Brand.DefectRate : 0.1f;
            if (condition != DeviceCondition.New)
            {
                float baseChance = condition switch
                {
                    DeviceCondition.Good => 0.05f,
                    DeviceCondition.Fair => 0.15f,
                    DeviceCondition.Poor => 0.30f,
                    _ => 0f
                };
                // Brand with high defect rate increases chance
                float adjustedChance = baseChance * (0.5f + brandDefectRate);
                HasHiddenDefect = Random.value < adjustedChance;
            }
        }

        /// <summary>Parameterless constructor for serialization.</summary>
        public DeviceInstance()
        {
            InstanceId = _nextId++;
            BatteryHealth = 100f;
            SerialNumber = "UNKNOWN";
        }

        // ── Value Calculation ──────────────────────────────────────────────

        /// <summary>
        /// Calculate current market value including brand premium, specs, and condition.
        /// 
        /// Enhanced Formula:
        ///   effectiveWholesale = BaseWholesale × BrandPremium
        ///   depreciated = effectiveWholesale - (EffectiveDepreciation × daysOwned)
        ///   conditioned = depreciated × conditionMultiplier
        ///   batteryAdjusted = conditioned × batteryHealthFactor
        ///   colorAdjusted = batteryAdjusted × colorPriceMultiplier
        ///   floor = effectiveWholesale × minValueFloor
        ///   value = max(colorAdjusted, floor)
        /// </summary>
        public float GetCurrentValue(int currentDay)
        {
            float effectiveWholesale = Data.GetEffectiveWholesalePrice();
            float effectiveDepreciation = Data.GetEffectiveDepreciation();
            float daysOwned = Mathf.Max(0, currentDay - DayPurchased);

            // Depreciate
            float depreciated = effectiveWholesale - (effectiveDepreciation * daysOwned);

            // Apply condition
            float condMultiplier = Data.GetConditionMultiplier(Condition);
            float value = depreciated * condMultiplier;

            // Battery health adjustment (poor battery = lower value)
            float batteryFactor = 0.7f + (0.3f * (BatteryHealth / 100f));
            value *= batteryFactor;

            // Color premium
            float colorMult = GetSelectedColorPriceMultiplier();
            value *= colorMult;

            // Screen scratches penalty
            if (HasScreenScratches)
                value *= 0.92f;

            // Refurbished slight discount
            if (IsRefurbished)
                value *= 0.95f;

            // Floor
            float floor = effectiveWholesale * Data.MinValueFloor;
            value = Mathf.Max(value, floor);

            return Mathf.Round(value * 100f) / 100f;
        }

        // ── Color Helpers ──────────────────────────────────────────────────

        /// <summary>Get the selected color variant, or null if invalid.</summary>
        public PhoneColorVariant GetSelectedColor()
        {
            if (Data.AvailableColors == null || Data.AvailableColors.Length == 0)
                return null;
            int idx = Mathf.Clamp(SelectedColorIndex, 0, Data.AvailableColors.Length - 1);
            return Data.AvailableColors[idx];
        }

        /// <summary>Get the color name for display.</summary>
        public string GetColorName()
        {
            var color = GetSelectedColor();
            return color != null ? color.ColorName : "Standard";
        }

        /// <summary>Get the price multiplier from the selected color.</summary>
        public float GetSelectedColorPriceMultiplier()
        {
            var color = GetSelectedColor();
            return color != null ? color.PriceMultiplier : 1f;
        }

        // ── Spec Summary Helpers ───────────────────────────────────────────

        /// <summary>Get a one-line spec summary for UI.</summary>
        public string GetSpecSummary()
        {
            if (Data.Category != DeviceCategory.Phone)
                return Data.Description;

            var s = Data.Specs;
            return $"{s.RAM_GB}GB RAM | {s.Storage_GB}GB | {s.MainCamera_MP}MP | {s.BatteryCapacity_mAh}mAh";
        }

        /// <summary>Get the brand name for display.</summary>
        public string GetBrandName()
        {
            return Data.Brand != null ? Data.Brand.BrandName : "Unbranded";
        }

        /// <summary>Get full display name: Brand + DeviceName.</summary>
        public string GetFullName()
        {
            string brand = GetBrandName();
            return brand != "Unbranded" ? $"{brand} {Data.DeviceName}" : Data.DeviceName;
        }

        // ── Profit / Margin ────────────────────────────────────────────────

        public float GetExpectedProfit() => PlayerSetPrice - PurchasePrice;

        public float GetExpectedMarginPercent()
        {
            if (PurchasePrice <= 0f) return 0f;
            return ((PlayerSetPrice - PurchasePrice) / PurchasePrice) * 100f;
        }

        public int GetDaysInStock(int currentDay) => Mathf.Max(0, currentDay - DayPurchased);

        // ── Condition Labels ───────────────────────────────────────────────

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

        public string GetBatteryHealthLabel()
        {
            if (BatteryHealth >= 90f) return "<color=#4CAF50>Excellent</color>";
            if (BatteryHealth >= 75f) return "<color=#8BC34A>Good</color>";
            if (BatteryHealth >= 60f) return "<color=#FFC107>Fair</color>";
            return "<color=#FF5722>Degraded</color>";
        }

        // ── Serial Number ──────────────────────────────────────────────────

        private static string GenerateSerial(DeviceData data)
        {
            string prefix = data.Category switch
            {
                DeviceCategory.Phone => "PH",
                DeviceCategory.Laptop => "LP",
                DeviceCategory.Tablet => "TB",
                DeviceCategory.Accessory => "AC",
                _ => "XX"
            };
            return $"{prefix}-{Random.Range(100000, 999999)}-{Random.Range(1000, 9999)}";
        }

        public override string ToString()
        {
            return $"[#{InstanceId}] {GetFullName()} ({GetConditionLabelPlain()}, {GetColorName()}) " +
                   $"Battery: {BatteryHealth:F0}% | Cost: ${PurchasePrice:F2} | SN: {SerialNumber}";
        }
    }
}
