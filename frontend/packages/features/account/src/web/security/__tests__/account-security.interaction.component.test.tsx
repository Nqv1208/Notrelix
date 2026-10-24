import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  securityCopyFixture,
  securityDefaultScenario,
  securityEdgeDataScenario,
  securityErrorScenario,
  securityLoadingScenario,
  securityNoticeScenario,
  securityChangePasswordRefusedScenario,
  securityChangePasswordScenario,
  securityPasswordChangedScenario,
  securityOwnPasswordScenario,
  type SecurityScenario,
} from "../../../verification/account-security-fixtures";
import { en } from "../account-security.copy";
import {
  AccountSecuritySurface,
  type AccountSecuritySurfaceProps,
} from "../account-security-surface";

const LOCALES = ["vi", "en"] as const;

function renderSecurity(
  scenario: SecurityScenario,
  overrides: Partial<AccountSecuritySurfaceProps> = {},
) {
  const props: AccountSecuritySurfaceProps = {
    ...scenario,
    onSetTwoFactor: vi.fn(),
    onRenewBackupCodes: vi.fn(),
    onSignOut: vi.fn(),
    onSignOutOthers: vi.fn(),
    onRetry: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<AccountSecuritySurface {...props} />);
  return { props, view };
}

describe("AccountSecuritySurface", () => {
  it("FUI[account.security:render] shows the password, second step and devices in vi and en", () => {
    for (const locale of LOCALES) {
      const copy = securityCopyFixture(locale);
      const { view } = renderSecurity(securityDefaultScenario(locale));
      expect(
        screen.getByRole("heading", { level: 2, name: copy.title }),
      ).toBeTruthy();
      for (const title of [
        copy.password.title,
        copy.twoFactor.title,
        copy.sessions.title,
      ])
        expect(
          screen.getByRole("heading", { level: 3, name: title }),
        ).toBeTruthy();
      expect(screen.getByText(copy.sessions.thisDevice)).toBeTruthy();
      // Times use the producer's clock: 2 hours and 3 days before "now".
      expect(document.body.textContent).toContain(
        locale === "vi" ? "2 giờ trước" : "2 hours ago",
      );
      expect(document.body.textContent).toContain(
        locale === "vi" ? "3 ngày trước" : "3 days ago",
      );
      view.unmount();
    }
  });

  it("FUI[account.security:sso] names the provider that owns the password, or offers the change", () => {
    const copy = securityCopyFixture("en");
    const sso = renderSecurity(securityDefaultScenario("en"));
    const link = screen.getByRole("link", {
      name: new RegExp(copy.password.ssoLink),
    });
    expect(link.getAttribute("target")).toBe("_blank");
    expect(link.getAttribute("rel")).toContain("noopener");
    sso.view.unmount();

    const own = renderSecurity(securityOwnPasswordScenario("en"), {
      onChangePassword: vi.fn(),
    });
    fireEvent.click(screen.getByRole("button", { name: copy.password.change }));
    expect(own.props.onChangePassword).toHaveBeenCalledTimes(1);
    own.view.unmount();

    // Without a way to change it the page says so instead of linking away.
    const none = renderSecurity(securityOwnPasswordScenario("en"));
    expect(screen.getByText(copy.password.unavailable)).toBeTruthy();
    none.view.unmount();
  });

  it("FUI[account.security:password-change] checks the new password before sending and hands over the three values", () => {
    for (const locale of LOCALES) {
      const copy = securityCopyFixture(locale);
      const t = copy.passwordFlow;
      const { props, view } = renderSecurity(
        securityChangePasswordScenario(locale),
        { onSubmitNewPassword: vi.fn(), onCloseFlow: vi.fn() },
      );
      const current = screen.getByLabelText(t.currentLabel);
      const next = screen.getByLabelText(t.newLabel);
      const repeat = screen.getByLabelText(t.confirmLabel);
      // No second step on this account: no code field.
      expect(screen.queryByLabelText(t.codeLabel)).toBeNull();
      const submit = () =>
        fireEvent.click(screen.getByRole("button", { name: t.submit }));

      submit();
      expect(screen.getByText(t.currentRequired)).toBeTruthy();
      expect(props.onSubmitNewPassword).not.toHaveBeenCalled();

      fireEvent.change(current, { target: { value: "old-password-1" } });
      fireEvent.change(next, { target: { value: "short" } });
      fireEvent.change(repeat, { target: { value: "short" } });
      submit();
      expect(
        screen.getByText(t.tooShort.replace("{{count}}", "8")),
      ).toBeTruthy();
      expect(next.getAttribute("aria-invalid")).toBe("true");

      fireEvent.change(next, { target: { value: "old-password-1" } });
      fireEvent.change(repeat, { target: { value: "old-password-1" } });
      submit();
      expect(screen.getByText(t.sameAsCurrent)).toBeTruthy();

      fireEvent.change(next, { target: { value: "new-password-2" } });
      submit();
      expect(screen.getByText(t.mismatch)).toBeTruthy();
      expect(props.onSubmitNewPassword).not.toHaveBeenCalled();

      fireEvent.change(repeat, { target: { value: "new-password-2" } });
      submit();
      expect(props.onSubmitNewPassword).toHaveBeenCalledWith({
        current: "old-password-1",
        next: "new-password-2",
        code: "",
      });

      fireEvent.click(screen.getByRole("button", { name: t.cancel }));
      expect(props.onCloseFlow).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[account.security:password-code] asks for an authenticator code while two-factor is on", () => {
    const copy = securityCopyFixture("en");
    const t = copy.passwordFlow;
    const { props, view } = renderSecurity(
      securityChangePasswordScenario("en", { needsCode: true }),
      { onSubmitNewPassword: vi.fn() },
    );
    fireEvent.change(screen.getByLabelText(t.currentLabel), {
      target: { value: "old-password-1" },
    });
    fireEvent.change(screen.getByLabelText(t.newLabel), {
      target: { value: "new-password-2" },
    });
    fireEvent.change(screen.getByLabelText(t.confirmLabel), {
      target: { value: "new-password-2" },
    });
    fireEvent.click(screen.getByRole("button", { name: t.submit }));
    expect(screen.getByText(t.codeRequired)).toBeTruthy();
    expect(props.onSubmitNewPassword).not.toHaveBeenCalled();

    fireEvent.change(screen.getByLabelText(t.codeLabel), {
      target: { value: " 482931 " },
    });
    fireEvent.click(screen.getByRole("button", { name: t.submit }));
    expect(props.onSubmitNewPassword).toHaveBeenCalledWith({
      current: "old-password-1",
      next: "new-password-2",
      code: "482931",
    });
    view.unmount();
  });

  it("FUI[account.security:password-result] tells what the producer refused and that the password changed", () => {
    for (const locale of LOCALES) {
      const t = securityCopyFixture(locale).passwordFlow;
      const refused = renderSecurity(
        securityChangePasswordRefusedScenario(locale),
      );
      const field = screen.getByLabelText(t.currentLabel);
      expect(screen.getByText(t.wrongCurrent)).toBeTruthy();
      expect(field.getAttribute("aria-invalid")).toBe("true");
      refused.view.unmount();

      for (const [error, text] of [
        ["wrongCode", t.wrongCode],
        ["rejected", t.rejected.replace("{{count}}", "8")],
        ["wait", t.wait.replace("{{seconds}}", "42")],
      ] as const) {
        const base = securityChangePasswordScenario(locale, {
          needsCode: true,
        });
        const view = renderSecurity({
          ...base,
          flow: { kind: "password", needsCode: true, error, wait: 42 },
        });
        expect(document.body.textContent).toContain(text);
        view.view.unmount();
      }

      const done = renderSecurity(securityPasswordChangedScenario(locale), {
        onPasswordChangedContinue: vi.fn(),
      });
      expect(screen.getByRole("status").textContent).toBe(t.doneText);
      fireEvent.click(screen.getByRole("button", { name: t.signInAgain }));
      expect(done.props.onPasswordChangedContinue).toHaveBeenCalledTimes(1);
      done.view.unmount();
    }
  });

  it("FUI[account.security:two-factor] turns the second step off and on", () => {
    for (const locale of LOCALES) {
      const copy = securityCopyFixture(locale);
      const on = renderSecurity(securityDefaultScenario(locale));
      fireEvent.click(
        screen.getByRole("button", { name: copy.twoFactor.disable }),
      );
      expect(on.props.onSetTwoFactor).toHaveBeenCalledWith(false);
      on.view.unmount();

      const off = renderSecurity(securityOwnPasswordScenario(locale));
      expect(
        screen.queryByRole("button", { name: copy.twoFactor.backupRenew }),
      ).toBeNull();
      fireEvent.click(
        screen.getByRole("button", { name: copy.twoFactor.enable }),
      );
      expect(off.props.onSetTwoFactor).toHaveBeenCalledWith(true);
      off.view.unmount();
    }
  });

  it("FUI[account.security:backup] counts the codes and asks for new ones", () => {
    const copy = securityCopyFixture("en");
    const { props, view } = renderSecurity(securityDefaultScenario("en"));
    expect(
      screen.getByText(
        copy.twoFactor.backupText_other.replace("{{count}}", "8"),
      ),
    ).toBeTruthy();
    fireEvent.click(
      screen.getByRole("button", { name: copy.twoFactor.backupRenew }),
    );
    expect(props.onRenewBackupCodes).toHaveBeenCalledTimes(1);
    view.unmount();
  });

  it("FUI[account.security:sessions] signs out one device by its own name", () => {
    const copy = securityCopyFixture("en");
    const { props, view } = renderSecurity(securityDefaultScenario("en"));
    fireEvent.click(
      screen.getByRole("button", {
        name: copy.sessions.signOutLabel.replace(
          "{{device}}",
          "iPad · ứng dụng Notrelix",
        ),
      }),
    );
    expect(props.onSignOut).toHaveBeenCalledWith("session-ipad");
    expect(
      screen.getByRole("button", {
        name: copy.sessions.signOutThisLabel.replace(
          "{{device}}",
          "MacBook · Chrome",
        ),
      }),
    ).toBeTruthy();
    view.unmount();
  });

  it("FUI[account.security:others] signs out every other device, and is off when there is none", () => {
    const copy = securityCopyFixture("en");
    const many = renderSecurity(securityDefaultScenario("en"));
    fireEvent.click(screen.getByRole("button", { name: copy.others.action }));
    expect(many.props.onSignOutOthers).toHaveBeenCalledTimes(1);
    many.view.unmount();

    const none = renderSecurity(securityOwnPasswordScenario("en"));
    expect(
      (
        screen.getByRole("button", {
          name: copy.others.action,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    expect(screen.getByText(copy.others.nobody)).toBeTruthy();
    none.view.unmount();
  });

  it("FUI[account.security:loading] shows a named loading state", () => {
    const copy = securityCopyFixture("en");
    const { view } = renderSecurity(securityLoadingScenario("en"));
    expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
    view.unmount();
  });

  it("FUI[account.security:error] offers a retry when the settings cannot load", () => {
    const copy = securityCopyFixture("en");
    const { props, view } = renderSecurity(securityErrorScenario("en"));
    const alert = screen.getByRole("alert");
    expect(alert.textContent).toContain(copy.loadError);
    fireEvent.click(within(alert).getByRole("button", { name: copy.retry }));
    expect(props.onRetry).toHaveBeenCalledTimes(1);
    view.unmount();
  });

  it("FUI[account.security:notice] shows the toast and locks the buttons while busy", () => {
    const copy = securityCopyFixture("en");
    const notice = renderSecurity(securityNoticeScenario("en"));
    expect(screen.getByRole("status").textContent).toContain(
      copy.twoFactor.renewed,
    );
    notice.view.unmount();
    const busy = renderSecurity({
      ...securityDefaultScenario("en"),
      busy: true,
    });
    expect(
      (
        screen.getByRole("button", {
          name: copy.twoFactor.backupRenew,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
    busy.view.unmount();
  });

  it("FUI[account.security:edge] keeps long device names and a failed action readable", () => {
    const copy = securityCopyFixture("vi");
    const { view } = renderSecurity(securityEdgeDataScenario());
    expect(document.body.textContent).toContain("Samsung Galaxy Tab S9 Ultra");
    expect(screen.getByRole("alert").textContent).toContain(copy.actionError);
    expect(
      screen.getByText(copy.twoFactor.backupText_one.replace("{{count}}", "1")),
    ).toBeTruthy();
    view.unmount();
  });

  it("FUI[account.security:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderSecurity(securityNoticeScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.password.title,
      en.twoFactor.title,
      en.sessions.title,
      en.others.action,
      en.twoFactor.renewed,
    ])
      expect(text).not.toContain(literal);
    view.unmount();
  });
});
