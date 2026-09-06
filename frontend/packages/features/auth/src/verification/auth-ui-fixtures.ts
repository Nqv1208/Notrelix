import type {
  AuthLoginFormSurfaceProps,
  AuthRegisterFormSurfaceProps,
} from "../web/components/auth-form-surfaces";

export function authLoginDefaultScenario(): AuthLoginFormSurfaceProps {
  return {
    status: "idle",
    serverError: null,
    fieldError: null,
    onSubmit: () => undefined,
    onGoogleSignIn: () => undefined,
    onGithubSignIn: () => undefined,
    onForgotPassword: () => undefined,
    onRegister: () => undefined,
  };
}

export function authLoginPendingScenario(): AuthLoginFormSurfaceProps {
  return {
    ...authLoginDefaultScenario(),
    status: "pending",
  };
}

export function authLoginErrorScenario(): AuthLoginFormSurfaceProps {
  return {
    ...authLoginDefaultScenario(),
    serverError: "Invalid email or password. Please try again.",
    fieldError: "password",
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

export function authForgotEmailDefaultScenario() {
  return {
    step: "email" as const,
    email: "",
    status: "idle" as const,
    serverError: null as string | null,
    onSendCode: () => undefined,
    onResendCode: () => undefined,
    onResetPassword: () => undefined,
    onStartOver: () => undefined,
    onBackToSignIn: () => undefined,
    onSignIn: () => undefined,
  };
}

export function authForgotOtpDefaultScenario() {
  return {
    ...authForgotEmailDefaultScenario(),
    step: "otp" as const,
    email: "owner@notrelix.dev",
  };
}

export function authForgotSuccessDefaultScenario() {
  return {
    ...authForgotEmailDefaultScenario(),
    step: "success" as const,
    email: "owner@notrelix.dev",
  };
}
