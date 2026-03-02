import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { downloadCsv } from "../export/downloadCsv";
import { EventFeed } from "./EventFeed";

vi.mock("../export/downloadCsv", () => ({
  downloadCsv: vi.fn()
}));

describe("EventFeed", () => {
  it("filters events by selected type", () => {
    render(
      <EventFeed
        events={[
          { type: "market.tick", payload: { volatility: 0.5, topCommodity: "SynthFiber", updatedAt: "2026-03-02T00:00:00.000Z" } },
          { type: "fleet.status", payload: { activeShips: 5, convoyScore: 8 } }
        ]}
      />
    );

    fireEvent.change(screen.getByLabelText("event-filter"), { target: { value: "fleet.status" } });

    expect(screen.getByText("fleet.status")).toBeInTheDocument();
    expect(screen.queryByText("market.tick")).not.toBeInTheDocument();
  });

  it("exports filtered events as csv", () => {
    render(
      <EventFeed
        events={[
          { type: "connection.state", payload: { online: true } },
          { type: "connection.state", payload: { online: false } }
        ]}
      />
    );

    fireEvent.change(screen.getByLabelText("event-filter"), { target: { value: "connection.state" } });
    fireEvent.click(screen.getByRole("button", { name: "Export Events CSV" }));

    expect(downloadCsv).toHaveBeenCalled();
  });
});
