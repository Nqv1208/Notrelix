import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import type { Checklist } from "@notrelix/work-management-core";
import { ItemChecklists, type ItemChecklistsModel } from "../item-checklists";
import { checklistCopy, en, vi as viCopy } from "../item-checklists.copy";

const LISTS: Checklist[] = [
  {
    id: "l1",
    title: "Chuẩn bị go-live",
    version: 3,
    position: 1,
    items: [
      { id: "i1", title: "Sao lưu dữ liệu", isDone: true, position: 1 },
      { id: "i2", title: "Chạy thử UAT", isDone: false, position: 2 },
      { id: "i3", title: "Thông báo phòng ban", isDone: false, position: 3 },
    ],
  },
];

type Spied = ItemChecklistsModel & Record<string, ReturnType<typeof vi.fn>>;

function model(overrides: Partial<ItemChecklistsModel> = {}): Spied {
  return {
    checklists: LISTS,
    canEdit: true,
    canConvert: true,
    addList: vi.fn(),
    renameList: vi.fn(),
    deleteList: vi.fn(),
    moveList: vi.fn(),
    addItem: vi.fn(),
    renameItem: vi.fn(),
    toggleItem: vi.fn(),
    deleteItem: vi.fn(),
    moveItem: vi.fn(),
    convertItem: vi.fn(() => Promise.resolve()),
    undo: vi.fn(),
    retry: vi.fn(),
    discard: vi.fn(),
    dismissConflict: vi.fn(),
    failure: null,
    conflict: false,
    pendingDelete: null,
    busy: false,
    ...overrides,
  } as unknown as Spied;
}

const render = (m: ItemChecklistsModel, copy = en) =>
  renderPureUi(<ItemChecklists model={m} copy={copy} />);

const openMenu = (name: string) =>
  fireEvent.keyDown(screen.getByRole("button", { name }), { key: "Enter" });

