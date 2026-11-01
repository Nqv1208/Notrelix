/**
 * Copy of the archived and deleted boards page, vi and en, registered as the `retired-boards`
 * catalog (`retired-boards.catalog.ts`). Placeholders use the catalog `{{name}}` form.
 */
export const en = {
  title: "Archived and deleted boards",
  intro:
    "Archived boards keep their items and read like any board until a manager unarchives them. A deleted board can be restored by a manager for 30 days, then it is deleted for good.",
  tabs: { label: "Board state", archived: "Archived", deleted: "Deleted" },
  columns: { name: "Board", when: "Since", action: "Action" },
  archivedSince: "Archived {{date}}",
  deletedSince: "Deleted {{date}}",
  restorableUntil: "Restorable until {{date}}",
  open: "Open",
  unarchive: "Unarchive",
  restore: "Restore",
  pending: "Working…",
  emptyArchived: "No archived board. Boards you archive wait here.",
  emptyDeleted: "No deleted board in the last 30 days.",
  loading: "Loading boards…",
  loadFailed: "The boards could not be loaded.",
  retry: "Retry",
  failures: {
    conflict:
      "Someone changed this board first. The list was reloaded, check it and try again.",
    window:
      "The 30-day window to restore this board has passed. It can no longer be restored.",
    denied: "Only a manager of this board can do that.",
    gone: "This board no longer exists. The list was reloaded.",
    failed: "That did not work and nothing changed. Try again.",
  },
  done: {
    unarchive: "The board was unarchived.",
    restore: "The board was restored.",
  },
};

export type RetiredBoardsCopy = typeof en;

export const vi: RetiredBoardsCopy = {
  title: "Board đã lưu trữ và đã xoá",
  intro:
    "Board lưu trữ giữ nguyên việc và đọc được như board bình thường cho đến khi quản lý bỏ lưu trữ. Board đã xoá được quản lý khôi phục trong 30 ngày, sau đó xoá vĩnh viễn.",
  tabs: {
    label: "Trạng thái board",
    archived: "Đã lưu trữ",
    deleted: "Đã xoá",
  },
  columns: { name: "Board", when: "Từ", action: "Thao tác" },
  archivedSince: "Lưu trữ {{date}}",
  deletedSince: "Xoá {{date}}",
  restorableUntil: "Khôi phục được đến {{date}}",
  open: "Mở",
  unarchive: "Bỏ lưu trữ",
  restore: "Khôi phục",
  pending: "Đang xử lý…",
  emptyArchived: "Chưa có board lưu trữ. Board bạn lưu trữ sẽ nằm ở đây.",
  emptyDeleted: "Không có board nào bị xoá trong 30 ngày qua.",
  loading: "Đang tải board…",
  loadFailed: "Chưa tải được danh sách board.",
  retry: "Thử lại",
  failures: {
    conflict:
      "Người khác vừa thay đổi board này trước. Danh sách đã được tải lại, hãy xem rồi thử lại.",
    window:
      "Đã quá 30 ngày khôi phục của board này nên không khôi phục được nữa.",
    denied: "Chỉ quản lý của board này mới làm được thao tác đó.",
    gone: "Board này không còn nữa. Danh sách đã được tải lại.",
    failed: "Chưa làm được và chưa có gì thay đổi. Hãy thử lại.",
  },
  done: { unarchive: "Đã bỏ lưu trữ board.", restore: "Đã khôi phục board." },
};

export function retiredBoardsCopy(locale: "vi" | "en"): RetiredBoardsCopy {
  return locale === "vi" ? vi : en;
}

export function fillRetiredBoardsCopy(
  template: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return template.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}
