export class Progression {
  constructor(eventBus, shop, economy) {
    this.eventBus = eventBus;
    this.shop = shop;
    this.economy = economy;
    this.upgrades = [
      {
        id: "shop-size",
        name: "Expand Shop Size",
        description: "Adds more customer capacity each day.",
        getLevel: () => this.shop.sizeLevel,
        cost: () => 650 * this.shop.sizeLevel,
        buy: () => {
          this.shop.sizeLevel += 1;
        },
      },
      {
        id: "interior",
        name: "Improve Interior",
        description: "Raises customer conversion chance.",
        getLevel: () => this.shop.interiorLevel,
        cost: () => 500 * this.shop.interiorLevel,
        buy: () => {
          this.shop.interiorLevel += 1;
        },
      },
      {
        id: "premium-unlock",
        name: "Premium Display Area",
        description: "Unlocks premium devices.",
        getLevel: () => (this.shop.unlockedCategories.has("premium") ? 1 : 0),
        cost: () => 1400,
        buy: () => {
          this.shop.unlockedCategories.add("premium");
        },
        isComplete: () => this.shop.unlockedCategories.has("premium"),
      },
    ];
  }

  buyUpgrade(upgradeId) {
    const upgrade = this.upgrades.find((item) => item.id === upgradeId);
    if (!upgrade || upgrade.isComplete?.()) return;
    const price = upgrade.cost();
    if (!this.economy.canSpend(price)) {
      this.eventBus.emit("log", { text: `Need $${price} for ${upgrade.name}.` });
      return;
    }
    this.economy.spend(price, upgrade.name);
    upgrade.buy();
    this.eventBus.emit("log", { text: `${upgrade.name} purchased.` });
    this.eventBus.emit("state:changed");
  }
}
