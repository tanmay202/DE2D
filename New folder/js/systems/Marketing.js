export class Marketing {
  constructor(eventBus, economy) {
    this.eventBus = eventBus;
    this.economy = economy;
    this.campaigns = [
      { id: "flyers", name: "Local Flyers", cost: 120, minGain: 4, maxGain: 9, risk: 0.05 },
      { id: "search-ads", name: "Search Ads", cost: 360, minGain: 10, maxGain: 21, risk: 0.15 },
      { id: "influencer", name: "Influencer Hype", cost: 850, minGain: 22, maxGain: 48, risk: 0.32 },
    ];
  }

  runCampaign(campaignId) {
    const campaign = this.campaigns.find((item) => item.id === campaignId);
    if (!campaign) return;
    if (!this.economy.canSpend(campaign.cost)) {
      this.eventBus.emit("log", { text: `Not enough money for ${campaign.name}.` });
      return;
    }
    this.economy.spend(campaign.cost, campaign.name);
    const backfire = Math.random() < campaign.risk;
    if (backfire) {
      const loss = Math.round(campaign.minGain * 0.65);
      this.economy.addPopularity(-loss);
      this.eventBus.emit("log", { text: `${campaign.name} missed the audience. Popularity -${loss}.` });
      return;
    }
    const gain = Math.round(campaign.minGain + Math.random() * (campaign.maxGain - campaign.minGain));
    this.economy.addPopularity(gain);
    this.eventBus.emit("log", { text: `${campaign.name} worked. Popularity +${gain}.` });
  }
}
