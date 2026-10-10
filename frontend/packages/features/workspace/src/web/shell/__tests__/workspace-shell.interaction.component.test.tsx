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
  sidebarEmptyScenario,
  sidebarErrorScenario,
  sidebarLoadingScenario,
} from "../../../verification/workspace-shell-fixtures";
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

function frame(ui: ReactNode) {
  return <SidebarProvider>{ui}</SidebarProvider>;
}

function renderSidebar(overrides: Partial<WorkspaceSidebarSurfaceProps> = {}) {
  const props: WorkspaceSidebarSurfaceProps = {
    ...sidebarDefaultScenario(),
    navigation: shellNavigationFixture(vi.fn()),
    onSwitchWorkspace: vi.fn(),
    onCreateWorkspace: vi.fn(),
    onTogglePin: vi.fn(),
    onRetryNavigation: vi.fn(),
    onOpenShortcuts: vi.fn(),
    onSignOut: vi.fn(),
    ...overrides,
  };
  renderPureUi(frame(<WorkspaceSidebarSurface {...props} />));
  return props;
}

function openMenu(trigger: HTMLElement) {
  fireEvent.keyDown(trigger, { key: "Enter" });
}

describe("WorkspaceSidebarSurface", () => {
  it("FUI[workspace.sidebar:render] renders every entry as a real destination and marks the current one", () => {
    renderSidebar();

    const nav = screen.getByRole("navigation", { name: copy.nav.label });
    const href = (name: string) =>
      within(nav).getByRole("link", { name }).getAttribute("href");
    expect(href(copy.nav.home)).toBe("/workspaces/ws-product");
    expect(href(copy.nav.myWork)).toBe("/workspaces/ws-product/my-work");
    expect(href(copy.nav.inbox)).toBe("/workspaces/ws-product/inbox");
    expect(href(copy.nav.library)).toBe("/workspaces/ws-product/library");
    expect(href(copy.nav.search)).toBe("/workspaces/ws-product/search");
    expect(href(copy.nav.members)).toBe("/workspaces/ws-product/members");
    expect(href(copy.nav.settings)).toBe("/workspaces/ws-product/settings");

    const roadmap = within(nav).getByRole("link", { name: /Product Roadmap/ });
    expect(roadmap.getAttribute("href")).toBe(
      "/workspaces/ws-product/boards/board-roadmap",
    );
    expect(roadmap.getAttribute("aria-current")).toBe("page");
    expect(
      within(nav)
        .getByRole("link", { name: /Sprint 42 retro/ })
        .getAttribute("href"),
    ).toBe("/workspaces/ws-product/docs/doc-retro");
    expect(screen.queryByText("Team online")).toBeNull();
    expect(screen.queryByText("Quick access")).toBeNull();
  });

  it("FUI[workspace.sidebar:navigate] hands plain clicks to the host router and leaves modified clicks to the browser", () => {
    const props = renderSidebar();
    const link = screen.getByRole("link", { name: copy.nav.myWork });

    fireEvent.click(link, { button: 0 });
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "my-work",
    });

    fireEvent.click(link, { button: 0, metaKey: true });
    expect(props.navigation.onNavigate).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.sidebar:switch-workspace] switches and creates workspaces from the switcher", () => {
    const props = renderSidebar();
    const trigger = screen.getByRole("button", {
      name: /Switch workspace, current: Product/,
    });

    openMenu(trigger);
    fireEvent.click(screen.getByRole("menuitem", { name: /Design Studio/ }));
    expect(props.onSwitchWorkspace).toHaveBeenCalledWith("ws-design");

    openMenu(trigger);
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.switcher.create }),
    );
    expect(props.onCreateWorkspace).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.sidebar:pin] pins and unpins from the row menu", () => {
    const props = renderSidebar();

    openMenu(
      screen.getByRole("button", { name: "More actions for Product Roadmap" }),
    );
    fireEvent.click(screen.getByRole("menuitem", { name: copy.lists.unpin }));
    expect(props.onTogglePin).toHaveBeenCalledWith(
      expect.objectContaining({ resourceId: "board-roadmap" }),
      false,
    );

    openMenu(
      screen.getByRole("button", { name: "More actions for Launch checklist" }),
    );
    fireEvent.click(screen.getByRole("menuitem", { name: copy.lists.pin }));
    expect(props.onTogglePin).toHaveBeenLastCalledWith(
      expect.objectContaining({ resourceId: "board-launch" }),
      true,
    );
  });

  it("FUI[workspace.sidebar:collapse] collapses the Pinned section", () => {
    renderSidebar();
    const toggle = screen.getByRole("button", { name: "Show or hide Pinned" });
    expect(toggle.getAttribute("aria-expanded")).toBe("true");

    fireEvent.click(toggle);

    expect(toggle.getAttribute("aria-expanded")).toBe("false");
    expect(screen.queryByRole("link", { name: /Product Roadmap/ })).toBeNull();
    expect(screen.getByRole("link", { name: /Launch checklist/ })).toBeTruthy();
  });

  it("FUI[workspace.sidebar:states] shows loading, empty teaching hints and a retry on error", () => {
    const onRetryNavigation = vi.fn();
    const { unmount } = renderPureUi(
      frame(
        <WorkspaceSidebarSurface
          {...sidebarLoadingScenario()}
          navigation={shellNavigationFixture()}
          onSwitchWorkspace={vi.fn()}
          onCreateWorkspace={vi.fn()}
          onTogglePin={vi.fn()}
          onRetryNavigation={onRetryNavigation}
          onOpenShortcuts={vi.fn()}
          onSignOut={vi.fn()}
        />,
      ),
    );
    expect(
      screen.getByRole("status", { name: copy.switcher.loading }),
    ).toBeTruthy();
    unmount();

    const empty = renderPureUi(
      frame(
        <WorkspaceSidebarSurface
          {...sidebarEmptyScenario()}
          navigation={shellNavigationFixture()}
          onSwitchWorkspace={vi.fn()}
          onCreateWorkspace={vi.fn()}
          onTogglePin={vi.fn()}
          onRetryNavigation={onRetryNavigation}
          onOpenShortcuts={vi.fn()}
          onSignOut={vi.fn()}
        />,
      ),
    );
    expect(screen.getByText(copy.lists.pinnedEmpty)).toBeTruthy();
    expect(screen.getByText(copy.lists.recentEmpty)).toBeTruthy();
    empty.unmount();

    renderPureUi(
      frame(
        <WorkspaceSidebarSurface
          {...sidebarErrorScenario()}
          navigation={shellNavigationFixture()}
          onSwitchWorkspace={vi.fn()}
          onCreateWorkspace={vi.fn()}
          onTogglePin={vi.fn()}
          onRetryNavigation={onRetryNavigation}
          onOpenShortcuts={vi.fn()}
          onSignOut={vi.fn()}
        />,
      ),
    );
    fireEvent.click(
      screen.getAllByRole("button", { name: copy.lists.retry })[0]!,
    );
    expect(onRetryNavigation).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.sidebar:user] opens shortcuts and signs out from the user card", () => {
    const props = renderSidebar();
    const trigger = screen.getByRole("button", {
      name: "Account menu for Linh Tran",
    });

    openMenu(trigger);
    fireEvent.click(
      screen.getByRole("menuitem", { name: copy.user.shortcuts }),
    );
    expect(props.onOpenShortcuts).toHaveBeenCalledTimes(1);

    openMenu(trigger);
    fireEvent.click(screen.getByRole("menuitem", { name: copy.user.profile }));
    expect(props.navigation.onNavigate).toHaveBeenCalledWith({
      section: "account",
      page: "profile",
    });

    openMenu(trigger);
    fireEvent.click(screen.getByRole("menuitem", { name: copy.user.signOut }));
    expect(props.onSignOut).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.sidebar:keyboard] keeps every destination keyboard reachable in reading order", () => {
    renderSidebar();
    const nav = screen.getByRole("navigation", { name: copy.nav.label });
    const links = within(nav).getAllByRole("link");
    expect(links.map((link) => link.textContent?.trim())).toEqual([
      copy.nav.home,
      copy.nav.myWork,
      copy.nav.inbox,
      copy.nav.library,
      copy.nav.search,
      "Board: Product Roadmap",
      "Doc: Q4 product specification",
      "Board: Launch checklist",
      "Doc: Sprint 42 retro",
      "Board: Bug triage",
      copy.nav.members,
      copy.nav.settings,
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
