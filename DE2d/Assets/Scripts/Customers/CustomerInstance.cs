// ============================================================================
// CustomerInstance.cs — Runtime Customer Instance (Phone-Enhanced)
// Responsibility: A single customer visiting the shop. Holds sampled traits,
//                 phone spec preferences, brand affinity, feature requirements,
//                 and spec satisfaction scoring.
// Dependencies: CustomerArchetypeData, DeviceData, PhoneBrandData
// Scene Placement: None — plain C# class.
// ============================================================================

using System.Linq;
using UnityEngine;
using DeviceEmpire.Inventory;

namespace DeviceEmpire.Customers
{
    /// <summary>
    /// A single customer with phone-aware purchasing behavior.
    /// Sampled traits + spec preferences determine WTP for specific devices.
    /// </summary>
    [System.Serializable]
    public class CustomerInstance
    {
        // ── Identity ───────────────────────────────────────────────────────
        public CustomerArchetypeData Archetype;
        public DeviceCategory WantedCategory;

        // ── Core Traits (sampled at spawn) ─────────────────────────────────
        public float BudgetSensitivity;
        public float TechSavviness;
        public float Patience;
        public float SocialInfluence;
        public float Budget;

        // ── Phone-Specific Traits (sampled at spawn) ───────────────────────
        /// <summary>How much this customer cares about brands (0=blind, 1=obsessed).</summary>
        public float BrandAffinity;

        /// <summary>Preferred brand for this visit (null = no preference).</summary>
        public PhoneBrandData PreferredBrand;

        /// <summary>Disliked brand for this visit (null = none).</summary>
        public PhoneBrandData DislikedBrand;

        // ── Spec Priority Weights (sampled with variance from archetype) ───
        public float PriorityPerformance;
        public float PriorityCamera;
        public float PriorityBattery;
        public float PriorityDisplay;
        public float PriorityMemory;
        public float PriorityBuildQuality;
        public float PriorityConnectivity;

        // ── Preferred Color (sampled from archetype) ───────────────────────
        public string PreferredColor;
        public float ColorPreference;

        // ── State ──────────────────────────────────────────────────────────
        public CustomerState State = CustomerState.Waiting;
        public int NegotiationRounds = 0;
        public int MaxNegotiationRounds;

        // ── Unique ID ──────────────────────────────────────────────────────
        private static int _nextId = 0;
        public int CustomerId { get; private set; }

        // ── Factory Method ─────────────────────────────────────────────────

        /// <summary>
        /// Spawn a new customer from an archetype. Rolls all traits from archetype ranges.
        /// </summary>
        public static CustomerInstance Spawn(CustomerArchetypeData archetype)
        {
            var customer = new CustomerInstance
            {
                Archetype = archetype,
                CustomerId = _nextId++,
                State = CustomerState.Waiting,
                NegotiationRounds = 0
            };

            // ── Sample core traits ─────────────────────────────────────────
            customer.BudgetSensitivity = Random.Range(archetype.BudgetSensitivity.x, archetype.BudgetSensitivity.y);
            customer.TechSavviness     = Random.Range(archetype.TechSavviness.x, archetype.TechSavviness.y);
            customer.Patience          = Random.Range(archetype.Patience.x, archetype.Patience.y);
            customer.SocialInfluence   = Random.Range(archetype.SocialInfluence.x, archetype.SocialInfluence.y);
            customer.Budget            = Random.Range(archetype.BudgetRange.x, archetype.BudgetRange.y);

            // ── Pick wanted category ───────────────────────────────────────
            if (archetype.PreferredCategories != null && archetype.PreferredCategories.Length > 0)
                customer.WantedCategory = archetype.PreferredCategories[Random.Range(0, archetype.PreferredCategories.Length)];
            else
                customer.WantedCategory = DeviceCategory.Phone;

            // ── Sample brand affinity ──────────────────────────────────────
            customer.BrandAffinity = Random.Range(archetype.BrandAffinity.x, archetype.BrandAffinity.y);

            // Pick a preferred brand (random from list, or null)
            if (archetype.PreferredBrands != null && archetype.PreferredBrands.Length > 0)
                customer.PreferredBrand = archetype.PreferredBrands[Random.Range(0, archetype.PreferredBrands.Length)];

            // Pick a disliked brand
            if (archetype.DislikedBrands != null && archetype.DislikedBrands.Length > 0)
                customer.DislikedBrand = archetype.DislikedBrands[Random.Range(0, archetype.DislikedBrands.Length)];

            // ── Sample spec priorities (archetype value ± 15% variance) ────
            customer.PriorityPerformance  = SamplePriority(archetype.PriorityPerformance);
            customer.PriorityCamera       = SamplePriority(archetype.PriorityCamera);
            customer.PriorityBattery      = SamplePriority(archetype.PriorityBattery);
            customer.PriorityDisplay      = SamplePriority(archetype.PriorityDisplay);
            customer.PriorityMemory       = SamplePriority(archetype.PriorityMemory);
            customer.PriorityBuildQuality = SamplePriority(archetype.PriorityBuildQuality);
            customer.PriorityConnectivity = SamplePriority(archetype.PriorityConnectivity);

            // ── Sample color preference ────────────────────────────────────
            customer.ColorPreference = archetype.ColorPreference;
            if (archetype.PreferredColorNames != null && archetype.PreferredColorNames.Length > 0)
                customer.PreferredColor = archetype.PreferredColorNames[Random.Range(0, archetype.PreferredColorNames.Length)];

            // ── Negotiation rounds ─────────────────────────────────────────
            customer.MaxNegotiationRounds = Mathf.RoundToInt(1f + customer.Patience * 2f);

            Debug.Log($"[Customer] Spawned #{customer.CustomerId} '{archetype.ArchetypeName}' " +
                      $"wants: {customer.WantedCategory}, budget: ${customer.Budget:F2}, " +
                      $"brand affinity: {customer.BrandAffinity:F2}, " +
                      $"preferred brand: {customer.PreferredBrand?.BrandName ?? "any"}");

            return customer;
        }

