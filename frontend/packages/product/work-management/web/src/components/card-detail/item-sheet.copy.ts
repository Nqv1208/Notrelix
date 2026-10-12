/**
 * Item side sheet copy, vi and en, registered as the `item-sheet` catalog
 * (`item-sheet.catalog.ts`). The pure sheet surface takes a `copy` prop;
 * `useItemSheetCopy` resolves it for the active locale in the container.
 * Placeholders use the catalog `{{name}}` form and are filled by
 * `fillSheetCopy`; counted messages carry `_one` / `_other` forms.
 */
export const en = {
  /** BCP 47 tag the sheet formats times with. */
  locale: "en",
  dialogLabel: "Item details",
  loading: "Loading item details",
  unavailable: {
    title: "Item unavailable",
    body: "This item could not be loaded or no longer exists.",
    close: "Close",
  },
  head: {
    crumb: "Location",
    previous: "Previous item (K)",
    next: "Next item (J)",
    copyLink: "Copy link",
    openFullPage: "Open full page",
    openFullPageUnavailable: "Items have no page of their own yet",
    more: "More",
    duplicate: "Duplicate",
    archive: "Archive",
    moveToGroup: "Move to group",
    close: "Close (Esc)",
  },
  title: {
    edit: "Edit title",
  },
  properties: {
    group: "Properties",
    status: "Status",
    noStatus: "No status",
    assignee: "Assignee",
    unassigned: "Unassigned",
    assigneeMore: "{{name}} +{{count}}",
    due: "Due",
    noDue: "No due date",
    dueToday: "{{date}} · today",
    dueOverdue_one: "{{date}} · {{count}} day overdue",
    dueOverdue_other: "{{date}} · {{count}} days overdue",
    clearDue: "Clear due date",
    priority: "Priority",
    noPriority: "No priority",
    priorities: {
      urgent: "Urgent",
      high: "High",
      medium: "Medium",
      low: "Low",
    },
    labels: "Label",
    addProperty: "Add property",
    addPropertyUnavailable: "Adding properties is not available yet",
  },
  sections: {
    description: "Description",
    noDescription: "No description yet.",
    childItems: "Sub-items",
    childProgress: "{{done}}/{{total}}",
    noChildItems: "No sub-items yet.",
    childDone: "Done",
    activity: "Activity",
    comments: "Comments {{count}}",
    history: "History",
    noComments: "No comments yet.",
    noHistory: "No activity yet.",
  },
  comment: {
    actions: "Comment actions",
    edit: "Edit",
    delete: "Delete",
    cancel: "Cancel",
    save: "Save",
    editLabel: "Edit comment",
  },
  composer: {
    placeholder: "Write a comment, type @ to mention someone…",
    label: "Write a comment",
    shortcut: "⌘↵",
    submit: "Send",
    mention: "Mention someone",
    searchPeople: "Search people…",
    noPeople: "No people found.",
    people: "People",
  },
  time: {
    justNow: "Just now",
    minutesAgo_one: "{{count}} minute ago",
    minutesAgo_other: "{{count}} minutes ago",
    hoursAgo_one: "{{count}} hour ago",
    hoursAgo_other: "{{count}} hours ago",
    yesterday: "Yesterday, {{time}}",
    daysAgo_one: "{{count}} day ago",
    daysAgo_other: "{{count}} days ago",
  },
  files: {
    add: "Add file",
    search: "Search files",
    searchLabel: "Search files",
    grid: "Grid view",
    list: "List view",
    drop: "Drag files here or click to upload",
    emptyTitle: "No files attached",
    emptyBody:
      "Attach specs, screenshots or handoff files so they stay with the item.",
  },
};

export type ItemSheetCopy = typeof en;

