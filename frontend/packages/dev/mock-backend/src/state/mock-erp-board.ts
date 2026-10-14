/**
 * Seed data of the ERP board "Triển khai ERP Q4" (the 2026-10 mockups): 16
 * top-level items in lanes Cần làm 6, Đang làm 3, Chờ duyệt 2, Hoàn thành 5,
 * with keys, labels, due days, priorities (Khẩn 1, Cao 5, Trung bình 7,
 * Thấp 3), assignees and child progress as docs/design/mockups/2026-10/
 * decisions.md fixes them. Dates are UTC instants of the Hanoi day (UTC+7);
 * the mockup clock is Monday 12/10/2026 10:40.
 *
 * Pure data: `MockStore.seedErpBoard` inserts it.
 */
import type {
  MockBoardAudienceRecord,
  MockBoardMemberRecord,
  MockFieldOptionRecord,
  MockLabelRecord,
} from "./records";
import { utcInstant } from "./clock";
import { mockIds } from "./mock-ids";

/** People of the ERP board who are not one of the base personas. */
export const ERP_PEOPLE = {
  /** Nguyễn Thu Hà is the `owner` persona: the member the mockups greet. */
  HA: {
    id: mockIds.users.owner,
    name: "Nguyễn Thu Hà",
    email: "ui-dev@notrelix.local",
  },
  MQ: {
    id: "mock-user-erp-quan",
    name: "Trần Minh Quân",
    email: "quan.tran@minhan.vn",
  },
  HP: {
    id: "mock-user-erp-phuc",
    name: "Lê Hoàng Phúc",
    email: "phuc.le@minhan.vn",
  },
  NA: {
    id: "mock-user-erp-ngoc-anh",
    name: "Phan Ngọc Anh",
    email: "anh.phan@minhan.vn",
  },
  VT: {
    id: "mock-user-erp-vu-thanh",
    name: "Vũ Thanh",
    email: "thanh.vu@minhan.vn",
  },
  TT: {
    id: "mock-user-erp-thanh-tam",
    name: "Bùi Thanh Tâm",
    email: "tam.bui@minhan.vn",
  },
  LT: {
    id: "mock-user-erp-trang",
    name: "Lý Thu Trang",
    email: "trang.ly@minhan.vn",
  },
  PH: {
    id: "mock-user-erp-hang",
    name: "Phạm Thu Hằng",
    email: "hang.pham@minhan.vn",
  },
  DT: {
    id: "mock-user-erp-tuan",
    name: "Đặng Minh Tuấn",
    email: "tuan@doitac-erp.vn",
  },
} as const;

/** Đỗ Khánh Linh is the `commenter` persona. */
export const ERP_COMMENTER = {
  id: mockIds.users.commenter,
  name: "Đỗ Khánh Linh",
  email: "linh.do@minhan.vn",
} as const;

type Assignee = keyof typeof ERP_PEOPLE | "KL";

/**
 * The board groups, in order: the Timeline mockup's four row groups. A Kanban
 * lane lists the cards of the first group, then the second, and so on, so an
 * item sits in the group that keeps every lane in the order the Kanban
 * mockup fixes (a lane's groups never go backwards): the mockup's own
 * grouping holds for ERP-129, 151, 142, 146, 138 and 147, and ERP-144 and
 * ERP-150 follow the lane they are in.
 */
export const ERP_GROUPS = [
  { key: "prep", id: "mock-erp-list-prep", title: "Chuẩn bị" },
  { key: "data", id: "mock-erp-list-data", title: "Dữ liệu" },
  { key: "test", id: "mock-erp-list-test", title: "Kiểm thử" },
  {
    key: "training",
    id: "mock-erp-list-training",
    title: "Đào tạo và vận hành",
  },
] as const;

export type ErpGroupKey = (typeof ERP_GROUPS)[number]["key"];

export function erpGroupId(key: ErpGroupKey): string {
  return ERP_GROUPS.find((group) => group.key === key)?.id ?? ERP_GROUPS[0].id;
}

export const ERP_STATUS_COLUMN_ID = "mock-erp-col-status";
export const ERP_PRIORITY_COLUMN_ID = "mock-erp-col-priority";
export const ERP_OWNER_COLUMN_ID = "mock-erp-col-owner";

/** Default Work Statuses of a new board (Cần làm, Đang làm, Chờ duyệt, Hoàn thành). */
export function defaultStatusOptions(prefix: string): MockFieldOptionRecord[] {
  return [
    {
      id: `${prefix}-todo`,
      name: "Cần làm",
      color: "#7A8391",
      position: "a0",
      category: "todo",
    },
    {
      id: `${prefix}-doing`,
      name: "Đang làm",
      color: "#1E7BE0",
      position: "a1",
      category: "in_progress",
    },
    {
      id: `${prefix}-review`,
      name: "Chờ duyệt",
      color: "#C18A12",
      position: "a2",
      category: "review",
    },
    {
      id: `${prefix}-done`,
      name: "Hoàn thành",
      color: "#2E8B57",
      position: "a3",
      category: "done",
    },
  ];
}

