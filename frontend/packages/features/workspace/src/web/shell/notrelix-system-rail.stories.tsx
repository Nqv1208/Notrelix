import type { Meta, StoryObj } from "@storybook/react";
import type { ReactNode } from "react";

import {
  systemRailDrawerScenario,
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
  onOpenAccount: noop,
  onTogglePinned: noop,
  onClose: noop,
};

/**
 * No cross-workspace search producer yet: Overlay, Pinned and Drawer omit
 * `onOpenGlobalSearch` (the entry shows "Chưa khả dụng"); EdgeData passes it.
 */

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
        onOpenGlobalSearch={noop}
      />
    </RailFrame>
  ),
  tags: ["fui-surface--workspace.system-rail", "fui-state--EdgeData"],
};

/** Narrow screens: the rail's workspaces at the top of the navigation sheet. */
export const Drawer: Story = {
  render: () => (
    <div className="flex h-screen w-72 max-w-full flex-col border-r border-border bg-surface-sidebar">
      <NotrelixSystemRailSurface
        {...systemRailDrawerScenario("vi")}
        {...callbacks}
      />
      <div className="flex-1 p-4 text-body text-muted-foreground">
        Workspace navigation
      </div>
    </div>
  ),
  tags: ["fui-surface--workspace.system-rail", "fui-state--Default"],
};
