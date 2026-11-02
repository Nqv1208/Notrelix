import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { erpTableToolbarProps } from "../../../../verification/table-fixtures";
import { MainTableToolbar, type MainTableToolbarProps } from "../table-toolbar";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

function renderToolbar(props: MainTableToolbarProps) {
  const spy: MainTableToolbarProps = {
    ...props,
    onSearchChange: vi.fn(),
    onShowDone: vi.fn(),
    onColumnVisibleChange: vi.fn(),
    onDensityChange: vi.fn(),
    onAddItem: vi.fn(),
  };
  renderPureUi(<MainTableToolbar {...spy} />);
  return spy;
}

describe("MainTableToolbar", () => {
  it("FUI[wm.table.toolbar:toolbar] renders the mockup controls and emits search and add", () => {
    const props = renderToolbar(erpTableToolbarProps("default"));
    const search = screen.getByRole("searchbox", { name: "Tìm trong bảng" });
    expect(search.getAttribute("placeholder")).toBe("Tìm trong bảng…");
    fireEvent.change(search, { target: { value: "công nợ" } });
    expect(props.onSearchChange).toHaveBeenCalledWith("công nợ");

    // Filter, sort and group live in the view bar slot.
    expect(screen.getByRole("button", { name: /^Lọc/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Cột 9/15" })).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: "Thêm công việc" }));
    expect(props.onAddItem).toHaveBeenCalledOnce();
  });

  it("FUI[wm.table.toolbar:chips] removes the done chip", () => {
    const props = renderToolbar(erpTableToolbarProps("default"));
    expect(screen.getByText("Trạng thái: chưa xong")).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: "Bỏ lọc Trạng thái: chưa xong" }),
    );
    expect(props.onShowDone).toHaveBeenCalledOnce();
  });

  it("FUI[wm.table.toolbar:columns] shows and hides columns and sets the row height", () => {
    const props = renderToolbar(erpTableToolbarProps("default"));
    fireEvent.click(screen.getByRole("button", { name: "Cột 9/15" }));
    const menu = screen.getByRole("dialog", { name: "Cột" });
    const title = within(menu).getByRole("checkbox", { name: "Tên công việc" });
    expect(title).toHaveProperty("disabled", true);
    const start = within(menu).getByRole("checkbox", { name: "Bắt đầu" });
    expect(start.getAttribute("aria-checked")).toBe("false");
    fireEvent.click(start);
    expect(props.onColumnVisibleChange).toHaveBeenCalledWith("start", true);

    expect(
      within(menu)
        .getByRole("radio", { name: "Thoải mái" })
        .getAttribute("aria-checked"),
    ).toBe("true");
    fireEvent.click(within(menu).getByRole("radio", { name: "Gọn" }));
    expect(props.onDensityChange).toHaveBeenCalledWith("compact");
  });

  it("moves a column with Alt+Arrow and by dropping it on another row", () => {
    const onOrder = vi.fn();
    renderToolbar({
      ...erpTableToolbarProps("default"),
      onColumnOrderChange: onOrder,
    });
    fireEvent.click(screen.getByRole("button", { name: "Cột 9/15" }));
    const menu = screen.getByRole("dialog", { name: "Cột" });
    const status = within(menu).getByRole("checkbox", { name: "Trạng thái" });
    fireEvent.keyDown(status, { key: "ArrowDown", altKey: true });
    expect(onOrder).toHaveBeenLastCalledWith(
      expect.arrayContaining(["status", "assignee"]),
    );
    expect(onOrder.mock.calls[0]![0].indexOf("status")).toBe(
      onOrder.mock.calls[0]![0].indexOf("assignee") + 1,
    );
    // Mã and Tên công việc stay in front: they cannot be moved.
    const key = within(menu).getByRole("checkbox", { name: "Mã" });
    fireEvent.keyDown(key, { key: "ArrowDown", altKey: true });
    expect(onOrder).toHaveBeenCalledTimes(1);

    const dragged = status.closest("label")!;
    const target = within(menu)
      .getByRole("checkbox", { name: "Ưu tiên" })
      .closest("label")!;
    fireEvent.dragStart(dragged, { dataTransfer: { effectAllowed: "" } });
    fireEvent.dragOver(target);
    fireEvent.drop(target);
    const dropped = onOrder.mock.calls[1]![0] as string[];
    // Dragged down onto Ưu tiên it lands where that row was: just after it.
    expect(dropped.indexOf("status")).toBe(dropped.indexOf("priority") + 1);
  });

  it("lets a viewer arrange columns and says why the shared view stays off", () => {
    const onReset = vi.fn();
    renderToolbar({
      ...erpTableToolbarProps("default"),
      columnState: {
        personal: true,
        canSaveToView: false,
        savingToView: false,
        onSaveToView: vi.fn(),
        onReset,
        saveFailed: false,
        onRetrySave: vi.fn(),
        saveToViewFailed: false,
        saveToViewConflict: false,
        savedToView: false,
      },
    });
    fireEvent.click(screen.getByRole("button", { name: "Cột 9/15" }));
    const save = screen.getByRole("button", { name: "Lưu vào view chung" });
    expect(save.getAttribute("aria-disabled")).toBe("true");
    expect(save.getAttribute("aria-describedby")).toBeTruthy();
    expect(
      screen.getByText("Chỉ người sửa board mới lưu được vào view chung."),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Về view chung" }));
    expect(onReset).toHaveBeenCalledOnce();
  });

  it("FUI[wm.table.toolbar:read-only] has no add button for a viewer", () => {
    renderToolbar(erpTableToolbarProps("read-only"));
    expect(screen.queryByRole("button", { name: "Thêm công việc" })).toBeNull();
    expect(screen.getByRole("button", { name: /^Lọc/ })).toBeTruthy();
  });

  it("copes with long names and every facet selected", () => {
    renderToolbar(erpTableToolbarProps("edge"));
    expect(screen.getByRole("button", { name: /^Cột 15\/15/ })).toBeTruthy();
  });

  it("speaks English with the en copy", () => {
    renderToolbar(erpTableToolbarProps("default", "en"));
    expect(screen.getByText("Status: not done")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Add item" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Columns 9/15" })).toBeTruthy();
  });
});
