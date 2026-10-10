import type { Meta, StoryObj } from "@storybook/react";

import {
  libraryDefaultScenario,
  libraryEdgeDataScenario,
  libraryEmptyScenario,
  libraryErrorScenario,
  libraryHighDensityScenario,
  libraryLoadingScenario,
  libraryPermissionLimitedScenario,
  libraryReadOnlyScenario,
} from "../../verification/workspace-library-fixtures";
import { WorkspaceLibrarySurface } from "./workspace-library-surface";

const meta: Meta = {
  title: "Workspace/Library",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = {
  onCreateBoard: noop,
  onCreateDoc: noop,
  onSetPinned: noop,
  onRetry: noop,
};

export const Default: Story = {
  render: () => (
    <WorkspaceLibrarySurface {...libraryDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.library", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => (
    <WorkspaceLibrarySurface {...libraryLoadingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.library", "fui-state--Loading"],
};

export const Empty: Story = {
  render: () => (
    <WorkspaceLibrarySurface {...libraryEmptyScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.library", "fui-state--Empty"],
};

export const ErrorState: Story = {
  name: "Error",
  render: () => (
    <WorkspaceLibrarySurface {...libraryErrorScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.library", "fui-state--Error"],
};

export const EdgeData: Story = {
  render: () => (
    <WorkspaceLibrarySurface {...libraryEdgeDataScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.library", "fui-state--EdgeData"],
};

export const HighDensity: Story = {
  render: () => (
    <WorkspaceLibrarySurface {...libraryHighDensityScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.library", "fui-state--HighDensity"],
};

export const ReadOnly: Story = {
  render: () => (
    <WorkspaceLibrarySurface {...libraryReadOnlyScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--workspace.library", "fui-state--ReadOnly"],
};

export const PermissionLimited: Story = {
  render: () => (
    <WorkspaceLibrarySurface
      {...libraryPermissionLimitedScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--workspace.library", "fui-state--PermissionLimited"],
};
