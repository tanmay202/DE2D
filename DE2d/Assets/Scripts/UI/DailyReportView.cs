// ============================================================================
// DailyReportView.cs — End-of-Day Summary Popup
// Responsibility: Show daily stats when a day ends — revenue, costs, profit,
//                 units sold, customers lost. Player clicks "Next Day" to continue.
// Dependencies: GameManager, SimulationClock, DailyStats
// Scene Placement: UI/DailyReport (Canvas panel, shown on day end)
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeviceEmpire.Core;

namespace DeviceEmpire.UI
{
    /// <summary>
    /// End-of-day report popup. Shows financial summary and performance metrics.
    /// Pauses the game while visible. Player must click "Start Next Day" to continue.
    /// </summary>
    public class DailyReportView : MonoBehaviour
    {
        // ── UI References ──────────────────────────────────────────────────
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Revenue Section")]
        [SerializeField] private TextMeshProUGUI revenueText;
        [SerializeField] private TextMeshProUGUI costOfGoodsText;
        [SerializeField] private TextMeshProUGUI profitText;
        [SerializeField] private TextMeshProUGUI marginText;

        [Header("Activity Section")]
        [SerializeField] private TextMeshProUGUI unitsSoldText;
        [SerializeField] private TextMeshProUGUI customersServedText;
        [SerializeField] private TextMeshProUGUI customersLostText;

        [Header("Balance Section")]
        [SerializeField] private TextMeshProUGUI endingCashText;
        [SerializeField] private TextMeshProUGUI inventoryValueText;
        [SerializeField] private TextMeshProUGUI netWorthText;

        [Header("Performance")]
        [SerializeField] private TextMeshProUGUI performanceText;
        [SerializeField] private Image performanceIcon;

        [Header("Actions")]
        [SerializeField] private Button nextDayButton;
        [SerializeField] private TextMeshProUGUI nextDayButtonText;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm?.Clock != null)
            {
                gm.Clock.OnDayEnd += ShowReport;
            }

            if (nextDayButton != null)
            {
                nextDayButton.onClick.AddListener(OnNextDayClicked);
            }

            // Start hidden
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm?.Clock != null)
            {
                gm.Clock.OnDayEnd -= ShowReport;
            }
        }

        // ── Report Display ─────────────────────────────────────────────────

        /// <summary>
        /// Show the daily report. Called when SimulationClock fires OnDayEnd.
        /// </summary>
        private void ShowReport()
        {
            gameObject.SetActive(true);

            var gm = GameManager.Instance;
            if (gm == null) return;

            // Get today's stats (they've been archived by now, so get the last one)
            DailyStats stats = null;
            var history = gm.Clock.History;
            if (history != null && history.Count > 0)
            {
                stats = history[history.Count - 1];
            }

            if (stats == null)
            {
                Debug.LogWarning("[DailyReportView] No stats available for report.");
                return;
            }

            // ── Header ────────────────────────────────────────────────────
            if (titleText != null)
                titleText.text = $"Day {stats.DayNumber} Report";

            // ── Revenue Section ────────────────────────────────────────────
            if (revenueText != null)
                revenueText.text = $"Revenue: ${stats.TotalRevenue:F2}";

            if (costOfGoodsText != null)
                costOfGoodsText.text = $"Cost of Goods: ${stats.TotalCostOfGoods:F2}";

            if (profitText != null)
            {
                float profit = stats.Profit;
                profitText.text = profit >= 0
                    ? $"Profit: +${profit:F2}"
                    : $"Loss: -${Mathf.Abs(profit):F2}";
                profitText.color = profit >= 0
                    ? new Color(0.3f, 0.9f, 0.3f)
                    : new Color(0.9f, 0.3f, 0.3f);
            }

            if (marginText != null)
            {
                float marginPct = stats.TotalRevenue > 0
                    ? (stats.Profit / stats.TotalRevenue) * 100f
                    : 0f;
                marginText.text = $"Margin: {marginPct:F1}%";
            }

            // ── Activity Section ───────────────────────────────────────────
            if (unitsSoldText != null)
                unitsSoldText.text = $"Units Sold: {stats.UnitsSold}";

            if (customersServedText != null)
                customersServedText.text = $"Customers Served: {stats.CustomersServed}";

            if (customersLostText != null)
            {
                customersLostText.text = $"Customers Lost: {stats.CustomersLost}";
                customersLostText.color = stats.CustomersLost > 0
                    ? new Color(1f, 0.6f, 0.2f)
                    : new Color(0.7f, 0.7f, 0.7f);
            }

            // ── Balance Section ────────────────────────────────────────────
            if (endingCashText != null)
                endingCashText.text = $"Cash: ${gm.PlayerCash:F2}";

            if (inventoryValueText != null && gm.Inventory != null)
            {
                float invValue = gm.Inventory.GetTotalStockValue(gm.Clock.CurrentDay);
                inventoryValueText.text = $"Inventory Value: ${invValue:F2}";
            }

            if (netWorthText != null && gm.Inventory != null)
            {
                float invValue = gm.Inventory.GetTotalStockValue(gm.Clock.CurrentDay);
                float netWorth = gm.PlayerCash + invValue;
                netWorthText.text = $"Net Worth: ${netWorth:F2}";
            }

            // ── Performance Rating ─────────────────────────────────────────
            if (performanceText != null)
            {
                string rating = GetPerformanceRating(stats);
                performanceText.text = rating;
            }

            // ── Next Day Button ────────────────────────────────────────────
            if (nextDayButtonText != null)
                nextDayButtonText.text = $"Start Day {gm.Clock.CurrentDay}";

            Debug.Log($"[DailyReportView] Showing report for Day {stats.DayNumber}");
        }

        // ── Button Handler ─────────────────────────────────────────────────

        private void OnNextDayClicked()
        {
            gameObject.SetActive(false);

            // Start the next day
            GameManager.Instance?.Clock?.StartDay();

            Debug.Log("[DailyReportView] Player clicked Next Day.");
        }

        // ── Performance Rating ─────────────────────────────────────────────

        private string GetPerformanceRating(DailyStats stats)
        {
            if (stats.UnitsSold == 0 && stats.CustomersLost == 0)
                return "🏖️ Quiet day — no customers visited.";

            if (stats.UnitsSold == 0 && stats.CustomersLost > 0)
                return "😰 Rough day — all customers left empty-handed!";

            float conversionRate = stats.CustomersServed > 0
                ? (float)stats.UnitsSold / (stats.CustomersServed + stats.CustomersLost)
                : 0f;

            float avgProfit = stats.AverageMargin;

            if (conversionRate >= 0.8f && avgProfit > 20f)
                return "🌟 Outstanding! High sales, great margins!";

            if (conversionRate >= 0.6f && avgProfit > 10f)
                return "👍 Good day! Solid performance.";

            if (conversionRate >= 0.4f)
                return "📊 Decent. Room for improvement on pricing.";

            if (avgProfit < 0f)
                return "📉 Selling at a loss! Raise your prices.";

            return "🤷 Mixed results. Experiment with pricing!";
        }
    }
}
