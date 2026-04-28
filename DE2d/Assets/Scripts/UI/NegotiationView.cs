// ============================================================================
// NegotiationView.cs — Customer Negotiation Dialog
// Responsibility: Show customer's counter-offer, feedback text, and
//                 Accept/Reject buttons during negotiation phase.
// Dependencies: GameManager, ShopController
// Scene Placement: UI/NegotiationPanel (Canvas panel, shown during negotiation)
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeviceEmpire.Core;

namespace DeviceEmpire.UI
{
    /// <summary>
    /// Negotiation dialog shown when a customer makes a counter-offer.
    /// Displays the customer's reaction, their counter price, and Accept/Reject buttons.
    /// 
    /// FLOW:
    /// 1. ShopController fires OnTransactionResolved with NegotiationOpened/HardHaggle
    /// 2. This panel activates and shows the counter-offer
    /// 3. Player clicks Accept → ShopController.AcceptCounterOffer()
    /// 4. Player clicks Reject → ShopController.RejectCounterOffer()
    /// 5. Panel deactivates
    /// </summary>
    public class NegotiationView : MonoBehaviour
    {
        // ── UI References ──────────────────────────────────────────────────
        [Header("Customer Info")]
        [SerializeField] private TextMeshProUGUI customerNameText;
        [SerializeField] private TextMeshProUGUI customerTypeText;
        [SerializeField] private Image customerVisual;

        [Header("Negotiation Info")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private TextMeshProUGUI yourPriceText;
        [SerializeField] private TextMeshProUGUI counterOfferText;
        [SerializeField] private TextMeshProUGUI profitIfAcceptText;

        [Header("Patience Indicator")]
        [SerializeField] private TextMeshProUGUI patienceText;
        [SerializeField] private Slider patienceBar;

        [Header("Actions")]
        [SerializeField] private Button acceptButton;
        [SerializeField] private TextMeshProUGUI acceptButtonText;
        [SerializeField] private Button rejectButton;
        [SerializeField] private TextMeshProUGUI rejectButtonText;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm?.Shop != null)
            {
                gm.Shop.OnTransactionResolved += HandleTransactionResolved;
                gm.Shop.OnSaleCompleted += HandleSaleCompleted;
                gm.Shop.OnFlowReset += HidePanel;
            }

            if (acceptButton != null)
                acceptButton.onClick.AddListener(OnAcceptClicked);

            if (rejectButton != null)
                rejectButton.onClick.AddListener(OnRejectClicked);

