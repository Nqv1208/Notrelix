import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import {
  ERP_TABLE_ITEM,
  erpTableArchivedProps,
  erpTableEdgeProps,
  erpTableEditingProps,
  erpTableEmptyProps,
  erpTableNoResultsProps,
  erpTableProps,
  erpTableReadOnlyProps,
  erpTableResizedProps,
  erpTableSelectedProps,
} from "../../../../verification/table-fixtures";
import { mainTableCopy } from "../main-table.copy";
import {
  MainTableSurface,
  type MainTableSurfaceProps,
} from "../main-table-surface";

beforeAll(() => {
  // Radix popovers and menus measure and scroll; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
  Element.prototype.setPointerCapture = () => undefined;
});

const copy = mainTableCopy("vi");

function spied(props: MainTableSurfaceProps): MainTableSurfaceProps {
  return {
    ...props,
    onRowSelectedChange: vi.fn(),
    onSelectAllChange: vi.fn(),
    onClearSelection: vi.fn(),
    onGroupToggle: vi.fn(),
    onOpenItem: vi.fn(),
    onStatusChange: vi.fn(),
    onCreateItem: vi.fn(),
    onClearFilter: vi.fn(),
    onShowDone: vi.fn(),
    onBulkStatus: vi.fn(),
    onBulkDue: vi.fn(),
    onBulkArchive: vi.fn(),
    onColumnWidthChange: vi.fn(),
    onColumnWidthReset: vi.fn(),
  };
}

function renderTable(props: MainTableSurfaceProps) {
  const spy = spied(props);
  const view = renderPureUi(<MainTableSurface {...spy} />);
  return { props: spy, view };
}

/** jsdom has no PointerEvent: a mouse event with a pointer id stands in. */
function pointer(
  target: Element,
  type: "pointerdown" | "pointermove" | "pointerup" | "pointercancel",
  init: { button?: number; clientX?: number; pointerId: number },
) {
  const event = new MouseEvent(type, {
    bubbles: true,
    button: init.button ?? 0,
    clientX: init.clientX ?? 0,
  });
  Object.defineProperty(event, "pointerId", { value: init.pointerId });
  fireEvent(target, event);
}

const row = (key: string) =>
  document.querySelector<HTMLElement>(
    `[data-row-id="${ERP_TABLE_ITEM(key)}"]`,
  )!;

