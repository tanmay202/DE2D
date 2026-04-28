// ============================================================================
// EconomyCore.cs — Economy State Tracker
// Responsibility: Track daily revenue, expenses, profit margins.
//                 Provides economy data to UI and other systems.
//                 V1 is lightweight — expanded in V2 with market simulation.
// Dependencies: GameManager, SimulationClock
// Scene Placement: Managers/EconomyCore
// ============================================================================

using System;
using UnityEngine;

namespace DeviceEmpire.Core
{
    /// <summary>
    /// Central economy tracker. Aggregates financial data across all transactions.
    /// In V1, this primarily feeds the Daily Report and HUD.
    /// In V2+, this will drive market pricing, supply/demand, and reputation.
    /// </summary>
    public class EconomyCore : MonoBehaviour
    {
        // ── Lifetime Stats ─────────────────────────────────────────────────
        [Header("Lifetime Statistics (Read Only)")]
        [SerializeField] private float _lifetimeRevenue;
        [SerializeField] private float _lifetimeCosts;
        [SerializeField] private int _lifetimeSales;

        public float LifetimeRevenue => _lifetimeRevenue;
        public float LifetimeCosts => _lifetimeCosts;
        public float LifetimeProfit => _lifetimeRevenue - _lifetimeCosts;
        public int LifetimeSales => _lifetimeSales;

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired after a sale is recorded. Params: salePrice, profit.</summary>
        public event Action<float, float> OnSaleRecorded;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void OnEnable()
        {
            // Subscribe to clock events for daily aggregation
            if (GameManager.Instance != null && GameManager.Instance.Clock != null)
            {
                GameManager.Instance.Clock.OnDayEnd += HandleDayEnd;
            }
        }

        private void Start()
        {
            // Delayed subscription in case GameManager initializes after us
            if (GameManager.Instance != null && GameManager.Instance.Clock != null)
            {
                GameManager.Instance.Clock.OnDayEnd -= HandleDayEnd; // prevent double
                GameManager.Instance.Clock.OnDayEnd += HandleDayEnd;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.Clock != null)
            {
                GameManager.Instance.Clock.OnDayEnd -= HandleDayEnd;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Record a completed sale. Called by ShopController after a transaction.
        /// </summary>
        /// <param name="salePrice">Price the customer paid</param>
        /// <param name="costOfGoods">What the player originally paid for the device</param>
        public void RecordSale(float salePrice, float costOfGoods)
        {
            _lifetimeRevenue += salePrice;
            _lifetimeCosts += costOfGoods;
            _lifetimeSales++;

            float profit = salePrice - costOfGoods;

            // Also record in today's daily stats
            if (GameManager.Instance?.Clock?.TodayStats != null)
            {
                GameManager.Instance.Clock.TodayStats.RecordSale(salePrice, costOfGoods);
            }

            OnSaleRecorded?.Invoke(salePrice, profit);

            Debug.Log($"[EconomyCore] Sale recorded: ${salePrice:F2} " +
                      $"(cost: ${costOfGoods:F2}, profit: ${profit:F2})");
        }

        /// <summary>
        /// Record a lost customer for today's stats.
        /// </summary>
        public void RecordLostCustomer()
        {
            GameManager.Instance?.Clock?.TodayStats?.RecordLostCustomer();
        }

        /// <summary>
        /// Calculate the player's current profit margin percentage.
        /// </summary>
        public float GetLifetimeProfitMargin()
        {
            if (_lifetimeRevenue <= 0f) return 0f;
            return (LifetimeProfit / _lifetimeRevenue) * 100f;
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void HandleDayEnd()
        {
            var stats = GameManager.Instance.Clock.TodayStats;
            Debug.Log($"[EconomyCore] Day {stats.DayNumber} summary: " +
                      $"Revenue=${stats.TotalRevenue:F2}, " +
                      $"Profit=${stats.Profit:F2}, " +
                      $"Sales={stats.UnitsSold}, " +
                      $"Lost={stats.CustomersLost}");
        }
    }
}
