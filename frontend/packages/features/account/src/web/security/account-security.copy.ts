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
      "Scan this QR code with your authenticator app, then enter the 6-digit code it shows.",
    qrLabel: "QR code for your authenticator app",
    secretLabel: "Can't scan it? Enter this key in the app instead:",
    disableTitle: "Confirm to turn off two-factor authentication",
    renewTitle: "Confirm to create new backup codes",
    othersTitle: "Confirm to sign out of all other devices",
    signOutThisTitle: "Sign out of this device?",
    signOutThisText:
      "You will have to sign in again on this device to use Notrelix.",
    signOutThisConfirm: "Sign out",
    codeText:
      "Enter a 6-digit code from your authenticator app or a backup code.",
    passwordText: "Enter your current password.",
    codeLabel: "Code",
    passwordLabel: "Current password",
    confirm: "Confirm",
    cancel: "Cancel",
    wrongCode:
      "That code is not right, or it was already used. Wait for the next code and try again.",
    wrongPassword: "That password is not right.",
    wait: "Too many tries. Try again in {{seconds}} seconds.",
    codesTitle: "Save your backup codes",
    codesText:
      "Each code works once when you don't have your phone. They are shown only now.",
    done: "I saved them",
  },
  passwordFlow: {
    title: "Change your password",
    text: "Enter your current password, then choose a new one.",
    textWithCode:
      "Enter your current password and a code from your authenticator app, then choose a new password.",
    currentLabel: "Current password",
    currentRequired: "Enter your current password.",
    wrongCurrent: "That password is not right.",
    codeLabel: "Code",
    codeRequired:
      "Enter the code from your authenticator app or a backup code.",
    wrongCode:
      "That code is not right, or it was already used. Wait for the next code and try again.",
    newLabel: "New password",
    newHint:
      "At least {{count}} characters, different from your current password.",
    tooShort: "Use at least {{count}} characters.",
    sameAsCurrent: "Choose a password different from your current one.",
    rejected:
      "We couldn't use that password. Use at least {{count}} characters, different from your current one.",
    confirmLabel: "Repeat the new password",
    mismatch: "The two passwords don't match.",
    signOutNote:
      "When the password changes, you are signed out of every device, this one included.",
    submit: "Change password",
    cancel: "Cancel",
    wait: "Too many tries. Try again in {{seconds}} seconds.",
    doneTitle: "Password changed",
    doneText:
      "You were signed out of every device, including this one. Sign in again with your new password.",
    signInAgain: "Sign in",
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
    gone: "That device was already signed out.",
  },
  others: {
    title: "Sign out of all other devices",
    text: "{{others}} will have to sign in again. {{current}} stays signed in.",
    and: " and ",
    action: "Sign out of all other devices",
    nobody: "No other device is signed in.",
    done: "Signed out of the other devices.",
    doneCount_one: "Signed out of {{count}} other device.",
    doneCount_other: "Signed out of {{count}} other devices.",
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
      "Quét mã QR này bằng ứng dụng xác thực, rồi nhập mã 6 số ứng dụng hiển thị.",
    qrLabel: "Mã QR cho ứng dụng xác thực",
    secretLabel: "Không quét được? Hãy nhập khóa này vào ứng dụng:",
    disableTitle: "Xác nhận để tắt xác thực hai lớp",
    renewTitle: "Xác nhận để tạo mã dự phòng mới",
    othersTitle: "Xác nhận để đăng xuất mọi thiết bị khác",
    signOutThisTitle: "Đăng xuất khỏi thiết bị này?",
    signOutThisText:
      "Bạn sẽ phải đăng nhập lại trên thiết bị này để dùng Notrelix.",
    signOutThisConfirm: "Đăng xuất",
    codeText: "Nhập mã 6 số từ ứng dụng xác thực hoặc một mã dự phòng.",
    passwordText: "Nhập mật khẩu hiện tại của bạn.",
    codeLabel: "Mã",
    passwordLabel: "Mật khẩu hiện tại",
    confirm: "Xác nhận",
    cancel: "Hủy",
    wrongCode:
      "Mã chưa đúng hoặc đã được dùng rồi. Hãy chờ mã tiếp theo rồi thử lại.",
    wrongPassword: "Mật khẩu chưa đúng.",
    wait: "Thử quá nhiều lần. Thử lại sau {{seconds}} giây.",
    codesTitle: "Lưu mã dự phòng của bạn",
    codesText:
      "Mỗi mã dùng được một lần khi bạn không có điện thoại. Mã chỉ hiện lúc này.",
    done: "Tôi đã lưu",
  },
  passwordFlow: {
    title: "Đổi mật khẩu",
    text: "Nhập mật khẩu hiện tại, rồi chọn mật khẩu mới.",
    textWithCode:
      "Nhập mật khẩu hiện tại và mã từ ứng dụng xác thực, rồi chọn mật khẩu mới.",
    currentLabel: "Mật khẩu hiện tại",
    currentRequired: "Hãy nhập mật khẩu hiện tại.",
    wrongCurrent: "Mật khẩu chưa đúng.",
    codeLabel: "Mã",
    codeRequired: "Hãy nhập mã từ ứng dụng xác thực hoặc một mã dự phòng.",
    wrongCode:
      "Mã chưa đúng hoặc đã được dùng rồi. Hãy chờ mã tiếp theo rồi thử lại.",
    newLabel: "Mật khẩu mới",
    newHint: "Ít nhất {{count}} ký tự, khác mật khẩu hiện tại.",
    tooShort: "Hãy dùng ít nhất {{count}} ký tự.",
    sameAsCurrent: "Hãy chọn mật khẩu khác mật khẩu hiện tại.",
    rejected:
      "Chưa dùng được mật khẩu này. Hãy dùng ít nhất {{count}} ký tự và khác mật khẩu hiện tại.",
    confirmLabel: "Nhập lại mật khẩu mới",
    mismatch: "Hai mật khẩu chưa khớp nhau.",
    signOutNote:
      "Khi đổi mật khẩu, bạn sẽ bị đăng xuất khỏi mọi thiết bị, kể cả thiết bị này.",
    submit: "Đổi mật khẩu",
    cancel: "Hủy",
    wait: "Thử quá nhiều lần. Thử lại sau {{seconds}} giây.",
    doneTitle: "Đã đổi mật khẩu",
    doneText:
      "Bạn đã bị đăng xuất khỏi mọi thiết bị, kể cả thiết bị này. Hãy đăng nhập lại bằng mật khẩu mới.",
    signInAgain: "Đăng nhập",
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
    gone: "Thiết bị đó đã được đăng xuất từ trước.",
  },
  others: {
    title: "Đăng xuất mọi thiết bị khác",
    text: "{{others}} phải đăng nhập lại. {{current}} này vẫn giữ đăng nhập.",
    and: " và ",
    action: "Đăng xuất mọi thiết bị khác",
    nobody: "Không còn thiết bị nào khác đang đăng nhập.",
    done: "Đã đăng xuất các thiết bị khác.",
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    doneCount_one: "Đã đăng xuất {{count}} thiết bị khác.",
    doneCount_other: "Đã đăng xuất {{count}} thiết bị khác.",
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
