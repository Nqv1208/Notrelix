/**
 * The other departments' Boards of the 2026-10 mockups: "Đóng sổ tháng 10"
 * (Kế toán, key KT) and "Ra mắt sản phẩm Q4" (Marketing, key MK). They hold
 * the items the Home, My work and Inbox mockups show beside the ERP Board's
 * (KT-88, KT-95, MK-21) and the Command mockup's search results (KT-88, KT-91). Dates are Hanoi days of 2026 (the mockup clock is
 * Monday 12/10/2026 10:40).
 *
 * Pure data: `MockStore.seedDepartmentBoards` inserts it.
 */
import { ERP_PEOPLE } from "./mock-erp-board";
import { mockIds } from "./mock-ids";

export interface DepartmentItem {
  readonly key: string;
  readonly title: string;
  readonly status: "todo" | "doing" | "review" | "done";
  readonly priority: "urgent" | "high" | "medium" | "low";
  /** Due day "dd/MM" at `dueHour` in Hanoi; null = no date. */
  readonly due: string | null;
  readonly dueHour?: number;
  /** Completion "dd/MM" at `completedHour` in Hanoi (done items). */
  readonly completed?: string;
  readonly completedHour?: number;
  /** Free text of the item; a search can match inside it. */
  readonly description?: string;
  /** Who is in charge, when it is somebody else than nobody. */
  readonly assigneeId?: string;
}

export interface DepartmentBoard {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly prefix: string;
  readonly color: string;
  readonly spaceId: string;
  readonly statusColumnId: string;
  readonly statusPrefix: string;
  readonly priorityColumnId: string;
  /** The board has a person column ("Người thực hiện"), so a pick can be assigned in bulk. */
  readonly ownerColumnId?: string;
  /** Defaults to the whole workspace. */
  readonly visibility?: "private" | "workspace";
  /** An archived board: readable, not writable. */
  readonly archived?: boolean;
  /** A board whose table has no group yet (it holds no list). */
  readonly noList?: boolean;
  readonly listId: string;
  readonly listTitle: string;
  readonly items: readonly DepartmentItem[];
}

export const DEPARTMENT_BOARDS: readonly DepartmentBoard[] = [
  {
    id: mockIds.boards.accounting,
    title: "Đóng sổ tháng 10",
    description: "Các việc đóng sổ kế toán tháng 10/2026.",
    prefix: "KT",
    color: "#B4500B",
    spaceId: mockIds.spaces.accounting,
    statusColumnId: "mock-kt-col-status",
    statusPrefix: "mock-kt-status",
    priorityColumnId: "mock-kt-col-priority",
    listId: "mock-kt-list-close",
    listTitle: "Đối chiếu",
    items: [
      {
        key: "KT-84",
        title: "Rà soát phiếu chi còn tồn",
        status: "doing",
        priority: "medium",
        due: "13/10",
        description:
          "Rà soát phiếu chi tồn đọng và đối chiếu với sổ quỹ trước khi đóng sổ.",
      },
      {
        key: "KT-85",
        title: "Lập bảng cân đối thử tháng 10",
        status: "todo",
        priority: "high",
        due: "27/10",
        description: "Số dư đầu kỳ phải đối chiếu với sổ cái tháng 9.",
      },
      {
        key: "KT-87",
        title: "Chốt chi phí trả trước tháng 10",
        status: "todo",
        priority: "medium",
        due: "28/10",
        description: "Đối chiếu bảng phân bổ với hợp đồng đã ký.",
      },
      {
        key: "KT-86",
        title: "Khoá sổ phụ ngân hàng tháng 9",
        status: "done",
        priority: "medium",
        due: "12/10",
        dueHour: 12,
        completed: "12/10",
        completedHour: 10,
      },
      {
        key: "KT-88",
        title: "Đối chiếu sao kê ngân hàng tháng 9",
        status: "todo",
        priority: "medium",
        due: "12/10",
        dueHour: 17,
      },
      {
        key: "KT-95",
        title: "Xác nhận số dư tài khoản ngân hàng trên ERP tháng 10",
        status: "todo",
        priority: "medium",
        due: "26/10",
      },
    ],
  },
  {
    id: mockIds.boards.payables,
    title: "Công nợ nhà cung cấp",
    description: "Theo dõi công nợ phải trả và đối soát với nhà cung cấp.",
    prefix: "KT",
    color: "#9A6A12",
    spaceId: mockIds.spaces.accounting,
    statusColumnId: "mock-ap-col-status",
    statusPrefix: "mock-ap-status",
    priorityColumnId: "mock-ap-col-priority",
    listId: "mock-ap-list-payables",
    listTitle: "Phải trả",
    items: [
      {
        key: "KT-91",
        title: "Đối chiếu công nợ 42 nhà cung cấp",
        status: "doing",
        priority: "medium",
        due: "20/10",
        assigneeId: "mock-user-erp-trang",
      },
      {
        key: "KT-92",
        title: "Gửi thư xác nhận số dư cho nhà cung cấp",
        status: "todo",
        priority: "low",
        due: "23/10",
      },
    ],
  },
  {
    id: mockIds.boards.marketing,
    title: "Ra mắt sản phẩm Q4",
    description: "Kế hoạch ra mắt sản phẩm quý 4/2026.",
    prefix: "MK",
    color: "#C2410C",
    spaceId: mockIds.spaces.marketing,
    statusColumnId: "mock-mk-col-status",
    statusPrefix: "mock-mk-status",
    priorityColumnId: "mock-mk-col-priority",
    listId: "mock-mk-list-launch",
    listTitle: "Nội dung",
    items: [
      {
        key: "MK-21",
        title: "Góp ý nội dung trang giới thiệu sản phẩm mới",
        status: "todo",
        priority: "medium",
        due: "16/10",
      },
    ],
  },
];

