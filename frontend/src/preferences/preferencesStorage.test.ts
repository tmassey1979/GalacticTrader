import { describe, expect, it, vi } from "vitest";
import { defaultPreferences } from "./defaultPreferences";
import { loadAppPreferences, saveAppPreferences } from "./preferencesStorage";

describe("preferencesStorage", () => {
  it("loads defaults when storage is empty", () => {
    const storage = {
      getItem: vi.fn().mockReturnValue(null)
    };

    const loaded = loadAppPreferences(storage);
    expect(loaded).toEqual(defaultPreferences);
  });

  it("saves and loads persisted preferences", () => {
    const savedState: { value: string | null } = { value: null };
    const storage = {
      getItem: vi.fn(() => savedState.value),
      setItem: vi.fn((_: string, value: string) => {
        savedState.value = value;
      })
    };

    saveAppPreferences(
      {
        keyboardShortcutsEnabled: false,
        heartbeatIntervalMs: 12000,
        compactPanels: true,
        csvExportTarget: "events"
      },
      storage
    );

    const loaded = loadAppPreferences(storage);
    expect(loaded.keyboardShortcutsEnabled).toBe(false);
    expect(loaded.heartbeatIntervalMs).toBe(12000);
    expect(loaded.compactPanels).toBe(true);
    expect(loaded.csvExportTarget).toBe("events");
  });
});
