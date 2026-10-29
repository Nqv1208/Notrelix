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

/** Radix opens a menu trigger on Enter. */
function openMenu(trigger: HTMLElement) {
  fireEvent.keyDown(trigger, { key: "Enter" });
}

describe("MainTableToolbar", () => {
  it("FUI[wm.table.toolbar:toolbar] renders the mockup controls and emits search and add", () => {
    const props = renderToolbar(erpTableToolbarProps("default"));
    const search = screen.getByRole("searchbox", { name: "Tìm trong bảng" });
    expect(search.getAttribute("placeholder")).toBe("Tìm trong bảng…");
    fireEvent.change(search, { target: { value: "công nợ" } });
    expect(props.onSearchChange).toHaveBeenCalledWith("công nợ");

    // The view controls come in the slot.
    expect(screen.getByRole("button", { name: /^Lọc/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: /^Sắp xếp/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: /^Nhóm/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Cột 9/15" })).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: "Thêm công việc" }));
    expect(props.onAddItem).toHaveBeenCalledOnce();
  });

  it("FUI[wm.table.toolbar:chips] removes the done chip from the toolbar and shows the filter chips from the view bar", () => {
    const props = renderToolbar(erpTableToolbarProps("filtered"));
    expect(screen.getByText("Trạng thái: chưa xong")).toBeTruthy();
    expect(screen.getByText("Ưu tiên: Khẩn, Cao")).toBeTruthy();
    expect(screen.getByText("Người phụ trách: Đặng Minh Tuấn")).toBeTruthy();
    expect(screen.getByText("Quá hạn")).toBeTruthy();

    fireEvent.click(
      screen.getByRole("button", { name: "Bỏ lọc Trạng thái: chưa xong" }),
    );
    expect(props.onShowDone).toHaveBeenCalledOnce();
  });

  it("FUI[wm.table.toolbar:columns] shows and hides columns and sets the row height", () => {
    const props = renderToolbar(erpTableToolbarProps("default"));
    openMenu(screen.getByRole("button", { name: "Cột 9/15" }));
    const title = screen.getByRole("menuitemcheckbox", {
      name: "Tên công việc",
    });
    expect(title.getAttribute("aria-disabled")).toBe("true");
    const start = screen.getByRole("menuitemcheckbox", { name: "Bắt đầu" });
    expect(start.getAttribute("aria-checked")).toBe("false");
    fireEvent.click(start);
    expect(props.onColumnVisibleChange).toHaveBeenCalledWith("start", true);

    const menu = screen.getByRole("menu");
    expect(
      within(menu)
        .getByRole("menuitemradio", { name: "Thoải mái" })
        .getAttribute("aria-checked"),
    ).toBe("true");
    fireEvent.click(within(menu).getByRole("menuitemradio", { name: "Gọn" }));
    expect(props.onDensityChange).toHaveBeenCalledWith("compact");
  });

  it("FUI[wm.table.toolbar:read-only] has no add button for a viewer", () => {
    renderToolbar(erpTableToolbarProps("read-only"));
    expect(screen.queryByRole("button", { name: "Thêm công việc" })).toBeNull();
    expect(screen.getByRole("button", { name: /^Lọc/ })).toBeTruthy();
  });

  it("copes with long names and every facet selected", () => {
    renderToolbar(erpTableToolbarProps("edge"));
    expect(screen.getByRole("button", { name: /^Cột 15\/15/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: /^Sắp xếp/ })).toBeTruthy();
  });

  it("speaks English with the en copy", () => {
    renderToolbar(erpTableToolbarProps("default", "en"));
    expect(screen.getByText("Status: not done")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Add item" })).toBeTruthy();
    expect(screen.getByRole("button", { name: /^Sort/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Columns 9/15" })).toBeTruthy();
  });
});