/** The other Boards of the Công nghệ department the mockups' sidebar lists. */
export const TECH_SIDE_BOARDS: readonly DepartmentBoard[] = [
  {
    id: "mock-board-it-infra",
    title: "Hạ tầng & bảo mật",
    description: "Hạ tầng, an toàn thông tin và vận hành hệ thống.",
    prefix: "HB",
    color: "#1E6FD0",
    spaceId: mockIds.spaces.engineering,
    statusColumnId: "mock-iti-col-status",
    statusPrefix: "mock-iti-status",
    priorityColumnId: "mock-iti-col-priority",
    listId: "mock-iti-list-ops",
    listTitle: "Vận hành",
    items: [],
  },
  {
    id: "mock-board-it-support",
    title: "Hỗ trợ nội bộ",
    description: "Yêu cầu hỗ trợ CNTT từ các phòng ban.",
    prefix: "HT",
    color: "#1E6FD0",
    spaceId: mockIds.spaces.engineering,
    statusColumnId: "mock-it-col-status",
    statusPrefix: "mock-it-status",
    priorityColumnId: "mock-it-col-priority",
    listId: "mock-it-list-requests",
    listTitle: "Yêu cầu",
    items: [],
  },
  {
    id: "mock-board-it-budget",
    title: "Ngân sách CNTT 2027",
    description: "Dự toán ngân sách CNTT năm 2027.",
    prefix: "NS",
    color: "#1E6FD0",
    spaceId: mockIds.spaces.engineering,
    statusColumnId: "mock-itb-col-status",
    statusPrefix: "mock-itb-status",
    priorityColumnId: "mock-itb-col-priority",
    listId: "mock-itb-list-plan",
    listTitle: "Dự toán",
    visibility: "private",
    items: [],
  },
];

/**
 * The overlay `board-states` adds two boards the Board content states (BC-Shell RO, NG) need: an archived one and one
 * with no group. They stay out of the plain world, whose sidebar and department counts the other specs measure.
 */
export const BOARD_STATE_BOARDS: readonly DepartmentBoard[] = [
  {
    id: "mock-board-it-archived",
    title: "Di dời trung tâm dữ liệu 2025",
    description: "Dự án đã xong, giữ lại để tra cứu.",
    prefix: "DD",
    color: "#1E6FD0",
    spaceId: mockIds.spaces.engineering,
    statusColumnId: "mock-ita-col-status",
    statusPrefix: "mock-ita-status",
    priorityColumnId: "mock-ita-col-priority",
    listId: "mock-ita-list-done",
    listTitle: "Đã xong",
    archived: true,
    items: [],
  },
  {
    id: "mock-board-it-no-group",
    title: "Kế hoạch đào tạo CNTT",
    description: "Board mới, chưa chia nhóm.",
    prefix: "DT",
    color: "#1E6FD0",
    spaceId: mockIds.spaces.engineering,
    statusColumnId: "mock-itn-col-status",
    statusPrefix: "mock-itn-status",
    priorityColumnId: "mock-itn-col-priority",
    listId: "mock-itn-list-none",
    listTitle: "Chưa có",
    noList: true,
    items: [],
  },
];

