import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import {
  kanbanToolbarCopyFixture,
  kanbanToolbarDefaultScenario,
  kanbanToolbarEdgeDataScenario,
  kanbanToolbarFilteredScenario,
  kanbanToolbarNoResultsScenario,
  kanbanToolbarReadOnlyScenario,
  type KanbanToolbarScenario,
} from "../../../../verification/kanban-toolbar-fixtures";
import { emptyKanbanFilter } from "../kanban-filter";
import { KanbanToolbar, type KanbanToolbarProps } from "../kanban-toolbar";

const LOCALES = ["vi", "en"] as const;

function renderToolbar(
  scenario: KanbanToolbarScenario,
  overrides: Partial<KanbanToolbarProps> = {},
) {
  const props: KanbanToolbarProps = {
    ...scenario,
    onSearchChange: vi.fn(),
    onFilterChange: vi.fn(),
    onGroupByChange: vi.fn(),
    onAddItem: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<KanbanToolbar {...props} />);
  return { props, view };
}

/** Radix opens a menu trigger on Enter. */
function openMenu(trigger: HTMLElement) {
  fireEvent.keyDown(trigger, { key: "Enter" });
}

function fill(message: string, values: Record<string, string | number>) {
  return message.replace(/\{\{(\w+)\}\}/g, (_m, key: string) =>
    String(values[key]),
  );
}

