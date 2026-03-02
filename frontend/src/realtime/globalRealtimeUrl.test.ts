import { describe, expect, it } from "vitest";
import { buildGlobalRealtimeUrl } from "./globalRealtimeUrl";

describe("buildGlobalRealtimeUrl", () => {
  it("builds websocket URL for the global channel", () => {
    const url = buildGlobalRealtimeUrl("ws://localhost:8080", "player-123");

    expect(url).toBe("ws://localhost:8080/api/communication/ws/global/global?playerId=player-123");
  });

  it("removes a trailing slash and encodes the player id", () => {
    const url = buildGlobalRealtimeUrl("wss://ops.example.com/", "player with space");

    expect(url).toBe("wss://ops.example.com/api/communication/ws/global/global?playerId=player%20with%20space");
  });
});
