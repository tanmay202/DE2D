// ============================================================================
// ShopController.cs — Player Interaction Orchestrator
// Responsibility: Orchestrate the sell flow — select device, set price,
//                 submit to TransactionEngine, handle outcomes.
//                 This is the GLUE between player input and game logic.
// Dependencies: GameManager, TransactionEngine, DeviceInstance, CustomerInstance
// Scene Placement: Managers/ShopController (or Shop/ShopController)
// ============================================================================

using System;
using UnityEngine;
using DeviceEmpire.Inventory;
using DeviceEmpire.Customers;

namespace DeviceEmpire.Core
{
    /// <summary>
    /// Central controller for the shop interaction flow.
    /// 
    /// FLOW (V1):
    /// 1. Customer arrives (via CustomerSpawner event)
    /// 2. Player selects a device from inventory (SelectDevice)
    /// 3. Player sets a price (SetPrice / adjusts slider)
    /// 4. Player submits the offer (SubmitOffer)
    /// 5. TransactionEngine evaluates → result returned
    /// 6. If negotiation: player accepts/rejects counter-offer
    /// 7. Sale completes or customer leaves
    /// 8. Next customer spawns after interval
    /// 
    /// ALL UI calls go through this controller. Views never talk to
    /// TransactionEngine or managers directly.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────────
        [Header("Current Transaction State (Read Only)")]
        [SerializeField] private string _debugSelectedDevice = "None";
        [SerializeField] private float _debugAskingPrice = 0f;
        [SerializeField] private string _debugActiveCustomer = "None";

        private DeviceInstance _selectedDevice;
        private float _currentAskingPrice;
        private TransactionResult _lastResult;
        private bool _isInNegotiation = false;

        /// <summary>Currently selected device (null if none).</summary>
        public DeviceInstance SelectedDevice => _selectedDevice;

        /// <summary>Current asking price set by the player.</summary>
        public float CurrentAskingPrice => _currentAskingPrice;

        /// <summary>Last transaction result (for UI display).</summary>
        public TransactionResult LastResult => _lastResult;

        /// <summary>Whether we're currently in a negotiation phase.</summary>
        public bool IsInNegotiation => _isInNegotiation;

        /// <summary>Whether the player has selected a device.</summary>
        public bool HasDeviceSelected => _selectedDevice != null;

        /// <summary>Whether there's an active customer to sell to.</summary>
        public bool HasActiveCustomer => GameManager.Instance?.Customers?.ActiveCustomer != null;

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired when a device is selected. Param = selected device (null if deselected).</summary>
        public event Action<DeviceInstance> OnDeviceSelected;

        /// <summary>Fired when the asking price changes. Param = new price.</summary>
        public event Action<float> OnPriceChanged;

        /// <summary>Fired when a transaction is evaluated. Param = result.</summary>
        public event Action<TransactionResult> OnTransactionResolved;

        /// <summary>Fired when a sale completes successfully. Params = salePrice, profit.</summary>
        public event Action<float, float> OnSaleCompleted;

        /// <summary>Fired when a negotiation round starts. Param = counter-offer amount.</summary>
        public event Action<float> OnNegotiationStarted;

        /// <summary>Fired when the sale flow resets (customer left, sale done, etc.).</summary>
        public event Action OnFlowReset;

        // ── Lifecycle ──────────────────────────────────────────────────────
        private void OnEnable()
        {
            // Subscribe to customer events
            if (GameManager.Instance?.Customers != null)
            {
                GameManager.Instance.Customers.OnCustomerArrived += HandleCustomerArrived;
                GameManager.Instance.Customers.OnCustomerLeft += HandleCustomerLeft;
            }
        }

        private void Start()
        {
            // Delayed subscription
            if (GameManager.Instance?.Customers != null)
            {
                GameManager.Instance.Customers.OnCustomerArrived -= HandleCustomerArrived;
                GameManager.Instance.Customers.OnCustomerArrived += HandleCustomerArrived;
                GameManager.Instance.Customers.OnCustomerLeft -= HandleCustomerLeft;
                GameManager.Instance.Customers.OnCustomerLeft += HandleCustomerLeft;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance?.Customers != null)
            {
                GameManager.Instance.Customers.OnCustomerArrived -= HandleCustomerArrived;
                GameManager.Instance.Customers.OnCustomerLeft -= HandleCustomerLeft;
            }
        }

        // ── Step 1: Device Selection ───────────────────────────────────────

