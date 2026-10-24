import type { Meta, StoryObj } from "@storybook/react";

import {
  securityDefaultScenario,
  securityLoadingScenario,
  securityErrorScenario,
  securityNoticeScenario,
  securityEdgeDataScenario,
  securityChangePasswordRefusedScenario,
  securityChangePasswordScenario,
  securityPasswordChangedScenario,
} from "../../verification/account-security-fixtures";
import { AccountSecuritySurface } from "./account-security-surface";

const meta: Meta = {
  title: "Account/Security",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

const noop = () => undefined;
const callbacks = {
  onSetTwoFactor: noop,
  onRenewBackupCodes: noop,
  onSignOut: noop,
  onSignOutOthers: noop,
  onRetry: noop,
};

export const Default: Story = {
  render: () => (
    <AccountSecuritySurface {...securityDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.security", "fui-state--Default"],
};

export const Loading: Story = {
  render: () => (
    <AccountSecuritySurface {...securityLoadingScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.security", "fui-state--Loading"],
};

export const ErrorState: Story = {
  name: "Error",
  render: () => (
    <AccountSecuritySurface {...securityErrorScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.security", "fui-state--Error"],
};

export const Saved: Story = {
  render: () => (
    <AccountSecuritySurface {...securityNoticeScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.security", "fui-state--Success"],
};

export const EdgeData: Story = {
  render: () => (
    <AccountSecuritySurface {...securityEdgeDataScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--account.security", "fui-state--EdgeData"],
};

export const ChangePassword: Story = {
  render: () => (
    <AccountSecuritySurface
      {...securityChangePasswordScenario("en", { needsCode: true })}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.security", "fui-state--EdgeData"],
};

export const ChangePasswordRefused: Story = {
  render: () => (
    <AccountSecuritySurface
      {...securityChangePasswordRefusedScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.security", "fui-state--Error"],
};

export const PasswordChanged: Story = {
  render: () => (
    <AccountSecuritySurface
      {...securityPasswordChangedScenario()}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--account.security", "fui-state--Success"],
};
