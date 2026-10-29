import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";

import { BoardTrashSheet } from "../board-trash-sheet";
import { DeleteItemDialog } from "../item-lifecycle-dialogs";
import { en } from "../item-lifecycle.copy";

describe("DeleteItemDialog", () => {
  it("FUI[wm.item-lifecycle:delete] asks before deleting and only deletes on confirm", async () => {
    const onConfirm = vi.fn();
    const onCancel = vi.fn();
    render(
      <DeleteItemDialog
        copy={en}
        title="Fix login"
        open
        onCancel={onCancel}
        onConfirm={onConfirm}
      />,
    );

    expect(screen.getByText("Delete “Fix login”?")).toBeTruthy();
    expect(onConfirm).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Delete item" }));
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });

  it("FUI[wm.item-lifecycle:delete-cancel] cancels without deleting", async () => {
    const onConfirm = vi.fn();
    const onCancel = vi.fn();
    render(
      <DeleteItemDialog
        copy={en}
        title="Fix login"
        open
        onCancel={onCancel}
        onConfirm={onConfirm}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onCancel).toHaveBeenCalled();
    expect(onConfirm).not.toHaveBeenCalled();
  });
});

describe("BoardTrashSheet", () => {
  it("FUI[wm.item-lifecycle:trash] lists deleted items with the last day they can come back and restores one", async () => {
    const onRestore = vi.fn();
    const row = {
      id: "i1",
      name: "Old task",
      itemKey: "ERP-9",
      restorableUntilDay: "2026-11-20",
    };
    render(
      <BoardTrashSheet
        copy={en}
        open
        rows={[row]}
        loading={false}
        failed={false}
        onRestore={onRestore}
        onClose={() => undefined}
      />,
    );

    expect(screen.getByText("Can be restored until 2026-11-20")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Restore" }));
    expect(onRestore).toHaveBeenCalledWith(row);
  });

  it("FUI[wm.item-lifecycle:trash-empty] shows an empty state", () => {
    render(
      <BoardTrashSheet
        copy={en}
        open
        rows={[]}
        loading={false}
        failed={false}
        onRestore={() => undefined}
        onClose={() => undefined}
      />,
    );

    expect(screen.getByText(en.trashEmpty)).toBeTruthy();
  });
});
