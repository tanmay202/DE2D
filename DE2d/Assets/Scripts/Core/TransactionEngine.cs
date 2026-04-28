// ============================================================================
// TransactionEngine.cs — Core Transaction Logic (Phone-Enhanced)
// Responsibility: THE core formula. Price vs WTP → outcome.
//                 Now includes: brand modifiers, spec satisfaction, feature
//                 must-have penalties, color matching, and battery health.
// Dependencies: DeviceInstance, CustomerInstance, PhoneBrandData
// Scene Placement: NONE — static utility class.
// ============================================================================

using UnityEngine;
using DeviceEmpire.Inventory;
using DeviceEmpire.Customers;

namespace DeviceEmpire.Core
{
    /// <summary>
    /// Static transaction evaluation engine — phone-enhanced.
    /// 
    /// V1 ENHANCED WTP FORMULA:
    /// 1. Start with device's current market value (brand-adjusted, depreciated, conditioned)
    /// 2. Apply archetype budget modifier
    /// 3. Apply spec satisfaction modifier (how well specs match customer priorities)
    /// 4. Apply brand match modifier (preferred/disliked brand)
    /// 5. Apply feature must-have penalty (missing required features)
    /// 6. Apply color match modifier
    /// 7. Apply battery health factor
    /// 8. Add random variance (±8%)
    /// 9. Apply tech-savvy defect detection
    /// 10. Cap at customer budget
    /// </summary>
    public static class TransactionEngine
    {
        // ── WTP Calculation ────────────────────────────────────────────────

        /// <summary>
        /// Calculate a customer's Willingness To Pay for a specific device.
        /// Full phone-enhanced formula with spec/brand/feature modifiers.
        /// </summary>
        public static float CalculateWTP(DeviceInstance device, CustomerInstance customer, int currentDay)
        {
            // Step 1: Device's current market value (already includes brand premium & depreciation)
            float baseValue = device.GetCurrentValue(currentDay);

            // Step 2: Archetype budget modifier
            // BudgetSensitivity 1.0 → pays exactly market value
            // BudgetSensitivity 0.0 → pays up to 20% above market
            float budgetModifier = 1f + (0.2f * (1f - customer.BudgetSensitivity));

            // Step 3: Spec satisfaction modifier
            // Good spec match → customer values the device more (up to +25%)
            // Poor spec match → customer values it less (down to -20%)
            float specSatisfaction = customer.CalculateSpecSatisfaction(device.Data);
            float specModifier = 0.8f + (specSatisfaction * 0.45f); // Range: 0.80 to 1.25

            // Step 4: Brand match modifier
            float brandModifier = customer.GetBrandMatchModifier(device.Data.Brand);

            // Step 5: Feature must-have penalty
            float featurePenalty = customer.GetFeatureMustHavePenalty(device.Data);

            // Step 6: Color match modifier
            float colorModifier = customer.GetColorMatchModifier(device);

            // Step 7: Battery health factor (customers care about battery)
            float batteryFactor = 1f;
            if (device.BatteryHealth < 80f)
            {
                // Tech-savvy customers notice and penalize poor battery
                float batteryAwareness = Mathf.Lerp(0.1f, 1f, customer.TechSavviness);
                float healthDeficit = (80f - device.BatteryHealth) / 80f;
                batteryFactor = 1f - (healthDeficit * 0.3f * batteryAwareness);
            }

            // Step 8: Minimum spec check — if device doesn't meet minimums, harsh penalty
            bool meetsMinimums = customer.MeetsMinimumSpecs(device.Data);
            float minimumPenalty = meetsMinimums ? 1f : 0.4f; // 60% WTP reduction if below minimum

            // Step 9: Random variance for unpredictability
            float variance = Random.Range(-0.08f, 0.08f);

            // Step 10: Tech-savvy defect detection
            float defectPenalty = 1f;
            if (device.HasHiddenDefect && customer.TechSavviness > 0.6f)
            {
                defectPenalty = 0.6f + (0.15f * (1f - customer.TechSavviness));
                Debug.Log($"[TransactionEngine] Tech-savvy customer spotted defect! Penalty: {defectPenalty:F2}");
            }

            // Screen scratches detection (easier to spot than hidden defects)
            float scratchPenalty = 1f;
            if (device.HasScreenScratches && customer.TechSavviness > 0.3f)
            {
                scratchPenalty = 0.92f + (0.08f * (1f - customer.TechSavviness));
            }

            // ── COMBINE ALL MODIFIERS ──────────────────────────────────────
            float wtp = baseValue
                      * budgetModifier
                      * specModifier
                      * brandModifier
                      * featurePenalty
                      * colorModifier
                      * batteryFactor
                      * minimumPenalty
                      * (1f + variance)
                      * defectPenalty
                      * scratchPenalty;

            // Cap at customer budget
            wtp = Mathf.Min(wtp, customer.Budget);

            Debug.Log($"[TransactionEngine] WTP Breakdown for '{device.GetFullName()}':\n" +
                      $"  Base Value: ${baseValue:F2}\n" +
                      $"  Budget Mod: ×{budgetModifier:F2}\n" +
                      $"  Spec Match: ×{specModifier:F2} (satisfaction: {specSatisfaction:F2})\n" +
                      $"  Brand Mod:  ×{brandModifier:F2}\n" +
                      $"  Feature:    ×{featurePenalty:F2}\n" +
                      $"  Color:      ×{colorModifier:F2}\n" +
                      $"  Battery:    ×{batteryFactor:F2}\n" +
                      $"  Min Specs:  ×{minimumPenalty:F2}\n" +
                      $"  Defect:     ×{defectPenalty:F2}\n" +
                      $"  Final WTP:  ${wtp:F2}");

            return Mathf.Round(wtp * 100f) / 100f;
        }

