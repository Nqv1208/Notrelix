/**
 * Deterministic fixtures for the account security page: fixed ISO dates and
 * locale-bound formatters, no clocks.
 */
import { createFormatters } from "@notrelix/i18n";
import {
  accountSecurityCopy,
  type AccountSecurityCopy,
} from "../web/security/account-security.copy";
import type {
  AccountSecurityData,
  AccountSecuritySurfaceProps,
} from "../web/security/account-security-surface";
import type { AccountFormatters } from "../web/shared/account-surface-model";

export function securityCopyFixture(
  locale: "en" | "vi" = "en",
): AccountSecurityCopy {
  return accountSecurityCopy(locale);
}

export function securityFormattersFixture(
  locale: "en" | "vi" = "en",
): AccountFormatters {
  return createFormatters(locale);
}

export const SECURITY_PASSWORD_CHANGED_AT = "2026-01-15T09:00:00.000Z";

export function securityFixture(
  overrides: Partial<AccountSecurityData> = {},
): AccountSecurityData {
  return {
    twoFactorEnabled: false,
    lastPasswordChange: SECURITY_PASSWORD_CHANGED_AT,
    activeSessions: 3,
    ...overrides,
  };
}

export type SecurityScenario = Omit<AccountSecuritySurfaceProps, "onRetry">;

export function securityDefaultScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  return {
    copy: securityCopyFixture(locale),
    formatters: securityFormattersFixture(locale),
    security: { status: "ready", data: securityFixture() },
  };
}

export function securityLoadingScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  return {
    ...securityDefaultScenario(locale),
    security: { status: "loading" },
  };
}

export function securityErrorScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  return { ...securityDefaultScenario(locale), security: { status: "error" } };
}

/** Two-factor on, a single session, in Vietnamese. */
export function securityTwoFactorScenario(
  locale: "en" | "vi" = "vi",
): SecurityScenario {
  return {
    ...securityDefaultScenario(locale),
    security: {
      status: "ready",
      data: securityFixture({ twoFactorEnabled: true, activeSessions: 1 }),
    },
  };
}

/** No password date, no active session and a very large session count. */
export function securityEdgeDataScenario(): SecurityScenario {
  return {
    ...securityDefaultScenario("vi"),
    security: {
      status: "ready",
      data: securityFixture({
        lastPasswordChange: null,
        activeSessions: 1234,
        twoFactorEnabled: true,
      }),
    },
  };
}

export function securityNoSessionScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  return {
    ...securityDefaultScenario(locale),
    security: { status: "ready", data: securityFixture({ activeSessions: 0 }) },
  };
}
