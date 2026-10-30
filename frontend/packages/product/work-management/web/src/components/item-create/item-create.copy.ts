/** Words of the "Add item" dialog (Vietnamese is the product's first language). */
export type ItemCreateCopy = {
  readonly title: string;
  readonly note: string;
  readonly name: string;
  readonly namePlaceholder: string;
  readonly nameRequired: string;
  readonly description: string;
  readonly descriptionPlaceholder: string;
  readonly board: string;
  readonly group: string;
  readonly status: string;
  readonly priority: string;
  readonly noPriority: string;
  readonly assignees: string;
  readonly noMembers: string;
  readonly startDate: string;
  readonly dueDate: string;
  readonly dueBeforeStart: string;
  readonly labels: string;
  readonly noLabels: string;
  readonly another: string;
  readonly cancel: string;
  readonly submit: string;
  readonly submitting: string;
  readonly failed: string;
  readonly created: string;
  readonly createdWithout: string;
  readonly detailNames: Readonly<
    Record<"status" | "lane" | "priority" | "assignees" | "labels", string>
  >;
  /** "Từ cột Cần làm": where the form was opened from. */
  readonly fromLane: string;
  readonly fromQuickAdd: string;
  readonly fromOutside: string;
  readonly close: string;
  readonly optional: string;
  readonly required: string;
  readonly dateFormat: string;
  readonly dateInvalid: string;
  readonly pickDate: string;
  readonly quickDue: string;
  readonly today: string;
  readonly tomorrow: string;
  readonly nextWeek: string;
  /** "{{weekday}} {{date}}" chip for the coming Friday. */
  readonly weekdayChip: string;
  readonly dueIn: string;
  readonly dueTodayHint: string;
  readonly dueTomorrowHint: string;
  readonly dueOverdueHint: string;
  readonly weekdays: Readonly<
    Record<"sun" | "mon" | "tue" | "wed" | "thu" | "fri" | "sat", string>
  >;
  readonly addPerson: string;
  readonly assignMe: string;
  readonly removePerson: string;
  readonly searchPeople: string;
  readonly noPeopleFound: string;
  readonly anotherKept: string;
  readonly addedStrip: string;
  readonly open: string;
  readonly undo: string;
  readonly undone: string;
  readonly undoFailed: string;
  readonly noGroup: string;
  readonly groupGone: string;
  readonly addGroup: string;
  readonly addingGroup: string;
  readonly groupAdded: string;
  readonly groupAddFailed: string;
  readonly defaultGroupTitle: string;
  readonly cannotAdd: string;
  readonly chooseBoard: string;
};

export const en: ItemCreateCopy = {
  title: "Add item",
  note: "Name the item and set what you already know. You can change everything later.",
  name: "Item name",
  namePlaceholder: "What needs to be done?",
  nameRequired: "Give the item a name",
  description: "Description",
  descriptionPlaceholder: "Add a description…",
  board: "Add to board",
  group: "Group",
  status: "Status",
  priority: "Priority",
  noPriority: "No priority",
  assignees: "Assignees",
  noMembers: "No one else is on this board yet.",
  startDate: "Start date",
  dueDate: "Due date",
  dueBeforeStart: "The due date cannot be before the start date",
  labels: "Labels",
  noLabels: "This board has no labels yet.",
  another: "Create another",
  cancel: "Cancel",
  submit: "Add item",
  submitting: "Adding…",
  failed: "The item could not be added. Try again.",
  created: "Item added",
  createdWithout:
    "Item added, but {{details}} could not be saved. Open the item to set it.",
  detailNames: {
    status: "the status",
    lane: "the lane",
    priority: "the priority",
    assignees: "the assignees",
    labels: "the labels",
  },
  fromLane: "From lane",
  fromQuickAdd: "From quick add in",
  fromOutside: "From",
  close: "Close the form",
  optional: "(optional)",
  required: "required",
  dateFormat: "dd/mm/yyyy",
  dateInvalid: "Use the day/month/year format, for example 16/10/2026",
  pickDate: "Pick a date",
  quickDue: "Quick due date",
  today: "Today",
  tomorrow: "Tomorrow",
  nextWeek: "Next week",
  weekdayChip: "{{weekday}} {{date}}",
  dueIn: "{{weekday}} · {{days}} days left",
  dueTodayHint: "Due today",
  dueTomorrowHint: "Due tomorrow",
  dueOverdueHint: "{{days}} days ago",
  weekdays: {
    sun: "Sunday",
    mon: "Monday",
    tue: "Tuesday",
    wed: "Wednesday",
    thu: "Thursday",
    fri: "Friday",
    sat: "Saturday",
  },
  addPerson: "Add people",
  assignMe: "Assign to me",
  removePerson: "Remove {{name}}",
  searchPeople: "Search people",
  noPeopleFound: "No one matches",
  anotherKept: "keeps status, priority, people and labels",
  addedStrip: "Added {{title}}",
  open: "Open",
  undo: "Undo",
  undone: "Item removed",
  undoFailed: "The item could not be removed. Open it to archive it.",
  noGroup:
    "This board has no group yet, and every item belongs to a group. Add a group to start adding items.",
  groupGone:
    "The group you picked is no longer available. Choose another group to add the item.",
  addGroup: "Add a group",
  addingGroup: "Adding the group…",
  groupAdded: "Group added. Add your item again.",
  groupAddFailed: "The group could not be added. Try again.",
  defaultGroupTitle: "Tasks",
  cannotAdd: "You do not have permission to add items to this board.",
  chooseBoard: "Choose a board first.",
};

