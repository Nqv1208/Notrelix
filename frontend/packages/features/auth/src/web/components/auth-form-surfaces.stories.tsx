import type { Meta, StoryObj } from "@storybook/react";

import {
  authForgotEmailDefaultScenario,
  authForgotOtpDefaultScenario,
  authForgotSuccessDefaultScenario,
  authLoginDefaultScenario,
  authLoginErrorScenario,
  authLoginPendingScenario,
  authRegisterDefaultScenario,
  authRegisterErrorScenario,
  authRegisterPendingScenario,
} from "../../verification/auth-ui-fixtures";
import {
  AuthForgotPasswordFormSurface,
  AuthLoginFormSurface,
  AuthRegisterFormSurface,
} from "./auth-form-surfaces";

const meta: Meta = {
  title: "Auth/Auth Form Surfaces",
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="flex min-h-screen items-center justify-center bg-background p-6 text-foreground">
        <div className="w-full max-w-md rounded-2xl border border-border bg-card p-8">
          <Story />
        </div>
      </div>
    ),
  ],
};

export default meta;
type Story = StoryObj;

export const LoginDefault: Story = {
  render: () => <AuthLoginFormSurface {...authLoginDefaultScenario()} />,
  tags: ["fui-surface--auth.login", "fui-state--Default"],
};

export const LoginPending: Story = {
  render: () => <AuthLoginFormSurface {...authLoginPendingScenario()} />,
  tags: ["fui-surface--auth.login", "fui-state--Loading"],
};

export const LoginError: Story = {
  render: () => <AuthLoginFormSurface {...authLoginErrorScenario()} />,
  tags: ["fui-surface--auth.login", "fui-state--EdgeData"],
};

export const RegisterDefault: Story = {
  render: () => <AuthRegisterFormSurface {...authRegisterDefaultScenario()} />,
  tags: ["fui-surface--auth.register", "fui-state--Default"],
};

export const RegisterPending: Story = {
  render: () => <AuthRegisterFormSurface {...authRegisterPendingScenario()} />,
  tags: ["fui-surface--auth.register", "fui-state--Loading"],
};

export const RegisterError: Story = {
  render: () => <AuthRegisterFormSurface {...authRegisterErrorScenario()} />,
  tags: ["fui-surface--auth.register", "fui-state--EdgeData"],
};

export const ForgotEmailDefault: Story = {
  render: () => (
    <AuthForgotPasswordFormSurface {...authForgotEmailDefaultScenario()} />
  ),
  tags: ["fui-surface--auth.forgot-password", "fui-state--Default"],
};

export const ForgotOtpDefault: Story = {
  render: () => (
    <AuthForgotPasswordFormSurface {...authForgotOtpDefaultScenario()} />
  ),
  tags: ["fui-surface--auth.forgot-password", "fui-state--EdgeData"],
};

export const ForgotSuccessDefault: Story = {
  render: () => (
    <AuthForgotPasswordFormSurface {...authForgotSuccessDefaultScenario()} />
  ),
  tags: ["fui-surface--auth.forgot-password", "fui-state--ReadOnly"],
};
