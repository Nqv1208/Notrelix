import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  membersCopyFixture,
  membersInvitationsScenario,
} from "../../../verification/workspace-members-fixtures";
import type { InviteManyOutcome } from "../../../core/api/invitations.service";
import {
  WorkspaceMembersSurface,
  type WorkspaceMembersSurfaceProps,
} from "../workspace-members-surface";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = membersCopyFixture();
const labels = copy.invitations.results;

const results: readonly InviteManyOutcome[] = [
  { email: "ana@example.com", outcome: "sent", code: null },
  { email: "ben@example.com", outcome: "already-member", code: null },
  { email: "cam@example.com", outcome: "already-pending", code: null },
  { email: "dan@example.com", outcome: "invalid", code: null },
  {
    email: "eve@example.com",
    outcome: "refused",
    code: "workspace.guest_invites_off",
  },
  { email: "fay@example.com", outcome: "refused", code: "Workspaces_X" },
  {
    email: "gus@example.com",
    outcome: "refused",
    code: "Workspaces_Invitation_GuestEntryRoleTooPrivileged",
  },
];

function renderResults(inviteResults: readonly InviteManyOutcome[] | null) {
  const onDismissInviteResults = vi.fn();
  const props: WorkspaceMembersSurfaceProps = {
    ...membersInvitationsScenario(),
    onTabChange: vi.fn(),
    onChangeRole: vi.fn(),
    onRemove: vi.fn(),
    onSuspend: vi.fn(),
    onActivate: vi.fn(),
    onInvite: vi.fn(),
    onInviteMany: vi.fn(),
    onRevoke: vi.fn(),
    onRetryMembers: vi.fn(),
    onRetryInvitations: vi.fn(),
    inviteResults,
    onDismissInviteResults,
  };
  renderPureUi(<WorkspaceMembersSurface {...props} />);
  return { onDismissInviteResults };
}

describe("invite-many results", () => {
  it("FUI[workspace.members:invite-results] lists every address with its own outcome and a one-line summary", () => {
    renderResults(results);
    const dialog = screen.getByRole("alertdialog");
    expect(within(dialog).getByText(labels.title)).toBeTruthy();
    expect(
      within(dialog).getByText(
        labels.summary.replace("{{sent}}", "1").replace("{{total}}", "7"),
      ),
    ).toBeTruthy();
    const row = (email: string) =>
      within(dialog).getByText(email).closest("li") as HTMLElement;
    expect(within(row("ana@example.com")).getByText(labels.sent)).toBeTruthy();
    expect(
      within(row("ben@example.com")).getByText(labels.alreadyMember),
    ).toBeTruthy();
    expect(
      within(row("cam@example.com")).getByText(labels.alreadyPending),
    ).toBeTruthy();
    expect(
      within(row("dan@example.com")).getByText(labels.invalid),
    ).toBeTruthy();
    expect(
      within(row("eve@example.com")).getByText(labels.guestInvitesOff),
    ).toBeTruthy();
    expect(
      within(row("fay@example.com")).getByText(labels.refused),
    ).toBeTruthy();
    expect(
      within(row("gus@example.com")).getByText(labels.entryRefused),
    ).toBeTruthy();
  });

  it("FUI[workspace.members:invite-results] closes from Done and shows nothing when there are no results", () => {
    const { onDismissInviteResults } = renderResults(results);
    fireEvent.click(screen.getByRole("button", { name: labels.done }));
    expect(onDismissInviteResults).toHaveBeenCalled();
  });

  it("FUI[workspace.members:invite-results] shows no dialog before a batch was sent", () => {
    renderResults(null);
    expect(screen.queryByRole("alertdialog")).toBeNull();
  });
});
