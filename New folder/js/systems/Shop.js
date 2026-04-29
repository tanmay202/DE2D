export class Shop {
  constructor(eventBus) {
    this.eventBus = eventBus;
    this.inventory = new Map();
    this.sizeLevel = 1;
    this.interiorLevel = 1;
    this.unlockedCategories = new Set(["budget", "midrange", "accessory"]);
  }

  get customerCapacity() {
    return 5 + this.sizeLevel * 3;
  }

  get conversionBonus() {
    return (this.interiorLevel - 1) * 0.055;
  }

  addDevice(device, stock = 0) {
    if (!this.inventory.has(device.id)) {
      this.inventory.set(device.id, { device, stock });
    }
    this.inventory.get(device.id).stock += stock;
  }

  canSellCategory(category) {
    return this.unlockedCategories.has(category);
  }

  getInventoryList() {
    return [...this.inventory.values()];
  }

  buyStock(device, quantity, economy) {
    if (!this.canSellCategory(device.category)) {
      return { ok: false, message: `${device.category} products are locked.` };
    }
    const totalCost = device.cost * quantity;
    if (!economy.canSpend(totalCost)) {
      return { ok: false, message: "Not enough cash for that stock order." };
    }
    economy.spend(totalCost, "Stock purchase");
    this.addDevice(device, quantity);
    this.eventBus.emit("log", { text: `Bought ${quantity}x ${device.brand} ${device.name} for $${totalCost}.` });
    return { ok: true };
  }

  updatePrice(deviceId, delta) {
    const entry = this.inventory.get(deviceId);
    if (!entry) return;
    const minPrice = Math.ceil(entry.device.cost * 1.05);
    entry.device.price = Math.max(minPrice, entry.device.price + delta);
    this.eventBus.emit("state:changed");
  }

  sellToCustomer(customer, economy, marketTrend) {
    const chosen = customer.chooseDevice(this.getInventoryList(), marketTrend);
    if (!chosen) return null;
    const conversionRoll = Math.random() + this.conversionBonus;
    if (conversionRoll < 0.16) return null;
    chosen.stock -= 1;
    economy.recordSale(chosen.device);
    return chosen.device;
  }
}
