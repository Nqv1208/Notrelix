import type { Meta, StoryObj } from "@storybook/react";

import {
  commandCommandScenario,
  commandEdgeDataScenario,
  commandNoneScenario,
  commandProducedOnlyScenario,
  commandRecentScenario,
  commandRunningScenario,
  commandSearchScenario,
} from "../../verification/workspace-command-fixtures";
import { WorkspaceCommandPaletteSurface } from "./command-palette-surface";

const meta: Meta = {
  title: "Workspace/Command palette",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;

function renderPalette(scenario: ReturnType<typeof commandSearchScenario>) {
  return (
    <WorkspaceCommandPaletteSurface
      {...scenario}
      onOpenChange={noop}
      onSelect={noop}
    />
  );
}

/** Final-Command `tim-kiem`: "đối chiếu" over Items, Docs, Boards and commands. */
export const PaletteSearch: Story = {
  render: () => renderPalette(commandSearchScenario()),
  tags: ["fui-surface--workspace.command-palette", "fui-state--Default"],
};

/** Final-Command `chua-go`: nothing typed, recent and quick commands. */
export const PaletteRecent: Story = {
  render: () => renderPalette(commandRecentScenario()),
  tags: ["fui-surface--workspace.command-palette", "fui-state--Default"],
};

/** Final-Command `lenh`: ">chuyển". */
export const PaletteCommands: Story = {
  render: () => renderPalette(commandCommandScenario()),
  tags: ["fui-surface--workspace.command-palette", "fui-state--Default"],
};

/** Final-Command `khong-thay`. */
export const PaletteEmpty: Story = {
  render: () => renderPalette(commandNoneScenario()),
  tags: ["fui-surface--workspace.command-palette", "fui-state--Empty"],
};

/** Final-Command `dang-chay`. */
export const PaletteRunning: Story = {
  render: () => renderPalette(commandRunningScenario()),
  tags: ["fui-surface--workspace.command-palette", "fui-state--Default"],
};

/** Only what the member's own lists hold. */
export const PaletteProducedOnly: Story = {
  render: () => renderPalette(commandProducedOnlyScenario()),
  tags: ["fui-surface--workspace.command-palette", "fui-state--EdgeData"],
};

/** Long names and many matches, in the dark theme. */
export const PaletteEdgeData: Story = {
  render: () => renderPalette(commandEdgeDataScenario()),
  tags: ["fui-surface--workspace.command-palette", "fui-state--EdgeData"],
};
