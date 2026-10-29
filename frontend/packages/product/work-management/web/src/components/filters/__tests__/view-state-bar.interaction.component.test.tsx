import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { erpViewBar } from "../../../verification/view-state-fixtures";
import { filtersCopy } from "../filters.copy";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const LOCALES = ["vi", "en"] as const;

/** Radix opens a menu trigger on Enter. */
const openMenu = (trigger: HTMLElement) =>
  fireEvent.keyDown(trigger, { key: "Enter" });

const owner = (values: string[] = ["user-dang-minh-tuan"]) => ({
  fieldId: "field-erp-owner",
  operator: "has_any" as const,
  values,
});

describe("view bar", () => {
  it("FUI[wm.filters.bar:builder] opens the dialog, adds a condition and emits the whole filter", () => {
    for (const locale of LOCALES) {
      const copy = filtersCopy(locale);
      const setFilter = vi.fn();
      const { unmount } = renderPureUi(
        <div>{erpViewBar({ locale, control: { setFilter } })}</div>,
      );
      fireEvent.click(
        screen.getByRole("button", { name: copy.filter.trigger }),
      );
      const dialog = screen.getByRole("dialog", { name: copy.filter.title });
      expect(dialog).toBeTruthy();
      fireEvent.click(
        within(dialog).getByRole("button", { name: copy.filter.add }),
      );
      expect(setFilter).toHaveBeenCalledOnce();
      const next = setFilter.mock.calls[0]![0] as {
        join: string;
        conditions: unknown[];
      };
      expect(next.join).toBe("and");
      expect(next.conditions).toHaveLength(1);
      unmount();
    }
  });

  it("FUI[wm.filters.bar:chips] names each condition, removes one and folds the rest into +N", () => {
    const setFilter = vi.fn();
    renderPureUi(
      <div>
        {erpViewBar({
          control: { setFilter },
          state: {
            filter: {
              join: "or",
              conditions: [
                owner(),
                {
                  fieldId: "field-erp-priority",
                  operator: "has_any",
                  values: ["p-urgent", "p-high"],
                },
                { fieldId: "$overdue", operator: "is_true", values: [] },
                {
                  fieldId: "$title",
                  operator: "contains",
                  values: ["hoá đơn"],
                },
              ],
            },
          },
        })}
      </div>,
    );
    expect(screen.getByText("Người phụ trách: Đặng Minh Tuấn")).toBeTruthy();
    expect(screen.getByText("Ưu tiên: Khẩn, Cao")).toBeTruthy();
    expect(screen.getAllByText("HOẶC")).toHaveLength(2);
    expect(screen.getByRole("button", { name: "+1 điều kiện" })).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", {
        name: "Xoá bộ lọc Người phụ trách: Đặng Minh Tuấn",
      }),
    );
    const next = setFilter.mock.calls[0]![0] as { conditions: unknown[] };
    expect(next.conditions).toHaveLength(3);
  });

  it("FUI[wm.filters.bar:sort] adds levels up to three and emits the whole list", () => {
    const setSorts = vi.fn();
    renderPureUi(
      <div>
        {erpViewBar({
          control: { setSorts },
          state: {
            sorts: [
              { fieldId: "field-erp-due", direction: "asc" },
              { fieldId: "field-erp-priority", direction: "desc" },
            ],
          },
        })}
      </div>,
    );
    fireEvent.click(screen.getByRole("button", { name: /^Sắp xếp/ }));
    fireEvent.click(screen.getByRole("button", { name: "Thêm cách sắp xếp" }));
    const next = setSorts.mock.calls[0]![0] as unknown[];
    expect(next).toHaveLength(3);
  });

  it("FUI[wm.filters.bar:group] groups by a person or a date bucket and clears the grouping", () => {
    const setGroup = vi.fn();
    renderPureUi(<div>{erpViewBar({ control: { setGroup } })}</div>);
    openMenu(screen.getByRole("button", { name: /^Nhóm/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Hạn" }));
    expect(setGroup).toHaveBeenLastCalledWith({
      fieldId: "field-erp-due",
      bucket: "week",
    });
  });

  it("FUI[wm.filters.bar:lane] offers the swimlane field and refuses the column field", () => {
    const setLane = vi.fn();
    renderPureUi(
      <div>{erpViewBar({ view: "kanban", control: { setLane } })}</div>,
    );
    openMenu(screen.getByRole("button", { name: /^Làn/ }));
    const status = screen.getByRole("menuitemradio", {
      name: /Trạng thái/,
    });
    expect(status.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(
      screen.getByRole("menuitemradio", { name: "Người phụ trách" }),
    );
    expect(setLane).toHaveBeenLastCalledWith({
      fieldId: "field-erp-owner",
    });
  });

  it("FUI[wm.filters.bar:personal] names the personal view and goes back to the shared one", () => {
    for (const locale of LOCALES) {
      const copy = filtersCopy(locale);
      const reset = vi.fn();
      const { unmount } = renderPureUi(
        <div>
          {erpViewBar({
            locale,
            control: { reset },
            state: { filter: { join: "and", conditions: [owner()] } },
          })}
        </div>,
      );
      const group = screen.getByRole("group", { name: copy.personal.label });
      fireEvent.click(
        within(group).getByRole("button", { name: copy.personal.reset }),
      );
      expect(reset).toHaveBeenCalledOnce();
      unmount();
    }
    // No personal state: no chip.
    renderPureUi(<div>{erpViewBar()}</div>);
    expect(screen.queryByRole("group", { name: "Cách xem riêng" })).toBeNull();
  });

  it("FUI[wm.filters.bar:stale] warns about a removed field, offers to drop it and to save as a new filter", () => {
    const setFilter = vi.fn();
    const onSaveAsNew = vi.fn();
    renderPureUi(
      <div>
        {erpViewBar({
          control: { setFilter },
          onSaveAsNew,
          state: {
            filter: {
              join: "and",
              conditions: [
                owner(),
                { fieldId: "field-gone", operator: "equals", values: ["x"] },
              ],
            },
          },
        })}
      </div>,
    );
    const hint = screen.getByRole("status");
    expect(hint.textContent).toContain("1 điều kiện dùng trường đã bị xoá");
    fireEvent.click(
      within(hint).getByRole("button", { name: "Lưu thành bộ lọc mới" }),
    );
    expect(onSaveAsNew).toHaveBeenCalledOnce();
    fireEvent.click(
      within(hint).getByRole("button", { name: "Xoá điều kiện bị bỏ qua" }),
    );
    const next = setFilter.mock.calls[0]![0] as { conditions: unknown[] };
    expect(next.conditions).toHaveLength(1);
  });

  it("FUI[wm.filters.bar:save-failed] keeps the view on screen and offers to try saving again", () => {
    const retrySave = vi.fn();
    renderPureUi(
      <div>
        {erpViewBar({
          control: { saveFailed: true, retrySave },
          state: { filter: { join: "and", conditions: [owner()] } },
        })}
      </div>,
    );
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain("Chưa lưu được cách xem của bạn");
    fireEvent.click(within(alert).getByRole("button", { name: "Thử lại" }));
    expect(retrySave).toHaveBeenCalledOnce();
  });

  it("FUI[wm.filters.bar:viewer] lets a viewer filter but shows saving disabled with the reason", () => {
    const setFilter = vi.fn();
    renderPureUi(
      <div>
        {erpViewBar({
          canSave: false,
          control: { setFilter },
          state: { filter: { join: "and", conditions: [owner()] } },
        })}
      </div>,
    );
    const save = screen.getByRole("button", { name: "Lưu bộ lọc" });
    expect((save as HTMLButtonElement).disabled).toBe(true);
    expect(save.getAttribute("aria-describedby")).toBeTruthy();
    expect(
      screen.getByText(
        /Bạn chỉ xem board này nên chưa lưu hay chia sẻ được bộ lọc/,
      ),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Lọc · 1" }));
    expect(screen.getByRole("dialog", { name: "Lọc việc" })).toBeTruthy();
  });
});
