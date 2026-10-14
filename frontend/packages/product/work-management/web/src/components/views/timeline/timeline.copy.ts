/**
 * Timeline view copy, vi and en, registered as the `timeline` catalog
 * (`timeline.catalog.ts`). The pure surface takes a `copy` prop;
 * `useTimelineCopy` resolves it for the active locale in the container.
 * Vietnamese is the mockup wording (`Final-Timeline`). Placeholders use the
 * catalog `{{name}}` form; counts use `_one`/`_other` (Vietnamese keeps both
 * complete).
 */
export const en = {
  today: "Today",
  todayLabel: "Scroll to today",
  previous: "Back",
  next: "Forward",
  scale: {
    label: "Time scale",
    day: "Day",
    week: "Week",
    month: "Month",
  },
  groupBy: { label: "Group by", value: "Item group" },
  barSpan: { label: "Bars run from", value: "Start → Due" },
  links: "Dependencies",
  criticalPath: "Critical path",
  addItem: "Add item",
  /** Controls whose data does not exist yet look like the design and say so. */
  unavailable: {
    groupBy: "Items can only be grouped by item group for now",
    barSpan: "Bars can only run from start to due for now",
    links: "No item has a dependency yet, so there is nothing to show",
    criticalPath: "The critical path needs dependencies between items",
    addItem: "Add items from the Table, Kanban or Calendar view",
  },
  undated_one: "{{count}} item without a date",
  undated_other: "{{count}} items without a date",
  head: { key: "Key", title: "Item", due: "Due" },
  month: {
    /** `{{month}}` is the month number, `{{name}}` its name in the locale. */
    long: "{{name}}",
    short: "{{short}}",
    year: ", {{year}}",
  },
  weekdays: {
    mon: "Mon",
    tue: "Tue",
    wed: "Wed",
    thu: "Thu",
    fri: "Fri",
    sat: "Sat",
    sun: "Sun",
  },
  week: {
    short: "Week {{week}}",
    /** `{{range}}` is "dd–dd/MM" or "dd/MM–dd/MM". */
    full: "Week {{week}} · {{range}}",
  },
  gridLabel: "Timeline of {{board}}, {{from}} to {{to}}/{{year}}",
  kind: { phase: "Phase", milestone: "Milestone" },
  rowLabel: "{{key}} {{title}}",
  bar: {
    /** `{{name}}` is "KEY Title" (or "Phase Title"). */
    label: "{{name}}, {{from}}–{{to}}, {{percent}}{{notes}}",
    overdue_one: ", {{days}} day overdue",
    overdue_other: ", {{days}} days overdue",
    clipped: ", starts before the range shown",
  },
  finished: {
    note: "Done {{date}}",
    before: "◂ Done {{date}}",
    label: "{{name}}, done {{date}}",
    beforeLabel: "{{name}}, done {{date}}, before the range shown",
    after: "Done {{date}} ▸",
  },
  outside: {
    before: "◂ Due {{date}}",
    beforeLabel: "{{name}}, due {{date}}, before the range shown",
    after: "Starts {{date}} ▸",
    afterLabel: "{{name}}, starts {{date}}, after the range shown",
  },
  milestone: {
    label: "Milestone {{title}}, {{date}}",
    title: "{{title}} · {{date}}",
  },
  late_one: "Overdue {{days}} day",
  late_other: "Overdue {{days}} days",
  tip: {
    resize: "Due: {{from}} → {{to}}",
    move: "{{from}} → {{to}}",
    cascade: "{{title}} moves back {{days}} days, to {{date}}",
  },
  keyboard: {
    reschedule:
      "Alt and arrow keys move the bar, Alt, Shift and arrow keys change the due date",
  },
  reschedule: { failed: "Could not move {{title}}" },
  legend: {
    todo: "To do",
    doing: "In progress",
    review: "In review",
    done: "Done",
    overdue: "Overdue",
    milestone: "Milestone",
    link: "An item waits for the one before it to finish",
    /** `{{date}}` is "dd/MM", `{{zone}}` the lower-case zone name. */
    today: "Blue vertical line: today {{date}}, {{zone}}",
  },
  zone: { hanoi: "Hanoi time", local: "local time" },
  empty: {
    title: "No item has a date yet",
    description:
      "Give an item a start and a due date and it appears on the timeline.",
  },
};

