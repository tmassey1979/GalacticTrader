import { describe, expect, it } from "vitest";
import { buildAnalyticsCsv } from "./buildAnalyticsCsv";

describe("buildAnalyticsCsv", () => {
  it("serializes top status metrics to csv", () => {
    const csv = buildAnalyticsCsv([
      { id: "net-worth", label: "Net Worth", value: "$1,200,000", tooltip: "", trend: [1, 2, 3, 4, 5] }
    ]);

    expect(csv).toContain("Metric,Value");
    expect(csv).toContain("\"$1,200,000\"");
  });
});
