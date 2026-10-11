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

export const InvitationsDefault: Story = {
  render: () => (
    <PendingInvitationsMenuSurface invitations={invitationsDefaultScenario()} />
  ),
  tags: ["fui-surface--workspace.pending-invitations", "fui-state--Default"],
};

export const InvitationsEmpty: Story = {
  render: () => (
    <PendingInvitationsMenuSurface invitations={invitationsEmptyScenario()} />
  ),
  tags: ["fui-surface--workspace.pending-invitations", "fui-state--Empty"],
};
