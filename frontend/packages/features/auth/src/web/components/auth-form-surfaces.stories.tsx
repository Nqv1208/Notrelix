import type { Meta, StoryObj } from "@storybook/react";

import {
  AUTH_PERSONAL_EMAIL,
  authForgotScenario,
  authGuestInvite,
  authInviteScenario,
  authRegisterDefaultScenario,
  authRegisterErrorScenario,
  authRegisterPendingScenario,
  authResetScenario,
  authSignInScenario,
  authSsoStep,
  authVerifyEmailScenario,
} from "../../verification/auth-ui-fixtures";
import { authCopy } from "../copy/auth.copy";
import { AuthForgotSurface } from "./auth-forgot-surface";
import { AuthInviteSurface } from "./auth-invite-surface";
import { AuthResetSurface } from "./auth-reset-surface";
import { AuthShell } from "./auth-shell";
import { AuthSignInSurface } from "./auth-sign-in-surface";
import { AuthVerifyEmailSurface } from "./auth-verify-email-surface";
import { AuthRegisterFormSurface } from "./auth-form-surfaces";

/**
 * The sign-in pages inside their frame, at the mockup's 1440 x 900
 * (Final-Auth). The register form is the older single-card layout and keeps
 * its own decorator below.
 */
const meta: Meta = {
  title: "Auth/Auth Form Surfaces",
  parameters: { layout: "fullscreen" },
};

export default meta;
type Story = StoryObj;

function Page({ children }: { children: React.ReactNode }) {
  return (
    <div className="h-225 w-360">
      <AuthShell
        copy={authCopy("vi")}
        locale="vi"
        onLocaleChange={() => undefined}
      >
        {children}
      </AuthShell>
    </div>
  );
}

export const SignInEmail: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface {...authSignInScenario()} />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Default"],
};

export const SignInEmailPending: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface {...authSignInScenario({ pending: true })} />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Loading"],
};

export const SignInEmailInvalid: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface
        {...authSignInScenario({ step: { kind: "email", error: "invalid" } })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Error"],
};

