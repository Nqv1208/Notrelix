import type { Meta, StoryObj } from "@storybook/react";
import type { ReactNode } from "react";

import {
  systemRailEdgeDataScenario,
  systemRailOverlayScenario,
  systemRailPinnedScenario,
} from "../../verification/workspace-system-rail-fixtures";
import { NotrelixSystemRailSurface } from "./notrelix-system-rail-surface";

const meta: Meta = {
  title: "Workspace/System rail",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = {
  onSwitchWorkspace: noop,
  onCreateWorkspace: noop,
  onOpenAllInbox: noop,
  onOpenGlobalSearch: noop,
  onOpenAccount: noop,
  onTogglePinned: noop,
  onClose: noop,
};

/** A neutral sidebar and canvas so the rail's overlay and push are visible. */
function RailFrame({ children }: { readonly children: ReactNode }) {
  return (
    <div className="flex h-screen bg-surface-canvas">
      {children}
      <div className="w-64 shrink-0 border-r border-border bg-surface-sidebar" />
      <div className="flex-1 p-6 text-body text-muted-foreground">
        Workspace content
      </div>
    </div>
  );
}

export const Overlay: Story = {
  render: () => (
    <RailFrame>
      <NotrelixSystemRailSurface
        {...systemRailOverlayScenario("vi")}
        {...callbacks}
      />
    </RailFrame>
  ),
  tags: ["fui-surface--workspace.system-rail", "fui-state--Default"],
};

export const Pinned: Story = {
  render: () => (
    <RailFrame>
      <NotrelixSystemRailSurface
        {...systemRailPinnedScenario("vi")}
        {...callbacks}
      />
    </RailFrame>
  ),
  tags: ["fui-surface--workspace.system-rail", "fui-state--Default"],
};

export const EdgeData: Story = {
  render: () => (
    <RailFrame>
      <NotrelixSystemRailSurface
        {...systemRailEdgeDataScenario("vi")}
        {...callbacks}
      />
    </RailFrame>
  ),
  tags: ["fui-surface--workspace.system-rail", "fui-state--EdgeData"],
};