        // ── Spec Satisfaction Scoring ──────────────────────────────────────

        /// <summary>
        /// Calculate how well a device matches this customer's spec preferences.
        /// Returns 0.0 (terrible match) to 1.0+ (exceeds expectations).
        /// 
        /// This factors into WTP: a device that matches well → higher WTP.
        /// </summary>
        public float CalculateSpecSatisfaction(DeviceData device)
        {
            if (device.Category != DeviceCategory.Phone)
                return 0.5f; // Non-phones get neutral satisfaction

            var s = device.Specs;
            float totalWeight = 0f;
            float weightedScore = 0f;

            // ── Performance ────────────────────────────────────────────────
            if (PriorityPerformance > 0.01f)
            {
                float perfScore = Mathf.Clamp01(s.PerformanceScore / 80f); // 80 = "great"
                weightedScore += perfScore * PriorityPerformance;
                totalWeight += PriorityPerformance;
            }

            // ── Camera ─────────────────────────────────────────────────────
            if (PriorityCamera > 0.01f)
            {
                float camScore = Mathf.Clamp01(s.CameraQualityScore / 80f);
                weightedScore += camScore * PriorityCamera;
                totalWeight += PriorityCamera;
            }

            // ── Battery ────────────────────────────────────────────────────
            if (PriorityBattery > 0.01f)
            {
                float battScore = Mathf.Clamp01((s.BatteryCapacity_mAh - 2000f) / 3000f);
                weightedScore += battScore * PriorityBattery;
                totalWeight += PriorityBattery;
            }

            // ── Display ────────────────────────────────────────────────────
            if (PriorityDisplay > 0.01f)
            {
                float displayTypeScore = s.DisplayType switch
                {
                    DisplayType.LCD => 0.2f,
                    DisplayType.IPS_LCD => 0.4f,
                    DisplayType.OLED => 0.65f,
                    DisplayType.AMOLED => 0.8f,
                    DisplayType.Super_AMOLED => 0.9f,
                    DisplayType.LTPO_AMOLED => 1.0f,
                    _ => 0.3f
                };
                float refreshScore = Mathf.Clamp01((s.RefreshRate_Hz - 60f) / 60f);
                float dispScore = (displayTypeScore * 0.6f) + (refreshScore * 0.4f);
                weightedScore += dispScore * PriorityDisplay;
                totalWeight += PriorityDisplay;
            }

            // ── Memory ─────────────────────────────────────────────────────
            if (PriorityMemory > 0.01f)
            {
                float ramScore = Mathf.Clamp01((s.RAM_GB - 2f) / 10f);
                float storageScore = Mathf.Clamp01((s.Storage_GB - 32f) / 224f);
                float memScore = (ramScore * 0.5f) + (storageScore * 0.5f);
                weightedScore += memScore * PriorityMemory;
                totalWeight += PriorityMemory;
            }

            // ── Build Quality ──────────────────────────────────────────────
            if (PriorityBuildQuality > 0.01f)
            {
                float materialScore = s.BackMaterial switch
                {
                    BackMaterial.Plastic => 0.2f,
                    BackMaterial.Polycarbonate => 0.3f,
                    BackMaterial.GlasticSamsung => 0.4f,
                    BackMaterial.Glass => 0.6f,
                    BackMaterial.Ceramic => 0.85f,
                    BackMaterial.Vegan_Leather => 0.7f,
                    BackMaterial.Metal => 0.75f,
                    BackMaterial.Kevlar => 0.8f,
                    _ => 0.3f
                };
                float ipScore = s.IPRating switch
                {
                    IPRating.None => 0f,
                    IPRating.IP52 => 0.2f,
                    IPRating.IP53 => 0.3f,
                    IPRating.IP54 => 0.35f,
                    IPRating.IP65 => 0.5f,
                    IPRating.IP67 => 0.75f,
                    IPRating.IP68 => 0.9f,
                    IPRating.IP69 => 1.0f,
                    _ => 0f
                };
                float buildScore = (materialScore * 0.5f) + (ipScore * 0.5f);
                weightedScore += buildScore * PriorityBuildQuality;
                totalWeight += PriorityBuildQuality;
            }

            // ── Connectivity ───────────────────────────────────────────────
            if (PriorityConnectivity > 0.01f)
            {
                float connScore = 0f;
                if (s.Has5G) connScore += 0.35f;
                if (s.HasNFC) connScore += 0.25f;
                connScore += s.WiFiStandard switch
                {
                    WiFiStandard.WiFi_4 => 0.05f,
                    WiFiStandard.WiFi_5 => 0.1f,
                    WiFiStandard.WiFi_6 => 0.2f,
                    WiFiStandard.WiFi_6E => 0.3f,
                    WiFiStandard.WiFi_7 => 0.4f,
                    _ => 0.05f
                };
                weightedScore += Mathf.Clamp01(connScore) * PriorityConnectivity;
                totalWeight += PriorityConnectivity;
            }

            return totalWeight > 0f ? weightedScore / totalWeight : 0.5f;
        }

