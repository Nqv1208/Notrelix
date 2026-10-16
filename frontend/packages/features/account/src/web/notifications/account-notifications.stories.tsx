import type { Meta, StoryObj } from "@storybook/react";

import {
  notificationsDefaultScenario,
  notificationsLoadingScenario,
  notificationsErrorScenario,
  notificationsSavedScenario,
  notificationsEdgeDataScenario,
} from "../../verification/account-notifications-fixtures";
import { AccountNotificationsSurface } from "./account-notifications-surface";

const meta: Meta = {
  title: "Account/Notifications",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = { onSave: noop, onRetry: noop };

export const Default: Story = {
  render: () => (
    <AccountNotificationsSurface
      {...notificationsDefaultScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.notifications", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => (
    <AccountNotificationsSurface
      {...notificationsLoadingScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.notifications", "fui-state--Loading"],
};

export const ErrorState: Story = {
  name: "Error",
  render: () => (
    <AccountNotificationsSurface
      {...notificationsErrorScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.notifications", "fui-state--Error"],
};

export const Saved: Story = {
  render: () => (
    <AccountNotificationsSurface
      {...notificationsSavedScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.notifications", "fui-state--Success"],
};

export const EdgeData: Story = {
  render: () => (
    <AccountNotificationsSurface
      {...notificationsEdgeDataScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.notifications", "fui-state--EdgeData"],
};
