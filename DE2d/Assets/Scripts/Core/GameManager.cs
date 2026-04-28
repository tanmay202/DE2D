// ============================================================================
// GameManager.cs — Root Singleton
// Responsibility: Single source of truth for global game state.
//                 All other managers register with this.
//                 Holds player cash, current phase, and manager references.
// Dependencies: None — this is the root node of the dependency graph.
// Scene Placement: Managers/GameManager (empty GameObject)
// ============================================================================

using System;
using UnityEngine;

namespace DeviceEmpire.Core
{
    /// <summary>
    /// Central game state manager. Singleton accessible via GameManager.Instance.
    /// All systems read/write shared state through this class.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Game State ─────────────────────────────────────────────────────
        [Header("Game State")]
        [Tooltip("Current progression phase of the player")]
        public GamePhase CurrentPhase = GamePhase.Roadside;

        [Tooltip("Player's current cash balance")]
        [SerializeField] private float _playerCash = 500f;

        /// <summary>Read-only access to player cash. Use AddCash/SpendCash to modify.</summary>
        public float PlayerCash => _playerCash;

        // ── Manager References ─────────────────────────────────────────────
        // Assigned in Inspector — drag from Managers/ hierarchy
        [Header("Manager References")]
        [Tooltip("Drag the SimulationClock GameObject here")]
        public SimulationClock Clock;

        [Tooltip("Drag the EconomyCore GameObject here")]
        public EconomyCore Economy;

        [Tooltip("Drag the InventoryManager GameObject here")]
        public Inventory.InventoryManager Inventory;

        [Tooltip("Drag the CustomerSpawner GameObject here")]
        public Customers.CustomerSpawner Customers;

        [Tooltip("Drag the ShopController GameObject here")]
        public ShopController Shop;

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired whenever PlayerCash changes. Param = new cash amount.</summary>
        public event Action<float> OnCashChanged;

        /// <summary>Fired when a phase transition occurs. Param = new phase.</summary>
        public event Action<GamePhase> OnPhaseChanged;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Awake()
        {
            // Singleton enforcement — destroy duplicates
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[GameManager] Duplicate instance destroyed on '{gameObject.name}'.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Persist across scene loads (future-proofing for V3+ multi-scene)
            DontDestroyOnLoad(gameObject);

            Debug.Log("[GameManager] Initialized. Starting cash: $" + _playerCash);
        }

        private void Start()
        {
            // Fire initial cash event so UI picks up the starting value
            OnCashChanged?.Invoke(_playerCash);

            // Validate manager references
            ValidateReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Cash Operations ────────────────────────────────────────────────

        /// <summary>
        /// Add cash to the player's balance. Used for sales revenue.
        /// </summary>
        /// <param name="amount">Amount to add (must be positive)</param>
        public void AddCash(float amount)
        {
            if (amount < 0f)
            {
                Debug.LogWarning("[GameManager] AddCash called with negative amount. Use SpendCash instead.");
                return;
            }

            _playerCash += amount;
            _playerCash = Mathf.Round(_playerCash * 100f) / 100f; // Round to 2 decimals
            OnCashChanged?.Invoke(_playerCash);

            Debug.Log($"[GameManager] +${amount:F2} → Balance: ${_playerCash:F2}");
        }

        /// <summary>
        /// Attempt to spend cash. Returns false if insufficient funds.
        /// </summary>
        /// <param name="amount">Amount to spend (must be positive)</param>
        /// <returns>True if transaction succeeded, false if insufficient funds</returns>
        public bool SpendCash(float amount)
        {
            if (amount < 0f)
            {
                Debug.LogWarning("[GameManager] SpendCash called with negative amount.");
                return false;
            }

            if (_playerCash < amount)
            {
                Debug.Log($"[GameManager] Insufficient funds. Need ${amount:F2}, have ${_playerCash:F2}");
                return false;
            }

            _playerCash -= amount;
            _playerCash = Mathf.Round(_playerCash * 100f) / 100f;
            OnCashChanged?.Invoke(_playerCash);

            Debug.Log($"[GameManager] -${amount:F2} → Balance: ${_playerCash:F2}");
            return true;
        }

        /// <summary>
        /// Check if the player can afford a purchase without actually spending.
        /// </summary>
        public bool CanAfford(float amount) => _playerCash >= amount;

        // ── Phase Management ───────────────────────────────────────────────

        /// <summary>
        /// Transition to a new game phase. V1 stays in Roadside.
        /// </summary>
        public void SetPhase(GamePhase newPhase)
        {
            if (CurrentPhase == newPhase) return;

            Debug.Log($"[GameManager] Phase transition: {CurrentPhase} → {newPhase}");
            CurrentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }

        // ── Validation ─────────────────────────────────────────────────────

        private void ValidateReferences()
        {
            if (Clock == null)    Debug.LogError("[GameManager] SimulationClock reference is missing!");
            if (Economy == null)  Debug.LogError("[GameManager] EconomyCore reference is missing!");
            if (Inventory == null) Debug.LogError("[GameManager] InventoryManager reference is missing!");
            if (Customers == null) Debug.LogError("[GameManager] CustomerSpawner reference is missing!");
            if (Shop == null)     Debug.LogError("[GameManager] ShopController reference is missing!");
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Progression phases — each unlocks new mechanics and systems.
    /// V1 only uses Roadside.
    /// </summary>
    public enum GamePhase
    {
        Roadside,       // V1: Folding table, 10 inventory slots
        SmallShop,      // V2: Proper shop, employees, displays
        MultiShop,      // V3: Multiple locations, supply chains
        BrandCreator,   // V4: Custom branding, marketing
        Manufacturer,   // V4+: Build your own devices
        GlobalCorp      // V5: Full corporate empire
    }
}
