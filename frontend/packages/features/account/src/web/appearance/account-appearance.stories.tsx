import type { Meta, StoryObj } from "@storybook/react";

import {
  appearanceDefaultScenario,
  appearanceEdgeDataScenario,
  appearanceErrorScenario,
  appearanceLoadingScenario,
  appearanceSavedScenario,
} from "../../verification/account-appearance-fixtures";
import { AccountAppearanceSurface } from "./account-appearance-surface";

const meta: Meta = {
  title: "Account/Appearance",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = {
  onModeChange: noop,
  onAccentChange: noop,
  onLocaleChange: noop,
  onSidebarCollapsedChange: noop,
  onRetryPreferences: noop,
};

export const Default: Story = {
  render: () => (
    <AccountAppearanceSurface {...appearanceDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.appearance", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => (
    <AccountAppearanceSurface {...appearanceLoadingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.appearance", "fui-state--Loading"],
};

export const ErrorState: Story = {
  name: "Error",
  render: () => (
    <AccountAppearanceSurface {...appearanceErrorScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.appearance", "fui-state--Error"],
};

export const Saved: Story = {
  render: () => (
    <AccountAppearanceSurface {...appearanceSavedScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.appearance", "fui-state--Success"],
};

export const EdgeData: Story = {
  render: () => (
    <AccountAppearanceSurface
      {...appearanceEdgeDataScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.appearance", "fui-state--EdgeData"],
};
