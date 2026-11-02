import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";
import { mainTableCopy } from "../main-table.copy";
import { DueEditor } from "../table-cell-editors";

const copy = mainTableCopy("en");

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

function editor(onPick: (day: string | null) => void | Promise<boolean>) {
  renderPureUi(
    <DueEditor
      copy={copy}
      dueAt={null}
      startAt="2026-10-15T00:00:00.000Z"
      label="Change due date of ERP-1"
      onPick={onPick}
    >
      <span>No due date</span>
    </DueEditor>,
  );
  fireEvent.click(screen.getByRole("button", { name: /Change due date/ }));
}

describe("DueEditor (the table's due calendar)", () => {
  it("offers no day before the start date", () => {
    editor(vi.fn());
    const days = [...screen.getByRole("grid").querySelectorAll("button")];
    expect(days.length).toBeGreaterThan(27);
    const disabled = days.filter((day) => day.hasAttribute("disabled"));
    expect(disabled.length).toBeGreaterThan(0);
    expect(disabled.length).toBeLessThan(days.length);
  });

  it("keeps the picker open with the refusal under the calendar and moves focus to it when the server refuses the range", async () => {
    const onPick = vi.fn(async () => false);
    editor(onPick);
    const enabled = [...screen.getByRole("grid").querySelectorAll("button")]
      .filter((day) => !day.hasAttribute("disabled"))
      .at(-1)!;
    fireEvent.click(enabled);

    const alert = await waitFor(() => {
      const found = document.querySelector<HTMLElement>("[role=alert]");
      expect(found).not.toBeNull();
      return found!;
    });
    expect(alert.textContent).toContain("before the start date");
    await waitFor(() => expect(document.activeElement === alert).toBe(true));
    expect(onPick).toHaveBeenCalledTimes(1);
  });

  it("closes after an accepted pick", async () => {
    editor(vi.fn(async () => true));
    const enabled = [...screen.getByRole("grid").querySelectorAll("button")]
      .filter((day) => !day.hasAttribute("disabled"))
      .at(-1)!;
    fireEvent.click(enabled);
    await waitFor(() =>
      expect(document.querySelector("[role=grid]")).toBeNull(),
    );
    expect(document.querySelector("[role=alert]")).toBeNull();
  });
});
