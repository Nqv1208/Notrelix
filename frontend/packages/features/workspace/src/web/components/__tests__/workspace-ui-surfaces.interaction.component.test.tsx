import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, waitFor } from "@notrelix/testing";

import {
  dashboardDefaultScenario,
  invitationsDefaultScenario,
  invitationsEmptyScenario,
  workspaceDirectoryDefaultScenario,
  workspaceHeaderDefaultScenario,
  workspaceTabsDefaultScenario,
} from "../../../verification/workspace-ui-fixtures";
import {
  PendingInvitationsMenuSurface,
  WorkspaceCompactHeaderSurface,
  WorkspaceDashboardSurface,
  WorkspaceDirectorySurface,
  WorkspaceViewTabsSurface,
} from "../workspace-ui-surfaces";

describe("workspace web pure surfaces", () => {
  it("FUI[workspace.directory:render] renders the workspace directory from deterministic fixtures", () => {
    renderPureUi(
      <WorkspaceDirectorySurface
        workspaces={workspaceDirectoryDefaultScenario()}
      />,
    );

    expect(screen.getByText("Product")).toBeTruthy();
    expect(screen.getByText("Design")).toBeTruthy();
    expect(screen.getByText("Enterprise Rollout")).toBeTruthy();
  });

  it("FUI[workspace.directory:open] routes workspace opening through the injected callback", () => {
    const onOpenWorkspace = vi.fn();

    renderPureUi(
      <WorkspaceDirectorySurface
        workspaces={workspaceDirectoryDefaultScenario()}
        onOpenWorkspace={onOpenWorkspace}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: /Product/ }));

    expect(onOpenWorkspace).toHaveBeenCalledWith("ws-product");
  });

  it("FUI[workspace.view-tabs:render] exposes a semantic tablist without nested interactive nodes", () => {
    const onSelectView = vi.fn();

    renderPureUi(
      <WorkspaceViewTabsSurface
        workspaceId="ws-main"
        views={workspaceTabsDefaultScenario()}
        activeViewId="view-board"
        onSelectView={onSelectView}
      />,
    );

    const tablist = screen.getByRole("tablist", { name: "Workspace views" });
    const tabs = screen.getAllByRole("tab");

    expect(tablist.querySelectorAll(":scope > [role='tab']")).toHaveLength(
      tabs.length,
    );
    expect(tabs).toHaveLength(3);
    expect(
      tabs.every(
        (tab) =>
          tab.querySelector("button, a, input, select, textarea") === null,
      ),
    ).toBe(true);
    expect(screen.getByRole("tab", { name: "Board" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(screen.getByRole("tab", { name: "Board" })).toHaveAttribute(
      "tabindex",
      "0",
    );
    expect(screen.getByRole("tab", { name: "Calendar" })).toHaveAttribute(
      "aria-selected",
      "false",
    );
    expect(screen.getByRole("tab", { name: "Calendar" })).toHaveAttribute(
      "tabindex",
      "-1",
    );
  });

  it("FUI[workspace.view-tabs:select] preserves focus and keyboard activation semantics", () => {
    const onSelectView = vi.fn();

    renderPureUi(
      <WorkspaceViewTabsSurface
        workspaceId="ws-main"
        views={workspaceTabsDefaultScenario()}
        activeViewId="view-board"
        onSelectView={onSelectView}
      />,
    );

    const calendarTab = screen.getByRole("tab", { name: "Calendar" });
    calendarTab.focus();
    expect(document.activeElement).toBe(calendarTab);

    fireEvent.click(calendarTab);

    expect(onSelectView).toHaveBeenCalledWith(
      expect.objectContaining({ id: "view-calendar" }),
    );
  });

  it("FUI[workspace.view-tabs:reorder] emits the reordered view ids from keyboard drag", async () => {
    const onReorderViews = vi.fn();

    renderPureUi(
      <WorkspaceViewTabsSurface
        workspaceId="ws-main"
        views={workspaceTabsDefaultScenario()}
        activeViewId="view-board"
        onReorderViews={onReorderViews}
      />,
    );

    const boardTab = screen.getByRole("tab", { name: "Board" });
    screen.getAllByRole("tab").forEach((tab, index) => {
      vi.spyOn(tab, "getBoundingClientRect").mockReturnValue(
        new DOMRect(index * 100, 0, 100, 36),
      );
    });
    boardTab.focus();
    fireEvent.keyDown(boardTab, { code: "Space", key: " " });
    await new Promise((resolve) => setTimeout(resolve, 0));
    fireEvent.keyDown(boardTab, { code: "ArrowRight", key: "ArrowRight" });
    fireEvent.keyDown(boardTab, { code: "Space", key: " " });

    await waitFor(() => {
      expect(onReorderViews).toHaveBeenCalledWith([
        "view-table",
        "view-board",
        "view-calendar",
      ]);
    });
  });

  it("FUI[workspace.compact-header:render] routes compact header actions through injected callbacks", () => {
    const onCopyLink = vi.fn();
    const onInvite = vi.fn();

    renderPureUi(
      <WorkspaceCompactHeaderSurface
        {...workspaceHeaderDefaultScenario()}
        onCopyLink={onCopyLink}
        onInvite={onInvite}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Invite" }));
    expect(onInvite).toHaveBeenCalledTimes(1);

    fireEvent.click(screen.getByRole("button", { name: /Product/ }));
    fireEvent.click(
      screen.getByRole("button", { name: /Copy workspace link/ }),
    );
    expect(onCopyLink).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.pending-invitations:render] accepts pending invitations through the injected callback", () => {
    const onAccept = vi.fn();

    renderPureUi(
      <PendingInvitationsMenuSurface
        invitations={invitationsDefaultScenario()}
        onAccept={onAccept}
      />,
    );

    fireEvent.click(
      screen.getByRole("button", { name: "Pending workspace invitations" }),
    );
    const acceptButtons = screen.getAllByRole("button", { name: "Accept" });
    fireEvent.click(acceptButtons[0]!);

    expect(onAccept).toHaveBeenCalledWith(
      expect.objectContaining({ id: "invite-1" }),
    );
  });

  it("FUI[workspace.pending-invitations:empty] renders the empty invitations state without query providers", () => {
    renderPureUi(
      <PendingInvitationsMenuSurface
        invitations={invitationsEmptyScenario()}
      />,
    );

    fireEvent.click(
      screen.getByRole("button", { name: "Pending workspace invitations" }),
    );

    expect(screen.getByText("No pending invitations")).toBeTruthy();
  });

  it("FUI[workspace.dashboard:render] renders the workspace dashboard from deterministic fixtures", () => {
    renderPureUi(<WorkspaceDashboardSurface {...dashboardDefaultScenario()} />);

    expect(screen.getByText("Product")).toBeTruthy();
    expect(screen.getByText("Ada Lovelace")).toBeTruthy();
    expect(screen.getByText("Operating plan")).toBeTruthy();
  });
});
