/**
 * Account navigation copy, vi and en, registered as the `account-nav`
 * catalog. The host's account sidebar resolves it with `useAccountNavCopy`.
 */
export const en = {
  label: "Account settings",
  title: "Your account",
  profile: "Profile",
  appearance: "Appearance",
  security: "Security",
  notifications: "Notifications",
  workspaces: "Your workspaces",
};

export type AccountNavCopy = typeof en;

export const vi: AccountNavCopy = {
  label: "Cài đặt tài khoản",
  title: "Tài khoản của bạn",
  profile: "Hồ sơ",
  appearance: "Giao diện",
  security: "Bảo mật",
  notifications: "Thông báo",
  workspaces: "Workspace của bạn",
};

export function accountNavCopy(locale: "vi" | "en"): AccountNavCopy {
  return locale === "vi" ? vi : en;
}
