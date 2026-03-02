import type { AppPreferences } from "../preferences/AppPreferences";
import type { CsvExportTarget } from "../preferences/CsvExportTarget";

type Props = {
  preferences: AppPreferences;
  onChange: (next: Partial<AppPreferences>) => void;
  onExportSelectedCsv: () => void;
};

export function SettingsScreen({ preferences, onChange, onExportSelectedCsv }: Props) {
  return (
    <section className="screen-grid" aria-label="settings-screen">
      <article className="panel wide">
        <header className="panel-header">
          <h2>Power-User Settings</h2>
        </header>
        <div className="stack">
          <label className="settings-row">
            <span>Enable Keyboard Shortcuts</span>
            <input
              type="checkbox"
              checked={preferences.keyboardShortcutsEnabled}
              onChange={(event) => onChange({ keyboardShortcutsEnabled: event.target.checked })}
            />
          </label>

          <label className="settings-row">
            <span>Realtime Refresh Interval</span>
            <select
              value={preferences.heartbeatIntervalMs}
              onChange={(event) => onChange({ heartbeatIntervalMs: Number(event.target.value) })}
            >
              <option value={3000}>Fast (3s)</option>
              <option value={6000}>Standard (6s)</option>
              <option value={12000}>Low Traffic (12s)</option>
            </select>
          </label>

          <label className="settings-row">
            <span>Compact Data Density</span>
            <input
              type="checkbox"
              checked={preferences.compactPanels}
              onChange={(event) => onChange({ compactPanels: event.target.checked })}
            />
          </label>
        </div>
      </article>

      <article className="panel">
        <h3>CSV Export Controls</h3>
        <label className="settings-row">
          <span>Export Source</span>
          <select
            value={preferences.csvExportTarget}
            onChange={(event) => onChange({ csvExportTarget: event.target.value as CsvExportTarget })}
          >
            <option value="analytics">Analytics Data</option>
            <option value="events">Event Feed Data</option>
          </select>
        </label>
        <button className="action-button" onClick={onExportSelectedCsv}>
          Export Selected CSV
        </button>
      </article>

      <article className="panel">
        <h3>Shortcut Reference</h3>
        <ul className="flat-list">
          <li>D Dashboard</li>
          <li>T Trade</li>
          <li>R Routes</li>
          <li>F Fleet</li>
          <li>B Battle</li>
          <li>P Reputation</li>
          <li>Y Territory</li>
          <li>A Analytics</li>
          <li>I Intelligence</li>
          <li>S Settings</li>
        </ul>
      </article>
    </section>
  );
}
