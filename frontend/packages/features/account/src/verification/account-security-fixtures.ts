/**
 * Deterministic fixtures for the account security page. "Now" is a fixed
 * instant so the relative times of the sessions never move.
 */
import {
  accountSecurityCopy,
  type AccountSecurityCopy,
} from "../web/security/account-security.copy";
import type {
  AccountSecurityData,
  AccountSecuritySurfaceProps,
} from "../web/security/account-security-surface";

export const SECURITY_NOW = "2026-10-12T03:40:00.000Z";

export function securityCopyFixture(
  locale: "en" | "vi" = "en",
): AccountSecurityCopy {
  return accountSecurityCopy(locale);
}

export function securityFixture(
  overrides: Partial<AccountSecurityData> = {},
): AccountSecurityData {
  return {
    ssoProvider: "Microsoft Entra",
    email: "ada@example.com",
    workspaceName: "Minh An Group",
    twoFactorEnabled: true,
    backupCodesLeft: 8,
    now: SECURITY_NOW,
    sessions: [
      {
        id: "session-macbook",
        device: "MacBook · Chrome",
        kind: "desktop",
        isCurrent: true,
        signedInAt: "2026-10-12T03:00:00.000Z",
      },
      {
        id: "session-iphone",
        device: "iPhone 15 · ứng dụng Notrelix",
        kind: "phone",
        isCurrent: false,
        signedInAt: "2026-10-12T01:40:00.000Z",
      },
      {
        id: "session-ipad",
        device: "iPad · ứng dụng Notrelix",
        kind: "tablet",
        isCurrent: false,
        signedInAt: "2026-10-09T03:40:00.000Z",
      },
    ],
    ...overrides,
  };
}

export type SecurityScenario = Omit<
  AccountSecuritySurfaceProps,
  | "onSetTwoFactor"
  | "onChangePassword"
  | "onSubmitNewPassword"
  | "onPasswordChangedContinue"
  | "onSubmitCode"
  | "onSubmitPassword"
  | "onCloseFlow"
  | "onLoadMoreActivity"
  | "onRenewBackupCodes"
  | "onSignOut"
  | "onSignOutOthers"
  | "onRetry"
>;

export function securityDefaultScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  return {
    copy: securityCopyFixture(locale),
    security: { status: "ready", data: securityFixture() },
    busy: false,
    actionError: false,
    notice: null,
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

/** An account with its own password, no second step and one device. */
export function securityOwnPasswordScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  const base = securityFixture();
  return {
    ...securityDefaultScenario(locale),
    security: {
      status: "ready",
      data: securityFixture({
        ssoProvider: null,
        twoFactorEnabled: false,
        backupCodesLeft: 0,
        sessions: base.sessions.slice(0, 1),
      }),
    },
  };
}

export function securityNoticeScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  return {
    ...securityDefaultScenario(locale),
    notice: securityCopyFixture(locale).twoFactor.renewed,
  };
}

/** Long device names, a failed action and a single backup code left. */
export function securityEdgeDataScenario(): SecurityScenario {
  const base = securityFixture();
  return {
    ...securityDefaultScenario("vi"),
    actionError: true,
    security: {
      status: "ready",
      data: securityFixture({
        backupCodesLeft: 1,
        email:
          "nguyen.thi.phuong.anh.tran.hoang.bao.ngoc@phong-ke-hoach-van-hanh.example.com",
        sessions: base.sessions.map((session, index) =>
          index === 1
            ? {
                ...session,
                device:
                  "Máy tính bảng Samsung Galaxy Tab S9 Ultra · ứng dụng Notrelix cho Android",
              }
            : session,
        ),
      }),
    },
  };
}

/** The change of password open on an account with its own password and no second step. */
export function securityChangePasswordScenario(
  locale: "en" | "vi" = "en",
  overrides: { needsCode?: boolean } = {},
): SecurityScenario {
  const own = securityOwnPasswordScenario(locale);
  return {
    ...own,
    security:
      overrides.needsCode === true
        ? { status: "ready", data: securityFixture({ ssoProvider: null }) }
        : own.security,
    flow: {
      kind: "password",
      needsCode: overrides.needsCode === true,
      error: null,
    },
  };
}

/** The change of password refused: the current password is not right. */
export function securityChangePasswordRefusedScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  return {
    ...securityChangePasswordScenario(locale),
    flow: { kind: "password", needsCode: false, error: "wrongCurrent" },
  };
}

/** The password changed: every session is over, this one too. */
export function securityPasswordChangedScenario(
  locale: "en" | "vi" = "en",
): SecurityScenario {
  return {
    ...securityOwnPasswordScenario(locale),
    flow: { kind: "passwordChanged" },
  };
}
