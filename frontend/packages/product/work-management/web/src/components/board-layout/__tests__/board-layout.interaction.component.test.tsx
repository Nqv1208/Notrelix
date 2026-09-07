import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { BoardToolbar } from "../board-toolbar";
import { ViewTabs, AddViewMenu } from "../view-tabs";

describe("BoardToolbar interactions", () => {
  it("FUI[wm.board-layout.toolbar:filter] emits the filter callback exactly once", () => {
    const onFilter = vi.fn();
    renderPureUi(
      <BoardToolbar
        boardTitle="Operating plan"
        activeView="kanban"
        onViewChange={vi.fn()}
        onFilter={onFilter}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Filter board" }));
    expect(onFilter).toHaveBeenCalledOnce();
  });

  it("FUI[wm.board-layout.toolbar:view] emits view selection through view tabs", () => {
    const onViewChange = vi.fn();
    renderPureUi(
      <BoardToolbar
        boardTitle="Operating plan"
        activeView="kanban"
        onViewChange={onViewChange}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /Table/ }));
    expect(onViewChange).toHaveBeenCalledOnce();
    expect(onViewChange).toHaveBeenCalledWith("table");
  });

  it("FUI[wm.board-layout.toolbar:search] emits controlled search changes", () => {
    const onSearchChange = vi.fn();
    renderPureUi(
      <BoardToolbar
        boardTitle="Operating plan"
        activeView="kanban"
        onViewChange={vi.fn()}
        searchQuery=""
        onSearchChange={onSearchChange}
      />,
    );
    fireEvent.change(screen.getByPlaceholderText("Search..."), {
      target: { value: "alpha" },
    });
    expect(onSearchChange).toHaveBeenCalledWith("alpha");
  });

  it("FUI[wm.board-layout.toolbar:add-view] keeps local AddView behavior observable", () => {
    const onAddView = vi.fn();
    renderPureUi(
      <BoardToolbar
        boardTitle="Operating plan"
        activeView="kanban"
        onViewChange={onAddView}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Add view" }));
    const timelineMatches = screen.getAllByText("Timeline");
    expect(timelineMatches).toHaveLength(2);
    const menuTimeline = timelineMatches[1];
    if (!menuTimeline) throw new Error("menu Timeline item missing");
    fireEvent.click(menuTimeline);
    expect(onAddView).toHaveBeenCalledWith("timeline");
  });
});

describe("ViewTabs interactions", () => {
  it("FUI[wm.board-layout.view-tabs:select] emits the selected view type", () => {
    const onViewChange = vi.fn();
    renderPureUi(<ViewTabs activeView="kanban" onViewChange={onViewChange} />);
    fireEvent.click(screen.getByRole("button", { name: /Calendar/ }));
    expect(onViewChange).toHaveBeenCalledOnce();
    expect(onViewChange).toHaveBeenCalledWith("calendar");
  });

  it("FUI[wm.board-layout.view-tabs:active] marks the active view", () => {
    renderPureUi(<ViewTabs activeView="table" onViewChange={vi.fn()} />);
    const activeTab = screen.getByRole("button", { name: /Table/ });
    expect(activeTab.className).toContain("bg-muted");
  });
});


describe("Board layout keyboard semantics", () => {
  it("FUI[wm.board-layout.view-tabs:keyboard] keeps native keyboard operability for focusable tab buttons", () => {
    const onViewChange = vi.fn();
    renderPureUi(<ViewTabs activeView="kanban" onViewChange={onViewChange} />);
    const tab = screen.getByRole("button", { name: /Table/ });
    tab.focus();
    expect(document.activeElement).toBe(tab);
    expect(tab.tagName).toBe("BUTTON");
    fireEvent.click(tab);
    expect(onViewChange).toHaveBeenCalledWith("table");
  });

  it("FUI[wm.board-layout.view-tabs:escape] closes the AddView menu on Escape", () => {
    renderPureUi(<AddViewMenu onAddView={vi.fn()} />);
    fireEvent.click(screen.getByRole("button", { name: "Add view" }));
    expect(screen.getByText("Board")).toBeTruthy();
    fireEvent.keyDown(document, { key: "Escape" });
    expect(screen.queryByText("Board")).toBeNull();
  });
});

describe("AddViewMenu interactions", () => {
  it("FUI[wm.board-layout.view-tabs:add] toggles the local menu and emits a view type", () => {
    const onAddView = vi.fn();
    renderPureUi(<AddViewMenu onAddView={onAddView} />);
    expect(screen.queryByText("Board")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Add view" }));
    expect(screen.getByText("Board")).toBeTruthy();
    fireEvent.click(screen.getByText("Board"));
    expect(onAddView).toHaveBeenCalledOnce();
    expect(onAddView).toHaveBeenCalledWith("kanban");
    expect(screen.queryByText("Board")).toBeNull();
  });
});
