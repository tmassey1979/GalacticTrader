import { useMemo, useState } from "react";
import { buildTerritoryCsv } from "../territory/territoryCsv";
import { downloadCsv } from "../territory/downloadCsv";
import type { TerritoryHeat } from "../territory/TerritoryZone";
import { territoryZones } from "../territory/territoryZones";

type TerritoryAction = "fleet" | "tax" | "incentive";

export function TerritoryScreen() {
  const [heatFilter, setHeatFilter] = useState<TerritoryHeat | "all">("all");
  const [selectedSystem, setSelectedSystem] = useState<string>(territoryZones[0]?.system ?? "");
  const [lastAction, setLastAction] = useState<string>("No action executed yet.");

  const visibleZones = useMemo(
    () => territoryZones.filter((zone) => heatFilter === "all" || zone.conflictHeat === heatFilter),
    [heatFilter]
  );

  function runAction(action: TerritoryAction) {
    const actionLabel = action === "fleet" ? "Protection fleet assigned" : action === "tax" ? "Tax policy adjusted" : "Trade incentive offered";
    setLastAction(`${actionLabel} in ${selectedSystem}.`);
  }

  function exportCsv() {
    const csv = buildTerritoryCsv(visibleZones);
    downloadCsv("territory-zones.csv", csv);
  }

  return (
    <section className="screen-grid" aria-label="territory-screen">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Controlled Systems</h2>
          <button className="ghost-button" onClick={exportCsv}>
            Export Territory CSV
          </button>
        </header>

        <div className="chip-row">
          <button className={`ghost-button ${heatFilter === "all" ? "active-filter" : ""}`} onClick={() => setHeatFilter("all")}>
            All
          </button>
          <button className={`ghost-button ${heatFilter === "low" ? "active-filter" : ""}`} onClick={() => setHeatFilter("low")}>
            Low Heat
          </button>
          <button className={`ghost-button ${heatFilter === "med" ? "active-filter" : ""}`} onClick={() => setHeatFilter("med")}>
            Medium Heat
          </button>
          <button className={`ghost-button ${heatFilter === "high" ? "active-filter" : ""}`} onClick={() => setHeatFilter("high")}>
            High Heat
          </button>
        </div>

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
              {visibleZones.map((zone) => (
                <tr key={zone.system} className={selectedSystem === zone.system ? "selected-row" : ""} onClick={() => setSelectedSystem(zone.system)}>
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
        <p className="kpi-hint">Selected: {selectedSystem}</p>
        <div className="stack">
          <button className="action-button" onClick={() => runAction("fleet")}>
            Assign Protection Fleet
          </button>
          <button className="ghost-button" onClick={() => runAction("tax")}>
            Adjust Taxation Policy
          </button>
          <button className="ghost-button" onClick={() => runAction("incentive")}>
            Offer Trade Incentive
          </button>
        </div>
      </article>

      <article className="panel">
        <h3>Territory Signals</h3>
        <ul className="flat-list">
          <li>Outer shipping lane piracy +12%</li>
          <li>Core system tariff compliance stable</li>
          <li>Draco Fringe diplomatic event pending</li>
        </ul>
        <p className="kpi-hint" aria-label="territory-last-action">
          {lastAction}
        </p>
      </article>
    </section>
  );
}