describe("MainTableSurface", () => {
  it("shows the three open groups with their counts and the default chip", () => {
    renderTable(erpTableProps());
    const groups = screen.getAllByRole("rowgroup");
    expect(groups.map((g) => g.getAttribute("aria-label"))).toEqual([
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
    ]);
    expect(
      groups.map((g) => g.querySelectorAll("[data-row-id]").length),
    ).toEqual([6, 3, 2]);
    expect(screen.getByText("Trạng thái: chưa xong")).toBeTruthy();
    expect(
      screen.getByText("11 công việc đang hiện · 5 ẩn bởi bộ lọc"),
    ).toBeTruthy();
    // Sorted by due date, soonest first, the item without a date last.
    const todo = within(groups[0]!);
    const keys = todo
      .getAllByRole("row")
      .filter((r) => r.hasAttribute("data-row-id"))
      .map((r) => r.querySelector(".font-mono")?.textContent);
    expect(keys).toEqual([
      "ERP-151",
      "ERP-147",
      "ERP-149",
      "ERP-150",
      "ERP-152",
      "ERP-153",
    ]);
  });

  it("flags the overdue item in the group header", () => {
    renderTable(erpTableProps());
    expect(screen.getByText(/· 1 quá hạn/)).toBeTruthy();
    expect(within(row("ERP-142")).getByText("Quá hạn 2 ngày")).toBeTruthy();
    expect(within(row("ERP-144")).getByText("Hôm nay")).toBeTruthy();
  });

  it("FUI[wm.table.main:select] selects rows and all rows with a mixed header checkbox", () => {
    const { props } = renderTable(erpTableSelectedProps());

    const header = screen.getByRole("checkbox", { name: "Chọn tất cả" });
    expect(header.getAttribute("aria-checked")).toBe("mixed");
    fireEvent.click(header);
    expect(props.onSelectAllChange).toHaveBeenCalledWith(true);

    const box = screen.getByRole("checkbox", { name: "Chọn ERP-151" });
    expect(box.getAttribute("aria-checked")).toBe("false");
    fireEvent.click(box);
    expect(props.onRowSelectedChange).toHaveBeenCalledWith(
      ERP_TABLE_ITEM("ERP-151"),
      true,
    );
    expect(
      screen
        .getByRole("checkbox", { name: "Chọn ERP-142" })
        .getAttribute("aria-checked"),
    ).toBe("true");
    expect(row("ERP-142").getAttribute("aria-selected")).toBe("true");
  });

  it("FUI[wm.table.main:bulk] shows the bulk bar with its count and runs its actions", () => {
    const { props } = renderTable(erpTableSelectedProps());
    const bar = screen.getByRole("toolbar", {
      name: "Thao tác với 2 công việc đã chọn",
    });
    expect(within(bar).getByText("2 đã chọn")).toBeTruthy();

    // Assign and move have no write behind them: present, inert, explained.
    for (const name of [/Giao cho/, /Chuyển board/]) {
      const button = within(bar).getByRole("button", { name });
      expect(button.getAttribute("aria-disabled")).toBe("true");
      expect(button.getAttribute("title")).toBe("Chưa khả dụng");
    }

    fireEvent.click(within(bar).getByRole("button", { name: "Lưu trữ" }));
    expect(props.onBulkArchive).toHaveBeenCalledOnce();

    fireEvent.click(within(bar).getByRole("button", { name: "Bỏ chọn (Esc)" }));
    expect(props.onClearSelection).toHaveBeenCalledOnce();

    fireEvent.keyDown(row("ERP-142"), { key: "Escape" });
    expect(props.onClearSelection).toHaveBeenCalledTimes(2);

    fireEvent.keyDown(within(bar).getByRole("button", { name: /Trạng thái/ }), {
      key: "Enter",
    });
    fireEvent.click(screen.getByRole("menuitem", { name: "Hoàn thành" }));
    expect(props.onBulkStatus).toHaveBeenCalledWith("status-done");
  });

  it("FUI[wm.table.main:archive] shows the archive notice with undo and close", () => {
    const onUndo = vi.fn();
    const onClose = vi.fn();
    renderTable({
      ...erpTableArchivedProps(),
      archivedNotice: { count: 2, onUndo, onClose },
    });
    const notice = screen.getByRole("status");
    expect(within(notice).getByText("Đã lưu trữ 2 việc")).toBeTruthy();
    fireEvent.click(within(notice).getByRole("button", { name: "Hoàn tác" }));
    expect(onUndo).toHaveBeenCalledOnce();
    fireEvent.click(
      within(notice).getByRole("button", { name: "Đóng thông báo" }),
    );
    expect(onClose).toHaveBeenCalledOnce();
  });

  it("FUI[wm.table.main:edit] opens the status list from the pill, filters it and chooses by key", () => {
    const { props } = renderTable(erpTableProps());
    fireEvent.click(
      within(row("ERP-144")).getByRole("button", {
        name: "Đổi trạng thái của ERP-144",
      }),
    );
    const list = screen.getByRole("listbox", { name: "Đổi trạng thái" });
    expect(
      within(list)
        .getAllByRole("option")
        .map((option) => option.textContent),
    ).toEqual(["Cần làm1", "Đang làm2", "Chờ duyệt3", "Hoàn thành4"]);
    expect(
      within(list)
        .getByRole("option", { name: /Đang làm/ })
        .getAttribute("aria-selected"),
    ).toBe("true");

    const search = screen.getByRole("combobox", { name: "Tìm trạng thái" });
    fireEvent.change(search, { target: { value: "cho duyet" } });
    expect(within(list).getAllByRole("option")).toHaveLength(1);
    fireEvent.keyDown(search, { key: "Enter" });
    expect(props.onStatusChange).toHaveBeenCalledWith(
      ERP_TABLE_ITEM("ERP-144"),
      "status-review",
    );
    expect(screen.queryByRole("listbox")).toBeNull();
  });

  it("chooses a status by its number and ignores the one it already has", () => {
    const { props } = renderTable(erpTableEditingProps());
    const search = screen.getByRole("combobox", { name: "Tìm trạng thái" });
    // The mockup opens ERP-144 (Đang làm) with Chờ duyệt highlighted.
    expect(
      screen
        .getByRole("option", { name: /Chờ duyệt/ })
        .className.includes("font-semibold"),
    ).toBe(true);
    fireEvent.keyDown(search, { key: "2" });
    expect(props.onStatusChange).not.toHaveBeenCalled();
    expect(screen.queryByRole("listbox")).toBeNull();
  });

  it("FUI[wm.table.main:keyboard] moves between rows, opens with Enter or Space and selects from the keyboard", () => {
    const { props } = renderTable(erpTableProps());
    const first = row("ERP-151");
    first.focus();
    fireEvent.keyDown(first, { key: "ArrowDown" });
    expect(document.activeElement).toBe(row("ERP-147"));
    fireEvent.keyDown(document.activeElement!, { key: "End" });
    expect(document.activeElement).toBe(row("ERP-140"));
    fireEvent.keyDown(document.activeElement!, { key: "Home" });
    expect(document.activeElement).toBe(first);

    fireEvent.keyDown(first, { key: "x" });
    expect(props.onRowSelectedChange).toHaveBeenCalledWith(
      ERP_TABLE_ITEM("ERP-151"),
      true,
    );
    fireEvent.keyDown(first, { key: " " });
    expect(props.onOpenItem).toHaveBeenCalledWith(ERP_TABLE_ITEM("ERP-151"));
    fireEvent.keyDown(first, { key: "Enter" });
    expect(props.onOpenItem).toHaveBeenCalledTimes(2);
    // The row owns Enter only when it has the focus itself: Enter on the
    // status pill belongs to the pill.
    expect(screen.queryByRole("listbox")).toBeNull();
  });

  it("FUI[wm.table.main:open] opens the item from the row and its title, not from the pill or the checkbox", () => {
    const { props } = renderTable(erpTableProps());
    const target = row("ERP-151");
    expect(
      within(target).queryByRole("button", { name: /^Mở chi tiết/ }),
    ).toBeNull();
    fireEvent.click(
      within(target).getByRole("button", { name: /Cấu hình quyền truy cập/ }),
    );
    expect(props.onOpenItem).toHaveBeenCalledTimes(1);
    fireEvent.click(within(target).getByRole("cell", { name: "ERP-151" }));
    expect(props.onOpenItem).toHaveBeenCalledTimes(2);
    expect(props.onOpenItem).toHaveBeenCalledWith(ERP_TABLE_ITEM("ERP-151"));
    fireEvent.click(
      within(target).getByRole("button", {
        name: "Đổi trạng thái của ERP-151",
      }),
    );
    fireEvent.click(
      within(target).getByRole("checkbox", { name: "Chọn ERP-151" }),
    );
    expect(props.onOpenItem).toHaveBeenCalledTimes(2);
  });

  it("FUI[wm.table.main:hover] gives the row a left edge in its group colour that deepens on hover", () => {
    renderTable(erpTableProps());
    const target = row("ERP-151");
    const edge = target.querySelector<HTMLElement>("[data-row-accent]")!;
    expect(edge).toBeTruthy();
    expect(target.style.getPropertyValue("--row-accent")).not.toBe("");
    expect(edge.className).toContain("group-hover/row:bg-(--row-accent)");
    expect(edge.className).toContain(
      "color-mix(in_oklch,var(--row-accent)_22%",
    );
    expect(within(target).queryByText("Mở")).toBeNull();
  });

  it("FUI[wm.table.main:add-group] adds a status from a quiet row under the last group", async () => {
    const onSubmit = vi.fn(async () => undefined);
    const base = erpTableProps();
    renderTable({
      ...base,
      addGroup: { ...base.addGroup!, onSubmit },
    });
    const last = screen.getAllByRole("rowgroup").at(-1)!;
    const control = screen.getByRole("button", { name: "Thêm trạng thái" });
    // The control follows the last group and is not part of any group.
    expect(
      last.compareDocumentPosition(control) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(last.contains(control)).toBe(false);
    expect(
      within(last).queryByRole("button", { name: "Thêm trạng thái" }),
    ).toBeNull();

    fireEvent.click(control);
    const name = screen.getByRole("textbox", { name: "Tên trạng thái" });
    // A name the board already has is refused and nothing is sent.
    fireEvent.change(name, { target: { value: "cần làm" } });
    fireEvent.submit(name.closest("form")!);
    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toBeTruthy();

    fireEvent.change(name, { target: { value: "  Kiểm thử  " } });
    fireEvent.click(screen.getByRole("radio", { name: "Hoàn thành" }));
    fireEvent.submit(name.closest("form")!);
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ name: "Kiểm thử", category: "done" }),
    );
    await waitFor(() =>
      expect(
        screen.queryByRole("textbox", { name: "Tên trạng thái" }),
      ).toBeNull(),
    );
  });

  it("keeps the form open with its values when the group cannot be added", async () => {
    const onSubmit = vi.fn(async () => {
      throw new Error("refused");
    });
    const base = erpTableProps();
    renderTable({
      ...base,
      addGroup: { ...base.addGroup!, kind: "group", onSubmit },
    });
    fireEvent.click(screen.getByRole("button", { name: "Thêm nhóm" }));
    const name = screen.getByRole("textbox", { name: "Tên nhóm" });
    // A Select field has no status group to choose.
    expect(screen.queryByRole("radio", { name: "Hoàn thành" })).toBeNull();
    fireEvent.change(name, { target: { value: "Backlog" } });
    fireEvent.submit(name.closest("form")!);
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(
      (screen.getByRole("textbox", { name: "Tên nhóm" }) as HTMLInputElement)
        .value,
    ).toBe("Backlog");
  });

  it("shows no add-group control to a viewer who cannot edit", () => {
    renderTable(erpTableReadOnlyProps());
    expect(
      screen.queryByRole("button", { name: "Thêm trạng thái" }),
    ).toBeNull();
  });

  it("FUI[wm.table.main:resize] resizes a column by pointer, by key and back to its default", () => {
    const { props, view } = renderTable(erpTableProps());
    const grid = () =>
      view.container
        .querySelector<HTMLElement>('[role="table"]')!
        .style.getPropertyValue("--table-grid");
    expect(grid()).toBe(
      "40px 84px 340px 140px 184px 132px 96px 120px 136px 120px",
    );
    const handle = screen.getByRole("separator", {
      name: "Đổi độ rộng cột Tên công việc",
    });
    expect(handle.getAttribute("aria-valuenow")).toBe("340");

    // The pointer resizes live and saves the width on release only.
    pointer(handle, "pointerdown", { button: 0, clientX: 500, pointerId: 1 });
    pointer(handle, "pointermove", { clientX: 560, pointerId: 1 });
    expect(grid()).toContain("40px 84px 400px 140px");
    expect(props.onColumnWidthChange).not.toHaveBeenCalled();
    pointer(handle, "pointermove", { clientX: -900, pointerId: 1 });
    expect(grid()).toContain("40px 84px 72px 140px");
    pointer(handle, "pointermove", { clientX: 580, pointerId: 1 });
    pointer(handle, "pointerup", { clientX: 580, pointerId: 1 });
    expect(props.onColumnWidthChange).toHaveBeenCalledWith("title", 420);

    // The keyboard: Arrow keys by 8 px, Shift by 32 px, Home resets.
    fireEvent.keyDown(handle, { key: "ArrowRight" });
    expect(props.onColumnWidthChange).toHaveBeenLastCalledWith("title", 348);
    fireEvent.keyDown(handle, { key: "ArrowLeft", shiftKey: true });
    expect(props.onColumnWidthChange).toHaveBeenLastCalledWith("title", 308);
    fireEvent.keyDown(handle, { key: "Home" });
    expect(props.onColumnWidthReset).toHaveBeenCalledWith("title");
    fireEvent.doubleClick(handle);
    expect(props.onColumnWidthReset).toHaveBeenCalledTimes(2);

    // A cancelled drag leaves the width as it was.
    pointer(handle, "pointerdown", { button: 0, clientX: 500, pointerId: 2 });
    pointer(handle, "pointermove", { clientX: 560, pointerId: 2 });
    pointer(handle, "pointercancel", { pointerId: 2 });
    expect(grid()).toContain("40px 84px 340px 140px");
  });

  it("applies the saved column widths to the header and the rows", () => {
    const { view } = renderTable(erpTableResizedProps());
    expect(
      view.container
        .querySelector<HTMLElement>('[role="table"]')!
        .style.getPropertyValue("--table-grid"),
    ).toBe("40px 84px 440px 140px 232px 132px 96px 120px 136px 120px");
    expect(
      screen
        .getByRole("separator", { name: "Đổi độ rộng cột Người phụ trách" })
        .getAttribute("aria-valuenow"),
    ).toBe("232");
  });

  it("keeps every column in the same scroll: nothing is docked to the left", () => {
    const { view } = renderTable(erpTableProps());
    const docked = Array.from(
      view.container.querySelectorAll<HTMLElement>(
        '[role="cell"], [role="columnheader"]',
      ),
    ).filter((cell) => cell.className.split(" ").includes("sticky"));
    expect(docked).toEqual([]);
    expect(screen.getAllByRole("row")[0]!.className.split(" ")).toContain(
      "sticky",
    );
  });

  it("FUI[wm.table.main:group] collapses a group and adds an item in it", () => {
    const { props } = renderTable(erpTableProps());
    fireEvent.click(
      screen.getByRole("button", { name: "Thu gọn nhóm Cần làm" }),
    );
    expect(props.onGroupToggle).toHaveBeenCalledWith("status-todo");

    fireEvent.click(screen.getByRole("button", { name: "Thêm vào Cần làm" }));
    const input = screen.getByRole("textbox", {
      name: "Tên việc mới trong Cần làm",
    });
    fireEvent.change(input, { target: { value: "  Việc mới  " } });
    fireEvent.submit(input.closest("form")!);
    expect(props.onCreateItem).toHaveBeenCalledWith("status-todo", "Việc mới");
    expect((input as HTMLInputElement).value).toBe("");
    fireEvent.keyDown(input, { key: "Escape" });
    expect(
      screen.queryByRole("textbox", { name: "Tên việc mới trong Cần làm" }),
    ).toBeNull();
  });

  it("hides a collapsed group's rows and its add row", () => {
    renderTable({
      ...erpTableProps(),
      collapsedGroupIds: new Set(["status-todo"]),
    });
    expect(row("ERP-151")).toBeNull();
    expect(
      screen.queryByRole("button", { name: "Thêm vào Cần làm" }),
    ).toBeNull();
    expect(
      screen
        .getByRole("button", { name: "Mở nhóm Cần làm" })
        .getAttribute("aria-expanded"),
    ).toBe("false");
  });

  it("FUI[wm.table.main:read-only] gives a viewer no status edit, add or archive", () => {
    renderTable(erpTableReadOnlyProps());
    expect(
      screen.queryByRole("button", { name: "Thêm vào Cần làm" }),
    ).toBeNull();
    expect(screen.queryByRole("button", { name: "Thêm công việc" })).toBeNull();
    const pill = within(row("ERP-144")).getByRole("button", {
      name: "Đổi trạng thái của ERP-144",
    });
    expect((pill as HTMLButtonElement).disabled).toBe(true);
    // Reading still works: the item opens.
    expect(
      within(row("ERP-144")).getByRole("button", { name: /Kiểm thử/ }),
    ).toBeTruthy();
  });

  it("FUI[wm.table.main:no-results] says nothing matches and clears the filter", () => {
    const { props } = renderTable(erpTableNoResultsProps());
    expect(screen.queryAllByRole("rowgroup")).toHaveLength(0);
    const note = screen.getByRole("status");
    expect(within(note).getByText("Không có việc khớp bộ lọc")).toBeTruthy();
    expect(
      screen.getByText("0 công việc đang hiện · 16 ẩn bởi bộ lọc"),
    ).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: "Hiện việc đã xong" }),
    ).toBeNull();
    fireEvent.click(within(note).getByRole("button", { name: "Bỏ lọc" }));
    expect(props.onClearFilter).toHaveBeenCalledOnce();
  });

  it("offers Hiện việc đã xong while finished items are hidden", () => {
    const { props } = renderTable(erpTableProps());
    fireEvent.click(screen.getByRole("button", { name: "Hiện việc đã xong" }));
    expect(props.onShowDone).toHaveBeenCalledOnce();
  });

  it("keeps empty groups of a new board with their add rows", () => {
    renderTable(erpTableEmptyProps());
    expect(screen.getAllByRole("rowgroup")).toHaveLength(4);
    expect(
      screen.getByRole("button", { name: "Thêm vào Hoàn thành" }),
    ).toBeTruthy();
  });

  it("sizes rows by density and drops hidden columns", () => {
    const { view } = renderTable(
      erpTableProps({
        density: "compact",
        hiddenColumnIds: ["updated", "label"],
      }),
    );
    const section = view.container.querySelector<HTMLElement>(
      "[data-slot=main-table-surface]",
    )!;
    expect(section.style.getPropertyValue("--table-row-height")).toBe("32px");
    expect(section.dataset.density).toBe("compact");
    const headers = screen
      .getAllByRole("columnheader")
      .map((h) => h.textContent);
    expect(headers).not.toContain("Cập nhật");
    expect(headers).not.toContain("Nhãn");
    expect(headers).toContain("Người phụ trách");
  });

  it("marks the sorted column and the open item", () => {
    renderTable(erpTableProps({ openItemId: ERP_TABLE_ITEM("ERP-142") }));
    expect(
      screen
        .getByRole("columnheader", { name: /^Hạn/ })
        .getAttribute("aria-sort"),
    ).toBe("ascending");
    expect(row("ERP-142").getAttribute("aria-current")).toBe("true");
  });

  it("survives edge data: long title, no key, no status", () => {
    renderTable(erpTableEdgeProps());
    expect(screen.getByText(/Đối soát toàn bộ số dư công nợ/)).toBeTruthy();
    expect(
      screen.getByRole("rowgroup", { name: "Không có trạng thái" }),
    ).toBeTruthy();
    expect(
      screen.getByRole("rowgroup", {
        name: "Đang triển khai tại các chi nhánh miền Nam và miền Trung",
      }),
    ).toBeTruthy();
  });

  it("speaks English for the en copy", () => {
    const props = erpTableProps({ locale: "en" });
    renderPureUi(<MainTableSurface {...props} />);
    expect(screen.getByText("Status: not done")).toBeTruthy();
    expect(
      screen.getByText("11 items shown · 5 hidden by filters"),
    ).toBeTruthy();
    expect(screen.getByRole("columnheader", { name: /^Due/ })).toBeTruthy();
    expect(copy.footer.showDone).toBe("Hiện việc đã xong");
  });
});

