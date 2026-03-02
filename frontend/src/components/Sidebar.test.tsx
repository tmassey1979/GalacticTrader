import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { Sidebar } from "./Sidebar";

describe("Sidebar", () => {
  it("renders navigation and invokes onChange when a screen is selected", () => {
    const onChange = vi.fn();
    render(<Sidebar active="dashboard" onChange={onChange} />);

    fireEvent.click(screen.getByRole("button", { name: "Trade" }));
    fireEvent.click(screen.getByRole("button", { name: "Territory" }));

    expect(onChange).toHaveBeenCalledWith("trade");
    expect(onChange).toHaveBeenCalledWith("territory");
  });
});
