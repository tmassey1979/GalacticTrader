import type { RealtimeEvent } from "../types";

export function buildEventsCsv(events: RealtimeEvent[]): string {
  const header = "Type,Payload";
  const rows = events.map((event) => `${event.type},${escapeCell(JSON.stringify(event.payload))}`);
  return [header, ...rows].join("\n");
}

function escapeCell(value: string): string {
  if (!value.includes(",") && !value.includes("\"") && !value.includes("\n")) {
    return value;
  }

  return `"${value.replace(/"/g, "\"\"")}"`;
}