describe("ItemChecklists", () => {
  it("shows one add row when there is no checklist, and grows it in place", () => {
    const m = model({ checklists: [] });
    render(m);
    fireEvent.click(screen.getByRole("button", { name: "Add checklist" }));
    const input = screen.getByRole("textbox", { name: "Checklist name" });
    expect(document.activeElement).toBe(input);
    fireEvent.change(input, { target: { value: "Launch" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(m.addList).toHaveBeenCalledWith("Launch");
    expect(
      screen.queryByRole("textbox", { name: "Checklist name" }),
    ).toBeNull();
  });

  it("shows x/y, a progress bar and ticks the checkbox", () => {
    const m = model();
    render(m);
    expect(screen.getAllByText("1/3").length).toBeGreaterThan(0);
    const bar = screen.getByRole("progressbar", { name: "1 of 3 done" });
    expect(bar.getAttribute("aria-valuenow")).toBe("1");
    const box = screen.getByRole("checkbox", {
      name: "Mark done: Chạy thử UAT",
    });
    expect(box.getAttribute("aria-checked")).toBe("false");
    fireEvent.click(box);
    expect(m.toggleItem).toHaveBeenCalledWith("l1", "i2");
  });

  it("adds items one after another: Enter adds and stays, Escape closes", () => {
    const m = model();
    render(m);
    fireEvent.click(screen.getByRole("button", { name: "Add item" }));
    const input = screen.getByRole("textbox", {
      name: "Checklist item",
    }) as HTMLInputElement;
    fireEvent.change(input, { target: { value: "Một" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(m.addItem).toHaveBeenLastCalledWith("l1", "Một");
    expect(input.value).toBe("");
    fireEvent.change(input, { target: { value: "Hai" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(m.addItem).toHaveBeenLastCalledWith("l1", "Hai");
    fireEvent.change(input, { target: { value: "bỏ" } });
    fireEvent.keyDown(input, { key: "Escape" });
    expect(m.addItem).toHaveBeenCalledTimes(2);
    expect(
      screen.queryByRole("textbox", { name: "Checklist item" }),
    ).toBeNull();
  });

  it("edits an item in place and deletes through the menu", () => {
    const m = model();
    render(m);
    openMenu("Options for Chạy thử UAT");
    fireEvent.click(screen.getByRole("menuitem", { name: "Edit" }));
    const input = screen.getByRole("textbox", {
      name: "Checklist item",
    }) as HTMLInputElement;
    expect(input.value).toBe("Chạy thử UAT");
    fireEvent.change(input, { target: { value: "Chạy thử UAT vòng 2" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(m.renameItem).toHaveBeenCalledWith(
      "l1",
      "i2",
      "Chạy thử UAT vòng 2",
    );

    openMenu("Options for Thông báo phòng ban");
    fireEvent.click(screen.getByRole("menuitem", { name: "Delete" }));
    expect(m.deleteItem).toHaveBeenCalledWith("l1", "i3");
  });

  it("converts an item to a sub-item, and says when that fails", async () => {
    const m = model({
      convertItem: vi.fn(() => Promise.reject(new Error("no"))),
    });
    render(m);
    openMenu("Options for Chạy thử UAT");
    fireEvent.click(
      screen.getByRole("menuitem", { name: "Convert to sub-item" }),
    );
    expect(m.convertItem).toHaveBeenCalledWith("l1", "i2");
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain(
      "The sub-item could not be created. The item is still here.",
    );
  });

  it("offers no conversion on a sub-item", () => {
    render(model({ canConvert: false }));
    openMenu("Options for Chạy thử UAT");
    expect(screen.getByRole("menuitem", { name: "Edit" })).toBeTruthy();
    expect(
      screen.queryByRole("menuitem", { name: "Convert to sub-item" }),
    ).toBeNull();
  });

  it("hides the done items on demand", () => {
    render(model());
    expect(screen.getByText("Sao lưu dữ liệu")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Hide done items" }));
    expect(screen.queryByText("Sao lưu dữ liệu")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Show 1 done item" }));
    expect(screen.getByText("Sao lưu dữ liệu")).toBeTruthy();
  });

  it("is read-only for a viewer: locked boxes, no add, no handles", () => {
    const m = model({ canEdit: false });
    render(m);
    const box = screen.getByRole("checkbox", {
      name: "Mark done: Chạy thử UAT",
    });
    expect(box.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(box);
    expect(m.toggleItem).not.toHaveBeenCalled();
    expect(screen.queryByRole("button", { name: "Add checklist" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Add item" })).toBeNull();
    expect(screen.queryByRole("button", { name: /^Move item/ })).toBeNull();
    expect(screen.getByText(en.readOnly)).toBeTruthy();
  });

  it("gives every item and list a keyboard drag handle when editable", () => {
    render(model());
    expect(
      screen.getByRole("button", { name: "Move item Chạy thử UAT" }),
    ).toBeTruthy();
    expect(
      screen.getByRole("button", { name: "Move checklist Chuẩn bị go-live" }),
    ).toBeTruthy();
  });

  it("keeps the text of a refused write and offers Retry and Discard", () => {
    const m = model({
      failure: {
        op: { kind: "add-item", checklistId: "l1", text: "Gửi biên bản" },
      },
    });
    render(m);
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain("“Gửi biên bản” was not saved");
    fireEvent.click(within(alert).getByRole("button", { name: "Retry" }));
    expect(m.retry).toHaveBeenCalled();
    fireEvent.click(within(alert).getByRole("button", { name: "Discard" }));
    expect(m.discard).toHaveBeenCalled();
  });

  it("says when only the cleanup of a conversion failed", () => {
    render(
      model({
        failure: {
          op: {
            kind: "convert-cleanup",
            checklistId: "l1",
            itemId: "i2",
            text: "Chạy thử UAT",
          },
        },
      }),
    );
    expect(screen.getByRole("alert").textContent).toContain(
      "Created the sub-item “Chạy thử UAT”, but the item is still in the checklist.",
    );
  });

  it("tells about a version conflict and lets the user undo a delete", () => {
    const m = model({
      conflict: true,
      pendingDelete: { kind: "item", text: "Chạy thử UAT" },
    });
    render(m);
    expect(screen.getByText(en.conflict)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Undo" }));
    expect(m.undo).toHaveBeenCalled();
  });

  it("reads in Vietnamese", () => {
    render(model({ checklists: [] }), checklistCopy("vi"));
    expect(screen.getByRole("button", { name: viCopy.addList })).toBeTruthy();
  });

  it("locks entries the server has not answered for", () => {
    render(
      model({
        checklists: [
          {
            ...LISTS[0]!,
            items: [
              { id: "pending-1", title: "Mới", isDone: false, position: 9 },
            ],
          },
        ],
      }),
    );
    expect(
      screen
        .getByRole("checkbox", { name: "Mark done: Mới" })
        .getAttribute("aria-disabled"),
    ).toBe("true");
  });
});
