import type { Meta, StoryObj } from "@storybook/react";

import {
  notificationsDefaultScenario,
  notificationsEdgeDataScenario,
  notificationsEmptyScenario,
} from "../../verification/notifications-ui-fixtures";
import { NotificationBellSurface } from "./notification-bell-surface";

const meta: Meta<typeof NotificationBellSurface> = {
  title: "Notifications/Notification Bell",
  component: NotificationBellSurface,
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="min-h-screen bg-background p-12 text-foreground">
        <Story />
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    notifications: notificationsDefaultScenario(),
    unreadCount: 2,
  },
  tags: ["fui-surface--notifications.bell", "fui-state--Default"],
};

export const Empty: Story = {
  args: {
    notifications: notificationsEmptyScenario(),
    unreadCount: 0,
  },
  tags: ["fui-surface--notifications.bell", "fui-state--Empty"],
};

export const EdgeData: Story = {
  args: {
    notifications: notificationsEdgeDataScenario(),
    unreadCount: 2,
  },
  tags: ["fui-surface--notifications.bell", "fui-state--EdgeData"],
};
