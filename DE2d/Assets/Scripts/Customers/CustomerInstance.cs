// ============================================================================
// CustomerInstance.cs — Runtime Customer Instance
// Responsibility: A single customer visiting the shop. Holds sampled traits,
//                 wanted category, current state, and budget.
// Dependencies: CustomerArchetypeData, DeviceCategory
// Scene Placement: None — plain C# class, not a MonoBehaviour.
// ============================================================================

using UnityEngine;
using DeviceEmpire.Inventory;

namespace DeviceEmpire.Customers
{
    /// <summary>
    /// Represents a single customer currently in (or visiting) the shop.
    /// Created by CustomerSpawner, consumed by ShopController and TransactionEngine.
    /// 
    /// KEY DISTINCTION:
    /// - CustomerArchetypeData = "what kind of shopper is this?" (shared template)
    /// - CustomerInstance = "this specific person who walked in at 2:30 PM" (unique)
    /// </summary>
    [System.Serializable]
    public class CustomerInstance
    {
        // ── Identity ───────────────────────────────────────────────────────
        /// <summary>Reference to the archetype this customer was spawned from.</summary>
        public CustomerArchetypeData Archetype;

        /// <summary>The device category this customer wants to buy.</summary>
        public DeviceCategory WantedCategory;

        // ── Sampled Traits ─────────────────────────────────────────────────
        // These are rolled once at spawn and remain constant for this customer's visit.

        /// <summary>How price-sensitive this customer is. 1.0 = very cheap, 0.0 = price doesn't matter.</summary>
        public float BudgetSensitivity;

        /// <summary>Ability to assess device quality/spot defects. 1.0 = expert.</summary>
        public float TechSavviness;

        /// <summary>Willingness to negotiate/wait. 1.0 = very patient.</summary>
        public float Patience;

        /// <summary>Word-of-mouth influence on reputation (V2+).</summary>
        public float SocialInfluence;

        /// <summary>Maximum this customer can spend.</summary>
        public float Budget;

        // ── State ──────────────────────────────────────────────────────────
        /// <summary>Current state of this customer in the shop flow.</summary>
        public CustomerState State = CustomerState.Waiting;

        /// <summary>How many negotiation rounds have occurred with this customer.</summary>
        public int NegotiationRounds = 0;

        /// <summary>Maximum negotiation rounds before customer leaves (based on Patience).</summary>
        public int MaxNegotiationRounds;

        // ── Unique ID ──────────────────────────────────────────────────────
        private static int _nextId = 0;
        public int CustomerId { get; private set; }

        // ── Factory Method ─────────────────────────────────────────────────

        /// <summary>
        /// Spawn a new customer from an archetype. Rolls all traits from archetype ranges.
        /// This is the ONLY way to create customers — ensures all traits are properly initialized.
        /// </summary>
        /// <param name="archetype">The customer archetype to spawn from</param>
        /// <returns>A fully initialized CustomerInstance</returns>
        public static CustomerInstance Spawn(CustomerArchetypeData archetype)
        {
            var customer = new CustomerInstance
            {
                Archetype = archetype,
                CustomerId = _nextId++,
                State = CustomerState.Waiting,
                NegotiationRounds = 0
            };

            // Sample traits from archetype ranges
            customer.BudgetSensitivity = Random.Range(archetype.BudgetSensitivity.x, archetype.BudgetSensitivity.y);
            customer.TechSavviness     = Random.Range(archetype.TechSavviness.x, archetype.TechSavviness.y);
            customer.Patience          = Random.Range(archetype.Patience.x, archetype.Patience.y);
            customer.SocialInfluence   = Random.Range(archetype.SocialInfluence.x, archetype.SocialInfluence.y);
            customer.Budget            = Random.Range(archetype.BudgetRange.x, archetype.BudgetRange.y);

            // Pick a random wanted category from archetype preferences
            if (archetype.PreferredCategories != null && archetype.PreferredCategories.Length > 0)
            {
                customer.WantedCategory = archetype.PreferredCategories[
                    Random.Range(0, archetype.PreferredCategories.Length)];
            }
            else
            {
                customer.WantedCategory = DeviceCategory.Phone; // fallback
            }

            // Max negotiation rounds: 1-3 based on patience
            customer.MaxNegotiationRounds = Mathf.RoundToInt(1f + customer.Patience * 2f);

            Debug.Log($"[Customer] Spawned #{customer.CustomerId} '{archetype.ArchetypeName}' " +
                      $"wants: {customer.WantedCategory}, budget: ${customer.Budget:F2}, " +
                      $"patience: {customer.Patience:F2}");

            return customer;
        }

        // ── State Transitions ──────────────────────────────────────────────

        /// <summary>Transition to BeingServed state when player interacts.</summary>
        public void StartServing()
        {
            State = CustomerState.BeingServed;
        }

        /// <summary>Mark as satisfied after a successful sale.</summary>
        public void MarkSatisfied()
        {
            State = CustomerState.Satisfied;
        }

        /// <summary>Mark as dissatisfied (rejected offer, overpriced, etc.).</summary>
        public void MarkDissatisfied()
        {
            State = CustomerState.Dissatisfied;
        }

        /// <summary>Mark as left (timed out or walked away).</summary>
        public void MarkLeft()
        {
            State = CustomerState.Left;
        }

        /// <summary>Increment negotiation counter. Returns true if more rounds allowed.</summary>
        public bool IncrementNegotiation()
        {
            NegotiationRounds++;
            return NegotiationRounds < MaxNegotiationRounds;
        }

        // ── Queries ────────────────────────────────────────────────────────

        /// <summary>Can this customer afford a device at the given price?</summary>
        public bool CanAfford(float price) => Budget >= price;

        /// <summary>Has this customer exhausted their negotiation patience?</summary>
        public bool IsOutOfPatience => NegotiationRounds >= MaxNegotiationRounds;

        /// <summary>
        /// Get a mood emoji for UI display based on current state.
        /// </summary>
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

        /// <summary>Get the archetype name for display.</summary>
        public string GetDisplayName()
        {
            return Archetype != null ? Archetype.ArchetypeName : "Unknown Customer";
        }

        public override string ToString()
        {
            return $"[Customer #{CustomerId}] {GetDisplayName()} | " +
                   $"Wants: {WantedCategory} | Budget: ${Budget:F2} | " +
                   $"State: {State}";
        }
    }

    // ── Customer State Enum ────────────────────────────────────────────────

    /// <summary>
    /// Lifecycle states of a customer visit.
    /// Flow: Waiting → BeingServed → Satisfied/Dissatisfied → (removed)
    ///                             → Left (timeout/reject)
    /// </summary>
    public enum CustomerState
    {
        Waiting,       // Just arrived, waiting for the player to interact
        BeingServed,   // Player is showing them devices / negotiating
        Satisfied,     // Sale completed successfully
        Dissatisfied,  // Left unhappy (overpriced, rejected, etc.)
        Left           // Walked out (timed out or dismissed)
    }
}
