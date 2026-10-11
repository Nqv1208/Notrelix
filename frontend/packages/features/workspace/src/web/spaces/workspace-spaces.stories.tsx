import type { Meta, StoryObj } from "@storybook/react";
import type { ReactNode } from "react";
import {
  Sidebar,
  SidebarContent,
  SidebarInset,
  SidebarProvider,
} from "@notrelix/ui-web";

import {
  createSpaceChildScenario,
  createSpaceDefaultScenario,
  createSpaceErrorScenario,
  createSpacePendingScenario,
  moveBoardScenario,
  moveHighDensityScenario,
  movePageScenario,
  moveErrorScenario,
  movePendingScenario,
  moveSpaceScenario,
  spacePageDefaultScenario,
  spacePageEdgeDataScenario,
  spacePageEmptyScenario,
  spacePageErrorScenario,
  spacePageLoadingScenario,
  spacePageMembersScenario,
  spacePageNotFoundScenario,
  spacePagePermissionLimitedScenario,
  spacePageReadOnlyScenario,
  spacePageSettingsScenario,
  spaceTreeDefaultScenario,
  spaceTreeEdgeDataScenario,
  spaceTreeEmptyScenario,
  spaceTreeErrorScenario,
  spaceTreeHighDensityScenario,
  spaceTreeLoadingScenario,
  spaceTreeReadOnlyScenario,
  spacesCopyFixture,
} from "../../verification/workspace-spaces-fixtures";
import { CreateSpaceDialog } from "./create-space-dialog";
import { MoveToSpaceDialog } from "./move-to-space-dialog";
import { SpaceTreeNav } from "./space-tree-nav";
import { WorkspaceSpaceSurface } from "./workspace-space-surface";

const meta: Meta = {
  title: "Workspace/Spaces",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;

function TreeFrame({ children }: { children: ReactNode }) {
  return (
    <SidebarProvider defaultOpenMobile className="bg-surface-canvas">
      <Sidebar mobileTitle="Spaces" mobileDescription="Product">
        <SidebarContent>{children}</SidebarContent>
      </Sidebar>
      <SidebarInset className="bg-surface-canvas">
        <div className="p-6 text-body text-muted-foreground">
          Workspace content
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}

const treeCallbacks = {
  onExpandedChange: noop,
  onCreateSpace: noop,
  onRetry: noop,
};

// ── Sidebar tree ──────────────────────────────────────────────────

export const TreeDefault: Story = {
  render: () => (
    <TreeFrame>
      <SpaceTreeNav {...spaceTreeDefaultScenario()} {...treeCallbacks} />
    </TreeFrame>
  ),
  tags: ["fui-surface--workspace.space-tree", "fui-state--Default"],
};

export const TreeLoading: Story = {
  render: () => (
    <TreeFrame>
      <SpaceTreeNav {...spaceTreeLoadingScenario()} {...treeCallbacks} />
    </TreeFrame>
  ),
  tags: ["fui-surface--workspace.space-tree", "fui-state--Loading"],
};

export const TreeError: Story = {
  render: () => (
    <TreeFrame>
      <SpaceTreeNav {...spaceTreeErrorScenario()} {...treeCallbacks} />
    </TreeFrame>
  ),
  tags: ["fui-surface--workspace.space-tree", "fui-state--Error"],
};

export const TreeEmpty: Story = {
  render: () => (
    <TreeFrame>
      <SpaceTreeNav {...spaceTreeEmptyScenario()} {...treeCallbacks} />
    </TreeFrame>
  ),
  tags: ["fui-surface--workspace.space-tree", "fui-state--Empty"],
};

export const TreeReadOnly: Story = {
  render: () => (
    <TreeFrame>
      <SpaceTreeNav {...spaceTreeReadOnlyScenario()} {...treeCallbacks} />
    </TreeFrame>
  ),
  tags: ["fui-surface--workspace.space-tree", "fui-state--ReadOnly"],
};

export const TreeEdgeData: Story = {
  render: () => (
    <TreeFrame>
      <SpaceTreeNav {...spaceTreeEdgeDataScenario()} {...treeCallbacks} />
    </TreeFrame>
  ),
  tags: ["fui-surface--workspace.space-tree", "fui-state--EdgeData"],
};

export const TreeHighDensity: Story = {
  render: () => (
    <TreeFrame>
      <SpaceTreeNav {...spaceTreeHighDensityScenario()} {...treeCallbacks} />
    </TreeFrame>
  ),
  tags: ["fui-surface--workspace.space-tree", "fui-state--HighDensity"],
};

// ── Space page ────────────────────────────────────────────────────

const pageCallbacks = {
  onTabChange: noop,
  onCreateChild: noop,
  onCreateBoard: noop,
  onCreateDoc: noop,
  onMoveSpace: noop,
  onMoveBoard: noop,
  onMoveDoc: noop,
  onUnarchive: noop,
  onSave: noop,
  onArchive: noop,
  onAddMember: noop,
  onChangeMemberRole: noop,
  onRemoveMember: noop,
  onRetry: noop,
  onRetryContent: noop,
  onRetryMembers: noop,
};

function PageFrame({ children }: { children: ReactNode }) {
  return <div className="min-h-svh bg-surface-canvas">{children}</div>;
}

export const PageDefault: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface
        {...spacePageDefaultScenario()}
        {...pageCallbacks}
      />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--Default"],
};

export const PageMembers: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface
        {...spacePageMembersScenario()}
        {...pageCallbacks}
      />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--Default"],
};

