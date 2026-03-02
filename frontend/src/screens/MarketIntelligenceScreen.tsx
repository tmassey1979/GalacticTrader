export function MarketIntelligenceScreen() {
  return (
    <section className="screen-grid">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Regional Heatmap</h2>
          <button className="ghost-button">Export Report</button>
        </header>
        <div className="heatmap-mock">
          <span className="heat low">Outer Rim</span>
          <span className="heat med">Core Trade Ring</span>
          <span className="heat high">Smuggling Corridor</span>
          <span className="heat med">Industrial Belt</span>
        </div>
      </article>

      <article className="panel">
        <h3>Trade Flow Diagram</h3>
        <p>Synthetic flow model indicates east-bound fuel demand surge.</p>
      </article>

      <article className="panel">
        <h3>Top Traders</h3>
        <ul className="flat-list">
          <li>Arc Meridian - $4.2M</li>
          <li>Kestrel Prime - $3.7M</li>
          <li>Nova Exchange - $3.1M</li>
        </ul>
      </article>
    </section>
  );
}
