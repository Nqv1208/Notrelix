import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  invitationsDefaultScenario,
  invitationsEmptyScenario,
} from "../../../verification/workspace-ui-fixtures";
import { PendingInvitationsMenuSurface } from "../workspace-ui-surfaces";

describe("workspace web pure surfaces", () => {
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
});
