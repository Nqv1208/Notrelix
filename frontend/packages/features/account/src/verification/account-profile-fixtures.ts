/**
 * Deterministic fixtures for the account profile page. No clocks or random
 * values: dates are fixed ISO strings and formatters are bound to a locale.
 */
import { createFormatters } from "@notrelix/i18n";
import {
  accountProfileCopy,
  type AccountProfileCopy,
} from "../web/profile/account-profile.copy";
import type {
  AccountProfileData,
  AccountProfileSurfaceProps,
} from "../web/profile/account-profile-surface";
import type { AccountFormatters } from "../web/shared/account-surface-model";

export function profileCopyFixture(
  locale: "en" | "vi" = "en",
): AccountProfileCopy {
  return accountProfileCopy(locale);
}

export function profileFormattersFixture(
  locale: "en" | "vi" = "en",
): AccountFormatters {
  return createFormatters(locale);
}

export function profileFixture(
  overrides: Partial<AccountProfileData> = {},
): AccountProfileData {
  return {
    name: "Ada Lovelace",
    email: "ada@example.com",
    timezone: "Asia/Ho_Chi_Minh",
    locale: "en",
    createdAt: "2025-03-12T09:00:00.000Z",
    ...overrides,
  };
}

export type ProfileScenario = Omit<
  AccountProfileSurfaceProps,
  "onSave" | "onRetry"
>;

export function profileDefaultScenario(
  locale: "en" | "vi" = "en",
): ProfileScenario {
  return {
    copy: profileCopyFixture(locale),
    formatters: profileFormattersFixture(locale),
    profile: { status: "ready", data: profileFixture() },
    save: { state: "idle" },
  };
}

export function profileLoadingScenario(
  locale: "en" | "vi" = "en",
): ProfileScenario {
  return { ...profileDefaultScenario(locale), profile: { status: "loading" } };
}

export function profileErrorScenario(
  locale: "en" | "vi" = "en",
): ProfileScenario {
  return { ...profileDefaultScenario(locale), profile: { status: "error" } };
}

export function profileSavingScenario(
  locale: "en" | "vi" = "en",
): ProfileScenario {
  return { ...profileDefaultScenario(locale), save: { state: "saving" } };
}

export function profileSavedScenario(
  locale: "en" | "vi" = "en",
): ProfileScenario {
  return { ...profileDefaultScenario(locale), save: { state: "saved" } };
}

export function profileSaveErrorScenario(
  locale: "en" | "vi" = "en",
): ProfileScenario {
  return { ...profileDefaultScenario(locale), save: { state: "error" } };
}

/**
 * Long Vietnamese name and address, a time zone and a language the page does
 * not list, and a failed save: nothing may overflow or lose its label.
 */
export function profileEdgeDataScenario(): ProfileScenario {
  return {
    ...profileDefaultScenario("vi"),
    profile: {
      status: "ready",
      data: profileFixture({
        name: "Nguyễn Thị Phương Anh Trần Hoàng Bảo Ngọc Lê Văn Đức Minh",
        email:
          "nguyen.thi.phuong.anh.tran.hoang.bao.ngoc@phong-ke-hoach-van-hanh-khu-vuc-dong-nam-a.example.com",
        timezone: "Pacific/Auckland",
        locale: "fr",
      }),
    },
    save: { state: "error" },
  };
}
