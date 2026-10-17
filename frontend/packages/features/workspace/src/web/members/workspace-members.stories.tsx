import type { Meta, StoryObj } from "@storybook/react";

import {
  membersAdminScenario,
  membersDefaultScenario,
  membersEdgeDataScenario,
  membersErrorScenario,
  membersInvitationsEmptyScenario,
  membersInvitationsScenario,
  membersLoadingScenario,
  membersReadOnlyScenario,
} from "../../verification/workspace-members-fixtures";
import { WorkspaceMembersSurface } from "./workspace-members-surface";

const meta: Meta = {
  title: "Workspace/Members",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = {
  onTabChange: noop,
  onChangeRole: noop,
  onRemove: noop,
  onSuspend: noop,
  onActivate: noop,
  onInvite: noop,
  onRevoke: noop,
  onRetryMembers: noop,
  onRetryInvitations: noop,
};

export const Default: Story = {
  render: () => (
    <WorkspaceMembersSurface {...membersDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.members", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => (
    <WorkspaceMembersSurface {...membersLoadingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.members", "fui-state--Loading"],
};

export const ErrorState: Story = {
  name: "Error",
  render: () => (
    <WorkspaceMembersSurface {...membersErrorScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.members", "fui-state--Error"],
};

export const Invitations: Story = {
  render: () => (
    <WorkspaceMembersSurface {...membersInvitationsScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.members", "fui-state--Success"],
};

export const InvitationsEmpty: Story = {
  render: () => (
    <WorkspaceMembersSurface
      {...membersInvitationsEmptyScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--workspace.members", "fui-state--Empty"],
};

export const EdgeData: Story = {
  render: () => (
    <WorkspaceMembersSurface {...membersEdgeDataScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.members", "fui-state--EdgeData"],
};

export const ReadOnly: Story = {
  render: () => (
    <WorkspaceMembersSurface {...membersReadOnlyScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.members", "fui-state--ReadOnly"],
};

export const PermissionLimited: Story = {
  render: () => (
    <WorkspaceMembersSurface {...membersAdminScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.members", "fui-state--PermissionLimited"],
};