describe("MainTableSurface in-place edits and bulk people and boards", () => {
  const members = [
    { userId: "u-ha", name: "Nguyễn Thu Hà", initials: "HA" },
    { userId: "u-quan", name: "Trần Minh Quân", initials: "MQ" },
  ];
  const editors = () => ({
    members,
    priorities: [
      { id: "p-urgent", label: "Khẩn" },
      { id: "p-high", label: "Cao" },
    ],
    onAssignee: vi.fn(),
    onDue: vi.fn(),
    onPriority: vi.fn(),
  });

  it("the assignee cell lists the people and a pick sends the person", () => {
    const ed = editors();
    renderTable({ ...erpTableProps(), editors: ed });
    fireEvent.click(
      screen.getByRole("button", { name: "Đổi người phụ trách của ERP-153" }),
    );
    fireEvent.click(screen.getByRole("button", { name: /Trần Minh Quân/ }));
    expect(ed.onAssignee).toHaveBeenCalledWith(
      ERP_TABLE_ITEM("ERP-153"),
      "u-quan",
    );
  });

  it("the assignee list can clear the assignee", () => {
    const ed = editors();
    renderTable({ ...erpTableProps(), editors: ed });
    fireEvent.click(
      screen.getByRole("button", { name: "Đổi người phụ trách của ERP-151" }),
    );
    fireEvent.click(screen.getByRole("button", { name: /Chưa giao/ }));
    expect(ed.onAssignee).toHaveBeenCalledWith(ERP_TABLE_ITEM("ERP-151"), null);
  });

  it("the priority cell offers the board's options", () => {
    const ed = editors();
    renderTable({ ...erpTableProps(), editors: ed });
    fireEvent.keyDown(
      screen.getByRole("button", { name: "Đổi ưu tiên của ERP-153" }),
      { key: "Enter" },
    );
    fireEvent.click(screen.getByRole("menuitem", { name: "Khẩn" }));
    expect(ed.onPriority).toHaveBeenCalledWith(
      ERP_TABLE_ITEM("ERP-153"),
      "p-urgent",
    );
  });

  it("the due cell clears the date", () => {
    const ed = editors();
    renderTable({ ...erpTableProps(), editors: ed });
    fireEvent.click(
      screen.getByRole("button", { name: "Đổi hạn của ERP-151" }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Bỏ hạn" }));
    expect(ed.onDue).toHaveBeenCalledWith(ERP_TABLE_ITEM("ERP-151"), null);
  });

  it("a read-only viewer gets no editor buttons", () => {
    renderTable({ ...erpTableReadOnlyProps(), editors: editors() });
    expect(
      screen.queryByRole("button", { name: /Đổi người phụ trách của/ }),
    ).toBeNull();
    expect(screen.queryByRole("button", { name: /Đổi hạn của/ })).toBeNull();
  });

  it("the bulk bar assigns the selection and moves it to a board", () => {
    const onAssign = vi.fn();
    const onMove = vi.fn();
    renderTable({
      ...erpTableSelectedProps(),
      bulkAssign: { members, onAssign },
      bulkMove: {
        boards: [{ id: "board-support", title: "Hỗ trợ nội bộ" }],
        onMove,
      },
    });
    fireEvent.click(screen.getByRole("button", { name: /^Giao cho/ }));
    fireEvent.click(screen.getByRole("button", { name: "Nguyễn Thu Hà" }));
    expect(onAssign).toHaveBeenCalledWith("u-ha");
    fireEvent.click(screen.getByRole("button", { name: /^Chuyển board/ }));
    fireEvent.click(screen.getByRole("button", { name: "Hỗ trợ nội bộ" }));
    expect(onMove).toHaveBeenCalledWith("board-support");
  });

  it("without a write behind them Giao cho and Chuyển board stay disabled with the reason", () => {
    renderTable(erpTableSelectedProps());
    for (const name of [/^Giao cho/, /^Chuyển board/]) {
      const button = screen.getByRole("button", { name });
      expect(button.getAttribute("aria-disabled")).toBe("true");
    }
  });

  it("the row menu opens the item, duplicates, copies the link and archives", () => {
    const rowActions = {
      onDuplicate: vi.fn(),
      onCopyLink: vi.fn(),
      onArchive: vi.fn(),
    };
    const { props } = renderTable({ ...erpTableProps(), rowActions });
    fireEvent.contextMenu(row("ERP-144"));
    fireEvent.click(screen.getByRole("menuitem", { name: "Nhân bản" }));
    expect(rowActions.onDuplicate).toHaveBeenCalledWith(
      ERP_TABLE_ITEM("ERP-144"),
    );
    fireEvent.contextMenu(row("ERP-144"));
    fireEvent.click(screen.getByRole("menuitem", { name: "Lưu trữ" }));
    expect(rowActions.onArchive).toHaveBeenCalledWith(
      ERP_TABLE_ITEM("ERP-144"),
    );
    fireEvent.contextMenu(row("ERP-144"));
    fireEvent.click(screen.getByRole("menuitem", { name: "Mở chi tiết" }));
    expect(props.onOpenItem).toHaveBeenCalledWith(ERP_TABLE_ITEM("ERP-144"));
  });

  it("the footer counts the items just archived", () => {
    renderTable(erpTableArchivedProps());
    expect(
      screen.getByText(
        "9 công việc đang hiện · 5 ẩn bởi bộ lọc · 2 vừa lưu trữ",
      ),
    ).toBeTruthy();
  });

  it("the empty note names the one person the filter narrows to", () => {
    renderTable({
      ...erpTableNoResultsProps(),
      noResultsAssignee: "Đặng Minh Tuấn",
    });
    expect(
      screen.getByText(
        "Đặng Minh Tuấn chưa phụ trách việc nào chưa xong trên board này.",
      ),
    ).toBeTruthy();
  });
});