        // ── Transaction Evaluation ─────────────────────────────────────────

        /// <summary>
        /// Evaluate a transaction: compare asking price to WTP → determine outcome.
        /// Same threshold logic as before, now with spec-aware feedback.
        /// </summary>
        public static TransactionResult Evaluate(float askingPrice, float wtp, CustomerInstance customer,
                                                  DeviceInstance device = null)
        {
            if (wtp <= 0f)
            {
                return new TransactionResult(TransactionOutcome.CustomerLeft, askingPrice, 0f,
                    "\"I can't afford anything here.\"");
            }

            float ratio = askingPrice / wtp;

            // ── ZONE 1: Heavily Underpriced (ratio ≤ 0.85) ────────────────
            if (ratio <= 0.85f)
            {
                string feedback = GetUnderpricedFeedback(ratio, device);
                return new TransactionResult(TransactionOutcome.ImmediateAccept, askingPrice, 0f, feedback);
            }

            // ── ZONE 2: Fair Price / Negotiation (0.85 < ratio ≤ 1.15) ───
            if (ratio <= 1.15f)
            {
                float counterMin = wtp * 0.90f;
                float counterMax = wtp * 1.05f;
                float counterOffer = Mathf.Round(Random.Range(counterMin, counterMax) * 100f) / 100f;

                string feedback = GetNegotiationFeedback(ratio, device, customer);
                return new TransactionResult(TransactionOutcome.NegotiationOpened, askingPrice, counterOffer, feedback);
            }

            // ── ZONE 3: Overpriced (ratio > 1.15) ─────────────────────────
            float leaveChance = Mathf.Clamp01((ratio - 1.15f) * 2f) * (1f - customer.Patience * 0.5f);

            if (Random.value < leaveChance)
            {
                string feedback = GetWalkAwayFeedback(ratio, device, customer);
                return new TransactionResult(TransactionOutcome.CustomerLeft, askingPrice, 0f, feedback);
            }

            // Hard haggle
            float hardCounterMin = wtp * 0.85f;
            float hardCounterMax = wtp * 0.95f;
            float hardCounter = Mathf.Round(Random.Range(hardCounterMin, hardCounterMax) * 100f) / 100f;

            string haggleFeedback = GetHardHaggleFeedback(ratio, device, customer);
            return new TransactionResult(TransactionOutcome.HardHaggle, askingPrice, hardCounter, haggleFeedback);
        }

        // ── Quick Assessment ───────────────────────────────────────────────

        public static PriceAssessment AssessPrice(float askingPrice, DeviceInstance device, int currentDay)
        {
            float marketValue = device.GetCurrentValue(currentDay);
            if (marketValue <= 0f) return PriceAssessment.Unknown;

            float ratio = askingPrice / marketValue;

            if (ratio <= 0.70f) return PriceAssessment.WayUnderpriced;
            if (ratio <= 0.90f) return PriceAssessment.Underpriced;
            if (ratio <= 1.20f) return PriceAssessment.FairPrice;
            if (ratio <= 1.50f) return PriceAssessment.Overpriced;
            return PriceAssessment.WayOverpriced;
        }

        public static Color GetAssessmentColor(PriceAssessment assessment)
        {
            return assessment switch
            {
                PriceAssessment.WayUnderpriced => new Color(0.2f, 0.6f, 1f),
                PriceAssessment.Underpriced    => new Color(0.3f, 0.8f, 0.4f),
                PriceAssessment.FairPrice      => new Color(0.4f, 0.9f, 0.3f),
                PriceAssessment.Overpriced     => new Color(1f, 0.8f, 0.2f),
                PriceAssessment.WayOverpriced  => new Color(1f, 0.3f, 0.2f),
                _ => Color.gray
            };
        }

