import type { ScreenKey } from "../types";

const hotkeyMap: Record<string, ScreenKey> = {
  d: "dashboard",
  t: "trade",
  r: "routes",
  f: "fleet",
  b: "battle",
  p: "reputation",
  y: "territory",
  a: "analytics",
  i: "intelligence",
  s: "settings"
};

export function resolveScreenHotkey(key: string): ScreenKey | null {
  return hotkeyMap[key.toLowerCase()] ?? null;
}
