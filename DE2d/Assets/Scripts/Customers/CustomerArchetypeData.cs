// ============================================================================
// CustomerArchetypeData.cs — Customer Type ScriptableObject
// Responsibility: Define a customer archetype's trait ranges and preferences.
//                 Sampled at spawn time to create unique CustomerInstances.
// Dependencies: None — pure data.
// Usage: Create via Assets → Create → DeviceEmpire → Customer Archetype
// ============================================================================

using UnityEngine;
using DeviceEmpire.Inventory;

namespace DeviceEmpire.Customers
{
    /// <summary>
    /// ScriptableObject defining a customer archetype (e.g., "Budget Buyer").
    /// Each archetype specifies trait RANGES — individual customers sample
    /// from these ranges at spawn time, creating natural variation.
    /// 
    /// TRAIT EXPLANATIONS:
    /// - BudgetSensitivity: How price-conscious (1.0 = very cheap, 0.0 = money is no object)
    /// - TechSavviness: Ability to spot defects/value (1.0 = expert, 0.0 = clueless)
    /// - Patience: Willingness to negotiate/wait (1.0 = very patient, 0.0 = walks immediately)
    /// - SocialInfluence: How much they affect/are affected by reputation (V2+)
    /// </summary>
    [CreateAssetMenu(fileName = "Archetype_New", menuName = "DeviceEmpire/Customer Archetype", order = 1)]
    public class CustomerArchetypeData : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────
        [Header("Identity")]
        [Tooltip("Display name for this archetype (shown in customer tooltip)")]
        public string ArchetypeName = "Standard Shopper";

        [Tooltip("Visual representation of this customer type")]
        public Sprite Visual;

        [Tooltip("Color tint for this archetype's UI elements")]
        public Color ArchetypeColor = Color.white;

        [Tooltip("Short description of this customer type")]
        [TextArea(2, 3)]
        public string Description = "A typical customer looking for a good deal.";

        // ── Trait Ranges ───────────────────────────────────────────────────
        // Each trait is a Vector2(min, max) — sampled uniformly at spawn
        [Header("Trait Ranges (min, max — sampled at spawn)")]
        
        [Tooltip("Price sensitivity: 1.0 = only buys cheap, 0.0 = doesn't care about price")]
        public Vector2 BudgetSensitivity = new(0.4f, 0.7f);

        [Tooltip("Technical knowledge: 1.0 = spots all defects, 0.0 = doesn't know phones from laptops")]
        public Vector2 TechSavviness = new(0.1f, 0.4f);

        [Tooltip("Willingness to negotiate: 1.0 = will haggle forever, 0.0 = walks at first overprice")]
        public Vector2 Patience = new(0.3f, 0.7f);

        [Tooltip("Reputation influence: 1.0 = huge word-of-mouth, 0.0 = tells nobody (V2+)")]
        public Vector2 SocialInfluence = new(0.1f, 0.4f);

        // ── Category Preferences ───────────────────────────────────────────
        [Header("Shopping Preferences")]
        [Tooltip("Device categories this archetype is interested in. One is chosen at random on spawn.")]
        public DeviceCategory[] PreferredCategories = { DeviceCategory.Phone };

        [Tooltip("Budget range in dollars — how much this archetype can spend")]
        public Vector2 BudgetRange = new(50f, 200f);

        // ── Spawn Weight ───────────────────────────────────────────────────
        [Header("Spawn Configuration")]
        [Tooltip("Relative spawn weight. Higher = more common. Default = 1.0")]
        [Min(0.1f)]
        public float SpawnWeight = 1.0f;

        // ── Validation ─────────────────────────────────────────────────────
        private void OnValidate()
        {
            // Ensure min <= max for all trait ranges
            ValidateRange(ref BudgetSensitivity, "BudgetSensitivity");
            ValidateRange(ref TechSavviness, "TechSavviness");
            ValidateRange(ref Patience, "Patience");
            ValidateRange(ref SocialInfluence, "SocialInfluence");
            ValidateRange(ref BudgetRange, "BudgetRange");

            if (PreferredCategories == null || PreferredCategories.Length == 0)
            {
                Debug.LogWarning($"[CustomerArchetype] '{ArchetypeName}' has no preferred categories!");
            }
        }

        private void ValidateRange(ref Vector2 range, string name)
        {
            range.x = Mathf.Clamp01(range.x);
            range.y = Mathf.Clamp01(range.y);
            if (range.x > range.y)
            {
                Debug.LogWarning($"[CustomerArchetype] '{ArchetypeName}': {name} min ({range.x}) > max ({range.y}). Swapping.");
                (range.x, range.y) = (range.y, range.x);
            }
        }
    }
}
