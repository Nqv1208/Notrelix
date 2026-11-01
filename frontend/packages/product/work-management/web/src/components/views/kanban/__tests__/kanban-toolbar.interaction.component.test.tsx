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
import { KanbanToolbar, type KanbanToolbarProps } from "../kanban-toolbar";

const LOCALES = ["vi", "en"] as const;

function renderToolbar(
  scenario: KanbanToolbarScenario,
  overrides: Partial<KanbanToolbarProps> = {},
) {
  const props: KanbanToolbarProps = {
    ...scenario,
    onSearchChange: vi.fn(),
    onClearFilter: vi.fn(),
    onAddItem: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<KanbanToolbar {...props} />);
  return { props, view };
}

function fill(message: string, values: Record<string, string | number>) {
  return message.replace(/\{\{(\w+)\}\}/g, (_m, key: string) =>
    String(values[key]),
  );
}

describe("KanbanToolbar", () => {
  it("FUI[wm.kanban.toolbar:toolbar] renders search, the view bar slot and add, and emits search and add", () => {
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

      // The view controls come from the view bar slot; no filter is active, so no no-results bar.
      expect(
        screen.getByRole("button", { name: /^(Lọc|Filter)/ }),
      ).toBeTruthy();
      expect(screen.queryByRole("status")).toBeNull();

      fireEvent.click(screen.getByRole("button", { name: copy.addItem }));
      expect(props.onAddItem).toHaveBeenCalledOnce();
      view.unmount();
    }
  });

  it("uses the mockup Vietnamese wording", () => {
    renderToolbar(kanbanToolbarNoResultsScenario("vi"));
    expect(screen.getByPlaceholderText("Tìm trong board…")).toBeTruthy();
    expect(screen.getByRole("status").textContent).toBe(
      "Không có việc khớp bộ lọc · 16 việc của board đang bị ẩn" + "Bỏ lọc",
    );
    expect(screen.getByRole("button", { name: "Thêm công việc" })).toBeTruthy();
  });

  it("FUI[wm.kanban.toolbar:read-only] hides add for a viewer who cannot add and keeps search and the view bar", () => {
    for (const locale of LOCALES) {
      const copy = kanbanToolbarCopyFixture(locale);
      const { view } = renderToolbar(kanbanToolbarReadOnlyScenario(locale));
      expect(screen.queryByRole("button", { name: copy.addItem })).toBeNull();
      expect(
        screen.getByRole("searchbox", { name: copy.search.label }),
      ).toBeTruthy();
      expect(
        screen.getByRole("button", { name: /^(Lọc|Filter)/ }),
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
      expect(props.onClearFilter).toHaveBeenCalledOnce();
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

  it("keeps a long search readable in the edge data", () => {
    const scenario = kanbanToolbarEdgeDataScenario("vi");
    renderToolbar(scenario);
    expect(
      (
        screen.getByRole("searchbox", {
          name: "Tìm trong board",
        }) as HTMLInputElement
      ).value,
    ).toBe(scenario.searchQuery);
  });
});
