import type { TerritoryZone } from "./TerritoryZone";

export const territoryZones: TerritoryZone[] = [
  {
    system: "Vega Reach",
    control: "Merchant League",
    protection: "Escort Grid Alpha",
    output: "$1.8M/day",
    conflictHeat: "low"
  },
  {
    system: "Orion Gate",
    control: "Independent Council",
    protection: "Rapid Response Wing",
    output: "$1.2M/day",
    conflictHeat: "med"
  },
  {
    system: "Draco Fringe",
    control: "Disputed",
    protection: "Contract Patrol",
    output: "$0.7M/day",
    conflictHeat: "high"
  }
];
