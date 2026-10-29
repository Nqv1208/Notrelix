import {
  en as kanbanToolbarEn,
  vi as kanbanToolbarVi,
  type KanbanToolbarCopy,
} from "../kanban/kanban-toolbar.copy";

/**
 * Calendar view copy, vi and en, registered as the `calendar` catalog
 * (`calendar.catalog.ts`). The pure surface takes a `copy` prop;
 * `useCalendarCopy` resolves it for the active locale in the container.
 * Vietnamese is the mockup wording (`Final-Calendar`). Placeholders use the
 * catalog `{{name}}` form; counts use `_one`/`_other` (Vietnamese keeps both
 * complete).
 */
export const en = {
  priority: {
    urgent: "Priority Urgent",
    high: "Priority High",
    medium: "Priority Medium",
    low: "Priority Low",
  },
  blockedBy: "blocked by {{count}} items",
  today: "Today",
  todayMarker: "Today",
  previous: {
    month: "Previous month",
    week: "Previous week",
    day: "Previous day",
  },
  next: {
    month: "Next month",
    week: "Next week",
    day: "Next day",
  },
  heading: {
    /** `{{month}}` is the month name of the locale. */
    month: "{{month}} {{year}}",
    /** `{{from}}`/`{{to}}` are "dd/MM", `{{year}}` the year of `to`. */
    week: "{{from}} – {{to}}/{{year}}",
    /** `{{weekday}}` is the weekday name of the locale. */
    day: "{{weekday}}, {{date}}",
  },
  gridLabel: {
    month: "Calendar for {{month}} {{year}}",
    week: "Calendar for the week {{from}} to {{to}}/{{year}}",
    day: "Calendar for {{date}}, {{zone}}",
  },
  ranges: {
    label: "Range shown",
    month: "Month",
    week: "Week",
    day: "Day",
  },
  basis: {
    label: "Order by",
    span: "Start → Due",
    due: "Due",
  },
  filter: {
    ...kanbanToolbarEn.filter,
    chip: { ...kanbanToolbarEn.filter.chip, assignee: "Assigned to {{name}}" },
    /** Before the summary of an active filter ("Overdue · Low priority"). */
    activePrefix: "Filter:",
  },
  filterSeparator: " · ",
  zone: {
    hanoi: "Hanoi time",
    local: "Local time",
    /** `{{zone}}` is `zone.hanoi` or `zone.local`, `{{offset}}` such as "GMT+7". */
    label: "{{zone}} ({{offset}})",
  },
  addItem: "Add item",
  weekdays: {
    mon: "Mon",
    tue: "Tue",
    wed: "Wed",
    thu: "Thu",
    fri: "Fri",
    sat: "Sat",
    sun: "Sun",
  },
  kind: {
    phase: "Phase",
    milestone: "Milestone",
  },
  /** Accessible notes after an entry's title. */
  note: {
    due: "due {{date}}",
    dueToday: "due today",
    done: "completed {{date}}",
    completed: "completed",
    review: "in review",
    range: "starts {{start}}",
    overdue_one: "{{days}} day overdue",
    overdue_other: "{{days}} days overdue",
    separator: ", ",
  },
  more: {
    /** "+N" button of a crowded day; the label names the day. */
    label_one: "+{{count}} more item",
    label_other: "+{{count}} more items",
    open: "{{count}} more items on {{date}}, open the day",
  },
  quickCreate: {
    placeholder: "Item name…",
    /** `{{date}}` is "dd/MM". */
    label: "Name of the new item, due {{date}}",
    hint: "Enter to create · due {{date}}",
    failed: "Could not create {{title}}",
  },
  dragTip: {
    /** `{{from}}` and `{{to}}` are "dd/MM". */
    reschedule: "Change due date: {{from}} → {{to}}",
    setDue: "Set due date: {{to}}",
  },
  reschedule: {
    failed: "Could not move {{title}}",
  },
  handle: {
    start: "Change the start day of {{key}}",
    due: "Change the due day of {{key}}",
    readOnly: "You can only view this board",
    readOnlyNote: "Items cannot be dragged or resized to change the schedule",
  },
  resizeTip: {
    start: "New start {{to}} · {{days}} days",
    due: "New due {{to}} · {{days}} days",
  },
  menu: {
    open: "Open details",
    changeDates: "Change dates…",
    changeDatesNote: "Set start and due with the keyboard",
    unschedule: "Move to Unscheduled",
    unscheduleNote: "Clears both dates",
  },
  dates: {
    title: "Change dates",
    start: "Start",
    due: "Due",
    cancel: "Cancel",
    save: "Save",
    invalid: "Due cannot be before the start day.",
    invalidHint: "Choose a due day from {{start}} on.",
  },
  scheduleToast: {
    changed: "Moved {{key}} to {{from}} – {{to}}",
    unscheduled: "Moved {{key}} to Unscheduled",
    scheduled: "Scheduled {{key}} on {{day}}",
    undo: "Undo",
    undone: "Undone",
    undoFailed: "Could not undo",
    failed: "Could not save the dates of {{key}}",
    failedNote: "You are offline. The new dates stay shown so you can retry.",
    retry: "Retry",
    conflict: "{{key}} was just changed by someone else",
    conflictNote:
      "Reload to see the latest dates, then change them again if needed.",
    reload: "Reload",
  },
  agenda: {
    overdue: "Overdue",
    allDay: "All day",
    due: "Due {{date}} · {{time}}",
    dueDate: "Due {{date}}",
    dueTime: "Due {{time}}",
    overdueBy_one: "{{days}} day overdue",
    overdueBy_other: "{{days}} days overdue",
    span: "{{from}} → {{to}} · in progress",
    spanPast: "{{from}} → {{to}}",
    now: "Now {{time}}",
  },
  undated: {
    region: "Items without a date",
    none: "Every item has a date",
    title_one: "{{count}} item without a date",
    title_other: "{{count}} items without a date",
    hint: "Drag it into the calendar or choose Set date.",
    hiddenByFilter_one: "This item does not match the filter, so it is hidden.",
    hiddenByFilter_other:
      "These items do not match the filter, so they are hidden.",
    unassigned: "Unassigned",
    setDue: "Set date",
    /** `{{key}}` is the item key or its title. */
    setDueLabel: "Set a date for {{key}}",
    dragLabel: "Drag {{key}} into the calendar",
  },
  legend: {
    title: "Legend",
    todo: "To do",
    doing: "In progress",
    review: "In review",
    done: "Done",
    overdue: "Overdue",
    milestone: "Project milestone",
  },
  noResults: {
    title: "No items match the filter",
    /** `{{range}}` is `noResults.range.*`. */
    description:
      "No items match the filter this {{range}}. Clear the filter to see everything again.",
    range: { month: "month", week: "week", day: "day" },
    clear: "Clear filter",
  },
  keyboard: {
    /** Announced on a movable entry. */
    reschedule: "Alt and arrow keys move the due date",
  },
  separator: " · ",
};

