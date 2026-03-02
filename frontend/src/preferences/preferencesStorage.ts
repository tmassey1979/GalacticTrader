import type { AppPreferences } from "./AppPreferences";
import { defaultPreferences } from "./defaultPreferences";

const PreferencesStorageKey = "gt-app-preferences";

export function loadAppPreferences(storage: Pick<Storage, "getItem"> = window.localStorage): AppPreferences {
  const raw = storage.getItem(PreferencesStorageKey);
  if (!raw) {
    return defaultPreferences;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<AppPreferences>;
    return {
      keyboardShortcutsEnabled: parsed.keyboardShortcutsEnabled ?? defaultPreferences.keyboardShortcutsEnabled,
      heartbeatIntervalMs: normalizeInterval(parsed.heartbeatIntervalMs),
      compactPanels: parsed.compactPanels ?? defaultPreferences.compactPanels,
      csvExportTarget: parsed.csvExportTarget ?? defaultPreferences.csvExportTarget
    };
  } catch {
    return defaultPreferences;
  }
}

export function saveAppPreferences(
  preferences: AppPreferences,
  storage: Pick<Storage, "setItem"> = window.localStorage
): void {
  storage.setItem(PreferencesStorageKey, JSON.stringify(preferences));
}

function normalizeInterval(value: number | undefined): number {
  if (!value || value <= 0) {
    return defaultPreferences.heartbeatIntervalMs;
  }

  return value;
}
