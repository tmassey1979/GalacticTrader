type TerritoryZone = {
  system: string;
  control: string;
  protection: string;
  output: string;
  conflictHeat: "low" | "med" | "high";
};

const territoryZones: TerritoryZone[] = [
  {
    system: "Vega Reach",
    control: "Merchant League",
    protection: "Escort Grid Alpha",
    output: "$1.8M/day",
    conflictHeat: "low"
  },
  {
    system: "Orion Gate",
    control: "Independent Council",
    protection: "Rapid Response Wing",
    output: "$1.2M/day",
    conflictHeat: "med"
  },
  {
    system: "Draco Fringe",
    control: "Disputed",
    protection: "Contract Patrol",
    output: "$0.7M/day",
    conflictHeat: "high"
  }
];

export function TerritoryScreen() {
  return (
    <section className="screen-grid" aria-label="territory-screen">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Controlled Systems</h2>
          <button className="ghost-button">Open Conflict Heatmap</button>
        </header>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>System</th>
                <th>Control</th>
                <th>Protection Zone</th>
                <th>Economic Output</th>
                <th>Conflict Heat</th>
              </tr>
            </thead>
            <tbody>
              {territoryZones.map((zone) => (
                <tr key={zone.system}>
                  <td>{zone.system}</td>
                  <td>{zone.control}</td>
                  <td>{zone.protection}</td>
                  <td>{zone.output}</td>
                  <td>
                    <span className={`chip territory-${zone.conflictHeat}`}>{zone.conflictHeat.toUpperCase()}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </article>

      <article className="panel">
        <h3>Protection Actions</h3>
        <div className="stack">
          <button className="action-button">Assign Protection Fleet</button>
          <button className="ghost-button">Adjust Taxation Policy</button>
          <button className="ghost-button">Offer Trade Incentive</button>
        </div>
      </article>

      <article className="panel">
        <h3>Territory Signals</h3>
        <ul className="flat-list">
          <li>Outer shipping lane piracy +12%</li>
          <li>Core system tariff compliance stable</li>
          <li>Draco Fringe diplomatic event pending</li>
        </ul>
      </article>
    </section>
  );
}
