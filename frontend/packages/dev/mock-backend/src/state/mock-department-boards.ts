/**
 * The other departments' Boards of the 2026-10 mockups: "Đóng sổ tháng 10"
 * (Kế toán, key KT) and "Ra mắt sản phẩm Q4" (Marketing, key MK). They hold
 * the items the Home, My work and Inbox mockups show beside the ERP Board's
 * (KT-88, KT-91, MK-21). Dates are Hanoi days of 2026 (the mockup clock is
 * Monday 12/10/2026 10:40).
 *
 * Pure data: `MockStore.seedDepartmentBoards` inserts it.
 */
import { mockIds } from "./mock-ids";

export interface DepartmentItem {
  readonly key: string;
  readonly title: string;
  readonly status: "todo" | "doing" | "review" | "done";
  /** Due day "dd/MM" at `dueHour` in Hanoi; null = no date. */
  readonly due: string | null;
  readonly dueHour?: number;
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
    listId: "mock-kt-list-close",
    listTitle: "Đối chiếu",
    items: [
      {
        key: "KT-88",
        title: "Đối chiếu sao kê ngân hàng tháng 9",
        status: "todo",
        due: "12/10",
        dueHour: 17,
      },
      {
        key: "KT-91",
        title: "Lập bảng cân đối thử tháng 10",
        status: "todo",
        due: "26/10",
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
    listId: "mock-mk-list-launch",
    listTitle: "Nội dung",
    items: [
      {
        key: "MK-21",
        title: "Góp ý nội dung trang giới thiệu sản phẩm mới",
        status: "todo",
        due: "16/10",
      },
    ],
  },
];

export function departmentItemId(key: string): string {
  return `mock-dept-item-${key.toLowerCase()}`;
}
