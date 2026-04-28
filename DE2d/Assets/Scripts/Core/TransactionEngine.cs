// ============================================================================
// TransactionEngine.cs — Core Transaction Logic
// Responsibility: THE core formula. Price vs Willingness-To-Pay → outcome.
//                 Pure static logic — no MonoBehaviour, no scene placement.
//                 This is the HEART of the game feel. Tune carefully.
// Dependencies: DeviceInstance, CustomerInstance
// Scene Placement: NONE — static utility class. Called by ShopController.
// Testing: Pure functions → ideal for unit tests.
// ============================================================================

using UnityEngine;
using DeviceEmpire.Inventory;
using DeviceEmpire.Customers;

namespace DeviceEmpire.Core
{
    /// <summary>
    /// Static transaction evaluation engine. Given a device, customer, and asking price,
    /// calculates the customer's Willingness To Pay (WTP) and determines the outcome.
    /// 
    /// V1 FORMULA BREAKDOWN:
    /// 1. Start with device's current market value (depreciated + condition)
    /// 2. Apply archetype modifier (budget buyers → lower WTP, enthusiasts → higher)
    /// 3. Add random variance (±8%) for unpredictability
    /// 4. Compare asking price to WTP → determine outcome
    /// 
    /// OUTCOME THRESHOLDS:
    /// - askingPrice <= 85% of WTP → Immediate Accept (player left money on the table)
    /// - askingPrice <= 115% of WTP → Negotiation (customer counters near WTP)
    /// - askingPrice > 115% of WTP → Hard Haggle or Walk Away (based on patience)
    /// </summary>
    public static class TransactionEngine
    {
        // ── WTP Calculation ────────────────────────────────────────────────

        /// <summary>
        /// Calculate a customer's Willingness To Pay for a specific device.
        /// This is the maximum amount the customer would consider paying.
        /// 
        /// THE FORMULA (V1 — expanded in V2 with reputation/brand):
        ///   WTP = DeviceCurrentValue × ArchetypeModifier × (1 + RandomVariance)
        /// 
        /// Where:
        ///   ArchetypeModifier = 1.0 + 0.2 × (1.0 - BudgetSensitivity)
        ///     → Budget buyers (sensitivity=1.0): modifier = 1.0 (pays market value)
        ///     → Big spenders (sensitivity=0.0): modifier = 1.2 (pays 20% above market)
        ///   RandomVariance = uniform random in [-0.08, +0.08]
        ///     → Creates natural price uncertainty so the player can't perfectly predict
        /// </summary>
        /// <param name="device">The device being considered</param>
        /// <param name="customer">The customer evaluating the device</param>
        /// <param name="currentDay">Current in-game day (for depreciation calc)</param>
        /// <returns>Maximum price the customer would consider</returns>
        public static float CalculateWTP(DeviceInstance device, CustomerInstance customer, int currentDay)
        {
            // Step 1: Get the device's current market value
            float baseValue = device.GetCurrentValue(currentDay);

            // Step 2: Archetype modifier — how much above/below market this customer pays
            // BudgetSensitivity 1.0 → pays exactly market value
            // BudgetSensitivity 0.0 → pays up to 20% above market
            float archetypeModifier = 1f + (0.2f * (1f - customer.BudgetSensitivity));

            // Step 3: Random variance for unpredictability
            float variance = Random.Range(-0.08f, 0.08f);

            // Step 4: Tech-savvy customers detect hidden defects → reduces WTP
            float defectPenalty = 1f;
            if (device.HasHiddenDefect && customer.TechSavviness > 0.6f)
            {
                // High tech savviness → spots the defect → WTP drops 25-40%
                defectPenalty = 0.6f + (0.15f * (1f - customer.TechSavviness));
                Debug.Log($"[TransactionEngine] Tech-savvy customer spotted hidden defect! " +
                          $"WTP penalty: {defectPenalty:F2}");
            }

            // Step 5: Budget cap — customer can't pay more than their budget
            float wtp = baseValue * archetypeModifier * (1f + variance) * defectPenalty;
            wtp = Mathf.Min(wtp, customer.Budget);

            return Mathf.Round(wtp * 100f) / 100f; // Round to cents
        }

        // ── Transaction Evaluation ─────────────────────────────────────────