export const ERP_STATUS_OPTIONS = defaultStatusOptions("mock-erp-status");

export const ERP_PRIORITY_OPTIONS: MockFieldOptionRecord[] = [
  {
    id: "mock-erp-priority-urgent",
    name: "Khẩn",
    color: "#C8143F",
    position: "a0",
  },
  {
    id: "mock-erp-priority-high",
    name: "Cao",
    color: "#B4500B",
    position: "a1",
  },
  {
    id: "mock-erp-priority-medium",
    name: "Trung bình",
    color: "#7A8391",
    position: "a2",
  },
  {
    id: "mock-erp-priority-low",
    name: "Thấp",
    color: "#7A8391",
    position: "a3",
  },
];

const LABELS = {
  data: { name: "Dữ liệu", color: "#1E6FD0" },
  security: { name: "Bảo mật", color: "#C8143F" },
  training: { name: "Đào tạo", color: "#2C5E4F" },
  report: { name: "Báo cáo", color: "#9A6A12" },
  doc: { name: "Tài liệu", color: "#3E4757" },
  test: { name: "Kiểm thử", color: "#5532A8" },
  infra: { name: "Hạ tầng", color: "#475569" },
  purchase: { name: "Mua sắm", color: "#C2410C" },
} as const;

type LabelKey = keyof typeof LABELS;

export const ERP_LABELS: MockLabelRecord[] = (
  Object.keys(LABELS) as LabelKey[]
).map((key) => ({
  id: `mock-erp-label-${key}`,
  boardId: mockIds.boards.erp,
  name: LABELS[key].name,
  color: LABELS[key].color,
}));

type StatusKey = "todo" | "doing" | "review" | "done";
type PriorityKey = "urgent" | "high" | "medium" | "low";

export interface ErpMockItem {
  readonly key: string;
  readonly title: string;
  readonly status: StatusKey;
  readonly priority: PriorityKey;
  readonly label: LabelKey;
  /** The board group (a Timeline row group) the item sits in. */
  readonly group: ErpGroupKey;
  readonly assignee: Assignee | null;
  /** Due day "dd/MM" at 17:00 in Hanoi; null = no date. */
  readonly due: string | null;
  /** The due moment's Hanoi hour; 0 = a date-only due (no time of day). */
  readonly dueHour?: number;
  readonly start?: string;
  readonly completed?: string;
  readonly children?: { readonly done: number; readonly total: number };
}

