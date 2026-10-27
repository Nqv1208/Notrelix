import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";

import { BoardTrashSheet } from "../board-trash-sheet";
import { DeleteItemDialog, MoveItemDialog } from "../item-lifecycle-dialogs";
import { en } from "../item-lifecycle.copy";

describe("DeleteItemDialog", () => {
  it("asks before deleting and only deletes on confirm", async () => {
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

  it("cancels without deleting", async () => {
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

describe("MoveItemDialog", () => {
  const boards = [
    { id: "b1", title: "Roadmap" },
    { id: "b2", title: "Support" },
  ];

  it("moves to the board the member picked and not before", async () => {
    const onConfirm = vi.fn();
    render(
      <MoveItemDialog
        copy={en}
        title="Fix login"
        boards={boards}
        open
        onCancel={() => undefined}
        onConfirm={onConfirm}
      />,
    );

    const move = screen.getByRole("button", { name: "Move item" });
    expect((move as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(screen.getByRole("option", { name: "Support" }));
    fireEvent.click(move);
    expect(onConfirm).toHaveBeenCalledWith("b2");
  });

  it("filters the boards by name", async () => {
    render(
      <MoveItemDialog
        copy={en}
        title="Fix login"
        boards={boards}
        open
        onCancel={() => undefined}
        onConfirm={() => undefined}
      />,
    );

    fireEvent.change(screen.getByLabelText("Find a board"), {
      target: { value: "road" },
    });
    expect(screen.queryByRole("option", { name: "Support" })).toBeNull();
    expect(screen.getByRole("option", { name: "Roadmap" })).toBeTruthy();
  });

  it("says so when no other board accepts items", () => {
    render(
      <MoveItemDialog
        copy={en}
        title="Fix login"
        boards={[]}
        open
        onCancel={() => undefined}
        onConfirm={() => undefined}
      />,
    );

    expect(screen.getByText(en.moveEmpty)).toBeTruthy();
  });
});

describe("BoardTrashSheet", () => {
  it("lists deleted items with the last day they can come back and restores one", async () => {
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

  it("shows an empty state", () => {
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
