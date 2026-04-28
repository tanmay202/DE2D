// ============================================================================
// CustomerArchetypeData.cs — Customer Type ScriptableObject (Phone-Enhanced)
// Responsibility: Define a customer archetype's trait ranges, phone spec
//                 preferences, brand affinity, and feature priorities.
// Dependencies: PhoneBrandData, DeviceCategory
// Usage: Create via Assets → Create → DeviceEmpire → Customer Archetype
// ============================================================================

using UnityEngine;
using DeviceEmpire.Inventory;

namespace DeviceEmpire.Customers
{
    /// <summary>
    /// ScriptableObject defining a customer archetype with phone-aware preferences.
    /// Each archetype specifies trait RANGES and spec priority weights.
    /// Individual customers sample from these ranges at spawn time.
    /// 
    /// PHONE-SPECIFIC ADDITIONS:
    /// - Brand affinity and preferred brands
    /// - Spec priority weights (camera vs performance vs battery)
    /// - Minimum spec requirements
    /// - Color preferences
    /// - Feature must-haves (5G, NFC, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "Archetype_New", menuName = "DeviceEmpire/Customer Archetype", order = 1)]
    public class CustomerArchetypeData : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════════
        // SECTION 1: IDENTITY
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ IDENTITY ═══")]
        [Tooltip("Display name for this archetype")]
        public string ArchetypeName = "Standard Shopper";

        [Tooltip("Visual representation of this customer type")]
        public Sprite Visual;

        [Tooltip("Color tint for UI elements")]
        public Color ArchetypeColor = Color.white;

        [Tooltip("Short description of this customer type")]
        [TextArea(2, 3)]
        public string Description = "A typical customer looking for a good deal.";

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 2: CORE TRAITS (sampled as ranges)
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ CORE TRAITS (min, max) ═══")]

        [Tooltip("Price sensitivity: 1.0 = only buys cheap, 0.0 = price doesn't matter")]
        public Vector2 BudgetSensitivity = new(0.4f, 0.7f);

        [Tooltip("Technical knowledge: 1.0 = spots defects, compares specs. 0.0 = clueless")]
        public Vector2 TechSavviness = new(0.1f, 0.4f);

        [Tooltip("Negotiation patience: 1.0 = haggles forever, 0.0 = walks immediately")]
        public Vector2 Patience = new(0.3f, 0.7f);

        [Tooltip("Reputation influence (V2+): 1.0 = big word-of-mouth")]
        public Vector2 SocialInfluence = new(0.1f, 0.4f);

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 3: SHOPPING PREFERENCES
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ SHOPPING PREFERENCES ═══")]

        [Tooltip("Device categories this archetype wants")]
        public DeviceCategory[] PreferredCategories = { DeviceCategory.Phone };

        [Tooltip("Budget range in dollars")]
        public Vector2 BudgetRange = new(50f, 200f);

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 4: BRAND PREFERENCES
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ BRAND PREFERENCES ═══")]

        [Tooltip("How much this customer cares about brands: 0.0 = brand-blind, 1.0 = brand-obsessed")]
        public Vector2 BrandAffinity = new(0.1f, 0.4f);

        [Tooltip("Preferred brands — customer pays more for these. Leave empty for no preference.")]
        public PhoneBrandData[] PreferredBrands;

        [Tooltip("Disliked brands — customer WTP drops for these. Leave empty for none.")]
        public PhoneBrandData[] DislikedBrands;

