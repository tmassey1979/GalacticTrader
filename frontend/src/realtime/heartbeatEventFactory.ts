import type { RealtimeEvent } from "../types";

export function createHeartbeatMarketTick(
  random: () => number = Math.random,
  nowIso: () => string = () => new Date().toISOString()
): RealtimeEvent {
  return {
    type: "market.tick",
    payload: {
      volatility: random(),
      topCommodity: "SynthFiber",
      updatedAt: nowIso()
    }
  };
}