        /// <summary>
        /// Player selects a device from inventory to offer the customer.
        /// Called by InventoryPanelView when a device button is clicked.
        /// </summary>
        public void SelectDevice(DeviceInstance device)
        {
            if (device == null)
            {
                DeselectDevice();
                return;
            }

            _selectedDevice = device;
            _currentAskingPrice = device.PlayerSetPrice; // Default to player's preset price
            _isInNegotiation = false;

            // Debug display
            _debugSelectedDevice = device.Data.DeviceName;
            _debugAskingPrice = _currentAskingPrice;

            Debug.Log($"[ShopController] Selected: {device}. Default price: ${_currentAskingPrice:F2}");

            OnDeviceSelected?.Invoke(device);
            OnPriceChanged?.Invoke(_currentAskingPrice);
        }

        /// <summary>Deselect the current device.</summary>
        public void DeselectDevice()
        {
            _selectedDevice = null;
            _currentAskingPrice = 0f;
            _debugSelectedDevice = "None";
            _debugAskingPrice = 0f;

            OnDeviceSelected?.Invoke(null);
        }

        // ── Step 2: Price Setting ──────────────────────────────────────────

        /// <summary>
        /// Player adjusts the asking price. Called by PricingPanelView slider.
        /// </summary>
        public void SetPrice(float price)
        {
            _currentAskingPrice = Mathf.Max(0f, price);
            _currentAskingPrice = Mathf.Round(_currentAskingPrice * 100f) / 100f;
            _debugAskingPrice = _currentAskingPrice;

            // Also store on the device instance for persistence across selections
            if (_selectedDevice != null)
            {
                _selectedDevice.PlayerSetPrice = _currentAskingPrice;
            }

            OnPriceChanged?.Invoke(_currentAskingPrice);
        }

        // ── Step 3: Submit Offer ───────────────────────────────────────────

        /// <summary>
        /// Player submits their price offer to the current customer.
        /// This triggers the TransactionEngine evaluation.
        /// </summary>
        public void SubmitOffer()
        {
            // Validation
            var customer = GameManager.Instance?.Customers?.ActiveCustomer;
            if (customer == null)
            {
                Debug.LogWarning("[ShopController] No active customer to sell to.");
                return;
            }

            if (_selectedDevice == null)
            {
                Debug.LogWarning("[ShopController] No device selected.");
                return;
            }

            if (_currentAskingPrice <= 0f)
            {
                Debug.LogWarning("[ShopController] Price must be greater than zero.");
                return;
            }

            // Mark customer as being served
            customer.StartServing();

            // Calculate WTP and evaluate
            int currentDay = GameManager.Instance.Clock.CurrentDay;
            float wtp = TransactionEngine.CalculateWTP(_selectedDevice, customer, currentDay);

            Debug.Log($"[ShopController] Submitting offer: ${_currentAskingPrice:F2} " +
                      $"(Customer WTP: ${wtp:F2}, ratio: {_currentAskingPrice / wtp:F2})");

            _lastResult = TransactionEngine.Evaluate(_currentAskingPrice, wtp, customer);
            OnTransactionResolved?.Invoke(_lastResult);

            // Handle outcome
            switch (_lastResult.Outcome)
            {
                case TransactionOutcome.ImmediateAccept:
                    CompleteSale(_currentAskingPrice);
                    break;

                case TransactionOutcome.NegotiationOpened:
                case TransactionOutcome.HardHaggle:
                    StartNegotiation(_lastResult.CustomerCounterOffer);
                    break;

                case TransactionOutcome.CustomerLeft:
                    HandleCustomerWalkAway(customer);
                    break;
            }
        }

        // ── Step 4: Negotiation Handling ───────────────────────────────────

        /// <summary>
        /// Player accepts the customer's counter-offer.
        /// </summary>
        public void AcceptCounterOffer()
        {
            if (!_isInNegotiation || _lastResult == null) return;

            Debug.Log($"[ShopController] Player accepted counter-offer: ${_lastResult.CustomerCounterOffer:F2}");
            CompleteSale(_lastResult.CustomerCounterOffer);
        }

        /// <summary>
        /// Player rejects the counter-offer. Customer may leave or player can re-price.
        /// </summary>
        public void RejectCounterOffer()
        {
            if (!_isInNegotiation) return;

            var customer = GameManager.Instance?.Customers?.ActiveCustomer;
            if (customer == null) return;

            // Check if customer has patience for more rounds
            bool canContinue = customer.IncrementNegotiation();

            if (!canContinue)
            {
                Debug.Log("[ShopController] Customer out of patience — leaving.");
                customer.MarkDissatisfied();
                HandleCustomerWalkAway(customer);
                return;
            }

            // Customer stays — player can adjust price and re-submit
            _isInNegotiation = false;
            Debug.Log($"[ShopController] Counter rejected. Customer patience: " +
                      $"{customer.NegotiationRounds}/{customer.MaxNegotiationRounds}. " +
                      $"Adjust price and re-submit.");

            OnFlowReset?.Invoke();
        }

