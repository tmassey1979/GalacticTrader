import type { AppPreferences } from "./AppPreferences";

export const defaultPreferences: AppPreferences = {
  keyboardShortcutsEnabled: true,
  heartbeatIntervalMs: 6000,
  compactPanels: false,
  csvExportTarget: "analytics"
};
