// ============================================================================
// SimulationClock.cs — In-Game Time Controller
// Responsibility: Track in-game days. Fire day-start/day-end events.
//                 Controls time scale (pause, normal, fast-forward).
// Dependencies: GameManager (registers on it)
// Scene Placement: Managers/SimulationClock
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeviceEmpire.Core
{
    /// <summary>
    /// Manages the flow of in-game time. One game day = configurable real seconds.
    /// Fires events at day boundaries that other systems subscribe to.
    /// </summary>
    public class SimulationClock : MonoBehaviour
    {
        // ── Configuration ──────────────────────────────────────────────────
        [Header("Time Configuration")]
        [Tooltip("How many real-world seconds equal one in-game day")]
        [SerializeField] private float realSecondsPerGameDay = 480f; // 8 minutes

        [Tooltip("Current time scale multiplier (1x, 2x, 3x)")]
        [SerializeField] private float timeScale = 1f;

        // ── State ──────────────────────────────────────────────────────────
        [Header("Current State (Read Only)")]
        [SerializeField] private int _currentDay = 1;
        [SerializeField] private float _dayProgress = 0f;
        [SerializeField] private bool _isRunning = false;

        /// <summary>Current in-game day number (1-indexed).</summary>
        public int CurrentDay => _currentDay;

        /// <summary>Progress through current day: 0.0 (dawn) to 1.0 (end of day).</summary>
        public float DayProgress => _dayProgress;

        /// <summary>Whether the clock is currently ticking.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Current time scale multiplier.</summary>
        public float TimeScale => timeScale;

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired when a new day begins. Subscribe to reset daily state.</summary>
        public event Action OnDayStart;

        /// <summary>Fired when the current day ends. Subscribe for daily reports.</summary>
        public event Action OnDayEnd;

        /// <summary>Fired every frame while running. Param = normalized day progress [0,1].</summary>
        public event Action<float> OnDayProgressUpdate;

        // ── Daily Stats Tracking ───────────────────────────────────────────
        // V1 simple tracking — expanded in V2
        private DailyStats _todayStats;
        public DailyStats TodayStats => _todayStats;

        private List<DailyStats> _history = new();
        public IReadOnlyList<DailyStats> History => _history.AsReadOnly();

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            _todayStats = new DailyStats(_currentDay);
        }

        private void Update()
        {
            if (!_isRunning) return;

            // Advance day progress based on real time and time scale
            float delta = (Time.deltaTime * timeScale) / realSecondsPerGameDay;
            _dayProgress += delta;

            // Notify listeners of progress (for UI time bar, etc.)
            OnDayProgressUpdate?.Invoke(_dayProgress);

            // Check for end of day
            if (_dayProgress >= 1f)
            {
                EndDay();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Start the current day. Call this when the player is ready.
        /// In V1, called automatically by GameManager on Start.
        /// </summary>
        public void StartDay()
        {
            _isRunning = true;
            _dayProgress = 0f;
            _todayStats = new DailyStats(_currentDay);

            Debug.Log($"[SimulationClock] Day {_currentDay} started.");
            OnDayStart?.Invoke();
        }

        /// <summary>Pause the clock. Time stops but state is preserved.</summary>
        public void PauseDay()
        {
            _isRunning = false;
            Debug.Log($"[SimulationClock] Day {_currentDay} paused at {_dayProgress:P0}.");
        }

        /// <summary>Resume the clock from where it was paused.</summary>
        public void ResumeDay()
        {
            _isRunning = true;
            Debug.Log($"[SimulationClock] Day {_currentDay} resumed.");
        }

        /// <summary>
        /// Set the time scale multiplier. Clamped to [0.5, 3.0].
        /// </summary>
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Clamp(scale, 0.5f, 3f);
            Debug.Log($"[SimulationClock] Time scale set to {timeScale}x.");
        }

        /// <summary>
        /// Force end the current day (e.g., player clicks "End Day" button).
        /// </summary>
        public void ForceEndDay()
        {
            if (_isRunning)
            {
                EndDay();
            }
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void EndDay()
        {
            _isRunning = false;
            _dayProgress = 0f;

            Debug.Log($"[SimulationClock] Day {_currentDay} ended. " +
                      $"Revenue: ${_todayStats.TotalRevenue:F2}, " +
                      $"Sales: {_todayStats.UnitsSold}");

            // Archive today's stats
            _history.Add(_todayStats);

            // Fire end-of-day event (daily report, save, etc.)
            OnDayEnd?.Invoke();

            // Advance to next day
            _currentDay++;
        }

        // ── Convenience: Get formatted time ────────────────────────────────

        /// <summary>
        /// Returns a formatted time string for UI display.
        /// Maps day progress to a 9AM–9PM business hours format.
        /// </summary>
        public string GetFormattedTime()
        {
            // Business hours: 9:00 AM to 9:00 PM (12 hours)
            float totalHours = 9f + (_dayProgress * 12f);
            int hours = Mathf.FloorToInt(totalHours);
            int minutes = Mathf.FloorToInt((totalHours - hours) * 60f);

            string period = hours >= 12 ? "PM" : "AM";
            int displayHour = hours > 12 ? hours - 12 : hours;
            if (displayHour == 0) displayHour = 12;

            return $"{displayHour}:{minutes:D2} {period}";
        }
    }

    // ── Daily Stats Container ──────────────────────────────────────────────

    /// <summary>
    /// Tracks all statistics for a single in-game day.
    /// Populated by various systems, read by DailyReportView.
    /// </summary>
    [System.Serializable]
    public class DailyStats
    {
        public int DayNumber;
        public float TotalRevenue;
        public float TotalCostOfGoods;   // What you paid for items sold
        public int UnitsSold;
        public int CustomersServed;
        public int CustomersLost;        // Walked away / rejected

        /// <summary>Net profit = Revenue - Cost of Goods</summary>
        public float Profit => TotalRevenue - TotalCostOfGoods;

        /// <summary>Average margin per sale</summary>
        public float AverageMargin => UnitsSold > 0 ? Profit / UnitsSold : 0f;

        public DailyStats(int dayNumber)
        {
            DayNumber = dayNumber;
        }

        /// <summary>Record a completed sale.</summary>
        public void RecordSale(float salePrice, float costOfGoods)
        {
            TotalRevenue += salePrice;
            TotalCostOfGoods += costOfGoods;
            UnitsSold++;
            CustomersServed++;
        }

        /// <summary>Record a lost customer (left or rejected).</summary>
        public void RecordLostCustomer()
        {
            CustomersLost++;
        }
    }
}
