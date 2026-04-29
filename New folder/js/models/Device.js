export class Device {
  constructor(config) {
    this.id = config.id;
    this.name = config.name;
    this.brand = config.brand;
    this.category = config.category;
    this.cost = config.cost;
    this.price = config.price;
    this.baseDemand = config.baseDemand;
    this.popularity = config.popularity;
    this.specs = { ...config.specs };
    this.unlockLevel = config.unlockLevel ?? 1;
    this.isPlayerMade = Boolean(config.isPlayerMade);
  }

  get valueScore() {
    const specs = this.specs.performance + this.specs.battery + this.specs.camera;
    return Math.round(specs * 0.55 + this.popularity * 0.45);
  }

  get profit() {
    return this.price - this.cost;
  }

  get marginPercent() {
    return Math.round((this.profit / this.price) * 100);
  }
}
