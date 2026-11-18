/**
 * Account API token copy (the Idn-Tokens boards), vi and en, registered as the
 * `account-tokens` catalog. Placeholders use `{{name}}`.
 */
export const en = {
  title: "API tokens",
  description:
    "A token lets a script or tool outside Notrelix act for you. It lasts at most 365 days.",
  create: "New token",
  createFirst: "Create the first token",
  loading: "Loading your tokens…",
  loadError: "We couldn't load the tokens.",
  retry: "Try again",
  loadMore: "Show more",
  columns: { name: "Token name", lastUsed: "Last used", expires: "Expires" },
  neverUsed: "Never used",
  noExpiry: "No expiry",
  noExpiryHint: "Issued without an expiry. Revoke it and issue a new one.",
  expiredTag: "Expired",
  revokedTag: "Revoked",
  revokedReason: {
    "member-removed": "The person who made it was removed from the workspace",
    "member-suspended": "The person who made it was suspended in the workspace",
    "member-left": "The person who made it left the workspace",
  },
  revoke: "Revoke",
  revokeLabel: "Revoke {{name}}",
  banner: {
    title: "Revoking takes effect at once",
    text: "A revoked token stops working within seconds, and every integration that uses it will fail.",
  },
  empty: {
    title: "You have no tokens yet",
    text: "Create a token to connect a CI tool, a report or a script. The full token is shown only once, when you create it.",
  },
  denied: {
    title: "Only workspace owners and admins manage API tokens",
    text: "Ask an owner or admin of this workspace to create the token you need.",
  },
  form: {
    title: "New API token",
    name: "Token name",
    placeholder: "For example: CI deploy",
    nameRequired: "Name the token so you can recognise it later.",
    expiry: "Expires after",
    days_30: "30 days",
    days_90: "90 days",
    days_365: "365 days",
    hint: "At most 365 days.",
    cancel: "Cancel",
    submit: "Create token",
  },
  proof: {
    title: "Confirm it's you",
    text: "This needs a fresh confirmation, even though you are signed in.",
    code: "6-digit code from your authenticator app",
    password: "Current password",
    provider: "Confirm with {{provider}}",
    providerText: "Confirm with the account you signed in with.",
    wrongCode: "That code isn't right. Try again.",
    wrongPassword: "That password isn't right. Try again.",
    wait: "Too many attempts. Wait {{seconds}} seconds.",
    expired: "That confirmation ran out. Confirm again.",
    failed: "We couldn't create the token. Try again.",
    lifetime: "A token can last at most 365 days.",
    confirm: "Confirm",
  },
  reveal: {
    title: "Copy your token",
    warnTitle: "This is the only time you will see the token",
    warnText:
      "After you close this, only the last 4 characters remain. If you lose it, revoke it and create a new one.",
    copy: "Copy token",
    copyClose: "Copy and close",
    close: "Close",
    copyFailed:
      "We couldn't copy it. Select the token and copy it by hand, then close.",
    detail: "{{name}} · expires {{date}}",
  },
  confirmRevoke: {
    title: "Revoke this token?",
    text: "Revoking takes effect at once and can't be undone.",
    warnTitle: "Anything using this token will stop working",
    warnText: "A tool that sends it will be refused from now on.",
    keep: "Keep token",
    action: "Revoke token",
  },
  revoked: "Token revoked",
  gone: "That token was already gone.",
  actionError: "We couldn't do that. Try again.",
};

export type AccountTokensCopy = typeof en;

