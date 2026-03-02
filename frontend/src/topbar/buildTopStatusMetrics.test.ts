import { describe, expect, it } from "vitest";
import { buildTopStatusMetrics } from "./buildTopStatusMetrics";

describe("buildTopStatusMetrics", () => {
  it("builds all required strategic metrics", () => {
    const metrics = buildTopStatusMetrics({
      dashboard: {
        wealth: 1_250_000,
        fleetStrength: 742,
        reputation: 68,
        activeRoutes: 9,
        globalRisk: 37,
        updatedAt: "2026-03-02T00:00:00.000Z"
      },
      online: true,
      bufferedEvents: 4
    });

    expect(metrics).toHaveLength(9);
    expect(metrics.find((metric) => metric.id === "player")?.value).toBe("Commander Orin Vale");
    expect(metrics.find((metric) => metric.id === "active-routes")?.value).toBe("9");
    expect(metrics.find((metric) => metric.id === "global-economic-index")).toBeDefined();
  });

  it("emits an alert when realtime is offline", () => {
    const metrics = buildTopStatusMetrics({
      dashboard: null,
      online: false,
      bufferedEvents: 21
    });

    expect(metrics.find((metric) => metric.id === "alerts")?.value).toBe("1");
  });
});