            // Start hidden
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm?.Shop != null)
            {
                gm.Shop.OnTransactionResolved -= HandleTransactionResolved;
                gm.Shop.OnSaleCompleted -= HandleSaleCompleted;
                gm.Shop.OnFlowReset -= HidePanel;
            }
        }

        // ── Event Handlers ─────────────────────────────────────────────────

        private void HandleTransactionResolved(TransactionResult result)
        {
            switch (result.Outcome)
            {
                case TransactionOutcome.ImmediateAccept:
                    // Show quick "SOLD!" flash then hide
                    ShowImmediateAccept(result);
                    break;

                case TransactionOutcome.NegotiationOpened:
                case TransactionOutcome.HardHaggle:
                    ShowNegotiation(result);
                    break;

                case TransactionOutcome.CustomerLeft:
                    ShowCustomerLeft(result);
                    break;
            }
        }

        private void HandleSaleCompleted(float price, float profit)
        {
            // Brief delay then hide
            Invoke(nameof(HidePanel), 1.5f);
        }

        // ── Display Methods ────────────────────────────────────────────────

        private void ShowNegotiation(TransactionResult result)
        {
            gameObject.SetActive(true);

            var customer = GameManager.Instance?.Customers?.ActiveCustomer;
            var device = GameManager.Instance?.Shop?.SelectedDevice;

            // Customer info
            if (customerNameText != null && customer != null)
                customerNameText.text = customer.GetDisplayName();

            if (customerTypeText != null && customer != null)
                customerTypeText.text = $"Wants: {customer.WantedCategory}";

            if (customerVisual != null && customer?.Archetype?.Visual != null)
                customerVisual.sprite = customer.Archetype.Visual;

            // Feedback text (the customer's spoken reaction)
            if (feedbackText != null)
                feedbackText.text = result.Feedback;

            // Price comparison
            if (yourPriceText != null)
                yourPriceText.text = $"Your price: ${result.AskingPrice:F2}";

            if (counterOfferText != null)
            {
                counterOfferText.text = $"Their offer: ${result.CustomerCounterOffer:F2}";
                // Color the counter offer
                bool isHardHaggle = result.Outcome == TransactionOutcome.HardHaggle;
                counterOfferText.color = isHardHaggle
                    ? new Color(1f, 0.6f, 0.2f)   // Orange for hard haggle
                    : new Color(0.3f, 0.9f, 0.5f); // Green for normal negotiation
            }

            // Profit if accepted
            if (profitIfAcceptText != null && device != null)
            {
                float profit = result.CustomerCounterOffer - device.PurchasePrice;
                profitIfAcceptText.text = profit >= 0
                    ? $"Profit if accepted: +${profit:F2}"
                    : $"LOSS if accepted: -${Mathf.Abs(profit):F2}";
                profitIfAcceptText.color = profit >= 0
                    ? new Color(0.3f, 0.9f, 0.3f)
                    : new Color(0.9f, 0.3f, 0.3f);
            }

            // Patience indicator
            if (customer != null)
            {
                int remaining = customer.MaxNegotiationRounds - customer.NegotiationRounds;

                if (patienceText != null)
                    patienceText.text = $"Patience: {remaining} rounds left";

                if (patienceBar != null)
                {
                    patienceBar.maxValue = customer.MaxNegotiationRounds;
                    patienceBar.value = remaining;
                }
            }

            // Button text
            if (acceptButtonText != null)
                acceptButtonText.text = $"Accept ${result.CustomerCounterOffer:F2}";

            if (rejectButtonText != null)
            {
                bool isHardHaggle = result.Outcome == TransactionOutcome.HardHaggle;
                rejectButtonText.text = isHardHaggle ? "Reject (risky!)" : "Reject & Re-price";
            }

            // Enable buttons
            if (acceptButton != null) acceptButton.interactable = true;
            if (rejectButton != null) rejectButton.interactable = true;
        }

        private void ShowImmediateAccept(TransactionResult result)
        {
            gameObject.SetActive(true);

            if (feedbackText != null)
                feedbackText.text = result.Feedback;

            if (counterOfferText != null)
            {
                counterOfferText.text = $"SOLD for ${result.AskingPrice:F2}!";
                counterOfferText.color = new Color(0.3f, 1f, 0.4f);
            }

            if (yourPriceText != null)
                yourPriceText.text = "";

            if (profitIfAcceptText != null)
            {
                var device = GameManager.Instance?.Shop?.SelectedDevice;
                if (device != null)
                {
                    float profit = result.AskingPrice - device.PurchasePrice;
                    profitIfAcceptText.text = $"Profit: ${profit:F2}";
                }
            }

            // Disable buttons — sale already happened
            if (acceptButton != null) acceptButton.interactable = false;
            if (rejectButton != null) rejectButton.interactable = false;

            // Auto-hide after delay
            Invoke(nameof(HidePanel), 2.5f);
        }

        private void ShowCustomerLeft(TransactionResult result)
        {
            gameObject.SetActive(true);

            if (feedbackText != null)
                feedbackText.text = result.Feedback;

            if (counterOfferText != null)
            {
                counterOfferText.text = "NO SALE";
                counterOfferText.color = new Color(0.9f, 0.3f, 0.3f);
            }

            if (yourPriceText != null)
                yourPriceText.text = $"You asked: ${result.AskingPrice:F2}";

            if (profitIfAcceptText != null)
                profitIfAcceptText.text = "Customer left the shop.";

            if (acceptButton != null) acceptButton.interactable = false;
            if (rejectButton != null) rejectButton.interactable = false;

            // Auto-hide
            Invoke(nameof(HidePanel), 2.5f);
        }

        // ── Button Handlers ────────────────────────────────────────────────

        private void OnAcceptClicked()
        {
            GameManager.Instance?.Shop?.AcceptCounterOffer();
            HidePanel();
        }

        private void OnRejectClicked()
        {
            GameManager.Instance?.Shop?.RejectCounterOffer();
            HidePanel();
        }

        // ── Utility ────────────────────────────────────────────────────────

        private void HidePanel()
        {
            CancelInvoke(nameof(HidePanel));
            gameObject.SetActive(false);
        }
    }
}
