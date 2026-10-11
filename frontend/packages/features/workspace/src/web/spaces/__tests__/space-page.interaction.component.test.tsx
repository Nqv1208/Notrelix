import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  spacePageArchivedScenario,
  spacePageDefaultScenario,
  spacePageEmptyScenario,
  spacePageMembersScenario,
  spacePageNotFoundScenario,
  spacePagePermissionLimitedScenario,
  spacePageReadOnlyScenario,
  spacePageSettingsScenario,
  spaceMembersFixture,
  spacesCopyFixture,
} from "../../../verification/workspace-spaces-fixtures";
import {
  WorkspaceSpaceSurface,
  type WorkspaceSpaceSurfaceProps,
} from "../workspace-space-surface";

beforeAll(() => {
  // Radix Select measures and scrolls items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const copy = spacesCopyFixture();

function renderPage(
  scenario = spacePageDefaultScenario(),
  overrides: Partial<WorkspaceSpaceSurfaceProps> = {},
) {
  const props: WorkspaceSpaceSurfaceProps = {
    ...scenario,
    onTabChange: vi.fn(),
    onCreateChild: vi.fn(),
    onCreateBoard: vi.fn(),
    onCreateDoc: vi.fn(),
    onMoveSpace: vi.fn(),
    onMoveBoard: vi.fn(),
    onUnarchive: vi.fn(),
    onSave: vi.fn(),
    onArchive: vi.fn(),
    onAddMember: vi.fn(),
    onChangeMemberRole: vi.fn(),
    onRemoveMember: vi.fn(),
    onRetry: vi.fn(),
    onRetryContent: vi.fn(),
    onRetryMembers: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<WorkspaceSpaceSurface {...props} />);
  return { props, view };
}

function memberRow(name: string): HTMLElement {
  return screen.getByText(name).closest("li") as HTMLElement;
}

describe("WorkspaceSpaceSurface", () => {
  it("FUI[workspace.space-page:render] shows the Space with its child Spaces, Boards and Docs", () => {
    renderPage();
    expect(screen.getByRole("heading", { level: 1, name: /^IT/ })).toBeTruthy();
    expect(screen.getAllByText(copy.tree.private).length).toBeGreaterThan(0);
    const spaces = screen.getByRole("region", { name: /^Spaces/ });
    expect(
      within(spaces)
        .getByRole("link", { name: "Platform" })
        .getAttribute("href"),
    ).toBe("/workspaces/ws-product/spaces/sp-it-platform");
    const boards = screen.getByRole("region", { name: /^Boards/ });
    expect(
      within(boards).getByRole("link", { name: "IT roadmap" }),
    ).toBeTruthy();
    expect(
      within(screen.getByRole("region", { name: /^Docs/ })).getByRole("link", {
        name: "Onboarding checklist",
      }),
    ).toBeTruthy();
  });

  it("FUI[workspace.space-page:actions] offers create, child Space and move to a Space manager", () => {
    const { props } = renderPage();
    fireEvent.click(screen.getByRole("button", { name: copy.page.newBoard }));
    expect(props.onCreateBoard).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: copy.page.newDoc }));
    expect(props.onCreateDoc).toHaveBeenCalledTimes(1);
    fireEvent.keyDown(
      screen.getByRole("button", { name: "More actions for IT" }),
      { key: "Enter" },
    );
    fireEvent.click(screen.getByRole("menuitem", { name: copy.page.move }));
    expect(props.onMoveSpace).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: "Move IT roadmap" }));
    expect(props.onMoveBoard).toHaveBeenCalledWith(
      expect.objectContaining({ id: "board-it-roadmap" }),
    );
  });

  it("FUI[workspace.space-page:permission] offers only what the access level allows", () => {
    const editor = renderPage(spacePagePermissionLimitedScenario());
    expect(
      screen.getByRole("button", { name: copy.page.newBoard }),
    ).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: /More actions for/ }),
    ).toBeNull();
    editor.view.unmount();

    renderPage(spacePageReadOnlyScenario());
    expect(
      screen.queryByRole("button", { name: copy.page.newBoard }),
    ).toBeNull();
    expect(screen.queryByRole("button", { name: /^Move / })).toBeNull();
    expect(screen.getByText(copy.access.Viewer, { exact: false })).toBeTruthy();
  });

  it("FUI[workspace.space-page:not-found] explains a Space the member cannot see", () => {
    renderPage(spacePageNotFoundScenario());
    expect(screen.getByText(copy.page.notFound)).toBeTruthy();
    expect(
      screen
        .getByRole("link", { name: copy.page.backHome })
        .getAttribute("href"),
    ).toBe("/workspaces/ws-product");
  });

  it("FUI[workspace.space-page:members-role] changes a member's Space role inline", () => {
    const { props } = renderPage(spacePageMembersScenario());
    const trigger = within(memberRow("Helpdesk")).getByRole("combobox", {
      name: "Role for Helpdesk",
    });
    fireEvent.keyDown(trigger, { key: "Enter" });
    fireEvent.click(screen.getByRole("option", { name: copy.roles.Viewer }));
    expect(props.onChangeMemberRole).toHaveBeenCalledWith(
      expect.objectContaining({
        subjectType: "Team",
        subjectId: "team-helpdesk",
      }),
      "Viewer",
    );
  });

  it("FUI[workspace.space-page:members-last-admin] protects the last Space admin with the reason", () => {
    renderPage(spacePageMembersScenario());
    const linh = memberRow("Linh Tran");
    expect(within(linh).queryByRole("combobox")).toBeNull();
    expect(within(linh).queryByRole("button")).toBeNull();
    expect(within(linh).getByText(copy.members.lastAdmin)).toBeTruthy();
  });

  it("FUI[workspace.space-page:members-remove] confirms removal in an AlertDialog", () => {
    const { props } = renderPage(spacePageMembersScenario());
    fireEvent.keyDown(
      within(memberRow("Minh Pham")).getByRole("button", {
        name: "Actions for Minh Pham",
      }),
      { key: "Enter" },
    );
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.members.remove }),
    );
    const dialog = screen.getByRole("alertdialog", {
      name: "Remove Minh Pham from IT?",
    });
    expect(props.onRemoveMember).not.toHaveBeenCalled();
    fireEvent.click(
      within(dialog).getByRole("button", {
        name: copy.members.confirm.removeAction,
      }),
    );
    expect(props.onRemoveMember).toHaveBeenCalledWith(
      expect.objectContaining({ subjectId: "user-minh" }),
    );
  });

  it("FUI[workspace.space-page:members-add] adds a team with a role, excluding existing entries", () => {
    const { props } = renderPage(spacePageMembersScenario());
    const form = screen.getByRole("form", { name: copy.members.add.title });
    fireEvent.click(
      within(form).getByRole("button", { name: copy.members.add.submit }),
    );
    expect(within(form).getByText(copy.members.add.required)).toBeTruthy();
    expect(props.onAddMember).not.toHaveBeenCalled();

    fireEvent.click(
      within(form).getByRole("radio", { name: copy.members.add.teamTab }),
    );
    fireEvent.keyDown(
      within(form).getByRole("combobox", { name: copy.members.add.teamLabel }),
      { key: "Enter" },
    );
    expect(screen.queryByRole("option", { name: /Helpdesk/ })).toBeNull();
    fireEvent.click(screen.getByRole("option", { name: /SRE/ }));
    fireEvent.click(
      within(form).getByRole("button", { name: copy.members.add.submit }),
    );
    expect(props.onAddMember).toHaveBeenCalledWith({
      subjectType: "Team",
      subjectId: "team-sre",
      role: "Member",
    });
  });

  it("FUI[workspace.space-page:settings-save] saves only the changed details", () => {
    const { props } = renderPage(spacePageSettingsScenario());
    const form = screen.getByRole("form", {
      name: copy.settings.profile.title,
    });
    const save = within(form).getByRole("button", {
      name: copy.settings.save,
    }) as HTMLButtonElement;
    expect(save.disabled).toBe(true);
    fireEvent.change(
      within(form).getByLabelText(copy.settings.profile.nameLabel),
      {
        target: { value: "  IT & Systems " },
      },
    );
    fireEvent.click(within(form).getByRole("radio", { name: /Open/ }));
    fireEvent.click(save);
    expect(props.onSave).toHaveBeenCalledWith({
      name: "IT & Systems",
      visibility: "Workspace",
    });
  });

  it("FUI[workspace.space-page:archive-blocked] explains why a Space with content cannot be archived", () => {
    renderPage(spacePageSettingsScenario());
    const archive = screen.getByRole("button", {
      name: copy.settings.danger.archive,
    }) as HTMLButtonElement;
    expect(archive.disabled).toBe(true);
    expect(
      screen.getByText(/still has 2 Spaces, 2 Boards and 1 Doc/),
    ).toBeTruthy();
  });

  it("FUI[workspace.space-page:archive] archives an empty Space after confirmation", () => {
    const { props } = renderPage({
      ...spacePageEmptyScenario(),
      tab: "settings",
    });
    fireEvent.click(
      screen.getByRole("button", { name: copy.settings.danger.archive }),
    );
    const dialog = screen.getByRole("alertdialog", {
      name: "Archive Security?",
    });
    fireEvent.click(
      within(dialog).getByRole("button", {
        name: copy.settings.danger.confirmAction,
      }),
    );
    expect(props.onArchive).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:archived] shows an archived Space read-only with Unarchive", () => {
    const { props } = renderPage(spacePageArchivedScenario());
    expect(screen.getByText(copy.page.archivedNotice)).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: copy.page.newBoard }),
    ).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: copy.page.unarchive }));
    expect(props.onUnarchive).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:refused] shows the producer's refusal when hidden content blocks archive", () => {
    renderPage({
      ...spacePageEmptyScenario(),
      tab: "settings",
      archiveRefused: true,
      members: { status: "ready", data: spaceMembersFixture() },
    });
    expect(screen.getByRole("alert").textContent).toBe(
      copy.settings.danger.blockedUnknown,
    );
  });
});
