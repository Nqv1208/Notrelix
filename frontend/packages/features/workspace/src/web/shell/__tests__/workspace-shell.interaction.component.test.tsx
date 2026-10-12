import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { SidebarProvider } from "@notrelix/ui-web";

import {
  breadcrumbFixture,
  shellBoardsFixture,
  shellCopyFixture,
  shellDocsFixture,
  shellMembersFixture,
  shellNavigationFixture,
  sidebarDefaultScenario,
  sidebarLoadingScenario,
  sidebarMockupScenario,
} from "../../../verification/workspace-shell-fixtures";
import {
  sidebarDepartmentsFixture,
  sidebarSpacesFixture,
} from "../../../verification/workspace-spaces-fixtures";
import { WorkspaceCommandPaletteSurface } from "../workspace-command-palette-surface";
import { WorkspaceScopeStateSurface } from "../workspace-scope-state-surface";
import { WorkspaceShortcutSheetSurface } from "../workspace-shortcut-sheet-surface";
import {
  WorkspaceSidebarSurface,
  type WorkspaceSidebarSurfaceProps,
} from "../workspace-sidebar-surface";
import { WorkspaceTopbarSurface } from "../workspace-topbar-surface";

beforeAll(() => {
  // cmdk scrolls the selected item into view; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
});

const copy = shellCopyFixture();
const viCopy = shellCopyFixture("vi");

function frame(ui: ReactNode) {
  return <SidebarProvider>{ui}</SidebarProvider>;
}

