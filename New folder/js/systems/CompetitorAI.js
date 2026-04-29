import { competitorConfigs } from "../data/competitors.js";

export class CompetitorAI {
  constructor(eventBus) {
    this.eventBus = eventBus;
    this.competitors = competitorConfigs.map((config) => ({
      ...config,
      lastLaunchDay: 0,
      pressure: Math.round(config.strength * 0.35),
    }));
  }

  getDemandPenalty(category) {
    return this.competitors
      .filter((competitor) => competitor.category === category)
      .reduce((sum, competitor) => sum + competitor.pressure * 0.0025, 0);
  }

  simulateDay(day) {
    this.competitors.forEach((competitor) => {
      const shouldLaunch = day - competitor.lastLaunchDay >= 4 && Math.random() < 0.42;
      if (!shouldLaunch) {
        competitor.pressure = Math.max(8, competitor.pressure - 1);
        return;
      }
      competitor.lastLaunchDay = day;
      const boost = Math.round(8 + Math.random() * 16);
      competitor.pressure = Math.min(100, competitor.pressure + boost);
      this.eventBus.emit("log", {
        text: `${competitor.name} released a new ${competitor.category} device. Demand pressure +${boost}.`,
      });
    });
  }
}
