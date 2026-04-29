import { EventBus } from "../core/EventBus.js";
import { Customer } from "../models/Customer.js";
import { starterDevices } from "../data/devices.js";
import { Shop } from "../systems/Shop.js";
import { Company } from "../systems/Company.js";
import { CompetitorAI } from "../systems/CompetitorAI.js";
import { Marketing } from "../systems/Marketing.js";
import { Progression } from "../systems/Progression.js";
import { EconomyManager } from "./EconomyManager.js";
import { UIManager } from "./UIManager.js";

export class GameManager {
  constructor() {
    this.eventBus = new EventBus();
    this.day = 1;
    this.marketTrend = "budget";
    this.availableDevices = starterDevices;
    this.customerQueue = [];
    this.shop = new Shop(this.eventBus);
    this.economy = new EconomyManager(this.eventBus);
    this.company = new Company(this.eventBus, this.shop, this.economy);
    this.competitorAI = new CompetitorAI(this.eventBus);
    this.marketing = new Marketing(this.eventBus, this.economy);
    this.progression = new Progression(this.eventBus, this.shop, this.economy);
    this.ui = new UIManager(this);
  }

  start() {
    this.ui.bind();
    this.registerEvents();
    this.shop.buyStock(this.availableDevices[0], 5, this.economy);
    this.shop.buyStock(this.availableDevices[2], 6, this.economy);
    this.eventBus.emit("log", { text: "Shop opened. Buy stock, tune prices, and run each day." });
    this.ui.render();
  }

  registerEvents() {
    this.eventBus.on("state:changed", () => this.ui.render());
    this.eventBus.on("log", ({ text }) => this.ui.appendLog(text));
  }

  runDay() {
    this.economy.startDay();
    this.rollMarketTrend();
    this.competitorAI.simulateDay(this.day);
    this.spawnCustomers();
    this.processCustomers();
    this.applyDailyOverheads();
    this.eventBus.emit("log", {
      text: `Day ${this.day} closed: ${this.economy.daily.unitsSold}/${this.economy.daily.customers} customers bought devices. Profit ${this.formatMoney(this.economy.daily.profit)}.`,
    });
    this.day += 1;
    this.eventBus.emit("state:changed");
  }

  rollMarketTrend() {
    const trends = ["budget", "midrange", "premium", "accessory"];
    const previous = this.marketTrend;
    this.marketTrend = trends[Math.floor(Math.random() * trends.length)];
    if (previous !== this.marketTrend) {
      this.eventBus.emit("log", { text: `Market trend changed: ${this.marketTrend} products are hot today.` });
    }
  }

  spawnCustomers() {
    const base = this.shop.customerCapacity;
    const popularityBonus = Math.floor(this.economy.popularity / 12);
    const reputationBonus = Math.floor(this.economy.reputation / 28);
    const count = Math.max(2, base + popularityBonus + reputationBonus + Math.floor(Math.random() * 4 - 1));
    this.customerQueue = Array.from({ length: count }, (_, index) => new Customer(index + 1, this.marketTrend, this.economy.popularity));
    this.economy.daily.customers = this.customerQueue.length;
  }

  processCustomers() {
    this.customerQueue.forEach((customer) => {
      const beforeUnits = this.economy.daily.unitsSold;
      const pressurePenalty = this.competitorAI.getDemandPenalty(customer.preference);
      if (Math.random() < pressurePenalty) {
        this.economy.daily.missedSales += 1;
        return;
      }
      const soldDevice = this.shop.sellToCustomer(customer, this.economy, this.marketTrend);
      if (soldDevice) {
        this.eventBus.emit("log", { text: `Customer bought ${soldDevice.brand} ${soldDevice.name} for ${this.formatMoney(soldDevice.price)}.` });
      }
      if (this.economy.daily.unitsSold === beforeUnits) {
        this.economy.daily.missedSales += 1;
      }
    });
  }

  applyDailyOverheads() {
    const rent = 38 + this.shop.sizeLevel * 22 + this.shop.interiorLevel * 15;
    this.economy.spend(rent, "Daily rent and wages");
    const demandLift = this.economy.daily.unitsSold > 0 ? 0.35 : -0.65;
    this.economy.addPopularity(demandLift);
  }

  buyStock(deviceId, quantity) {
    const device = this.availableDevices.find((item) => item.id === deviceId);
    if (device) this.shop.buyStock(device, quantity, this.economy);
  }

  restockBasics() {
    ["nova-spark", "pulse-buds"].forEach((deviceId) => this.buyStock(deviceId, 3));
  }

  changePrice(deviceId, delta) {
    this.shop.updatePrice(deviceId, delta);
  }

  formatMoney(value) {
    return `$${Math.round(value).toLocaleString("en-US")}`;
  }
}
