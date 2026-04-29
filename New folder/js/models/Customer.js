const preferenceWeights = {
  budget: { maxPrice: 320, valueBias: 1.25, premiumBias: 0.7 },
  midrange: { maxPrice: 650, valueBias: 1, premiumBias: 1 },
  premium: { maxPrice: 1200, valueBias: 0.85, premiumBias: 1.3 },
};

export class Customer {
  constructor(id, marketTrend, shopPopularity) {
    this.id = id;
    this.preference = Customer.pickPreference(marketTrend);
    this.budget = Customer.rollBudget(this.preference, shopPopularity);
    this.patience = Math.floor(55 + Math.random() * 45);
    this.randomness = 0.75 + Math.random() * 0.55;
  }

  static pickPreference(marketTrend) {
    const roll = Math.random();
    if (marketTrend === "budget" && roll < 0.52) return "budget";
    if (marketTrend === "premium" && roll < 0.44) return "premium";
    if (roll < 0.34) return "budget";
    if (roll < 0.78) return "midrange";
    return "premium";
  }

  static rollBudget(preference, shopPopularity) {
    const popularityBonus = Math.min(130, shopPopularity * 3);
    if (preference === "budget") return Math.round(160 + Math.random() * 210 + popularityBonus * 0.25);
    if (preference === "midrange") return Math.round(360 + Math.random() * 360 + popularityBonus * 0.55);
    return Math.round(760 + Math.random() * 620 + popularityBonus);
  }

  chooseDevice(inventory, trend) {
    let best = null;
    let bestScore = -Infinity;
    const weights = preferenceWeights[this.preference];

    inventory
      .filter((entry) => entry.stock > 0 && entry.device.price <= this.budget)
      .forEach((entry) => {
        const device = entry.device;
        const priceFit = Math.max(0, 1 - device.price / Math.max(1, this.budget));
        const trendBonus = device.category === trend ? 14 : 0;
        const premiumFit = device.price > 700 ? weights.premiumBias : weights.valueBias;
        const score = (device.valueScore * premiumFit + priceFit * 45 + trendBonus) * this.randomness;
        if (score > bestScore) {
          bestScore = score;
          best = entry;
        }
      });

    if (!best) return null;
    const priceResistance = best.device.price / this.budget;
    const conversionChance = Math.min(0.96, 0.38 + bestScore / 180 - priceResistance * 0.14);
    return Math.random() < conversionChance ? best : null;
  }
}