function renderSidebar(
  overrides: Partial<WorkspaceSidebarSurfaceProps> = {},
  scenario = sidebarDefaultScenario(),
) {
  const props: WorkspaceSidebarSurfaceProps = {
    ...scenario,
    navigation: shellNavigationFixture(vi.fn()),
    onOpenSystemRail: vi.fn(),
    onOpenCommand: vi.fn(),
    onOpenShortcuts: vi.fn(),
    onSignOut: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(frame(<WorkspaceSidebarSurface {...props} />));
  return { props, unmount: view.unmount };
}

function openMenu(trigger: HTMLElement) {
  fireEvent.keyDown(trigger, { key: "Enter" });
}

function primaryLinks(name: string) {
  const nav = screen.getByRole("navigation", { name });
  const list = within(nav).getByRole("list", { name: /Main|Chính/ });
  return within(list).getAllByRole("link");
}

describe("WorkspaceSidebarSurface", () => {
  it("FUI[workspace.sidebar:render] lists Home, My work, Inbox, Docs and Reports in order as real destinations, in vi and en", () => {
    const en = renderSidebar({ location: { section: "home" } });
    expect(
      primaryLinks(copy.nav.label).map((link) => [
        link.textContent?.replace(/\d+|99\+/g, "").trim(),
        link.getAttribute("href"),
      ]),
    ).toEqual([
      ["Home", "/workspaces/ws-product"],
      ["My work", "/workspaces/ws-product/my-work"],
      ["Inbox", "/workspaces/ws-product/inbox"],
      ["Docs", "/workspaces/ws-product/library"],
      ["Reports", "/workspaces/ws-product/reports"],
    ]);
    expect(
      screen.getByRole("link", { name: "Home" }).getAttribute("aria-current"),
    ).toBe("page");
    en.unmount();

    renderSidebar({}, sidebarMockupScenario());
    expect(
      primaryLinks(viCopy.nav.label).map((link) =>
        link.textContent?.replace(/\d+|99\+/g, "").trim(),
      ),
    ).toEqual(["Trang chủ", "Việc của tôi", "Hộp thư", "Tài liệu", "Báo cáo"]);
    expect(
      primaryLinks(viCopy.nav.label).some((link) =>
        link.hasAttribute("aria-current"),
      ),
    ).toBe(false);
    // Pins and recents stay on Home; the old Manage group is gone.
    expect(screen.queryByText(/Đã ghim|Gần đây|Pinned|Recent/)).toBeNull();
  });

  it("FUI[workspace.sidebar:navigate] hands plain clicks to the host router and leaves modified clicks to the browser", () => {
    const { props } = renderSidebar();
    const link = screen.getByRole("link", { name: /^My work/ });

    fireEvent.click(link, { button: 0 });
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "my-work",
    });

    fireEvent.click(link, { button: 0, metaKey: true });
    expect(props.navigation.onNavigate).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.sidebar:counts] shows quiet counts, 99+ above 99 with the full number spoken, and nothing when unavailable or zero", () => {
    const first = renderSidebar(
      { counts: { myWork: 7, inbox: null } },
      sidebarMockupScenario(),
    );
    const myWork = screen.getByRole("link", { name: "Việc của tôi, 7 việc" });
    expect(myWork.textContent).toContain("7");
    const inbox = screen.getByRole("link", { name: "Hộp thư" });
    expect(inbox.textContent).toBe("Hộp thư");
    expect(inbox.textContent).not.toContain("0");
    first.unmount();

    renderSidebar(
      { counts: { myWork: 0, inbox: 120 } },
      sidebarMockupScenario(),
    );
    expect(
      screen.getByRole("link", { name: "Việc của tôi" }).textContent,
    ).toBe("Việc của tôi");
    const busy = screen.getByRole("link", { name: "Hộp thư, 120 chưa đọc" });
    expect(busy.textContent).toContain("99+");
    expect(busy.textContent).not.toContain("120");
  });

  it("FUI[workspace.sidebar:command] opens the command palette from the search row", () => {
    const { props } = renderSidebar({}, sidebarMockupScenario());
    const search = screen.getByRole("button", { name: /Tìm hoặc chạy lệnh/ });
    expect(search.textContent).toContain("⌘K");
    fireEvent.click(search);
    expect(props.onOpenCommand).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.sidebar:departments] heads the Space tree with Departments and offers Create department only to managers", () => {
    const spaces = { ...sidebarDepartmentsFixture(), onCreateSpace: vi.fn() };
    const manager = renderSidebar({ spaces }, sidebarMockupScenario());
    const nav = screen.getByRole("navigation", { name: viCopy.nav.label });
    expect(within(nav).getByText("Phòng ban")).toBeTruthy();
    fireEvent.click(within(nav).getByRole("button", { name: "Tạo phòng ban" }));
    expect(spaces.onCreateSpace).toHaveBeenCalledTimes(1);
    manager.unmount();

    renderSidebar(
      { spaces: { ...spaces, canCreateRoot: false } },
      sidebarMockupScenario(),
    );
    expect(screen.getByText("Phòng ban")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Tạo phòng ban" })).toBeNull();
  });

  it("FUI[workspace.sidebar:spaces] lists an expanded department's boards, marks private ones and keeps sub-departments indented", () => {
    const { props } = renderSidebar(
      { spaces: sidebarDepartmentsFixture() },
      sidebarMockupScenario(),
    );
    const tree = screen.getByRole("tree", { name: "Phòng ban" });
    const items = within(tree).getAllByRole("treeitem");
    expect(items.slice(0, 7).map((node) => node.textContent?.trim())).toEqual([
      "CNCông nghệ, Riêng tư",
      "Triển khai ERP Q4",
      "Hạ tầng & bảo mật",
      "Hỗ trợ nội bộ",
      "Ngân sách CNTT 2027, Riêng tư",
      "NTNhóm Hạ tầng",
      "NDNhóm Ứng dụng, Riêng tư",
    ]);
    const budget = within(tree).getByRole("treeitem", {
      name: /Ngân sách CNTT 2027/,
    });
    expect(budget.getAttribute("href")).toBe(
      "/workspaces/ws-product/boards/board-cn-budget",
    );
    expect(budget.getAttribute("aria-level")).toBe("2");
    fireEvent.click(budget, { button: 0 });
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "board",
      boardId: "board-cn-budget",
    });

    const sub = within(tree).getByRole("treeitem", { name: /Nhóm Hạ tầng/ });
    const top = within(tree).getByRole("treeitem", { name: /Công nghệ/ });
    expect(sub.getAttribute("aria-level")).toBe("2");
    expect(parseInt(sub.style.paddingInlineStart, 10)).toBeGreaterThan(
      parseInt(top.style.paddingInlineStart, 10),
    );
  });

  it("FUI[workspace.sidebar:setup] keeps workspace setup in a footer disclosure", () => {
    renderSidebar({}, sidebarMockupScenario());
    const toggle = screen.getByRole("button", { name: "Thiết lập workspace" });
    expect(toggle.getAttribute("aria-expanded")).toBe("false");
    expect(screen.queryByRole("link", { name: "Cài đặt chung" })).toBeNull();

    fireEvent.click(toggle);

    expect(toggle.getAttribute("aria-expanded")).toBe("true");
    const href = (name: string) =>
      screen.getByRole("link", { name }).getAttribute("href");
    expect(href("Cài đặt chung")).toBe("/workspaces/ws-product/settings");
    expect(href("Thành viên")).toBe("/workspaces/ws-product/members");
    expect(href("Nhóm")).toBe("/workspaces/ws-product/teams");
    expect(href("Gói và thanh toán")).toBe("/workspaces/ws-product/billing");
  });

  it("FUI[workspace.sidebar:user] shows the member's name and workspace role and keeps the account menu", () => {
    const { props } = renderSidebar({}, sidebarMockupScenario());
    const trigger = screen.getByRole("button", {
      name: "Menu tài khoản của Nguyễn Thu Hà",
    });
    expect(trigger.textContent).toContain("Nguyễn Thu Hà");
    expect(trigger.textContent).toContain("Quản trị workspace");

    openMenu(trigger);
    fireEvent.click(
      screen.getByRole("menuitem", { name: viCopy.user.shortcuts }),
    );
    expect(props.onOpenShortcuts).toHaveBeenCalledTimes(1);

    openMenu(trigger);
    fireEvent.click(screen.getByRole("menuitem", { name: viCopy.user.profile }));
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "account",
      page: "profile",
    });

    openMenu(trigger);
    fireEvent.click(screen.getByRole("menuitem", { name: viCopy.user.signOut }));
    expect(props.onSignOut).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.sidebar:system-rail] the workspace chip shows the name and size and opens the system rail", () => {
    const { props } = renderSidebar({}, sidebarMockupScenario());
    const chip = screen.getByRole("button", { name: /^Minh An Group/ });
    expect(chip.textContent).toContain("Minh An Group");
    expect(chip.textContent).toContain("5 phòng ban · 86 người");

    fireEvent.click(chip);
    openMenu(chip);

    expect(props.onOpenSystemRail).toHaveBeenCalled();
    expect(screen.queryByRole("menu")).toBeNull();
  });

  it("FUI[workspace.sidebar:states] shows a workspace placeholder while loading and no counts when unavailable", () => {
    renderSidebar({}, sidebarLoadingScenario());
    expect(
      screen.getByRole("status", { name: copy.switcher.loading }),
    ).toBeTruthy();
    expect(screen.getByRole("link", { name: "My work" }).textContent).toBe(
      "My work",
    );
    expect(screen.getByRole("link", { name: "Inbox" }).textContent).toBe(
      "Inbox",
    );
  });

  it("FUI[workspace.sidebar:keyboard] keeps every destination keyboard reachable in reading order", () => {
    renderSidebar({ spaces: sidebarSpacesFixture() });
    const nav = screen.getByRole("navigation", { name: copy.nav.label });
    fireEvent.click(screen.getByRole("button", { name: copy.sidebar.setup }));
    const links = within(nav)
      .getAllByRole("link")
      .filter((link) => link.getAttribute("role") !== "treeitem");
    expect(links.map((link) => link.textContent?.trim())).toEqual([
      copy.nav.home,
      `${copy.nav.myWork}7`,
      `${copy.nav.inbox}3`,
      copy.nav.library,
      copy.nav.reports,
      copy.sidebar.setupGeneral,
      copy.sidebar.setupMembers,
      copy.sidebar.setupTeams,
      copy.sidebar.setupBilling,
    ]);
    for (const link of links) {
      expect(link.getAttribute("tabindex")).not.toBe("-1");
      link.focus();
      expect(document.activeElement).toBe(link);
    }
  });
});

