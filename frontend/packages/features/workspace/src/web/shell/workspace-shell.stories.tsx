import type { Meta, StoryObj } from "@storybook/react";
import type { ReactNode } from "react";
import { SidebarInset, SidebarProvider } from "@notrelix/ui-web";

import {
  breadcrumbFixture,
  shellBoardsFixture,
  shellCopyFixture,
  shellDocsFixture,
  shellMembersFixture,
  shellNavigationFixture,
  sidebarEdgeDataScenario,
  sidebarEmptyScenario,
  sidebarErrorScenario,
  sidebarLoadingScenario,
  sidebarMockupScenario,
} from "../../verification/workspace-shell-fixtures";
import {
  sidebarDepartmentsEdgeFixture,
  sidebarDepartmentsFixture,
  sidebarDepartmentsStateFixture,
} from "../../verification/workspace-spaces-fixtures";
import { WorkspaceCommandPaletteSurface } from "./workspace-command-palette-surface";
import { WorkspaceScopeStateSurface } from "./workspace-scope-state-surface";
import { WorkspaceShortcutSheetSurface } from "./workspace-shortcut-sheet-surface";
import { WorkspaceSidebarSurface } from "./workspace-sidebar-surface";
import { WorkspaceTopbarSurface } from "./workspace-topbar-surface";

