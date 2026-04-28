// ============================================================================
// DeviceData.cs — Device ScriptableObject Definition (Phone-First)
// Responsibility: Data container for a device TYPE (not instance).
//                 V1 focuses on PHONES with full spec depth:
//                 Brand, RAM, Storage, Battery, Display, Camera, Performance,
//                 Colors, Connectivity, OS, and Economics.
//                 Other categories (Laptop, Tablet, Accessory) added later.
// Dependencies: PhoneBrandData
// Usage: Create via Assets → Create → DeviceEmpire → Device Data
// ============================================================================

using System;
using UnityEngine;

namespace DeviceEmpire.Inventory
{
    /// <summary>
    /// ScriptableObject defining a device archetype with full phone specifications.
    /// Each physical device in the game references one of these for its base stats.
    /// 
    /// ARCHITECTURE: Phone-first design. The PhoneSpecs section is the detailed
    /// specification block. When Laptop/Tablet are added later, they'll get their
    /// own spec blocks (LaptopSpecs, TabletSpecs) and share the common fields.
    /// </summary>
    [CreateAssetMenu(fileName = "Device_New", menuName = "DeviceEmpire/Device Data", order = 0)]
    public class DeviceData : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════════
        // SECTION 1: IDENTITY
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ IDENTITY ═══")]
        [Tooltip("Display name shown to the player (e.g., 'Starphone Galaxy S24')")]
        public string DeviceName = "New Device";

        [Tooltip("Device category — determines which systems and customers interact with it")]
        public DeviceCategory Category = DeviceCategory.Phone;

        [Tooltip("Device icon for inventory, shop, and customer UI")]
        public Sprite Icon;

        [Tooltip("Short marketing description (shown in tooltips and detail views)")]
        [TextArea(2, 4)]
        public string Description = "A standard electronic device.";

        [Tooltip("Model year — affects depreciation and customer perception")]
        [Range(2018, 2030)]
        public int ModelYear = 2024;

        [Tooltip("Release generation (e.g., 'Series 24', 'Gen 5')")]
        public string Generation = "";

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 2: BRAND
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ BRAND ═══")]
        [Tooltip("Reference to the phone brand ScriptableObject")]
        public PhoneBrandData Brand;

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 3: PHONE SPECIFICATIONS
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ PHONE SPECS ═══")]
        public PhoneSpecs Specs;

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 4: AVAILABLE COLORS
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ COLORS ═══")]
        [Tooltip("Available color variants for this device model")]
        public PhoneColorVariant[] AvailableColors;

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 5: ECONOMICS
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ ECONOMICS ═══")]
        [Tooltip("Base wholesale price in NEW condition (before brand premium)")]
        [Min(1f)]
        public float BaseWholesalePrice = 100f;

        [Tooltip("Starting suggested retail price — player's pricing reference")]
        [Min(1f)]
        public float SuggestedRetailPrice = 150f;

        [Tooltip("Value lost per in-game day (base, before brand depreciation modifier)")]
        [Min(0f)]
        public float DepreciationPerDay = 0.5f;

        [Tooltip("Minimum value floor as % of wholesale (prevents $0 devices)")]
        [Range(0.05f, 0.5f)]
        public float MinValueFloor = 0.1f;

        // ── Condition Multipliers ──────────────────────────────────────────
        [Header("Condition Value Multipliers")]
        [Range(0.5f, 1.5f)] public float ConditionMultiplier_New = 1.0f;
        [Range(0.3f, 1.2f)] public float ConditionMultiplier_Good = 0.85f;
        [Range(0.2f, 1.0f)] public float ConditionMultiplier_Fair = 0.65f;
        [Range(0.1f, 0.8f)] public float ConditionMultiplier_Poor = 0.40f;

        // ═══════════════════════════════════════════════════════════════════
        // COMPUTED PROPERTIES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Get the EFFECTIVE wholesale price including brand premium.
        /// A premium brand phone costs more to acquire from suppliers.
        /// </summary>
        public float GetEffectiveWholesalePrice()
        {
            float brandMod = Brand != null ? Brand.PricePremiumMultiplier : 1f;
            return Mathf.Round(BaseWholesalePrice * brandMod * 100f) / 100f;
        }

