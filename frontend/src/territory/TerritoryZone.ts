export type TerritoryHeat = "low" | "med" | "high";

export type TerritoryZone = {
  system: string;
  control: string;
  protection: string;
  output: string;
  conflictHeat: TerritoryHeat;
};
