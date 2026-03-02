import { describe, expect, it } from "vitest";
import { buildEventsCsv } from "./buildEventsCsv";

describe("buildEventsCsv", () => {
  it("serializes realtime events to csv", () => {
    const csv = buildEventsCsv([
      {
        type: "connection.state",
        payload: {
          online: true
        }
      }
    ]);

    expect(csv).toContain("Type,Payload");
    expect(csv).toContain("connection.state");
    expect(csv).toContain("online");
  });
});
