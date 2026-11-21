import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  membersCopyFixture,
  membersInvitationsScenario,
} from "../../../verification/workspace-members-fixtures";
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
const entryOptions = {
  teams: [{ id: "team-erp", name: "Nhóm Kỹ sư ERP" }],
  spaces: [{ id: "space-cn", name: "Công nghệ" }],
};

function renderMembers(overrides: Partial<WorkspaceMembersSurfaceProps> = {}) {
  const props: WorkspaceMembersSurfaceProps = {
    ...membersInvitationsScenario(),
    entryOptions,
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
  if (props.invitations)
    fireEvent.click(
      screen.getByRole("button", { name: copy.invitations.invitePeople }),
    );
  return props;
}

const email = () => screen.getByLabelText(copy.invitations.emailLabel);
const send = () =>
  fireEvent.click(screen.getByRole("button", { name: copy.invitations.send }));
const openPicker = () =>
  fireEvent.keyDown(
    screen.getByRole("combobox", { name: copy.invitations.entries.addLabel }),
    { key: "Enter" },
  );
function pick(label: string) {
  openPicker();
  fireEvent.click(screen.getByRole("option", { name: new RegExp(label) }));
}
function chooseRole(role: "member" | "guest") {
  fireEvent.keyDown(
    screen.getByRole("combobox", { name: copy.invitations.roleLabel }),
    { key: "Enter" },
  );
  fireEvent.click(screen.getByRole("option", { name: copy.roles[role] }));
}

describe("The invite form keeps a Guest out of Team entries", () => {
  it("FUI[workspace.members:invite-guest-no-team-choice] offers Space choices but no Team choice while Guest is selected", () => {
    renderMembers();
    chooseRole("guest");

    openPicker();

    expect(screen.queryByRole("option", { name: /Nhóm Kỹ sư ERP/ })).toBeNull();
    expect(screen.getByRole("option", { name: /Công nghệ/ })).toBeTruthy();
  });

  it("FUI[workspace.members:invite-guest-clears-team] switching to Guest clears the Team entries, keeps the Space entries, and Member does not bring them back", () => {
    renderMembers();
    pick("Nhóm Kỹ sư ERP");
    pick("Công nghệ");
    expect(screen.getByText("Nhóm Kỹ sư ERP")).toBeTruthy();

    chooseRole("guest");

    expect(screen.queryByText("Nhóm Kỹ sư ERP")).toBeNull();
    expect(screen.getByText("Công nghệ")).toBeTruthy();

    chooseRole("member");

    expect(screen.queryByText("Nhóm Kỹ sư ERP")).toBeNull();
    openPicker();
    expect(screen.getByRole("option", { name: /Nhóm Kỹ sư ERP/ })).toBeTruthy();
  });

  it("FUI[workspace.members:invite-guest-payload] the invitation sent for a Guest never carries a Team entry", () => {
    const props = renderMembers();
    pick("Nhóm Kỹ sư ERP");
    pick("Công nghệ");
    chooseRole("guest");
    fireEvent.change(email(), { target: { value: "khach@dtvina.vn" } });

    send();

    expect(props.onInvite).toHaveBeenCalledWith({
      email: "khach@dtvina.vn",
      role: "guest",
      initialEntries: [{ kind: "space", targetId: "space-cn", role: "member" }],
    });
  });

  it("FUI[workspace.members:invite-guest-space-admin] a Space admin entry falls back to member for a Guest", () => {
    const props = renderMembers();
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
    chooseRole("guest");
    fireEvent.change(email(), { target: { value: "khach@dtvina.vn" } });

    send();

    expect(props.onInvite).toHaveBeenCalledWith({
      email: "khach@dtvina.vn",
      role: "guest",
      initialEntries: [{ kind: "space", targetId: "space-cn", role: "member" }],
    });
  });

  it("FUI[workspace.members:invite-guest-refusal] a refusal that comes back for an entry still shows under it", () => {
    renderMembers({
      entryRefusals: new Map([["team:team-erp", "not-active" as const]]),
    });
    pick("Nhóm Kỹ sư ERP");

    expect(screen.getByRole("alert").textContent).toBe(
      "Nhóm Kỹ sư ERP isn't active any more, so it can't take this person. Remove it or choose another.",
    );
  });
});