        /// <summary>
        /// Evaluate a transaction: compare asking price to WTP and determine outcome.
        /// 
        /// OUTCOME LOGIC:
        /// 
        /// ratio = askingPrice / WTP
        /// 
        /// ratio ≤ 0.85  → IMMEDIATE ACCEPT
        ///   Customer grabs the deal instantly. Player underpriced.
        ///   Feedback: "Wow, what a deal!" (teaches player they left money on the table)
        /// 
        /// ratio ≤ 1.15  → NEGOTIATION OPENED
        ///   Normal haggling zone. Customer makes a counter-offer near their WTP.
        ///   Counter = random(WTP × 0.90, WTP × 1.05)
        ///   This is the "skill zone" — pricing here maximizes profit.
        /// 
        /// ratio > 1.15  → OVERPRICE ZONE
        ///   Customer may walk away or hard-haggle.
        ///   Walk chance = (ratio - 1.15) × 2.0 × (1 - patience × 0.5)
        ///   If they stay: counter = WTP × random(0.88, 0.95) (lowball)
        /// </summary>
        /// <param name="askingPrice">The price the player set</param>
        /// <param name="wtp">Customer's willingness to pay (from CalculateWTP)</param>
        /// <param name="customer">The customer (for patience checks)</param>
        /// <returns>TransactionResult with outcome and counter-offer</returns>
        public static TransactionResult Evaluate(float askingPrice, float wtp, CustomerInstance customer)
        {
            // Prevent division by zero
            if (wtp <= 0f)
            {
                return new TransactionResult(TransactionOutcome.CustomerLeft, askingPrice, 0f, 
                    "Customer can't afford anything.");
            }

            float ratio = askingPrice / wtp;

            // ── ZONE 1: Heavily Underpriced (ratio ≤ 0.85) ────────────────
            if (ratio <= 0.85f)
            {
                string feedback = GetUnderpricedFeedback(ratio);
                return new TransactionResult(
                    TransactionOutcome.ImmediateAccept,
                    askingPrice,
                    0f,
                    feedback
                );
            }

            // ── ZONE 2: Fair Price / Negotiation (0.85 < ratio ≤ 1.15) ───
            if (ratio <= 1.15f)
            {
                // Customer makes a reasonable counter-offer
                float counterMin = wtp * 0.90f;
                float counterMax = wtp * 1.05f;
                float counterOffer = Random.Range(counterMin, counterMax);
                counterOffer = Mathf.Round(counterOffer * 100f) / 100f;

                string feedback = GetNegotiationFeedback(ratio);
                return new TransactionResult(
                    TransactionOutcome.NegotiationOpened,
                    askingPrice,
                    counterOffer,
                    feedback
                );
            }

            // ── ZONE 3: Overpriced (ratio > 1.15) ─────────────────────────
            // Check if customer walks away
            float leaveChance = Mathf.Clamp01((ratio - 1.15f) * 2f) * (1f - customer.Patience * 0.5f);

            if (Random.value < leaveChance)
            {
                string feedback = GetWalkAwayFeedback(ratio);
                return new TransactionResult(
                    TransactionOutcome.CustomerLeft,
                    askingPrice,
                    0f,
                    feedback
                );
            }

            // Customer stays but hard-haggles (lowball counter)
            float hardCounterMin = wtp * 0.85f;
            float hardCounterMax = wtp * 0.95f;
            float hardCounter = Random.Range(hardCounterMin, hardCounterMax);
            hardCounter = Mathf.Round(hardCounter * 100f) / 100f;

            string haggleFeedback = GetHardHaggleFeedback(ratio);
            return new TransactionResult(
                TransactionOutcome.HardHaggle,
                askingPrice,
                hardCounter,
                haggleFeedback
            );
        }

        // ── Quick Evaluation (for UI preview) ──────────────────────────────

        /// <summary>
        /// Get a quick price assessment without rolling randomness.
        /// Used by PricingPanel to show the player a hint about their pricing.
        /// Returns: "Great Deal", "Fair Price", "Overpriced", "Way Too High"
        /// </summary>
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

        /// <summary>
        /// Get color for price assessment (for UI display).
        /// </summary>
        public static Color GetAssessmentColor(PriceAssessment assessment)
        {
            return assessment switch
            {
                PriceAssessment.WayUnderpriced => new Color(0.2f, 0.6f, 1f),    // Blue — losing money
                PriceAssessment.Underpriced    => new Color(0.3f, 0.8f, 0.4f),   // Green — easy sell
                PriceAssessment.FairPrice      => new Color(0.4f, 0.9f, 0.3f),   // Bright green — sweet spot
                PriceAssessment.Overpriced     => new Color(1f, 0.8f, 0.2f),     // Yellow — risky
                PriceAssessment.WayOverpriced  => new Color(1f, 0.3f, 0.2f),     // Red — will lose customer
                _ => Color.gray
            };
        }

