export function ReputationServicesScreen() {
  return (
    <section className="screen-grid">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Faction Standing Matrix</h2>
          <span className="chip">Alignment tracked</span>
        </header>
        <div className="matrix">
          <div className="matrix-row">
            <span>Civitas Union</span>
            <strong>Allied</strong>
            <span>Tax -20%</span>
          </div>
          <div className="matrix-row">
            <span>Free Traders Pact</span>
            <strong>Friendly</strong>
            <span>Priority docking</span>
          </div>
          <div className="matrix-row">
            <span>Shadow Cartel</span>
            <strong>Distrusted</strong>
            <span>High scan frequency</span>
          </div>
        </div>
      </article>

      <article className="panel">
        <h3>NPC Service Contracts</h3>
        <p>Brokerage, escort assignments, and tactical intel contracts.</p>
        <button className="action-button">Open Contracts</button>
      </article>

      <article className="panel">
        <h3>Alignment Tracker</h3>
        <p className="hero-metric">Lawful +68</p>
        <p>Legal insurance costs reduced, black-market contracts restricted.</p>
      </article>
    </section>
  );
}
