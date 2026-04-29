import { Device } from "../models/Device.js";

export class Company {
  constructor(eventBus, shop, economy) {
    this.eventBus = eventBus;
    this.shop = shop;
    this.economy = economy;
    this.brandName = "";
    this.created = false;
    this.products = [];
  }

  get isUnlocked() {
    return this.economy.level >= 4 || this.economy.reputation >= 55;
  }

  createBrand(name) {
    if (!this.isUnlocked || this.created) return;
    const cost = 2200;
    if (!this.economy.canSpend(cost)) {
      this.eventBus.emit("log", { text: `Creating a company needs $${cost}.` });
      return;
    }
    this.economy.spend(cost, "Company registration");
    this.created = true;
    this.brandName = name.trim() || "PlayerTech";
    this.eventBus.emit("log", { text: `${this.brandName} is now registered as your own device company.` });
    this.eventBus.emit("state:changed");
  }

  designDevice(formData) {
    if (!this.created) return;
    const performance = Number(formData.performance);
    const battery = Number(formData.battery);
    const camera = Number(formData.camera);
    const price = Number(formData.price);
    const productionCost = Math.round(95 + performance * 4.2 + battery * 2.6 + camera * 3.8);
    const launchCost = productionCost * 5 + 750;
    if (!this.economy.canSpend(launchCost)) {
      this.eventBus.emit("log", { text: `Need $${launchCost} to produce the first batch.` });
      return;
    }
    const category = price >= 760 ? "premium" : price >= 340 ? "midrange" : "budget";
    const device = new Device({
      id: `player-${Date.now()}`,
      name: formData.name.trim() || "Prototype One",
      brand: this.brandName,
      category,
      cost: productionCost,
      price,
      baseDemand: Math.round((performance + battery + camera) / 3),
      popularity: Math.round(36 + this.economy.reputation * 0.6),
      specs: { performance, battery, camera },
      isPlayerMade: true,
    });
    this.economy.spend(launchCost, "Product launch");
    this.products.push(device);
    this.shop.addDevice(device, 5);
    this.eventBus.emit("log", { text: `${device.brand} ${device.name} launched with 5 units in stock.` });
    this.eventBus.emit("state:changed");
  }
}
