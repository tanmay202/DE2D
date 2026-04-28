// ============================================================================
// HUDView.cs — Heads-Up Display
// Responsibility: Show cash, day number, time progress, inventory count.
//                 Pure passive view — reads data, displays it, calls nothing.
// Dependencies: GameManager (subscribes to events)
// Scene Placement: UI/HUD (Canvas panel, always visible)
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeviceEmpire.Core;

namespace DeviceEmpire.UI
{
    /// <summary>
    /// Always-visible HUD showing essential game state.
    /// PASSIVE VIEW PATTERN: This script ONLY reads data and updates UI.
    /// It never modifies game state or calls game logic methods.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        // ── UI References ──────────────────────────────────────────────────
        [Header("Cash Display")]
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI cashChangeText; // +$50 flash

        [Header("Time Display")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Slider dayProgressBar;

        [Header("Inventory Display")]
        [SerializeField] private TextMeshProUGUI inventoryCountText;

        [Header("Customer Display")]
        [SerializeField] private TextMeshProUGUI customerStatusText;

        [Header("Buttons")]
        [SerializeField] private Button supplierButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button endDayButton;

        [Header("References")]
        [SerializeField] private SupplierPanel supplierPanel;

        // ── State ──────────────────────────────────────────────────────────
        private float _lastCash;
        private float _cashChangeTimer;
        private const float CASH_CHANGE_DISPLAY_TIME = 2f;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            // Subscribe to events
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnCashChanged += UpdateCash;

                if (gm.Clock != null)
                {
                    gm.Clock.OnDayStart += UpdateDayDisplay;
                    gm.Clock.OnDayProgressUpdate += UpdateTimeProgress;
                }

                if (gm.Inventory != null)
                {
                    gm.Inventory.OnInventoryChanged += UpdateInventoryCount;
                }

                if (gm.Customers != null)
                {
                    gm.Customers.OnCustomerArrived += (c) => UpdateCustomerStatus();
                    gm.Customers.OnCustomerLeft += UpdateCustomerStatus;
                }

                _lastCash = gm.PlayerCash;
            }

            // Button listeners
            if (supplierButton != null && supplierPanel != null)
                supplierButton.onClick.AddListener(() => supplierPanel.TogglePanel());

            if (pauseButton != null)
                pauseButton.onClick.AddListener(TogglePause);

            if (speedButton != null)
                speedButton.onClick.AddListener(CycleSpeed);

            if (endDayButton != null)
                endDayButton.onClick.AddListener(EndDay);

            // Initial display
            RefreshAll();
        }

        private void Update()
        {
            // Fade out cash change indicator
            if (_cashChangeTimer > 0f)
            {
                _cashChangeTimer -= Time.deltaTime;
                if (_cashChangeTimer <= 0f && cashChangeText != null)
                {
                    cashChangeText.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnCashChanged -= UpdateCash;

                if (gm.Clock != null)
                {
                    gm.Clock.OnDayStart -= UpdateDayDisplay;
                    gm.Clock.OnDayProgressUpdate -= UpdateTimeProgress;
                }

                if (gm.Inventory != null)
                {
                    gm.Inventory.OnInventoryChanged -= UpdateInventoryCount;
                }
            }
        }

        // ── Update Methods ─────────────────────────────────────────────────

        private void UpdateCash(float newCash)
        {
            if (cashText != null)
            {
                cashText.text = $"${newCash:F2}";
            }

            // Show cash change flash
            float change = newCash - _lastCash;
            if (Mathf.Abs(change) > 0.01f && cashChangeText != null)
            {
                string prefix = change > 0 ? "+" : "";
                cashChangeText.text = $"{prefix}${change:F2}";
                cashChangeText.color = change > 0 ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
                cashChangeText.gameObject.SetActive(true);
                _cashChangeTimer = CASH_CHANGE_DISPLAY_TIME;
            }

            _lastCash = newCash;
        }

        private void UpdateDayDisplay()
        {
            if (dayText != null && GameManager.Instance?.Clock != null)
            {
                dayText.text = $"Day {GameManager.Instance.Clock.CurrentDay}";
            }
        }

        private void UpdateTimeProgress(float progress)
        {
            if (dayProgressBar != null)
            {
                dayProgressBar.value = progress;
            }

            if (timeText != null && GameManager.Instance?.Clock != null)
            {
                timeText.text = GameManager.Instance.Clock.GetFormattedTime();
            }
        }

        private void UpdateInventoryCount(int count)
        {
            if (inventoryCountText != null && GameManager.Instance?.Inventory != null)
            {
                int max = GameManager.Instance.Inventory.MaxCapacity;
                inventoryCountText.text = $"Inventory: {count}/{max}";
            }
        }

        private void UpdateCustomerStatus()
        {
            if (customerStatusText == null) return;

            var customer = GameManager.Instance?.Customers?.ActiveCustomer;
            if (customer != null)
            {
                customerStatusText.text = $"{customer.GetMoodEmoji()} {customer.GetDisplayName()} wants: {customer.WantedCategory}";
            }
            else
            {
                customerStatusText.text = "Waiting for customers...";
            }
        }

        private void RefreshAll()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            UpdateCash(gm.PlayerCash);
            UpdateDayDisplay();
            UpdateInventoryCount(gm.Inventory?.CurrentCount ?? 0);
            UpdateCustomerStatus();

            if (dayProgressBar != null)
                dayProgressBar.value = gm.Clock?.DayProgress ?? 0f;
        }

        // ── Button Handlers ────────────────────────────────────────────────

        private void TogglePause()
        {
            var clock = GameManager.Instance?.Clock;
            if (clock == null) return;

            if (clock.IsRunning)
            {
                clock.PauseDay();
                if (pauseButton != null)
                {
                    var text = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null) text.text = "▶ Play";
                }
            }
            else
            {
                clock.ResumeDay();
                if (pauseButton != null)
                {
                    var text = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null) text.text = "⏸ Pause";
                }
            }
        }

        private void CycleSpeed()
        {
            var clock = GameManager.Instance?.Clock;
            if (clock == null) return;

            float newScale = clock.TimeScale switch
            {
                <= 1f => 2f,
                <= 2f => 3f,
                _ => 1f
            };

            clock.SetTimeScale(newScale);

            if (speedButton != null)
            {
                var text = speedButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = $"{newScale}x";
            }
        }

        private void EndDay()
        {
            GameManager.Instance?.Clock?.ForceEndDay();
        }
    }
}
