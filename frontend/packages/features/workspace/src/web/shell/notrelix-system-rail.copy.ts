/**
 * Notrelix system rail copy, vi and en, registered as the
 * `workspace-system-rail` catalog (`notrelix-system-rail.catalog.ts`).
 * Vietnamese strings are the Final-Space mockup strings; placeholders use
 * `{{name}}`.
 */
export const en = {
  label: "Notrelix system",
  logo: "Notrelix",
  guest: "(guest)",
  unread: "{{count}} unread",
  user: "Signed in as {{name}}",
  create: {
    label: "Add",
    title: "Create or join a workspace",
  },
  allInbox: {
    label: "All",
    title: "Inbox across all workspaces",
    unavailable: "Not available yet",
  },
  search: {
    label: "Search",
    title: "Search across all workspaces",
    unavailable: "Not available yet",
  },
  settings: {
    label: "Settings",
    title: "Account and appearance",
  },
  pin: {
    label: "Keep the Notrelix system rail open",
  },
};

export type NotrelixSystemRailCopy = typeof en;

export const vi: NotrelixSystemRailCopy = {
  label: "Hệ thống Notrelix",
  logo: "Notrelix",
  guest: "(khách)",
  unread: "{{count}} chưa đọc",
  user: "Đang đăng nhập: {{name}}",
  create: {
    label: "Thêm",
    title: "Tạo hoặc tham gia workspace",
  },
  allInbox: {
    label: "Tất cả",
    title: "Hộp thư mọi workspace",
    unavailable: "Chưa khả dụng",
  },
  search: {
    label: "Tìm",
    title: "Tìm trong mọi workspace",
    unavailable: "Chưa khả dụng",
  },
  settings: {
    label: "Cài đặt",
    title: "Tài khoản và giao diện",
  },
  pin: {
    label: "Giữ thanh hệ thống Notrelix luôn mở",
  },
};

export function notrelixSystemRailCopy(
  locale: "vi" | "en",
): NotrelixSystemRailCopy {
  return locale === "vi" ? vi : en;
}
