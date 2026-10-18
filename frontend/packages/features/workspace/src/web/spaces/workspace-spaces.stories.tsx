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