const meta: Meta = {
  title: "Workspace/Shell",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;

const sidebarCallbacks = {
  navigation: shellNavigationFixture(),
  onOpenSystemRail: noop,
  onOpenCommand: noop,
  onOpenShortcuts: noop,
  onSignOut: noop,
};

function ShellFrame({
  children,
  openMobile = false,
}: {
  children: ReactNode;
  openMobile?: boolean;
}) {
  return (
    <SidebarProvider
      defaultOpenMobile={openMobile}
      className="bg-surface-canvas"
    >
      {children}
      <SidebarInset className="bg-surface-canvas">
        <div className="p-6 text-body text-muted-foreground">
          Workspace content
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}

// ── Sidebar (WX-SHELL-001, Final-Space `tong-quan`) ──────────────

export const SidebarDefault: Story = {
  render: () => (
    <ShellFrame openMobile>
      <WorkspaceSidebarSurface
        {...sidebarMockupScenario()}
        spaces={sidebarDepartmentsFixture()}
        {...sidebarCallbacks}
      />
    </ShellFrame>
  ),
  tags: ["fui-surface--workspace.sidebar", "fui-state--Default"],
};

export const SidebarLoading: Story = {
  render: () => (
    <ShellFrame>
      <WorkspaceSidebarSurface
        {...sidebarLoadingScenario()}
        spaces={sidebarDepartmentsStateFixture("loading")}
        {...sidebarCallbacks}
      />
    </ShellFrame>
  ),
  tags: ["fui-surface--workspace.sidebar", "fui-state--Loading"],
};

export const SidebarEmpty: Story = {
  render: () => (
    <ShellFrame>
      <WorkspaceSidebarSurface
        {...sidebarEmptyScenario()}
        spaces={sidebarDepartmentsStateFixture("empty")}
        {...sidebarCallbacks}
      />
    </ShellFrame>
  ),
  tags: ["fui-surface--workspace.sidebar", "fui-state--Empty"],
};

export const SidebarError: Story = {
  render: () => (
    <ShellFrame>
      <WorkspaceSidebarSurface
        {...sidebarErrorScenario()}
        spaces={sidebarDepartmentsStateFixture("error")}
        {...sidebarCallbacks}
      />
    </ShellFrame>
  ),
  tags: ["fui-surface--workspace.sidebar", "fui-state--Error"],
};

export const SidebarEdgeData: Story = {
  render: () => (
    <ShellFrame>
      <WorkspaceSidebarSurface
        {...sidebarEdgeDataScenario()}
        spaces={sidebarDepartmentsEdgeFixture()}
        {...sidebarCallbacks}
      />
    </ShellFrame>
  ),
  tags: ["fui-surface--workspace.sidebar", "fui-state--EdgeData"],
};

// ── Topbar (WX-SHELL-002) ─────────────────────────────────────────

function TopbarFrame({ children }: { children: ReactNode }) {
  return (
    <SidebarProvider className="min-h-0 bg-surface-canvas">
      <div className="w-full">{children}</div>
    </SidebarProvider>
  );
}

const topbarBase = {
  copy: shellCopyFixture(),
  navigation: shellNavigationFixture(),
  appearance: "light" as const,
  commandShortcut: "⌘K",
  onOpenCommand: noop,
  onToggleTheme: noop,
};

export const TopbarDefault: Story = {
  render: () => (
    <TopbarFrame>
      <WorkspaceTopbarSurface
        {...topbarBase}
        breadcrumb={breadcrumbFixture({
          section: "board",
          boardId: "board-roadmap",
          view: "table",
        })}
      />
    </TopbarFrame>
  ),
  tags: ["fui-surface--workspace.topbar", "fui-state--Default"],
};

export const TopbarLoading: Story = {
  render: () => (
    <TopbarFrame>
      <WorkspaceTopbarSurface
        {...topbarBase}
        breadcrumb={breadcrumbFixture(
          { section: "doc", docId: "doc-spec" },
          { loadingNames: true },
        )}
      />
    </TopbarFrame>
  ),
  tags: ["fui-surface--workspace.topbar", "fui-state--Loading"],
};

export const TopbarEdgeData: Story = {
  render: () => {
    const copy = shellCopyFixture("vi");
    return (
      <TopbarFrame>
        <WorkspaceTopbarSurface
          {...topbarBase}
          copy={copy}
          appearance="dark"
          commandShortcut="Ctrl K"
          breadcrumb={breadcrumbFixture(
            { section: "board", boardId: "board-missing", view: "timeline" },
            {
              copy,
              workspaceName: "Không gian làm việc của nhóm Vận hành Toàn cầu",
            },
          )}
        />
      </TopbarFrame>
    );
  },
  tags: ["fui-surface--workspace.topbar", "fui-state--EdgeData"],
};

// ── Command palette (WX-SHELL-003) ────────────────────────────────

const paletteBase = {
  copy: shellCopyFixture(),
  open: true,
  onOpenChange: noop,
  boards: shellBoardsFixture,
  docs: shellDocsFixture,
  members: shellMembersFixture(),
  appearance: "light" as const,
  canCreateWorkspace: true,
  onSelect: noop,
};

export const CommandPaletteDefault: Story = {
  render: () => <WorkspaceCommandPaletteSurface {...paletteBase} />,
  tags: ["fui-surface--workspace.command-palette", "fui-state--Default"],
};

export const CommandPaletteEmpty: Story = {
  render: () => (
    <WorkspaceCommandPaletteSurface
      {...paletteBase}
      boards={[]}
      docs={[]}
      members={[]}
      defaultQuery="quarterly budget"
    />
  ),
  tags: ["fui-surface--workspace.command-palette", "fui-state--Empty"],
};

export const CommandPaletteEdgeData: Story = {
  render: () => (
    <WorkspaceCommandPaletteSurface
      {...paletteBase}
      copy={shellCopyFixture("vi")}
      boards={[
        ...shellBoardsFixture,
        {
          id: "board-long",
          name: "Kế hoạch ra mắt sản phẩm toàn cầu — giai đoạn hai (bản nháp cuối cùng)",
        },
      ]}
      defaultQuery="Kế hoạch"
    />
  ),
  tags: ["fui-surface--workspace.command-palette", "fui-state--EdgeData"],
};

// ── Shortcut sheet (WX-SHELL-005) ─────────────────────────────────

export const ShortcutSheetDefault: Story = {
  render: () => (
    <WorkspaceShortcutSheetSurface
      copy={shellCopyFixture()}
      open
      onOpenChange={noop}
      commandShortcut="⌘K"
    />
  ),
  tags: ["fui-surface--workspace.shortcut-sheet", "fui-state--Default"],
};

// ── Scope states (WX-SHELL-004) ───────────────────────────────────

const scopeBase = {
  copy: shellCopyFixture(),
  onRetry: noop,
  exitHref: "/home",
  onExit: noop,
};

export const ScopeForbidden: Story = {
  render: () => <WorkspaceScopeStateSurface {...scopeBase} state="forbidden" />,
  tags: ["fui-surface--workspace.scope-state", "fui-state--Default"],
};

export const ScopeLoading: Story = {
  render: () => <WorkspaceScopeStateSurface {...scopeBase} state="loading" />,
  tags: ["fui-surface--workspace.scope-state", "fui-state--Loading"],
};

export const ScopeError: Story = {
  render: () => <WorkspaceScopeStateSurface {...scopeBase} state="error" />,
  tags: ["fui-surface--workspace.scope-state", "fui-state--Error"],
};