        // ── Feedback Strings ───────────────────────────────────────────────
        // These create the "game feel" — the player learns pricing through feedback

        private static string GetUnderpricedFeedback(float ratio)
        {
            if (ratio <= 0.5f)
                return "\"I can't believe this price! SOLD!\" (You're basically giving it away...)";
            if (ratio <= 0.7f)
                return "\"This is a steal! I'll take it!\" (You could have charged more.)";
            return "\"Great price, deal!\" (Slightly below market — quick sale though.)";
        }

        private static string GetNegotiationFeedback(float ratio)
        {
            if (ratio <= 0.95f)
                return "\"Hmm, that's reasonable. How about...\"";
            if (ratio <= 1.05f)
                return "\"Fair enough, but can you come down a bit?\"";
            return "\"That's a bit high... I was thinking more like...\"";
        }

        private static string GetWalkAwayFeedback(float ratio)
        {
            if (ratio >= 2.0f)
                return "\"Are you serious?! I'm out of here!\" (Customer stormed off.)";
            if (ratio >= 1.5f)
                return "\"Way too expensive. I'll shop elsewhere.\" (Customer left.)";
            return "\"Sorry, that's more than I want to spend. Goodbye.\" (Customer left.)";
        }

        private static string GetHardHaggleFeedback(float ratio)
        {
            if (ratio >= 1.5f)
                return "\"Look, I'll give you this much, take it or leave it.\"";
            return "\"That's too high for me, but I'd pay...\"";
        }
    }

    // ── Transaction Result ─────────────────────────────────────────────────

    /// <summary>
    /// Result of a transaction evaluation. Contains the outcome, prices, and feedback text.
    /// </summary>
    public class TransactionResult
    {
        /// <summary>What happened: accept, negotiate, haggle, or leave.</summary>
        public TransactionOutcome Outcome;

        /// <summary>The price the player asked for.</summary>
        public float AskingPrice;

        /// <summary>The customer's counter-offer (0 if immediate accept or left).</summary>
        public float CustomerCounterOffer;

        /// <summary>Flavor text describing the customer's reaction.</summary>
        public string Feedback;

        public TransactionResult(TransactionOutcome outcome, float askingPrice, float counterOffer, string feedback = "")
        {
            Outcome = outcome;
            AskingPrice = askingPrice;
            CustomerCounterOffer = counterOffer;
            Feedback = feedback;
        }

        /// <summary>Whether this outcome results in an immediate sale (no further interaction needed).</summary>
        public bool IsImmediateSale => Outcome == TransactionOutcome.ImmediateAccept;

        /// <summary>Whether this outcome opens a negotiation dialog.</summary>
        public bool RequiresPlayerResponse => Outcome == TransactionOutcome.NegotiationOpened ||
                                              Outcome == TransactionOutcome.HardHaggle;

        /// <summary>Whether the customer left without buying.</summary>
        public bool CustomerLeftEmpty => Outcome == TransactionOutcome.CustomerLeft;

        public override string ToString()
        {
            return $"[Transaction] {Outcome}: Asked ${AskingPrice:F2}, " +
                   $"Counter: ${CustomerCounterOffer:F2} — {Feedback}";
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Possible outcomes of a price evaluation.
    /// </summary>
    public enum TransactionOutcome
    {
        ImmediateAccept,     // Price so good, customer buys instantly
        NegotiationOpened,   // Normal haggling — customer makes reasonable counter
        HardHaggle,          // Customer stays but lowballs hard
        CustomerLeft         // Customer walks away — price too high
    }

    /// <summary>
    /// Quick price assessment categories for UI feedback.
    /// </summary>
    public enum PriceAssessment
    {
        Unknown,
        WayUnderpriced,   // Losing significant money
        Underpriced,      // Below market but profitable
        FairPrice,        // Sweet spot — good margin, good sell rate
        Overpriced,       // Above market — risk of losing customer
        WayOverpriced     // Will almost certainly lose the customer
    }
}