export const vi: ItemCreateCopy = {
  title: "Thêm công việc",
  note: "Đặt tên và nhập những gì bạn đã biết. Mọi thứ đều sửa lại được sau.",
  name: "Tên công việc",
  namePlaceholder: "Việc cần làm là gì?",
  nameRequired: "Hãy đặt tên cho công việc để tiếp tục",
  description: "Mô tả",
  descriptionPlaceholder: "Thêm bối cảnh, đầu việc hoặc liên kết…",
  board: "Thêm vào board",
  group: "Nhóm",
  status: "Trạng thái",
  priority: "Ưu tiên",
  noPriority: "Không ưu tiên",
  assignees: "Người làm",
  noMembers: "Board này chưa có thành viên nào khác.",
  startDate: "Ngày bắt đầu",
  dueDate: "Hạn hoàn thành",
  dueBeforeStart: "Hạn không được trước ngày bắt đầu",
  labels: "Nhãn",
  noLabels: "Board này chưa có nhãn nào.",
  another: "Tạo tiếp công việc khác",
  cancel: "Huỷ",
  submit: "Tạo công việc",
  submitting: "Đang thêm…",
  failed: "Chưa thêm được công việc. Hãy thử lại.",
  created: "Đã thêm công việc",
  createdWithout:
    "Đã thêm công việc, nhưng chưa lưu được {{details}}. Mở công việc để đặt lại.",
  detailNames: {
    status: "trạng thái",
    lane: "cột",
    priority: "mức ưu tiên",
    assignees: "người phụ trách",
    labels: "nhãn",
  },
  fromLane: "Từ cột",
  fromQuickAdd: "Từ thêm nhanh trong",
  fromOutside: "Từ",
  close: "Đóng popup",
  optional: "(không bắt buộc)",
  required: "bắt buộc",
  dateFormat: "dd/mm/yyyy",
  dateInvalid: "Nhập ngày theo dạng ngày/tháng/năm, ví dụ 16/10/2026",
  pickDate: "Chọn ngày",
  quickDue: "Hạn nhanh",
  today: "Hôm nay",
  tomorrow: "Ngày mai",
  nextWeek: "Tuần sau",
  weekdayChip: "{{weekday}} {{date}}",
  dueIn: "{{weekday}} · còn {{days}} ngày",
  dueTodayHint: "Hạn là hôm nay",
  dueTomorrowHint: "Hạn là ngày mai",
  dueOverdueHint: "Đã qua {{days}} ngày",
  weekdays: {
    sun: "Chủ nhật",
    mon: "Thứ Hai",
    tue: "Thứ Ba",
    wed: "Thứ Tư",
    thu: "Thứ Năm",
    fri: "Thứ Sáu",
    sat: "Thứ Bảy",
  },
  addPerson: "Thêm người",
  assignMe: "Giao cho tôi",
  removePerson: "Bỏ {{name}}",
  searchPeople: "Tìm người",
  noPeopleFound: "Không có ai khớp",
  anotherKept: "giữ trạng thái, ưu tiên, người làm, nhãn",
  addedStrip: "Đã thêm {{title}}",
  open: "Mở",
  undo: "Hoàn tác",
  undone: "Đã bỏ công việc vừa thêm",
  undoFailed: "Chưa bỏ được công việc. Hãy mở công việc để lưu trữ.",
  noGroup:
    "Board này chưa có nhóm nào, mà mọi công việc đều thuộc một nhóm. Hãy thêm nhóm để bắt đầu thêm công việc.",
  groupGone:
    "Nhóm bạn chọn không còn nữa. Hãy chọn nhóm khác để thêm công việc.",
  addGroup: "Thêm nhóm",
  addingGroup: "Đang thêm nhóm…",
  groupAdded: "Đã thêm nhóm. Hãy thêm lại công việc.",
  groupAddFailed: "Chưa thêm được nhóm. Hãy thử lại.",
  defaultGroupTitle: "Công việc",
  cannotAdd: "Bạn không có quyền thêm công việc vào board này.",
  chooseBoard: "Hãy chọn board trước.",
};