describe("KanbanToolbar", () => {
  it("FUI[wm.kanban.toolbar:toolbar] renders search, filter, grouping and add, and emits search and add", () => {
    for (const locale of LOCALES) {
      const copy = kanbanToolbarCopyFixture(locale);
      const { props, view } = renderToolbar(
        kanbanToolbarDefaultScenario(locale),
      );

      const search = screen.getByRole("searchbox", {
        name: copy.search.label,
      });
      expect(search.getAttribute("placeholder")).toBe(copy.search.placeholder);
      fireEvent.change(search, { target: { value: "hoá đơn" } });
      expect(props.onSearchChange).toHaveBeenCalledWith("hoá đơn");

      expect(
        screen.getByRole("button", { name: copy.filter.trigger }),
      ).toBeTruthy();
      expect(
        screen.getByRole("button", {
          name: `${copy.groupBy.label} Trạng thái`,
        }),
      ).toBeTruthy();
      // No filter is active: no chip and no no-results bar.
      expect(screen.queryByRole("status")).toBeNull();
      expect(
        screen.queryByRole("button", { name: /^(Bỏ lọc|Clear filter) / }),
      ).toBeNull();

      fireEvent.click(screen.getByRole("button", { name: copy.addItem }));
      expect(props.onAddItem).toHaveBeenCalledOnce();
      view.unmount();
    }
  });

  it("uses the mockup Vietnamese wording", () => {
    renderToolbar(kanbanToolbarNoResultsScenario("vi"));
    expect(screen.getByPlaceholderText("Tìm trong board…")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Lọc" })).toBeTruthy();
    expect(
      screen.getByRole("button", { name: "Nhóm theo Trạng thái" }),
    ).toBeTruthy();
    expect(screen.getByText("Quá hạn · Ưu tiên Thấp")).toBeTruthy();
    expect(
      screen.getByRole("button", { name: "Bỏ lọc Quá hạn, Ưu tiên Thấp" }),
    ).toBeTruthy();
    expect(screen.getByRole("status").textContent).toBe(
      "Không có việc khớp bộ lọc · 16 việc của board đang bị ẩn" + "Bỏ lọc",
    );
    expect(screen.getByRole("button", { name: "Thêm công việc" })).toBeTruthy();
  });

  it("FUI[wm.kanban.toolbar:filter] lists the five filter facets and emits the whole toggled filter", () => {
    for (const locale of LOCALES) {
      const copy = kanbanToolbarCopyFixture(locale);
      const scenario = kanbanToolbarFilteredScenario(locale);
      const { props, view } = renderToolbar(scenario);
      const onFilterChange = vi.mocked(props.onFilterChange);

      openMenu(screen.getByRole("button", { name: copy.filter.trigger }));
      const menu = screen.getByRole("menu");
      for (const heading of [
        copy.filter.status,
        copy.filter.priority,
        copy.filter.assignee,
        copy.filter.label,
      ]) {
        expect(within(menu).getByText(heading)).toBeTruthy();
      }

      // Checked state mirrors the filter prop.
      const doing = within(menu).getByRole("menuitemcheckbox", {
        name: "Đang làm",
      });
      expect(doing.getAttribute("aria-checked")).toBe("true");
      expect(
        within(menu)
          .getByRole("menuitemcheckbox", { name: "Cần làm" })
          .getAttribute("aria-checked"),
      ).toBe("false");

      fireEvent.click(
        within(menu).getByRole("menuitemcheckbox", { name: "Cần làm" }),
      );
      expect(onFilterChange).toHaveBeenLastCalledWith({
        ...scenario.filter,
        statusOptionIds: ["status-doing", "status-todo"],
      });

      fireEvent.click(doing);
      expect(onFilterChange).toHaveBeenLastCalledWith({
        ...scenario.filter,
        statusOptionIds: [],
      });

      fireEvent.click(
        within(menu).getByRole("menuitemcheckbox", {
          name: copy.filter.priorities.low,
        }),
      );
      expect(onFilterChange).toHaveBeenLastCalledWith({
        ...scenario.filter,
        priorities: ["low"],
      });

      fireEvent.click(
        within(menu).getByRole("menuitemcheckbox", { name: "Khánh Linh" }),
      );
      expect(onFilterChange).toHaveBeenLastCalledWith({
        ...scenario.filter,
        assigneeIds: ["user-khanh-linh"],
      });

      fireEvent.click(
        within(menu).getByRole("menuitemcheckbox", { name: "Bảo mật" }),
      );
      expect(onFilterChange).toHaveBeenLastCalledWith({
        ...scenario.filter,
        labelIds: [],
      });

      fireEvent.click(
        within(menu).getByRole("menuitemcheckbox", {
          name: copy.filter.overdue,
        }),
      );
      expect(onFilterChange).toHaveBeenLastCalledWith({
        ...scenario.filter,
        overdue: true,
      });
      // Toggling keeps the menu open for the next choice.
      expect(screen.getByRole("menu")).toBeTruthy();
      expect(onFilterChange).toHaveBeenCalledTimes(6);
      view.unmount();
    }
  });

  it("FUI[wm.kanban.toolbar:filter-chip] summarises the active filter in one chip whose remove button clears it", () => {
    for (const locale of LOCALES) {
      const copy = kanbanToolbarCopyFixture(locale);
      const { props, view } = renderToolbar(
        kanbanToolbarNoResultsScenario(locale),
      );
      const parts = [
        copy.filter.chip.overdue,
        fill(copy.filter.chip.priority, {
          name: copy.filter.priorities.low,
        }),
      ];
      expect(screen.getByText(parts.join(copy.separator))).toBeTruthy();
      fireEvent.click(
        screen.getByRole("button", {
          name: fill(copy.filter.clear, {
            filters: parts.join(copy.filter.listSeparator),
          }),
        }),
      );
      expect(props.onFilterChange).toHaveBeenCalledOnce();
      expect(props.onFilterChange).toHaveBeenCalledWith(emptyKanbanFilter());
      expect(props.onSearchChange).not.toHaveBeenCalled();
      view.unmount();
    }
  });

  it("names chip values from the options and leaves out ids the board no longer has", () => {
    const scenario = kanbanToolbarFilteredScenario("vi");
    renderToolbar({
      ...scenario,
      filter: {
        ...scenario.filter,
        assigneeIds: ["user-gone", "user-tran-thao"],
      },
    });
    expect(
      screen.getByText("Đang làm · Người phụ trách Trần Thảo · Nhãn Bảo mật"),
    ).toBeTruthy();
  });

  it("FUI[wm.kanban.toolbar:group-by] lists the grouping fields and emits the chosen field", () => {
    for (const locale of LOCALES) {
      const copy = kanbanToolbarCopyFixture(locale);
      const { props, view } = renderToolbar(
        kanbanToolbarDefaultScenario(locale),
      );
      openMenu(
        screen.getByRole("button", {
          name: `${copy.groupBy.label} Trạng thái`,
        }),
      );
      const items = within(screen.getByRole("menu")).getAllByRole(
        "menuitemradio",
      );
      expect(items.map((item) => item.textContent)).toEqual([
        "Trạng thái",
        "Ưu tiên",
        "Nhãn",
      ]);
      expect(items[0]?.getAttribute("aria-checked")).toBe("true");

      // Choosing the current field is not a change.
      fireEvent.click(items[0]!);
      expect(props.onGroupByChange).not.toHaveBeenCalled();

      openMenu(
        screen.getByRole("button", {
          name: `${copy.groupBy.label} Trạng thái`,
        }),
      );
      fireEvent.click(
        within(screen.getByRole("menu")).getByRole("menuitemradio", {
          name: "Ưu tiên",
        }),
      );
      expect(props.onGroupByChange).toHaveBeenCalledOnce();
      expect(props.onGroupByChange).toHaveBeenCalledWith("field-priority");
      view.unmount();
    }
  });

  it("disables grouping when the board has no status or select field", () => {
    renderToolbar({
      ...kanbanToolbarDefaultScenario("vi"),
      groupByFields: [],
      groupByFieldId: null,
    });
    const pill = screen.getByRole("button", { name: "Nhóm theo" });
    expect((pill as HTMLButtonElement).disabled).toBe(true);
  });

  it("FUI[wm.kanban.toolbar:read-only] hides add for a viewer who cannot add and keeps search, filter and grouping", () => {
    for (const locale of LOCALES) {
      const copy = kanbanToolbarCopyFixture(locale);
      const { view } = renderToolbar(kanbanToolbarReadOnlyScenario(locale));
      expect(screen.queryByRole("button", { name: copy.addItem })).toBeNull();
      expect(
        screen.getByRole("searchbox", { name: copy.search.label }),
      ).toBeTruthy();
      expect(
        screen.getByRole("button", { name: copy.filter.trigger }),
      ).toBeTruthy();
      expect(
        screen.getByRole("button", {
          name: `${copy.groupBy.label} Trạng thái`,
        }),
      ).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[wm.kanban.toolbar:no-results] states how many items are hidden and clears the filter and search", () => {
    for (const locale of LOCALES) {
      const copy = kanbanToolbarCopyFixture(locale);
      const scenario = kanbanToolbarNoResultsScenario(locale);
      const { props, view } = renderToolbar({
        ...scenario,
        searchQuery: "hoá đơn",
      });
      const bar = screen.getByRole("status");
      expect(within(bar).getByText(copy.noResults.title)).toBeTruthy();
      expect(
        bar.textContent?.includes(
          fill(copy.noResults.hidden_other, { count: 16 }),
        ),
      ).toBe(true);
      fireEvent.click(
        within(bar).getByRole("button", { name: copy.noResults.clear }),
      );
      expect(props.onFilterChange).toHaveBeenCalledWith(emptyKanbanFilter());
      expect(props.onSearchChange).toHaveBeenCalledWith("");
      view.unmount();
    }
  });

  it("uses the singular English form for one hidden item and omits the bar without a no-results state", () => {
    const { view } = renderToolbar({
      ...kanbanToolbarNoResultsScenario("en"),
      noResults: { hiddenCount: 1 },
    });
    expect(screen.getByRole("status").textContent).toContain(
      "1 board item is hidden",
    );
    view.unmount();
    renderToolbar(kanbanToolbarFilteredScenario("en"));
    expect(screen.queryByRole("status")).toBeNull();
  });

  it("keeps long names and a full filter readable in the edge data", () => {
    const scenario = kanbanToolbarEdgeDataScenario("vi");
    renderToolbar(scenario);
    expect(
      screen.getByRole("button", {
        name: "Nhóm theo Giai đoạn triển khai theo kế hoạch tổng thể năm tài chính",
      }),
    ).toBeTruthy();
    expect(
      screen.getByText(/Người phụ trách Nguyễn Hoàng Phương Thảo Nguyên/),
    ).toBeTruthy();
    expect(
      (
        screen.getByRole("searchbox", {
          name: "Tìm trong board",
        }) as HTMLInputElement
      ).value,
    ).toBe(scenario.searchQuery);
  });
});
