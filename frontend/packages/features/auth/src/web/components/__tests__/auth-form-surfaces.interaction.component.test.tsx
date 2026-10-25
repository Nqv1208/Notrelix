import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  AUTH_PERSONAL_EMAIL,
  authForgotScenario,
  authInviteScenario,
  authRegisterDefaultScenario,
  authSignInScenario,
  authSsoStep,
  authVerifyEmailScenario,
} from "../../../verification/auth-ui-fixtures";
import { PASSWORD_MIN_LENGTH } from "../../../core/schemas/password-policy";
import { authCopy } from "../../copy/auth.copy";
import { fillAuthCopy } from "../../copy/fill-auth-copy";
import { AuthForgotSurface } from "../auth-forgot-surface";
import { AuthInviteSurface } from "../auth-invite-surface";
import { AuthRegisterFormSurface } from "../auth-form-surfaces";
import { AuthShell } from "../auth-shell";
import { AuthSignInSurface } from "../auth-sign-in-surface";
import { AuthVerifyEmailSurface } from "../auth-verify-email-surface";

const copy = authCopy("vi");

describe("auth sign-in surface", () => {
  it("FUI[auth.sign-in:email] asks for the email, rejects a malformed one and continues with a good one", () => {
    const onContinue = vi.fn();
    renderPureUi(<AuthSignInSurface {...authSignInScenario({ onContinue })} />);
    const field = screen.getByLabelText(copy.email.label);
    fireEvent.change(field, { target: { value: "khong-phai-email" } });
    fireEvent.click(screen.getByRole("button", { name: copy.email.continue }));
    expect(onContinue).not.toHaveBeenCalled();
    expect(screen.getByRole("alert").textContent).toContain(copy.email.invalid);
    fireEvent.change(field, { target: { value: "  ten@congty.vn " } });
    fireEvent.click(screen.getByRole("button", { name: copy.email.continue }));
    expect(onContinue).toHaveBeenCalledWith("ten@congty.vn");
  });

  it("FUI[auth.sign-in:providers] offers Google and Microsoft and a way to create a workspace", () => {
    const onProvider = vi.fn();
    const onCreateWorkspace = vi.fn();
    renderPureUi(
      <AuthSignInSurface
        {...authSignInScenario({ onProvider, onCreateWorkspace })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.email.google }));
    fireEvent.click(screen.getByRole("button", { name: copy.email.microsoft }));
    expect(onProvider.mock.calls).toEqual([["google"], ["microsoft"]]);
    fireEvent.click(
      screen.getByRole("button", { name: copy.email.createWorkspace }),
    );
    expect(onCreateWorkspace).toHaveBeenCalledTimes(1);
  });

  it("FUI[auth.sign-in:pending] keeps the form from being sent twice while it waits", () => {
    renderPureUi(
      <AuthSignInSurface {...authSignInScenario({ pending: true })} />,
    );
    expect(
      screen
        .getByRole("button", { name: copy.email.checking })
        .hasAttribute("disabled"),
    ).toBe(true);
    expect(
      screen
        .getByRole("button", { name: copy.email.google })
        .hasAttribute("disabled"),
    ).toBe(true);
  });

  it("FUI[auth.sign-in:password] sends the password, shows it on request and goes back to change the email or sign in another way", () => {
    const onSubmitPassword = vi.fn();
    const onChangeEmail = vi.fn();
    const onForgot = vi.fn();
    const onBack = vi.fn();
    renderPureUi(
      <AuthSignInSurface
        {...authSignInScenario({
          step: { kind: "password", email: AUTH_PERSONAL_EMAIL },
          onSubmitPassword,
          onChangeEmail,
          onForgot,
          onBack,
        })}
      />,
    );
    expect(screen.getByText(AUTH_PERSONAL_EMAIL)).toBeTruthy();
    const field = screen.getByLabelText(
      copy.password.label,
    ) as HTMLInputElement;
    expect(field.type).toBe("password");
    fireEvent.click(screen.getByRole("button", { name: copy.password.show }));
    expect(field.type).toBe("text");
    fireEvent.change(field, { target: { value: "mat-khau-cua-toi" } });
    fireEvent.click(screen.getByRole("button", { name: copy.password.signIn }));
    expect(onSubmitPassword).toHaveBeenCalledWith("mat-khau-cua-toi");
    fireEvent.click(
      screen.getByRole("button", { name: copy.password.changeLabel }),
    );
    fireEvent.click(screen.getByRole("button", { name: copy.password.forgot }));
    fireEvent.click(screen.getByRole("button", { name: copy.password.other }));
    expect(onChangeEmail).toHaveBeenCalledTimes(1);
    expect(onForgot).toHaveBeenCalledTimes(1);
    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it("FUI[auth.sign-in:wrong] says the password is wrong and never counts tries", () => {
    renderPureUi(
      <AuthSignInSurface
        {...authSignInScenario({
          step: {
            kind: "password",
            email: AUTH_PERSONAL_EMAIL,
            error: "wrong",
          },
        })}
      />,
    );
    expect(screen.getByRole("alert").textContent).toContain(
      copy.password.wrong,
    );
    expect(screen.queryByText(/lần thử/)).toBeNull();
    expect(
      screen.getByLabelText(copy.password.label).getAttribute("aria-invalid"),
    ).toBe("true");
  });

  it("FUI[auth.sign-in:locked] shows the lock card and a button that waits, with the time left", () => {
    const onSubmitPassword = vi.fn();
    renderPureUi(
      <AuthSignInSurface
        {...authSignInScenario({
          step: {
            kind: "password",
            email: AUTH_PERSONAL_EMAIL,
            error: null,
            lockedSeconds: 48,
          },
          onSubmitPassword,
        })}
      />,
    );
    expect(screen.getByText("Thử lại sau 48 giây")).toBeTruthy();
    const button = screen.getByRole("button", {
      name: /Đăng nhập, mở lại sau 48 giây/,
    });
    expect(button.hasAttribute("disabled")).toBe(true);
    expect(button.textContent).toContain("0:48");
    fireEvent.submit(button.closest("form") as HTMLFormElement);
    expect(onSubmitPassword).not.toHaveBeenCalled();
  });

  it("FUI[auth.sign-in:sso] names the company and its provider and goes on to the provider", () => {
    const onSso = vi.fn();
    const onChangeEmail = vi.fn();
    renderPureUi(
      <AuthSignInSurface
        {...authSignInScenario({ step: authSsoStep, onSso, onChangeEmail })}
      />,
    );
    expect(screen.getByRole("heading", { level: 1 }).textContent).toContain(
      "Minh An Group dùng đăng nhập một lần qua Microsoft Entra",
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Tiếp tục với Microsoft Entra" }),
    );
    fireEvent.click(screen.getByRole("button", { name: copy.sso.other }));
    expect(onSso).toHaveBeenCalledTimes(1);
    expect(onChangeEmail).toHaveBeenCalledTimes(1);
  });

  it("FUI[auth.sign-in:otp] takes six digits one by one or pasted, backs up with Backspace and verifies", () => {
    const onVerify = vi.fn();
    renderPureUi(
      <AuthSignInSurface
        {...authSignInScenario({
          step: { kind: "otp", email: AUTH_PERSONAL_EMAIL },
          onVerify,
        })}
      />,
    );
    const verify = screen.getByRole("button", { name: copy.otp.verify });
    expect(verify.hasAttribute("disabled")).toBe(true);
    const box = (index: number) =>
      screen.getByLabelText(`Số ${index} trên 6`) as HTMLInputElement;
    fireEvent.change(box(1), { target: { value: "4" } });
    expect(document.activeElement).toBe(box(2));
    fireEvent.keyDown(box(2), { key: "Backspace" });
    expect(box(1).value).toBe("");
    // Pasting six digits into the first box fills them all.
    fireEvent.paste(box(1), {
      clipboardData: { getData: () => "482 931" },
    });
    expect([1, 2, 3, 4, 5, 6].map((index) => box(index).value).join("")).toBe(
      "482931",
    );
    fireEvent.click(verify);
    expect(onVerify).toHaveBeenCalledWith({ code: "482931" });
  });

  it("FUI[auth.sign-in:otp-backup] asks for a backup code instead and says when a code is wrong", () => {
    const onVerify = vi.fn();
    const onToggleBackup = vi.fn();
    renderPureUi(
      <AuthSignInSurface
        {...authSignInScenario({
          step: {
            kind: "otp",
            email: AUTH_PERSONAL_EMAIL,
            backup: true,
            error: "wrong",
          },
          onVerify,
          onToggleBackup,
        })}
      />,
    );
    fireEvent.change(screen.getByLabelText(copy.otp.backupLegend), {
      target: { value: "BK7Q-M2XN-91TD" },
    });
    fireEvent.click(screen.getByRole("button", { name: copy.otp.verify }));
    expect(onVerify).toHaveBeenCalledWith({ code: "BK7Q-M2XN-91TD" });
    expect(screen.getByRole("alert").textContent).toContain(copy.otp.wrong);
    fireEvent.click(screen.getByRole("button", { name: copy.otp.useApp }));
    expect(onToggleBackup).toHaveBeenCalledTimes(1);
  });
});

describe("auth forgot-password surface", () => {
  it("FUI[auth.forgot:ask] asks for the email, rejects a malformed one and sends a good one", () => {
    const onSend = vi.fn();
    const onBack = vi.fn();
    renderPureUi(
      <AuthForgotSurface {...authForgotScenario({ onSend, onBack })} />,
    );
    const field = screen.getByLabelText(copy.forgot.label);
    fireEvent.change(field, { target: { value: "sai" } });
    fireEvent.click(screen.getByRole("button", { name: copy.forgot.send }));
    expect(onSend).not.toHaveBeenCalled();
    expect(screen.getByRole("alert").textContent).toContain(
      copy.forgot.invalid,
    );
    fireEvent.change(field, { target: { value: AUTH_PERSONAL_EMAIL } });
    fireEvent.click(screen.getByRole("button", { name: copy.forgot.send }));
    expect(onSend).toHaveBeenCalledWith(AUTH_PERSONAL_EMAIL);
    fireEvent.click(screen.getByRole("button", { name: copy.forgot.back }));
    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it("FUI[auth.forgot:sent] never says whether the email has an account, waits before sending again and then lets it", () => {
    const onResend = vi.fn();
    const { rerender } = renderPureUi(
      <AuthForgotSurface
        {...authForgotScenario({
          step: { kind: "sent", email: AUTH_PERSONAL_EMAIL, resendSeconds: 42 },
          onResend,
        })}
      />,
    );
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(
      copy.sent.title,
    );
    expect(screen.getByText(/Nếu email có tài khoản/)).toBeTruthy();
    const wait = screen.getByRole("button", {
      name: /Gửi lại, dùng được sau 42 giây/,
    });
    expect(wait.hasAttribute("disabled")).toBe(true);
    expect(wait.textContent).toContain("0:42");
    rerender(
      <AuthForgotSurface
        {...authForgotScenario({
          step: { kind: "sent", email: AUTH_PERSONAL_EMAIL, resendSeconds: 0 },
          onResend,
        })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.sent.resend }));
    expect(onResend).toHaveBeenCalledTimes(1);
  });
});

describe("auth verify-email surface", () => {
  it("FUI[auth.verify-email:confirm] says it is confirming, then that the email is confirmed and continues", () => {
    const onContinue = vi.fn();
    const { rerender } = renderPureUi(
      <AuthVerifyEmailSurface
        {...authVerifyEmailScenario({ step: { kind: "confirming" } })}
      />,
    );
    expect(screen.getByRole("status").textContent).toContain(
      copy.verifyEmail.confirmingTitle,
    );
    rerender(
      <AuthVerifyEmailSurface
        {...authVerifyEmailScenario({ step: { kind: "done" }, onContinue })}
      />,
    );
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(
      copy.verifyEmail.doneTitle,
    );
    fireEvent.click(
      screen.getByRole("button", { name: copy.verifyEmail.continue }),
    );
    expect(onContinue).toHaveBeenCalledTimes(1);
  });

  it("FUI[auth.verify-email:resend] offers a new link for a dead link, rejects a malformed email, sends a good one and goes back", () => {
    const onResend = vi.fn();
    const onBack = vi.fn();
    renderPureUi(
      <AuthVerifyEmailSurface
        {...authVerifyEmailScenario({
          step: { kind: "failed" },
          onResend,
          onBack,
        })}
      />,
    );
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(
      copy.verifyEmail.failedTitle,
    );
    const field = screen.getByLabelText(copy.verifyEmail.label);
    fireEvent.change(field, { target: { value: "sai" } });
    fireEvent.click(
      screen.getByRole("button", { name: copy.verifyEmail.send }),
    );
    expect(onResend).not.toHaveBeenCalled();
    expect(screen.getByRole("alert").textContent).toContain(
      copy.verifyEmail.invalid,
    );
    fireEvent.change(field, { target: { value: AUTH_PERSONAL_EMAIL } });
    fireEvent.click(
      screen.getByRole("button", { name: copy.verifyEmail.send }),
    );
    expect(onResend).toHaveBeenCalledWith(AUTH_PERSONAL_EMAIL);
    fireEvent.click(
      screen.getByRole("button", { name: copy.verifyEmail.back }),
    );
    expect(onBack).toHaveBeenCalledTimes(1);
  });
});

describe("auth invite surface", () => {
  it("FUI[auth.invite:accept] shows who invited, the guest level and its end, keeps the email fixed and checks the name and password", () => {
    const onAccept = vi.fn();
    renderPureUi(<AuthInviteSurface {...authInviteScenario({ onAccept })} />);
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(
      "Nguyễn Thu Hà mời bạn vào board Triển khai ERP Q4 của Minh An Group",
    );
    expect(
      screen.getByText(/Khách · Có thể sửa · đến 31\/01\/2027/),
    ).toBeTruthy();
    const email = screen.getByLabelText(
      copy.invite.emailLabel,
    ) as HTMLInputElement;
    expect(email.readOnly).toBe(true);
    const accept = screen.getByRole("button", { name: copy.invite.accept });
    fireEvent.change(screen.getByLabelText(copy.invite.passwordLabel), {
      target: { value: "ngan" },
    });
    fireEvent.click(accept);
    expect(onAccept).not.toHaveBeenCalled();
    expect(
      screen.getByText(
        fillAuthCopy(copy.invite.passwordShort, { count: PASSWORD_MIN_LENGTH }),
      ),
    ).toBeTruthy();
    // The minimum is the backend policy (8): seven characters are refused, eight pass.
    fireEvent.change(screen.getByLabelText(copy.invite.passwordLabel), {
      target: { value: "x".repeat(PASSWORD_MIN_LENGTH - 1) },
    });
    fireEvent.click(accept);
    expect(onAccept).not.toHaveBeenCalled();
    fireEvent.change(screen.getByLabelText(copy.invite.passwordLabel), {
      target: { value: "mot-mat-khau-du-dai" },
    });
    fireEvent.click(accept);
    expect(onAccept).toHaveBeenCalledWith({
      name: "Đặng Minh Tuấn",
      password: "mot-mat-khau-du-dai",
    });
  });

  it("FUI[auth.invite:decline] declines, or signs in to accept with an existing account", () => {
    const onDecline = vi.fn();
    const onSignIn = vi.fn();
    renderPureUi(
      <AuthInviteSurface {...authInviteScenario({ onDecline, onSignIn })} />,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.invite.decline }));
    fireEvent.click(
      screen.getByRole("button", { name: copy.invite.signInToAccept }),
    );
    expect(onDecline).toHaveBeenCalledTimes(1);
    expect(onSignIn).toHaveBeenCalledTimes(1);
  });

  it("FUI[auth.invite:gone] says an expired, used or missing invitation cannot be used and goes to sign-in", () => {
    const onToSignIn = vi.fn();
    const { rerender } = renderPureUi(
      <AuthInviteSurface
        {...authInviteScenario({
          view: { kind: "gone", reason: "expired", orgName: "Minh An Group" },
          onToSignIn,
        })}
      />,
    );
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(
      copy.gone.expiredTitle,
    );
    expect(screen.getByText(/Lời mời đến Minh An Group/)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: copy.gone.toSignIn }));
    expect(onToSignIn).toHaveBeenCalledTimes(1);
    for (const [reason, title] of [
      ["used", copy.gone.usedTitle],
      ["missing", copy.gone.missingTitle],
    ] as const) {
      rerender(
        <AuthInviteSurface
          {...authInviteScenario({
            view: { kind: "gone", reason, orgName: null },
          })}
        />,
      );
      expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(title);
    }
  });
});

describe("auth shell", () => {
  it("FUI[auth.sign-in:language] switches the language from the footer", () => {
    const onLocaleChange = vi.fn();
    renderPureUi(
      <AuthShell copy={copy} locale="vi" onLocaleChange={onLocaleChange}>
        <p>form</p>
      </AuthShell>,
    );
    const group = screen.getByRole("group", { name: copy.shell.language });
    expect(
      within(group)
        .getByRole("button", { name: copy.shell.vi })
        .getAttribute("aria-pressed"),
    ).toBe("true");
    fireEvent.click(within(group).getByRole("button", { name: copy.shell.en }));
    expect(onLocaleChange).toHaveBeenCalledWith("en");
  });
});

describe("auth register surface", () => {
  it("FUI[auth.register:submit] submits register credentials through the injected callback", () => {
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
});
