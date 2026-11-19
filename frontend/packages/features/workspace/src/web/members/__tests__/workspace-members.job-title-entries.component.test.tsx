import { beforeAll, describe, expect, it, vi } from "vitest";
import {
  fireEvent,
  renderPureUi,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";

import {
  membersCopyFixture,
  membersDefaultScenario,
  membersInvitationsScenario,
} from "../../../verification/workspace-members-fixtures";
import type { JobTitleOutcome } from "../edit-job-title-dialog";
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
  renderPureUi(<WorkspaceMembersSurface {...props} />);
  return props;
}

const rowOf = (name: string) =>
  screen.getByText(name).closest("tr") as HTMLElement;
const openMenu = (name: string) =>
  fireEvent.keyDown(
    within(rowOf(name)).getByRole("button", { name: `Actions for ${name}` }),
    { key: "Enter" },
  );
const openTitleDialog = (name: string) => {
  openMenu(name);
  fireEvent.click(
    screen.getByRole("menuitem", { name: copy.actions.editJobTitle }),
  );
  return screen.getByRole("dialog");
};
const save = () => screen.getByRole("button", { name: copy.jobTitle.save });

describe("Edit another member's job title (F08)", () => {
  it("FUI[workspace.members:job-title-save] saves a new title for the person and closes", async () => {
    const onSaveJobTitle = vi.fn(async (): Promise<JobTitleOutcome> => ({
      kind: "saved",
    }));
    renderMembers(membersDefaultScenario(), { onSaveJobTitle });
    const dialog = openTitleDialog("Grace Hopper");
    expect((save() as HTMLButtonElement).disabled).toBe(true);
    fireEvent.change(within(dialog).getByLabelText(copy.jobTitle.label), {
      target: { value: "  Engineering lead " },
    });
    fireEvent.click(save());
    await waitFor(() =>
      expect(onSaveJobTitle).toHaveBeenCalledWith(
        expect.objectContaining({ userId: "u-grace" }),
        "Engineering lead",
      ),
    );
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
  });

  it("FUI[workspace.members:job-title-clear] a blank title clears it", async () => {
    const onSaveJobTitle = vi.fn(async (): Promise<JobTitleOutcome> => ({
      kind: "saved",
    }));
    const scenario = membersDefaultScenario();
    if (scenario.members.status !== "ready") throw new Error("fixture");
    renderMembers(
      {
        ...scenario,
        members: {
          status: "ready",
          data: scenario.members.data.map((member) =>
            member.userId === "u-grace"
              ? { ...member, jobTitle: "Analyst" }
              : member,
          ),
        },
      },
      { onSaveJobTitle },
    );
    const dialog = openTitleDialog("Grace Hopper");
    expect(
      (within(dialog).getByLabelText(copy.jobTitle.label) as HTMLInputElement)
        .value,
    ).toBe("Analyst");
    fireEvent.change(within(dialog).getByLabelText(copy.jobTitle.label), {
      target: { value: "   " },
    });
    fireEvent.click(save());
    await waitFor(() =>
      expect(onSaveJobTitle).toHaveBeenCalledWith(
        expect.objectContaining({ userId: "u-grace" }),
        null,
      ),
    );
  });

  it("FUI[workspace.members:job-title-validation] a title past the limit shows the field error and sends nothing", () => {
    const onSaveJobTitle = vi.fn();
    renderMembers(membersDefaultScenario(), { onSaveJobTitle });
    const dialog = openTitleDialog("Grace Hopper");
    fireEvent.change(within(dialog).getByLabelText(copy.jobTitle.label), {
      target: { value: "x".repeat(101) },
    });
    expect(screen.getByText(/at most 100 characters/)).toBeTruthy();
    expect((save() as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(save());
    expect(onSaveJobTitle).not.toHaveBeenCalled();
  });

  it("FUI[workspace.members:job-title-pending] shows Save as running and cannot send twice", async () => {
    let finish: (value: JobTitleOutcome) => void = () => {};
    const onSaveJobTitle = vi.fn(
      () => new Promise<JobTitleOutcome>((resolve) => (finish = resolve)),
    );
    renderMembers(membersDefaultScenario(), { onSaveJobTitle });
    const dialog = openTitleDialog("Grace Hopper");
    fireEvent.change(within(dialog).getByLabelText(copy.jobTitle.label), {
      target: { value: "Lead" },
    });
    fireEvent.click(save());
    const running = await screen.findByRole("button", {
      name: copy.jobTitle.saving,
    });
    expect((running as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(running);
    expect(onSaveJobTitle).toHaveBeenCalledTimes(1);
    finish({ kind: "saved" });
  });

  it("FUI[workspace.members:job-title-conflict] a stale version says the profile changed and Reload reads the members again", async () => {
    const onSaveJobTitle = vi.fn(async (): Promise<JobTitleOutcome> => ({
      kind: "conflict",
    }));
    const props = renderMembers(membersDefaultScenario(), { onSaveJobTitle });
    const dialog = openTitleDialog("Grace Hopper");
    fireEvent.change(within(dialog).getByLabelText(copy.jobTitle.label), {
      target: { value: "Lead" },
    });
    fireEvent.click(save());
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain(copy.jobTitle.conflict);
    fireEvent.click(
      within(alert).getByRole("button", { name: copy.jobTitle.reload }),
    );
    expect(props.onRetryMembers).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.members:job-title-denied] a refusal says only owners and admins can, and a plain failure says nothing changed", async () => {
    const answers: JobTitleOutcome[] = [{ kind: "denied" }, { kind: "failed" }];
    const onSaveJobTitle = vi.fn(async () => answers.shift()!);
    renderMembers(membersDefaultScenario(), { onSaveJobTitle });
    const dialog = openTitleDialog("Grace Hopper");
    const field = within(dialog).getByLabelText(copy.jobTitle.label);
    fireEvent.change(field, { target: { value: "Lead" } });
    fireEvent.click(save());
    expect((await screen.findByRole("alert")).textContent).toBe(
      copy.jobTitle.denied,
    );
    fireEvent.change(field, { target: { value: "Lead 2" } });
    fireEvent.click(save());
    await waitFor(() =>
      expect(screen.getByRole("alert").textContent).toBe(copy.jobTitle.failed),
    );
  });

  it("FUI[workspace.members:job-title-who] an Admin may edit the owner's title, nobody edits their own here, and a Member or an absent action gets no menu item", () => {
    const onSaveJobTitle = vi.fn();
    const scenario = membersDefaultScenario();
    // The Admin edits the Owner's title: a profile fact, not a role change.
    renderMembers(
      { ...scenario, viewer: { userId: "u-alan", role: "admin" } },
      { onSaveJobTitle },
    );
    openMenu("Ada Lovelace");
    expect(
      screen.getByRole("menuitem", { name: copy.actions.editJobTitle }),
    ).toBeTruthy();
  });

  it("FUI[workspace.members:job-title-self] the viewer's own row offers no title edit", () => {
    renderMembers(membersDefaultScenario(), { onSaveJobTitle: vi.fn() });
    expect(
      within(rowOf("Ada Lovelace")).queryByRole("button", {
        name: "Actions for Ada Lovelace",
      }),
    ).toBeNull();
  });

  it("FUI[workspace.members:job-title-hidden] no action without the host callback", () => {
    renderMembers(membersDefaultScenario());
    openMenu("Grace Hopper");
    expect(
      screen.queryByRole("menuitem", { name: copy.actions.editJobTitle }),
    ).toBeNull();
  });
});

const entryOptions = {
  teams: [{ id: "team-erp", name: "Nhóm Kỹ sư ERP" }],
  spaces: [{ id: "space-cn", name: "Công nghệ" }],
};
const email = () => screen.getByLabelText(copy.invitations.emailLabel);
const send = () =>
  fireEvent.click(screen.getByRole("button", { name: copy.invitations.send }));
function pick(label: string) {
  fireEvent.keyDown(
    screen.getByRole("combobox", { name: copy.invitations.entries.addLabel }),
    { key: "Enter" },
  );
  fireEvent.click(screen.getByRole("option", { name: new RegExp(label) }));
}

describe("Invite with initial Team and Space entries (G02)", () => {
  it("FUI[workspace.members:invite-entries] sends the chosen Team and Space entries with the invitation", () => {
    const props = renderMembers(membersInvitationsScenario(), { entryOptions });
    pick("Nhóm Kỹ sư ERP");
    pick("Công nghệ");
    expect(screen.getByText("Nhóm Kỹ sư ERP")).toBeTruthy();
    fireEvent.change(email(), { target: { value: "an.do@minhan.vn" } });
    send();
    expect(props.onInvite).toHaveBeenCalledWith({
      email: "an.do@minhan.vn",
      role: "member",
      initialEntries: [
        { kind: "team", targetId: "team-erp", role: "member" },
        { kind: "space", targetId: "space-cn", role: "member" },
      ],
    });
  });

  it("FUI[workspace.members:invite-entries-role] a Team lead entry is sent as lead, and removing an entry drops it", () => {
    const props = renderMembers(membersInvitationsScenario(), { entryOptions });
    pick("Nhóm Kỹ sư ERP");
    pick("Công nghệ");
    fireEvent.keyDown(
      screen.getByRole("combobox", {
        name: "Role in Nhóm Kỹ sư ERP",
      }),
      { key: "Enter" },
    );
    fireEvent.click(
      screen.getByRole("option", {
        name: copy.invitations.entries.teamRoles.lead,
      }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Remove Công nghệ" }));
    fireEvent.change(email(), { target: { value: "an.do@minhan.vn" } });
    send();
    expect(props.onInvite).toHaveBeenCalledWith({
      email: "an.do@minhan.vn",
      role: "member",
      initialEntries: [{ kind: "team", targetId: "team-erp", role: "lead" }],
    });
  });

  it("FUI[workspace.members:invite-entries-guest] a Guest invitation is never sent with a Team entry or a Space admin entry", () => {
    const props = renderMembers(membersInvitationsScenario(), { entryOptions });
    pick("Nhóm Kỹ sư ERP");
    pick("Công nghệ");
    fireEvent.keyDown(
      screen.getByRole("combobox", { name: "Role in Công nghệ" }),
      { key: "Enter" },
    );
    fireEvent.click(
      screen.getByRole("option", {
        name: copy.invitations.entries.spaceRoles.admin,
      }),
    );
    // Switch the invitation to Guest: the Team entry is dropped and the admin entry falls back to Member.
    fireEvent.keyDown(
      screen.getByRole("combobox", { name: copy.invitations.roleLabel }),
      {
        key: "Enter",
      },
    );
    fireEvent.click(screen.getByRole("option", { name: copy.roles.guest }));
    expect(screen.getByText(copy.invitations.entries.guestNote)).toBeTruthy();
    fireEvent.change(email(), { target: { value: "khach@dtvina.vn" } });
    send();
    expect(props.onInvite).toHaveBeenCalledWith({
      email: "khach@dtvina.vn",
      role: "guest",
      initialEntries: [{ kind: "space", targetId: "space-cn", role: "member" }],
    });
  });

  it("FUI[workspace.members:invite-entries-denied] a kind the inviter cannot assign is not offered and says why", () => {
    renderMembers(membersInvitationsScenario(), {
      entryOptions: { teams: null, spaces: entryOptions.spaces },
    });
    expect(screen.getByText(copy.invitations.entries.teamsDenied)).toBeTruthy();
    fireEvent.keyDown(
      screen.getByRole("combobox", { name: copy.invitations.entries.addLabel }),
      { key: "Enter" },
    );
    expect(screen.queryByRole("option", { name: /Nhóm Kỹ sư ERP/ })).toBeNull();
    expect(screen.getByRole("option", { name: /Công nghệ/ })).toBeTruthy();
  });

  it("FUI[workspace.members:invite-entries-none] with no options there is no picker, and a plain invitation carries no entries", () => {
    const props = renderMembers(membersInvitationsScenario());
    expect(
      screen.queryByRole("combobox", {
        name: copy.invitations.entries.addLabel,
      }),
    ).toBeNull();
    fireEvent.change(email(), { target: { value: "an.do@minhan.vn" } });
    send();
    expect(props.onInvite).toHaveBeenCalledWith({
      email: "an.do@minhan.vn",
      role: "member",
    });
  });

  it("FUI[workspace.members:invite-entries-refused] shows the producer's refusal of an entry next to the form", () => {
    renderMembers(
      {
        ...membersInvitationsScenario(),
        inviteError: copy.invitations.entries.refused,
      },
      { entryOptions },
    );
    expect(screen.getByText(copy.invitations.entries.refused)).toBeTruthy();
  });
  it("FUI[workspace.members:invite-entry-refused] names each refused entry under it and leaves the others alone", () => {
    renderMembers(membersInvitationsScenario(), {
      entryOptions,
      entryRefusals: new Map([["team:team-erp", "not-active" as const]]),
    });
    pick("Nhóm Kỹ sư ERP");
    pick("Công nghệ");

    const refused = screen.getByRole("alert");
    expect(refused.textContent).toBe(
      "Nhóm Kỹ sư ERP isn't active any more, so it can't take this person. Remove it or choose another.",
    );
    expect(refused.closest("li")?.textContent).toContain("Nhóm Kỹ sư ERP");
    expect(
      within(
        screen.getByText("Công nghệ").closest("li") as HTMLElement,
      ).queryByRole("alert"),
      "the entry that was not refused",
    ).toBeNull();
  });

  it("FUI[workspace.members:invite-entry-refused-guest] says a Guest cannot hold the role of a refused entry", () => {
    renderMembers(membersInvitationsScenario(), {
      entryOptions,
      entryRefusals: new Map([
        ["space:space-cn", "guest-too-privileged" as const],
      ]),
    });
    pick("Công nghệ");

    expect(screen.getByRole("alert").textContent).toBe(
      "A Guest can't have this role in Công nghệ. Choose a lower role or remove it.",
    );
  });
});
