import type { ReactNode } from "react";
import type { Meta, StoryObj } from "@storybook/react";

import {
  spacePageArchivedScenario,
  spacePageContentErrorScenario,
  spacePageContentLoadingScenario,
  spacePageCreateMenuScenario,
  spacePageEditorScenario,
  spacePageErrorScenario,
  spacePageLoadingScenario,
  spacePageMembersScenario,
  spacePageNewScenario,
  spacePageNotFoundScenario,
  spacePageOverviewScenario,
  spacePageProducedOnlyScenario,
  spacePageReadOnlyScenario,
  spacePageSettingsReadOnlyScenario,
  spacePageSettingsScenario,
  spacePageViewAsScenario,
  type SpacePageScenario,
} from "../../verification/workspace-space-page-fixtures";
import { WorkspaceSpaceSurface } from "./workspace-space-surface";

const meta: Meta = {
  title: "Workspace/Space page",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;

const callbacks = {
  onCreateChild: noop,
  onCreateBoard: noop,
  onCreateDoc: noop,
  onMoveSpace: noop,
  onUnarchive: noop,
  onSave: noop,
  onArchive: noop,
  onDelete: noop,
  onAddMember: noop,
  onChangeMemberRole: noop,
  onRemoveMember: noop,
  onRetry: noop,
  onRetryContent: noop,
  onRetryMembers: noop,
};

/** The main area is as high as the window; the story gives it the mockup's 1000 px. */
function Stage({ children }: { children: ReactNode }) {
  return <div className="h-[1000px] w-full">{children}</div>;
}

function render(scenario: SpacePageScenario) {
  return (
    <Stage>
      <WorkspaceSpaceSurface {...scenario} {...callbacks} />
    </Stage>
  );
}

/** Final-Space `tong-quan`: running Boards, sub-departments, Docs and the aside. */
export const SpaceOverview: Story = {
  render: () => render(spacePageOverviewScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Default"],
};

/** Final-Space `tao-moi`: the Create menu open over the head. */
export const SpaceCreateMenu: Story = {
  render: () => render(spacePageCreateMenuScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Default"],
};

/** Final-Space `phong-moi`: the first steps and the templates. */
export const SpaceNew: Story = {
  render: () => render(spacePageNewScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Empty"],
};

/** Final-Space `thiet-lap`: settings with the delete confirmation open. */
export const SpaceSettings: Story = {
  render: () => render(spacePageSettingsScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Default"],
};

/** Final-Space `chi-xem`: previewing what a department member sees. */
export const SpaceViewAs: Story = {
  render: () => render(spacePageViewAsScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--PermissionLimited"],
};

export const SpaceEditor: Story = {
  render: () => render(spacePageEditorScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--PermissionLimited"],
};

export const SpaceReadOnly: Story = {
  render: () => render(spacePageReadOnlyScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--ReadOnly"],
};

export const SpaceSettingsReadOnly: Story = {
  render: () => render(spacePageSettingsReadOnlyScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--ReadOnly"],
};

/** What the producers fill today: no progress, attention or people columns. */
export const SpaceProducedOnly: Story = {
  render: () => render(spacePageProducedOnlyScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--EdgeData"],
};

export const SpaceArchived: Story = {
  render: () => render(spacePageArchivedScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--ReadOnly"],
};

export const SpaceLoading: Story = {
  render: () => render(spacePageLoadingScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Loading"],
};

export const SpaceContentLoading: Story = {
  render: () => render(spacePageContentLoadingScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Loading"],
};

export const SpaceError: Story = {
  render: () => render(spacePageErrorScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Error"],
};

export const SpaceContentError: Story = {
  render: () => render(spacePageContentErrorScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Error"],
};

export const SpaceNotFound: Story = {
  render: () => render(spacePageNotFoundScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Unavailable"],
};

/** The Members tab under the new head (the members panel). */
export const SpaceMembers: Story = {
  render: () => render(spacePageMembersScenario()),
  tags: ["fui-surface--workspace.space-page", "fui-state--Default"],
};
