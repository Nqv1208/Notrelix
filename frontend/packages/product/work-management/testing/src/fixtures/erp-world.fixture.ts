/**
 * The ERP world of the 2026-10 mockups: board "Triển khai ERP Q4" with 16
 * top-level items (lanes Cần làm 6, Đang làm 3, Chờ duyệt 2, Hoàn thành 5),
 * read as the generated `GET /boards/{id}/full` contract returns it, plus the
 * board access summaries of Nguyễn Thu Hà (manager) and Đỗ Khánh Linh
 * (commenter). The clock is Monday 12/10/2026 10:40 in Asia/Ho_Chi_Minh;
 * dates are UTC instants as the backend sends them.
 *
 * Source: docs/design/mockups/2026-10/decisions.md and Final-Board/Table.
 * ERP-133 and ERP-135 are the two Hoàn thành items the mockup keeps behind
 * "Xem thêm 3 việc"; their titles are fixture data.
 */
import type {
  BoardAccess,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import type {
  GeneratedBoardAccessDto,
  GeneratedFullBoardDto,
} from "@notrelix/work-management-state";
import {
  mapBoardAccessDto,
  mapFullBoardDto,
} from "@notrelix/work-management-state";

/** Monday 12/10/2026 10:40 in Asia/Ho_Chi_Minh. */
export const ERP_NOW = "2026-10-12T10:40:00+07:00";

export function erpClock(): Date {
  return new Date(ERP_NOW);
}

export const ERP_WORKSPACE_ID = "workspace-minh-an";
export const ERP_BOARD_ID = "board-erp-q4";
export const ERP_SPACE_ID = "space-cong-nghe";
export const ERP_GROUP_ID = "group-erp-plan";
export const ERP_STATUS_FIELD_ID = "field-erp-status";
export const ERP_PRIORITY_FIELD_ID = "field-erp-priority";

export const ERP_STATUS_OPTION_IDS = {
  todo: "status-todo",
  doing: "status-doing",
  review: "status-review",
  done: "status-done",
} as const;

export const ERP_PRIORITY_OPTION_IDS = {
  urgent: "priority-urgent",
  high: "priority-high",
  medium: "priority-medium",
  low: "priority-low",
} as const;

/** People of the ERP board, keyed by their mockup initials. */
export const ERP_PEOPLE = {
  HA: { userId: "user-nguyen-thu-ha", name: "Nguyễn Thu Hà" },
  MQ: { userId: "user-tran-minh-quan", name: "Trần Minh Quân" },
  HP: { userId: "user-le-hoang-phuc", name: "Lê Hoàng Phúc" },
  NA: { userId: "user-phan-ngoc-anh", name: "Phan Ngọc Anh" },
  KL: { userId: "user-do-khanh-linh", name: "Đỗ Khánh Linh" },
  VT: { userId: "user-vu-thanh", name: "Vũ Thanh" },
  TT: { userId: "user-bui-thanh-tam", name: "Bùi Thanh Tâm" },
  DT: { userId: "user-dang-minh-tuan", name: "Đặng Minh Tuấn" },
} as const;

type Person = keyof typeof ERP_PEOPLE;

/** Board labels; colours are board data (label tones resolve by name). */
export const ERP_LABELS = {
  data: { labelId: "label-data", name: "Dữ liệu", color: "#1E6FD0" },
  security: { labelId: "label-security", name: "Bảo mật", color: "#C8143F" },
  training: { labelId: "label-training", name: "Đào tạo", color: "#2C5E4F" },
  report: { labelId: "label-report", name: "Báo cáo", color: "#9A6A12" },
  doc: { labelId: "label-doc", name: "Tài liệu", color: "#3E4757" },
  test: { labelId: "label-test", name: "Kiểm thử", color: "#5532A8" },
  infra: { labelId: "label-infra", name: "Hạ tầng", color: "#475569" },
  purchase: { labelId: "label-purchase", name: "Mua sắm", color: "#C2410C" },
} as const;

type Status = keyof typeof ERP_STATUS_OPTION_IDS;
type Priority = keyof typeof ERP_PRIORITY_OPTION_IDS;

/** One top-level ERP item as the mockup defines it. */
export interface ErpItemSeed {
  readonly key: string;
  readonly title: string;
  readonly status: Status;
  readonly priority: Priority;
  readonly label: keyof typeof ERP_LABELS;
  readonly assignee: Person | null;
  /** Local due day "dd/MM" (17:00 Hanoi); null = no date. */
  readonly due: string | null;
  readonly start?: string;
  readonly completed?: string;
  readonly children?: { readonly done: number; readonly total: number };
}

/** Board order: lane by lane, in the order the Kanban shows them. */
export const ERP_ITEMS: readonly ErpItemSeed[] = [
  {
    key: "ERP-151",
    title: "Cấu hình quyền truy cập theo phòng ban cho phân hệ Kế toán",
    status: "todo",
    priority: "high",
    label: "security",
    assignee: "NA",
    due: "18/10",
    children: { done: 0, total: 4 },
  },
  {
    key: "ERP-149",
    title: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
    status: "todo",
    priority: "medium",
    label: "doc",
    assignee: "KL",
    due: "25/10",
  },
  {
    key: "ERP-147",
    title: "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng",
    status: "todo",
    priority: "medium",
    label: "training",
    assignee: "TT",
    due: "21/10",
  },
  {
    key: "ERP-150",
    title: "Thiết lập cảnh báo khi đồng bộ tồn kho thất bại",
    status: "todo",
    priority: "medium",
    label: "infra",
    assignee: "VT",
    due: "28/10",
  },
  {
    key: "ERP-152",
    title: "Kế hoạch hỗ trợ người dùng tuần đầu vận hành",
    status: "todo",
    priority: "low",
    label: "training",
    assignee: "HA",
    due: "30/10",
  },
  {
    key: "ERP-153",
    title: "Rà soát hợp đồng bản quyền phần mềm với nhà cung cấp",
    status: "todo",
    priority: "low",
    label: "purchase",
    assignee: null,
    due: null,
  },
  {
    key: "ERP-142",
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
    title: "Kiểm thử phân hệ công nợ với số liệu tháng 9",
    status: "doing",
    priority: "high",
    label: "test",
    assignee: "HP",
    due: "12/10",
    children: { done: 6, total: 9 },
  },
  {
    key: "ERP-146",
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
    title: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
    status: "review",
    priority: "high",
    label: "security",
    assignee: "MQ",
    due: "14/10",
  },
  {
    key: "ERP-140",
    title: "Mẫu báo cáo doanh thu theo chi nhánh",
    status: "review",
    priority: "medium",
    label: "report",
    assignee: "NA",
    due: "15/10",
  },
  {
    key: "ERP-131",
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
    title: "Khảo sát quy trình nghiệp vụ hiện tại của phòng Kế toán",
    status: "done",
    priority: "low",
    label: "report",
    assignee: "NA",
    due: "26/09",
    completed: "25/09",
  },
];

/** "dd/MM" at 17:00 in Hanoi (UTC+7) as a UTC instant, in 2026. */
function hanoiDay(dayMonth: string, hour = 17): string {
  const [day, month] = dayMonth.split("/");
  const utcHour = String(hour - 7).padStart(2, "0");
  return `2026-${month}-${day}T${utcHour}:00:00.000Z`;
}

/** Fractional-index style keys that sort in board order ("a00" < "a01"). */
function positionKey(index: number): string {
  return `a${index.toString(36).padStart(2, "0")}`;
}

export function erpItemId(key: string): string {
  return `item-${key.toLowerCase()}`;
}

export function erpFullBoardDto(): GeneratedFullBoardDto {
  return {
    id: ERP_BOARD_ID,
    workspaceId: ERP_WORKSPACE_ID,
    title: "Triển khai ERP Q4",
    description: "Đưa ERP vào vận hành trước ngày 01/11/2026.",
    background: null,
    visibility: "Workspace",
    version: 42,
    spaceId: ERP_SPACE_ID,
    itemKeyPrefix: "ERP",
    columns: [
      {
        id: ERP_BOARD_ID,
        boardId: ERP_BOARD_ID,
        name: "Task",
        fieldType: "text",
        settings: '{"system":"title"}',
        defaultValue: null,
        position: "a0",
        isSystem: true,
        isDeleted: false,
        version: 0,
      },
      {
        id: ERP_STATUS_FIELD_ID,
        boardId: ERP_BOARD_ID,
        name: "Trạng thái",
        fieldType: "Status",
        settings: "{}",
        defaultValue: null,
        position: "a1",
        isSystem: true,
        isDeleted: false,
        version: 4,
        options: [
          {
            id: ERP_STATUS_OPTION_IDS.todo,
            name: "Cần làm",
            color: "#7A8391",
            position: "a0",
            category: "todo",
          },
          {
            id: ERP_STATUS_OPTION_IDS.doing,
            name: "Đang làm",
            color: "#1E7BE0",
            position: "a1",
            category: "in_progress",
          },
          {
            id: ERP_STATUS_OPTION_IDS.review,
            name: "Chờ duyệt",
            color: "#C18A12",
            position: "a2",
            category: "review",
          },
          {
            id: ERP_STATUS_OPTION_IDS.done,
            name: "Hoàn thành",
            color: "#2E8B57",
            position: "a3",
            category: "done",
          },
        ],
      },
      {
        id: ERP_PRIORITY_FIELD_ID,
        boardId: ERP_BOARD_ID,
        name: "Ưu tiên",
        fieldType: "Select",
        settings: "{}",
        defaultValue: null,
        position: "a2",
        isSystem: false,
        isDeleted: false,
        version: 2,
        options: [
          {
            id: ERP_PRIORITY_OPTION_IDS.urgent,
            name: "Khẩn",
            color: "#C8143F",
            position: "a0",
          },
          {
            id: ERP_PRIORITY_OPTION_IDS.high,
            name: "Cao",
            color: "#B4500B",
            position: "a1",
          },
          {
            id: ERP_PRIORITY_OPTION_IDS.medium,
            name: "Trung bình",
            color: "#7A8391",
            position: "a2",
          },
          {
            id: ERP_PRIORITY_OPTION_IDS.low,
            name: "Thấp",
            color: "#7A8391",
            position: "a3",
          },
        ],
      },
    ],
    lists: [
      {
        id: ERP_GROUP_ID,
        title: "Kế hoạch triển khai",
        color: "#1E6FD0",
        position: "a0",
        isArchived: false,
        version: 3,
        boardItems: ERP_ITEMS.map((item, index) => {
          const assignee = item.assignee ? ERP_PEOPLE[item.assignee] : null;
          return {
            id: erpItemId(item.key),
            name: item.title,
            memberCount: assignee ? 1 : 0,
            members: assignee
              ? [
                  {
                    userId: assignee.userId,
                    name: assignee.name,
                    avatar: null,
                    assignedAt: "2026-09-28T02:00:00.000Z",
                  },
                ]
              : [],
            labels: [ERP_LABELS[item.label]],
            checklistProgress: 0,
            checklistTotal: 0,
            commentCount: item.key === "ERP-142" ? 4 : 0,
            attachmentCount: 0,
            position: positionKey(index),
            createdAt: "2026-09-21T02:00:00.000Z",
            updatedAt: "2026-10-12T01:40:00.000Z",
            version: 3,
            itemKey: item.key,
            parentItemId: null,
            statusOptionId: ERP_STATUS_OPTION_IDS[item.status],
            priorityOptionId: ERP_PRIORITY_OPTION_IDS[item.priority],
            dueAt: item.due ? hanoiDay(item.due) : null,
            startedAt: item.start ? hanoiDay(item.start, 9) : null,
            completedAt: item.completed ? hanoiDay(item.completed, 16) : null,
            childProgress: item.children ?? { done: 0, total: 0 },
          };
        }),
      },
    ],
    members: (Object.keys(ERP_PEOPLE) as Person[]).map((initials) => ({
      userId: ERP_PEOPLE[initials].userId,
      name: ERP_PEOPLE[initials].name,
      avatar: null,
      role:
        initials === "HP"
          ? "Owner"
          : initials === "HA"
            ? "Admin"
            : initials === "DT"
              ? "Guest"
              : "Member",
      joinedAt: "2026-09-15T02:00:00.000Z",
    })),
  };
}

/** The mapped ERP board as `useFullBoard` returns it. */
export function erpBoardScenario(): FullBoardResponse {
  return mapFullBoardDto(erpFullBoardDto(), {
    workspaceId: ERP_WORKSPACE_ID,
  });
}

/**
 * Hà's access summary: manager through the department (workspace Admin), with
 * every source named. The board is visible to its Space, so the count is a
 * lower bound (`peopleCountComplete: false`).
 */
export function erpOwnerAccessDto(): GeneratedBoardAccessDto {
  return {
    boardId: ERP_BOARD_ID,
    version: 42,
    myLevel: "manager",
    mySource: { kind: "workspace-admin" },
    peopleCount: 32,
    guestCount: 1,
    peopleCountComplete: false,
    sources: [
      { kind: "space", id: ERP_SPACE_ID, name: "Phòng Công nghệ", count: 24 },
      {
        kind: "team",
        id: "team-ke-toan-tong-hop",
        name: "Nhóm Kế toán tổng hợp",
        count: 6,
      },
      {
        kind: "user",
        id: ERP_PEOPLE.KL.userId,
        name: ERP_PEOPLE.KL.name,
        count: 1,
      },
      { kind: "guest", id: null, name: null, count: 1 },
    ],
  };
}

/**
 * Linh's access summary: commenter through an explicit entry added by Lê
 * Hoàng Phúc. She is not a manager and cannot see Phòng Công nghệ, so every
 * source is a count only.
 */
export function erpCommenterAccessDto(): GeneratedBoardAccessDto {
  return {
    boardId: ERP_BOARD_ID,
    version: 42,
    myLevel: "commenter",
    mySource: {
      kind: "explicit",
      grantedBy: {
        userId: ERP_PEOPLE.HP.userId,
        name: ERP_PEOPLE.HP.name,
      },
    },
    peopleCount: 32,
    guestCount: 1,
    peopleCountComplete: false,
    sources: [
      { kind: "space", id: null, name: null, count: 24 },
      { kind: "team", id: null, name: null, count: 6 },
      { kind: "user", id: null, name: null, count: 1 },
      { kind: "guest", id: null, name: null, count: 1 },
    ],
  };
}

export function erpOwnerAccess(): BoardAccess {
  return mapBoardAccessDto(erpOwnerAccessDto());
}

export function erpCommenterAccess(): BoardAccess {
  return mapBoardAccessDto(erpCommenterAccessDto());
}
