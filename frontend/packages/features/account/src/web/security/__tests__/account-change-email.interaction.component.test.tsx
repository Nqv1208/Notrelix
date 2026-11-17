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
  flow: AccountSecurityFlow | null,
  locale: "vi" | "en",
  scenario: "own" | "sso" = "own",
) {
  const base =
    scenario === "own"
      ? securityOwnPasswordScenario(locale)
      : securityDefaultScenario(locale);
  const props: AccountSecuritySurfaceProps = {
    ...base,
    flow,
    onSetTwoFactor: vi.fn(),
    onRenewBackupCodes: vi.fn(),
    onSignOut: vi.fn(),
    onSignOutOthers: vi.fn(),
    onRetry: vi.fn(),
    onChangeEmail: vi.fn(),
    onSubmitEmail: vi.fn(),
    onCloseFlow: vi.fn(),
  };
  renderPureUi(<AccountSecuritySurface {...props} />);
  return props;
}

describe.each(LOCALES)("changing the sign-in email (%s)", (locale) => {
  const t = securityCopyFixture(locale).email;

  it("offers the change and starts it", () => {
    const props = renderFlow(null, locale);
    fireEvent.click(screen.getByRole("button", { name: t.change }));
    expect(props.onChangeEmail).toHaveBeenCalledTimes(1);
  });

  it("leaves the email to the company when single sign-on owns the account", () => {
    renderFlow(null, locale, "sso");
    expect(screen.queryByRole("button", { name: t.change })).toBeNull();
  });

  it("names the proof step", () => {
    renderFlow(
      {
        kind: "stepUp",
        purpose: "changeEmail",
        factor: "code",
        error: null,
      },
      locale,
    );
    expect(screen.getByRole("region", { name: t.proofTitle })).not.toBeNull();
  });

  it("tells a malformed or current address before sending, and sends a good one", () => {
    const props = renderFlow({ kind: "changeEmail", error: null }, locale);
    const field = screen.getByLabelText(t.label);
    const submit = screen.getByRole("button", { name: t.submit });

    fireEvent.change(field, { target: { value: "not-an-email" } });
    fireEvent.click(submit);
    expect(screen.getByRole("alert").textContent).toBe(t.invalid);

    fireEvent.change(field, { target: { value: "ADA@example.com" } });
    fireEvent.click(submit);
    expect(props.onSubmitEmail).not.toHaveBeenCalled();

    fireEvent.change(field, { target: { value: "new@example.com" } });
    fireEvent.click(submit);
    expect(props.onSubmitEmail).toHaveBeenCalledWith("new@example.com");
  });

  it("says the same words once the link is on its way", () => {
    renderFlow({ kind: "emailSent", email: "new@example.com" }, locale);
    const region = screen.getByRole("region", { name: t.sentTitle });
    expect(region.textContent).toContain("new@example.com");
  });
});
