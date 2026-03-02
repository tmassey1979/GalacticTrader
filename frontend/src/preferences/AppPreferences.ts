import type { CsvExportTarget } from "./CsvExportTarget";

export type AppPreferences = {
  keyboardShortcutsEnabled: boolean;
  heartbeatIntervalMs: number;
  compactPanels: boolean;
  csvExportTarget: CsvExportTarget;
};
