import type { Meta, StoryObj } from "@storybook/react";

import {
  settingsDefaultScenario,
  settingsEdgeDataScenario,
  settingsErrorScenario,
  settingsLoadingScenario,
  settingsReadOnlyScenario,
  settingsSuccessScenario,
  settingsVersionUnavailableScenario,
} from "../../verification/workspace-settings-fixtures";
import { WorkspaceSettingsSurface } from "./workspace-settings-surface";

const meta: Meta = {
  title: "Workspace/Settings",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = {
  onSaveProfile: noop,
  onSaveDefaults: noop,
  onArchive: noop,
  onReload: noop,
  onRetryDefaults: noop,
};

export const Default: Story = {
  render: () => (
    <WorkspaceSettingsSurface {...settingsDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.settings", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => (
    <WorkspaceSettingsSurface {...settingsLoadingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.settings", "fui-state--Loading"],
};

export const ErrorState: Story = {
  name: "Error",
  render: () => (
    <WorkspaceSettingsSurface {...settingsErrorScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.settings", "fui-state--Error"],
};

export const Saved: Story = {
  render: () => (
    <WorkspaceSettingsSurface {...settingsSuccessScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.settings", "fui-state--Success"],
};

export const EdgeData: Story = {
  render: () => (
    <WorkspaceSettingsSurface {...settingsEdgeDataScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.settings", "fui-state--EdgeData"],
};

export const ReadOnly: Story = {
  render: () => (
    <WorkspaceSettingsSurface {...settingsReadOnlyScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.settings", "fui-state--ReadOnly"],
};

export const VersionUnavailable: Story = {
  render: () => (
    <WorkspaceSettingsSurface
      {...settingsVersionUnavailableScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--workspace.settings", "fui-state--Unavailable"],
};
