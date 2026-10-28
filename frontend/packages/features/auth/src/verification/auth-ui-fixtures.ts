import type { AuthRegisterFormSurfaceProps } from "../web/components/auth-form-surfaces";
import type { AuthForgotSurfaceProps } from "../web/components/auth-forgot-surface";
import type {
  AuthInvite,
  AuthInviteSurfaceProps,
} from "../web/components/auth-invite-surface";
import type { AuthSignInSurfaceProps } from "../web/components/auth-sign-in-surface";
import type { AuthVerifyEmailSurfaceProps } from "../web/components/auth-verify-email-surface";
import { authCopy } from "../web/copy/auth.copy";

/**
 * Mockup world of the sign-in pages (Final-Auth), for stories and tests
 * only: a personal account with a password and a second step, a company
 * whose people sign in through Microsoft Entra, a guest invitation to the ERP
 * board. Addresses are the mockup's, none is a real mailbox.
 */
const noop = () => undefined;

export const AUTH_PERSONAL_EMAIL = "thuha.nguyen@gmail.com";
export const AUTH_COMPANY_EMAIL = "thuha.nguyen@minhan.vn";

export function authSignInScenario(
  overrides: Partial<AuthSignInSurfaceProps> = {},
): AuthSignInSurfaceProps {
  return {
    copy: authCopy("vi"),
    step: { kind: "email" },
    onContinue: noop,
    onProvider: noop,
    onCreateWorkspace: noop,
    onChangeEmail: noop,
    onSubmitPassword: noop,
    onForgot: noop,
    onSso: noop,
    onVerify: noop,
    onToggleBackup: noop,
    onBack: noop,
    ...overrides,
  };
}

export const authSsoStep = {
  kind: "sso",
  email: AUTH_COMPANY_EMAIL,
  org: {
    name: "Minh An Group",
    initials: "MA",
    providerLabel: "Microsoft Entra",
    domain: "minhan.vn",
  },
} as const;

export function authForgotScenario(
  overrides: Partial<AuthForgotSurfaceProps> = {},
): AuthForgotSurfaceProps {
  return {
    copy: authCopy("vi"),
    step: { kind: "ask" },
    onSend: noop,
    onResend: noop,
    onBack: noop,
    onOtherEmail: noop,
    ...overrides,
  };
}

export function authVerifyEmailScenario(
  overrides: Partial<AuthVerifyEmailSurfaceProps> = {},
): AuthVerifyEmailSurfaceProps {
  return {
    copy: authCopy("vi"),
    step: { kind: "waiting" },
    onResend: noop,
    onContinue: noop,
    onBack: noop,
    ...overrides,
  };
}

export const authGuestInvite: AuthInvite = {
  inviterName: "Nguyễn Thu Hà",
  inviterInitials: "HA",
  orgName: "Minh An Group",
  boardTitle: "Triển khai ERP Q4",
  level: "editor",
  until: "31/01/2027",
  email: "tuan@doitac-erp.vn",
  name: "Đặng Minh Tuấn",
  hasAccount: false,
};

export function authInviteScenario(
  overrides: Partial<AuthInviteSurfaceProps> = {},
): AuthInviteSurfaceProps {
  return {
    copy: authCopy("vi"),
    view: { kind: "invite", invite: authGuestInvite },
    onAccept: noop,
    onDecline: noop,
    onSignIn: noop,
    onToSignIn: noop,
    ...overrides,
  };
}

export function authRegisterDefaultScenario(): AuthRegisterFormSurfaceProps {
  return {
    status: "idle",
    serverError: null,
    onSubmit: () => undefined,
    onGoogleSignIn: () => undefined,
    onGithubSignIn: () => undefined,
    onSignIn: () => undefined,
  };
}

export function authRegisterPendingScenario(): AuthRegisterFormSurfaceProps {
  return {
    ...authRegisterDefaultScenario(),
    status: "pending",
  };
}

export function authRegisterErrorScenario(): AuthRegisterFormSurfaceProps {
  return {
    ...authRegisterDefaultScenario(),
    serverError: "An account with this email already exists.",
  };
}
