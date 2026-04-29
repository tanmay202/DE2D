export class UIManager {
  constructor(game) {
    this.game = game;
    this.nodes = {
      dayLabel: document.querySelector("#dayLabel"),
      moneyStat: document.querySelector("#moneyStat"),
      repStat: document.querySelector("#repStat"),
      popularityStat: document.querySelector("#popularityStat"),
      levelStat: document.querySelector("#levelStat"),
      nextDayBtn: document.querySelector("#nextDayBtn"),
      restockAllBtn: document.querySelector("#restockAllBtn"),
      customerQueue: document.querySelector("#customerQueue"),
      dailyStats: document.querySelector("#dailyStats"),
      eventLog: document.querySelector("#eventLog"),
      inventoryList: document.querySelector("#inventoryList"),
      supplierList: document.querySelector("#supplierList"),
      upgradeList: document.querySelector("#upgradeList"),
      marketingList: document.querySelector("#marketingList"),
      companyPanel: document.querySelector("#companyPanel"),
      companyStatus: document.querySelector("#companyStatus"),
      competitorList: document.querySelector("#competitorList"),
      trendBadge: document.querySelector("#trendBadge"),
      shopTierBadge: document.querySelector("#shopTierBadge"),
    };
  }

  bind() {
    this.nodes.nextDayBtn.addEventListener("click", () => this.game.runDay());
    this.nodes.restockAllBtn.addEventListener("click", () => this.game.restockBasics());
    this.nodes.supplierList.addEventListener("click", (event) => {
      const button = event.target.closest("[data-buy-device]");
      if (button) this.game.buyStock(button.dataset.buyDevice, Number(button.dataset.quantity));
    });
    this.nodes.inventoryList.addEventListener("click", (event) => {
      const button = event.target.closest("[data-price-device]");
      if (button) this.game.changePrice(button.dataset.priceDevice, Number(button.dataset.delta));
    });
    this.nodes.upgradeList.addEventListener("click", (event) => {
      const button = event.target.closest("[data-upgrade]");
      if (button) this.game.progression.buyUpgrade(button.dataset.upgrade);
    });
    this.nodes.marketingList.addEventListener("click", (event) => {
      const button = event.target.closest("[data-campaign]");
      if (button) this.game.marketing.runCampaign(button.dataset.campaign);
    });
    this.nodes.companyPanel.addEventListener("submit", (event) => {
      event.preventDefault();
      if (event.target.id === "brandForm") {
        this.game.company.createBrand(new FormData(event.target).get("brandName"));
      }
      if (event.target.id === "deviceForm") {
        this.game.company.designDevice(Object.fromEntries(new FormData(event.target)));
      }
    });
  }

  render() {
    const { economy, shop } = this.game;
    this.nodes.dayLabel.textContent = `Day ${this.game.day} - ${this.game.marketTrend} devices trending`;
    this.nodes.moneyStat.textContent = this.money(economy.money);
    this.nodes.repStat.textContent = Math.floor(economy.reputation);
    this.nodes.popularityStat.textContent = Math.floor(economy.popularity);
    this.nodes.levelStat.textContent = economy.level;
    this.nodes.trendBadge.textContent = `Trend: ${this.game.marketTrend}`;
    this.nodes.shopTierBadge.textContent = `Size ${shop.sizeLevel} / Interior ${shop.interiorLevel}`;
    this.renderDailyStats();
    this.renderCustomers();
    this.renderInventory();
    this.renderSuppliers();
    this.renderUpgrades();
    this.renderMarketing();
    this.renderCompany();
    this.renderCompetitors();
  }

  renderDailyStats() {
    const daily = this.game.economy.daily;
    this.nodes.dailyStats.innerHTML = [
      this.metric("Customers", daily.customers),
      this.metric("Units Sold", daily.unitsSold),
      this.metric("Revenue", this.money(daily.revenue)),
      this.metric("Profit", this.money(daily.profit), daily.profit >= 0 ? "positive" : "negative"),
      this.metric("Missed", daily.missedSales),
    ].join("");
  }

  renderCustomers() {
    if (this.game.customerQueue.length === 0) {
      this.nodes.customerQueue.innerHTML = `<div class="customer-card muted">Run a day to bring customers into the shop.</div>`;
      return;
    }
    this.nodes.customerQueue.innerHTML = this.game.customerQueue
      .map(
        (customer) => `
        <div class="customer-card">
          <strong>${customer.preference}</strong>
          <div class="small">Budget ${this.money(customer.budget)}</div>
          <div class="small">Patience ${customer.patience}%</div>
        </div>`,
      )
      .join("");
  }

  renderInventory() {
    const entries = this.game.shop.getInventoryList();
    if (entries.length === 0) {
      this.nodes.inventoryList.innerHTML = `<div class="item-card muted">No inventory yet. Buy stock from suppliers.</div>`;
      return;
    }
    this.nodes.inventoryList.innerHTML = entries.map((entry) => this.deviceCard(entry.device, entry.stock, true)).join("");
  }

  renderSuppliers() {
    this.nodes.supplierList.innerHTML = this.game.availableDevices
      .map((device) => {
        const locked = device.unlockLevel > this.game.economy.level || !this.game.shop.canSellCategory(device.category);
        return `
          <div class="item-card">
            ${this.deviceSummary(device)}
            <div class="item-actions">
              <button data-buy-device="${device.id}" data-quantity="1" ${locked ? "disabled" : ""}>Buy 1</button>
              <button data-buy-device="${device.id}" data-quantity="5" ${locked ? "disabled" : ""}>Buy 5</button>
            </div>
            ${locked ? `<div class="small warning">Requires level ${device.unlockLevel} and category access.</div>` : ""}
          </div>`;
      })
      .join("");
  }

  renderUpgrades() {
    this.nodes.upgradeList.innerHTML = this.game.progression.upgrades
      .map((upgrade) => {
        const complete = upgrade.isComplete?.() ?? false;
        const cost = upgrade.cost();
        return `
          <div class="item-card">
            <strong>${upgrade.name}</strong>
            <div class="small">${upgrade.description}</div>
            <div class="small">Level ${upgrade.getLevel()} - Cost ${this.money(cost)}</div>
            <button data-upgrade="${upgrade.id}" ${complete ? "disabled" : ""}>${complete ? "Complete" : "Buy Upgrade"}</button>
          </div>`;
      })
      .join("");
  }

  renderMarketing() {
    this.nodes.marketingList.innerHTML = this.game.marketing.campaigns
      .map(
        (campaign) => `
        <div class="item-card">
          <strong>${campaign.name}</strong>
          <div class="small">Cost ${this.money(campaign.cost)} - Gain ${campaign.minGain}-${campaign.maxGain} popularity</div>
          <div class="small">Backfire risk ${Math.round(campaign.risk * 100)}%</div>
          <button data-campaign="${campaign.id}">Launch</button>
        </div>`,
      )
      .join("");
  }

  renderCompany() {
    const company = this.game.company;
    this.nodes.companyStatus.textContent = company.created ? company.brandName : company.isUnlocked ? "Available" : "Locked";
    if (!company.isUnlocked) {
      this.nodes.companyPanel.innerHTML = `<div class="item-card muted">Reach level 4 or reputation 55 to register your own tech company.</div>`;
      return;
    }
    if (!company.created) {
      this.nodes.companyPanel.innerHTML = `
        <form id="brandForm" class="company-form">
          <div class="field-row">
            <label for="brandName">Brand name</label>
            <input id="brandName" name="brandName" value="PlayerTech" maxlength="24" />
          </div>
          <button class="primary-action">Register Company - $2,200</button>
        </form>`;
      return;
    }
    this.nodes.companyPanel.innerHTML = `
      <form id="deviceForm" class="company-form">
        <div class="field-row"><label for="name">Device name</label><input id="name" name="name" value="Origin One" maxlength="28" /></div>
        <div class="field-row"><label for="performance">Performance</label><input id="performance" name="performance" type="number" min="25" max="100" value="62" /></div>
        <div class="field-row"><label for="battery">Battery</label><input id="battery" name="battery" type="number" min="25" max="100" value="68" /></div>
        <div class="field-row"><label for="camera">Camera</label><input id="camera" name="camera" type="number" min="0" max="100" value="58" /></div>
        <div class="field-row"><label for="price">Selling price</label><input id="price" name="price" type="number" min="149" max="1599" value="549" /></div>
        <button class="primary-action">Launch Product</button>
      </form>
      <div class="small muted">Production cost is calculated from specs. First batch adds 5 units.</div>`;
  }

  renderCompetitors() {
    this.nodes.competitorList.innerHTML = this.game.competitorAI.competitors
      .map(
        (competitor) => `
        <div class="item-card">
          <strong>${competitor.name}</strong>
          <div class="small">Focus ${competitor.category}</div>
          <div class="small">Market pressure ${competitor.pressure}</div>
        </div>`,
      )
      .join("");
  }

  appendLog(text) {
    const entry = document.createElement("div");
    entry.className = "log-entry";
    entry.textContent = text;
    this.nodes.eventLog.prepend(entry);
    while (this.nodes.eventLog.children.length > 45) {
      this.nodes.eventLog.lastElementChild.remove();
    }
  }

  deviceCard(device, stock, withPricing) {
    return `
      <div class="item-card">
        ${this.deviceSummary(device)}
        <div class="small">Stock ${stock}</div>
        ${
          withPricing
            ? `<div class="item-actions">
                <button data-price-device="${device.id}" data-delta="-10">- $10</button>
                <button data-price-device="${device.id}" data-delta="10">+ $10</button>
              </div>`
            : ""
        }
      </div>`;
  }

  deviceSummary(device) {
    return `
      <strong>${device.brand} ${device.name}</strong>
      <div class="small">${device.category} - Cost ${this.money(device.cost)} - Price ${this.money(device.price)} - Margin ${device.marginPercent}%</div>
      <div class="spec-grid">
        <span>CPU ${device.specs.performance}</span>
        <span>Battery ${device.specs.battery}</span>
        <span>Camera ${device.specs.camera}</span>
      </div>`;
  }

  metric(label, value, className = "") {
    return `<div class="metric ${className}"><span>${label}</span><strong>${value}</strong></div>`;
  }

  money(value) {
    return `$${Math.round(value).toLocaleString("en-US")}`;
  }
}