export const PageSettings: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface
        {...spacePageSettingsScenario()}
        {...pageCallbacks}
      />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--Default"],
};

export const PagePermissionLimited: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface
        {...spacePagePermissionLimitedScenario()}
        {...pageCallbacks}
      />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--PermissionLimited"],
};

export const PageLoading: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface
        {...spacePageLoadingScenario()}
        {...pageCallbacks}
      />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--Loading"],
};

export const PageError: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface {...spacePageErrorScenario()} {...pageCallbacks} />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--Error"],
};

export const PageEmpty: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface {...spacePageEmptyScenario()} {...pageCallbacks} />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--Empty"],
};

export const PageReadOnly: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface
        {...spacePageReadOnlyScenario()}
        {...pageCallbacks}
      />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--ReadOnly"],
};

export const PageEdgeData: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface
        {...spacePageEdgeDataScenario()}
        {...pageCallbacks}
      />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--EdgeData"],
};

export const PageNotFound: Story = {
  render: () => (
    <PageFrame>
      <WorkspaceSpaceSurface
        {...spacePageNotFoundScenario()}
        {...pageCallbacks}
      />
    </PageFrame>
  ),
  tags: ["fui-surface--workspace.space-page", "fui-state--Unavailable"],
};

// ── Create a Space ────────────────────────────────────────────────

const dialogCallbacks = { onOpenChange: noop, onSubmit: noop };

export const CreateDefault: Story = {
  render: () => (
    <CreateSpaceDialog {...createSpaceDefaultScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.create-space", "fui-state--Default"],
};

export const CreateLoading: Story = {
  render: () => (
    <CreateSpaceDialog {...createSpacePendingScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.create-space", "fui-state--Loading"],
};

export const CreateError: Story = {
  render: () => (
    <CreateSpaceDialog {...createSpaceErrorScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.create-space", "fui-state--Error"],
};

export const CreateEdgeData: Story = {
  render: () => (
    <CreateSpaceDialog
      {...createSpaceChildScenario()}
      copy={spacesCopyFixture("vi")}
      parent={{
        id: "sp-edge",
        name: "Phòng Kế toán – Tài chính doanh nghiệp và Kiểm soát nội bộ",
      }}
      {...dialogCallbacks}
    />
  ),
  tags: ["fui-surface--workspace.create-space", "fui-state--EdgeData"],
};

// ── Move to a Space ───────────────────────────────────────────────

export const MoveDefault: Story = {
  render: () => (
    <MoveToSpaceDialog {...moveSpaceScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.move-to-space", "fui-state--Default"],
};

export const MoveBoard: Story = {
  render: () => (
    <MoveToSpaceDialog {...moveBoardScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.move-to-space", "fui-state--EdgeData"],
};

export const MovePage: Story = {
  render: () => (
    <MoveToSpaceDialog {...movePageScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.move-to-space", "fui-state--EdgeData"],
};

export const MoveLoading: Story = {
  render: () => (
    <MoveToSpaceDialog {...movePendingScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.move-to-space", "fui-state--Loading"],
};

export const MoveToSpaceHighDensity: Story = {
  render: () => (
    <MoveToSpaceDialog {...moveHighDensityScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.move-to-space", "fui-state--HighDensity"],
};

export const MoveError: Story = {
  render: () => (
    <MoveToSpaceDialog {...moveErrorScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.move-to-space", "fui-state--Error"],
};
