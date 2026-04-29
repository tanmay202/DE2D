export class EconomyManager {
  constructor(eventBus) {
    this.eventBus = eventBus;
    this.money = 1800;
    this.reputation = 5;
    this.popularity = 8;
    this.level = 1;
    this.experience = 0;
    this.totalRevenue = 0;
    this.totalExpenses = 0;
    this.daily = this.createDailyStats();
  }

  createDailyStats() {
    return { revenue: 0, expenses: 0, profit: 0, unitsSold: 0, customers: 0, missedSales: 0 };
  }

  startDay() {
    this.daily = this.createDailyStats();
  }

  canSpend(amount) {
    return this.money >= amount;
  }

  spend(amount, reason = "Expense") {
    this.money -= amount;
    this.totalExpenses += amount;
    this.daily.expenses += amount;
    this.daily.profit -= amount;
    this.eventBus.emit("expense", { amount, reason });
    this.eventBus.emit("state:changed");
  }

  recordSale(device) {
    this.money += device.price;
    this.totalRevenue += device.price;
    this.daily.revenue += device.price;
    this.daily.profit += device.price - device.cost;
    this.daily.unitsSold += 1;
    this.reputation += device.isPlayerMade ? 2 : 1;
    this.popularity += 0.45;
    this.experience += Math.max(8, Math.round(device.profit / 5));
    this.checkLevelUp();
  }

  addPopularity(amount) {
    this.popularity = Math.max(0, this.popularity + amount);
    this.eventBus.emit("state:changed");
  }

  addReputation(amount) {
    this.reputation = Math.max(0, this.reputation + amount);
    this.checkLevelUp();
    this.eventBus.emit("state:changed");
  }

  checkLevelUp() {
    const needed = this.level * 100;
    if (this.experience >= needed) {
      this.experience -= needed;
      this.level += 1;
      this.reputation += 5;
      this.eventBus.emit("log", { text: `Reputation level increased to ${this.level}. New suppliers are watching.` });
    }
  }
}
