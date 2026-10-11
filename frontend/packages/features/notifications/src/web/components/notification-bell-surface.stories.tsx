import type { Meta, StoryObj } from "@storybook/react";

import {
  NOTIFICATIONS_REFERENCE_DATE,
  notificationsCopyFixture,
  notificationsDefaultScenario,
  notificationsEdgeDataScenario,
  notificationsEmptyScenario,
  notificationsFormattersFixture,
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
    referenceDate: NOTIFICATIONS_REFERENCE_DATE,
    copy: notificationsCopyFixture("en"),
    formatters: notificationsFormattersFixture("en"),
  },
  tags: ["fui-surface--notifications.bell", "fui-state--Default"],
};

export const Empty: Story = {
  args: {
    notifications: notificationsEmptyScenario(),
    unreadCount: 0,
    copy: notificationsCopyFixture("en"),
    formatters: notificationsFormattersFixture("en"),
  },
  tags: ["fui-surface--notifications.bell", "fui-state--Empty"],
};

export const EdgeData: Story = {
  args: {
    // Long Vietnamese titles and an unread count past the 99+ cap.
    notifications: notificationsEdgeDataScenario(),
    unreadCount: 100,
    referenceDate: NOTIFICATIONS_REFERENCE_DATE,
    copy: notificationsCopyFixture("vi"),
    formatters: notificationsFormattersFixture("vi"),
  },
  tags: ["fui-surface--notifications.bell", "fui-state--EdgeData"],
};
