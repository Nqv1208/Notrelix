import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  authForgotEmailDefaultScenario,
  authForgotOtpDefaultScenario,
  authLoginDefaultScenario,
  authRegisterDefaultScenario,
} from "../../../verification/auth-ui-fixtures";
import {
  AuthForgotPasswordFormSurface,
  AuthLoginFormSurface,
  AuthRegisterFormSurface,
} from "../auth-form-surfaces";

describe("auth web pure surfaces", () => {
  it("submits login credentials through the injected callback", () => {
    const onSubmit = vi.fn();

    renderPureUi(
      <AuthLoginFormSurface
        {...authLoginDefaultScenario()}
        onSubmit={onSubmit}
      />,
    );

    fireEvent.change(screen.getByLabelText("Email"), {
      target: { value: "owner@notrelix.dev" },
    });
    fireEvent.change(screen.getByLabelText("Password"), {
      target: { value: "secret-password" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    expect(onSubmit).toHaveBeenCalledWith({
      email: "owner@notrelix.dev",
      password: "secret-password",
    });
  });

  it("routes the login navigation callbacks without router providers", () => {
    const onForgotPassword = vi.fn();
    const onRegister = vi.fn();

    renderPureUi(
      <AuthLoginFormSurface
        {...authLoginDefaultScenario()}
        onForgotPassword={onForgotPassword}
        onRegister={onRegister}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Forgot password?" }));
    fireEvent.click(screen.getByRole("button", { name: "Create one" }));

    expect(onForgotPassword).toHaveBeenCalledTimes(1);
    expect(onRegister).toHaveBeenCalledTimes(1);
  });

  it("submits register credentials through the injected callback", () => {
    const onSubmit = vi.fn();

    renderPureUi(
      <AuthRegisterFormSurface
        {...authRegisterDefaultScenario()}
        onSubmit={onSubmit}
      />,
    );

    fireEvent.change(screen.getByLabelText("First name"), {
      target: { value: "Ada" },
    });
    fireEvent.change(screen.getByLabelText("Last name"), {
      target: { value: "Lovelace" },
    });
    fireEvent.change(screen.getByLabelText("Work email"), {
      target: { value: "ada@notrelix.dev" },
    });
    fireEvent.change(screen.getByLabelText("Password"), {
      target: { value: "very-secure" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Create account" }));

    expect(onSubmit).toHaveBeenCalledWith({
      firstName: "Ada",
      lastName: "Lovelace",
      email: "ada@notrelix.dev",
      password: "very-secure",
    });
  });

  it("sends the forgot-password email through the injected callback", () => {
    const onSendCode = vi.fn();

    renderPureUi(
      <AuthForgotPasswordFormSurface
        {...authForgotEmailDefaultScenario()}
        onSendCode={onSendCode}
      />,
    );

    fireEvent.change(screen.getByLabelText("Email address"), {
      target: { value: "owner@notrelix.dev" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: "Send verification code" }),
    );

    expect(onSendCode).toHaveBeenCalledWith("owner@notrelix.dev");
  });

  it("resets the password from the OTP step through the injected callback", () => {
    const onResetPassword = vi.fn();

    renderPureUi(
      <AuthForgotPasswordFormSurface
        {...authForgotOtpDefaultScenario()}
        onResetPassword={onResetPassword}
      />,
    );

    fireEvent.change(screen.getByLabelText("Verification code"), {
      target: { value: "123456" },
    });
    fireEvent.change(screen.getByLabelText("New password"), {
      target: { value: "new-secret" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Reset password" }));

    expect(onResetPassword).toHaveBeenCalledWith({
      code: "123456",
      newPassword: "new-secret",
    });
  });
});
