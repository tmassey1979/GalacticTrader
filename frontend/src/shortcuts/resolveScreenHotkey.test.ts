import { describe, expect, it } from "vitest";
import { resolveScreenHotkey } from "./resolveScreenHotkey";

describe("resolveScreenHotkey", () => {
  it("maps supported keys to screens", () => {
    expect(resolveScreenHotkey("d")).toBe("dashboard");
    expect(resolveScreenHotkey("A")).toBe("analytics");
    expect(resolveScreenHotkey("s")).toBe("settings");
  });

  it("returns null for unsupported keys", () => {
    expect(resolveScreenHotkey("x")).toBeNull();
  });
});
