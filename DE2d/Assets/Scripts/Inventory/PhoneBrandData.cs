// ============================================================================
// PhoneBrandData.cs — Phone Brand ScriptableObject
// Responsibility: Defines a phone manufacturer/brand with popularity, trust,
//                 and market tier. Brands affect customer WTP and preference.
// Dependencies: None — pure data.
// Usage: Create via Assets → Create → DeviceEmpire → Phone Brand
// ============================================================================

using UnityEngine;

namespace DeviceEmpire.Inventory
{
    /// <summary>
    /// ScriptableObject defining a phone brand (e.g., "Starphone" → Samsung-like).
    /// Brands have market presence, trust ratings, and target demographics.
    /// 
    /// DESIGN DECISION: Fictional brand names to avoid legal issues while
    /// maintaining clear real-world parallels for intuitive gameplay.
    /// 
    /// BRAND TIERS:
    /// - Premium:  High wholesale, high margins, brand-loyal customers (Apple/Samsung-like)
    /// - MidRange: Moderate prices, good value proposition (OnePlus/Pixel-like)
    /// - Budget:   Low wholesale, thin margins, price-sensitive buyers (Xiaomi/Realme-like)
    /// - Generic:  No-name brands, cheapest, some quality concerns
    /// </summary>
    [CreateAssetMenu(fileName = "Brand_New", menuName = "DeviceEmpire/Phone Brand", order = 2)]
    public class PhoneBrandData : ScriptableObject
    {
        // ── Identity ───────────────────────────────────────────────────────
        [Header("Brand Identity")]
        [Tooltip("Brand name shown to the player (use fictional names)")]
        public string BrandName = "NewBrand";

        [Tooltip("Brand logo/icon for UI display")]
        public Sprite BrandLogo;

        [Tooltip("Brand color for UI theming")]
        public Color BrandColor = Color.white;

        [Tooltip("Short tagline (e.g., 'Innovation for Everyone')")]
        public string Tagline = "Quality devices.";

        [Tooltip("Country of origin (flavor text)")]
        public string CountryOfOrigin = "Unknown";

        // ── Market Position ────────────────────────────────────────────────
        [Header("Market Position")]
        [Tooltip("Brand tier — affects pricing baseline and customer expectations")]
        public BrandTier Tier = BrandTier.MidRange;

        [Tooltip("Brand popularity: 0.0 = unknown, 1.0 = household name. " +
                 "Affects customer recognition and WTP.")]
        [Range(0f, 1f)]
        public float Popularity = 0.5f;

        [Tooltip("Consumer trust/reliability: 0.0 = unreliable, 1.0 = bulletproof. " +
                 "High trust = customers pay premium, low trust = needs discounting.")]
        [Range(0f, 1f)]
        public float TrustRating = 0.5f;

        [Tooltip("After-sales support quality: 0.0 = nonexistent, 1.0 = excellent. " +
                 "Affects customer satisfaction and return rate (V2+).")]
        [Range(0f, 1f)]
        public float ServiceQuality = 0.5f;

        // ── Economics ──────────────────────────────────────────────────────
        [Header("Brand Economics")]
        [Tooltip("Price premium multiplier. 1.0 = no premium. " +
                 "Premium brands (1.3+) cost more wholesale but sell for more.")]
        [Range(0.7f, 2.0f)]
        public float PricePremiumMultiplier = 1.0f;

        [Tooltip("How fast this brand's devices depreciate. " +
                 "1.0 = normal, 0.5 = holds value well, 2.0 = drops fast.")]
        [Range(0.3f, 3.0f)]
        public float DepreciationMultiplier = 1.0f;

        [Tooltip("Demand multiplier — how many customers specifically seek this brand. " +
                 "1.0 = average, 2.0 = high demand, 0.5 = niche.")]
        [Range(0.2f, 3.0f)]
        public float DemandMultiplier = 1.0f;

        // ── Resale Value ───────────────────────────────────────────────────
        [Header("Resale Characteristics")]
        [Tooltip("How well used devices of this brand hold value. " +
                 "0.0 = drops like a rock, 1.0 = holds value extremely well.")]
        [Range(0f, 1f)]
        public float ResaleValueRetention = 0.5f;

        [Tooltip("Likelihood of defects in used devices of this brand. " +
                 "0.0 = rock solid, 1.0 = very defect prone.")]
        [Range(0f, 1f)]
        public float DefectRate = 0.1f;

        // ── Computed Properties ────────────────────────────────────────────

        /// <summary>
        /// Calculate the brand value modifier applied to a device's base price.
        /// Combines popularity, trust, and tier premium.
        /// 
        /// Formula: PricePremium × (0.8 + 0.2 × Popularity) × (0.9 + 0.1 × Trust)
        /// 
        /// Results:
        /// - Premium popular trusted brand: ~1.5-2.0x value
        /// - Budget unknown untrusted brand: ~0.5-0.7x value
        /// - Mid-range average brand: ~1.0x value
        /// </summary>
        public float GetBrandValueModifier()
        {
            float popFactor = 0.8f + (0.2f * Popularity);
            float trustFactor = 0.9f + (0.1f * TrustRating);
            return PricePremiumMultiplier * popFactor * trustFactor;
        }

        /// <summary>
        /// Get a customer WTP modifier based on brand affinity.
        /// Brand-conscious customers pay more for popular brands.
        /// </summary>
        /// <param name="customerBrandAffinity">How much the customer cares about brands (0-1)</param>
        public float GetCustomerBrandWTPModifier(float customerBrandAffinity)
        {
            // Brand-indifferent customers: modifier ≈ 1.0
            // Brand-conscious customers: modifier scales with popularity
            float brandEffect = Mathf.Lerp(1f, GetBrandValueModifier(), customerBrandAffinity);
            return brandEffect;
        }

        /// <summary>
        /// Get display string for brand tier.
        /// </summary>
        public string GetTierLabel()
        {
            return Tier switch
            {
                BrandTier.Premium  => "<color=#FFD700>PREMIUM</color>",
                BrandTier.MidRange => "<color=#4FC3F7>MID-RANGE</color>",
                BrandTier.Budget   => "<color=#81C784>BUDGET</color>",
                BrandTier.Generic  => "<color=#9E9E9E>GENERIC</color>",
                _ => "UNKNOWN"
            };
        }

        // ── Validation ─────────────────────────────────────────────────────
        private void OnValidate()
        {
            // Auto-set premium multiplier based on tier if it looks default
            if (PricePremiumMultiplier == 1.0f)
            {
                PricePremiumMultiplier = Tier switch
                {
                    BrandTier.Premium  => 1.5f,
                    BrandTier.MidRange => 1.0f,
                    BrandTier.Budget   => 0.8f,
                    BrandTier.Generic  => 0.6f,
                    _ => 1.0f
                };
            }
        }
    }

    // ── Brand Tier Enum ────────────────────────────────────────────────────

    /// <summary>
    /// Market tier of a phone brand. Determines pricing baseline,
    /// customer expectations, and target demographic.
    /// </summary>
    public enum BrandTier
    {
        Premium,    // Apple/Samsung-tier: expensive, high margins, loyal fans
        MidRange,   // OnePlus/Pixel-tier: balanced price/quality
        Budget,     // Xiaomi/Realme-tier: aggressive pricing, value-focused
        Generic     // No-name/white-label: cheapest, questionable quality
    }
}
