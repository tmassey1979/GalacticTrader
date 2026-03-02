import type { ScreenKey } from "../types";

const screens: Array<{ key: ScreenKey; label: string }> = [
  { key: "dashboard", label: "Dashboard" },
  { key: "trade", label: "Trade" },
  { key: "routes", label: "Route Planning" },
  { key: "fleet", label: "Fleet Ops" },
  { key: "battle", label: "Battle Lab" },
  { key: "reputation", label: "Reputation" },
  { key: "territory", label: "Territory" },
  { key: "analytics", label: "Analytics" },
  { key: "intelligence", label: "Market Intel" },
  { key: "settings", label: "Settings" }
];

type Props = {
  active: ScreenKey;
  onChange: (screen: ScreenKey) => void;
};

export function Sidebar({ active, onChange }: Props) {
  return (
    <aside className="sidebar">
      <div className="brand">
        <p className="brand-mark">GT</p>
        <p>Command Deck</p>
      </div>
      <nav>
        {screens.map((screen, index) => (
          <button
            key={screen.key}
            className={`nav-button ${active === screen.key ? "active" : ""}`}
            onClick={() => onChange(screen.key)}
            style={{ animationDelay: `${index * 70}ms` }}
          >
            {screen.label}
          </button>
        ))}
      </nav>
    </aside>
  );
}
