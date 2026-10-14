import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { SidebarProvider } from "@notrelix/ui-web";

import {
  breadcrumbFixture,
  shellCopyFixture,
  shellNavigationFixture,
  sidebarDefaultScenario,
  sidebarDocsTreeFixture,
  sidebarLoadingScenario,
  sidebarMockupScenario,
} from "../../../verification/workspace-shell-fixtures";
import {
  sidebarDepartmentsFixture,
  sidebarSpacesFixture,
} from "../../../verification/workspace-spaces-fixtures";
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
const LOCALES = ["vi", "en"] as const;

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
    onTogglePin: vi.fn(),
    onRetryNavigation: vi.fn(),
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

  it("FUI[workspace.sidebar:counts] shows quiet counts, 99+ above 99 with the full number spoken, and nothing when unavailable or zero, in vi and en", () => {
    const expected = {
      vi: {
        myWork: "Việc của tôi",
        inbox: "Hộp thư",
        seven: "Việc của tôi, 7 việc",
        one: "Việc của tôi, 1 việc",
        busy: "Hộp thư, 120 chưa đọc",
      },
      en: {
        myWork: "My work",
        inbox: "Inbox",
        seven: "My work, 7 tasks",
        one: "My work, 1 task",
        busy: "Inbox, 120 unread",
      },
    } as const;
    for (const locale of LOCALES) {
      const text = expected[locale];
      const first = renderSidebar(
        { counts: { myWork: 7, inbox: null } },
        sidebarMockupScenario(locale),
      );
      const myWork = screen.getByRole("link", { name: text.seven });
      expect(myWork.textContent).toContain("7");
      const inbox = screen.getByRole("link", { name: text.inbox });
      expect(inbox.textContent).toBe(text.inbox);
      expect(inbox.textContent).not.toContain("0");
      first.unmount();

      const second = renderSidebar(
        { counts: { myWork: 0, inbox: 120 } },
        sidebarMockupScenario(locale),
      );
      expect(screen.getByRole("link", { name: text.myWork }).textContent).toBe(
        text.myWork,
      );
      const busy = screen.getByRole("link", { name: text.busy });
      expect(busy.textContent).toContain("99+");
      expect(busy.textContent).not.toContain("120");
      second.unmount();

      const third = renderSidebar(
        { counts: { myWork: 1, inbox: null } },
        sidebarMockupScenario(locale),
      );
      expect(screen.getByRole("link", { name: text.one })).toBeTruthy();
      third.unmount();
    }
  });

  it("FUI[workspace.sidebar:command] opens the command palette from the search row, in vi and en", () => {
    for (const locale of LOCALES) {
      const { props, unmount } = renderSidebar(
        {},
        sidebarMockupScenario(locale),
      );
      const search = screen.getByRole("button", {
        name: new RegExp(shellCopyFixture(locale).sidebar.search),
      });
      expect(search.textContent).toContain("⌘K");
      fireEvent.click(search);
      expect(props.onOpenCommand).toHaveBeenCalledTimes(1);
      unmount();
    }
    expect(shellCopyFixture("vi").sidebar.search).toBe("Tìm hoặc chạy lệnh");
  });

  it("FUI[workspace.sidebar:departments] heads the Space tree with Departments and offers Create department only to managers, in vi and en", () => {
    const text = {
      vi: { title: "Phòng ban", create: "Tạo phòng ban" },
      en: { title: "Departments", create: "Create department" },
    } as const;
    for (const locale of LOCALES) {
      const spaces = {
        ...sidebarDepartmentsFixture(locale),
        onCreateSpace: vi.fn(),
      };
      const manager = renderSidebar({ spaces }, sidebarMockupScenario(locale));
      const nav = screen.getByRole("navigation", {
        name: shellCopyFixture(locale).nav.label,
      });
      expect(within(nav).getByText(text[locale].title)).toBeTruthy();
      fireEvent.click(
        within(nav).getByRole("button", { name: text[locale].create }),
      );
      expect(spaces.onCreateSpace).toHaveBeenCalledTimes(1);
      manager.unmount();

      const member = renderSidebar(
        { spaces: { ...spaces, canCreateRoot: false } },
        sidebarMockupScenario(locale),
      );
      expect(screen.getByText(text[locale].title)).toBeTruthy();
      expect(
        screen.queryByRole("button", { name: text[locale].create }),
      ).toBeNull();
      member.unmount();
    }
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
    // Top-level rows take their inset from the 10 px row padding; nested
    // rows indent their content to 39 px.
    expect(parseInt(sub.style.paddingInlineStart, 10)).toBe(39);
    expect(top.style.paddingInlineStart).toBe("");
  });

  it("FUI[workspace.sidebar:docs-tree] opens the department's document tree under Docs and marks the open page", () => {
    const { props } = renderSidebar(
      {
        location: { section: "doc", docId: "page-invoice" },
        docsTree: sidebarDocsTreeFixture(),
      },
      sidebarMockupScenario(),
    );

    // A document page keeps the Docs row selected.
    expect(
      screen
        .getByRole("link", { name: "Tài liệu" })
        .getAttribute("aria-current"),
    ).toBe("page");
    const group = screen.getByRole("group", { name: "Tài liệu" });
    const open = within(group).getByRole("link", {
      name: "Nhập hoá đơn đầu vào",
    });
    expect(open.getAttribute("aria-current")).toBe("page");
    expect(open.getAttribute("href")).toBe(
      "/workspaces/ws-product/docs/page-invoice",
    );
    fireEvent.click(within(group).getByRole("link", { name: "Quy trình" }), {
      button: 0,
    });
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "doc",
      docId: "page-process",
    });
    const collapse = within(group).getByRole("button", {
      name: "Thu gọn Kế toán",
    });
    expect(collapse.getAttribute("aria-expanded")).toBe("true");
  });

  it("FUI[workspace.sidebar:setup] keeps every workspace setup link in a footer disclosure for owners and admins, in vi and en", () => {
    const cases = [
      {
        locale: "vi",
        role: "admin",
        toggle: "Thiết lập workspace",
        links: ["Cài đặt chung", "Thành viên", "Nhóm", "Gói và thanh toán"],
      },
      {
        locale: "en",
        role: "owner",
        toggle: "Workspace setup",
        links: ["General settings", "Members", "Teams", "Plan and billing"],
      },
    ] as const;
    for (const item of cases) {
      const scenario = sidebarMockupScenario(item.locale);
      const { unmount } = renderSidebar(
        { workspace: { ...scenario.workspace!, role: item.role } },
        scenario,
      );
      const toggle = screen.getByRole("button", { name: item.toggle });
      expect(toggle.getAttribute("aria-expanded")).toBe("false");
      expect(screen.queryByRole("link", { name: item.links[0] })).toBeNull();

      fireEvent.click(toggle);

      expect(toggle.getAttribute("aria-expanded")).toBe("true");
      const href = (name: string) =>
        screen.getByRole("link", { name }).getAttribute("href");
      expect(item.links.map(href)).toEqual([
        "/workspaces/ws-product/settings",
        "/workspaces/ws-product/members",
        "/workspaces/ws-product/teams",
        "/workspaces/ws-product/billing",
      ]);
      unmount();
    }
  });

  it("FUI[workspace.sidebar:setup-member] shows members only Members and Teams", () => {
    const scenario = sidebarMockupScenario();
    renderSidebar(
      { workspace: { ...scenario.workspace!, role: "member" } },
      scenario,
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Thiết lập workspace" }),
    );
    expect(screen.getByRole("link", { name: "Thành viên" })).toBeTruthy();
    expect(screen.getByRole("link", { name: "Nhóm" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: "Cài đặt chung" })).toBeNull();
    expect(
      screen.queryByRole("link", { name: "Gói và thanh toán" }),
    ).toBeNull();
  });

  it("FUI[workspace.sidebar:setup-guest] gives guests no workspace setup", () => {
    const scenario = sidebarMockupScenario();
    renderSidebar(
      { workspace: { ...scenario.workspace!, role: "guest" } },
      scenario,
    );
    expect(
      screen.queryByRole("button", { name: "Thiết lập workspace" }),
    ).toBeNull();
    expect(screen.queryByRole("link", { name: "Thành viên" })).toBeNull();
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
    fireEvent.click(
      screen.getByRole("menuitem", { name: viCopy.user.profile }),
    );
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "account",
      page: "profile",
    });

    openMenu(trigger);
    fireEvent.click(
      screen.getByRole("menuitem", { name: viCopy.user.signOut }),
    );
    expect(props.onSignOut).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.sidebar:system-rail] the workspace chip shows the name and size and opens the system rail", () => {
    const { props } = renderSidebar({}, sidebarMockupScenario());
    const chip = screen.getByRole("button", { name: /^Minh An Group/ });
    expect(chip.textContent).toContain("Minh An Group");
    expect(chip.textContent).toContain("5 phòng ban · 86 người");

    fireEvent.click(chip);

    expect(props.onOpenSystemRail).toHaveBeenCalledTimes(1);
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

  it("FUI[workspace.sidebar:pinned] lists pinned Boards and Docs above the departments and unpins from the keyboard, in vi and en", () => {
    const text = {
      vi: {
        heading: "Ghim",
        departments: "Phòng ban",
        unpin: "Bỏ ghim Triển khai ERP Q4",
      },
      en: {
        heading: "Pinned",
        departments: "Departments",
        unpin: "Unpin Triển khai ERP Q4",
      },
    } as const;
    for (const locale of LOCALES) {
      const { props, unmount } = renderSidebar(
        { spaces: sidebarDepartmentsFixture(locale) },
        sidebarMockupScenario(locale),
      );
      const nav = screen.getByRole("navigation", {
        name: shellCopyFixture(locale).nav.label,
      });
      const heading = within(nav).getByText(text[locale].heading);
      const departments = within(nav).getByText(text[locale].departments);
      expect(
        heading.compareDocumentPosition(departments) &
          Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy();

      const pinned = within(nav).getByRole("list", {
        name: text[locale].heading,
      });
      const links = within(pinned).getAllByRole("link");
      expect(links.map((link) => link.getAttribute("href"))).toEqual([
        "/workspaces/ws-product/boards/board-cn-erp",
        "/workspaces/ws-product/docs/doc-cn-handbook",
      ]);
      expect(links[0]!.textContent).toContain("Triển khai ERP Q4");

      const unpin = within(pinned).getByRole("button", {
        name: text[locale].unpin,
      });
      expect(unpin.getAttribute("tabindex")).not.toBe("-1");
      unpin.focus();
      expect(document.activeElement).toBe(unpin);
      fireEvent.click(unpin);
      expect(props.onTogglePin).toHaveBeenCalledTimes(1);
      expect(props.onTogglePin).toHaveBeenCalledWith(
        expect.objectContaining({ resourceId: "board-cn-erp" }),
        false,
      );
      unmount();
    }
  });

  it("FUI[workspace.sidebar:pinned-empty] hides the Pinned group without pins and while loading, and offers a retry when pins fail, in vi and en", () => {
    const text = {
      vi: { heading: "Ghim", retry: "Thử lại" },
      en: { heading: "Pinned", retry: "Retry" },
    } as const;
    for (const locale of LOCALES) {
      for (const pinned of [
        { status: "ready", entries: [] },
        { status: "loading" },
      ] as const) {
        const view = renderSidebar({ pinned }, sidebarMockupScenario(locale));
        expect(screen.queryByText(text[locale].heading)).toBeNull();
        view.unmount();
      }

      const failed = renderSidebar(
        { pinned: { status: "error" } },
        sidebarMockupScenario(locale),
      );
      expect(screen.getByText(text[locale].heading)).toBeTruthy();
      expect(
        screen.getByText(shellCopyFixture(locale).sidebar.pinnedError),
      ).toBeTruthy();
      fireEvent.click(screen.getByRole("button", { name: text[locale].retry }));
      expect(failed.props.onRetryNavigation).toHaveBeenCalledTimes(1);
      failed.unmount();
    }
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
      "Board: Product Roadmap",
      "Doc: Q4 product specification",
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
