import { describe, expect, it } from "vitest";
import { buildTerritoryCsv } from "./territoryCsv";

describe("buildTerritoryCsv", () => {
  it("serializes territory rows with csv header", () => {
    const csv = buildTerritoryCsv([
      {
        system: "Vega Reach",
        control: "Merchant League",
        protection: "Escort Grid Alpha",
        output: "$1.8M/day",
        conflictHeat: "low"
      }
    ]);

    expect(csv).toContain("System,Control,Protection,EconomicOutput,ConflictHeat");
    expect(csv).toContain("Vega Reach,Merchant League,Escort Grid Alpha,$1.8M/day,low");
  });

  it("escapes quoted and comma values", () => {
    const csv = buildTerritoryCsv([
      {
        system: "Orion, Gate",
        control: "Council \"Prime\"",
        protection: "Rapid Response",
        output: "$1.2M/day",
        conflictHeat: "med"
      }
    ]);

    expect(csv).toContain("\"Orion, Gate\"");
    expect(csv).toContain("\"Council \"\"Prime\"\"\"");
  });
});
