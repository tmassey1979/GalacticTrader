import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { TerritoryScreen } from "./TerritoryScreen";

describe("TerritoryScreen", () => {
  it("renders the core territory sections and controls", () => {
    render(<TerritoryScreen />);

    expect(screen.getByRole("heading", { name: "Controlled Systems" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Assign Protection Fleet" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Adjust Taxation Policy" })).toBeInTheDocument();
    expect(screen.getByText("Draco Fringe")).toBeInTheDocument();
  });
});
