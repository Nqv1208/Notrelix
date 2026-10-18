import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import type { WorkspaceDestination } from "../../../core/model/navigation";
import {
  spacePageArchivedScenario,
  spacePageContentErrorScenario,
  spacePageEditorScenario,
  spacePageMembersEmptyScenario,
  spacePageMembersErrorScenario,
  spacePageMembersInviteScenario,
  spacePageMembersLastAdminScenario,
  spacePageMembersLoadingScenario,
  spacePageMembersReadOnlyScenario,
  spacePageMembersRemoveScenario,
  spacePageMembersScenario,
  spacePageNewScenario,
  spacePageNotFoundScenario,
  spacePageOverviewScenario,
  spacePageProducedOnlyScenario,
  spacePageReadOnlyScenario,
  spacePageSettingsBlockedScenario,
  spacePageSettingsEmptyScenario,
  spacePageSettingsScenario,
  spacePageViewAsScenario,
  type SpacePageScenario,
} from "../../../verification/workspace-space-page-fixtures";
import { spacesCopyFixture } from "../../../verification/workspace-spaces-fixtures";
import { shellNavigationFixture } from "../../../verification/workspace-shell-fixtures";
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

const copy = spacesCopyFixture("vi");

function renderPage(
  scenario: SpacePageScenario = spacePageOverviewScenario(),
  overrides: Partial<WorkspaceSpaceSurfaceProps> = {},
) {
  const navigated: WorkspaceDestination[] = [];
  const props: WorkspaceSpaceSurfaceProps = {
    ...scenario,
    navigation: shellNavigationFixture((destination) =>
      navigated.push(destination),
    ),
    onCreateChild: vi.fn(),
    onCreateBoard: vi.fn(),
    onCreateDoc: vi.fn(),
    onMoveSpace: vi.fn(),
    onUnarchive: vi.fn(),
    onSave: vi.fn(),
    onArchive: vi.fn(),
    onDelete: null,
    onAddMembers: vi.fn(),
    onExpandTeam: vi.fn(),
    onChangeMemberRole: vi.fn(),
    onRemoveMember: vi.fn(),
    onRetry: vi.fn(),
    onRetryContent: vi.fn(),
    onRetryMembers: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<WorkspaceSpaceSurface {...props} />);
  return { props, view, navigated };
}

function renderMembers(overrides: Partial<WorkspaceSpaceSurfaceProps> = {}) {
  return renderPage(spacePageMembersScenario(), overrides);
}

function openCreateMenu() {
  fireEvent.click(screen.getByRole("button", { name: /^Tạo mới/ }));
  return screen.getByRole("menu");
}

