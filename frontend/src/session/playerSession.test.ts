import { describe, expect, it, vi } from "vitest";
import { getOrCreatePlayerId } from "./playerSession";

describe("getOrCreatePlayerId", () => {
  it("returns the existing player id when one is stored", () => {
    const storage = {
      getItem: vi.fn().mockReturnValue("existing-id"),
      setItem: vi.fn()
    };

    const result = getOrCreatePlayerId(storage, () => "new-id");

    expect(result).toBe("existing-id");
    expect(storage.setItem).not.toHaveBeenCalled();
  });

  it("creates and stores a new id when no id exists", () => {
    const storage = {
      getItem: vi.fn().mockReturnValue(null),
      setItem: vi.fn()
    };

    const result = getOrCreatePlayerId(storage, () => "generated-id");

    expect(result).toBe("generated-id");
    expect(storage.setItem).toHaveBeenCalledWith("gt-player-id", "generated-id");
  });
});
