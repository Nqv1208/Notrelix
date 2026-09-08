import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { WorkspaceContextualToolbar } from "../workspace-contextual-toolbar";

describe("WorkspaceContextualToolbar interactions", () => {
  it("FUI[workspace.contextual-toolbar:search] emits controlled search changes in kanban mode", () => {
    const onSearchChange = vi.fn();
    renderPureUi(
      <WorkspaceContextualToolbar
        activeType="kanban"
        searchQuery=""
        onSearchChange={onSearchChange}
        onAction={vi.fn()}
      />,
    );
    fireEvent.change(screen.getByPlaceholderText("Search cards"), {
      target: { value: "alpha" },
    });
    expect(onSearchChange).toHaveBeenCalledWith("alpha");
  });

  it("FUI[workspace.contextual-toolbar:action] emits typed action identifiers for every enabled kanban action", () => {
    const onAction = vi.fn();
    renderPureUi(
      <WorkspaceContextualToolbar
        activeType="kanban"
        searchQuery=""
        onSearchChange={vi.fn()}
        onAction={onAction}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /Person/ }));
    fireEvent.click(screen.getByRole("button", { name: /Filter/ }));
    fireEvent.click(screen.getByRole("button", { name: /Sort/ }));
    fireEvent.click(screen.getByRole("button", { name: /Group by status/ }));
    fireEvent.click(screen.getByRole("button", { name: /Board settings/ }));
    expect(onAction).toHaveBeenCalledWith("person");
    expect(onAction).toHaveBeenCalledWith("filter");
    expect(onAction).toHaveBeenCalledWith("sort");
    expect(onAction).toHaveBeenCalledWith("group");
    expect(onAction).toHaveBeenCalledWith("settings");
    expect(onAction).toHaveBeenCalledTimes(5);
  });

  it("FUI[workspace.contextual-toolbar:timeline-actions] emits timeline mode actions", () => {
    const onAction = vi.fn();
    renderPureUi(
      <WorkspaceContextualToolbar
        activeType="timeline"
        searchQuery=""
        onSearchChange={vi.fn()}
        onAction={onAction}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /Today/ }));
    fireEvent.click(screen.getByRole("button", { name: /Person/ }));
    fireEvent.click(screen.getByRole("button", { name: /Group by list/ }));
    fireEvent.click(screen.getByRole("button", { name: /Timeline settings/ }));
    expect(onAction).toHaveBeenCalledWith("today");
    expect(onAction).toHaveBeenCalledWith("person");
    expect(onAction).toHaveBeenCalledWith("group");
    expect(onAction).toHaveBeenCalledWith("settings");
  });

  it("FUI[workspace.contextual-toolbar:dashboard-actions] emits dashboard mode actions", () => {
    const onAction = vi.fn();
    renderPureUi(
      <WorkspaceContextualToolbar
        activeType="dashboard"
        searchQuery=""
        onSearchChange={vi.fn()}
        onAction={onAction}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /Add widget/ }));
    fireEvent.click(screen.getByRole("button", { name: /Refresh/ }));
    fireEvent.click(screen.getByRole("button", { name: /AI summary/ }));
    expect(onAction).toHaveBeenCalledWith("add-widget");
    expect(onAction).toHaveBeenCalledWith("refresh");
    expect(onAction).toHaveBeenCalledWith("ai-summary");
  });

  it("FUI[workspace.contextual-toolbar:calendar-actions] emits calendar mode today action", () => {
    const onAction = vi.fn();
    renderPureUi(
      <WorkspaceContextualToolbar
        activeType="calendar"
        searchQuery=""
        onSearchChange={vi.fn()}
        onAction={onAction}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /Today/ }));
    expect(onAction).toHaveBeenCalledWith("today");
  });

  it("FUI[workspace.contextual-toolbar:local] keeps calendar ToggleGroup local without providers", () => {
    renderPureUi(
      <WorkspaceContextualToolbar
        activeType="calendar"
        searchQuery=""
        onSearchChange={vi.fn()}
        onAction={vi.fn()}
      />,
    );
    const week = screen.getByRole("radio", { name: "Week" });
    expect(week.getAttribute("data-state")).toBe("off");
    fireEvent.click(week);
    expect(week.getAttribute("data-state")).toBe("on");
  });

  it("FUI[workspace.contextual-toolbar:doc-actions] emits doc mode add-block action", () => {
    const onAction = vi.fn();
    renderPureUi(
      <WorkspaceContextualToolbar
        activeType="doc"
        searchQuery=""
        onSearchChange={vi.fn()}
        onAction={onAction}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /Add block/ }));
    expect(onAction).toHaveBeenCalledWith("add-block");
  });
});
