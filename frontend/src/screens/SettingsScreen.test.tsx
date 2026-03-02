import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { SettingsScreen } from "./SettingsScreen";

describe("SettingsScreen", () => {
  it("renders settings controls and emits preference changes", () => {
    const onChange = vi.fn();
    const onExportSelectedCsv = vi.fn();

    render(
      <SettingsScreen
        preferences={{
          keyboardShortcutsEnabled: true,
          heartbeatIntervalMs: 6000,
          compactPanels: false,
          csvExportTarget: "analytics"
        }}
        onChange={onChange}
        onExportSelectedCsv={onExportSelectedCsv}
      />
    );

    fireEvent.click(screen.getByLabelText("Enable Keyboard Shortcuts"));
    fireEvent.change(screen.getByRole("combobox", { name: "Realtime Refresh Interval" }), { target: { value: "12000" } });
    fireEvent.click(screen.getByRole("button", { name: "Export Selected CSV" }));

    expect(onChange).toHaveBeenCalled();
    expect(onExportSelectedCsv).toHaveBeenCalled();
  });
});
