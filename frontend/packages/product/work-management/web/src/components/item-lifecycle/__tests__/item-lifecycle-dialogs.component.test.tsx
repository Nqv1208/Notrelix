import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";

import { BoardTrashSheet } from "../board-trash-sheet";
import { ArchiveItemDialog } from "../archive-item-dialog";
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

describe("ArchiveItemDialog", () => {
  const impact = {
    subItems: 3,
    dependencies: { count: 2, keys: ["ERP-140", "ERP-141"] },
    checking: false,
  };
  const props = {
    copy: en,
    title: "Reconcile stock",
    impact,
    open: true,
    onCancel: vi.fn(),
    onConfirm: vi.fn(),
  };

  it("FUI[wm.item-lifecycle:archive] names the sub-items and the dependencies and counts every item it archives", () => {
    const onConfirm = vi.fn();
    render(
      <ArchiveItemDialog {...props} status="idle" onConfirm={onConfirm} />,
    );

    expect(
      screen.getByRole("dialog", { name: "Archive this item?" }),
    ).toBeTruthy();
    expect(screen.getByText("3 sub-items")).toBeTruthy();
    expect(screen.getByText("2 dependency links")).toBeTruthy();
    expect(screen.getByText(/ERP-140, ERP-141/)).toBeTruthy();
    expect(onConfirm).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Archive 4 items" }));
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });

  it("FUI[wm.item-lifecycle:archive-plain] says nothing about sub-items or links an item does not have", () => {
    render(
      <ArchiveItemDialog
        {...props}
        status="idle"
        impact={{
          subItems: 0,
          dependencies: { count: 0, keys: [] },
          checking: false,
        }}
      />,
    );
    expect(screen.queryByText(/sub-item/)).toBeNull();
    expect(screen.queryByText(/dependency link/)).toBeNull();
    expect(screen.getByRole("button", { name: "Archive item" })).toBeTruthy();
  });

  it("FUI[wm.item-lifecycle:archive-busy] cannot be dismissed or repeated while the write is out", () => {
    render(<ArchiveItemDialog {...props} status="busy" />);
    expect(screen.queryByRole("button", { name: "Close" })).toBeNull();
    expect(
      (screen.getByRole("button", { name: "Cancel" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    expect(
      (screen.getByRole("button", { name: "Archiving…" }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });

  const refusal = (
    status: "error" | "conflict" | "denied" | "gone",
    heading: string,
    canRetry: boolean,
  ) => {
    render(<ArchiveItemDialog {...props} status={status} />);
    expect(screen.getByRole("alert").textContent).toContain(heading);
    expect(Boolean(screen.queryByRole("button", { name: "Try again" }))).toBe(
      canRetry,
    );
  };

  it("FUI[wm.item-lifecycle:archive-error] explains a network failure in place and offers Try again", () => {
    refusal("error", "Couldn't archive", true);
  });

  it("FUI[wm.item-lifecycle:archive-conflict] says someone changed the item and offers Try again", () => {
    refusal("conflict", "Someone changed this item", true);
  });

  it("FUI[wm.item-lifecycle:archive-denied] says the person may not archive and offers no retry", () => {
    refusal("denied", "You can't archive this item", false);
  });

  it("FUI[wm.item-lifecycle:archive-gone] says the item is gone and offers no retry", () => {
    refusal("gone", "This item is no longer here", false);
  });

  it("FUI[wm.item-lifecycle:archive-links-pending] waits for the dependency count before it can be confirmed", () => {
    render(
      <ArchiveItemDialog
        {...props}
        status="idle"
        impact={{ ...impact, checking: true }}
      />,
    );
    expect(
      (
        screen.getByRole("button", {
          name: "Checking links…",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });
});
