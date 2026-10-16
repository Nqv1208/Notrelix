import type { ReactNode } from "react";
import type { Meta, StoryObj } from "@storybook/react";

import {
  createTeamDefaultScenario,
  createTeamEdgeDataScenario,
  createTeamErrorScenario,
  createTeamPendingScenario,
  securityDefaultScenario,
  securityErrorScenario,
  securityLinksOnScenario,
  securityLoadingScenario,
  securityReadOnlyScenario,
  teamArchivedScenario,
  teamBackNavigationFixture,
  teamDefaultScenario,
  teamEdgeDataScenario,
  teamEmptyScenario,
  teamErrorScenario,
  teamHighDensityScenario,
  teamLoadingScenario,
  teamReadOnlyScenario,
  teamRoleMenuScenario,
  teamUsageNavigationFixture,
  teamsArchiveScenario,
  teamsArchivedScenario,
  teamsDefaultScenario,
  teamsEdgeDataScenario,
  teamsEmptyScenario,
  teamsErrorScenario,
  teamsHighDensityScenario,
  teamsLoadingScenario,
  teamsMenuScenario,
  teamsNavigationFixture,
  teamsReadOnlyScenario,
} from "../../verification/workspace-teams-fixtures";
import { CreateTeamDialog } from "./create-team-dialog";
import { WorkspaceSecuritySurface } from "./workspace-security-surface";
import { WorkspaceTeamSurface } from "./workspace-team-surface";
import { WorkspaceTeamsSurface } from "./workspace-teams-surface";

const meta: Meta = {
  title: "Workspace/Teams",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const listCallbacks = {
  navigation: teamsNavigationFixture(),
  onCreate: noop,
  onArchive: noop,
  onRestore: noop,
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
  backNavigation: teamBackNavigationFixture(),
  usageNavigation: teamUsageNavigationFixture(),
};
const securityCallbacks = { onToggleLinks: noop, onRetry: noop };

/** The main area is as high as the window; the story gives it the mockup's 1000 px. */
function Stage({ children }: { children: ReactNode }) {
  return <div className="h-[1000px] w-full">{children}</div>;
}

// ── Team list ─────────────────────────────────────────────────────

/** Final-Teams `danh-sach`. */
export const ListDefault: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsDefaultScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Default"],
};

export const ListLoading: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsLoadingScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Loading"],
};

export const ListError: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsErrorScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Error"],
};

export const ListEmpty: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsEmptyScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Empty"],
};

/** Final-Teams `khong-co-quyen`: whom to ask. */
export const ListReadOnly: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsReadOnlyScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--ReadOnly"],
};

/** A search that reaches the archived Team. */
export const ListArchived: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsArchivedScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Success"],
};

export const ListEdgeData: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsEdgeDataScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--EdgeData"],
};

export const ListHighDensity: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface
        {...teamsHighDensityScenario()}
        {...listCallbacks}
      />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--HighDensity"],
};

/** Final-Teams `luu-tru`: the archive confirmation of Nhóm Kỹ sư ERP. */
export const ListArchiveConfirm: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsArchiveScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Default"],
};

export const ListRowMenu: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamsSurface {...teamsMenuScenario()} {...listCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.teams", "fui-state--Default"],
};

// ── Create a Team ─────────────────────────────────────────────────

/** Final-Teams `tao-moi`. */
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

/** Final-Teams `chi-tiet`. */
export const TeamDefault: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamDefaultScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Default"],
};

/** The role menu of the only lead: it cannot be lowered or removed. */
export const TeamRoleMenu: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamRoleMenuScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Default"],
};

export const TeamLoading: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamLoadingScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Loading"],
};

export const TeamError: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamErrorScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Error"],
};

export const TeamEmpty: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamEmptyScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--Empty"],
};

export const TeamReadOnly: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamReadOnlyScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--ReadOnly"],
};

/** An archived Team: people frozen, the paused access shown, Restore offered. */
export const TeamArchived: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamArchivedScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--PermissionLimited"],
};

export const TeamEdgeData: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamEdgeDataScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--EdgeData"],
};

export const TeamHighDensity: Story = {
  render: () => (
    <Stage>
      <WorkspaceTeamSurface {...teamHighDensityScenario()} {...teamCallbacks} />
    </Stage>
  ),
  tags: ["fui-surface--workspace.team", "fui-state--HighDensity"],
};

// ── Sharing and security ──────────────────────────────────────────

/** Final-Teams `chia-se`. */
export const SecurityDefault: Story = {
  render: () => (
    <Stage>
      <WorkspaceSecuritySurface
        {...securityDefaultScenario()}
        {...securityCallbacks}
      />
    </Stage>
  ),
  tags: ["fui-surface--workspace.security", "fui-state--Default"],
};

export const SecurityLinksOn: Story = {
  render: () => (
    <Stage>
      <WorkspaceSecuritySurface
        {...securityLinksOnScenario()}
        {...securityCallbacks}
      />
    </Stage>
  ),
  tags: ["fui-surface--workspace.security", "fui-state--Default"],
};

export const SecurityLoading: Story = {
  render: () => (
    <Stage>
      <WorkspaceSecuritySurface
        {...securityLoadingScenario()}
        {...securityCallbacks}
      />
    </Stage>
  ),
  tags: ["fui-surface--workspace.security", "fui-state--Loading"],
};

export const SecurityError: Story = {
  render: () => (
    <Stage>
      <WorkspaceSecuritySurface
        {...securityErrorScenario()}
        {...securityCallbacks}
      />
    </Stage>
  ),
  tags: ["fui-surface--workspace.security", "fui-state--Error"],
};

export const SecurityReadOnly: Story = {
  render: () => (
    <Stage>
      <WorkspaceSecuritySurface
        {...securityReadOnlyScenario()}
        {...securityCallbacks}
      />
    </Stage>
  ),
  tags: ["fui-surface--workspace.security", "fui-state--ReadOnly"],
};
