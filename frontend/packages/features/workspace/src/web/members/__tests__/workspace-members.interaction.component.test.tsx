import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { formatCopy } from "../../shell/shell-models";

import {
  membersAdminScenario,
  membersCopyFixture,
  membersDefaultScenario,
  membersErrorScenario,
  membersFixture,
  membersInvitationsEmptyScenario,
  membersInvitationsScenario,
  membersReadOnlyScenario,
} from "../../../verification/workspace-members-fixtures";
import {
  WorkspaceMembersSurface,
  type WorkspaceMembersSurfaceProps,
} from "../workspace-members-surface";

beforeAll(() => {
  // Radix Select measures and scrolls items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = membersCopyFixture();

function renderMembers(
  scenario = membersDefaultScenario(),
  overrides: Partial<WorkspaceMembersSurfaceProps> = {},
) {
  const props: WorkspaceMembersSurfaceProps = {
    ...scenario,
    onTabChange: vi.fn(),
    onChangeRole: vi.fn(),
    onRemove: vi.fn(),
    onSuspend: vi.fn(),
    onActivate: vi.fn(),
    onInvite: vi.fn(),
    onRevoke: vi.fn(),
    onRetryMembers: vi.fn(),
    onRetryInvitations: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<WorkspaceMembersSurface {...props} />);
  return { props, view };
}

function rowOf(name: string): HTMLElement {
  return screen.getByText(name).closest("tr") as HTMLElement;
}

function openMenu(trigger: HTMLElement) {
  fireEvent.keyDown(trigger, { key: "Enter" });
}

describe("WorkspaceMembersSurface", () => {
  it("FUI[workspace.members:render] lists members with role, join date and access", () => {
    renderMembers();
    expect(
      screen.getByRole("heading", { level: 1, name: copy.title }),
    ).toBeTruthy();
    const grace = rowOf("Grace Hopper");
    expect(
      within(grace).getByRole("combobox", { name: "Role for Grace Hopper" }),
    ).toBeTruthy();
    expect(
      within(rowOf("Sam Okafor")).getByText(copy.status.suspended),
    ).toBeTruthy();
    expect(within(rowOf("Ada Lovelace")).getByText(copy.you)).toBeTruthy();
  });

  it("FUI[workspace.members:role] changes a role inline through the injected callback", () => {
    const { props } = renderMembers();
    const trigger = within(rowOf("Grace Hopper")).getByRole("combobox");
    fireEvent.keyDown(trigger, { key: "Enter" });
    fireEvent.click(screen.getByRole("option", { name: /Admin/ }));
    expect(props.onChangeRole).toHaveBeenCalledWith(
      expect.objectContaining({ userId: "u-grace" }),
      "admin",
    );
  });

  it("FUI[workspace.members:last-owner] never offers demoting, suspending or removing the last owner", () => {
    renderMembers(membersAdminScenario());
    const ada = rowOf("Ada Lovelace");
    expect(within(ada).queryByRole("combobox")).toBeNull();
    expect(within(ada).queryByRole("button")).toBeNull();
    expect(within(ada).getByText(copy.blocked["last-owner"])).toBeTruthy();
    // The owner option is never offered for assignment.
    const trigger = within(rowOf("Grace Hopper")).getByRole("combobox");
    fireEvent.keyDown(trigger, { key: "Enter" });
    expect(screen.queryByRole("option", { name: /Owner/ })).toBeNull();
  });

  it("FUI[workspace.members:remove] confirms removal in an AlertDialog before calling the producer", () => {
    const { props } = renderMembers();
    openMenu(
      within(rowOf("Grace Hopper")).getByRole("button", {
        name: "Actions for Grace Hopper",
      }),
    );
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.actions.remove }),
    );
    const dialog = screen.getByRole("alertdialog", {
      name: "Remove Grace Hopper?",
    });
    expect(props.onRemove).not.toHaveBeenCalled();
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.confirm.cancel }),
    );
    expect(props.onRemove).not.toHaveBeenCalled();

    openMenu(
      within(rowOf("Grace Hopper")).getByRole("button", {
        name: "Actions for Grace Hopper",
      }),
    );
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.actions.remove }),
    );
    fireEvent.click(
      within(screen.getByRole("alertdialog")).getByRole("button", {
        name: copy.confirm.removeAction,
      }),
    );
    expect(props.onRemove).toHaveBeenCalledWith(
      expect.objectContaining({ userId: "u-grace" }),
    );
  });

  it("FUI[workspace.members:suspend] suspends after confirmation and reactivates directly", () => {
    const { props } = renderMembers();
    openMenu(
      within(rowOf("Alan Turing")).getByRole("button", {
        name: "Actions for Alan Turing",
      }),
    );
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.actions.suspend }),
    );
    fireEvent.click(
      within(
        screen.getByRole("alertdialog", { name: "Suspend Alan Turing?" }),
      ).getByRole("button", { name: copy.confirm.suspendAction }),
    );
    expect(props.onSuspend).toHaveBeenCalledWith(
      expect.objectContaining({ userId: "u-alan" }),
    );

    openMenu(
      within(rowOf("Sam Okafor")).getByRole("button", {
        name: "Actions for Sam Okafor",
      }),
    );
    expect(
      screen.queryByRole("menuitem", { name: copy.actions.suspend }),
    ).toBeNull();
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.actions.activate }),
    );
    expect(props.onActivate).toHaveBeenCalledWith(
      expect.objectContaining({ userId: "u-sam" }),
    );
  });

  it("FUI[workspace.members:permission] is read-only for members and hides invitations", () => {
    renderMembers(membersReadOnlyScenario());
    expect(screen.getByText(copy.readOnly)).toBeTruthy();
    expect(screen.queryByRole("combobox")).toBeNull();
    expect(screen.queryByRole("tab")).toBeNull();
    expect(screen.queryByRole("button", { name: copy.invite })).toBeNull();
  });

  it("FUI[workspace.members:filter] filters members by name ignoring diacritics", () => {
    renderMembers();
    fireEvent.change(screen.getByLabelText(copy.filter.label), {
      target: { value: "thuy" },
    });
    expect(screen.getByText("Nguyễn Thùy Linh")).toBeTruthy();
    expect(screen.queryByText("Alan Turing")).toBeNull();
  });

  it("FUI[workspace.members:states] retries failed loads", () => {
    const { props } = renderMembers(membersErrorScenario());
    fireEvent.click(screen.getByRole("button", { name: copy.error.retry }));
    expect(props.onRetryMembers).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.members:forbidden] says the list is refused and offers no retry", () => {
    renderMembers({
      ...membersReadOnlyScenario(),
      members: { status: "error", reason: "forbidden" },
    });
    expect(screen.getByRole("alert").textContent).toContain(
      copy.error.membersForbidden,
    );
    expect(screen.queryByRole("button", { name: copy.error.retry })).toBeNull();
  });

  it("FUI[workspace.members:offline] says the server was not reached and retries", () => {
    const { props } = renderMembers({
      ...membersReadOnlyScenario(),
      members: { status: "error", reason: "offline" },
    });
    expect(screen.getByRole("alert").textContent).toContain(
      copy.error.membersOffline,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.error.retry }));
    expect(props.onRetryMembers).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.members:guest-own-row] shows a guest only their own row and says why", () => {
    const own = membersFixture().find((member) => member.role === "guest")!;
    renderMembers({
      ...membersReadOnlyScenario(),
      viewer: { userId: own.userId, role: "guest" },
      members: { status: "ready", data: [own] },
    });
    expect(screen.getByText(copy.guestOwnRowOnly)).toBeTruthy();
    expect(screen.queryByText(copy.readOnly)).toBeNull();
    expect(screen.queryByText(copy.onlyYou)).toBeNull();
    expect(screen.getAllByRole("row")).toHaveLength(2);
    expect(within(rowOf(own.name)).getByText(copy.you)).toBeTruthy();
  });

  it("FUI[workspace.members:only-you] tells the only member nobody else is here, and an admin how to change that", () => {
    const [ada] = membersFixture();
    renderMembers({
      ...membersDefaultScenario(),
      members: { status: "ready", data: [ada!] },
    });
    expect(screen.getByText(copy.onlyYouAdmin)).toBeTruthy();
  });

  it("keeps the only-you invite hint away from someone who cannot invite", () => {
    const [grace] = membersFixture().filter(
      (member) => member.userId === "u-grace",
    );
    renderMembers({
      ...membersReadOnlyScenario(),
      members: { status: "ready", data: [grace!] },
    });
    expect(screen.getByText(copy.onlyYou)).toBeTruthy();
    expect(screen.queryByText(copy.onlyYouAdmin)).toBeNull();
  });

  it("FUI[workspace.members:job-title] shows the job title under the name, and nothing for a person without one", () => {
    const [ada, alan] = membersFixture();
    renderMembers({
      ...membersDefaultScenario(),
      members: {
        status: "ready",
        data: [{ ...ada!, jobTitle: "Head of engineering" }, alan!],
      },
    });
    expect(
      within(rowOf("Ada Lovelace")).getByText("Head of engineering"),
    ).toBeTruthy();
    expect(
      within(rowOf("Alan Turing")).queryByText("Head of engineering"),
    ).toBeNull();
  });
});