export const SignInPassword: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface
        {...authSignInScenario({
          step: { kind: "password", email: AUTH_PERSONAL_EMAIL },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Default"],
};

export const SignInPasswordWrong: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface
        {...authSignInScenario({
          step: {
            kind: "password",
            email: AUTH_PERSONAL_EMAIL,
            error: "wrong",
          },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Error"],
};

/** Too many wrong passwords: the lock card counts down, the button waits. */
export const SignInLocked: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface
        {...authSignInScenario({
          step: {
            kind: "password",
            email: AUTH_PERSONAL_EMAIL,
            error: null,
            lockedSeconds: 48,
          },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Error"],
};

export const SignInSso: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface {...authSignInScenario({ step: authSsoStep })} />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Default"],
};

export const SignInOtp: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface
        {...authSignInScenario({
          step: { kind: "otp", email: AUTH_PERSONAL_EMAIL },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Default"],
};

export const SignInOtpBackup: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface
        {...authSignInScenario({
          step: { kind: "otp", email: AUTH_PERSONAL_EMAIL, backup: true },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Default"],
};

export const SignInOtpWrong: Story = {
  render: () => (
    <Page>
      <AuthSignInSurface
        {...authSignInScenario({
          step: {
            kind: "otp",
            email: AUTH_PERSONAL_EMAIL,
            error: "wrong",
          },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.sign-in", "fui-state--Error"],
};

export const ForgotAsk: Story = {
  render: () => (
    <Page>
      <AuthForgotSurface {...authForgotScenario()} />
    </Page>
  ),
  tags: ["fui-surface--auth.forgot", "fui-state--Default"],
};

export const ForgotAskPending: Story = {
  render: () => (
    <Page>
      <AuthForgotSurface {...authForgotScenario({ pending: true })} />
    </Page>
  ),
  tags: ["fui-surface--auth.forgot", "fui-state--Loading"],
};

export const ForgotAskInvalid: Story = {
  render: () => (
    <Page>
      <AuthForgotSurface
        {...authForgotScenario({ step: { kind: "ask", error: "invalid" } })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.forgot", "fui-state--Error"],
};

export const ForgotSent: Story = {
  render: () => (
    <Page>
      <AuthForgotSurface
        {...authForgotScenario({
          step: { kind: "sent", email: AUTH_PERSONAL_EMAIL, resendSeconds: 42 },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.forgot", "fui-state--Success"],
};

export const ForgotSentReady: Story = {
  render: () => (
    <Page>
      <AuthForgotSurface
        {...authForgotScenario({
          step: { kind: "sent", email: AUTH_PERSONAL_EMAIL, resendSeconds: 0 },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.forgot", "fui-state--Success"],
};

export const ResetForm: Story = {
  render: () => (
    <Page>
      <AuthResetSurface {...authResetScenario()} />
    </Page>
  ),
  tags: ["fui-surface--auth.reset", "fui-state--Default"],
};

export const ResetPending: Story = {
  render: () => (
    <Page>
      <AuthResetSurface {...authResetScenario({ pending: true })} />
    </Page>
  ),
  tags: ["fui-surface--auth.reset", "fui-state--Loading"],
};

export const ResetRefused: Story = {
  render: () => (
    <Page>
      <AuthResetSurface {...authResetScenario({ error: "rejected" })} />
    </Page>
  ),
  tags: ["fui-surface--auth.reset", "fui-state--Error"],
};

export const VerifyEmailWaiting: Story = {
  render: () => (
    <Page>
      <AuthVerifyEmailSurface {...authVerifyEmailScenario()} />
    </Page>
  ),
  tags: ["fui-surface--auth.verify-email", "fui-state--Default"],
};

export const VerifyEmailConfirming: Story = {
  render: () => (
    <Page>
      <AuthVerifyEmailSurface
        {...authVerifyEmailScenario({ step: { kind: "confirming" } })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.verify-email", "fui-state--Loading"],
};

export const VerifyEmailSending: Story = {
  render: () => (
    <Page>
      <AuthVerifyEmailSurface {...authVerifyEmailScenario({ pending: true })} />
    </Page>
  ),
  tags: ["fui-surface--auth.verify-email", "fui-state--Loading"],
};

export const VerifyEmailInvalid: Story = {
  render: () => (
    <Page>
      <AuthVerifyEmailSurface
        {...authVerifyEmailScenario({ error: "invalid" })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.verify-email", "fui-state--Error"],
};

export const VerifyEmailFailed: Story = {
  render: () => (
    <Page>
      <AuthVerifyEmailSurface
        {...authVerifyEmailScenario({ step: { kind: "failed" } })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.verify-email", "fui-state--Error"],
};

export const VerifyEmailDone: Story = {
  render: () => (
    <Page>
      <AuthVerifyEmailSurface
        {...authVerifyEmailScenario({ step: { kind: "done" } })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.verify-email", "fui-state--Success"],
};

export const InviteDefault: Story = {
  render: () => (
    <Page>
      <AuthInviteSurface {...authInviteScenario()} />
    </Page>
  ),
  tags: ["fui-surface--auth.invite", "fui-state--Default"],
};

export const InvitePending: Story = {
  render: () => (
    <Page>
      <AuthInviteSurface {...authInviteScenario({ pending: "accept" })} />
    </Page>
  ),
  tags: ["fui-surface--auth.invite", "fui-state--Loading"],
};

export const InviteRefused: Story = {
  render: () => (
    <Page>
      <AuthInviteSurface
        {...authInviteScenario({
          view: {
            kind: "invite",
            invite: authGuestInvite,
            fieldErrors: { password: "Dùng ít nhất 8 ký tự." },
          },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.invite", "fui-state--Error"],
};

export const InviteExpired: Story = {
  render: () => (
    <Page>
      <AuthInviteSurface
        {...authInviteScenario({
          view: { kind: "gone", reason: "expired", orgName: "Minh An Group" },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.invite", "fui-state--Unavailable"],
};

export const InviteUsed: Story = {
  render: () => (
    <Page>
      <AuthInviteSurface
        {...authInviteScenario({
          view: { kind: "gone", reason: "used", orgName: "Minh An Group" },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.invite", "fui-state--Unavailable"],
};

export const InviteDeclined: Story = {
  render: () => (
    <Page>
      <AuthInviteSurface
        {...authInviteScenario({
          view: { kind: "declined", inviterName: "Nguyễn Thu Hà" },
        })}
      />
    </Page>
  ),
  tags: ["fui-surface--auth.invite", "fui-state--Success"],
};

const registerFrame = (children: React.ReactNode) => (
  <div className="flex min-h-screen items-center justify-center bg-background p-6 text-foreground">
    <div className="w-full max-w-md rounded-2xl border border-border bg-card p-8">
      {children}
    </div>
  </div>
);

export const RegisterDefault: Story = {
  render: () =>
    registerFrame(
      <AuthRegisterFormSurface {...authRegisterDefaultScenario()} />,
    ),
  tags: ["fui-surface--auth.register", "fui-state--Default"],
};

export const RegisterPending: Story = {
  render: () =>
    registerFrame(
      <AuthRegisterFormSurface {...authRegisterPendingScenario()} />,
    ),
  tags: ["fui-surface--auth.register", "fui-state--Loading"],
};

export const RegisterError: Story = {
  render: () =>
    registerFrame(<AuthRegisterFormSurface {...authRegisterErrorScenario()} />),
  tags: ["fui-surface--auth.register", "fui-state--Error"],
};
