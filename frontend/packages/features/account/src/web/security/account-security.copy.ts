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
  actionError: "We couldn't do that. Try again.",
  password: {
    title: "Password",
    managed: "Managed by {{workspace}}",
    ssoTitle: "Single sign-on through {{provider}}",
    ssoText:
      "You sign in with your company account {{email}}. The password is changed at {{provider}}, not in Notrelix.",
    ssoLink: "Change password at Microsoft",
    ssoLinkOpens: " (opens a new tab)",
    localTitle: "Your Notrelix password",
    localText: "You sign in to Notrelix with this email and a password.",
    change: "Change password",
    unavailable: "Changing your password isn't available here yet.",
  },
  twoFactor: {
    title: "Two-factor authentication",
    enabled: "On",
    disabled: "Off",
    appTitle: "Authenticator app",
    appText:
      "Enter a 6-digit code from the app each time you sign in on a new device",
    offText:
      "Add a second step to your sign-in: a 6-digit code from an authenticator app.",
    enable: "Turn on two-factor authentication",
    disable: "Turn off two-factor authentication",
    backupTitle: "Backup codes",
    backupText_one:
      "{{count}} backup code left. Each code works once when you don't have your phone.",
    backupText_other:
      "{{count}} backup codes left. Each code works once when you don't have your phone.",
    backupRenew: "Create new codes",
    renewed: "New backup codes created.",
    turnedOn: "Two-factor authentication is on.",
    turnedOff: "Two-factor authentication is off.",
  },
  flow: {
    enrollTitle: "Set up the authenticator app",
    enrollText:
      "Add this key to your authenticator app, then enter the 6-digit code it shows.",
    disableTitle: "Confirm to turn off two-factor authentication",
    renewTitle: "Confirm to create new backup codes",
    codeText:
      "Enter a 6-digit code from your authenticator app or a backup code.",
    passwordText: "Enter your current password.",
    codeLabel: "Code",
    passwordLabel: "Current password",
    confirm: "Confirm",
    cancel: "Cancel",
    wrongCode: "That code is not right. Check the app and try again.",
    wrongPassword: "That password is not right.",
    wait: "Too many tries. Try again in {{seconds}} seconds.",
    codesTitle: "Save your backup codes",
    codesText:
      "Each code works once when you don't have your phone. They are shown only now.",
    done: "I saved them",
  },
  activity: {
    title: "Recent security activity",
    more: "Show more",
    types: {
      SignInSucceeded: "Signed in",
      SignInFailed: "Failed sign-in attempt",
      PasswordChanged: "Password changed",
      PasswordReset: "Password reset",
      MfaEnabled: "Two-factor authentication turned on",
      MfaDisabled: "Two-factor authentication turned off",
      SessionRevoked: "A device was signed out",
      OtherSessionsRevoked: "Other devices were signed out",
    },
  },
  sessions: {
    signedIn: "Signed in {{age}}",
    title: "Active sessions",
    count_one: "{{count}} session is active",
    count_other: "{{count}} sessions are active",
    thisDevice: "This device",
    active: "in use",
    signOut: "Sign out",
    signOutLabel: "Sign out {{device}}",
    signOutThisLabel: "Sign out {{device}} (this device)",
    signedOut: "Signed out.",
  },
  others: {
    title: "Sign out of all other devices",
    text: "{{others}} will have to sign in again. {{current}} stays signed in.",
    and: " and ",
    action: "Sign out of all other devices",
    nobody: "No other device is signed in.",
    done: "Signed out of the other devices.",
  },
  time: {
    now: "just now",
    minutes_one: "{{count}} minute ago",
    minutes_other: "{{count}} minutes ago",
    hours_one: "{{count}} hour ago",
    hours_other: "{{count}} hours ago",
    days_one: "{{count}} day ago",
    days_other: "{{count}} days ago",
  },
};

export type AccountSecurityCopy = typeof en;