export type CalendarCopy = typeof en;

export const vi: CalendarCopy = {
  priority: {
    urgent: "Ưu tiên Khẩn",
    high: "Ưu tiên Cao",
    medium: "Ưu tiên Trung bình",
    low: "Ưu tiên Thấp",
  },
  blockedBy: "bị chặn bởi {{count}} việc",
  today: "Hôm nay",
  todayMarker: "Hôm nay",
  previous: {
    month: "Tháng trước",
    week: "Tuần trước",
    day: "Ngày trước",
  },
  next: {
    month: "Tháng sau",
    week: "Tuần sau",
    day: "Ngày sau",
  },
  heading: {
    month: "{{month}}, {{year}}",
    week: "{{from}} – {{to}}/{{year}}",
    day: "{{weekday}}, {{date}}",
  },
  gridLabel: {
    month: "Lịch {{monthLower}} năm {{year}}",
    week: "Lịch tuần {{from}} đến {{to}}/{{year}}",
    day: "Lịch ngày {{date}}, {{zone}}",
  },
  ranges: {
    label: "Khoảng hiển thị",
    month: "Tháng",
    week: "Tuần",
    day: "Ngày",
  },
  basis: {
    label: "Xếp theo",
    span: "Bắt đầu → Hạn",
    due: "Hạn",
  },
  filter: {
    ...kanbanToolbarVi.filter,
    chip: { ...kanbanToolbarVi.filter.chip, assignee: "Giao cho {{name}}" },
    activePrefix: "Lọc:",
  },
  filterSeparator: " · ",
  zone: {
    hanoi: "Giờ Hà Nội",
    local: "Giờ địa phương",
    label: "{{zone}} ({{offset}})",
  },
  addItem: "Thêm công việc",
  weekdays: {
    mon: "Thứ 2",
    tue: "Thứ 3",
    wed: "Thứ 4",
    thu: "Thứ 5",
    fri: "Thứ 6",
    sat: "Thứ 7",
    sun: "Chủ nhật",
  },
  kind: {
    phase: "Giai đoạn",
    milestone: "Mốc",
  },
  note: {
    due: "hạn {{date}}",
    dueToday: "hạn hôm nay",
    done: "hoàn thành {{date}}",
    completed: "hoàn thành",
    review: "chờ duyệt",
    range: "bắt đầu {{start}}",
    // Vietnamese has no singular/plural distinction; both forms stay complete.
    overdue_one: "quá hạn {{days}} ngày",
    overdue_other: "quá hạn {{days}} ngày",
    separator: ", ",
  },
  more: {
    label_one: "+{{count}} việc",
    label_other: "+{{count}} việc",
    open: "{{count}} việc nữa trong ngày {{date}}, mở ngày này",
  },
  quickCreate: {
    placeholder: "Tên công việc…",
    label: "Tên công việc mới, hạn {{date}}",
    hint: "Enter để tạo · hạn {{date}}",
    failed: "Không tạo được {{title}}",
  },
  dragTip: {
    reschedule: "Đổi hạn: {{from}} → {{to}}",
    setDue: "Đặt hạn: {{to}}",
  },
  reschedule: {
    failed: "Không đổi được hạn của {{title}}",
  },
  handle: {
    start: "Đổi ngày bắt đầu của {{key}}",
    due: "Đổi hạn của {{key}}",
    readOnly: "Bạn chỉ có quyền xem board này",
    readOnlyNote: "Không thể kéo hay đổi kích thước để đổi lịch",
  },
  resizeTip: {
    start: "Bắt đầu mới {{to}} · {{days}} ngày",
    due: "Hạn mới {{to}} · {{days}} ngày",
  },
  menu: {
    open: "Mở chi tiết",
    changeDates: "Đổi ngày…",
    changeDatesNote: "Đặt bắt đầu và hạn bằng bàn phím",
    unschedule: "Chuyển về Chưa lên lịch",
    unscheduleNote: "Xoá cả hai ngày",
  },
  dates: {
    title: "Đổi ngày",
    start: "Bắt đầu",
    due: "Hạn",
    cancel: "Huỷ",
    save: "Lưu",
    invalid: "Hạn không được trước ngày bắt đầu.",
    invalidHint: "Chọn hạn từ {{start}} trở đi.",
  },
  scheduleToast: {
    changed: "Đã dời {{key}} sang {{from}} đến {{to}}",
    unscheduled: "Đã chuyển {{key}} về Chưa lên lịch",
    scheduled: "Đã đặt {{key}} vào {{day}}",
    undo: "Hoàn tác",
    undone: "Đã hoàn tác",
    undoFailed: "Chưa hoàn tác được",
    failed: "Chưa lưu được ngày của {{key}}",
    failedNote: "Mất kết nối. Ngày mới vẫn hiển thị để bạn thử lại.",
    retry: "Thử lại",
    conflict: "{{key}} vừa được người khác đổi ngày",
    conflictNote: "Tải lại để xem ngày mới nhất, rồi đổi lại nếu cần.",
    reload: "Tải lại",
  },
  agenda: {
    overdue: "Quá hạn",
    allDay: "Cả ngày",
    due: "Hạn {{date}} · {{time}}",
    dueDate: "Hạn {{date}}",
    dueTime: "Hạn {{time}}",
    overdueBy_one: "Quá hạn {{days}} ngày",
    overdueBy_other: "Quá hạn {{days}} ngày",
    span: "{{from}} → {{to}} · đang diễn ra",
    spanPast: "{{from}} → {{to}}",
    now: "Bây giờ {{time}}",
  },
  undated: {
    region: "Công việc chưa có ngày",
    none: "Mọi việc đều có ngày",
    title_one: "{{count}} việc chưa có ngày",
    title_other: "{{count}} việc chưa có ngày",
    hint: "Kéo vào lịch hoặc bấm Đặt ngày.",
    hiddenByFilter_one: "Việc này không khớp bộ lọc nên đang ẩn.",
    hiddenByFilter_other: "Các việc này không khớp bộ lọc nên đang ẩn.",
    unassigned: "Chưa giao",
    setDue: "Đặt ngày",
    setDueLabel: "Đặt ngày cho {{key}}",
    dragLabel: "Kéo {{key}} vào lịch",
  },
  legend: {
    title: "Chú thích",
    todo: "Cần làm",
    doing: "Đang làm",
    review: "Chờ duyệt",
    done: "Hoàn thành",
    overdue: "Quá hạn",
    milestone: "Mốc dự án",
  },
  noResults: {
    title: "Không có việc nào khớp bộ lọc",
    description:
      "Không việc nào khớp bộ lọc trong {{range}} này. Bỏ lọc để xem lại toàn bộ.",
    range: { month: "tháng", week: "tuần", day: "ngày" },
    clear: "Bỏ lọc",
  },
  keyboard: {
    reschedule: "Alt cùng phím mũi tên để dời hạn",
  },
  separator: " · ",
};

export function calendarCopy(locale: "vi" | "en"): CalendarCopy {
  return locale === "vi" ? vi : en;
}

/** The filter copy the shared Kanban filter menu and summary read. */
export function calendarFilterCopy(
  copy: CalendarCopy,
): KanbanToolbarCopy["filter"] {
  return copy.filter;
}