        /// <summary>
        /// Check if a device meets this customer's minimum spec requirements.
        /// Returns true if all minimums are met, false if any are violated.
        /// </summary>
        public bool MeetsMinimumSpecs(DeviceData device)
        {
            if (device.Category != DeviceCategory.Phone) return true;
            if (Archetype == null) return true;

            var s = device.Specs;

            if (Archetype.MinRAM_GB > 0 && s.RAM_GB < Archetype.MinRAM_GB) return false;
            if (Archetype.MinStorage_GB > 0 && s.Storage_GB < Archetype.MinStorage_GB) return false;
            if (Archetype.MinBattery_mAh > 0 && s.BatteryCapacity_mAh < Archetype.MinBattery_mAh) return false;
            if (Archetype.MinCamera_MP > 0 && s.MainCamera_MP < Archetype.MinCamera_MP) return false;
            if (Archetype.MinPerformanceScore > 0 && s.PerformanceScore < Archetype.MinPerformanceScore) return false;

            return true;
        }

        /// <summary>
        /// Check if a device has all feature must-haves.
        /// Returns a penalty multiplier: 1.0 = all met, lower = missing features.
        /// </summary>
        public float GetFeatureMustHavePenalty(DeviceData device)
        {
            if (device.Category != DeviceCategory.Phone) return 1f;
            if (Archetype == null) return 1f;

            var s = device.Specs;
            float penalty = 1f;

            if (Archetype.Requires5G && !s.Has5G) penalty *= 0.5f;
            if (Archetype.RequiresNFC && !s.HasNFC) penalty *= 0.7f;
            if (Archetype.RequiresHeadphoneJack && !s.HasHeadphoneJack) penalty *= 0.8f;
            if (Archetype.RequiresExpandableStorage && !s.HasExpandableStorage) penalty *= 0.75f;
            if (Archetype.RequiresWirelessCharging && !s.HasWirelessCharging) penalty *= 0.7f;
            if (Archetype.RequiresWaterResistance && s.IPRating == IPRating.None) penalty *= 0.6f;

            if (Archetype.RequiresAMOLED)
            {
                bool hasAMOLED = s.DisplayType == DisplayType.AMOLED ||
                                 s.DisplayType == DisplayType.Super_AMOLED ||
                                 s.DisplayType == DisplayType.LTPO_AMOLED;
                if (!hasAMOLED) penalty *= 0.65f;
            }

            return penalty;
        }