        /// <summary>
        /// Player dismisses the customer without attempting a sale.
        /// </summary>
        public void DismissCustomer()
        {
            var customer = GameManager.Instance?.Customers?.ActiveCustomer;
            if (customer != null)
            {
                customer.MarkLeft();
                GameManager.Instance.Economy?.RecordLostCustomer();
            }

            ResetFlow();
            GameManager.Instance?.Customers?.DismissCustomer();

            Debug.Log("[ShopController] Player dismissed the customer.");
        }

        // ── Internal Methods ───────────────────────────────────────────────

        private void StartNegotiation(float counterOffer)
        {
            _isInNegotiation = true;
            Debug.Log($"[ShopController] Negotiation opened. Counter-offer: ${counterOffer:F2}");
            OnNegotiationStarted?.Invoke(counterOffer);
        }

        private void CompleteSale(float salePrice)
        {
            if (_selectedDevice == null) return;

            var customer = GameManager.Instance.Customers.ActiveCustomer;
            float costOfGoods = _selectedDevice.PurchasePrice;
            float profit = salePrice - costOfGoods;

            // Update economy
            GameManager.Instance.AddCash(salePrice);
            GameManager.Instance.Economy?.RecordSale(salePrice, costOfGoods);

            // Remove device from inventory
            GameManager.Instance.Inventory.RemoveDevice(_selectedDevice);

            // Mark customer satisfied
            if (customer != null)
            {
                customer.MarkSatisfied();
            }

            Debug.Log($"[ShopController] *** SALE COMPLETE *** " +
                      $"Price: ${salePrice:F2}, Cost: ${costOfGoods:F2}, " +
                      $"Profit: ${profit:F2} ({(profit / costOfGoods * 100f):F1}% margin)");

            OnSaleCompleted?.Invoke(salePrice, profit);

            // Reset and dismiss customer
            ResetFlow();
            GameManager.Instance.Customers?.DismissCustomer();
        }

        private void HandleCustomerWalkAway(CustomerInstance customer)
        {
            customer.MarkDissatisfied();
            GameManager.Instance.Economy?.RecordLostCustomer();

            Debug.Log($"[ShopController] Customer #{customer.CustomerId} walked away. " +
                      $"Asked: ${_currentAskingPrice:F2}");

            ResetFlow();
            GameManager.Instance.Customers?.DismissCustomer();
        }

        private void ResetFlow()
        {
            _selectedDevice = null;
            _currentAskingPrice = 0f;
            _lastResult = null;
            _isInNegotiation = false;
            _debugSelectedDevice = "None";
            _debugAskingPrice = 0f;

            OnFlowReset?.Invoke();
        }

        // ── Event Handlers ─────────────────────────────────────────────────

        private void HandleCustomerArrived(CustomerInstance customer)
        {
            _debugActiveCustomer = customer.GetDisplayName();
            Debug.Log($"[ShopController] Customer arrived: {customer.GetDisplayName()} " +
                      $"wants {customer.WantedCategory}");
        }

        private void HandleCustomerLeft()
        {
            _debugActiveCustomer = "None";
            if (_isInNegotiation)
            {
                ResetFlow();
            }
        }

        // ── Utility / Query Methods ────────────────────────────────────────

        /// <summary>
        /// Get a price assessment for the currently selected device at the current price.
        /// Used by PricingPanel to show feedback color/text.
        /// </summary>
        public PriceAssessment GetCurrentPriceAssessment()
        {
            if (_selectedDevice == null) return PriceAssessment.Unknown;
            int currentDay = GameManager.Instance?.Clock?.CurrentDay ?? 1;
            return TransactionEngine.AssessPrice(_currentAskingPrice, _selectedDevice, currentDay);
        }

        /// <summary>
        /// Get the expected profit at the current asking price.
        /// </summary>
        public float GetExpectedProfit()
        {
            if (_selectedDevice == null) return 0f;
            return _currentAskingPrice - _selectedDevice.PurchasePrice;
        }

        /// <summary>
        /// Get the expected margin percentage at the current asking price.
        /// </summary>
        public float GetExpectedMarginPercent()
        {
            if (_selectedDevice == null || _selectedDevice.PurchasePrice <= 0f) return 0f;
            return ((_currentAskingPrice - _selectedDevice.PurchasePrice) / _selectedDevice.PurchasePrice) * 100f;
        }
    }
}
