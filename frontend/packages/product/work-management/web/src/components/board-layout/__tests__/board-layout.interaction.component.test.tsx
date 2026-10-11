import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { BoardHeaderCopyContext } from "../board-header-copy-context";
import { en, vi as viCopy } from "../board-header.copy";
import { BoardToolbar } from "../board-toolbar";
import { ViewTabs, AddViewMenu } from "../view-tabs";

beforeAll(() => {
  // Radix menus measure and scroll items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const baseToolbar = {
  boardTitle: "Operating plan",
  activeView: "kanban",
  copy: en,
};

/** Radix opens a menu trigger on Enter and selects a tab on mouse down. */
function openMenu(trigger: HTMLElement) {
  fireEvent.keyDown(trigger, { key: "Enter" });
}

describe("BoardToolbar interactions", () => {
  it("FUI[wm.board-layout.toolbar:filter] emits the filter callback exactly once", () => {
    const onFilter = vi.fn();
    renderPureUi(
      <BoardToolbar
        {...baseToolbar}
        onViewChange={vi.fn()}
        onFilter={onFilter}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Filter board" }));
    expect(onFilter).toHaveBeenCalledOnce();
  });

  it("FUI[wm.board-layout.toolbar:filter-absent] renders no filter control without a handler", () => {
    renderPureUi(<BoardToolbar {...baseToolbar} onViewChange={vi.fn()} />);
    expect(screen.queryByRole("button", { name: "Filter board" })).toBeNull();
  });

  it("FUI[wm.board-layout.toolbar:view] emits view selection through view tabs", () => {
    const onViewChange = vi.fn();
    renderPureUi(<BoardToolbar {...baseToolbar} onViewChange={onViewChange} />);
    fireEvent.mouseDown(screen.getByRole("tab", { name: "Table" }));
    expect(onViewChange).toHaveBeenCalledOnce();
    expect(onViewChange).toHaveBeenCalledWith("table");
  });

  it("FUI[wm.board-layout.toolbar:search] emits controlled search changes", () => {
    const onSearchChange = vi.fn();
    renderPureUi(
      <BoardToolbar
        {...baseToolbar}
        onViewChange={vi.fn()}
        searchQuery=""
        onSearchChange={onSearchChange}
      />,
    );
    fireEvent.change(screen.getByRole("textbox", { name: "Search board" }), {
      target: { value: "alpha" },
    });
    expect(onSearchChange).toHaveBeenCalledWith("alpha");
  });

  it("FUI[wm.board-layout.toolbar:add-view] emits the chosen view from the Add view menu", () => {
    const onAddView = vi.fn();
    renderPureUi(<BoardToolbar {...baseToolbar} onViewChange={onAddView} />);
    openMenu(screen.getByRole("button", { name: "Add view" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Timeline" }));
    expect(onAddView).toHaveBeenCalledWith("timeline");
  });

  it("FUI[wm.board-layout.toolbar:locale] reads vi copy from the route-provided context when no copy is passed", () => {
    renderPureUi(
      <BoardHeaderCopyContext.Provider value={viCopy}>
        <BoardToolbar
          boardTitle="Kế hoạch"
          activeView="kanban"
          onViewChange={vi.fn()}
          onFilter={vi.fn()}
          onSearchChange={vi.fn()}
        />
      </BoardHeaderCopyContext.Provider>,
    );
    expect(
      screen.getByRole("tablist", { name: viCopy.boardViews }),
    ).toBeTruthy();
    expect(screen.getByRole("button", { name: viCopy.filter })).toBeTruthy();
    expect(screen.getByRole("button", { name: viCopy.addView })).toBeTruthy();
    expect(screen.getByPlaceholderText(viCopy.search)).toBeTruthy();
    for (const name of Object.values(viCopy.views)) {
      expect(screen.getByRole("tab", { name })).toBeTruthy();
    }
  });
});

describe("ViewTabs interactions", () => {
  it("FUI[wm.board-layout.view-tabs:select] emits the selected view type", () => {
    const onViewChange = vi.fn();
    renderPureUi(
      <ViewTabs activeView="kanban" onViewChange={onViewChange} copy={en} />,
    );
    fireEvent.mouseDown(screen.getByRole("tab", { name: "Calendar" }));
    expect(onViewChange).toHaveBeenCalledOnce();
    expect(onViewChange).toHaveBeenCalledWith("calendar");
  });

  it("FUI[wm.board-layout.view-tabs:active] exposes a tablist with the active tab selected", () => {
    renderPureUi(
      <ViewTabs activeView="table" onViewChange={vi.fn()} copy={en} />,
    );
    const list = screen.getByRole("tablist", { name: "Board views" });
    const tabs = within(list).getAllByRole("tab");
    expect(tabs.map((tab) => tab.textContent)).toEqual([
      "Board",
      "Table",
      "Calendar",
      "Timeline",
    ]);
    expect(tabs.map((tab) => tab.getAttribute("aria-selected"))).toEqual([
      "false",
      "true",
      "false",
      "false",
    ]);
  });
});

describe("Board layout keyboard semantics", () => {
  it("FUI[wm.board-layout.view-tabs:keyboard] is one tab stop that moves focus with the arrow keys without switching view", async () => {
    const onViewChange = vi.fn();
    renderPureUi(
      <ViewTabs activeView="kanban" onViewChange={onViewChange} copy={en} />,
    );
    const list = screen.getByRole("tablist", { name: "Board views" });
    const board = screen.getByRole("tab", { name: "Board" });
    const table = screen.getByRole("tab", { name: "Table" });
    // The tab list is one tab stop and hands focus to the active tab.
    expect(list.getAttribute("tabindex")).toBe("0");
    expect(board.getAttribute("tabindex")).toBe("-1");
    list.focus();
    expect(document.activeElement).toBe(board);
    fireEvent.keyDown(board, { key: "ArrowRight" });
    await waitFor(() => expect(document.activeElement).toBe(table));
    expect(onViewChange).not.toHaveBeenCalled();
    fireEvent.keyDown(table, { key: "Enter" });
    expect(onViewChange).toHaveBeenCalledWith("table");
  });
});

describe("AddViewMenu interactions", () => {
  it("FUI[wm.board-layout.view-tabs:add] opens the menu and emits a view type", () => {
    const onAddView = vi.fn();
    renderPureUi(<AddViewMenu onAddView={onAddView} copy={en} />);
    expect(screen.queryByRole("menu")).toBeNull();
    openMenu(screen.getByRole("button", { name: "Add view" }));
    const menu = screen.getByRole("menu");
    expect(
      within(menu)
        .getAllByRole("menuitem")
        .map((item) => item.textContent),
    ).toEqual(["Board", "Table", "Calendar", "Timeline"]);
    fireEvent.click(within(menu).getByRole("menuitem", { name: "Board" }));
    expect(onAddView).toHaveBeenCalledOnce();
    expect(onAddView).toHaveBeenCalledWith("kanban");
    expect(screen.queryByRole("menu")).toBeNull();
  });

  it("FUI[wm.board-layout.view-tabs:menu-keyboard] moves between items with the arrow keys", async () => {
    renderPureUi(<AddViewMenu onAddView={vi.fn()} copy={en} />);
    openMenu(screen.getByRole("button", { name: "Add view" }));
    const menuItem = (index: number) => {
      const item = screen.getAllByRole("menuitem")[index];
      if (!item) throw new Error("menu item missing");
      return item;
    };
    await waitFor(() => expect(document.activeElement).toBe(menuItem(0)));
    fireEvent.keyDown(menuItem(0), { key: "ArrowDown" });
    await waitFor(() => expect(document.activeElement).toBe(menuItem(1)));
    fireEvent.keyDown(menuItem(1), { key: "ArrowUp" });
    await waitFor(() => expect(document.activeElement).toBe(menuItem(0)));
  });

  it("FUI[wm.board-layout.view-tabs:escape] closes the menu on Escape and returns focus to the trigger", async () => {
    renderPureUi(<AddViewMenu onAddView={vi.fn()} copy={en} />);
    const trigger = screen.getByRole("button", { name: "Add view" });
    openMenu(trigger);
    const item = screen.getAllByRole("menuitem")[0];
    if (!item) throw new Error("menu item missing");
    fireEvent.keyDown(item, { key: "Escape" });
    expect(screen.queryByRole("menu")).toBeNull();
    await waitFor(() => expect(document.activeElement).toBe(trigger));
  });

  it("FUI[wm.board-layout.view-tabs:outside-click] closes the menu on an outside pointer press without selecting", async () => {
    const onAddView = vi.fn();
    renderPureUi(
      <div>
        <button type="button">outside</button>
        <AddViewMenu onAddView={onAddView} copy={en} />
      </div>,
    );
    openMenu(screen.getByRole("button", { name: "Add view" }));
    expect(screen.getByRole("menu")).toBeTruthy();
    // Radix arms its outside-press listener on the next task.
    await new Promise((resolve) => setTimeout(resolve, 10));
    fireEvent.pointerDown(screen.getByText("outside"));
    await waitFor(() => expect(screen.queryByRole("menu")).toBeNull());
    expect(onAddView).not.toHaveBeenCalled();
  });

  it("FUI[wm.board-layout.view-tabs:stacking] renders the menu on the modal layer role, not a raw z-index or backdrop", () => {
    renderPureUi(<AddViewMenu onAddView={vi.fn()} copy={en} />);
    openMenu(screen.getByRole("button", { name: "Add view" }));
    const menu = screen.getByRole("menu");
    expect(menu.className).toContain("z-modal");
    expect(menu.className).not.toMatch(/\bz-(40|50)\b/);
    expect(document.querySelector(".fixed.inset-0")).toBeNull();
  });
});
