import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { Card } from "@notrelix/work-management-core";
import { en } from "../kanban-card.copy";
import {
  KanbanCardQuickEdit,
  type KanbanQuickEdit,
} from "../kanban-card-quick-edit";

const CARD = {
  id: "c1",
  title: "Old name",
  labels: [{ id: "l1", name: "Bug", color: "red" }],
  members: [],
} as unknown as Card;

function setup() {
  const edit: KanbanQuickEdit = {
    labels: [
      { id: "l1", name: "Bug" },
      { id: "l2", name: "Docs" },
    ],
    members: [{ userId: "u1", name: "Ha" }],
    onRename: vi.fn(),
    onToggleLabel: vi.fn(),
    onToggleMember: vi.fn(),
    onSetDates: vi.fn(),
    onOpen: vi.fn(),
  };
  render(
    <article>
      <KanbanCardQuickEdit
        card={CARD}
        copy={en}
        edit={edit}
        rows={[]}
        open
        onOpenChange={() => {}}
        className=""
      />
    </article>,
  );
  return edit;
}

describe("KanbanCardQuickEdit", () => {
  it("renames with Save, only when the name changed", () => {
    const edit = setup();
    fireEvent.click(screen.getByRole("button", { name: en.quickSave }));
    expect(edit.onRename).not.toHaveBeenCalled();
    fireEvent.change(screen.getByRole("textbox"), {
      target: { value: "  New name " },
    });
    fireEvent.click(screen.getByRole("button", { name: en.quickSave }));
    expect(edit.onRename).toHaveBeenCalledWith(CARD, "New name");
  });

  it("toggles a label on and off from what the card has", () => {
    const edit = setup();
    fireEvent.click(screen.getByRole("button", { name: en.editLabels }));
    fireEvent.click(screen.getByRole("menuitemcheckbox", { name: "Docs" }));
    expect(edit.onToggleLabel).toHaveBeenCalledWith(CARD, "l2", true);
    fireEvent.click(screen.getByRole("menuitemcheckbox", { name: "Bug" }));
    expect(edit.onToggleLabel).toHaveBeenCalledWith(CARD, "l1", false);
  });

  it("refuses a due date before the start date", () => {
    const edit = setup();
    fireEvent.click(screen.getByRole("button", { name: en.editDates }));
    fireEvent.change(screen.getByLabelText(en.startDay), {
      target: { value: "2026-11-10" },
    });
    fireEvent.change(screen.getByLabelText(en.dueDay), {
      target: { value: "2026-11-01" },
    });
    expect(screen.getByRole("alert").textContent).toBe(en.datesBad);
    expect(
      (screen.getByRole("button", { name: en.saveDates }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(edit.onSetDates).not.toHaveBeenCalled();
  });

  it("opens the card in full", () => {
    const edit = setup();
    fireEvent.click(screen.getByRole("button", { name: en.openCard }));
    expect(edit.onOpen).toHaveBeenCalledWith("c1");
  });
});