        [Tooltip("Preferred brand tier (if no specific brand preference)")]
        public BrandTier? PreferredTier;

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 5: SPEC PRIORITY WEIGHTS
        // Each weight determines how much that spec matters to this archetype.
        // Total doesn't need to sum to 1 — they're relative weights.
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ SPEC PRIORITIES (0=don't care, 1=critical) ═══")]

        [Tooltip("How much performance/speed matters")]
        [Range(0f, 1f)]
        public float PriorityPerformance = 0.3f;

        [Tooltip("How much camera quality matters")]
        [Range(0f, 1f)]
        public float PriorityCamera = 0.3f;

        [Tooltip("How much battery life matters")]
        [Range(0f, 1f)]
        public float PriorityBattery = 0.5f;

        [Tooltip("How much display quality matters (size, type, refresh rate)")]
        [Range(0f, 1f)]
        public float PriorityDisplay = 0.3f;

        [Tooltip("How much storage/RAM matters")]
        [Range(0f, 1f)]
        public float PriorityMemory = 0.3f;

        [Tooltip("How much build quality matters (materials, IP rating, weight)")]
        [Range(0f, 1f)]
        public float PriorityBuildQuality = 0.2f;

        [Tooltip("How much connectivity features matter (5G, NFC, WiFi standard)")]
        [Range(0f, 1f)]
        public float PriorityConnectivity = 0.2f;

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 6: MINIMUM SPEC REQUIREMENTS
        // Customer won't consider a device that falls below these.
        // Set to 0 = no requirement for that spec.
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ MINIMUM REQUIREMENTS (0 = no requirement) ═══")]

        [Tooltip("Minimum RAM in GB (0 = any)")]
        [Range(0, 16)]
        public int MinRAM_GB = 0;

        [Tooltip("Minimum storage in GB (0 = any)")]
        [Range(0, 512)]
        public int MinStorage_GB = 0;

        [Tooltip("Minimum battery capacity in mAh (0 = any)")]
        [Range(0, 6000)]
        public int MinBattery_mAh = 0;

        [Tooltip("Minimum main camera MP (0 = any)")]
        [Range(0, 108)]
        public int MinCamera_MP = 0;

        [Tooltip("Minimum performance score (0 = any)")]
        [Range(0f, 100f)]
        public float MinPerformanceScore = 0f;

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 7: FEATURE MUST-HAVES
        // If a must-have is true but the device lacks it, WTP drops sharply.
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ FEATURE MUST-HAVES ═══")]

        [Tooltip("Customer requires 5G support")]
        public bool Requires5G = false;

        [Tooltip("Customer requires NFC")]
        public bool RequiresNFC = false;

        [Tooltip("Customer requires headphone jack")]
        public bool RequiresHeadphoneJack = false;

        [Tooltip("Customer requires expandable storage")]
        public bool RequiresExpandableStorage = false;

        [Tooltip("Customer requires wireless charging")]
        public bool RequiresWirelessCharging = false;

        [Tooltip("Customer requires water resistance (any IP rating)")]
        public bool RequiresWaterResistance = false;

        [Tooltip("Customer requires AMOLED or better display")]
        public bool RequiresAMOLED = false;

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 8: COLOR PREFERENCES
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ COLOR PREFERENCES ═══")]

        [Tooltip("How much color matters to this archetype: 0 = any, 1 = very picky")]
        [Range(0f, 1f)]
        public float ColorPreference = 0.1f;

        [Tooltip("Preferred color names (case-insensitive matching). Empty = no preference.")]
        public string[] PreferredColorNames;

        // ═══════════════════════════════════════════════════════════════════
        // SECTION 9: SPAWN
        // ═══════════════════════════════════════════════════════════════════
        [Header("═══ SPAWN CONFIGURATION ═══")]

        [Tooltip("Relative spawn weight. Higher = more common.")]
        [Min(0.1f)]
        public float SpawnWeight = 1.0f;

        // ═══════════════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════════════
        private void OnValidate()
        {
            ValidateRange(ref BudgetSensitivity, "BudgetSensitivity");
            ValidateRange(ref TechSavviness, "TechSavviness");
            ValidateRange(ref Patience, "Patience");
            ValidateRange(ref SocialInfluence, "SocialInfluence");
            ValidateRange(ref BrandAffinity, "BrandAffinity");

            BudgetRange.x = Mathf.Max(0f, BudgetRange.x);
            BudgetRange.y = Mathf.Max(BudgetRange.x, BudgetRange.y);

            if (PreferredCategories == null || PreferredCategories.Length == 0)
                Debug.LogWarning($"[CustomerArchetype] '{ArchetypeName}' has no preferred categories!");
        }

        private void ValidateRange(ref Vector2 range, string name)
        {
            range.x = Mathf.Clamp01(range.x);
            range.y = Mathf.Clamp01(range.y);
            if (range.x > range.y)
            {
                Debug.LogWarning($"[CustomerArchetype] '{ArchetypeName}': {name} min > max. Swapping.");
                (range.x, range.y) = (range.y, range.x);
            }
        }
    }
}
