const analyticsMetrics = [
  { label: "Revenue / Hour", value: "$184,200" },
  { label: "ROI / Ship", value: "18.4%" },
  { label: "Risk-Adjusted Return", value: "1.72" },
  { label: "Battle-to-Profit Ratio", value: "0.14" },
  { label: "Market Share", value: "7.8%" },
  { label: "System Influence", value: "12.1%" }
];

const performanceRows = [
  { ship: "Guardian-4", roi: "16.2%", riskReturn: "1.48", revenueHour: "$51,000" },
  { ship: "Atlas-2", roi: "19.9%", riskReturn: "1.84", revenueHour: "$68,700" },
  { ship: "Nova-7", roi: "21.5%", riskReturn: "1.96", revenueHour: "$64,500" }
];

export function AnalyticsScreen() {
  return (
    <section className="screen-grid" aria-label="analytics-screen">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Strategic Performance Analytics</h2>
          <button className="ghost-button">Export Analytics CSV</button>
        </header>
        <div className="kpi-grid">
          {analyticsMetrics.map((metric) => (
            <article key={metric.label} className="kpi-card">
              <p className="kpi-label">{metric.label}</p>
              <p className="kpi-value">{metric.value}</p>
            </article>
          ))}
        </div>
      </article>

      <article className="panel wide">
        <h3>Fleet Efficiency Breakdown</h3>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Ship</th>
                <th>ROI</th>
                <th>Risk-Adjusted Return</th>
                <th>Revenue / Hour</th>
              </tr>
            </thead>
            <tbody>
              {performanceRows.map((row) => (
                <tr key={row.ship}>
                  <td>{row.ship}</td>
                  <td>{row.roi}</td>
                  <td>{row.riskReturn}</td>
                  <td>{row.revenueHour}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </article>

      <article className="panel">
        <h3>Interpretation</h3>
        <p>
          Atlas-2 leads blended profitability while Nova-7 maintains highest risk-adjusted output. Current market share trend supports
          expansion into contested corridors.
        </p>
      </article>
    </section>
  );
}