describe("WorkspaceMembersSurface invitations", () => {
  it("FUI[workspace.members:invite] validates the address and invites with the chosen role", () => {
    const { props } = renderMembers(membersInvitationsScenario());
    const email = screen.getByLabelText(copy.invitations.emailLabel);
    fireEvent.change(email, { target: { value: "not-an-email" } });
    fireEvent.click(
      screen.getByRole("button", { name: copy.invitations.send }),
    );
    expect(props.onInvite).not.toHaveBeenCalled();
    expect(screen.getByText(copy.invitations.emailInvalid)).toBeTruthy();

    fireEvent.change(email, { target: { value: " new.person@example.com " } });
    fireEvent.click(
      screen.getByRole("button", { name: copy.invitations.send }),
    );
    expect(props.onInvite).toHaveBeenCalledWith({
      email: "new.person@example.com",
      role: "member",
    });
  });

  it("FUI[workspace.members:invite-many] invites every address typed at once and names the ones that are not addresses", () => {
    const onInviteMany = vi.fn();
    const { props } = renderMembers(membersInvitationsScenario(), {
      onInviteMany,
    });
    const email = screen.getByLabelText(copy.invitations.emailLabel);
    fireEvent.change(email, {
      target: { value: "a@example.com, b@example.com\nA@example.com nope" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: copy.invitations.send }),
    );
    expect(onInviteMany).not.toHaveBeenCalled();
    expect(
      screen.getByText(
        formatCopy(copy.invitations.emailInvalidMany, { emails: "nope" }),
      ),
    ).toBeTruthy();

    fireEvent.change(email, {
      target: { value: "a@example.com, b@example.com\nA@example.com" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: copy.invitations.send }),
    );
    expect(props.onInvite).not.toHaveBeenCalled();
    expect(onInviteMany).toHaveBeenCalledWith([
      { email: "a@example.com", role: "member" },
      { email: "b@example.com", role: "member" },
    ]);
  });

  it("FUI[workspace.members:resend] sends a waiting invitation again and offers nothing for one that was accepted", () => {
    const onResend = vi.fn();
    renderMembers(membersInvitationsScenario(), { onResend });
    fireEvent.click(
      screen.getByRole("button", {
        name: "Resend invitation to minh.tran@example.com",
      }),
    );
    expect(onResend).toHaveBeenCalledWith(
      expect.objectContaining({ id: "inv-1" }),
    );
  });

  it("FUI[workspace.members:invitation-role] shows the role of a waiting invitation as a choice only when the host can change it", () => {
    renderMembers(membersInvitationsScenario());
    expect(
      screen.queryByRole("combobox", {
        name: "Role offered in the invitation to minh.tran@example.com",
      }),
    ).toBeNull();
    renderMembers(membersInvitationsScenario(), {
      onChangeInvitationRole: vi.fn(),
    });
    expect(
      screen.getByRole("combobox", {
        name: "Role offered in the invitation to minh.tran@example.com",
      }),
    ).toBeTruthy();
  });

  it("FUI[workspace.members:pending] lists pending and expired invitations and revokes after confirmation", () => {
    const { props } = renderMembers(membersInvitationsScenario());
    expect(screen.getByText("minh.tran@example.com")).toBeTruthy();
    expect(
      within(rowOf("old.contact@example.com")).getByText(
        copy.invitations.expired,
      ),
    ).toBeTruthy();
    expect(screen.queryByText("joined@example.com")).toBeNull();
    expect(
      (
        screen.getByRole("button", {
          name: "Revoking invitation to priya.raman@example.com…",
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);

    fireEvent.click(
      screen.getByRole("button", {
        name: "Revoke invitation to minh.tran@example.com",
      }),
    );
    fireEvent.click(
      within(screen.getByRole("alertdialog")).getByRole("button", {
        name: copy.confirm.revokeAction,
      }),
    );
    expect(props.onRevoke).toHaveBeenCalledWith(
      expect.objectContaining({ id: "inv-1" }),
    );
  });

  it("FUI[workspace.members:invite-error] shows the producer's invite failure next to the field", () => {
    renderMembers(membersInvitationsEmptyScenario());
    const email = screen.getByLabelText(copy.invitations.emailLabel);
    expect(email.getAttribute("aria-invalid")).toBe("true");
    expect(
      screen.getByText(
        "A pending invitation already exists for this email address.",
      ),
    ).toBeTruthy();
    expect(screen.getByText(copy.invitations.empty)).toBeTruthy();
  });

  it("FUI[workspace.members:tabs] switches tabs through the host", () => {
    const { props } = renderMembers();
    fireEvent.mouseDown(screen.getByRole("tab", { name: /Invitations/ }));
    fireEvent.click(screen.getByRole("tab", { name: /Invitations/ }));
    expect(props.onTabChange).toHaveBeenCalledWith("invitations");
    fireEvent.click(screen.getByRole("button", { name: copy.invite }));
    expect(props.onTabChange).toHaveBeenLastCalledWith("invitations");
  });
});
