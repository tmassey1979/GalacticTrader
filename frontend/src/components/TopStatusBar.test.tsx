import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { TopStatusBar } from "./TopStatusBar";

describe("TopStatusBar", () => {
  it("renders status cards with metric labels and values", () => {
    render(
      <TopStatusBar
        metrics={[
          {
            id: "player",
            label: "Player",
            value: "Commander Orin Vale",
            tooltip: "Player identity",
            trend: [10, 20, 30, 40, 50]
          },
          {
            id: "net-worth",
            label: "Net Worth",
            value: "$1,200,000",
            tooltip: "Asset value",
            trend: [50, 45, 55, 60, 64]
          }
        ]}
      />
    );

    expect(screen.getByLabelText("top-status-bar")).toBeInTheDocument();
    expect(screen.getByText("Player")).toBeInTheDocument();
    expect(screen.getByText("Commander Orin Vale")).toBeInTheDocument();
    expect(screen.getByText("Net Worth")).toBeInTheDocument();
  });
});
