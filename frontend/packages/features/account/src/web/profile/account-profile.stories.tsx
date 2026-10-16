import type { Meta, StoryObj } from "@storybook/react";

import {
  profileDefaultScenario,
  profileLoadingScenario,
  profileErrorScenario,
  profileSavedScenario,
  profileEdgeDataScenario,
} from "../../verification/account-profile-fixtures";
import { AccountProfileSurface } from "./account-profile-surface";

const meta: Meta = {
  title: "Account/Profile",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = { onSave: noop, onRetry: noop, onPhotoChange: noop };

export const Default: Story = {
  render: () => (
    <AccountProfileSurface {...profileDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.profile", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => (
    <AccountProfileSurface {...profileLoadingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.profile", "fui-state--Loading"],
};

export const ErrorState: Story = {
  name: "Error",
  render: () => (
    <AccountProfileSurface {...profileErrorScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.profile", "fui-state--Error"],
};

export const Saved: Story = {
  render: () => (
    <AccountProfileSurface {...profileSavedScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.profile", "fui-state--Success"],
};

export const EdgeData: Story = {
  render: () => (
    <AccountProfileSurface {...profileEdgeDataScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.profile", "fui-state--EdgeData"],
};
