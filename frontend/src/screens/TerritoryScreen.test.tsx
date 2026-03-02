import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { downloadCsv } from "../export/downloadCsv";
import { TerritoryScreen } from "./TerritoryScreen";

vi.mock("../export/downloadCsv", () => ({
  downloadCsv: vi.fn()
}));

describe("TerritoryScreen", () => {
  it("renders core territory sections and controls", () => {
    render(<TerritoryScreen />);

    expect(screen.getByRole("heading", { name: "Controlled Systems" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Assign Protection Fleet" })).toBeInTheDocument();
    expect(screen.getByText("Draco Fringe")).toBeInTheDocument();
  });

  it("filters visible rows by conflict heat", () => {
    render(<TerritoryScreen />);
    fireEvent.click(screen.getByRole("button", { name: "High Heat" }));

    expect(screen.getByText("Draco Fringe")).toBeInTheDocument();
    expect(screen.queryByText("Vega Reach")).not.toBeInTheDocument();
  });

  it("updates last action feedback for territory controls", () => {
    render(<TerritoryScreen />);
    fireEvent.click(screen.getByRole("button", { name: "Assign Protection Fleet" }));

    expect(screen.getByLabelText("territory-last-action")).toHaveTextContent("Protection fleet assigned");
  });

  it("triggers csv download for visible territory rows", () => {
    render(<TerritoryScreen />);
    fireEvent.click(screen.getByRole("button", { name: "Export Territory CSV" }));

    expect(downloadCsv).toHaveBeenCalled();
  });
});