        /// <summary>
        /// Calculate brand match modifier.
        /// Returns > 1.0 for preferred brand, < 1.0 for disliked, 1.0 for neutral.
        /// </summary>
        public float GetBrandMatchModifier(PhoneBrandData deviceBrand)
        {
            if (deviceBrand == null || BrandAffinity < 0.05f) return 1f;

            float modifier = 1f;

            // Preferred brand bonus
            if (PreferredBrand != null && deviceBrand == PreferredBrand)
                modifier += 0.15f * BrandAffinity; // Up to +15% for brand fans

            // Disliked brand penalty
            if (DislikedBrand != null && deviceBrand == DislikedBrand)
                modifier -= 0.2f * BrandAffinity; // Up to -20% for disliked brands

            // General brand value (popular trusted brands are worth more)
            modifier *= deviceBrand.GetCustomerBrandWTPModifier(BrandAffinity);

            return Mathf.Max(0.5f, modifier); // Floor at 50%
        }

        /// <summary>
        /// Calculate color match modifier.
        /// Returns > 1.0 for preferred color, 1.0 for neutral.
        /// </summary>
        public float GetColorMatchModifier(DeviceInstance device)
        {
            if (ColorPreference < 0.05f || string.IsNullOrEmpty(PreferredColor))
                return 1f;

            string deviceColor = device.GetColorName();
            if (string.IsNullOrEmpty(deviceColor)) return 1f;

            bool match = deviceColor.ToLower().Contains(PreferredColor.ToLower());
            if (match)
                return 1f + (0.05f * ColorPreference); // Small bonus for matching color
            else
                return 1f - (0.03f * ColorPreference); // Tiny penalty for wrong color
        }

        // ── State Transitions ──────────────────────────────────────────────
        public void StartServing() => State = CustomerState.BeingServed;
        public void MarkSatisfied() => State = CustomerState.Satisfied;
        public void MarkDissatisfied() => State = CustomerState.Dissatisfied;
        public void MarkLeft() => State = CustomerState.Left;

        public bool IncrementNegotiation()
        {
            NegotiationRounds++;
            return NegotiationRounds < MaxNegotiationRounds;
        }

        // ── Queries ────────────────────────────────────────────────────────
        public bool CanAfford(float price) => Budget >= price;
        public bool IsOutOfPatience => NegotiationRounds >= MaxNegotiationRounds;

        public string GetMoodEmoji()
        {
            return State switch
            {
                CustomerState.Waiting      => "🕐",
                CustomerState.BeingServed  => "🤔",
                CustomerState.Satisfied    => "😊",
                CustomerState.Dissatisfied => "😤",
                CustomerState.Left         => "🚶",
                _ => "❓"
            };
        }

        public string GetDisplayName() => Archetype != null ? Archetype.ArchetypeName : "Unknown Customer";

        /// <summary>Get what this customer is looking for as a string.</summary>
        public string GetWantsSummary()
        {
            string wants = $"Wants: {WantedCategory}";
            if (PreferredBrand != null)
                wants += $" (prefers {PreferredBrand.BrandName})";

            // Find top priority
            float maxPriority = Mathf.Max(PriorityPerformance, PriorityCamera, PriorityBattery,
                                          PriorityDisplay, PriorityMemory);
            if (maxPriority == PriorityCamera && PriorityCamera > 0.5f) wants += " 📷";
            else if (maxPriority == PriorityPerformance && PriorityPerformance > 0.5f) wants += " ⚡";
            else if (maxPriority == PriorityBattery && PriorityBattery > 0.5f) wants += " 🔋";
            else if (maxPriority == PriorityDisplay && PriorityDisplay > 0.5f) wants += " 📱";

            return wants;
        }

        public override string ToString()
        {
            return $"[Customer #{CustomerId}] {GetDisplayName()} | " +
                   $"Wants: {WantedCategory} | Budget: ${Budget:F2} | " +
                   $"Brand: {PreferredBrand?.BrandName ?? "any"} | State: {State}";
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private static float SamplePriority(float archetypeValue)
        {
            float variance = Random.Range(-0.15f, 0.15f);
            return Mathf.Clamp01(archetypeValue + variance);
        }
    }

    // ── Customer State Enum ────────────────────────────────────────────────
    public enum CustomerState
    {
        Waiting,
        BeingServed,
        Satisfied,
        Dissatisfied,
        Left
    }
}