export const vi: AccountSecurityCopy = {
  title: "Bảo mật",
  description: "Quản lý mật khẩu và các cài đặt bảo mật của bạn.",
  loading: "Đang tải cài đặt bảo mật của bạn…",
  loadError: "Không thể tải cài đặt bảo mật của bạn.",
  retry: "Thử lại",
  actionError: "Chưa làm được việc này. Hãy thử lại.",
  password: {
    title: "Mật khẩu",
    managed: "Do {{workspace}} quản lý",
    ssoTitle: "Đăng nhập một lần qua {{provider}}",
    ssoText:
      "Bạn đăng nhập bằng tài khoản công ty {{email}}. Mật khẩu đổi ở Microsoft, không đổi ở Notrelix.",
    ssoLink: "Đổi mật khẩu ở Microsoft",
    ssoLinkOpens: " (mở thẻ mới)",
    localTitle: "Mật khẩu Notrelix của bạn",
    localText: "Bạn đăng nhập Notrelix bằng email này và một mật khẩu.",
    change: "Đổi mật khẩu",
    unavailable: "Hiện chưa thể đổi mật khẩu tại đây.",
  },
  twoFactor: {
    title: "Xác thực hai yếu tố",
    enabled: "Đã bật",
    disabled: "Chưa bật",
    appTitle: "Ứng dụng xác thực",
    appText: "Nhập mã 6 số từ ứng dụng mỗi lần đăng nhập trên thiết bị mới",
    offText: "Thêm một bước nữa khi đăng nhập: mã 6 số từ ứng dụng xác thực.",
    enable: "Bật xác thực hai yếu tố",
    disable: "Tắt xác thực hai yếu tố",
    backupTitle: "Mã dự phòng",
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    backupText_one:
      "Còn {{count}} mã dự phòng. Mỗi mã dùng được một lần khi không có điện thoại.",
    backupText_other:
      "Còn {{count}} mã dự phòng. Mỗi mã dùng được một lần khi không có điện thoại.",
    backupRenew: "Tạo mã mới",
    renewed: "Đã tạo mã dự phòng mới.",
    turnedOn: "Đã bật xác thực hai yếu tố.",
    turnedOff: "Đã tắt xác thực hai yếu tố.",
  },
  flow: {
    enrollTitle: "Thiết lập ứng dụng xác thực",
    enrollText:
      "Thêm khóa này vào ứng dụng xác thực, rồi nhập mã 6 số ứng dụng hiển thị.",
    disableTitle: "Xác nhận để tắt xác thực hai lớp",
    renewTitle: "Xác nhận để tạo mã dự phòng mới",
    codeText: "Nhập mã 6 số từ ứng dụng xác thực hoặc một mã dự phòng.",
    passwordText: "Nhập mật khẩu hiện tại của bạn.",
    codeLabel: "Mã",
    passwordLabel: "Mật khẩu hiện tại",
    confirm: "Xác nhận",
    cancel: "Hủy",
    wrongCode: "Mã chưa đúng. Kiểm tra ứng dụng rồi thử lại.",
    wrongPassword: "Mật khẩu chưa đúng.",
    wait: "Thử quá nhiều lần. Thử lại sau {{seconds}} giây.",
    codesTitle: "Lưu mã dự phòng của bạn",
    codesText:
      "Mỗi mã dùng được một lần khi bạn không có điện thoại. Mã chỉ hiện lúc này.",
    done: "Tôi đã lưu",
  },
  activity: {
    title: "Hoạt động bảo mật gần đây",
    more: "Xem thêm",
    types: {
      SignInSucceeded: "Đăng nhập",
      SignInFailed: "Đăng nhập không thành công",
      PasswordChanged: "Đổi mật khẩu",
      PasswordReset: "Đặt lại mật khẩu",
      MfaEnabled: "Bật xác thực hai lớp",
      MfaDisabled: "Tắt xác thực hai lớp",
      SessionRevoked: "Một thiết bị đã bị đăng xuất",
      OtherSessionsRevoked: "Các thiết bị khác đã bị đăng xuất",
    },
  },
  sessions: {
    signedIn: "Đăng nhập {{age}}",
    title: "Phiên đăng nhập đang hoạt động",
    count_one: "{{count}} phiên đang hoạt động",
    count_other: "{{count}} phiên đang hoạt động",
    thisDevice: "Thiết bị này",
    active: "đang dùng",
    signOut: "Đăng xuất",
    signOutLabel: "Đăng xuất {{device}}",
    signOutThisLabel: "Đăng xuất {{device}} (thiết bị này)",
    signedOut: "Đã đăng xuất.",
  },
  others: {
    title: "Đăng xuất mọi thiết bị khác",
    text: "{{others}} phải đăng nhập lại. {{current}} này vẫn giữ đăng nhập.",
    and: " và ",
    action: "Đăng xuất mọi thiết bị khác",
    nobody: "Không còn thiết bị nào khác đang đăng nhập.",
    done: "Đã đăng xuất các thiết bị khác.",
  },
  time: {
    now: "vừa xong",
    minutes_one: "{{count}} phút trước",
    minutes_other: "{{count}} phút trước",
    hours_one: "{{count}} giờ trước",
    hours_other: "{{count}} giờ trước",
    days_one: "{{count}} ngày trước",
    days_other: "{{count}} ngày trước",
  },
};

export function accountSecurityCopy(locale: "vi" | "en"): AccountSecurityCopy {
  return locale === "vi" ? vi : en;
}