/** Board order, lane by lane as the Kanban shows it. */
export const ERP_ITEMS: readonly ErpMockItem[] = [
  {
    key: "ERP-151",
    group: "prep",
    title: "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
    status: "todo",
    priority: "high",
    label: "security",
    assignee: "NA",
    due: "18/10",
    start: "13/10",
    children: { done: 0, total: 4 },
  },
  {
    key: "ERP-149",
    group: "training",
    title: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
    status: "todo",
    priority: "medium",
    label: "doc",
    assignee: "KL",
    due: "25/10",
  },
  {
    key: "ERP-147",
    group: "training",
    title: "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng",
    status: "todo",
    priority: "medium",
    label: "training",
    assignee: "TT",
    due: "21/10",
    start: "16/10",
  },
  {
    key: "ERP-150",
    group: "training",
    title: "Thiết lập cảnh báo khi đồng bộ tồn kho thất bại",
    status: "todo",
    priority: "medium",
    label: "infra",
    assignee: "VT",
    due: "28/10",
    start: "22/10",
  },
  {
    key: "ERP-152",
    group: "training",
    title: "Kế hoạch hỗ trợ người dùng tuần đầu vận hành",
    status: "todo",
    priority: "low",
    label: "training",
    assignee: "HA",
    due: "30/10",
  },
  {
    key: "ERP-153",
    group: "training",
    title: "Rà soát hợp đồng bản quyền phần mềm với nhà cung cấp",
    status: "todo",
    priority: "low",
    label: "purchase",
    assignee: null,
    due: null,
  },
  {
    key: "ERP-142",
    group: "data",
    title: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    status: "doing",
    priority: "urgent",
    label: "data",
    assignee: "MQ",
    due: "10/10",
    start: "01/10",
    children: { done: 3, total: 5 },
  },
  {
    key: "ERP-144",
    group: "data",
    title: "Kiểm thử phân hệ công nợ với số liệu tháng 9",
    status: "doing",
    priority: "high",
    label: "test",
    assignee: "HP",
    due: "12/10",
    dueHour: 0,
    start: "08/10",
    children: { done: 6, total: 9 },
  },
  {
    key: "ERP-146",
    group: "data",
    title: "Đồng bộ tồn kho giữa kho Bình Dương và kho Long An",
    status: "doing",
    priority: "medium",
    label: "data",
    assignee: "VT",
    due: "16/10",
    start: "13/10",
    children: { done: 1, total: 3 },
  },
  {
    key: "ERP-138",
    group: "test",
    title: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
    status: "review",
    priority: "high",
    label: "security",
    assignee: "MQ",
    due: "14/10",
    start: "09/10",
  },
  {
    key: "ERP-140",
    group: "test",
    title: "Mẫu báo cáo doanh thu theo chi nhánh",
    status: "review",
    priority: "medium",
    label: "report",
    assignee: "NA",
    due: "15/10",
  },
  {
    key: "ERP-131",
    group: "prep",
    title: "Sao lưu cơ sở dữ liệu tự động hằng đêm",
    status: "done",
    priority: "medium",
    label: "infra",
    assignee: "VT",
    due: "06/10",
    completed: "05/10",
  },
  {
    key: "ERP-129",
    group: "prep",
    title: "Chốt danh mục tài khoản kế toán dùng chung",
    status: "done",
    priority: "medium",
    label: "report",
    assignee: "KL",
    due: "30/09",
    completed: "30/09",
  },
  {
    key: "ERP-136",
    group: "prep",
    title: "Chốt danh sách người dùng đợt 1",
    status: "done",
    priority: "high",
    label: "training",
    assignee: "HA",
    due: "09/10",
    completed: "09/10",
  },
  {
    key: "ERP-133",
    group: "prep",
    title: "Cài đặt máy chủ ứng dụng cho môi trường kiểm thử",
    status: "done",
    priority: "high",
    label: "infra",
    assignee: "VT",
    due: "08/10",
    completed: "07/10",
  },
  {
    key: "ERP-135",
    group: "prep",
    title: "Khảo sát quy trình nghiệp vụ hiện tại của phòng Kế toán",
    status: "done",
    priority: "low",
    label: "report",
    assignee: "NA",
    due: "26/09",
    completed: "25/09",
  },
];

export function erpItemId(key: string): string {
  return `mock-erp-item-${key.slice(key.indexOf("-") + 1)}`;
}

export function erpStatusOptionId(status: StatusKey): string {
  return `mock-erp-status-${status}`;
}

export function erpPriorityOptionId(priority: PriorityKey): string {
  return `mock-erp-priority-${priority}`;
}

export function erpLabelId(label: LabelKey): string {
  return `mock-erp-label-${label}`;
}

export function erpAssigneeId(assignee: Assignee): string {
  return assignee === "KL" ? ERP_COMMENTER.id : ERP_PEOPLE[assignee].id;
}

/** "dd/MM" in 2026 at `hour` in Hanoi (UTC+7), as a UTC instant. */
export function hanoiInstant(dayMonth: string, hour = 17): string {
  const [day, month] = dayMonth.split("/");
  return utcInstant(2026, Number(month) - 1, Number(day), hour - 7);
}

export const ERP_BOARD_MEMBERS: MockBoardMemberRecord[] = [
  { userId: ERP_PEOPLE.HA.id, role: "Admin" },
  { userId: ERP_PEOPLE.MQ.id, role: "Member" },
  { userId: ERP_PEOPLE.HP.id, role: "Owner" },
  { userId: ERP_PEOPLE.NA.id, role: "Member" },
  { userId: ERP_PEOPLE.VT.id, role: "Member" },
  { userId: ERP_PEOPLE.TT.id, role: "Member" },
  { userId: ERP_PEOPLE.DT.id, role: "Guest" },
].map((member) => ({
  boardId: mockIds.boards.erp,
  joinedAt: "2026-09-15T02:00:00.000Z",
  ...member,
})) as MockBoardMemberRecord[];

/**
 * Who reaches the ERP board: 24 people of the department (Engineering in the
 * mock world), 6 of Nhóm Kế toán tổng hợp, Đỗ Khánh Linh and 1 guest.
 */
export const ERP_AUDIENCE: MockBoardAudienceRecord = {
  boardId: mockIds.boards.erp,
  guestCount: 1,
  sources: [
    { kind: "space", id: mockIds.spaces.engineering, name: null, count: 24 },
    {
      kind: "team",
      id: "mock-team-erp-accounting",
      name: "Nhóm Kế toán tổng hợp",
      count: 6,
    },
    { kind: "user", id: ERP_COMMENTER.id, name: ERP_COMMENTER.name, count: 1 },
    { kind: "guest", id: null, name: null, count: 1 },
  ],
};