export const vi: AccountTokensCopy = {
  title: "Token API",
  description:
    "Token cho phép script và công cụ ngoài truy cập Notrelix thay bạn. Hạn tối đa 365 ngày.",
  create: "Tạo token",
  createFirst: "Tạo token đầu tiên",
  loading: "Đang tải token…",
  loadError: "Không tải được danh sách token.",
  retry: "Thử lại",
  loadMore: "Xem thêm",
  columns: { name: "Tên token", lastUsed: "Dùng lần cuối", expires: "Hết hạn" },
  neverUsed: "Chưa dùng",
  noExpiry: "Không hết hạn",
  noExpiryHint:
    "Token này được tạo không có hạn. Hãy thu hồi và tạo token mới.",
  expiredTag: "Đã hết hạn",
  revokedTag: "Đã thu hồi",
  revokedReason: {
    "member-removed": "Người tạo đã bị xoá khỏi workspace",
    "member-suspended": "Người tạo bị tạm khoá trong workspace",
    "member-left": "Người tạo đã rời workspace",
  },
  revoke: "Thu hồi",
  revokeLabel: "Thu hồi {{name}}",
  banner: {
    title: "Thu hồi có hiệu lực ngay",
    text: "Token bị thu hồi sẽ ngừng hoạt động trong vài giây và mọi tích hợp đang dùng nó sẽ lỗi.",
  },
  empty: {
    title: "Bạn chưa có token nào",
    text: "Tạo token để kết nối công cụ CI, báo cáo hoặc script. Token chỉ hiện đầy đủ một lần khi tạo.",
  },
  denied: {
    title: "Chỉ chủ sở hữu và quản trị viên workspace quản lý token API",
    text: "Hãy nhờ chủ sở hữu hoặc quản trị viên của workspace này tạo token bạn cần.",
  },
  form: {
    title: "Tạo token API",
    name: "Tên token",
    placeholder: "Ví dụ: CI triển khai",
    nameRequired: "Hãy đặt tên để nhận ra token này sau này.",
    expiry: "Hết hạn sau",
    days_30: "30 ngày",
    days_90: "90 ngày",
    days_365: "365 ngày",
    hint: "Tối đa 365 ngày.",
    cancel: "Huỷ",
    submit: "Tạo token",
  },
  proof: {
    title: "Xác nhận lại danh tính",
    text: "Thao tác này cần xác nhận lại danh tính, dù bạn đã đăng nhập.",
    code: "Mã 6 số từ ứng dụng xác thực",
    password: "Mật khẩu hiện tại",
    provider: "Xác nhận với {{provider}}",
    providerText: "Hãy xác nhận bằng tài khoản bạn đã dùng để đăng nhập.",
    wrongCode: "Mã chưa đúng. Hãy thử lại.",
    wrongPassword: "Mật khẩu chưa đúng. Hãy thử lại.",
    wait: "Thử quá nhiều lần. Hãy đợi {{seconds}} giây.",
    expired: "Lần xác nhận đó đã hết hạn. Hãy xác nhận lại.",
    failed: "Không tạo được token. Hãy thử lại.",
    lifetime: "Token chỉ có hạn tối đa 365 ngày.",
    confirm: "Xác nhận",
  },
  reveal: {
    title: "Sao chép token của bạn",
    warnTitle: "Đây là lần duy nhất bạn thấy token",
    warnText:
      "Sau khi đóng, chỉ còn 4 ký tự cuối. Mất token thì thu hồi và tạo cái mới.",
    copy: "Sao chép token",
    copyClose: "Sao chép và đóng",
    close: "Đóng",
    copyFailed:
      "Không sao chép được. Hãy chọn token, sao chép thủ công rồi đóng.",
    detail: "{{name}} · hết hạn {{date}}",
  },
  confirmRevoke: {
    title: "Thu hồi token này?",
    text: "Thu hồi có hiệu lực ngay và không hoàn tác được.",
    warnTitle: "Thứ đang dùng token này sẽ ngừng hoạt động",
    warnText: "Công cụ đang gửi token này sẽ bị từ chối.",
    keep: "Giữ token",
    action: "Thu hồi token",
  },
  revoked: "Đã thu hồi token",
  gone: "Token này đã không còn.",
  actionError: "Không thực hiện được. Hãy thử lại.",
};

export function accountTokensCopy(locale: "vi" | "en"): AccountTokensCopy {
  return locale === "vi" ? vi : en;
}