describe("WorkspaceSpaceSurface", () => {
  it("FUI[workspace.space-page:render] shows the department with its Boards, sub-departments, Docs and who can open it", () => {
    renderPage();
    expect(
      screen.getByRole("heading", { level: 1, name: "Phòng Công nghệ" }),
    ).toBeTruthy();
    expect(screen.getAllByText(copy.tree.private).length).toBeGreaterThan(0);
    const tabs = screen.getByRole("navigation", { name: "Phòng Công nghệ" });
    expect(
      within(tabs)
        .getByRole("link", { name: /Tổng quan/ })
        .getAttribute("aria-current"),
    ).toBe("page");
    expect(within(tabs).getByText("24")).toBeTruthy();
    const boards = screen.getByRole("region", { name: copy.overview.boards });
    const erp = within(boards).getByRole("link", { name: /Triển khai ERP Q4/ });
    expect(erp.getAttribute("href")).toBe(
      "/workspaces/ws-product/boards/b-erp",
    );
    expect(within(boards).getByText("1 quá hạn")).toBeTruthy();
    expect(
      within(boards).getByText(copy.overview.listHead.progress),
    ).toBeTruthy();
    const subs = screen.getByRole("region", { name: copy.overview.children });
    expect(
      within(subs)
        .getByRole("link", { name: /Nhóm Hạ tầng/ })
        .getAttribute("href"),
    ).toBe("/workspaces/ws-product/spaces/sp-ha-tang");
    expect(
      within(
        screen.getByRole("region", { name: copy.overview.docs }),
      ).getByRole("link", { name: /Kế hoạch chuyển đổi dữ liệu/ }),
    ).toBeTruthy();
    const aside = screen.getByRole("complementary", { name: copy.aside.label });
    expect(
      within(aside).getByText("Lê Hoàng Phúc, Nguyễn Thu Hà"),
    ).toBeTruthy();
  });

  it("FUI[workspace.space-page:actions] opens the Create menu with a keyboard and creates a Board, Doc or sub-department", () => {
    const { props } = renderPage();
    const trigger = screen.getByRole("button", { name: /^Tạo mới/ });
    expect(trigger.getAttribute("aria-expanded")).toBe("false");
    const menu = openCreateMenu();
    expect(trigger.getAttribute("aria-expanded")).toBe("true");
    const items = within(menu).getAllByRole("menuitem");
    expect(items.map((item) => item.textContent)).toEqual([
      expect.stringContaining("Board"),
      expect.stringContaining("Tài liệu"),
      expect.stringContaining("Phòng ban con"),
      expect.stringContaining("Từ mẫu"),
    ]);
    // Focus lands on the first item; arrows wrap; Escape returns to the button.
    expect(document.activeElement).toBe(items[0]);
    fireEvent.keyDown(menu, { key: "ArrowUp" });
    expect(document.activeElement).toBe(items[3]);
    fireEvent.keyDown(menu, { key: "ArrowDown" });
    expect(document.activeElement).toBe(items[0]);
    fireEvent.keyDown(menu, { key: "Escape" });
    expect(screen.queryByRole("menu")).toBeNull();
    expect(document.activeElement).toBe(trigger);

    fireEvent.click(
      within(openCreateMenu()).getByRole("menuitem", { name: /^Board/ }),
    );
    expect(props.onCreateBoard).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("menu")).toBeNull();
    fireEvent.keyDown(openCreateMenu(), { key: "d" });
    expect(props.onCreateDoc).toHaveBeenCalledTimes(1);
    fireEvent.click(
      within(openCreateMenu()).getByRole("menuitem", {
        name: /^Phòng ban con/,
      }),
    );
    expect(props.onCreateChild).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:template-inert] draws the template item inert with the reason", () => {
    const { props } = renderPage();
    const template = within(openCreateMenu()).getByRole("menuitem", {
      name: /^Từ mẫu/,
    });
    expect(template.getAttribute("aria-disabled")).toBe("true");
    expect(template.getAttribute("title")).toBe(
      copy.createMenu.templateUnavailable,
    );
    fireEvent.click(template);
    expect(props.onCreateBoard).not.toHaveBeenCalled();
    expect(screen.getByRole("menu")).toBeTruthy();
  });

  it("FUI[workspace.space-page:navigation] goes to the members and settings tabs from the head", () => {
    const { navigated } = renderPage();
    const add = screen.getByRole("link", { name: copy.page.addMember });
    expect(add.getAttribute("href")).toBe(
      "/workspaces/ws-product/spaces/sp-cong-nghe?tab=members",
    );
    fireEvent.click(
      screen.getByRole("button", { name: copy.page.settingsButton }),
    );
    expect(navigated.at(-1)).toEqual({
      section: "space",
      spaceId: "sp-cong-nghe",
      tab: "settings",
    });
    const tabs = screen.getByRole("navigation", { name: "Phòng Công nghệ" });
    const content = within(tabs).getByText(copy.page.tabs.content);
    expect(content.getAttribute("aria-disabled")).toBe("true");
  });

  it("FUI[workspace.space-page:permission] offers only what the access level allows", () => {
    // An editor creates Boards and Docs but does not manage the department.
    const editor = renderPage(spacePageEditorScenario());
    expect(
      screen.queryByRole("link", { name: copy.page.addMember }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: copy.page.settingsButton }),
    ).toBeNull();
    expect(
      screen.queryByRole("link", { name: copy.page.tabs.settings }),
    ).toBeNull();
    const menu = openCreateMenu();
    expect(
      within(menu)
        .getByRole("menuitem", { name: /^Board/ })
        .getAttribute("aria-disabled"),
    ).toBeNull();
    const child = within(menu).getByRole("menuitem", {
      name: /^Phòng ban con/,
    });
    expect(child.getAttribute("aria-disabled")).toBe("true");
    expect(child.getAttribute("title")).toBe(copy.createMenu.childUnavailable);
    fireEvent.click(child);
    expect(editor.props.onCreateChild).not.toHaveBeenCalled();
    editor.view.unmount();

    renderPage(spacePageReadOnlyScenario());
    expect(screen.queryByRole("button", { name: /^Tạo mới/ })).toBeNull();
    expect(
      screen.queryByRole("link", { name: copy.page.addMember }),
    ).toBeNull();
  });

  it("FUI[workspace.space-page:view-as] says what a member sees and leaves with Exit", () => {
    const scenario = spacePageViewAsScenario();
    const onExit = vi.fn();
    renderPage(scenario, { viewAs: { onExit } });
    expect(screen.getByRole("status").textContent).toContain(
      copy.page.viewAs.lead,
    );
    expect(
      screen.queryByRole("link", { name: copy.page.addMember }),
    ).toBeNull();
    expect(screen.queryByRole("button", { name: /^Tạo mới/ })).toBeNull();
    fireEvent.click(
      screen.getByRole("button", { name: copy.page.viewAs.exit }),
    );
    expect(onExit).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:first-steps] shows the three steps of a new department", () => {
    const { props, navigated } = renderPage(spacePageNewScenario());
    expect(screen.getByText("Bắt đầu với Phòng Pháp chế")).toBeTruthy();
    fireEvent.click(
      screen.getAllByRole("button", { name: copy.setup.membersAction })[0]!,
    );
    expect(navigated.at(-1)).toEqual({
      section: "space",
      spaceId: "sp-phap-che",
      tab: "members",
    });
    fireEvent.click(
      screen.getByRole("button", { name: copy.setup.boardAction }),
    );
    expect(props.onCreateBoard).toHaveBeenCalledTimes(1);
    const template = screen
      .getAllByText(copy.setup.useTemplate)[0]!
      .closest("a")!;
    expect(template.getAttribute("aria-disabled")).toBe("true");
  });

  it("FUI[workspace.space-page:produced-only] closes the grid over the columns no producer fills", () => {
    renderPage(spacePageProducedOnlyScenario());
    const boards = screen.getByRole("region", { name: copy.overview.boards });
    expect(
      within(boards).queryByText(copy.overview.listHead.progress),
    ).toBeNull();
    expect(
      within(boards).queryByText(copy.overview.listHead.people),
    ).toBeNull();
    expect(
      within(boards).getByText(copy.overview.listHead.updated),
    ).toBeTruthy();
    expect(screen.queryByText(copy.aside.attention)).toBeNull();
    expect(screen.queryByText(copy.aside.workload)).toBeNull();
  });

  it("FUI[workspace.space-page:content-error] offers a retry when the Boards and Docs cannot load", () => {
    const { props } = renderPage(spacePageContentErrorScenario());
    expect(screen.getByRole("alert").textContent).toContain(
      copy.overview.error,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.page.retry }));
    expect(props.onRetryContent).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:not-found] explains a department the member cannot see", () => {
    renderPage(spacePageNotFoundScenario());
    expect(screen.getByText(copy.page.notFound)).toBeTruthy();
    expect(
      screen
        .getByRole("link", { name: copy.page.backHome })
        .getAttribute("href"),
    ).toBe("/workspaces/ws-product");
  });

  it("FUI[workspace.space-page:members-render] lists teams with their people, then people with role, source and activity", () => {
    renderMembers();
    const list = screen.getByRole("table", {
      name: "Thành viên Phòng Công nghệ",
    });
    const rows = within(list).getAllByRole("row");
    expect(rows[1]?.textContent).toContain("Nhóm Kỹ sư ERP");
    expect(rows[1]?.textContent).toContain("Nhóm workspace · 4 người");
    expect(rows[1]?.textContent).toContain("Thêm trực tiếp cả nhóm");
    expect(rows[1]?.textContent).toContain("từ 01/09");
    // Both teams are open: their people show, the note shows once.
    expect(screen.getByText("Phan Ngọc Anh")).toBeTruthy();
    expect(screen.getAllByText(/Tổng giám đốc/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Đổi người trong nhóm ở/)).toHaveLength(1);
    const ha = screen.getByText("Nguyễn Thu Hà").closest('[role="row"]')!;
    expect(ha.textContent).toContain("Bạn");
    expect(ha.textContent).toContain("Quản trị workspace");
    expect(ha.textContent).toContain("Trực tiếp · người tạo phòng");
    // The summary counts entries by role and the waiting invitation.
    expect(
      screen.getByText(
        /2 quản trị · 15 thành viên · 3 người xem · 1 lời mời chờ/,
      ),
    ).toBeTruthy();
    // The invitation row has no producer behind its buttons.
    expect(
      screen
        .getByRole("button", { name: copy.members.inviteResend })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    // Seven rows, then "Show 13 more".
    fireEvent.click(screen.getByRole("button", { name: "Hiện 13 người khác" }));
    expect(screen.getByText("Uông Minh Châu")).toBeTruthy();
  });

  it("FUI[workspace.space-page:members-role] changes a role from the menu, with the keyboard, and gives focus back", () => {
    const { props } = renderMembers();
    const row = screen
      .getByText("Trần Minh Quân", { selector: "b" })
      .closest('[role="row"]') as HTMLElement;
    const trigger = within(row).getByRole("button", { name: /Thành viên/ });
    fireEvent.click(trigger);
    const menu = screen.getByRole("menu", {
      name: "Đổi vai trò của Trần Minh Quân",
    });
    const roles = within(menu).getAllByRole("menuitemradio");
    expect(roles.map((role) => role.getAttribute("aria-checked"))).toEqual([
      "false",
      "true",
      "false",
    ]);
    expect(document.activeElement).toBe(roles[1]);
    fireEvent.keyDown(menu, { key: "ArrowDown" });
    expect(document.activeElement).toBe(roles[2]);
    fireEvent.keyDown(roles[2]!, { key: "Enter" });
    expect(props.onChangeMemberRole).toHaveBeenCalledWith(
      expect.objectContaining({ subjectType: "User", subjectId: "u-quan" }),
      "Viewer",
    );
    expect(screen.queryByRole("menu")).toBeNull();
    expect(document.activeElement).toBe(trigger);
    // Choosing the role the person has does nothing; Escape closes.
    fireEvent.click(trigger);
    fireEvent.keyDown(screen.getByRole("menu"), { key: "Escape" });
    expect(screen.queryByRole("menu")).toBeNull();
    expect(props.onChangeMemberRole).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:members-last-admin] protects the only admin with the reason", () => {
    const { props } = renderPage(spacePageMembersLastAdminScenario());
    const menu = screen.getByRole("menu", {
      name: /Đổi vai trò của Lê Hoàng Phúc/,
    });
    const roles = within(menu).getAllByRole("menuitemradio");
    expect(roles[1]?.getAttribute("aria-disabled")).toBe("true");
    expect(
      within(menu).getAllByText("Không hạ được: Phúc là quản trị duy nhất"),
    ).toHaveLength(2);
    const remove = within(menu).getByRole("menuitem", {
      name: /Gỡ khỏi phòng/,
    });
    expect(remove.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(roles[1]!);
    fireEvent.click(remove);
    expect(props.onChangeMemberRole).not.toHaveBeenCalled();
    expect(screen.queryByRole("dialog")).toBeNull();
    expect(
      within(menu).getByText(/Phòng ban phải còn ít nhất 1 quản trị/),
    ).toBeTruthy();
  });

  it("FUI[workspace.space-page:members-remove] shows who loses access when a team goes, and confirms", () => {
    const { props } = renderPage(spacePageMembersRemoveScenario());
    const dialog = screen.getByRole("dialog", {
      name: "Gỡ Nhóm Kỹ sư ERP khỏi phòng?",
    });
    // Quân is also added directly, so he stays; the other three lose access.
    expect(
      within(dialog).getByText(
        /3 người sẽ mất quyền vào phòng; Trần Minh Quân vẫn còn/,
      ),
    ).toBeTruthy();
    expect(within(dialog).getAllByText("· mất quyền vào phòng")).toHaveLength(
      3,
    );
    expect(
      within(dialog).getByText("· vẫn là Thành viên, thêm trực tiếp"),
    ).toBeTruthy();
    expect(props.onRemoveMember).not.toHaveBeenCalled();
    fireEvent.click(within(dialog).getByRole("button", { name: "Gỡ nhóm" }));
    expect(props.onRemoveMember).toHaveBeenCalledWith(
      expect.objectContaining({ subjectType: "Team", subjectId: "t-erp" }),
    );
  });

  it("FUI[workspace.space-page:members-remove-person] asks before removing a person from the menu", () => {
    const { props } = renderMembers();
    const row = screen
      .getByText("Vũ Thanh", { selector: "b" })
      .closest('[role="row"]') as HTMLElement;
    fireEvent.click(
      within(row).getByRole("button", { name: "Thao tác với Vũ Thanh" }),
    );
    fireEvent.click(screen.getByRole("menuitem", { name: /Gỡ khỏi phòng/ }));
    const dialog = screen.getByRole("dialog", {
      name: "Gỡ Vũ Thanh khỏi Phòng Công nghệ?",
    });
    expect(props.onRemoveMember).not.toHaveBeenCalled();
    fireEvent.click(within(dialog).getByRole("button", { name: "Gỡ" }));
    expect(props.onRemoveMember).toHaveBeenCalledWith(
      expect.objectContaining({ subjectId: "u-thanh" }),
    );
  });

  it("FUI[workspace.space-page:members-add] adds people and teams with a role, skipping an email that is not a member", () => {
    const { props } = renderMembers();
    fireEvent.click(screen.getByRole("button", { name: copy.page.addMember }));
    const dialog = screen.getByRole("dialog", {
      name: "Thêm vào Phòng Công nghệ",
    });
    const submit = () => within(dialog).getByRole("button", { name: /^Thêm/ });
    expect(submit().getAttribute("aria-disabled")).toBe("true");
    const input = within(dialog).getByRole("combobox", {
      name: copy.members.add.inputLabel,
    });
    fireEvent.change(input, { target: { value: "hung" } });
    fireEvent.keyDown(input, { key: "Enter" });
    fireEvent.change(input, { target: { value: "QA" } });
    fireEvent.keyDown(input, { key: "Enter" });
    // A team that is already in the department is not offered.
    fireEvent.change(input, { target: { value: "ERP" } });
    expect(within(dialog).queryByRole("option")).toBeNull();
    fireEvent.change(input, { target: { value: "tuan@doitac-erp.vn" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(within(dialog).getByRole("alert").textContent).toContain(
      "chưa là thành viên workspace",
    );
    expect(submit().textContent).toBe("Thêm 6 người · bỏ qua 1 email");
    expect(
      within(dialog).getByText(/9 người sẽ thấy ngay|6 người sẽ thấy ngay/),
    ).toBeTruthy();
    fireEvent.click(submit());
    expect(props.onAddMembers).toHaveBeenCalledWith([
      { subjectType: "User", subjectId: "u-hung", role: "Member" },
      { subjectType: "Team", subjectId: "t-qa", role: "Member" },
    ]);
  });

  it("FUI[workspace.space-page:members-add-role] picks the role for everyone added", () => {
    const { props } = renderPage(spacePageMembersInviteScenario());
    const dialog = screen.getByRole("dialog");
    fireEvent.click(within(dialog).getByRole("button", { name: /Thành viên/ }));
    fireEvent.click(screen.getByRole("option", { name: /Người xem/ }));
    fireEvent.click(
      within(dialog).getByRole("button", { name: /^Thêm 6 người/ }),
    );
    expect(props.onAddMembers).toHaveBeenCalledWith([
      { subjectType: "User", subjectId: "u-hung", role: "Viewer" },
      { subjectType: "Team", subjectId: "t-qa", role: "Viewer" },
    ]);
  });

  it("FUI[workspace.space-page:members-filter] narrows by text, role and how they joined", () => {
    renderMembers();
    const search = screen.getByRole("searchbox");
    fireEvent.change(search, { target: { value: "tran minh" } });
    expect(screen.getByText("Trần Minh Quân", { selector: "b" })).toBeTruthy();
    expect(screen.queryByText("Vũ Thanh", { selector: "b" })).toBeNull();
    fireEvent.change(search, { target: { value: "zzz" } });
    expect(screen.getByText(copy.members.noMatches)).toBeTruthy();
    fireEvent.change(search, { target: { value: "" } });
    fireEvent.click(screen.getByRole("button", { name: /Vai trò/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Quản trị" }));
    expect(screen.getByText("Lê Hoàng Phúc", { selector: "b" })).toBeTruthy();
    expect(screen.queryByText("Vũ Thanh", { selector: "b" })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: /Vào qua/ }));
    fireEvent.click(screen.getByRole("menuitemradio", { name: "Cả nhóm" }));
    expect(screen.getByText(copy.members.noMatches)).toBeTruthy();
  });

  it("FUI[workspace.space-page:members-read-only] shows roles without a way to change them", () => {
    renderPage(spacePageMembersReadOnlyScenario());
    const row = screen
      .getByText("Lê Hoàng Phúc", { selector: "b" })
      .closest('[role="row"]') as HTMLElement;
    const role = within(row).getByRole("button", { name: /Quản trị/ });
    expect(role.getAttribute("aria-disabled")).toBe("true");
    expect(role.getAttribute("title")).toBe(copy.members.readOnly);
    fireEvent.click(role);
    expect(screen.queryByRole("menu")).toBeNull();
    expect(
      within(row).queryByRole("button", { name: /Thao tác với/ }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: copy.page.addMember }),
    ).toBeNull();
  });

  it("FUI[workspace.space-page:members-states] shows loading, an empty department and an error with a retry", () => {
    const empty = renderPage(spacePageMembersEmptyScenario());
    expect(screen.getByText(copy.members.empty)).toBeTruthy();
    empty.view.unmount();
    const loading = renderPage(spacePageMembersLoadingScenario());
    expect(
      screen.getByRole("status", { name: copy.members.loading }),
    ).toBeTruthy();
    loading.view.unmount();
    const { props } = renderPage(spacePageMembersErrorScenario());
    fireEvent.click(
      within(screen.getByRole("alert")).getByRole("button", {
        name: copy.members.retry,
      }),
    );
    expect(props.onRetryMembers).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:settings-save] saves the name and description only when they changed", () => {
    const { props } = renderPage(spacePageSettingsScenario());
    const save = screen.getByRole("button", { name: copy.settings.save });
    expect(save.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(save);
    expect(props.onSave).not.toHaveBeenCalled();
    fireEvent.change(screen.getByLabelText(copy.settings.nameLabel), {
      target: { value: "  Công nghệ thông tin " },
    });
    expect(save.getAttribute("aria-disabled")).toBeNull();
    fireEvent.click(save);
    expect(props.onSave).toHaveBeenCalledWith({ name: "Công nghệ thông tin" });
  });

  it("FUI[workspace.space-page:visibility] saves the visibility when it is chosen, with the arrow keys too", () => {
    const { props } = renderPage(spacePageSettingsScenario());
    const [priv, open] = screen.getAllByRole("radio");
    expect(priv!.getAttribute("aria-checked")).toBe("true");
    expect(priv!.getAttribute("tabindex")).toBe("0");
    expect(open!.getAttribute("tabindex")).toBe("-1");
    fireEvent.click(open!);
    expect(props.onSave).toHaveBeenLastCalledWith({ visibility: "Workspace" });
    fireEvent.click(priv!);
    expect(props.onSave).toHaveBeenCalledTimes(1);
    fireEvent.keyDown(priv!, { key: "ArrowDown" });
    expect(props.onSave).toHaveBeenCalledTimes(2);
    expect(document.activeElement).toBe(open);
  });

  it("FUI[workspace.space-page:move] moves the department from the tree placement", () => {
    const { props } = renderPage(spacePageSettingsScenario());
    fireEvent.click(
      screen.getByRole("button", { name: copy.settings.placeMove }),
    );
    expect(props.onMoveSpace).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:archive-blocked] explains why a department with content cannot be archived", () => {
    renderPage(spacePageSettingsBlockedScenario());
    const archive = screen.getByRole("button", { name: copy.settings.archive });
    expect(archive.getAttribute("aria-disabled")).toBe("true");
    expect(
      screen.getByText(/còn 2 phòng ban con, 6 board và 3 tài liệu/),
    ).toBeTruthy();
  });

  it("FUI[workspace.space-page:archive] archives an empty department after confirmation", () => {
    const { props } = renderPage(spacePageSettingsEmptyScenario());
    fireEvent.click(
      screen.getByRole("button", { name: copy.settings.archive }),
    );
    const dialog = screen.getByRole("alertdialog", {
      name: "Lưu trữ Phòng Công nghệ?",
    });
    fireEvent.click(
      within(dialog).getByRole("button", {
        name: copy.settings.archiveConfirmAction,
      }),
    );
    expect(props.onArchive).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:delete-inert] draws Delete inert while no delete write exists", () => {
    renderPage(spacePageSettingsEmptyScenario());
    const remove = screen.getByRole("button", { name: copy.settings.delete });
    expect(remove.getAttribute("aria-disabled")).toBe("true");
    expect(remove.getAttribute("title")).toBe(copy.settings.deleteUnavailable);
    fireEvent.click(remove);
    expect(screen.queryByRole("group")).toBeNull();
  });

  it("FUI[workspace.space-page:delete-confirm] asks for the name before the red button works", () => {
    const onDelete = vi.fn();
    renderPage(spacePageSettingsScenario(), { onDelete });
    const group = screen.getByRole("group", { name: /^Xoá Phòng Công nghệ\?/ });
    const confirm = within(group).getByRole("button", {
      name: copy.settings.delete,
    });
    expect(confirm.getAttribute("aria-disabled")).toBe("true");
    fireEvent.click(confirm);
    expect(onDelete).not.toHaveBeenCalled();
    fireEvent.change(within(group).getByLabelText(/Gõ/), {
      target: { value: "Phòng Công nghệ" },
    });
    fireEvent.click(confirm);
    expect(onDelete).toHaveBeenCalledTimes(1);
    fireEvent.click(
      within(group).getByRole("button", { name: copy.settings.cancel }),
    );
    expect(screen.queryByRole("group")).toBeNull();
  });

  it("FUI[workspace.space-page:archived] shows an archived department read-only with Unarchive", () => {
    const { props } = renderPage(spacePageArchivedScenario());
    expect(screen.getByText(copy.page.archivedNotice)).toBeTruthy();
    expect(screen.queryByRole("button", { name: /^Tạo mới/ })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: copy.page.unarchive }));
    expect(props.onUnarchive).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.space-page:refused] shows the producer's refusal when hidden content blocks archive", () => {
    renderPage(spacePageSettingsEmptyScenario(), { archiveRefused: true });
    expect(screen.getByText(copy.settings.archiveBlockedUnknown)).toBeTruthy();
  });
});
