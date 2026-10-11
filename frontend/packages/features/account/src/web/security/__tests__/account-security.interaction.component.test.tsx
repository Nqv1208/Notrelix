import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  SECURITY_PASSWORD_CHANGED_AT,
  securityCopyFixture,
  securityDefaultScenario,
  securityEdgeDataScenario,
  securityErrorScenario,
  securityFormattersFixture,
  securityLoadingScenario,
  securityNoSessionScenario,
  securityTwoFactorScenario,
  type SecurityScenario,
} from "../../../verification/account-security-fixtures";
import { fillCopy, pluralCopy } from "../../shared/account-surface-model";
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
    onRetry: vi.fn(),
    ...overrides,
  };
  const view = renderPureUi(<AccountSecuritySurface {...props} />);
  return { props, view };
}

function card(name: string) {
  return screen.getByRole("region", { name });
}

describe("AccountSecuritySurface", () => {
  it("FUI[account.security:render] groups password, two-factor and sessions with a localized last-changed date", () => {
    for (const locale of LOCALES) {
      const copy = securityCopyFixture(locale);
      const { view } = renderSecurity(securityDefaultScenario(locale));

      expect(
        screen.getByRole("heading", { level: 1, name: copy.title }),
      ).toBeTruthy();
      const password = card(copy.password.title);
      // The date goes through the injected locale formatters.
      const changed = copy.password.lastChanged.replace(
        "{{date}}",
        securityFormattersFixture(locale).date(SECURITY_PASSWORD_CHANGED_AT),
      );
      expect(within(password).getByText(changed)).toBeTruthy();
      expect(
        within(card(copy.twoFactor.title)).getByText(copy.twoFactor.disabled),
      ).toBeTruthy();
      expect(
        within(card(copy.sessions.title)).getByText(
          pluralCopy(
            { one: copy.sessions.count_one, other: copy.sessions.count_other },
            3,
          ),
        ),
      ).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[account.security:actions] explains why the password and two-factor actions are unavailable", () => {
    for (const locale of LOCALES) {
      const copy = securityCopyFixture(locale);
      const { view } = renderSecurity(securityDefaultScenario(locale));

      const change = screen.getByRole("button", {
        name: copy.password.change,
      }) as HTMLButtonElement;
      expect(change.disabled).toBe(true);
      const hint = change.getAttribute("aria-describedby");
      expect(hint).toBeTruthy();
      expect(document.getElementById(hint as string)?.textContent).toBe(
        copy.password.unavailable,
      );

      const enable = screen.getByRole("button", {
        name: copy.twoFactor.enable,
      }) as HTMLButtonElement;
      expect(enable.disabled).toBe(true);
      expect(
        document.getElementById(enable.getAttribute("aria-describedby") ?? "")
          ?.textContent,
      ).toBe(copy.twoFactor.unavailable);
      view.unmount();
    }
  });

  it("FUI[account.security:two-factor] shows the enabled state and offers to disable it", () => {
    for (const locale of LOCALES) {
      const copy = securityCopyFixture(locale);
      const { view } = renderSecurity(securityTwoFactorScenario(locale));
      const twoFactor = card(copy.twoFactor.title);
      expect(within(twoFactor).getByText(copy.twoFactor.enabled)).toBeTruthy();
      expect(
        within(twoFactor).getByRole("button", { name: copy.twoFactor.disable }),
      ).toBeTruthy();
      expect(
        within(card(copy.sessions.title)).getByText(
          pluralCopy(
            { one: copy.sessions.count_one, other: copy.sessions.count_other },
            1,
          ),
        ),
      ).toBeTruthy();
      view.unmount();
    }
  });

  it("FUI[account.security:loading] shows a named loading state without the cards", () => {
    for (const locale of LOCALES) {
      const copy = securityCopyFixture(locale);
      const { view } = renderSecurity(securityLoadingScenario(locale));
      expect(screen.getByRole("status", { name: copy.loading })).toBeTruthy();
      expect(screen.queryByRole("region")).toBeNull();
      view.unmount();
    }
  });

  it("FUI[account.security:error] offers a retry when the settings cannot load", () => {
    for (const locale of LOCALES) {
      const copy = securityCopyFixture(locale);
      const { props, view } = renderSecurity(securityErrorScenario(locale));
      const alert = screen.getByRole("alert");
      expect(alert.textContent).toContain(copy.loadError);
      fireEvent.click(within(alert).getByRole("button", { name: copy.retry }));
      expect(props.onRetry).toHaveBeenCalledTimes(1);
      view.unmount();
    }
  });

  it("FUI[account.security:edge] handles a missing date and session counts of 0, 1 and 1234", () => {
    const copy = securityCopyFixture("vi");
    const edge = renderSecurity(securityEdgeDataScenario());
    expect(
      within(card(copy.password.title)).getByText(
        copy.password.lastChangedUnknown,
      ),
    ).toBeTruthy();
    // Large counts use the locale number format (vi groups with a dot).
    expect(securityFormattersFixture("vi").number(1234)).toBe("1.234");
    expect(
      within(card(copy.sessions.title)).getByText(
        fillCopy(copy.sessions.count_other, { count: "1.234" }),
      ),
    ).toBeTruthy();
    edge.view.unmount();

    const none = renderSecurity(securityNoSessionScenario("en"));
    expect(
      within(card(securityCopyFixture("en").sessions.title)).getByText(
        "0 sessions are currently active",
      ),
    ).toBeTruthy();
    none.view.unmount();
  });

  it("FUI[account.security:copy] leaves no English literal in the Vietnamese page", () => {
    const { view } = renderSecurity(securityTwoFactorScenario("vi"));
    const text = document.body.textContent ?? "";
    for (const literal of [
      en.title,
      en.description,
      en.password.title,
      en.password.change,
      en.twoFactor.title,
      en.twoFactor.enabled,
      en.twoFactor.disable,
      en.sessions.title,
      "session",
    ]) {
      expect(text).not.toContain(literal);
    }
    view.unmount();
  });
});