/**
 * The overlay `bulk-states` adds one board with 301 items in one group (BC-Bulk `bk-grp`, `bk-cap`): the group
 * select covers 200 of them, one more pick passes the limit.
 */
export const BULK_STATE_BOARDS: readonly DepartmentBoard[] = [
  {
    id: "mock-board-it-bulk",
    title: "Nhập liệu hàng loạt",
    description: "Board có một nhóm rất lớn, để thử chọn nhiều việc.",
    prefix: "NL",
    color: "#1E6FD0",
    spaceId: mockIds.spaces.engineering,
    statusColumnId: "mock-itk-col-status",
    statusPrefix: "mock-itk-status",
    priorityColumnId: "mock-itk-col-priority",
    ownerColumnId: "mock-itk-col-owner",
    listId: "mock-itk-list-doing",
    listTitle: "Đang làm",
    items: Array.from({ length: 301 }, (_, index) => ({
      key: `NL-${index + 1}`,
      title: `Việc nhập liệu ${index + 1}`,
      status: "doing" as const,
      priority: "medium" as const,
      due: null,
    })),
  },
];

const DUE_DAY_FORMAT = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "numeric",
  timeZone: "UTC",
});

/** A day "dd/MM" counted from 10/10/2026, one per row, so the due order is the row order. */
function dueDay(index: number): string {
  const parts = DUE_DAY_FORMAT.formatToParts(Date.UTC(2026, 9, 10 + index));
  const part = (type: string) =>
    Number(parts.find((p) => p.type === type)?.value);
  return `${part("day")}/${part("month")}`;
}

export const LARGE_BOARD_ID = "mock-board-it-large";

const LARGE_ASSIGNEES = [
  ERP_PEOPLE.MQ.id,
  ERP_PEOPLE.HP.id,
  ERP_PEOPLE.TT.id,
] as const;

/**
 * The overlay `large-board` adds one board of 5,000 items that the server reads a page at a time (BC-Large): 200 in
 * Đang làm, 22 in Cần làm, 3 in Chờ duyệt and 4,775 done, each lane in due order; one person
 * holds every 16th of Đang làm (12 of 200) and every item of the two other open lanes: 37 open items, as BC-Large draws
 * (the table hides the done lane, so the open lanes are what its filtered total counts).
 */
export const LARGE_STATE_BOARDS: readonly DepartmentBoard[] = [
  {
    id: LARGE_BOARD_ID,
    title: "Triển khai ERP Q4 (board lớn)",
    description: "Board 5.000 việc, đọc từng trang.",
    prefix: "ERP",
    color: "#1E6FD0",
    spaceId: mockIds.spaces.engineering,
    statusColumnId: "mock-itl-col-status",
    statusPrefix: "mock-itl-status",
    priorityColumnId: "mock-itl-col-priority",
    ownerColumnId: "mock-itl-col-owner",
    listId: "mock-itl-list-all",
    listTitle: "Tất cả",
    items: [
      ...Array.from({ length: 22 }, (_, index) => ({
        key: `ERP-${500 + index + 1}`,
        title: `Việc cần làm số ${500 + index + 1}`,
        status: "todo" as const,
        priority: "medium" as const,
        due: dueDay(index),
        assigneeId: ERP_PEOPLE.NA.id,
      })),
      ...Array.from({ length: 200 }, (_, index) => ({
        key: `ERP-${101 + index}`,
        title: `Việc trong nhóm lớn số ${101 + index}`,
        status: "doing" as const,
        priority: "medium" as const,
        due: dueDay(index),
        assigneeId:
          (index + 1) % 16 === 0
            ? ERP_PEOPLE.NA.id
            : LARGE_ASSIGNEES[index % LARGE_ASSIGNEES.length],
      })),
      ...Array.from({ length: 3 }, (_, index) => ({
        key: `ERP-${600 + index + 1}`,
        title: `Việc chờ duyệt số ${600 + index + 1}`,
        status: "review" as const,
        priority: "medium" as const,
        due: dueDay(index),
        assigneeId: ERP_PEOPLE.NA.id,
      })),
      ...Array.from({ length: 4775 }, (_, index) => ({
        key: `ERP-${1001 + index}`,
        title: `Việc đã xong số ${1001 + index}`,
        status: "done" as const,
        priority: "low" as const,
        due: null,
        completed: "1/10",
        assigneeId: LARGE_ASSIGNEES[index % LARGE_ASSIGNEES.length],
      })),
    ],
  },
];

export function departmentItemId(key: string): string {
  return `mock-dept-item-${key.toLowerCase()}`;
}