        /// <summary>
        /// Get the effective depreciation rate including brand modifier.
        /// Premium brands hold value better.
        /// </summary>
        public float GetEffectiveDepreciation()
        {
            float brandMod = Brand != null ? Brand.DepreciationMultiplier : 1f;
            return DepreciationPerDay * brandMod;
        }

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
        /// Includes brand premium.
        /// </summary>
        public float GetWholesalePrice(DeviceCondition condition)
        {
            float baseCost = GetEffectiveWholesalePrice();
            float condFactor = condition switch
            {
                DeviceCondition.New  => 1.0f,
                DeviceCondition.Good => 0.7f,
                DeviceCondition.Fair => 0.5f,
                DeviceCondition.Poor => 0.3f,
                _ => 1f
            };
            return Mathf.Round(baseCost * condFactor * 100f) / 100f;
        }

        /// <summary>
        /// Calculate the overall "spec score" of this phone (0-100).
        /// Used by tech-savvy customers to evaluate value-for-money.
        /// Weighs all specs proportionally.
        /// </summary>
        public float GetSpecScore()
        {
            if (Category != DeviceCategory.Phone) return 50f; // Non-phones get average

            float score = 0f;
            float totalWeight = 0f;

            // RAM (weight: 15) — 2GB = 10, 4GB = 30, 8GB = 60, 12GB = 80, 16GB+ = 95
            float ramScore = Mathf.Clamp01((Specs.RAM_GB - 2f) / 14f) * 100f;
            score += ramScore * 15f; totalWeight += 15f;

            // Storage (weight: 12) — 16GB = 10, 64GB = 40, 128GB = 60, 256GB = 80, 512GB+ = 95
            float storageScore = Mathf.Clamp01((Specs.Storage_GB - 16f) / 496f) * 100f;
            score += storageScore * 12f; totalWeight += 12f;

            // Battery (weight: 12) — 2000 = 20, 3000 = 40, 4500 = 65, 5000 = 80, 6000+ = 95
            float batteryScore = Mathf.Clamp01((Specs.BatteryCapacity_mAh - 2000f) / 4000f) * 100f;
            score += batteryScore * 12f; totalWeight += 12f;

            // Display size (weight: 8) — 5.0 = 30, 6.0 = 50, 6.5 = 70, 6.8+ = 85
            float displayScore = Mathf.Clamp01((Specs.DisplaySize_inches - 5f) / 2f) * 100f;
            score += displayScore * 8f; totalWeight += 8f;

            // Display type (weight: 8)
            float displayTypeScore = Specs.DisplayType switch
            {
                DisplayType.LCD => 20f,
                DisplayType.IPS_LCD => 35f,
                DisplayType.OLED => 65f,
                DisplayType.AMOLED => 80f,
                DisplayType.Super_AMOLED => 90f,
                DisplayType.LTPO_AMOLED => 100f,
                _ => 30f
            };
            score += displayTypeScore * 8f; totalWeight += 8f;

            // Refresh rate (weight: 7) — 60Hz = 30, 90Hz = 55, 120Hz = 80, 144Hz = 95
            float refreshScore = Mathf.Clamp01((Specs.RefreshRate_Hz - 60f) / 84f) * 100f;
            score += refreshScore * 7f; totalWeight += 7f;

            // Camera (weight: 10) — 8MP = 15, 12MP = 30, 48MP = 60, 108MP = 85, 200MP = 100
            float cameraScore = Mathf.Clamp01((Specs.MainCamera_MP - 8f) / 192f) * 100f;
            score += cameraScore * 10f; totalWeight += 10f;

            // Performance (weight: 15) — direct 0-100 mapping
            score += Specs.PerformanceScore * 15f; totalWeight += 15f;

            // 5G support (weight: 5)
            float connectivityScore = Specs.Has5G ? 90f : 30f;
            score += connectivityScore * 5f; totalWeight += 5f;

            // Fast charging (weight: 5) — 10W = 20, 25W = 45, 45W = 70, 65W = 85, 120W+ = 100
            float chargeScore = Mathf.Clamp01((Specs.FastChargeWatts - 10f) / 110f) * 100f;
            score += chargeScore * 5f; totalWeight += 5f;

            // NFC (weight: 3)
            score += (Specs.HasNFC ? 80f : 10f) * 3f; totalWeight += 3f;

            return totalWeight > 0 ? score / totalWeight : 50f;
        }

        /// <summary>
        /// Get a human-readable spec tier label based on the spec score.
        /// </summary>
        public string GetSpecTier()
        {
            float score = GetSpecScore();
            if (score >= 85f) return "Flagship";
            if (score >= 70f) return "Upper Mid-Range";
            if (score >= 50f) return "Mid-Range";
            if (score >= 30f) return "Entry-Level";
            return "Basic";
        }

        /// <summary>
        /// Get expected margin at SRP for editor validation.
        /// </summary>
        public float GetExpectedMarginPercent()
        {
            float cost = GetEffectiveWholesalePrice();
            if (cost <= 0) return 0;
            return ((SuggestedRetailPrice - cost) / cost) * 100f;
        }