describe("WorkspaceTopbarSurface", () => {
  function renderTopbar(
    breadcrumb = breadcrumbFixture({
      section: "board",
      boardId: "board-roadmap",
      view: "table",
    }),
  ) {
    const props = {
      copy,
      navigation: shellNavigationFixture(vi.fn()),
      breadcrumb,
      appearance: "light" as const,
      commandShortcut: "⌘K",
      onOpenCommand: vi.fn(),
      onToggleTheme: vi.fn(),
    };
    renderPureUi(frame(<WorkspaceTopbarSurface {...props} />));
    return props;
  }

  it("FUI[workspace.topbar:breadcrumb] names the hierarchy from loaded data, not from the URL", () => {
    renderTopbar();
    const crumbs = screen.getByRole("navigation", {
      name: copy.breadcrumb.label,
    });
    expect(
      within(crumbs)
        .getByRole("link", { name: "Product" })
        .getAttribute("href"),
    ).toBe("/workspaces/ws-product");
    expect(
      within(crumbs).getByRole("link", { name: "Product Roadmap" }),
    ).toBeTruthy();
    expect(within(crumbs).getByText(copy.views.table)).toBeTruthy();
    expect(within(crumbs).queryByText("board-roadmap")).toBeNull();
  });

  it("FUI[workspace.topbar:breadcrumb-unknown] never shows a raw id for a resource missing from the authorized list", () => {
    renderTopbar(
      breadcrumbFixture({ section: "doc", docId: "doc-secret-123" }),
    );
    const crumbs = screen.getByRole("navigation", {
      name: copy.breadcrumb.label,
    });
    expect(within(crumbs).getByText(copy.breadcrumb.unavailable)).toBeTruthy();
    expect(within(crumbs).queryByText(/doc-secret-123/)).toBeNull();
  });

  it("FUI[workspace.topbar:actions] opens the palette, toggles the theme and links to the workspace", () => {
    const props = renderTopbar();

    fireEvent.click(
      screen.getAllByRole("button", { name: copy.topbar.commandLabel })[0]!,
    );
    expect(props.onOpenCommand).toHaveBeenCalledTimes(1);

    fireEvent.click(
      screen.getByRole("button", { name: copy.topbar.themeToDark }),
    );
    expect(props.onToggleTheme).toHaveBeenCalledTimes(1);

    fireEvent.click(screen.getByRole("link", { name: "Product" }), {
      button: 0,
    });
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "home",
    });
  });
});

