import type { TerritoryZone } from "./TerritoryZone";

export function buildTerritoryCsv(zones: TerritoryZone[]): string {
  const header = ["System", "Control", "Protection", "EconomicOutput", "ConflictHeat"];
  const rows = zones.map((zone) =>
    [zone.system, zone.control, zone.protection, zone.output, zone.conflictHeat]
      .map(escapeCsvCell)
      .join(",")
  );

  return [header.join(","), ...rows].join("\n");
}

function escapeCsvCell(value: string): string {
  if (!value.includes(",") && !value.includes("\"") && !value.includes("\n")) {
    return value;
  }

  const escaped = value.replace(/"/g, "\"\"");
  return `"${escaped}"`;
}
