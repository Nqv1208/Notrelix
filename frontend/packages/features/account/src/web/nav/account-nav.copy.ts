/**
 * Account navigation copy, vi and en, registered as the `account-nav`
 * catalog. The host's account sidebar resolves it with `useAccountNavCopy`.
 */
export const en = {
  label: "Account settings",
  back: "Back to workspace",
  profile: "Profile",
  appearance: "Appearance",
  security: "Security",
  notifications: "Notifications",
};

export type AccountNavCopy = typeof en;

export const vi: AccountNavCopy = {
  label: "Cài đặt tài khoản",
  back: "Quay lại không gian làm việc",
  profile: "Hồ sơ",
  appearance: "Giao diện",
  security: "Bảo mật",
  notifications: "Thông báo",
};

export function accountNavCopy(locale: "vi" | "en"): AccountNavCopy {
  return locale === "vi" ? vi : en;
}
