import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AnalyticsScreen } from "./AnalyticsScreen";

describe("AnalyticsScreen", () => {
  it("renders codex-required analytics metrics", () => {
    render(<AnalyticsScreen />);

    expect(screen.getByLabelText("analytics-screen")).toBeInTheDocument();
    expect(screen.getAllByText("Revenue / Hour").length).toBeGreaterThan(0);
    expect(screen.getAllByText("ROI / Ship").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Risk-Adjusted Return").length).toBeGreaterThan(0);
    expect(screen.getByText("Battle-to-Profit Ratio")).toBeInTheDocument();
    expect(screen.getByText("Market Share")).toBeInTheDocument();
    expect(screen.getByText("System Influence")).toBeInTheDocument();
  });
});