export type TimelineCopy = typeof en;

export const vi: TimelineCopy = {
  today: "Hôm nay",
  todayLabel: "Cuộn tới hôm nay",
  previous: "Lùi",
  next: "Tiến",
  scale: {
    label: "Thang thời gian",
    day: "Ngày",
    week: "Tuần",
    month: "Tháng",
  },
  groupBy: { label: "Nhóm theo", value: "Nhóm việc" },
  barSpan: { label: "Thanh dài từ", value: "Bắt đầu → Hạn" },
  links: "Phụ thuộc",
  criticalPath: "Đường găng",
  addItem: "Thêm công việc",
  unavailable: {
    groupBy: "Hiện chỉ nhóm được theo nhóm việc",
    barSpan: "Hiện thanh chỉ chạy từ ngày bắt đầu đến hạn",
    links: "Chưa có việc nào có phụ thuộc nên chưa có gì để hiện",
    criticalPath: "Đường găng cần có phụ thuộc giữa các việc",
    addItem: "Thêm công việc ở chế độ Bảng, Kanban hoặc Lịch",
  },
  undated_one: "{{count}} việc chưa có ngày",
  undated_other: "{{count}} việc chưa có ngày",
  head: { key: "Mã", title: "Công việc", due: "Hạn" },
  month: {
    long: "Tháng {{month}}",
    short: "Th {{month}}",
    year: ", {{year}}",
  },
  weekdays: {
    mon: "T2",
    tue: "T3",
    wed: "T4",
    thu: "T5",
    fri: "T6",
    sat: "T7",
    sun: "CN",
  },
  week: {
    short: "Tuần {{week}}",
    full: "Tuần {{week}} · {{range}}",
  },
  gridLabel: "Dòng thời gian {{board}}, {{from}} đến {{to}}/{{year}}",
  kind: { phase: "Giai đoạn", milestone: "Mốc" },
  rowLabel: "{{key}} {{title}}",
  bar: {
    label: "{{name}}, {{from}}–{{to}}, {{percent}}{{notes}}",
    overdue_one: ", quá hạn {{days}} ngày",
    overdue_other: ", quá hạn {{days}} ngày",
    clipped: ", bắt đầu trước khoảng đang xem",
  },
  finished: {
    note: "Hoàn thành {{date}}",
    before: "◂ Hoàn thành {{date}}",
    label: "{{name}}, hoàn thành {{date}}",
    beforeLabel: "{{name}}, hoàn thành {{date}}, trước khoảng đang xem",
    after: "Hoàn thành {{date}} ▸",
  },
  outside: {
    before: "◂ Hạn {{date}}",
    beforeLabel: "{{name}}, hạn {{date}}, trước khoảng đang xem",
    after: "Bắt đầu {{date}} ▸",
    afterLabel: "{{name}}, bắt đầu {{date}}, sau khoảng đang xem",
  },
  milestone: {
    label: "Mốc {{title}}, {{date}}",
    title: "{{title}} · {{date}}",
  },
  late_one: "Quá hạn {{days}} ngày",
  late_other: "Quá hạn {{days}} ngày",
  tip: {
    resize: "Hạn: {{from}} → {{to}}",
    move: "{{from}} → {{to}}",
    cascade: "{{title}} tự lùi {{days}} ngày, sang {{date}}",
  },
  keyboard: {
    reschedule:
      "Alt và phím mũi tên dời cả thanh, Alt, Shift và phím mũi tên đổi hạn",
  },
  reschedule: { failed: "Không dời được {{title}}" },
  legend: {
    todo: "Cần làm",
    doing: "Đang làm",
    review: "Chờ duyệt",
    done: "Hoàn thành",
    overdue: "Quá hạn",
    milestone: "Mốc",
    link: "Việc sau chờ việc trước xong",
    today: "Vạch dọc màu lam: hôm nay {{date}}, {{zone}}",
  },
  zone: { hanoi: "giờ Hà Nội", local: "giờ địa phương" },
  empty: {
    title: "Chưa có việc nào có ngày",
    description:
      "Đặt ngày bắt đầu và hạn cho một việc, việc đó sẽ hiện trên dòng thời gian.",
  },
};

export function timelineCopy(locale: "vi" | "en"): TimelineCopy {
  return locale === "vi" ? vi : en;
}
