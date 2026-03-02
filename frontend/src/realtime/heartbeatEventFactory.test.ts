import { describe, expect, it } from "vitest";
import { createHeartbeatMarketTick } from "./heartbeatEventFactory";

describe("createHeartbeatMarketTick", () => {
  it("creates a deterministic market tick event with injected providers", () => {
    const event = createHeartbeatMarketTick(
      () => 0.42,
      () => "2026-03-02T12:00:00.000Z"
    );

    expect(event).toEqual({
      type: "market.tick",
      payload: {
        volatility: 0.42,
        topCommodity: "SynthFiber",
        updatedAt: "2026-03-02T12:00:00.000Z"
      }
    });
  });
});