export const vi: ItemSheetCopy = {
  locale: "vi",
  dialogLabel: "Chi tiết công việc",
  loading: "Đang tải chi tiết công việc",
  unavailable: {
    title: "Không mở được công việc",
    body: "Công việc này không tải được hoặc không còn tồn tại.",
    close: "Đóng",
  },
  head: {
    crumb: "Vị trí",
    previous: "Công việc trước (K)",
    next: "Công việc sau (J)",
    copyLink: "Sao chép liên kết",
    openFullPage: "Mở toàn trang",
    openFullPageUnavailable: "Công việc chưa có trang riêng",
    more: "Thêm",
    duplicate: "Nhân bản",
    archive: "Lưu trữ",
    moveToGroup: "Chuyển nhóm",
    close: "Đóng (Esc)",
  },
  title: {
    edit: "Sửa tên công việc",
  },
  properties: {
    group: "Thuộc tính",
    status: "Trạng thái",
    noStatus: "Không có trạng thái",
    assignee: "Người phụ trách",
    unassigned: "Chưa giao",
    assigneeMore: "{{name}} +{{count}}",
    due: "Hạn",
    noDue: "Chưa có hạn",
    dueToday: "{{date}} · hôm nay",
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    dueOverdue_one: "{{date}} · quá hạn {{count}} ngày",
    dueOverdue_other: "{{date}} · quá hạn {{count}} ngày",
    clearDue: "Xoá hạn",
    priority: "Ưu tiên",
    noPriority: "Chưa có ưu tiên",
    priorities: {
      urgent: "Khẩn",
      high: "Cao",
      medium: "Trung bình",
      low: "Thấp",
    },
    labels: "Nhãn",
    addProperty: "Thêm thuộc tính",
    addPropertyUnavailable: "Chưa thêm được thuộc tính mới",
  },
  sections: {
    description: "Mô tả",
    noDescription: "Chưa có mô tả.",
    childItems: "Việc con",
    childProgress: "{{done}}/{{total}}",
    noChildItems: "Chưa có việc con.",
    childDone: "Đã xong",
    activity: "Hoạt động",
    comments: "Bình luận {{count}}",
    history: "Lịch sử",
    noComments: "Chưa có bình luận.",
    noHistory: "Chưa có hoạt động.",
  },
  comment: {
    actions: "Thao tác với bình luận",
    edit: "Sửa",
    delete: "Xoá",
    cancel: "Huỷ",
    save: "Lưu",
    editLabel: "Sửa bình luận",
  },
  composer: {
    placeholder: "Viết bình luận, gõ @ để nhắc ai đó…",
    label: "Viết bình luận",
    shortcut: "⌘↵",
    submit: "Gửi",
    mention: "Nhắc ai đó",
    searchPeople: "Tìm người…",
    noPeople: "Không tìm thấy ai.",
    people: "Mọi người",
  },
  time: {
    justNow: "Vừa xong",
    minutesAgo_one: "{{count}} phút trước",
    minutesAgo_other: "{{count}} phút trước",
    hoursAgo_one: "{{count}} giờ trước",
    hoursAgo_other: "{{count}} giờ trước",
    yesterday: "Hôm qua, {{time}}",
    daysAgo_one: "{{count}} ngày trước",
    daysAgo_other: "{{count}} ngày trước",
  },
  files: {
    add: "Thêm tệp",
    search: "Tìm tệp",
    searchLabel: "Tìm trong tệp",
    grid: "Dạng lưới",
    list: "Dạng danh sách",
    drop: "Kéo tệp vào đây hoặc bấm để tải lên",
    emptyTitle: "Chưa có tệp đính kèm",
    emptyBody:
      "Đính kèm tài liệu, ảnh chụp màn hình hoặc tệp bàn giao để chúng đi cùng công việc.",
  },
};

export function itemSheetCopy(locale: "vi" | "en"): ItemSheetCopy {
  return locale === "vi" ? vi : en;
}

/** Fills `{{name}}` placeholders; an unknown placeholder is left visible. */
export function fillSheetCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralSheetCopy(
  one: string,
  other: string,
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillSheetCopy(count === 1 ? one : other, { ...values, count });
}
