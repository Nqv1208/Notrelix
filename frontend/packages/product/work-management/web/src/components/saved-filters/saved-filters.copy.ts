/**
 * Saved-filters copy, vi and en, registered as the `saved-filters` catalog
 * (`saved-filters.catalog.ts`). The pure surface takes a `copy` prop;
 * `useSavedFiltersCopy` resolves it for the active locale in the container.
 * Vietnamese is the mockup wording. Placeholders use the catalog `{{name}}` form.
 */
import { viewerSaveReason } from "../viewer-save-reason.copy";

export const en = {
  save: {
    trigger: "Save filter",
    title: "Save the current filter",
    summaryLabel: "What is saved",
    name: "Filter name",
    namePlaceholder: "Name this filter",
    visibilityLabel: "Who sees this filter",
    personal: "Only me",
    personalHint: "Default. No one else sees or changes it.",
    shared: "Share with the board",
    sharedHint:
      "Everyone who can see the board sees it; people who can edit views can change it.",
    cancel: "Cancel",
    submit: "Save filter",
    saving: "Saving…",
    nameRequired: "Name the filter first.",
    failed:
      "Could not save: the server did not answer. What you typed is still here.",
    forbidden: "You can only view this board, so you cannot save filters.",
    retry: "Try again",
  },
  menu: {
    trigger: "Saved filters",
    label: "Saved filters",
    mine: "Mine · only you see them",
    shared: "Shared with the board · everyone on the board sees them",
    badgeMine: "Only me",
    badgeShared: "Shared",
    applied: "Applied",
    clear: "Clear the applied filter",
    empty: "No saved filters yet",
    emptyHint: "Filter the work, then press “Save filter” to reuse it later.",
    loading: "Loading saved filters",
    loadFailed:
      "Could not load saved filters. Your current filter still works.",
    retry: "Try again",
    readOnly: viewerSaveReason.en,
    gone: "That filter is gone or you can no longer see it. The list was refreshed.",
  },
  row: {
    apply: "Apply filter {{name}}",
    rename: "Rename {{name}}",
    renameLabel: "New name of the filter",
    renameSave: "Save",
    renameCancel: "Cancel",
    share: "Share {{name}} with the board",
    unshare: "Make {{name}} only mine",
    remove: "Delete {{name}}",
  },
  toast: {
    saved: "Saved filter {{name}}",
    savedShared: "Saved filter {{name}} and shared it with the board",
    renamed: "Renamed to {{name}}",
    shared: "{{name}} is shared with the board",
    unshared: "{{name}} is only yours again",
    deleted: "Deleted filter {{name}}",
    undo: "Undo",
    restored: "Restored filter {{name}}",
    restoreFailed: "Could not restore filter {{name}}",
    failed: "Could not change filter {{name}}. Try again.",
    forbidden: "You do not have the right to change filter {{name}}.",
  },
};

export type SavedFiltersCopy = typeof en;

export const vi: SavedFiltersCopy = {
  save: {
    trigger: "Lưu bộ lọc",
    title: "Lưu bộ lọc hiện tại",
    summaryLabel: "Nội dung được lưu",
    name: "Tên bộ lọc",
    namePlaceholder: "Đặt tên cho bộ lọc",
    visibilityLabel: "Ai thấy bộ lọc này",
    personal: "Chỉ mình tôi",
    personalHint: "Mặc định. Không ai khác thấy hay sửa.",
    shared: "Chia sẻ với board",
    sharedHint:
      "Mọi người xem được board đều thấy; người có quyền sửa view cũng sửa được.",
    cancel: "Huỷ",
    submit: "Lưu bộ lọc",
    saving: "Đang lưu…",
    nameRequired: "Hãy đặt tên cho bộ lọc.",
    failed:
      "Chưa lưu được: máy chủ không phản hồi. Tên và lựa chọn của bạn vẫn còn ở đây.",
    forbidden: "Bạn chỉ xem board này nên chưa lưu được bộ lọc.",
    retry: "Thử lại",
  },
  menu: {
    trigger: "Bộ lọc đã lưu",
    label: "Bộ lọc đã lưu",
    mine: "Của tôi · chỉ mình bạn thấy",
    shared: "Chia sẻ với board · mọi người trong board thấy",
    badgeMine: "Riêng tôi",
    badgeShared: "Chia sẻ",
    applied: "Đang áp dụng",
    clear: "Bỏ bộ lọc đang áp dụng",
    empty: "Chưa có bộ lọc nào",
    emptyHint: "Lọc việc rồi bấm “Lưu bộ lọc” để dùng lại lần sau.",
    loading: "Đang tải bộ lọc đã lưu",
    loadFailed:
      "Chưa tải được bộ lọc đã lưu. Bộ lọc hiện tại của bạn vẫn dùng được.",
    retry: "Thử lại",
    readOnly: viewerSaveReason.vi,
    gone: "Bộ lọc này không còn nữa hoặc bạn không còn thấy nó. Danh sách đã được làm mới.",
  },
  row: {
    apply: "Áp dụng bộ lọc {{name}}",
    rename: "Đổi tên {{name}}",
    renameLabel: "Tên mới của bộ lọc",
    renameSave: "Lưu",
    renameCancel: "Huỷ",
    share: "Chia sẻ {{name}} với board",
    unshare: "Chuyển {{name}} về riêng tôi",
    remove: "Xoá {{name}}",
  },
  toast: {
    saved: "Đã lưu bộ lọc {{name}}",
    savedShared: "Đã lưu bộ lọc {{name}} và chia sẻ với board",
    renamed: "Đã đổi tên thành {{name}}",
    shared: "{{name}} đã chia sẻ với board",
    unshared: "{{name}} lại chỉ mình bạn thấy",
    deleted: "Đã xoá bộ lọc {{name}}",
    undo: "Hoàn tác",
    restored: "Đã khôi phục bộ lọc {{name}}",
    restoreFailed: "Chưa khôi phục được bộ lọc {{name}}",
    failed: "Chưa đổi được bộ lọc {{name}}. Hãy thử lại.",
    forbidden: "Bạn không có quyền đổi bộ lọc {{name}}.",
  },
};

export function savedFiltersCopy(locale: "vi" | "en"): SavedFiltersCopy {
  return locale === "vi" ? vi : en;
}

/** Fills the catalog `{{name}}` placeholders. */
export function fillSavedFiltersCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}
