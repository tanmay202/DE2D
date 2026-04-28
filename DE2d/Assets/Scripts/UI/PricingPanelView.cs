// ============================================================================
// PricingPanelView.cs — Price Setting Panel
// Responsibility: Slider + input field for setting the asking price.
//                 Shows margin info, price assessment, and submit button.
// Dependencies: GameManager, ShopController
// Scene Placement: UI/PricingPanel (Canvas panel)
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeviceEmpire.Core;
using DeviceEmpire.Inventory;

namespace DeviceEmpire.UI
{
    /// <summary>
    /// Pricing panel — allows the player to set an asking price for the selected device.
    /// Shows real-time feedback on margin, market assessment, and expected profit.
    /// </summary>
    public class PricingPanelView : MonoBehaviour
    {
        // ── UI References ──────────────────────────────────────────────────
        [Header("Device Info")]
        [SerializeField] private TextMeshProUGUI deviceNameText;
        [SerializeField] private TextMeshProUGUI deviceConditionText;
        [SerializeField] private TextMeshProUGUI purchasePriceText;
        [SerializeField] private TextMeshProUGUI marketValueText;
        [SerializeField] private Image deviceIcon;

        [Header("Price Setting")]
        [SerializeField] private Slider priceSlider;
        [SerializeField] private TMP_InputField priceInputField;
        [SerializeField] private TextMeshProUGUI currentPriceText;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI marginText;
        [SerializeField] private TextMeshProUGUI profitText;
        [SerializeField] private TextMeshProUGUI assessmentText;
        [SerializeField] private Image assessmentIndicator;

        [Header("Actions")]
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI submitButtonText;
        [SerializeField] private Button dismissButton;

        [Header("No Device Selected State")]
        [SerializeField] private GameObject noDevicePanel;
        [SerializeField] private GameObject pricingContent;

        // ── Configuration ──────────────────────────────────────────────────
        [Header("Price Range")]
        [SerializeField] private float priceMultiplierMin = 0.1f;  // 10% of wholesale
        [SerializeField] private float priceMultiplierMax = 3.0f;  // 300% of wholesale

        // ── State ──────────────────────────────────────────────────────────
        private DeviceInstance _currentDevice;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm?.Shop != null)
            {
                gm.Shop.OnDeviceSelected += HandleDeviceSelected;
                gm.Shop.OnPriceChanged += HandlePriceChanged;
                gm.Shop.OnFlowReset += HandleFlowReset;
            }

            // Slider listener
            if (priceSlider != null)
                priceSlider.onValueChanged.AddListener(OnSliderChanged);

            // Input field listener
            if (priceInputField != null)
                priceInputField.onEndEdit.AddListener(OnInputFieldChanged);

