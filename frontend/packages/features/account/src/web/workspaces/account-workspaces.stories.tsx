import type { Meta, StoryObj } from "@storybook/react";

import {
  workspacesDefaultScenario,
  workspacesLoadingScenario,
  workspacesErrorScenario,
  workspacesNoticeScenario,
  workspacesSoleOwnerScenario,
  workspacesEdgeDataScenario,
} from "../../verification/account-workspaces-fixtures";
import { AccountWorkspacesSurface } from "./account-workspaces-surface";

const meta: Meta = {
  title: "Account/Workspaces",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = { onSetDefault: noop, onLeave: noop, onRetry: noop };

export const Default: Story = {
  render: () => (
    <AccountWorkspacesSurface {...workspacesDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.workspaces", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => (
    <AccountWorkspacesSurface {...workspacesLoadingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.workspaces", "fui-state--Loading"],
};

export const ErrorState: Story = {
  name: "Error",
  render: () => (
    <AccountWorkspacesSurface {...workspacesErrorScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.workspaces", "fui-state--Error"],
};

export const Saved: Story = {
  render: () => (
    <AccountWorkspacesSurface {...workspacesNoticeScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.workspaces", "fui-state--Success"],
};

export const SoleOwner: Story = {
  render: () => (
    <AccountWorkspacesSurface
      {...workspacesSoleOwnerScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.workspaces", "fui-state--PermissionLimited"],
};

export const EdgeData: Story = {
  render: () => (
    <AccountWorkspacesSurface
      {...workspacesEdgeDataScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.workspaces", "fui-state--EdgeData"],
};
