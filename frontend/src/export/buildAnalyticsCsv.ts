import type { TopStatusMetric } from "../topbar/TopStatusMetric";

export function buildAnalyticsCsv(metrics: TopStatusMetric[]): string {
  const header = "Metric,Value";
  const rows = metrics.map((metric) => `${escapeCell(metric.label)},${escapeCell(metric.value)}`);
  return [header, ...rows].join("\n");
}

function escapeCell(value: string): string {
  if (!value.includes(",") && !value.includes("\"") && !value.includes("\n")) {
    return value;
  }

  return `"${value.replace(/"/g, "\"\"")}"`;
}
