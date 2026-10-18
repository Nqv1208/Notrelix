import type { ReactNode } from "react";
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
  myWorkByBoardScenario,
  myWorkCreateScenario,
  myWorkDefaultScenario,
  myWorkDetailScenario,
  myWorkDoneScenario,
  myWorkEdgeDataScenario,
  myWorkEmptyScenario,
  myWorkErrorScenario,
  myWorkGroupMenuScenario,
  myWorkHighDensityScenario,
  myWorkLoadingScenario,
  myWorkProducedOnlyScenario,
  myWorkSelectingScenario,
  myWorkStatusPopScenario,
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

const myWorkCallbacks = {
  onComplete: noop,
  onCreate: null,
  onOpenItem: noop,
  onLoadMore: noop,
  onRetry: noop,
  onChangeStatus: noop,
};

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

// ── My work (Final-MyWork) ────────────────────────────────────────

/** The page is as tall as the window; the story gives it the mockup's 900 px. */
function Stage({ children }: { children: ReactNode }) {
  return <div className="h-[900px] w-full">{children}</div>;
}

/** A stand-in for the item sheet the host places over the right edge. */
const SheetStandIn = (
  <aside
    aria-label="Item sheet"
    className="absolute inset-y-0 right-0 z-sticky w-[560px] border-l border-line bg-popover shadow-sheet"
  />
);

function renderMyWork(scenario: ReturnType<typeof myWorkDefaultScenario>) {
  return (
    <Stage>
      <WorkspaceMyWorkSurface {...scenario} {...myWorkCallbacks} />
    </Stage>
  );
}

export const MyWorkDefault: Story = {
  render: () => renderMyWork(myWorkDefaultScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

export const MyWorkLoading: Story = {
  render: () => renderMyWork(myWorkLoadingScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Loading"],
};

export const MyWorkEmpty: Story = {
  render: () => renderMyWork(myWorkEmptyScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Empty"],
};

export const MyWorkError: Story = {
  render: () => renderMyWork(myWorkErrorScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Error"],
};

export const MyWorkEdgeData: Story = {
  render: () => renderMyWork(myWorkEdgeDataScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--EdgeData"],
};

export const MyWorkHighDensity: Story = {
  render: () => renderMyWork(myWorkHighDensityScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--HighDensity"],
};

/** Final-MyWork `theo-board`. */
export const MyWorkByBoard: Story = {
  render: () => renderMyWork(myWorkByBoardScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

/** Final-MyWork `chon-nhieu`: two rows selected, the bulk bar over the list. */
export const MyWorkSelecting: Story = {
  render: () => renderMyWork(myWorkSelectingScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

/** Final-MyWork `da-xong`. */
export const MyWorkDone: Story = {
  render: () => renderMyWork(myWorkDoneScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

/** Final-MyWork `chi-tiet`: the list narrows to make room for the sheet. */
export const MyWorkDetail: Story = {
  render: () => (
    <Stage>
      <WorkspaceMyWorkSurface
        {...myWorkDetailScenario()}
        {...myWorkCallbacks}
        sheet={SheetStandIn}
      />
    </Stage>
  ),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

/** Final-MyWork `chon-nhom`. */
export const MyWorkGroupMenu: Story = {
  render: () => renderMyWork(myWorkGroupMenuScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

/** Final-MyWork `doi-trang-thai`. */
export const MyWorkStatusPop: Story = {
  render: () => renderMyWork(myWorkStatusPopScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

/** Final-MyWork `them-viec`. */
export const MyWorkCreate: Story = {
  render: () => renderMyWork(myWorkCreateScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--Default"],
};

/** What the host fills today: no key, Status, Priority or person; inert tabs. */
export const MyWorkProducedOnly: Story = {
  render: () => renderMyWork(myWorkProducedOnlyScenario()),
  tags: ["fui-surface--workspace.my-work", "fui-state--EdgeData"],
};
