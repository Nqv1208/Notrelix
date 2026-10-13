import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import {
  ERP_TABLE_ITEM,
  erpTableArchivedProps,
  erpTableEdgeProps,
  erpTableEditingProps,
  erpTableEmptyProps,
  erpTableNoResultsProps,
  erpTableProps,
  erpTableReadOnlyProps,
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
  };
}

function renderTable(props: MainTableSurfaceProps) {
  const spy = spied(props);
  const view = renderPureUi(<MainTableSurface {...spy} />);
  return { props: spy, view };
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

  it("FUI[wm.table.main:keyboard] moves between rows and edits, opens and selects from the keyboard", () => {
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
    expect(
      screen.getByRole("listbox", { name: "Đổi trạng thái" }),
    ).toBeTruthy();
  });

  it("FUI[wm.table.main:open] opens the item from its title and from Mở", () => {
    const { props } = renderTable(erpTableProps());
    const target = row("ERP-151");
    fireEvent.click(
      within(target).getByRole("button", { name: /Cấu hình quyền truy cập/ }),
    );
    fireEvent.click(
      within(target).getByRole("button", { name: "Mở chi tiết ERP-151" }),
    );
    expect(props.onOpenItem).toHaveBeenCalledTimes(2);
    expect(props.onOpenItem).toHaveBeenCalledWith(ERP_TABLE_ITEM("ERP-151"));
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
        .getByRole("columnheader", { name: "Hạn" })
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
    expect(screen.getByRole("columnheader", { name: "Due" })).toBeTruthy();
    expect(copy.footer.showDone).toBe("Hiện việc đã xong");
  });
});
