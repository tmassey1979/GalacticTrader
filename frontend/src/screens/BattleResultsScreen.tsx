export function BattleResultsScreen() {
  return (
    <section className="screen-grid">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Battle Prediction</h2>
          <span className="chip">Insurance impact enabled</span>
        </header>
        <div className="duel-grid">
          <div>
            <h4>Your Fleet Rating</h4>
            <p className="hero-metric">842</p>
          </div>
          <div>
            <h4>Opponent Rating</h4>
            <p className="hero-metric">777</p>
          </div>
          <div>
            <h4>Win Probability</h4>
            <p className="hero-metric">62%</p>
          </div>
        </div>
      </article>

      <article className="panel">
        <h3>Modifier Breakdown</h3>
        <ul className="flat-list">
          <li>Environmental +8%</li>
          <li>Escort Coverage +6%</li>
          <li>Reputation Penalty -3%</li>
          <li>Subsystem Integrity +4%</li>
        </ul>
      </article>

      <article className="panel">
        <h3>Damage & Insurance</h3>
        <p>Projected hull loss: 19%</p>
        <p>Insurance payout estimate: $142,000</p>
      </article>
    </section>
  );
}
