import { LineChart } from "../components/LineChart";
import type { MarketPoint } from "../types";

const commodities = [
  { name: "SynthFiber", category: "Industrial", trend: "+3.2%", tariff: "4%" },
  { name: "CryoFuel", category: "Energy", trend: "-1.1%", tariff: "6%" },
  { name: "BioCatalyst", category: "Medical", trend: "+7.5%", tariff: "2%" },
  { name: "Contraband Mesh", category: "Restricted", trend: "+11.9%", tariff: "18%" }
];

type Props = {
  marketSeries: MarketPoint[];
};

export function TradeScreen({ marketSeries }: Props) {
  return (
    <section className="screen-grid">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Commodity Browser</h2>
          <button className="ghost-button">Apply Filters</button>
        </header>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Commodity</th>
                <th>Category</th>
                <th>Price Move</th>
                <th>Tariff</th>
              </tr>
            </thead>
            <tbody>
              {commodities.map((commodity) => (
                <tr key={commodity.name}>
                  <td>{commodity.name}</td>
                  <td>{commodity.category}</td>
                  <td>{commodity.trend}</td>
                  <td>{commodity.tariff}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </article>

      <article className="panel">
        <h3>Margin Preview</h3>
        <p className="hero-metric">$12,480</p>
        <p>Projected margin after tariff and route risk premium.</p>
        <button className="action-button">Confirm Trade</button>
      </article>

      <article className="panel wide">
        <h3>Supply/Demand Curve</h3>
        <LineChart points={marketSeries.map((point) => ({ x: point.t.slice(11, 16), y: point.demand * 100 }))} color="var(--accent-amber)" />
      </article>
    </section>
  );
}
