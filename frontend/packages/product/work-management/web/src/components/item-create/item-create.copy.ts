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
    Record<"status" | "priority" | "assignees" | "labels", string>
  >;
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
    priority: "the priority",
    assignees: "the assignees",
    labels: "the labels",
  },
};

export const vi: ItemCreateCopy = {
  title: "Thêm công việc",
  note: "Đặt tên và nhập những gì bạn đã biết. Mọi thứ đều sửa lại được sau.",
  name: "Tên công việc",
  namePlaceholder: "Cần làm gì?",
  nameRequired: "Hãy đặt tên cho công việc",
  description: "Mô tả",
  descriptionPlaceholder: "Thêm mô tả…",
  board: "Thêm vào board",
  group: "Nhóm",
  status: "Trạng thái",
  priority: "Mức ưu tiên",
  noPriority: "Chưa có mức ưu tiên",
  assignees: "Người phụ trách",
  noMembers: "Board này chưa có thành viên nào khác.",
  startDate: "Ngày bắt đầu",
  dueDate: "Hạn chót",
  dueBeforeStart: "Hạn chót không được trước ngày bắt đầu",
  labels: "Nhãn",
  noLabels: "Board này chưa có nhãn nào.",
  another: "Tạo tiếp công việc khác",
  cancel: "Huỷ",
  submit: "Thêm công việc",
  submitting: "Đang thêm…",
  failed: "Chưa thêm được công việc. Hãy thử lại.",
  created: "Đã thêm công việc",
  createdWithout:
    "Đã thêm công việc, nhưng chưa lưu được {{details}}. Mở công việc để đặt lại.",
  detailNames: {
    status: "trạng thái",
    priority: "mức ưu tiên",
    assignees: "người phụ trách",
    labels: "nhãn",
  },
};
