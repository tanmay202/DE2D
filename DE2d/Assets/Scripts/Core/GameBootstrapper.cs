// ============================================================================
// GameBootstrapper.cs — Scene Initialization & Day Startup
// Responsibility: Wire up all managers, validate references, start the first day.
//                 This is the FIRST script to run (via Script Execution Order or
//                 by setting it to execute after GameManager in the Inspector).
// Dependencies: All managers must be assigned in GameManager Inspector.
// Scene Placement: Managers/GameBootstrapper (or on the GameManager GameObject)
// ============================================================================

using UnityEngine;
using DeviceEmpire.Core;

namespace DeviceEmpire.Core
{
    /// <summary>
    /// Bootstrapper that initializes the game on scene load.
    /// Ensures all systems are wired up and starts the first day.
    /// 
    /// EXECUTION ORDER:
    /// 1. GameManager.Awake() — singleton setup
    /// 2. All other managers' Awake() — individual init
    /// 3. GameBootstrapper.Start() — wire everything, start day 1
    /// 
    /// WHY A SEPARATE BOOTSTRAPPER?
    /// - Keeps GameManager clean (it's just state + references)
    /// - Centralizes initialization logic
    /// - Easy to add loading screens, save/load, etc. later
    /// - Clear place to put "game start" logic vs "system init" logic
    /// </summary>
    [DefaultExecutionOrder(100)] // Run after all other Awake() calls
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Auto-Start Configuration")]
        [Tooltip("Automatically start Day 1 when the scene loads")]
        [SerializeField] private bool autoStartDay = true;

        [Tooltip("Delay before starting Day 1 (seconds)")]
        [SerializeField] private float startDelay = 1f;

        [Header("Debug")]
        [Tooltip("Enable verbose debug logging during initialization")]
        [SerializeField] private bool verboseLogging = true;

        private void Start()
        {
            Log("=== DEVICE EMPIRE V1 — INITIALIZING ===");

            // Step 1: Validate GameManager exists
            if (GameManager.Instance == null)
            {
                Debug.LogError("[Bootstrapper] GameManager.Instance is null! " +
                               "Make sure a GameManager exists in the scene with the GameManager component.");
                return;
            }

            // Step 2: Validate all manager references
            bool allValid = ValidateManagers();
            if (!allValid)
            {
                Debug.LogError("[Bootstrapper] One or more manager references are missing. " +
                               "Check the GameManager Inspector.");
                return;
            }

            // Step 3: Log initialization summary
            LogInitSummary();

            // Step 4: Start Day 1
            if (autoStartDay)
            {
                if (startDelay > 0f)
                {
                    Log($"Starting Day 1 in {startDelay} seconds...");
                    Invoke(nameof(StartFirstDay), startDelay);
                }
                else
                {
                    StartFirstDay();
                }
            }
            else
            {
                Log("Auto-start disabled. Call StartFirstDay() manually or via UI.");
            }

            Log("=== INITIALIZATION COMPLETE ===");
        }

        /// <summary>
        /// Start the first day of gameplay.
        /// </summary>
        public void StartFirstDay()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            Log($"Starting Day {gm.Clock.CurrentDay}...");
            gm.Clock.StartDay();

            Log($"Game started! Cash: ${gm.PlayerCash:F2}, " +
                $"Inventory: {gm.Inventory.CurrentCount}/{gm.Inventory.MaxCapacity}");
        }

        // ── Validation ─────────────────────────────────────────────────────

        private bool ValidateManagers()
        {
            var gm = GameManager.Instance;
            bool valid = true;

            if (gm.Clock == null)
            {
                Debug.LogError("[Bootstrapper] ✗ SimulationClock is not assigned!");
                valid = false;
            }
            else Log("✓ SimulationClock");

            if (gm.Economy == null)
            {
                Debug.LogError("[Bootstrapper] ✗ EconomyCore is not assigned!");
                valid = false;
            }
            else Log("✓ EconomyCore");

            if (gm.Inventory == null)
            {
                Debug.LogError("[Bootstrapper] ✗ InventoryManager is not assigned!");
                valid = false;
            }
            else Log("✓ InventoryManager");

            if (gm.Customers == null)
            {
                Debug.LogError("[Bootstrapper] ✗ CustomerSpawner is not assigned!");
                valid = false;
            }
            else Log("✓ CustomerSpawner");

            if (gm.Shop == null)
            {
                Debug.LogError("[Bootstrapper] ✗ ShopController is not assigned!");
                valid = false;
            }
            else Log("✓ ShopController");

            return valid;
        }

        private void LogInitSummary()
        {
            var gm = GameManager.Instance;

            Log("─── Init Summary ───");
            Log($"  Phase: {gm.CurrentPhase}");
            Log($"  Cash: ${gm.PlayerCash:F2}");
            Log($"  Inventory Capacity: {gm.Inventory.MaxCapacity}");
            Log($"  Day: {gm.Clock.CurrentDay}");
            Log($"  Day Length: {gm.Clock.GetFormattedTime()} → configured in SimulationClock");
            Log("────────────────────");
        }

        private void Log(string message)
        {
            if (verboseLogging)
                Debug.Log($"[Bootstrapper] {message}");
        }
    }
}
