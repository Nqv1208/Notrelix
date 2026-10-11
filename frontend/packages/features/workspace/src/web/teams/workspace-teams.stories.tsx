import type { Meta, StoryObj } from "@storybook/react";

import {
  createTeamDefaultScenario,
  createTeamEdgeDataScenario,
  createTeamErrorScenario,
  createTeamPendingScenario,
  teamArchivedScenario,
  teamDefaultScenario,
  teamEdgeDataScenario,
  teamEmptyScenario,
  teamErrorScenario,
  teamHighDensityScenario,
  teamLoadingScenario,
  teamReadOnlyScenario,
  teamsArchivedScenario,
  teamsDefaultScenario,
  teamsEdgeDataScenario,
  teamsEmptyScenario,
  teamsErrorScenario,
  teamsHighDensityScenario,
  teamsLoadingScenario,
  teamsNavigationFixture,
  teamsReadOnlyScenario,
} from "../../verification/workspace-teams-fixtures";
import { CreateTeamDialog } from "./create-team-dialog";
import { WorkspaceTeamSurface } from "./workspace-team-surface";
import { WorkspaceTeamsSurface } from "./workspace-teams-surface";

const meta: Meta = {
  title: "Workspace/Teams",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const navigation = teamsNavigationFixture();
const listCallbacks = {
  navigation,
  onFilterChange: noop,
  onCreate: noop,
  onRetry: noop,
};
const dialogCallbacks = { onOpenChange: noop, onSubmit: noop };
const teamCallbacks = {
  onRename: noop,
  onDescribe: noop,
  onArchive: noop,
  onUnarchive: noop,
  onAddMember: noop,
  onChangeRole: noop,
  onRemove: noop,
  onRetry: noop,
};

// ── Team list ─────────────────────────────────────────────────────

export const ListDefault: Story = {
  render: () => (
    <WorkspaceTeamsSurface {...teamsDefaultScenario()} {...listCallbacks} />
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Default"],
};

export const ListLoading: Story = {
  render: () => (
    <WorkspaceTeamsSurface {...teamsLoadingScenario()} {...listCallbacks} />
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Loading"],
};

export const ListError: Story = {
  render: () => (
    <WorkspaceTeamsSurface {...teamsErrorScenario()} {...listCallbacks} />
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Error"],
};

export const ListEmpty: Story = {
  render: () => (
    <WorkspaceTeamsSurface {...teamsEmptyScenario()} {...listCallbacks} />
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Empty"],
};

export const ListReadOnly: Story = {
  render: () => (
    <WorkspaceTeamsSurface {...teamsReadOnlyScenario()} {...listCallbacks} />
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--ReadOnly"],
};

/** The Archived filter: archived Teams stay listed, read-only elsewhere. */
export const ListArchived: Story = {
  render: () => (
    <WorkspaceTeamsSurface {...teamsArchivedScenario()} {...listCallbacks} />
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Success"],
};

export const ListEdgeData: Story = {
  render: () => (
    <WorkspaceTeamsSurface {...teamsEdgeDataScenario()} {...listCallbacks} />
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--EdgeData"],
};

export const ListHighDensity: Story = {
  render: () => (
    <WorkspaceTeamsSurface {...teamsHighDensityScenario()} {...listCallbacks} />
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--HighDensity"],
};

// ── Create a Team ─────────────────────────────────────────────────

export const CreateDefault: Story = {
  render: () => (
    <CreateTeamDialog {...createTeamDefaultScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.create-team", "fui-state--Default"],
};

export const CreateLoading: Story = {
  render: () => (
    <CreateTeamDialog {...createTeamPendingScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.create-team", "fui-state--Loading"],
};

export const CreateError: Story = {
  render: () => (
    <CreateTeamDialog {...createTeamErrorScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.create-team", "fui-state--Error"],
};

export const CreateEdgeData: Story = {
  render: () => (
    <CreateTeamDialog {...createTeamEdgeDataScenario()} {...dialogCallbacks} />
  ),
  tags: ["fui-surface--workspace.create-team", "fui-state--EdgeData"],
};

// ── Team detail ───────────────────────────────────────────────────

export const TeamDefault: Story = {
  render: () => (
    <WorkspaceTeamSurface {...teamDefaultScenario()} {...teamCallbacks} />
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Default"],
};

export const TeamLoading: Story = {
  render: () => (
    <WorkspaceTeamSurface {...teamLoadingScenario()} {...teamCallbacks} />
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Loading"],
};

export const TeamError: Story = {
  render: () => (
    <WorkspaceTeamSurface {...teamErrorScenario()} {...teamCallbacks} />
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Error"],
};

export const TeamEmpty: Story = {
  render: () => (
    <WorkspaceTeamSurface {...teamEmptyScenario()} {...teamCallbacks} />
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Empty"],
};

export const TeamReadOnly: Story = {
  render: () => (
    <WorkspaceTeamSurface {...teamReadOnlyScenario()} {...teamCallbacks} />
  ),
  tags: ["fui-surface--workspace.team", "fui-state--ReadOnly"],
};

/** An archived Team: membership frozen, reason shown, Unarchive offered. */
export const TeamArchived: Story = {
  render: () => (
    <WorkspaceTeamSurface {...teamArchivedScenario()} {...teamCallbacks} />
  ),
  tags: ["fui-surface--workspace.team", "fui-state--PermissionLimited"],
};

export const TeamEdgeData: Story = {
  render: () => (
    <WorkspaceTeamSurface {...teamEdgeDataScenario()} {...teamCallbacks} />
  ),
  tags: ["fui-surface--workspace.team", "fui-state--EdgeData"],
};

export const TeamHighDensity: Story = {
  render: () => (
    <WorkspaceTeamSurface {...teamHighDensityScenario()} {...teamCallbacks} />
  ),
  tags: ["fui-surface--workspace.team", "fui-state--HighDensity"],
};
