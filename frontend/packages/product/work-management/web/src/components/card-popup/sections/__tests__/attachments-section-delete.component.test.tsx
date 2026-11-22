import { describe, expect, it, vi } from "vitest";
import { act, fireEvent, screen, waitFor } from "@notrelix/testing";
import { fileOf, openOptions, showSection, T } from "./attachments-fixtures";

// The linked pages of the item are read from the server; these tests are about the files only.
vi.mock("@notrelix/work-management-state", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@notrelix/work-management-state")>()),
  useItemLinkedPages: () => ({
    pages: [],
    hiddenCount: 0,
    isError: false,
    isLoading: false,
  }),
}));

const FILE = fileOf();
const OTHER = fileOf({ id: "file-2", name: "bang-gia.xlsx" });

async function confirmDelete(name: string) {
  openOptions(name);
  fireEvent.click(await screen.findByRole("menuitem", { name: T.deleteFile }));
  return screen.findByRole("alertdialog");
}

describe("item attachments: delete a file (UC-COL-B11)", () => {
  it("does not show the last refusal when Delete is opened on another file", async () => {
    const onDeleteFile = vi.fn(async () => false);
    showSection({
      data: { files: [FILE, OTHER] },
      callbacks: { onDeleteFile },
    });

    await confirmDelete("bao-cao.pdf");
    fireEvent.click(screen.getByRole("button", { name: T.deleteConfirm }));
    expect((await screen.findByRole("alert")).textContent).toBe(T.deleteFailed);
    fireEvent.click(screen.getByRole("button", { name: T.deleteKeep }));
    await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());

    await confirmDelete("bang-gia.xlsx");

    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("keeps the dialog busy until the delete answers, then reports a refusal", async () => {
    let answer!: (ok: boolean) => void;
    const onDeleteFile = vi.fn(
      () => new Promise<boolean>((resolve) => (answer = resolve)),
    );
    showSection({ data: { files: [FILE] }, callbacks: { onDeleteFile } });

    await confirmDelete("bao-cao.pdf");
    fireEvent.click(screen.getByRole("button", { name: T.deleteConfirm }));

    expect(
      (
        screen.getByRole("button", {
          name: T.deleteConfirm,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    expect(
      (screen.getByRole("button", { name: T.deleteKeep }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);

    await act(async () => answer(false));

    expect(screen.getByRole("alert").textContent).toBe(T.deleteFailed);
    expect(
      (
        screen.getByRole("button", {
          name: T.deleteConfirm,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(false);
    expect(screen.getByText("bao-cao.pdf")).toBeTruthy();
  });

  it("closes the dialog on success and lets the row leave when the list is read again", async () => {
    const onDeleteFile = vi.fn(async () => true);
    const view = showSection({
      data: { files: [FILE, OTHER] },
      callbacks: { onDeleteFile },
    });

    await confirmDelete("bao-cao.pdf");
    fireEvent.click(screen.getByRole("button", { name: T.deleteConfirm }));
    await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
    view.rerender({ files: [OTHER] });

    expect(onDeleteFile).toHaveBeenCalledWith(FILE);
    expect(screen.queryByText("bao-cao.pdf")).toBeNull();
    expect(screen.getByText("bang-gia.xlsx")).toBeTruthy();
  });

  it("moves focus to Add after the deleted row disappears", async () => {
    const view = showSection({ data: { files: [FILE, OTHER] } });

    await confirmDelete("bao-cao.pdf");
    fireEvent.click(screen.getByRole("button", { name: T.deleteConfirm }));
    await waitFor(() => expect(screen.queryByRole("alertdialog")).toBeNull());
    view.rerender({ files: [OTHER] });

    await waitFor(() =>
      expect(document.activeElement).toBe(
        screen.getByRole("button", { name: T.addLabel }),
      ),
    );
  });

  it("opens the options with the keyboard and confirms Delete from the keyboard", async () => {
    const { callbacks } = showSection({ data: { files: [FILE] } });

    const trigger = screen.getByRole("button", {
      name: "Options for bao-cao.pdf",
    });
    trigger.focus();
    fireEvent.keyDown(trigger, { key: "Enter" });
    const item = await screen.findByRole("menuitem", { name: T.deleteFile });
    fireEvent.keyDown(item, { key: "Enter" });
    const confirm = await screen.findByRole("button", {
      name: T.deleteConfirm,
    });
    fireEvent.click(confirm);

    await waitFor(() =>
      expect(callbacks.onDeleteFile).toHaveBeenCalledWith(FILE),
    );
  });
});
