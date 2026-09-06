import type { Meta, StoryObj } from "@storybook/react";

import {
  dashboardDefaultScenario,
  dashboardEmptyScenario,
  dashboardLoadingScenario,
  invitationsDefaultScenario,
  invitationsEmptyScenario,
  workspaceDirectoryDefaultScenario,
  workspaceDirectoryEdgeDataScenario,
  workspaceDirectoryEmptyScenario,
  workspaceHeaderDefaultScenario,
  workspaceTabsDefaultScenario,
  workspaceTabsEdgeDataScenario,
  workspaceTabsEmptyScenario,
} from "../../verification/workspace-ui-fixtures";
import {
  PendingInvitationsMenuSurface,
  WorkspaceCompactHeaderSurface,
  WorkspaceDashboardSurface,
  WorkspaceDirectorySurface,
  WorkspaceViewTabsSurface,
} from "./workspace-ui-surfaces";

const meta: Meta = {
  title: "Workspace/Workspace UI Surfaces",
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="min-h-screen bg-background p-6 text-foreground">
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj;

export const DirectoryDefault: Story = {
  render: () => (
    <WorkspaceDirectorySurface
      workspaces={workspaceDirectoryDefaultScenario()}
    />
  ),
  tags: ["fui-surface--workspace.directory", "fui-state--Default"],
};

export const DirectoryEmpty: Story = {
  render: () => (
    <WorkspaceDirectorySurface workspaces={workspaceDirectoryEmptyScenario()} />
  ),
  tags: ["fui-surface--workspace.directory", "fui-state--Empty"],
};

export const DirectoryEdgeData: Story = {
  render: () => (
    <WorkspaceDirectorySurface
      workspaces={workspaceDirectoryEdgeDataScenario()}
    />
  ),
  tags: ["fui-surface--workspace.directory", "fui-state--EdgeData"],
};

export const CompactHeaderDefault: Story = {
  render: () => (
    <WorkspaceCompactHeaderSurface {...workspaceHeaderDefaultScenario()} />
  ),
  tags: ["fui-surface--workspace.compact-header", "fui-state--Default"],
};

export const ViewTabsDefault: Story = {
  render: () => (
    <WorkspaceViewTabsSurface
      workspaceId="ws-main"
      views={workspaceTabsDefaultScenario()}
      activeViewId="view-board"
    />
  ),
  tags: ["fui-surface--workspace.view-tabs", "fui-state--Default"],
};

export const ViewTabsEmpty: Story = {
  render: () => (
    <WorkspaceViewTabsSurface
      workspaceId="ws-main"
      views={workspaceTabsEmptyScenario()}
    />
  ),
  tags: ["fui-surface--workspace.view-tabs", "fui-state--Empty"],
};

export const ViewTabsEdgeData: Story = {
  render: () => (
    <WorkspaceViewTabsSurface
      workspaceId="ws-enterprise"
      views={workspaceTabsEdgeDataScenario()}
    />
  ),
  tags: ["fui-surface--workspace.view-tabs", "fui-state--EdgeData"],
};

export const InvitationsDefault: Story = {
  render: () => (
    <PendingInvitationsMenuSurface invitations={invitationsDefaultScenario()} />
  ),
  tags: ["fui-surface--workspace.pending-invitations", "fui-state--Default"],
};

export const InvitationsEmpty: Story = {
  render: () => (
    <PendingInvitationsMenuSurface invitations={invitationsEmptyScenario()} />
  ),
  tags: ["fui-surface--workspace.pending-invitations", "fui-state--Empty"],
};

export const DashboardDefault: Story = {
  render: () => <WorkspaceDashboardSurface {...dashboardDefaultScenario()} />,
  tags: ["fui-surface--workspace.dashboard", "fui-state--Default"],
};

export const DashboardEmpty: Story = {
  render: () => <WorkspaceDashboardSurface {...dashboardEmptyScenario()} />,
  tags: ["fui-surface--workspace.dashboard", "fui-state--Empty"],
};

export const DashboardLoading: Story = {
  render: () => <WorkspaceDashboardSurface {...dashboardLoadingScenario()} />,
  tags: ["fui-surface--workspace.dashboard", "fui-state--Loading"],
};
