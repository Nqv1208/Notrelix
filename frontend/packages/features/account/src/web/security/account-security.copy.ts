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
    noneTitle: "No password yet",
    noneText:
      "You sign in with {{providers}}. Set a password to also sign in with {{email}}.",
    set: "Set a password",
  },
  email: {
    title: "Sign-in email",
    pageText: "You sign in with this email and we send security notices to it.",
    current: "Current email",
    placeholder: "name@company.com",
    proofTitle: "Confirm to change your sign-in email",
    label: "New email",
    invalid: "Enter a valid email address.",
    sameAsCurrent: "That is already your sign-in email.",
    submit: "Send confirmation link",
    wait: "Too many attempts. Try again in {{seconds}} seconds.",
    sentTitle: "Check the new inbox",
    sentText:
      "If {{email}} can be used, a link is on its way. Open it within an hour to finish. Until then you keep signing in with your current email.",
    sentNote:
      "When you open the link, every device is signed out and you sign in with the new address.",
    sentDone: "Done",
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
  },
  policy: {
    title: "Account policy",
    required: "Required",
    notRequired: "Not required",
    rowTitle: "Require two-step verification",
    ownerText:
      "When it is on, every member of this account must set up two-step verification at their next sign-in.",
    memberText:
      "Set by the account Owner. When it is on, every member must use two-step verification.",
    require: "Require for everyone",
    stop: "Stop requiring",
    savedOn: "Two-step verification is now required for the account.",
    savedOff: "Two-step verification is no longer required for the account.",
  },
  flow: {
    enrollTitle: "Set up the authenticator app",
    enrollText:
      "Scan this QR code with your authenticator app, then enter the 6-digit code it shows.",
    qrLabel: "QR code for your authenticator app",
    requiredByAccount:
      "Your account requires two-step verification, so it can't be turned off.",
    secretLabel: "Can't scan it? Enter this key in the app instead:",
    disableTitle: "Confirm to turn off two-factor authentication",
    renewTitle: "Confirm to create new backup codes",
    othersTitle: "Confirm to sign out of all other devices",
    linkTitle: "Confirm to connect {{provider}}",
    unlinkTitle: "Confirm to disconnect {{provider}}",
    setPasswordTitle: "Confirm to set a password",
    unlinkText:
      "You will no longer be able to sign in with {{provider}}. Your other ways to sign in stay.",
    enrollProofTitle: "Confirm to turn on two-factor authentication",
    enrollProofText:
      "Once it is on, you are signed out of every other device. This one stays signed in.",
    disableText:
      "Turning it off voids your backup codes and signs you out of every device, this one included.",
    signOutThisTitle: "Sign out of this device?",
    signOutThisText:
      "You will have to sign in again on this device to use Notrelix.",
    signOutThisConfirm: "Sign out",
    codeText:
      "Enter a 6-digit code from your authenticator app or a backup code.",
    passwordText: "Enter your current password.",
    providerText:
      "You signed in with a provider, so there is no password to enter. Confirm it is you with the same account.",
    providerButton: "Confirm with {{provider}}",
    providerFailed:
      "We could not confirm you with {{provider}}. Try again, or use another way to sign in.",
    providerNone:
      "Set a password or turn on two-factor authentication to change security settings.",
    codeLabel: "Code",
    passwordLabel: "Current password",
    confirm: "Confirm",
    cancel: "Cancel",
    wrongCode:
      "That code is not right, or it was already used. Wait for the next code and try again.",
    wrongPassword: "That password is not right.",
    wait: "Too many tries. Try again in {{seconds}} seconds.",
    expired:
      "That verification expired, so we started a new one. Enter a fresh code.",
    codesTitle: "Save your backup codes",
    codesText:
      "Each code works once when you don't have your phone. They are shown only now.",
    codesOthers_one: "{{count}} other device was signed out.",
    codesOthers_other: "{{count}} other devices were signed out.",
    done: "I saved them",
    disabledTitle: "Two-factor authentication is off",
    disabledText:
      "Your backup codes no longer work. You were signed out of every device, including this one. Sign in again.",
    signInAgain: "Sign in",
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
  firstPassword: {
    title: "Set a password",
    text: "Choose a password to sign in with {{email}}. Your other devices are signed out; this one stays signed in.",
    hint: "At least {{count}} characters.",
    rejected:
      "We couldn't use that password. Use at least {{count}} characters.",
    submit: "Set password",
    done: "Password set. You can now sign in with your email and password.",
    already: "A password is already set. Use Change password instead.",
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
      ProviderLinked: "A sign-in account was connected",
      ProviderUnlinked: "A sign-in account was disconnected",
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
  providers: {
    title: "Connected accounts",
    pageText:
      "Connect Google or Microsoft to sign in faster. You always need at least one way to sign in.",
    methodsTitle: "How you sign in",
    passwordTitle: "Password",
    passwordSet: "Set",
    passwordNone: "Not set",
    lastTitle: "{{provider}} is your only way to sign in",
    connected: "Connected",
    connect: "Connect",
    disconnect: "Disconnect",
    connectedText: "You can sign in to Notrelix with this account.",
    notConnectedText: "Not connected. Connect it to sign in with one click.",
    pageTextLast:
      "Connect an account to sign in faster. You always need at least one way to sign in.",
    setPassword: "Set a password",
    lastMethod:
      "Set a password before you disconnect it, or you will not be able to sign in again.",
    linked: "{{provider}} is connected.",
    already: "{{provider}} was already connected.",
    collision:
      "That {{provider}} account is already used with another Notrelix account. Use a different {{provider}} account.",
    failed: "We could not connect {{provider}}. Try again.",
    unlinked: "{{provider}} is disconnected.",
    unlinkRefused:
      "{{provider}} is your only way to sign in, so it cannot be disconnected.",
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
  sso: {
    title: "Single sign-on",
    loading: "Loading single sign-on settings…",
    loadError: "We couldn't load the single sign-on settings.",
    forbidden:
      "Only the account Owner can set up single sign-on. Ask the Owner if you need a change.",
    failed: "We couldn't do that. Try again.",
    forbiddenAction: "Only the account Owner can change this.",
    intro:
      "Let people sign in with your company's identity provider (OpenID Connect). Set up the provider, prove your domain, then decide whether single sign-on is required.",
    tag: {
      notSet: "Not set up",
      draft: "Draft",
      active: "Active",
      disabled: "Disabled",
      required: "Required",
    },
    provider: {
      title: "Identity provider",
      emptyText:
        "No provider yet. Add the issuer address, client ID and client secret from your provider.",
      setUp: "Set up provider",
      edit: "Edit provider",
      remove: "Remove provider",
      removeConfirm: "Remove the provider and its stored secret?",
      removeYes: "Remove",
      removeKeep: "Keep",
      name: "Display name",
      nameHint:
        'Shown on the sign-in page as "Continue with …", for example your company name.',
      issuer: "Issuer URL",
      issuerHint:
        "The https address of the provider, for example https://<provider-host>/<tenant>/v2.0.",
      clientId: "Client ID",
      secret: "Client secret",
      secretSaved: "Secret saved",
      secretKeep: "A secret is saved. Leave this empty to keep it.",
      secretHint: "Stored encrypted. It is never shown again.",
      jit: "Create accounts on first sign-in",
      jitText:
        "When it is on, a person on a verified domain who signs in for the first time joins this account as a member. When it is off, only people who already have an account can sign in.",
      redirect: "Redirect URI",
      redirectText:
        "Register this address at your provider as the redirect (callback) URI.",
      copy: "Copy",
      copied: "Copied",
      copyFailed: "Could not copy. Select the text and copy it.",
      save: "Save provider",
      saving: "Saving…",
      cancel: "Cancel",
      statusActive:
        "Active. People on a verified domain can sign in through it.",
      statusDraft: "Not active yet.",
      statusDisabled: "Disabled. Nobody can sign in through it.",
      draftReason: "Why: {{reason}}",
      issuerLine: "{{issuer}} · client ID {{clientId}}",
      jitOn: "New people are created on first sign-in.",
      jitOff: "Only existing accounts can sign in.",
      savedActive: "Provider saved. It is active.",
      savedDraft: "Provider saved as a draft.",
      removed: "Provider removed.",
      nameRequired: "Enter a display name.",
      issuerInvalid: "Enter the provider's https address.",
      clientIdRequired: "Enter the client ID.",
      secretRequired: "Enter the client secret.",
      invalidIssuer:
        "That provider address cannot be used. Check it and try again.",
      invalid: "Some details are not valid. Check them and try again.",
      inUse: 'Turn off "Require single sign-on" before removing the provider.',
      inUseReplace:
        "Single sign-on is required, so the provider cannot be replaced by one that does not validate. Turn it off first.",
    },
    domains: {
      title: "Domains",
      text: "People whose email is on a verified domain sign in through the provider. Add a domain and prove you own it with a DNS TXT record.",
      empty: "No domain yet.",
      label: "Domain",
      placeholder: "example.com",
      add: "Add domain",
      adding: "Adding…",
      pending: "Pending",
      verified: "Verified",
      rejected: "Not verified",
      claimed: "Claimed elsewhere",
      txtTitle: "Add this TXT record at your DNS provider",
      txtHost: "Host",
      txtValue: "Value",
      txtHint:
        "DNS changes can take from a few minutes up to a day to show. Then press Verify.",
      copyHost: "Copy host of {{domain}}",
      copyValue: "Copy value of {{domain}}",
      verify: "Verify",
      verifyLabel: "Verify {{domain}}",
      verifying: "Checking…",
      stillPending:
        "{{domain}}: we haven't found the record yet. Check it and try again later.",
      verifiedNow: "{{domain}} is verified.",
      added: "{{domain}} added. Add the TXT record, then verify.",
      claimedRow: "{{domain}} is already claimed by another account.",
      claimedHint:
        "It cannot be used here. Remove it, or ask whoever owns it to release it.",
      rejectedHint:
        "This domain could not be verified. Remove it and add it again.",
      remove: "Remove",
      removeLabel: "Remove {{domain}}",
      removeConfirm: "Remove {{domain}}?",
      removeYes: "Remove",
      removeKeep: "Keep",
      removed: "{{domain}} removed.",
      invalid: "Enter a domain such as example.com.",
      publicProvider:
        "Public email providers such as gmail.com cannot be used. Use your company's own domain.",
      claimedAdd: "That domain is already claimed by another account.",
      inUse:
        'Turn off "Require single sign-on" before removing the last verified domain.',
    },
    require: {
      title: "Require single sign-on",
      text: "When it is on, people on a verified domain cannot sign in with a password or a social account. Only the account Owner can still use a password.",
      reason:
        "Turn it on after the provider is Active and at least one domain is Verified.",
      savedOn: "Single sign-on is now required.",
      savedOff: "Single sign-on is no longer required.",
      notReady:
        "It cannot be turned on yet: the provider must be Active and a domain Verified.",
      inUse:
        "It is on, so the active provider and the last verified domain cannot be removed.",
    },
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
    noneTitle: "Chưa có mật khẩu",
    noneText:
      "Bạn đăng nhập bằng {{providers}}. Hãy đặt mật khẩu để đăng nhập cả bằng {{email}}.",
    set: "Đặt mật khẩu",
  },
  email: {
    title: "Email đăng nhập",
    pageText: "Email này dùng để đăng nhập và nhận thông báo bảo mật.",
    current: "Email hiện tại",
    placeholder: "ten@congty.vn",
    proofTitle: "Xác nhận để đổi email đăng nhập",
    label: "Email mới",
    invalid: "Hãy nhập một địa chỉ email hợp lệ.",
    sameAsCurrent: "Đây đã là email đăng nhập của bạn.",
    submit: "Gửi liên kết xác nhận",
    wait: "Thử quá nhiều lần. Hãy thử lại sau {{seconds}} giây.",
    sentTitle: "Hãy kiểm tra hộp thư mới",
    sentText:
      "Nếu dùng được {{email}}, một liên kết đang được gửi tới. Hãy mở trong vòng một giờ để hoàn tất. Cho tới lúc đó bạn vẫn đăng nhập bằng email hiện tại.",
    sentNote:
      "Khi bạn mở liên kết, mọi thiết bị sẽ bị đăng xuất và bạn đăng nhập bằng địa chỉ mới.",
    sentDone: "Xong",
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
  },
  policy: {
    title: "Chính sách tài khoản",
    required: "Bắt buộc",
    notRequired: "Không bắt buộc",
    rowTitle: "Bắt buộc xác minh hai bước",
    ownerText:
      "Khi bật, mọi thành viên của tài khoản phải thiết lập xác minh hai bước ở lần đăng nhập tiếp theo.",
    memberText:
      "Do chủ sở hữu tài khoản đặt. Khi bật, mọi thành viên phải dùng xác minh hai bước.",
    require: "Bắt buộc cho mọi người",
    stop: "Ngừng bắt buộc",
    savedOn: "Tài khoản đã bắt buộc xác minh hai bước.",
    savedOff: "Tài khoản không còn bắt buộc xác minh hai bước.",
  },
  flow: {
    enrollTitle: "Thiết lập ứng dụng xác thực",
    enrollText:
      "Quét mã QR này bằng ứng dụng xác thực, rồi nhập mã 6 số ứng dụng hiển thị.",
    qrLabel: "Mã QR cho ứng dụng xác thực",
    requiredByAccount:
      "Tài khoản của bạn bắt buộc xác minh hai bước nên không thể tắt.",
    secretLabel: "Không quét được? Hãy nhập khóa này vào ứng dụng:",
    disableTitle: "Xác nhận để tắt xác thực hai lớp",
    renewTitle: "Xác nhận để tạo mã dự phòng mới",
    othersTitle: "Xác nhận để đăng xuất mọi thiết bị khác",
    linkTitle: "Xác nhận để liên kết {{provider}}",
    unlinkTitle: "Xác nhận để gỡ liên kết {{provider}}",
    setPasswordTitle: "Xác nhận để đặt mật khẩu",
    unlinkText:
      "Bạn sẽ không còn đăng nhập được bằng {{provider}}. Các cách đăng nhập khác vẫn dùng được.",
    enrollProofTitle: "Xác nhận để bật xác thực hai yếu tố",
    enrollProofText:
      "Khi bật xong, bạn sẽ bị đăng xuất khỏi mọi thiết bị khác. Thiết bị này vẫn giữ đăng nhập.",
    disableText:
      "Tắt xác thực hai lớp sẽ vô hiệu các mã dự phòng và đăng xuất bạn khỏi mọi thiết bị, kể cả thiết bị này.",
    signOutThisTitle: "Đăng xuất khỏi thiết bị này?",
    signOutThisText:
      "Bạn sẽ phải đăng nhập lại trên thiết bị này để dùng Notrelix.",
    signOutThisConfirm: "Đăng xuất",
    codeText: "Nhập mã 6 số từ ứng dụng xác thực hoặc một mã dự phòng.",
    passwordText: "Nhập mật khẩu hiện tại của bạn.",
    providerText:
      "Bạn đăng nhập bằng nhà cung cấp nên không có mật khẩu để nhập. Hãy xác nhận đó là bạn bằng cùng tài khoản đó.",
    providerButton: "Xác nhận bằng {{provider}}",
    providerFailed:
      "Chưa xác nhận được bạn với {{provider}}. Hãy thử lại hoặc dùng cách đăng nhập khác.",
    providerNone:
      "Hãy đặt mật khẩu hoặc bật xác thực hai lớp để đổi cài đặt bảo mật.",
    codeLabel: "Mã",
    passwordLabel: "Mật khẩu hiện tại",
    confirm: "Xác nhận",
    cancel: "Hủy",
    wrongCode:
      "Mã chưa đúng hoặc đã được dùng rồi. Hãy chờ mã tiếp theo rồi thử lại.",
    wrongPassword: "Mật khẩu chưa đúng.",
    wait: "Thử quá nhiều lần. Thử lại sau {{seconds}} giây.",
    expired: "Lần xác minh đó đã hết hạn nên đã mở lần mới. Hãy nhập mã mới.",
    codesTitle: "Lưu mã dự phòng của bạn",
    codesText:
      "Mỗi mã dùng được một lần khi bạn không có điện thoại. Mã chỉ hiện lúc này.",
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    codesOthers_one: "Đã đăng xuất {{count}} thiết bị khác.",
    codesOthers_other: "Đã đăng xuất {{count}} thiết bị khác.",
    done: "Tôi đã lưu",
    disabledTitle: "Đã tắt xác thực hai lớp",
    disabledText:
      "Các mã dự phòng không còn dùng được. Bạn đã bị đăng xuất khỏi mọi thiết bị, kể cả thiết bị này. Hãy đăng nhập lại.",
    signInAgain: "Đăng nhập",
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
  firstPassword: {
    title: "Đặt mật khẩu",
    text: "Hãy chọn mật khẩu để đăng nhập bằng {{email}}. Các thiết bị khác sẽ bị đăng xuất; thiết bị này vẫn đăng nhập.",
    hint: "Ít nhất {{count}} ký tự.",
    rejected: "Chưa dùng được mật khẩu này. Hãy dùng ít nhất {{count}} ký tự.",
    submit: "Đặt mật khẩu",
    done: "Đã đặt mật khẩu. Từ giờ bạn có thể đăng nhập bằng email và mật khẩu.",
    already: "Đã có mật khẩu. Hãy dùng Đổi mật khẩu.",
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
      ProviderLinked: "Đã liên kết một tài khoản đăng nhập",
      ProviderUnlinked: "Đã gỡ liên kết một tài khoản đăng nhập",
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
  providers: {
    title: "Tài khoản liên kết",
    pageText:
      "Liên kết để đăng nhập nhanh bằng tài khoản Google hoặc Microsoft. Bạn luôn cần ít nhất một cách đăng nhập.",
    methodsTitle: "Cách đăng nhập của bạn",
    passwordTitle: "Mật khẩu",
    passwordSet: "Đã đặt",
    passwordNone: "Chưa đặt",
    lastTitle: "{{provider}} là cách đăng nhập duy nhất của bạn",
    connected: "Đã liên kết",
    connect: "Liên kết",
    disconnect: "Gỡ liên kết",
    connectedText: "Dùng để đăng nhập Notrelix.",
    notConnectedText: "Chưa liên kết",
    pageTextLast:
      "Liên kết để đăng nhập nhanh. Bạn luôn cần ít nhất một cách đăng nhập.",
    setPassword: "Đặt mật khẩu",
    lastMethod:
      "Đặt mật khẩu trước khi gỡ liên kết, nếu không bạn sẽ không đăng nhập lại được.",
    linked: "Đã liên kết {{provider}}.",
    already: "{{provider}} đã được liên kết từ trước.",
    collision:
      "Tài khoản {{provider}} đó đang dùng cho một tài khoản Notrelix khác. Hãy dùng tài khoản {{provider}} khác.",
    failed: "Không liên kết được {{provider}}. Hãy thử lại.",
    unlinked: "Đã gỡ liên kết {{provider}}.",
    unlinkRefused:
      "{{provider}} là cách đăng nhập duy nhất của bạn nên không thể gỡ liên kết.",
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
  sso: {
    title: "Đăng nhập một lần",
    loading: "Đang tải cài đặt đăng nhập một lần…",
    loadError: "Không tải được cài đặt đăng nhập một lần.",
    forbidden:
      "Chỉ chủ sở hữu tài khoản mới thiết lập được đăng nhập một lần. Hãy nhờ chủ sở hữu nếu bạn cần thay đổi.",
    failed: "Chưa thực hiện được. Hãy thử lại.",
    forbiddenAction: "Chỉ chủ sở hữu tài khoản mới đổi được mục này.",
    intro:
      "Cho phép mọi người đăng nhập bằng nhà cung cấp danh tính của công ty (OpenID Connect). Thiết lập nhà cung cấp, xác minh tên miền, rồi quyết định có bắt buộc đăng nhập một lần hay không.",
    tag: {
      notSet: "Chưa thiết lập",
      draft: "Bản nháp",
      active: "Đang hoạt động",
      disabled: "Đã tắt",
      required: "Bắt buộc",
    },
    provider: {
      title: "Nhà cung cấp danh tính",
      emptyText:
        "Chưa có nhà cung cấp. Nhập địa chỉ issuer, client ID và client secret từ nhà cung cấp của bạn.",
      setUp: "Thiết lập nhà cung cấp",
      edit: "Sửa nhà cung cấp",
      remove: "Gỡ nhà cung cấp",
      removeConfirm: "Gỡ nhà cung cấp và secret đã lưu?",
      removeYes: "Gỡ",
      removeKeep: "Giữ lại",
      name: "Tên hiển thị",
      nameHint:
        'Hiện ở trang đăng nhập dạng "Tiếp tục với …", ví dụ tên công ty.',
      issuer: "Địa chỉ issuer",
      issuerHint:
        "Địa chỉ https của nhà cung cấp, ví dụ https://<provider-host>/<tenant>/v2.0.",
      clientId: "Client ID",
      secret: "Client secret",
      secretSaved: "Đã lưu secret",
      secretKeep: "Đã lưu một secret. Để trống nếu muốn giữ nguyên.",
      secretHint: "Được lưu mã hóa và không bao giờ hiện lại.",
      jit: "Tạo tài khoản ở lần đăng nhập đầu tiên",
      jitText:
        "Khi bật, người thuộc tên miền đã xác minh đăng nhập lần đầu sẽ vào tài khoản này với vai trò thành viên. Khi tắt, chỉ người đã có tài khoản mới đăng nhập được.",
      redirect: "Redirect URI",
      redirectText:
        "Đăng ký địa chỉ này ở nhà cung cấp làm redirect (callback) URI.",
      copy: "Sao chép",
      copied: "Đã sao chép",
      copyFailed: "Không sao chép được. Hãy chọn văn bản rồi sao chép.",
      save: "Lưu nhà cung cấp",
      saving: "Đang lưu…",
      cancel: "Hủy",
      statusActive:
        "Đang hoạt động. Người thuộc tên miền đã xác minh đăng nhập qua đây được.",
      statusDraft: "Chưa hoạt động.",
      statusDisabled: "Đã tắt. Không ai đăng nhập qua đây được.",
      draftReason: "Lý do: {{reason}}",
      issuerLine: "{{issuer}} · client ID {{clientId}}",
      jitOn: "Người mới được tạo tài khoản ở lần đăng nhập đầu.",
      jitOff: "Chỉ tài khoản đã có mới đăng nhập được.",
      savedActive: "Đã lưu nhà cung cấp. Nhà cung cấp đang hoạt động.",
      savedDraft: "Đã lưu nhà cung cấp dưới dạng bản nháp.",
      removed: "Đã gỡ nhà cung cấp.",
      nameRequired: "Nhập tên hiển thị.",
      issuerInvalid: "Nhập địa chỉ https của nhà cung cấp.",
      clientIdRequired: "Nhập client ID.",
      secretRequired: "Nhập client secret.",
      invalidIssuer:
        "Không dùng được địa chỉ nhà cung cấp này. Hãy kiểm tra rồi thử lại.",
      invalid: "Một số thông tin chưa hợp lệ. Hãy kiểm tra rồi thử lại.",
      inUse: 'Hãy tắt "Bắt buộc đăng nhập một lần" trước khi gỡ nhà cung cấp.',
      inUseReplace:
        "Đang bắt buộc đăng nhập một lần nên không thể thay nhà cung cấp bằng một nhà cung cấp chưa hợp lệ. Hãy tắt bắt buộc trước.",
    },
    domains: {
      title: "Tên miền",
      text: "Người có email thuộc tên miền đã xác minh sẽ đăng nhập qua nhà cung cấp. Thêm tên miền và chứng minh bạn sở hữu nó bằng bản ghi DNS TXT.",
      empty: "Chưa có tên miền.",
      label: "Tên miền",
      placeholder: "example.com",
      add: "Thêm tên miền",
      adding: "Đang thêm…",
      pending: "Chờ xác minh",
      verified: "Đã xác minh",
      rejected: "Chưa xác minh được",
      claimed: "Đã có tài khoản khác",
      txtTitle: "Thêm bản ghi TXT này ở nhà cung cấp DNS của bạn",
      txtHost: "Host",
      txtValue: "Giá trị",
      txtHint:
        "DNS có thể mất vài phút đến một ngày mới có hiệu lực. Sau đó bấm Xác minh.",
      copyHost: "Sao chép host của {{domain}}",
      copyValue: "Sao chép giá trị của {{domain}}",
      verify: "Xác minh",
      verifyLabel: "Xác minh {{domain}}",
      verifying: "Đang kiểm tra…",
      stillPending:
        "{{domain}}: chưa thấy bản ghi. Hãy kiểm tra rồi thử lại sau.",
      verifiedNow: "{{domain}} đã được xác minh.",
      added: "Đã thêm {{domain}}. Hãy thêm bản ghi TXT rồi xác minh.",
      claimedRow:
        "{{domain}} đã được một tài khoản khác xác nhận quyền sở hữu.",
      claimedHint:
        "Không dùng được ở đây. Hãy gỡ nó, hoặc nhờ bên đang sở hữu giải phóng.",
      rejectedHint: "Không xác minh được tên miền này. Hãy gỡ rồi thêm lại.",
      remove: "Gỡ",
      removeLabel: "Gỡ {{domain}}",
      removeConfirm: "Gỡ {{domain}}?",
      removeYes: "Gỡ",
      removeKeep: "Giữ lại",
      removed: "Đã gỡ {{domain}}.",
      invalid: "Nhập tên miền, ví dụ example.com.",
      publicProvider:
        "Không dùng được nhà cung cấp email công cộng như gmail.com. Hãy dùng tên miền riêng của công ty.",
      claimedAdd: "Tên miền này đã được một tài khoản khác xác nhận.",
      inUse:
        'Hãy tắt "Bắt buộc đăng nhập một lần" trước khi gỡ tên miền đã xác minh cuối cùng.',
    },
    require: {
      title: "Bắt buộc đăng nhập một lần",
      text: "Khi bật, người thuộc tên miền đã xác minh không thể đăng nhập bằng mật khẩu hay tài khoản mạng xã hội. Chỉ chủ sở hữu tài khoản vẫn dùng được mật khẩu.",
      reason:
        "Bật sau khi nhà cung cấp đang hoạt động và có ít nhất một tên miền đã xác minh.",
      savedOn: "Đã bắt buộc đăng nhập một lần.",
      savedOff: "Không còn bắt buộc đăng nhập một lần.",
      notReady:
        "Chưa bật được: nhà cung cấp phải đang hoạt động và có tên miền đã xác minh.",
      inUse:
        "Đang bật nên không gỡ được nhà cung cấp đang hoạt động và tên miền đã xác minh cuối cùng.",
    },
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
