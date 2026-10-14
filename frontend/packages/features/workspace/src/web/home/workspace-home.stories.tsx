import type { Meta, StoryObj } from "@storybook/react";

import {
  homeAllDoneScenario,
  homeCopyFixture,
  homeDefaultScenario,
  homeEdgeDataScenario,
  homeErrorScenario,
  homeLoadingScenario,
  homeOnboardingScenario,
  homeProducedOnlyScenario,
  myWorkDefaultScenario,
  myWorkEdgeDataScenario,
  myWorkEmptyScenario,
  myWorkErrorScenario,
  myWorkHighDensityScenario,
  myWorkLoadingScenario,
} from "../../verification/workspace-home-fixtures";
import { CreateResourceDialog } from "./create-resource-dialog";
import { WorkspaceHomeSurface } from "./workspace-home-surface";
import { WorkspaceMyWorkSurface } from "./workspace-my-work-surface";
import { WorkspaceOnboardingSurface } from "./workspace-onboarding-surface";

const meta: Meta = {
  title: "Workspace/Home",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;

const homeCallbacks = {
  onComplete: noop,
  onAddItem: null,
  onCreateBoard: noop,
  onCreateDoc: noop,
  onInvite: noop,
  onRetryContinue: noop,
  onRetryMyWork: noop,
};

const myWorkCallbacks = { onComplete: noop, onRetry: noop, onLoadMore: noop };

// ── Home (WX-HOME-001) ────────────────────────────────────────────

export const HomeDefault: Story = {
  render: () => (
    <WorkspaceHomeSurface {...homeDefaultScenario()} {...homeCallbacks} />
  ),
  tags: ["fui-surface--workspace.home", "fui-state--Default"],
};

/** Final-Home `xong-viec`: nothing due today, one item overdue. */
export const HomeAllDone: Story = {
  render: () => (
    <WorkspaceHomeSurface {...homeAllDoneScenario()} {...homeCallbacks} />
  ),
  tags: ["fui-surface--workspace.home", "fui-state--Empty"],
};

/** What the host fills today: no replies, schedule, key, Status or Priority. */
export const HomeProducedOnly: Story = {
  render: () => (
    <WorkspaceHomeSurface {...homeProducedOnlyScenario()} {...homeCallbacks} />
  ),
  tags: ["fui-surface--workspace.home", "fui-state--EdgeData"],
};

export const HomeLoading: Story = {
  render: () => (
    <WorkspaceHomeSurface {...homeLoadingScenario()} {...homeCallbacks} />
  ),
  tags: ["fui-surface--workspace.home", "fui-state--Loading"],
};

export const HomeError: Story = {
  render: () => (
    <WorkspaceHomeSurface {...homeErrorScenario()} {...homeCallbacks} />
  ),
  tags: ["fui-surface--workspace.home", "fui-state--Error"],
};

export const HomeEdgeData: Story = {
  render: () => (
    <WorkspaceHomeSurface {...homeEdgeDataScenario()} {...homeCallbacks} />
  ),
  tags: ["fui-surface--workspace.home", "fui-state--EdgeData"],
};

export const HomeOnboarding: Story = {
  render: () => (
    <WorkspaceHomeSurface {...homeOnboardingScenario()} {...homeCallbacks} />
  ),
  tags: ["fui-surface--workspace.home", "fui-state--Empty"],
};

// ── Onboarding (WX-HOME-002) ──────────────────────────────────────

export const OnboardingDefault: Story = {
  render: () => (
    <div className="max-w-3xl p-6">
      <WorkspaceOnboardingSurface
        copy={homeCopyFixture()}
        workspaceName="Product"
        canCreate
        canInvite
        onCreateBoard={noop}
        onCreateDoc={noop}
        onInvite={noop}
      />
    </div>
  ),
  tags: ["fui-surface--workspace.onboarding", "fui-state--Default"],
};

export const OnboardingGuest: Story = {
  render: () => (
    <div className="max-w-3xl p-6">
      <WorkspaceOnboardingSurface
        copy={homeCopyFixture()}
        workspaceName="Product"
        canCreate={false}
        canInvite={false}
        onCreateBoard={noop}
        onCreateDoc={noop}
        onInvite={noop}
      />
    </div>
  ),
  tags: ["fui-surface--workspace.onboarding", "fui-state--PermissionLimited"],
};

// ── Create a Board or Doc ─────────────────────────────────────────

export const CreateResourceDefault: Story = {
  render: () => (
    <CreateResourceDialog
      copy={homeCopyFixture()}
      kind="board"
      pending={false}
      error={null}
      onOpenChange={noop}
      onSubmit={noop}
    />
  ),
  tags: ["fui-surface--workspace.create-resource", "fui-state--Default"],
};

export const CreateResourceLoading: Story = {
  render: () => (
    <CreateResourceDialog
      copy={homeCopyFixture()}
      kind="doc"
      pending
      error={null}
      onOpenChange={noop}
      onSubmit={noop}
    />
  ),
  tags: ["fui-surface--workspace.create-resource", "fui-state--Loading"],
};

export const CreateResourceError: Story = {
  render: () => (
    <CreateResourceDialog
      copy={homeCopyFixture("vi")}
      kind="board"
      pending={false}
      error={homeCopyFixture("vi").create.error}
      onOpenChange={noop}
      onSubmit={noop}
    />
  ),
  tags: ["fui-surface--workspace.create-resource", "fui-state--Error"],
};

// ── My work (WX-HOME-001) ─────────────────────────────────────────

export const MyWorkDefault: Story = {
  render: () => (
    <WorkspaceMyWorkSurface {...myWorkDefaultScenario()} {...myWorkCallbacks} />
  ),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

export const MyWorkLoading: Story = {
  render: () => (
    <WorkspaceMyWorkSurface {...myWorkLoadingScenario()} {...myWorkCallbacks} />
  ),
  tags: ["fui-surface--workspace.my-work", "fui-state--Loading"],
};

export const MyWorkEmpty: Story = {
  render: () => (
    <WorkspaceMyWorkSurface {...myWorkEmptyScenario()} {...myWorkCallbacks} />
  ),
  tags: ["fui-surface--workspace.my-work", "fui-state--Empty"],
};

export const MyWorkError: Story = {
  render: () => (
    <WorkspaceMyWorkSurface {...myWorkErrorScenario()} {...myWorkCallbacks} />
  ),
  tags: ["fui-surface--workspace.my-work", "fui-state--Error"],
};

export const MyWorkEdgeData: Story = {
  render: () => (
    <WorkspaceMyWorkSurface
      {...myWorkEdgeDataScenario()}
      {...myWorkCallbacks}
    />
  ),
  tags: ["fui-surface--workspace.my-work", "fui-state--EdgeData"],
};

export const MyWorkHighDensity: Story = {
  render: () => (
    <WorkspaceMyWorkSurface
      {...myWorkHighDensityScenario()}
      {...myWorkCallbacks}
    />
  ),
  tags: ["fui-surface--workspace.my-work", "fui-state--HighDensity"],
};