        // ── Phone-Aware Feedback Strings ──────────────────────────────────

        private static string GetUnderpricedFeedback(float ratio, DeviceInstance device)
        {
            string specNote = device != null ? $" for this {device.Data.GetSpecTier()}" : "";
            if (ratio <= 0.5f)
                return $"\"Wow, ${device?.PlayerSetPrice:F2}{specNote}?! SOLD!\" (Basically giving it away...)";
            if (ratio <= 0.7f)
                return $"\"This is a steal{specNote}! I'll take it!\" (Could have charged more.)";
            return $"\"Great price{specNote}! Deal!\" (Quick sale, slightly below market.)";
        }

        private static string GetNegotiationFeedback(float ratio, DeviceInstance device, CustomerInstance customer)
        {
            string brandNote = "";
            if (device?.Data.Brand != null && customer?.PreferredBrand != null)
            {
                if (device.Data.Brand == customer.PreferredBrand)
                    brandNote = $" I do like {device.Data.Brand.BrandName} though...";
                else if (device.Data.Brand == customer.DislikedBrand)
                    brandNote = $" Not my favorite brand honestly.";
            }

            if (ratio <= 0.95f)
                return $"\"Hmm, that's reasonable.{brandNote} How about...\"";
            if (ratio <= 1.05f)
                return $"\"Fair enough, but can you come down a bit?{brandNote}\"";
            return $"\"That's a bit high...{brandNote} I was thinking more like...\"";
        }

        private static string GetWalkAwayFeedback(float ratio, DeviceInstance device, CustomerInstance customer)
        {
            // Spec-aware walk-away reasons
            if (device != null && customer != null)
            {
                float specMatch = customer.CalculateSpecSatisfaction(device.Data);
                if (specMatch < 0.3f)
                    return "\"These specs aren't what I'm looking for, AND the price is too high. Bye!\"";

                if (!customer.MeetsMinimumSpecs(device.Data))
                    return "\"This doesn't even have the basics I need. No thanks.\"";

                float featurePenalty = customer.GetFeatureMustHavePenalty(device.Data);
                if (featurePenalty < 0.7f)
                    return "\"It's missing features I need, and you want THAT much? I'm out.\"";
            }

            if (ratio >= 2.0f)
                return "\"Are you serious?! I'm out of here!\" (Customer stormed off.)";
            if (ratio >= 1.5f)
                return "\"Way too expensive. I'll shop elsewhere.\" (Customer left.)";
            return "\"Sorry, that's more than I want to spend. Goodbye.\" (Customer left.)";
        }

        private static string GetHardHaggleFeedback(float ratio, DeviceInstance device, CustomerInstance customer)
        {
            string specNote = "";
            if (device != null)
            {
                var specs = device.Data.Specs;
                if (device.BatteryHealth < 75f)
                    specNote = " The battery isn't great either.";
                else if (device.HasScreenScratches)
                    specNote = " Plus, there are scratches on the screen.";
            }

            if (ratio >= 1.5f)
                return $"\"Look, I'll give you this much, take it or leave it.{specNote}\"";
            return $"\"That's too high for me, but I'd pay...{specNote}\"";
        }
    }

    // ── Transaction Result ─────────────────────────────────────────────────

    public class TransactionResult
    {
        public TransactionOutcome Outcome;
        public float AskingPrice;
        public float CustomerCounterOffer;
        public string Feedback;

        public TransactionResult(TransactionOutcome outcome, float askingPrice, float counterOffer, string feedback = "")
        {
            Outcome = outcome;
            AskingPrice = askingPrice;
            CustomerCounterOffer = counterOffer;
            Feedback = feedback;
        }

        public bool IsImmediateSale => Outcome == TransactionOutcome.ImmediateAccept;
        public bool RequiresPlayerResponse => Outcome == TransactionOutcome.NegotiationOpened ||
                                              Outcome == TransactionOutcome.HardHaggle;
        public bool CustomerLeftEmpty => Outcome == TransactionOutcome.CustomerLeft;

        public override string ToString()
        {
            return $"[Transaction] {Outcome}: Asked ${AskingPrice:F2}, " +
                   $"Counter: ${CustomerCounterOffer:F2} — {Feedback}";
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    public enum TransactionOutcome
    {
        ImmediateAccept,
        NegotiationOpened,
        HardHaggle,
        CustomerLeft
    }

    public enum PriceAssessment
    {
        Unknown,
        WayUnderpriced,
        Underpriced,
        FairPrice,
        Overpriced,
        WayOverpriced
    }
}
