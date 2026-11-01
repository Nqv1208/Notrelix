import type { ReactNode } from "react";
import type { Meta, StoryObj } from "@storybook/react";

import {
  invitationsDefaultScenario,
  invitationsEmptyScenario,
} from "../../verification/workspace-ui-fixtures";
import { PendingInvitationsMenuSurface } from "./workspace-ui-surfaces";

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

/** The invitations entry lives in the dark system rail: its light label needs that ground to be legible. */
function OnRail({ children }: { children: ReactNode }) {
  return (
    <div className="inline-flex bg-rail text-rail-foreground [--focus-ring:var(--rail-focus-ring)]">
      {children}
    </div>
  );
}

export const InvitationsDefault: Story = {
  render: () => (
    <OnRail>
      <PendingInvitationsMenuSurface
        invitations={invitationsDefaultScenario()}
      />
    </OnRail>
  ),
  tags: ["fui-surface--workspace.pending-invitations", "fui-state--Default"],
};

export const InvitationsEmpty: Story = {
  render: () => (
    <OnRail>
      <PendingInvitationsMenuSurface invitations={invitationsEmptyScenario()} />
    </OnRail>
  ),
  tags: ["fui-surface--workspace.pending-invitations", "fui-state--Empty"],
};
