import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";

import {
  securityCopyFixture,
  securityDefaultScenario,
  securityOwnPasswordScenario,
} from "../../../verification/account-security-fixtures";
import {
  AccountSecuritySurface,
  type AccountSecurityFlow,
  type AccountSecuritySurfaceProps,
} from "../account-security-surface";

const LOCALES = ["vi", "en"] as const;

function renderFlow(
  flow: AccountSecurityFlow,
  locale: "vi" | "en",
  overrides: Partial<AccountSecuritySurfaceProps> = {},
) {
  const props: AccountSecuritySurfaceProps = {
    ...securityDefaultScenario(locale),
    flow,
    onSetTwoFactor: vi.fn(),
    onRenewBackupCodes: vi.fn(),
    onSignOut: vi.fn(),
    onSignOutOthers: vi.fn(),
    onRetry: vi.fn(),
    onSubmitCode: vi.fn(),
    onSubmitPassword: vi.fn(),
    onCloseFlow: vi.fn(),
    onConfirmSignOutThis: vi.fn(),
    onPasswordChangedContinue: vi.fn(),
    ...overrides,
  };
  return { props, view: renderPureUi(<AccountSecuritySurface {...props} />) };
}

describe("AccountSecuritySurface step-up and two-factor flows", () => {
  it("FUI[account.security:stepup-code] opens on the code field, sends the trimmed-by-caller value once, and cancels", () => {
    for (const locale of LOCALES) {
      const t = securityCopyFixture(locale).flow;
      const { props, view } = renderFlow(
        { kind: "stepUp", purpose: "disable", factor: "code", error: null },
        locale,
      );
      const field = screen.getByLabelText(t.codeLabel);
      expect(document.activeElement).toBe(field);
      expect(screen.getByText(t.disableText, { exact: false })).toBeTruthy();
      fireEvent.change(field, { target: { value: "482931" } });
      fireEvent.click(screen.getByRole("button", { name: t.confirm }));
      expect(props.onSubmitCode).toHaveBeenCalledWith("482931");
      expect(props.onSubmitPassword).not.toHaveBeenCalled();
      // The answer is cleared after it is sent, so a wrong code is not resubmitted by accident.
      expect((field as HTMLInputElement).value).toBe("");
      fireEvent.click(screen.getByRole("button", { name: t.cancel }));
      expect(props.onCloseFlow).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[account.security:stepup-password] asks for the password when no second factor is on", () => {
    for (const locale of LOCALES) {
      const t = securityCopyFixture(locale).flow;
      const { props, view } = renderFlow(
        { kind: "stepUp", purpose: "enroll", factor: "password", error: null },
        locale,
        securityOwnPasswordScenario(locale),
      );
      const field = screen.getByLabelText(t.passwordLabel);
      expect(field.getAttribute("type")).toBe("password");
      expect(document.activeElement).toBe(field);
      expect(
        screen.getByText(t.enrollProofText, { exact: false }),
      ).toBeTruthy();
      fireEvent.change(field, { target: { value: "a-password" } });
      fireEvent.click(screen.getByRole("button", { name: t.confirm }));
      expect(props.onSubmitPassword).toHaveBeenCalledWith("a-password");
      expect(props.onSubmitCode).not.toHaveBeenCalled();
      view.unmount();
    }
  });

  it("FUI[account.security:stepup-empty] does not send an empty answer", () => {
    const t = securityCopyFixture("en").flow;
    const { props, view } = renderFlow(
      { kind: "stepUp", purpose: "renew", factor: "code", error: null },
      "en",
    );
    const confirm = screen.getByRole("button", { name: t.confirm });
    expect((confirm as HTMLButtonElement).disabled).toBe(true);
    fireEvent.submit(screen.getByLabelText(t.codeLabel).closest("form")!);
    expect(props.onSubmitCode).not.toHaveBeenCalled();
    view.unmount();
  });

  it("FUI[account.security:stepup-errors] says what went wrong in words and marks the field invalid", () => {
    for (const locale of LOCALES) {
      const t = securityCopyFixture(locale);
      const cases: [
        Extract<AccountSecurityFlow, { kind: "stepUp" }>,
        string,
      ][] = [
        [
          {
            kind: "stepUp",
            purpose: "disable",
            factor: "code",
            error: "wrong",
          },
          t.flow.wrongCode,
        ],
        [
          {
            kind: "stepUp",
            purpose: "enroll",
            factor: "password",
            error: "wrong",
          },
          t.flow.wrongPassword,
        ],
        [
          {
            kind: "stepUp",
            purpose: "renew",
            factor: "code",
            error: "expired",
          },
          t.flow.expired,
        ],
        [
          {
            kind: "stepUp",
            purpose: "others",
            factor: "code",
            error: "wait",
            wait: 42,
          },
          t.flow.wait.replace("{{seconds}}", "42"),
        ],
        [
          {
            kind: "stepUp",
            purpose: "others",
            factor: "code",
            error: "failed",
          },
          t.actionError,
        ],
      ];
      for (const [flow, text] of cases) {
        const { view } = renderFlow(flow, locale);
        expect(screen.getByRole("alert").textContent).toBe(text);
        const labelText =
          flow.factor === "password" ? t.flow.passwordLabel : t.flow.codeLabel;
        expect(
          screen.getByLabelText(labelText).getAttribute("aria-invalid"),
        ).toBe("true");
        const confirm = screen.getByRole("button", {
          name: t.flow.confirm,
        }) as HTMLButtonElement;
        // A throttled step cannot be sent again until the wait is over.
        if (flow.error === "wait") expect(confirm.disabled).toBe(true);
        view.unmount();
      }
    }
  });

  it("FUI[account.security:stepup-pending] holds Confirm while a request runs, even with an answer typed", () => {
    const t = securityCopyFixture("en").flow;
    const { view } = renderFlow(
      { kind: "stepUp", purpose: "disable", factor: "code", error: null },
      "en",
      { busy: true },
    );
    fireEvent.change(screen.getByLabelText(t.codeLabel), {
      target: { value: "123456" },
    });
    expect(
      (screen.getByRole("button", { name: t.confirm }) as HTMLButtonElement)
        .disabled,
    ).toBe(true);
    view.unmount();
  });

  it("FUI[account.security:enroll-errors] keeps the key readable while a wrong first code is refused", () => {
    for (const locale of LOCALES) {
      const t = securityCopyFixture(locale);
      for (const [error, text] of [
        ["wrong", t.flow.wrongCode],
        ["expired", t.flow.expired],
        ["wait", t.flow.wait.replace("{{seconds}}", "30")],
      ] as const) {
        const { view } = renderFlow(
          {
            kind: "enroll",
            secret: "JBSWY3DPEHPK3PXP",
            otpAuthUri: "",
            error,
            wait: 30,
          },
          locale,
        );
        expect(screen.getByRole("alert").textContent).toBe(text);
        expect(screen.getByText("JBSWY3DPEHPK3PXP")).toBeTruthy();
        view.unmount();
      }
    }
  });

  it("FUI[account.security:codes] shows the codes once, says how many other devices were signed out, and focuses the confirm button", () => {
    const t = securityCopyFixture("en").flow;
    const codes = ["AAAA-1111", "BBBB-2222"];
    const some = renderFlow({ kind: "codes", codes, othersEnded: 2 }, "en");
    expect(screen.getByText("AAAA-1111")).toBeTruthy();
    expect(screen.getByRole("status").textContent).toBe(
      t.codesOthers_other.replace("{{count}}", "2"),
    );
    const done = screen.getByRole("button", { name: t.done });
    expect(document.activeElement).toBe(done);
    fireEvent.click(done);
    expect(some.props.onCloseFlow).toHaveBeenCalledTimes(1);
    some.view.unmount();

    const none = renderFlow({ kind: "codes", codes, othersEnded: 0 }, "en");
    expect(screen.getByRole("status").textContent).toBe(
      securityCopyFixture("en").others.nobody,
    );
    none.view.unmount();

    const fresh = renderFlow({ kind: "codes", codes }, "en");
    expect(screen.queryByRole("status")).toBeNull();
    fresh.view.unmount();
  });

  it("FUI[account.security:sign-out-this] asks before ending this device and focuses the safe answer", () => {
    for (const locale of LOCALES) {
      const t = securityCopyFixture(locale).flow;
      const { props, view } = renderFlow({ kind: "signOutThis" }, locale);
      const cancel = screen.getByRole("button", { name: t.cancel });
      expect(document.activeElement).toBe(cancel);
      fireEvent.click(
        screen.getByRole("button", { name: t.signOutThisConfirm }),
      );
      expect(props.onConfirmSignOutThis).toHaveBeenCalledTimes(1);
      fireEvent.click(cancel);
      expect(props.onCloseFlow).toHaveBeenCalledTimes(1);
      view.unmount();
    }
    const busy = renderFlow({ kind: "signOutThis" }, "en", { busy: true });
    expect(
      (
        screen.getByRole("button", {
          name: securityCopyFixture("en").flow.signOutThisConfirm,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    busy.view.unmount();
  });

  it("FUI[account.security:two-factor-off] tells the session ended and focuses the sign-in button", () => {
    for (const locale of LOCALES) {
      const t = securityCopyFixture(locale).flow;
      const { props, view } = renderFlow({ kind: "twoFactorOff" }, locale);
      expect(screen.getByRole("status").textContent).toBe(t.disabledText);
      const signIn = screen.getByRole("button", { name: t.signInAgain });
      expect(document.activeElement).toBe(signIn);
      fireEvent.click(signIn);
      expect(props.onPasswordChangedContinue).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });
});
