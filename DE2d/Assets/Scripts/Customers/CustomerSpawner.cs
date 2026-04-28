// ============================================================================
// CustomerSpawner.cs — Customer Spawn Controller
// Responsibility: Spawn customers on a timer based on the current phase.
//                 V1 = one customer at a time, simple interval.
//                 V2+ = queue system, multiple customers, demand curves.
// Dependencies: GameManager, CustomerArchetypeData, CustomerInstance
// Scene Placement: Managers/CustomerSpawner
// ============================================================================

using System;
using UnityEngine;

namespace DeviceEmpire.Customers
{
    /// <summary>
    /// Spawns customers at regular intervals. In V1, only one customer at a time.
    /// The spawner picks a random archetype (weighted) and creates a CustomerInstance.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        // ── Configuration ──────────────────────────────────────────────────
        [Header("Spawn Configuration")]
        [Tooltip("Available customer archetypes to spawn from. Assign in Inspector.")]
        [SerializeField] private CustomerArchetypeData[] availableArchetypes;

        [Tooltip("Time between customer spawns in real seconds")]
        [SerializeField] private float spawnIntervalSeconds = 30f;

        [Tooltip("Random variance on spawn interval (±seconds)")]
        [SerializeField] private float spawnIntervalVariance = 10f;

        [Tooltip("Delay before first customer spawns")]
        [SerializeField] private float initialDelay = 5f;

        // ── State ──────────────────────────────────────────────────────────
        private float _timer;
        private float _currentInterval;
        private bool _isSpawning = true;
        private bool _initialDelayPassed = false;

        /// <summary>The customer currently in the shop (null if none). V1 = one at a time.</summary>
        public CustomerInstance ActiveCustomer { get; private set; }

        /// <summary>Whether there is currently a customer in the shop.</summary>
        public bool HasCustomer => ActiveCustomer != null;

        /// <summary>Total customers spawned this session (for stats).</summary>
        public int TotalSpawned { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired when a new customer arrives. Param = the customer.</summary>
        public event Action<CustomerInstance> OnCustomerArrived;

        /// <summary>Fired when the active customer leaves (any reason).</summary>
        public event Action OnCustomerLeft;

        /// <summary>Fired when a customer is dismissed by the player.</summary>
        public event Action<CustomerInstance> OnCustomerDismissed;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            _timer = 0f;
            _currentInterval = initialDelay;
            _initialDelayPassed = false;

            // Validate archetypes
            if (availableArchetypes == null || availableArchetypes.Length == 0)
            {
                Debug.LogError("[CustomerSpawner] No customer archetypes assigned! " +
                               "Drag CustomerArchetypeData assets into the Available Archetypes array.");
                _isSpawning = false;
            }
        }

        private void OnEnable()
        {
            // Subscribe to day events
            if (Core.GameManager.Instance?.Clock != null)
            {
                Core.GameManager.Instance.Clock.OnDayStart += HandleDayStart;
                Core.GameManager.Instance.Clock.OnDayEnd += HandleDayEnd;
            }
        }

        private void OnDisable()
        {
            if (Core.GameManager.Instance?.Clock != null)
            {
                Core.GameManager.Instance.Clock.OnDayStart -= HandleDayStart;
                Core.GameManager.Instance.Clock.OnDayEnd -= HandleDayEnd;
            }
        }

        private void Update()
        {
            // Don't spawn if disabled, or if there's already a customer (V1: one at a time)
            if (!_isSpawning || ActiveCustomer != null) return;

            _timer += Time.deltaTime;
            if (_timer >= _currentInterval)
            {
                SpawnNext();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Dismiss the active customer. Called when sale completes or player rejects.
        /// </summary>
        public void DismissCustomer()
        {
            if (ActiveCustomer == null) return;

            var dismissed = ActiveCustomer;
            Debug.Log($"[CustomerSpawner] Customer #{dismissed.CustomerId} dismissed. State: {dismissed.State}");

            ActiveCustomer = null;
            ResetSpawnTimer();

            OnCustomerDismissed?.Invoke(dismissed);
            OnCustomerLeft?.Invoke();
        }

        /// <summary>
        /// Enable or disable customer spawning.
        /// </summary>
        public void SetSpawningEnabled(bool enabled)
        {
            _isSpawning = enabled;
            if (!enabled)
            {
                Debug.Log("[CustomerSpawner] Spawning disabled.");
            }
            else
            {
                Debug.Log("[CustomerSpawner] Spawning enabled.");
                ResetSpawnTimer();
            }
        }

        /// <summary>
        /// Force spawn a customer immediately (for testing / tutorials).
        /// </summary>
        public void ForceSpawn()
        {
            if (ActiveCustomer != null)
            {
                Debug.LogWarning("[CustomerSpawner] Cannot force spawn — customer already present.");
                return;
            }
            SpawnNext();
        }

        /// <summary>
        /// Change spawn interval (for difficulty / phase scaling).
        /// </summary>
        public void SetSpawnInterval(float seconds, float variance = 10f)
        {
            spawnIntervalSeconds = Mathf.Max(5f, seconds);
            spawnIntervalVariance = Mathf.Max(0f, variance);
            Debug.Log($"[CustomerSpawner] Interval set to {spawnIntervalSeconds}s (±{spawnIntervalVariance}s).");
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void SpawnNext()
        {
            // Pick a weighted random archetype
            var archetype = PickWeightedArchetype();
            if (archetype == null)
            {
                Debug.LogError("[CustomerSpawner] Failed to pick archetype!");
                return;
            }

            // Spawn the customer
            ActiveCustomer = CustomerInstance.Spawn(archetype);
            TotalSpawned++;

            // Reset timer for next spawn
            ResetSpawnTimer();

            Debug.Log($"[CustomerSpawner] Customer #{ActiveCustomer.CustomerId} arrived! " +
                      $"({ActiveCustomer.GetDisplayName()}, wants: {ActiveCustomer.WantedCategory})");

            OnCustomerArrived?.Invoke(ActiveCustomer);
        }

        private CustomerArchetypeData PickWeightedArchetype()
        {
            if (availableArchetypes.Length == 1)
                return availableArchetypes[0];

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var arch in availableArchetypes)
            {
                totalWeight += arch.SpawnWeight;
            }

            // Pick random point in weight range
            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var arch in availableArchetypes)
            {
                cumulative += arch.SpawnWeight;
                if (roll <= cumulative)
                    return arch;
            }

            // Fallback (shouldn't happen)
            return availableArchetypes[0];
        }

        private void ResetSpawnTimer()
        {
            _timer = 0f;
            _currentInterval = spawnIntervalSeconds + 
                               UnityEngine.Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
            _currentInterval = Mathf.Max(5f, _currentInterval); // minimum 5 seconds
        }

        // ── Day Event Handlers ─────────────────────────────────────────────

        private void HandleDayStart()
        {
            _isSpawning = true;
            ResetSpawnTimer();
            Debug.Log("[CustomerSpawner] New day — spawning enabled.");
        }

        private void HandleDayEnd()
        {
            _isSpawning = false;

            // Clear any remaining customer at end of day
            if (ActiveCustomer != null)
            {
                ActiveCustomer.MarkLeft();
                Core.GameManager.Instance?.Economy?.RecordLostCustomer();
                DismissCustomer();
            }

            Debug.Log($"[CustomerSpawner] Day ended. Total spawned today: {TotalSpawned}");
        }
    }
}