describe("WorkspaceCommandPaletteSurface", () => {
  function renderPalette(
    overrides: { defaultQuery?: string; empty?: boolean } = {},
  ) {
    const props = {
      copy,
      open: true,
      onOpenChange: vi.fn(),
      boards: overrides.empty ? [] : shellBoardsFixture,
      docs: overrides.empty ? [] : shellDocsFixture,
      members: overrides.empty ? [] : shellMembersFixture(),
      appearance: "light" as const,
      canCreateWorkspace: true,
      onSelect: vi.fn(),
      defaultQuery: overrides.defaultQuery,
    };
    renderPureUi(<WorkspaceCommandPaletteSurface {...props} />);
    return props;
  }

  it("FUI[workspace.command-palette:a11y] is a labelled dialog with a labelled input that holds focus", () => {
    renderPalette();
    const dialog = screen.getByRole("dialog", { name: copy.palette.title });
    const input = within(dialog).getByRole("combobox", {
      name: copy.palette.title,
    });
    expect(document.activeElement).toBe(input);
    expect(within(dialog).getByRole("listbox")).toBeTruthy();
    expect(within(dialog).getByText(copy.palette.groups.boards)).toBeTruthy();
    expect(within(dialog).getByText(copy.palette.groups.docs)).toBeTruthy();
    expect(within(dialog).getByText(copy.palette.groups.members)).toBeTruthy();
    expect(
      within(dialog).getByText(copy.palette.groups.navigation),
    ).toBeTruthy();
  });

  it("FUI[workspace.command-palette:navigate] filters and opens a Board", () => {
    const props = renderPalette();
    const input = screen.getByRole("combobox", { name: copy.palette.title });
    fireEvent.change(input, { target: { value: "roadmap" } });

    fireEvent.click(screen.getByRole("option", { name: /Product Roadmap/ }));

    expect(props.onOpenChange).toHaveBeenCalledWith(false);
    expect(props.onSelect).toHaveBeenCalledWith({
      type: "navigate",
      destination: { section: "board", boardId: "board-roadmap" },
    });
  });

  it("FUI[workspace.command-palette:search] hands the query to workspace search", () => {
    const props = renderPalette({ defaultQuery: "launch" });
    fireEvent.click(
      screen.getByRole("option", { name: /Search the workspace for “launch”/ }),
    );
    expect(props.onSelect).toHaveBeenCalledWith({
      type: "search",
      query: "launch",
    });
  });

  it("FUI[workspace.command-palette:actions] runs shell actions", () => {
    const props = renderPalette();
    fireEvent.click(
      screen.getByRole("option", { name: new RegExp(copy.palette.shortcuts) }),
    );
    expect(props.onSelect).toHaveBeenCalledWith({
      type: "action",
      action: "show-shortcuts",
    });
  });

  it("FUI[workspace.command-palette:empty] explains when nothing matches", () => {
    renderPalette({ defaultQuery: "zzqq-nothing", empty: true });
    expect(screen.getByText("Nothing matches “zzqq-nothing”.")).toBeTruthy();
  });
});

