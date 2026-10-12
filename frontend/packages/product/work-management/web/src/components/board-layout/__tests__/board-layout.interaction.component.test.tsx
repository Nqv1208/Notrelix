import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { BoardHeaderCopyContext } from "../board-header-copy-context";
import { BoardHeaderDataContext } from "../board-header-data-context";
import { en, vi as viCopy, type BoardHeaderCopy } from "../board-header.copy";
import type { BoardHeaderData } from "../board-header-surface";
import { BoardToolbar } from "../board-toolbar";
import { ViewTabs, AddViewMenu } from "../view-tabs";

beforeAll(() => {
  // Radix menus measure and scroll items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const baseToolbar = {
  boardTitle: "Triển khai ERP Q4",
  activeView: "kanban",
};

const header: BoardHeaderData = {
  space: {
    name: "Phòng Công nghệ",
    initials: "CN",
    markTone: "blue",
    href: "/workspaces/ws-1/spaces/space-cn",
  },
  visibility: "space",
  access: {
    myLevel: "manager",
    mySource: { kind: "workspace-admin" },
    peopleCount: 4,
    guestCount: 0,
    sources: [
      { kind: "space", id: "space-cn", name: "Phòng Công nghệ", count: 4 },
    ],
  },
  people: [
    { id: "user-ha", name: "Nguyễn Thu Hà", initials: "HA", tone: "sage" },
  ],
  onCopyLink: () => undefined,
};

const LOCALES = [
  ["vi", viCopy],
  ["en", en],
] as const satisfies readonly (readonly [string, BoardHeaderCopy])[];

/** Radix opens a menu trigger on Enter and selects a tab on mouse down. */
function openMenu(trigger: HTMLElement) {
  fireEvent.keyDown(trigger, { key: "Enter" });
}

describe.each(LOCALES)("BoardToolbar interactions (%s)", (_locale, copy) => {
  it("FUI[wm.board-layout.toolbar:view] emits view selection through view tabs", () => {
    const onViewChange = vi.fn();
    renderPureUi(
      <BoardToolbar {...baseToolbar} copy={copy} onViewChange={onViewChange} />,
    );
    fireEvent.mouseDown(screen.getByRole("tab", { name: copy.views.table }));
    expect(onViewChange).toHaveBeenCalledOnce();
    expect(onViewChange).toHaveBeenCalledWith("table");
  });

  it("FUI[wm.board-layout.toolbar:add-view] emits the chosen view from the icon-only Add view menu", () => {
    const onAddView = vi.fn();
    renderPureUi(
      <BoardToolbar {...baseToolbar} copy={copy} onViewChange={onAddView} />,
    );
    const trigger = screen.getByRole("button", { name: copy.addView });
    expect(trigger.textContent).toBe("");
    openMenu(trigger);
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.views.timeline }),
    );
    expect(onAddView).toHaveBeenCalledWith("timeline");
  });

  it("FUI[wm.board-layout.toolbar:filter-absent] carries no search or filter control: the view toolbars own them", () => {
    renderPureUi(
      <BoardToolbar
        {...baseToolbar}
        copy={copy}
        header={header}
        onViewChange={vi.fn()}
      />,
    );
    expect(screen.queryByRole("textbox")).toBeNull();
    expect(screen.queryByRole("searchbox")).toBeNull();
  });

  it("FUI[wm.board-layout.toolbar:header-context] renders the header data the route provides through context", () => {
    renderPureUi(
      <BoardHeaderDataContext.Provider value={header}>
        <BoardToolbar {...baseToolbar} copy={copy} onViewChange={vi.fn()} />
      </BoardHeaderDataContext.Provider>,
    );
    expect(
      screen.getByRole("heading", { level: 1, name: "Triển khai ERP Q4" }),
    ).toBeTruthy();
    expect(screen.getByRole("link", { name: "Phòng Công nghệ" })).toBeTruthy();
    expect(
      screen.getByRole("button", { name: copy.header.moreOptions }),
    ).toBeTruthy();
  });

  it("FUI[wm.board-layout.toolbar:dom-order] keeps DOM order equal to the reading order with no order-* classes", () => {
    const { container } = renderPureUi(
      <BoardToolbar
        {...baseToolbar}
        copy={copy}
        header={header}
        onViewChange={vi.fn()}
      />,
    );
    expect(container.innerHTML).not.toMatch(/(^|[\s"':])order-/);
    const controls = [
      screen.getByRole("link", { name: "Phòng Công nghệ" }),
      screen.getByRole("heading", { level: 1 }),
      screen.getByRole("button", { name: "Phòng Công nghệ" }),
      screen.getByRole("button", { name: /^4 / }),
      screen.getByRole("button", { name: copy.header.settings }),
      screen.getByRole("button", { name: copy.header.share }),
      screen.getByRole("button", { name: copy.header.moreOptions }),
      screen.getByRole("tablist", { name: copy.boardViews }),
      screen.getByRole("button", { name: copy.addView }),
    ];
    for (let index = 1; index < controls.length; index += 1) {
      const previous = controls[index - 1];
      const current = controls[index];
      if (!previous || !current) throw new Error("control missing");
      expect(
        previous.compareDocumentPosition(current) &
          Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();
    }
  });
});

describe("BoardToolbar copy source", () => {
  it("FUI[wm.board-layout.toolbar:locale] reads vi copy from the route-provided context when no copy is passed", () => {
    renderPureUi(
      <BoardHeaderCopyContext.Provider value={viCopy}>
        <BoardToolbar {...baseToolbar} onViewChange={vi.fn()} />
      </BoardHeaderCopyContext.Provider>,
    );
    expect(screen.getByRole("tablist", { name: "Chế độ xem" })).toBeTruthy();
    expect(
      screen.getByRole("button", { name: "Thêm chế độ xem" }),
    ).toBeTruthy();
    expect(screen.getAllByRole("tab").map((tab) => tab.textContent)).toEqual([
      "Bảng",
      "Kanban",
      "Lịch",
      "Dòng thời gian",
    ]);
  });

  it("FUI[wm.board-layout.toolbar:untitled] falls back to English copy without a provider", () => {
    renderPureUi(<BoardToolbar {...baseToolbar} onViewChange={vi.fn()} />);
    expect(screen.getByRole("tablist", { name: en.boardViews })).toBeTruthy();
  });
});

describe.each(LOCALES)("ViewTabs interactions (%s)", (_locale, copy) => {
  it("FUI[wm.board-layout.view-tabs:select] emits the selected view type", () => {
    const onViewChange = vi.fn();
    renderPureUi(
      <ViewTabs activeView="kanban" onViewChange={onViewChange} copy={copy} />,
    );
    fireEvent.mouseDown(screen.getByRole("tab", { name: copy.views.calendar }));
    expect(onViewChange).toHaveBeenCalledOnce();
    expect(onViewChange).toHaveBeenCalledWith("calendar");
  });

  it("FUI[wm.board-layout.view-tabs:active] lists Table, Kanban, Calendar and Timeline in that order with the active tab selected", () => {
    renderPureUi(
      <ViewTabs activeView="kanban" onViewChange={vi.fn()} copy={copy} />,
    );
    const list = screen.getByRole("tablist", { name: copy.boardViews });
    const tabs = within(list).getAllByRole("tab");
    expect(tabs.map((tab) => tab.textContent)).toEqual([
      copy.views.table,
      copy.views.kanban,
      copy.views.calendar,
      copy.views.timeline,
    ]);
    expect(tabs.map((tab) => tab.getAttribute("aria-selected"))).toEqual([
      "false",
      "true",
      "false",
      "false",
    ]);
  });

  it("FUI[wm.board-layout.view-tabs:keyboard] is one tab stop that moves focus with the arrow keys without switching view", async () => {
    const onViewChange = vi.fn();
    renderPureUi(
      <ViewTabs activeView="table" onViewChange={onViewChange} copy={copy} />,
    );
    const list = screen.getByRole("tablist", { name: copy.boardViews });
    const table = screen.getByRole("tab", { name: copy.views.table });
    const kanban = screen.getByRole("tab", { name: copy.views.kanban });
    // The tab list is one tab stop and hands focus to the active tab.
    expect(list.getAttribute("tabindex")).toBe("0");
    expect(table.getAttribute("tabindex")).toBe("-1");
    list.focus();
    expect(document.activeElement).toBe(table);
    fireEvent.keyDown(table, { key: "ArrowRight" });
    await waitFor(() => expect(document.activeElement).toBe(kanban));
    expect(onViewChange).not.toHaveBeenCalled();
    fireEvent.keyDown(kanban, { key: "Enter" });
    expect(onViewChange).toHaveBeenCalledWith("kanban");
  });
});

describe("Board layout accessibility structure", () => {
  it("FUI[wm.board-layout.view-tabs:aria-controls] leaves no tab pointing at a missing element", () => {
    renderPureUi(
      <ViewTabs activeView="table" onViewChange={vi.fn()} copy={en} />,
    );
    for (const tab of screen.getAllByRole("tab")) {
      const controls = tab.getAttribute("aria-controls");
      if (controls) expect(document.getElementById(controls)).not.toBeNull();
    }
  });

  it("FUI[wm.board-layout.view-tabs:selected-style] underlines the selected tab with the primary token and no fill", () => {
    renderPureUi(
      <ViewTabs activeView="table" onViewChange={vi.fn()} copy={en} />,
    );
    const selected = screen.getByRole("tab", { name: "Table" });
    const other = screen.getByRole("tab", { name: "Kanban" });
    expect(selected.getAttribute("data-state")).toBe("active");
    expect(selected.className).toContain(
      "data-[state=active]:border-b-primary",
    );
    expect(selected.className).toContain("data-[state=active]:font-semibold");
    expect(selected.className).not.toContain("bg-state-selected");
    expect(other.getAttribute("data-state")).toBe("inactive");
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
    ).toEqual(["Table", "Kanban", "Calendar", "Timeline"]);
    fireEvent.click(within(menu).getByRole("menuitem", { name: "Kanban" }));
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