        // ═══════════════════════════════════════════════════════════════════
        // EDITOR VALIDATION
        // ═══════════════════════════════════════════════════════════════════
        private void OnValidate()
        {
            float effectiveWholesale = GetEffectiveWholesalePrice();

            if (SuggestedRetailPrice < effectiveWholesale)
            {
                Debug.LogWarning($"[DeviceData] '{DeviceName}': SRP (${SuggestedRetailPrice}) " +
                                 $"< effective wholesale (${effectiveWholesale}). Player loses money at default price!");
            }

            if (ConditionMultiplier_Good > ConditionMultiplier_New ||
                ConditionMultiplier_Fair > ConditionMultiplier_Good ||
                ConditionMultiplier_Poor > ConditionMultiplier_Fair)
            {
                Debug.LogWarning($"[DeviceData] '{DeviceName}': Condition multipliers should decrease: New > Good > Fair > Poor");
            }

            if (Brand == null && Category == DeviceCategory.Phone)
            {
                Debug.LogWarning($"[DeviceData] '{DeviceName}': Phone device has no Brand assigned!");
            }

            if (AvailableColors == null || AvailableColors.Length == 0)
            {
                Debug.LogWarning($"[DeviceData] '{DeviceName}': No color variants defined!");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHONE SPECIFICATIONS — Serializable data block
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Complete phone hardware specifications. Embedded in DeviceData.
    /// Every field maps to a real phone spec that affects gameplay:
    /// - Customers compare specs to their needs/wants
    /// - Tech-savvy customers weigh specs more heavily
    /// - Spec score affects WTP calculation
    /// </summary>
    [Serializable]
    public class PhoneSpecs
    {
        // ── Processor / Performance ────────────────────────────────────────
        [Header("╔═ Processor & Performance ═╗")]
        [Tooltip("Processor/chipset name (e.g., 'Snapdragon 8 Gen 3', 'Dimensity 9200')")]
        public string ProcessorName = "Generic Quad-Core";

        [Tooltip("Processor brand for customer preference matching")]
        public ProcessorBrand ProcessorBrand = ProcessorBrand.MediaTek;

        [Tooltip("Overall performance score: 0 = unusable, 100 = best-in-class flagship. " +
                 "Roughly: Budget=20-35, MidRange=40-60, Flagship=70-95")]
        [Range(0f, 100f)]
        public float PerformanceScore = 50f;

        [Tooltip("AnTuTu-like benchmark score (thousands). For display/comparison. " +
                 "Budget=100-300K, Mid=400-700K, Flagship=800K-2M")]
        [Min(50)]
        public int BenchmarkScore = 300;

        // ── Memory ─────────────────────────────────────────────────────────
        [Header("╔═ Memory ═╗")]
        [Tooltip("RAM in gigabytes. Common: 2, 3, 4, 6, 8, 12, 16")]
        [Range(1, 24)]
        public int RAM_GB = 4;

        [Tooltip("RAM type (e.g., LPDDR4X, LPDDR5, LPDDR5X)")]
        public RAMType RAMType = RAMType.LPDDR4X;

        [Tooltip("Internal storage in gigabytes. Common: 32, 64, 128, 256, 512, 1024")]
        [Range(8, 2048)]
        public int Storage_GB = 64;

        [Tooltip("Storage type")]
        public StorageType StorageType = StorageType.eMMC;

        [Tooltip("Whether the device supports expandable storage (microSD)")]
        public bool HasExpandableStorage = false;

        [Tooltip("Max expandable storage in GB (0 if not supported)")]
        [Range(0, 2048)]
        public int MaxExpandableStorage_GB = 0;

        // ── Display ────────────────────────────────────────────────────────
        [Header("╔═ Display ═╗")]
        [Tooltip("Screen size in inches (diagonal)")]
        [Range(4f, 8f)]
        public float DisplaySize_inches = 6.1f;

        [Tooltip("Display panel technology")]
        public DisplayType DisplayType = DisplayType.IPS_LCD;

        [Tooltip("Screen resolution")]
        public ScreenResolution Resolution = ScreenResolution.FHD_Plus;

        [Tooltip("Refresh rate in Hz. Common: 60, 90, 120, 144")]
        [Range(30, 240)]
        public int RefreshRate_Hz = 60;

        [Tooltip("Peak brightness in nits (affects outdoor visibility marketing)")]
        [Range(200, 3000)]
        public int PeakBrightness_nits = 500;

        [Tooltip("Whether the display has HDR support")]
        public bool HasHDR = false;

        [Tooltip("Whether it has Always-On Display")]
        public bool HasAlwaysOnDisplay = false;

        [Tooltip("Screen protection type")]
        public ScreenProtection ScreenProtection = ScreenProtection.None;

        // ── Camera System ──────────────────────────────────────────────────
        [Header("╔═ Camera System ═╗")]
        [Tooltip("Main rear camera megapixels")]
        [Range(2, 200)]
        public int MainCamera_MP = 12;

        [Tooltip("Number of rear camera lenses (1-5)")]
        [Range(1, 5)]
        public int RearCameraCount = 1;

        [Tooltip("Whether device has ultrawide lens")]
        public bool HasUltrawide = false;

        [Tooltip("Whether device has telephoto/zoom lens")]
        public bool HasTelephoto = false;

        [Tooltip("Maximum optical zoom (1 = no optical zoom)")]
        [Range(1f, 10f)]
        public float MaxOpticalZoom = 1f;

        [Tooltip("Front camera megapixels")]
        [Range(2, 60)]
        public int FrontCamera_MP = 8;

        [Tooltip("Whether device can record 4K video")]
        public bool CanRecord4K = false;

        [Tooltip("Whether device has OIS (Optical Image Stabilization)")]
        public bool HasOIS = false;

        [Tooltip("Overall camera quality score: 0-100")]
        [Range(0f, 100f)]
        public float CameraQualityScore = 40f;

        // ── Battery & Charging ─────────────────────────────────────────────
        [Header("╔═ Battery & Charging ═╗")]
        [Tooltip("Battery capacity in milliamp-hours")]
        [Range(1000, 7000)]
        public int BatteryCapacity_mAh = 4000;

        [Tooltip("Estimated screen-on time in hours (manufacturer claim)")]
        [Range(2f, 20f)]
        public float ScreenOnTime_hours = 6f;

        [Tooltip("Fast charging wattage (0 = no fast charge)")]
        [Range(0, 240)]
        public int FastChargeWatts = 0;

        [Tooltip("Whether device supports wireless charging")]
        public bool HasWirelessCharging = false;

        [Tooltip("Wireless charging wattage (0 if not supported)")]
        [Range(0, 50)]
        public int WirelessChargeWatts = 0;

        [Tooltip("Whether device supports reverse wireless charging")]
        public bool HasReverseCharging = false;

        // ── Connectivity ───────────────────────────────────────────────────
        [Header("╔═ Connectivity ═╗")]
        [Tooltip("5G network support")]
        public bool Has5G = false;

        [Tooltip("WiFi standard")]
        public WiFiStandard WiFiStandard = WiFiStandard.WiFi_5;

        [Tooltip("Bluetooth version")]
        public BluetoothVersion BluetoothVersion = BluetoothVersion.BT_5_0;

        [Tooltip("NFC for contactless payments")]
        public bool HasNFC = false;

        [Tooltip("Whether device has a 3.5mm headphone jack")]
        public bool HasHeadphoneJack = true;

        [Tooltip("USB port type")]
        public USBType USBType = USBType.MicroUSB;

        [Tooltip("Number of SIM card slots")]
        [Range(0, 3)]
        public int SimSlots = 1;

        [Tooltip("eSIM support")]
        public bool HasESIM = false;

        [Tooltip("Whether device has IR blaster")]
        public bool HasIRBlaster = false;

        // ── Build & Design ─────────────────────────────────────────────────
        [Header("╔═ Build & Design ═╗")]
        [Tooltip("Device weight in grams")]
        [Range(100, 300)]
        public int Weight_grams = 175;

        [Tooltip("IP water/dust resistance rating")]
        public IPRating IPRating = IPRating.None;

        [Tooltip("Back material")]
        public BackMaterial BackMaterial = BackMaterial.Plastic;

        [Tooltip("Frame material")]
        public FrameMaterial FrameMaterial = FrameMaterial.Plastic;

        [Tooltip("Whether device has fingerprint sensor")]
        public bool HasFingerprint = true;

        [Tooltip("Fingerprint sensor type")]
        public FingerprintType FingerprintType = FingerprintType.Rear;

        [Tooltip("Whether device has face unlock")]
        public bool HasFaceUnlock = false;

        [Tooltip("Whether device has stereo speakers")]
        public bool HasStereoSpeakers = false;

        // ── Software ───────────────────────────────────────────────────────
        [Header("╔═ Software ═╗")]
        [Tooltip("Operating system")]
        public PhoneOS OperatingSystem = PhoneOS.Android;

        [Tooltip("OS version (e.g., 14 for Android 14, 17 for iOS 17)")]
        [Range(1, 30)]
        public int OSVersion = 13;

        [Tooltip("Guaranteed years of OS updates remaining")]
        [Range(0, 7)]
        public int YearsOfUpdates = 2;

        [Tooltip("Guaranteed years of security patches remaining")]
        [Range(0, 7)]
        public int YearsOfSecurityPatches = 3;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COLOR VARIANT — Serializable
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A color variant of a phone model. Each device can come in multiple colors.
    /// Color can affect price slightly (special editions cost more).
    /// </summary>
    [Serializable]
    public class PhoneColorVariant
    {
        [Tooltip("Color name (e.g., 'Midnight Black', 'Phantom Silver')")]
        public string ColorName = "Black";

        [Tooltip("Actual color for UI preview")]
        public Color DisplayColor = Color.black;

        [Tooltip("Whether this is a limited/special edition color")]
        public bool IsSpecialEdition = false;

        [Tooltip("Price premium for this color (1.0 = no premium, 1.1 = 10% more)")]
        [Range(0.9f, 1.5f)]
        public float PriceMultiplier = 1.0f;

        [Tooltip("How popular this color is (affects demand)")]
        [Range(0f, 1f)]
        public float Popularity = 0.5f;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ENUMS — Complete phone specification enums
    // ═══════════════════════════════════════════════════════════════════════

    public enum DeviceCategory { Phone, Laptop, Tablet, Accessory }

    public enum DeviceCondition
    {
        New,    // Factory sealed, full value
        Good,   // Light use, minor cosmetic wear
        Fair,   // Visible wear, fully functional
        Poor    // Heavy wear, possible hidden defects
    }

    public enum ProcessorBrand
    {
        Qualcomm,       // Snapdragon
        MediaTek,       // Dimensity / Helio
        Samsung_Exynos, // Exynos
        Apple_Bionic,   // A-series / M-series
        Google_Tensor,  // Tensor
        HiSilicon,      // Kirin
        Unisoc,         // Budget SoCs
        Generic         // Unknown/unnamed
    }

    public enum RAMType { LPDDR3, LPDDR4, LPDDR4X, LPDDR5, LPDDR5X }

    public enum StorageType { eMMC, UFS_2_1, UFS_3_0, UFS_3_1, UFS_4_0, NVMe }

    public enum DisplayType
    {
        LCD,            // Basic LCD
        IPS_LCD,        // Good LCD with better viewing angles
        OLED,           // Organic LED — deep blacks, vibrant colors
        AMOLED,         // Active Matrix OLED — most common premium
        Super_AMOLED,   // Samsung's enhanced AMOLED
        LTPO_AMOLED     // Variable refresh rate AMOLED — flagship
    }

    public enum ScreenResolution
    {
        HD,         // 720p  — budget
        HD_Plus,    // 720p+ — entry-level
        FHD,        // 1080p — mid-range standard
        FHD_Plus,   // 1080p+ — most common
        QHD_Plus,   // 1440p+ — flagship
        FourK       // 4K — rare, Sony-style
    }

    public enum ScreenProtection
    {
        None,
        GorillaGlass_3,
        GorillaGlass_5,
        GorillaGlass_Victus,
        GorillaGlass_Victus_2,
        CeramicShield,
        Sapphire
    }

    public enum WiFiStandard { WiFi_4, WiFi_5, WiFi_6, WiFi_6E, WiFi_7 }
    public enum BluetoothVersion { BT_4_2, BT_5_0, BT_5_1, BT_5_2, BT_5_3 }
    public enum USBType { MicroUSB, USB_C, Lightning }

    public enum IPRating
    {
        None,       // No rating
        IP52,       // Splash resistant
        IP53,       // Light rain
        IP54,       // Splash proof
        IP65,       // Water jet resistant
        IP67,       // 1m submersible
        IP68,       // 1.5m+ submersible
        IP69        // High-pressure wash (rare)
    }

    public enum BackMaterial { Plastic, Polycarbonate, GlasticSamsung, Glass, Ceramic, Vegan_Leather, Metal, Kevlar }
    public enum FrameMaterial { Plastic, Aluminum, Stainless_Steel, Titanium }

    public enum FingerprintType
    {
        None,
        Rear,           // Back of phone
        Side,           // Power button
        UnderDisplay,   // Optical under screen
        Ultrasonic      // Samsung-style ultrasonic
    }

    public enum PhoneOS
    {
        Android,
        iOS,
        HarmonyOS,
        KaiOS,          // Feature phones
        Custom_Android  // Forked Android (no Google services)
    }
}
