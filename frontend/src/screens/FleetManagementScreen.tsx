const ships = [
  { name: "Guardian-4", class: "Escort", status: "Ready", crew: 11, efficiency: 84 },
  { name: "Atlas-2", class: "Hauler", status: "Route", crew: 8, efficiency: 73 },
  { name: "Nova-7", class: "Battleship", status: "Docked", crew: 16, efficiency: 91 }
];

export function FleetManagementScreen() {
  return (
    <section className="screen-grid">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Fleet List</h2>
          <button className="ghost-button">Bulk Actions</button>
        </header>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Ship</th>
                <th>Class</th>
                <th>Status</th>
                <th>Crew</th>
                <th>Efficiency</th>
              </tr>
            </thead>
            <tbody>
              {ships.map((ship) => (
                <tr key={ship.name}>
                  <td>{ship.name}</td>
                  <td>{ship.class}</td>
                  <td>{ship.status}</td>
                  <td>{ship.crew}</td>
                  <td>{ship.efficiency}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </article>

      <article className="panel">
        <h3>Upgrade Interface</h3>
        <p>Install hardpoint, shield, and sensor modules with slot validation.</p>
        <button className="action-button">Open Upgrade Bay</button>
      </article>

      <article className="panel">
        <h3>Crew Operations</h3>
        <p>Manage hiring, morale, and role assignments per vessel.</p>
        <button className="ghost-button">Manage Crew</button>
      </article>
    </section>
  );
}