            // Button listeners
            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);

            if (dismissButton != null)
                dismissButton.onClick.AddListener(OnDismissClicked);

            // Start with no device selected
            ShowNoDeviceState();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm?.Shop != null)
            {
                gm.Shop.OnDeviceSelected -= HandleDeviceSelected;
                gm.Shop.OnPriceChanged -= HandlePriceChanged;
                gm.Shop.OnFlowReset -= HandleFlowReset;
            }
        }

        // ── Event Handlers ─────────────────────────────────────────────────

        private void HandleDeviceSelected(DeviceInstance device)
        {
            if (device == null)
            {
                ShowNoDeviceState();
                return;
            }

            _currentDevice = device;
            ShowPricingState();
            PopulateDeviceInfo(device);
            SetupSliderRange(device);
            UpdateFeedback(device.PlayerSetPrice);
        }

        private void HandlePriceChanged(float newPrice)
        {
            UpdatePriceDisplay(newPrice);
            if (_currentDevice != null)
                UpdateFeedback(newPrice);
        }

        private void HandleFlowReset()
        {
            // Don't fully reset if we still have a device selected
            if (GameManager.Instance?.Shop?.SelectedDevice == null)
                ShowNoDeviceState();
        }

        // ── UI Update Methods ──────────────────────────────────────────────

        private void PopulateDeviceInfo(DeviceInstance device)
        {
            int currentDay = GameManager.Instance?.Clock?.CurrentDay ?? 1;

            if (deviceNameText != null)
                deviceNameText.text = device.Data.DeviceName;

            if (deviceConditionText != null)
                deviceConditionText.text = device.GetConditionLabelPlain();

            if (purchasePriceText != null)
                purchasePriceText.text = $"You paid: ${device.PurchasePrice:F2}";

            if (marketValueText != null)
            {
                float value = device.GetCurrentValue(currentDay);
                marketValueText.text = $"Market value: ${value:F2}";
            }

            if (deviceIcon != null && device.Data.Icon != null)
                deviceIcon.sprite = device.Data.Icon;
        }

        private void SetupSliderRange(DeviceInstance device)
        {
            if (priceSlider == null) return;

            float minPrice = device.Data.BaseWholesalePrice * priceMultiplierMin;
            float maxPrice = device.Data.BaseWholesalePrice * priceMultiplierMax;

            priceSlider.minValue = minPrice;
            priceSlider.maxValue = maxPrice;
            priceSlider.value = device.PlayerSetPrice;
        }

        private void UpdatePriceDisplay(float price)
        {
            if (currentPriceText != null)
                currentPriceText.text = $"${price:F2}";

            // Update slider without triggering callback loop
            if (priceSlider != null && Mathf.Abs(priceSlider.value - price) > 0.01f)
            {
                priceSlider.SetValueWithoutNotify(price);
            }

            // Update input field
            if (priceInputField != null)
            {
                priceInputField.SetTextWithoutNotify($"{price:F2}");
            }
        }

        private void UpdateFeedback(float price)
        {
            if (_currentDevice == null) return;

            // Profit calculation
            float profit = price - _currentDevice.PurchasePrice;
            float marginPercent = _currentDevice.PurchasePrice > 0
                ? (profit / _currentDevice.PurchasePrice) * 100f
                : 0f;

            if (profitText != null)
            {
                profitText.text = profit >= 0
                    ? $"Profit: +${profit:F2}"
                    : $"LOSS: -${Mathf.Abs(profit):F2}";
                profitText.color = profit >= 0
                    ? new Color(0.3f, 0.9f, 0.3f)
                    : new Color(0.9f, 0.3f, 0.3f);
            }

            if (marginText != null)
            {
                marginText.text = $"Margin: {marginPercent:F1}%";
                marginText.color = marginPercent >= 0
                    ? new Color(0.3f, 0.9f, 0.3f)
                    : new Color(0.9f, 0.3f, 0.3f);
            }

            // Price assessment
            var assessment = GameManager.Instance?.Shop?.GetCurrentPriceAssessment() ?? PriceAssessment.Unknown;
            if (assessmentText != null)
            {
                assessmentText.text = assessment switch
                {
                    PriceAssessment.WayUnderpriced => "WAY UNDERPRICED - You're losing money!",
                    PriceAssessment.Underpriced    => "Underpriced - Quick sell, low profit",
                    PriceAssessment.FairPrice      => "Fair Price - Good balance",
                    PriceAssessment.Overpriced     => "Overpriced - Risk of losing customer",
                    PriceAssessment.WayOverpriced  => "WAY OVERPRICED - Customer will leave!",
                    _ => "Select a device to see assessment"
                };

                Color assessColor = TransactionEngine.GetAssessmentColor(assessment);
                assessmentText.color = assessColor;
            }

            if (assessmentIndicator != null)
            {
                assessmentIndicator.color = TransactionEngine.GetAssessmentColor(assessment);
            }

            // Enable/disable submit based on customer presence
            bool canSubmit = GameManager.Instance?.Customers?.HasCustomer == true;
            if (submitButton != null)
                submitButton.interactable = canSubmit;

            if (submitButtonText != null)
                submitButtonText.text = canSubmit ? "OFFER TO CUSTOMER" : "No Customer";
        }

        // ── Input Handlers ─────────────────────────────────────────────────

        private void OnSliderChanged(float value)
        {
            float roundedPrice = Mathf.Round(value * 100f) / 100f;
            GameManager.Instance?.Shop?.SetPrice(roundedPrice);
        }

        private void OnInputFieldChanged(string text)
        {
            if (float.TryParse(text, out float price))
            {
                price = Mathf.Max(0f, price);
                GameManager.Instance?.Shop?.SetPrice(price);
            }
        }

        private void OnSubmitClicked()
        {
            GameManager.Instance?.Shop?.SubmitOffer();
        }

        private void OnDismissClicked()
        {
            GameManager.Instance?.Shop?.DismissCustomer();
        }

        // ── State Management ───────────────────────────────────────────────

        private void ShowNoDeviceState()
        {
            _currentDevice = null;
            if (noDevicePanel != null) noDevicePanel.SetActive(true);
            if (pricingContent != null) pricingContent.SetActive(false);
        }

        private void ShowPricingState()
        {
            if (noDevicePanel != null) noDevicePanel.SetActive(false);
            if (pricingContent != null) pricingContent.SetActive(true);
        }
    }
}
