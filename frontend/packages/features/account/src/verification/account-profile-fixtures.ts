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
    jobTitle: "Implementation lead",
    email: "ada@example.com",
    phone: "+84912468135",
    timezone: "Asia/Ho_Chi_Minh",
    dateFormat: "dd/mm/yyyy",
    joinedAt: "2024-03-04T00:00:00.000Z",
    avatarUrl: null,
    managedBy: "Microsoft Entra",
    workspaceName: "Minh An Group",
    ...overrides,
  };
}

export type ProfileScenario = Omit<
  AccountProfileSurfaceProps,
  "onSave" | "onRetry" | "onPhotoChange" | "onPhotoRemove"
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
 * Long Vietnamese name and address, a time zone the page does not list, an
 * email nobody manages and a refused photo: nothing may overflow or lose its
 * label.
 */
export function profileEdgeDataScenario(): ProfileScenario {
  return {
    ...profileDefaultScenario("vi"),
    profile: {
      status: "ready",
      data: profileFixture({
        name: "Nguyễn Thị Phương Anh Trần Hoàng Bảo Ngọc Lê Văn Đức Minh",
        jobTitle:
          "Trưởng nhóm triển khai và vận hành hệ thống hoạch định nguồn lực doanh nghiệp khu vực Đông Nam Á",
        email:
          "nguyen.thi.phuong.anh.tran.hoang.bao.ngoc@phong-ke-hoach-van-hanh-khu-vuc-dong-nam-a.example.com",
        timezone: "Pacific/Auckland",
        managedBy: null,
      }),
    },
    photoError: "unsupported",
    save: { state: "error" },
  };
}
