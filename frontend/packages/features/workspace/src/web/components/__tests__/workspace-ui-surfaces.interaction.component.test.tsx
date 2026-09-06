import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

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
  it("renders the workspace directory from deterministic fixtures", () => {
    renderPureUi(
      <WorkspaceDirectorySurface
        workspaces={workspaceDirectoryDefaultScenario()}
      />,
    );

    expect(screen.getByText("Product")).toBeTruthy();
    expect(screen.getByText("Design")).toBeTruthy();
    expect(screen.getByText("Enterprise Rollout")).toBeTruthy();
  });

  it("routes workspace opening through the injected callback", () => {
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

  it("routes view selection through the injected callback", () => {
    const onSelectView = vi.fn();

    renderPureUi(
      <WorkspaceViewTabsSurface
        workspaceId="ws-main"
        views={workspaceTabsDefaultScenario()}
        onSelectView={onSelectView}
      />,
    );

    fireEvent.click(screen.getByRole("tab", { name: "Calendar" }));

    expect(onSelectView).toHaveBeenCalledWith(
      expect.objectContaining({ id: "view-calendar" }),
    );
  });

  it("routes compact header actions through injected callbacks", () => {
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

  it("accepts pending invitations through the injected callback", () => {
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

  it("renders the empty invitations state without query providers", () => {
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

  it("renders the workspace dashboard from deterministic fixtures", () => {
    renderPureUi(<WorkspaceDashboardSurface {...dashboardDefaultScenario()} />);

    expect(screen.getByText("Product")).toBeTruthy();
    expect(screen.getByText("Ada Lovelace")).toBeTruthy();
    expect(screen.getByText("Operating plan")).toBeTruthy();
  });
});
