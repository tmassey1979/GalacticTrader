const nodes = [
  { name: "Alpha", risk: 22, economy: 68 },
  { name: "Beta", risk: 37, economy: 54 },
  { name: "Gamma", risk: 64, economy: 82 },
  { name: "Delta", risk: 29, economy: 73 }
];

export function RoutePlanningScreen() {
  return (
    <section className="screen-grid">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Node Star Map</h2>
          <div className="chip-row">
            <span className="chip">Risk Overlay</span>
            <span className="chip">Economic Density</span>
            <span className="chip">Pirate Probability</span>
          </div>
        </header>
        <div className="map-mock">
          {nodes.map((node) => (
            <div key={node.name} className="map-node">
              <strong>{node.name}</strong>
              <small>Risk {node.risk}%</small>
              <small>Economy {node.economy}</small>
            </div>
          ))}
        </div>
      </article>

      <article className="panel">
        <h3>Autopilot Modes</h3>
        <div className="stack">
          <button className="ghost-button">Standard</button>
          <button className="ghost-button">Stealth</button>
          <button className="ghost-button">Aggressive</button>
        </div>
      </article>

      <article className="panel">
        <h3>Risk Simulation</h3>
        <p className="hero-metric">31%</p>
        <p>Projected interdiction probability for selected route.</p>
        <button className="action-button">Launch Autopilot</button>
      </article>
    </section>
  );
}
