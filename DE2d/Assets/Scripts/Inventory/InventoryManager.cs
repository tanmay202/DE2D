// ============================================================================
// InventoryManager.cs — Player Inventory System
// Responsibility: Add/remove devices. Query stock. Track capacity.
// Dependencies: GameManager, DeviceInstance
// Scene Placement: Managers/InventoryManager
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeviceEmpire.Inventory
{
    /// <summary>
    /// Manages the player's device inventory. V1 = simple list with capacity cap.
    /// V2+ adds shelf slots, backroom/frontroom split, warehouse storage.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        // ── Configuration ──────────────────────────────────────────────────
        [Header("Capacity")]
        [Tooltip("Maximum devices the player can hold. V1 = 10 (folding table).")]
        [SerializeField] private int maxCapacity = 10;

        // ── Internal State ─────────────────────────────────────────────────
        private List<DeviceInstance> _stock = new();

        // ── Public Access ──────────────────────────────────────────────────
        /// <summary>Read-only view of current stock.</summary>
        public IReadOnlyList<DeviceInstance> Stock => _stock.AsReadOnly();

        /// <summary>Number of free inventory slots.</summary>
        public int FreeSlots => maxCapacity - _stock.Count;

        /// <summary>Current number of devices in stock.</summary>
        public int CurrentCount => _stock.Count;

        /// <summary>Maximum capacity.</summary>
        public int MaxCapacity => maxCapacity;

        /// <summary>Whether the inventory is full.</summary>
        public bool IsFull => _stock.Count >= maxCapacity;

        // ── Events ─────────────────────────────────────────────────────────
        /// <summary>Fired when a device is added. Param = the added device.</summary>
        public event Action<DeviceInstance> OnDeviceAdded;

        /// <summary>Fired when a device is removed (sold). Param = the removed device.</summary>
        public event Action<DeviceInstance> OnDeviceRemoved;

        /// <summary>Fired when inventory count changes. Param = new count.</summary>
        public event Action<int> OnInventoryChanged;

        // ── Add / Remove ───────────────────────────────────────────────────

        /// <summary>
        /// Add a device to inventory. Returns false if inventory is full.
        /// </summary>
        public bool AddDevice(DeviceInstance device)
        {
            if (device == null)
            {
                Debug.LogWarning("[InventoryManager] Attempted to add null device.");
                return false;
            }

            if (_stock.Count >= maxCapacity)
            {
                Debug.Log($"[InventoryManager] Inventory full ({_stock.Count}/{maxCapacity}). Cannot add {device.Data.DeviceName}.");
                return false;
            }

            _stock.Add(device);
            OnDeviceAdded?.Invoke(device);
            OnInventoryChanged?.Invoke(_stock.Count);

            Debug.Log($"[InventoryManager] Added: {device}. Stock: {_stock.Count}/{maxCapacity}");
            return true;
        }

        /// <summary>
        /// Remove a specific device from inventory. Returns false if not found.
        /// </summary>
        public bool RemoveDevice(DeviceInstance device)
        {
            if (device == null) return false;

            bool removed = _stock.Remove(device);
            if (removed)
            {
                OnDeviceRemoved?.Invoke(device);
                OnInventoryChanged?.Invoke(_stock.Count);
                Debug.Log($"[InventoryManager] Removed: {device}. Stock: {_stock.Count}/{maxCapacity}");
            }
            return removed;
        }

        // ── Queries ────────────────────────────────────────────────────────

        /// <summary>Get all devices of a specific category.</summary>
        public List<DeviceInstance> GetByCategory(DeviceCategory category)
        {
            return _stock.Where(d => d.Data.Category == category).ToList();
        }

        /// <summary>Get the first device matching a category, or null.</summary>
        public DeviceInstance GetFirstByCategory(DeviceCategory category)
        {
            return _stock.FirstOrDefault(d => d.Data.Category == category);
        }

        /// <summary>Check if any device of a category is in stock.</summary>
        public bool HasCategory(DeviceCategory category)
        {
            return _stock.Any(d => d.Data.Category == category);
        }

        /// <summary>Get a device by its unique instance ID.</summary>
        public DeviceInstance GetById(int instanceId)
        {
            return _stock.FirstOrDefault(d => d.InstanceId == instanceId);
        }

        /// <summary>
        /// Get total inventory value at current market prices.
        /// Useful for HUD display and daily reports.
        /// </summary>
        public float GetTotalStockValue(int currentDay)
        {
            return _stock.Sum(d => d.GetCurrentValue(currentDay));
        }

        /// <summary>
        /// Get total cost basis (what the player paid for all current stock).
        /// </summary>
        public float GetTotalCostBasis()
        {
            return _stock.Sum(d => d.PurchasePrice);
        }

        // ── Capacity Management ────────────────────────────────────────────

        /// <summary>
        /// Upgrade inventory capacity. Called during phase transitions.
        /// </summary>
        public void SetCapacity(int newCapacity)
        {
            maxCapacity = Mathf.Max(maxCapacity, newCapacity); // never shrink
            Debug.Log($"[InventoryManager] Capacity upgraded to {maxCapacity}.");
        }
    }
}
