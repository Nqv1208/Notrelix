import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { ItemDuePopover } from "../item-field-editors";

describe("ItemDuePopover", () => {
  it("opens the calendar on the month of the date it already holds", async () => {
    renderPureUi(
      <ItemDuePopover
        dueAt="2026-11-09T12:00:00.000Z"
        clearLabel="Clear due date"
        readOnly={false}
        onChange={vi.fn()}
      >
        <button type="button">09/11</button>
      </ItemDuePopover>,
    );

    fireEvent.click(screen.getByRole("button", { name: "09/11" }));

    expect(await screen.findByText(/November 2026/)).toBeTruthy();
  });
});