describe("WorkspaceShortcutSheetSurface", () => {
  it("FUI[workspace.shortcut-sheet:render] lists the shortcuts and closes", () => {
    const onOpenChange = vi.fn();
    renderPureUi(
      <WorkspaceShortcutSheetSurface
        copy={copy}
        open
        onOpenChange={onOpenChange}
        commandShortcut="⌘K"
      />,
    );
    const dialog = screen.getByRole("dialog", { name: copy.shortcuts.title });
    expect(within(dialog).getByText(copy.shortcuts.goHome)).toBeTruthy();
    expect(within(dialog).getByText(copy.shortcuts.goMyWork)).toBeTruthy();
    expect(within(dialog).getByText(copy.shortcuts.goInbox)).toBeTruthy();
    expect(within(dialog).getByText("⌘K")).toBeTruthy();

    fireEvent.click(
      within(dialog).getByRole("button", { name: copy.shortcuts.close }),
    );
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });
});

describe("WorkspaceScopeStateSurface", () => {
  it("FUI[workspace.scope-state:forbidden] explains lost access with a retry and a way out", () => {
    const onRetry = vi.fn();
    const onExit = vi.fn();
    renderPureUi(
      <WorkspaceScopeStateSurface
        copy={copy}
        state="forbidden"
        onRetry={onRetry}
        exitHref="/home"
        onExit={onExit}
      />,
    );
    expect(
      screen.getByRole("heading", { name: copy.scope.forbiddenTitle }),
    ).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: copy.scope.retry }));
    expect(onRetry).toHaveBeenCalledTimes(1);

    const exit = screen.getByRole("link", { name: copy.scope.exit });
    expect(exit.getAttribute("href")).toBe("/home");
    fireEvent.click(exit, { button: 0 });
    expect(onExit).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.scope-state:error] separates a failed load from lost access", () => {
    renderPureUi(
      <WorkspaceScopeStateSurface
        copy={copy}
        state="error"
        onRetry={vi.fn()}
        exitHref="/home"
        onExit={vi.fn()}
      />,
    );
    expect(
      screen.getByRole("heading", { name: copy.scope.errorTitle }),
    ).toBeTruthy();
    expect(screen.queryByText(copy.scope.forbiddenTitle)).toBeNull();
  });

  it("FUI[workspace.scope-state:loading] announces the scope check", () => {
    renderPureUi(
      <WorkspaceScopeStateSurface
        copy={copy}
        state="loading"
        onRetry={vi.fn()}
        exitHref="/home"
        onExit={vi.fn()}
      />,
    );
    expect(
      screen.getByRole("status", { name: copy.scope.loadingTitle }),
    ).toBeTruthy();
  });
});
