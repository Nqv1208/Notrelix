import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  teamArchivedScenario,
  teamCandidatesFixture,
  teamDefaultScenario,
  teamEmptyScenario,
  teamErrorScenario,
  teamFixture,
  teamHighDensityScenario,
  teamLoadingScenario,
  teamMemberFixture,
  teamMembersErrorScenario,
  teamPendingScenario,
  teamReadOnlyScenario,
  teamsCopyFixture,
} from "../../../verification/workspace-teams-fixtures";
import {
  WorkspaceTeamSurface,
  type WorkspaceTeamSurfaceProps,
} from "../workspace-team-surface";

beforeAll(() => {
  // Radix Select measures and scrolls items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = teamsCopyFixture();

function renderTeam(
  scenario = teamDefaultScenario(),
  overrides: Partial<WorkspaceTeamSurfaceProps> = {},
) {
  const props: WorkspaceTeamSurfaceProps = {
    ...scenario,
    onRename: vi.fn(),
    onDescribe: vi.fn(),
    onArchive: vi.fn(),
    onUnarchive: vi.fn(),
    onAddMember: vi.fn(),
    onChangeRole: vi.fn(),
    onRemove: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<WorkspaceTeamSurface {...props} />);
  return { props, view };
}

function rowOf(name: string): HTMLElement {
  return screen.getByText(name).closest("li") as HTMLElement;
}

function choose(trigger: HTMLElement, option: RegExp | string) {
  fireEvent.keyDown(trigger, { key: "Enter" });
  fireEvent.click(screen.getByRole("option", { name: option }));
}

describe("WorkspaceTeamSurface", () => {
  it("FUI[workspace.team:render] shows the Team, its description and members with e-mail only when reported", () => {
    renderTeam();
    expect(
      screen.getByRole("heading", { level: 1, name: "Platform engineering" }),
    ).toBeTruthy();
    expect(
      screen.getByText("Owns the shared runtime, CI and developer tooling."),
    ).toBeTruthy();
    expect(screen.getByText("3 members")).toBeTruthy();

    const ada = rowOf("Ada Lovelace");
    expect(within(ada).getByText(copy.members.you)).toBeTruthy();
    expect(within(ada).queryByText(/@/)).toBeNull();
    const alan = rowOf("Alan Turing");
    expect(within(alan).getByText("alan.turing@example.com")).toBeTruthy();
    expect(
      within(alan).getByRole("combobox", { name: "Team role for Alan Turing" }),
    ).toBeTruthy();
  });

  it("FUI[workspace.team:rename] renames and describes with validation and a save per field", () => {
    const { props } = renderTeam();
    const name = screen.getByLabelText(copy.team.nameLabel);
    const rename = screen.getByRole("button", {
      name: copy.team.rename,
    }) as HTMLButtonElement;
    expect(rename.disabled).toBe(true);

    fireEvent.change(name, { target: { value: "   " } });
    fireEvent.click(rename);
    expect(screen.getByText(copy.team.nameRequired)).toBeTruthy();
    expect(props.onRename).not.toHaveBeenCalled();

    fireEvent.change(name, { target: { value: "x".repeat(161) } });
    fireEvent.click(rename);
    expect(screen.getByText(copy.team.nameTooLong)).toBeTruthy();
    expect(props.onRename).not.toHaveBeenCalled();

    fireEvent.change(name, { target: { value: "  Platform ops  " } });
    fireEvent.click(rename);
    expect(props.onRename).toHaveBeenCalledWith("Platform ops");

    const description = screen.getByLabelText(copy.team.descriptionLabel);
    const save = screen.getByRole("button", {
      name: copy.team.saveDescription,
    }) as HTMLButtonElement;
    expect(save.disabled).toBe(true);
    fireEvent.change(description, { target: { value: "d".repeat(1025) } });
    expect(screen.getByText(copy.team.descriptionTooLong)).toBeTruthy();
    expect(save.disabled).toBe(true);
    fireEvent.change(description, { target: { value: "  Shared runtime  " } });
    fireEvent.click(save);
    expect(props.onDescribe).toHaveBeenCalledWith("Shared runtime");

    fireEvent.change(description, { target: { value: "" } });
    fireEvent.click(save);
    expect(props.onDescribe).toHaveBeenLastCalledWith(null);
  });

  it("FUI[workspace.team:role] changes a Team role inline through the injected callback", () => {
    const { props } = renderTeam();
    choose(within(rowOf("Alan Turing")).getByRole("combobox"), copy.roles.Lead);
    expect(props.onChangeRole).toHaveBeenCalledWith("u-alan", "Lead");

    // Choosing the current role is not a change.
    (props.onChangeRole as ReturnType<typeof vi.fn>).mockClear();
    choose(
      within(rowOf("Alan Turing")).getByRole("combobox"),
      copy.roles.Member,
    );
    expect(props.onChangeRole).not.toHaveBeenCalled();
  });

  it("FUI[workspace.team:last-lead] never offers demoting or removing the last Lead and shows why", () => {
    const { view } = renderTeam();
    const ada = rowOf("Ada Lovelace");
    expect(within(ada).queryByRole("combobox")).toBeNull();
    expect(within(ada).queryByRole("button")).toBeNull();
    expect(within(ada).getByText(copy.blocked.lastLead)).toBeTruthy();
    expect(within(ada).getByText(copy.roles.Lead)).toBeTruthy();
    view.unmount();

    // With a second Lead, either may be changed again.
    const scenario = teamDefaultScenario();
    renderTeam({
      ...scenario,
      members: {
        status: "ready",
        data: [
          teamMemberFixture({
            userId: "u-ada",
            displayName: "Ada Lovelace",
            role: "Lead",
          }),
          teamMemberFixture({
            userId: "u-alan",
            displayName: "Alan Turing",
            role: "Lead",
          }),
        ],
      },
    });
    expect(
      within(rowOf("Ada Lovelace")).getByRole("combobox", {
        name: "Team role for Ada Lovelace",
      }),
    ).toBeTruthy();
    expect(screen.queryByText(copy.blocked.lastLead)).toBeNull();
  });

  it("FUI[workspace.team:add] filters candidates by name, defaults the role to Member and adds a guest or member", () => {
    const { props } = renderTeam();
    const person = screen.getByRole("combobox", {
      name: copy.members.add.userLabel,
    });
    const role = screen.getByRole("combobox", {
      name: copy.members.add.roleLabel,
    });
    expect(role.textContent).toContain(copy.roles.Member);

    // Present members and suspended members are never candidates.
    fireEvent.keyDown(person, { key: "Enter" });
    const names = screen
      .getAllByRole("option")
      .map((option) => option.textContent);
    expect(names).toEqual(["Nguyễn Thùy Linh", "Priya Raman"]);
    fireEvent.keyDown(document.activeElement ?? person, { key: "Escape" });

    // Add without choosing anyone.
    fireEvent.click(
      screen.getByRole("button", { name: copy.members.add.submit }),
    );
    expect(screen.getByText(copy.members.add.required)).toBeTruthy();
    expect(props.onAddMember).not.toHaveBeenCalled();

    fireEvent.change(screen.getByLabelText(copy.members.add.filterLabel), {
      target: { value: "thuy" },
    });
    fireEvent.keyDown(person, { key: "Enter" });
    expect(screen.getAllByRole("option")).toHaveLength(1);
    fireEvent.click(screen.getByRole("option", { name: "Nguyễn Thùy Linh" }));
    fireEvent.click(
      screen.getByRole("button", { name: copy.members.add.submit }),
    );
    expect(props.onAddMember).toHaveBeenCalledWith({
      userId: "u-linh",
      role: "Member",
    });
  });

  it("says when nobody is left to add or nobody matches", () => {
    renderTeam({
      ...teamDefaultScenario(),
      candidates: teamCandidatesFixture().filter((member) =>
        ["u-ada", "u-alan", "u-grace"].includes(member.userId),
      ),
    });
    expect(screen.getByText(copy.members.add.noCandidates)).toBeTruthy();
  });

  it("FUI[workspace.team:remove] confirms removal in an AlertDialog before calling the producer", () => {
    const { props } = renderTeam();
    fireEvent.click(
      screen.getByRole("button", { name: "Remove Alan Turing from the Team" }),
    );
    const dialog = screen.getByRole("alertdialog", {
      name: "Remove Alan Turing from Platform engineering?",
    });
    expect(props.onRemove).not.toHaveBeenCalled();
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.confirmRemove.cancel }),
    );
    expect(props.onRemove).not.toHaveBeenCalled();

    fireEvent.click(
      screen.getByRole("button", { name: "Remove Alan Turing from the Team" }),
    );
    fireEvent.click(
      within(screen.getByRole("alertdialog")).getByRole("button", {
        name: copy.confirmRemove.action,
      }),
    );
    expect(props.onRemove).toHaveBeenCalledWith("u-alan");
  });

  it("FUI[workspace.team:archive] confirms archiving and unarchiving", () => {
    const active = renderTeam();
    fireEvent.click(screen.getByRole("button", { name: copy.danger.archive }));
    const dialog = screen.getByRole("alertdialog", {
      name: "Archive Platform engineering?",
    });
    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.confirmArchive.cancel }),
    );
    expect(active.props.onArchive).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: copy.danger.archive }));
    fireEvent.click(
      within(screen.getByRole("alertdialog")).getByRole("button", {
        name: copy.confirmArchive.action,
      }),
    );
    expect(active.props.onArchive).toHaveBeenCalledTimes(1);
    expect(active.props.onUnarchive).not.toHaveBeenCalled();
    active.view.unmount();

    const archived = renderTeam(teamArchivedScenario());
    expect(
      screen.queryByRole("button", { name: copy.danger.archive }),
    ).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: copy.danger.unarchive }),
    );
    fireEvent.click(
      within(
        screen.getByRole("alertdialog", {
          name: "Unarchive Platform engineering?",
        }),
      ).getByRole("button", { name: copy.confirmArchive.unarchiveAction }),
    );
    expect(archived.props.onUnarchive).toHaveBeenCalledTimes(1);
    expect(archived.props.onArchive).not.toHaveBeenCalled();
  });

  it("FUI[workspace.team:archived-blocked] freezes membership on an archived Team and says why", () => {
    renderTeam(teamArchivedScenario());
    expect(
      screen.getByRole("heading", { level: 1, name: /Platform engineering/ }),
    ).toBeTruthy();
    expect(screen.getAllByText(copy.team.archivedBadge).length).toBeGreaterThan(
      0,
    );
    expect(screen.getByText(copy.archivedReason)).toBeTruthy();
    // The Domain rejects rename and describe on an archived Team.
    expect(screen.queryByLabelText(copy.team.nameLabel)).toBeNull();
    expect(screen.queryByLabelText(copy.team.descriptionLabel)).toBeNull();
    expect(screen.queryByRole("button", { name: copy.team.rename })).toBeNull();
    expect(
      screen.queryByRole("button", { name: copy.team.saveDescription }),
    ).toBeNull();
    expect(
      screen.getByText("Owns the shared runtime, CI and developer tooling."),
    ).toBeTruthy();
    expect(screen.queryByRole("combobox")).toBeNull();
    expect(screen.queryByRole("button", { name: /from the Team$/ })).toBeNull();
    expect(
      screen.queryByRole("button", { name: copy.members.add.submit }),
    ).toBeNull();
    expect(
      within(rowOf("Alan Turing")).getByText(copy.roles.Member),
    ).toBeTruthy();
  });

  it("FUI[workspace.team:read-only] is view-only for workspace members who are not admins", () => {
    renderTeam(teamReadOnlyScenario());
    expect(screen.getByText(copy.readOnly)).toBeTruthy();
    expect(screen.queryByLabelText(copy.team.nameLabel)).toBeNull();
    expect(screen.queryByLabelText(copy.team.descriptionLabel)).toBeNull();
    expect(screen.queryByRole("combobox")).toBeNull();
    expect(screen.queryByRole("button", { name: copy.team.rename })).toBeNull();
    expect(
      screen.queryByRole("button", { name: copy.danger.archive }),
    ).toBeNull();
    expect(screen.queryByRole("button", { name: /from the Team$/ })).toBeNull();
    expect(
      screen.getByText("Owns the shared runtime, CI and developer tooling."),
    ).toBeTruthy();
    expect(
      within(rowOf("Grace Hopper")).getByText(copy.roles.Member),
    ).toBeTruthy();
  });

  it("FUI[workspace.team:high-density] renders 200 members", () => {
    renderTeam(teamHighDensityScenario());
    expect(
      within(
        screen.getByRole("list", { name: copy.members.title }),
      ).getAllByRole("listitem"),
    ).toHaveLength(200);
    expect(screen.getByText("Thành viên số 200")).toBeTruthy();
  });

  it("marks members with a write in flight and disables their controls", () => {
    renderTeam(teamPendingScenario());
    const trigger = within(rowOf("Alan Turing")).getByRole("combobox", {
      name: "Updating Team role for Alan Turing…",
    });
    expect(
      trigger.hasAttribute("disabled") ||
        trigger.getAttribute("data-disabled") !== null,
    ).toBe(true);
  });

  it("shows loading, empty and error states and retries", () => {
    const loading = renderTeam(teamLoadingScenario());
    expect(
      screen.getByRole("status", { name: copy.team.loading }),
    ).toBeTruthy();
    loading.view.unmount();

    const failed = renderTeam(teamErrorScenario());
    expect(screen.getByRole("alert").textContent).toContain(copy.team.error);
    fireEvent.click(screen.getByRole("button", { name: copy.team.retry }));
    expect(failed.props.onRetry).toHaveBeenCalledTimes(1);
    failed.view.unmount();

    const membersFailed = renderTeam(teamMembersErrorScenario());
    expect(
      screen.getByRole("heading", { level: 1, name: "Platform engineering" }),
    ).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.members.retry }));
    expect(membersFailed.props.onRetry).toHaveBeenCalledTimes(1);
    membersFailed.view.unmount();

    renderTeam(teamEmptyScenario());
    expect(screen.getByText(copy.members.empty)).toBeTruthy();
    // The empty description is an empty field for an admin.
    expect(
      (screen.getByLabelText(copy.team.descriptionLabel) as HTMLTextAreaElement)
        .value,
    ).toBe("");
  });

  it("states that a read-only viewer sees no description when there is none", () => {
    renderTeam({
      ...teamReadOnlyScenario(),
      team: { status: "ready", data: teamFixture({ description: null }) },
    });
    expect(screen.getByText(copy.team.noDescription)).toBeTruthy();
  });
});
