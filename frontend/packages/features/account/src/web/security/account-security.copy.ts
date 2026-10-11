/**
 * Account security copy, vi and en, registered as the `account-security`
 * catalog (`account-security.catalog.ts`). Counts use the `_one`/`_other`
 * forms (Vietnamese keeps both complete); placeholders use `{{name}}`.
 */
export const en = {
  title: "Security",
  description: "Manage your password and security settings.",
  loading: "Loading your security settings…",
  loadError: "We couldn't load your security settings.",
  retry: "Try again",
  password: {
    title: "Password",
    lastChanged: "Last changed {{date}}",
    lastChangedUnknown: "Last changed: unknown",
    change: "Change password",
    unavailable: "Changing your password isn't available here yet.",
  },
  twoFactor: {
    title: "Two-factor authentication",
    enabled: "Enabled",
    disabled: "Not enabled",
    enable: "Enable two-factor authentication",
    disable: "Disable two-factor authentication",
    unavailable: "Managing two-factor authentication isn't available here yet.",
  },
  sessions: {
    title: "Active sessions",
    count_one: "{{count}} session is currently active",
    count_other: "{{count}} sessions are currently active",
  },
};

export type AccountSecurityCopy = typeof en;

export const vi: AccountSecurityCopy = {
  title: "Bảo mật",
  description: "Quản lý mật khẩu và các cài đặt bảo mật của bạn.",
  loading: "Đang tải cài đặt bảo mật của bạn…",
  loadError: "Không thể tải cài đặt bảo mật của bạn.",
  retry: "Thử lại",
  password: {
    title: "Mật khẩu",
    lastChanged: "Đổi lần cuối vào {{date}}",
    lastChangedUnknown: "Đổi lần cuối: không rõ",
    change: "Đổi mật khẩu",
    unavailable: "Hiện chưa thể đổi mật khẩu tại đây.",
  },
  twoFactor: {
    title: "Xác thực hai yếu tố",
    enabled: "Đã bật",
    disabled: "Chưa bật",
    enable: "Bật xác thực hai yếu tố",
    disable: "Tắt xác thực hai yếu tố",
    unavailable: "Hiện chưa thể quản lý xác thực hai yếu tố tại đây.",
  },
  sessions: {
    title: "Phiên đăng nhập đang hoạt động",
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    count_one: "{{count}} phiên đang hoạt động",
    count_other: "{{count}} phiên đang hoạt động",
  },
};

export function accountSecurityCopy(locale: "vi" | "en"): AccountSecurityCopy {
  return locale === "vi" ? vi : en;
}
