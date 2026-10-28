/**
 * Descriptions and comments of the ERP board's items, as the Detail mockup
 * (docs/design/mockups/2026-10/src/Final-Detail.dc.html) shows ERP-142. Pure
 * data; `MockStore.seedErpBoard` inserts it. Times are UTC instants of the
 * Hanoi clock the mockups use (Monday 12/10/2026 10:40).
 */
import { ERP_COMMENTER, ERP_PEOPLE } from "./mock-erp-board";

/** Item description (markdown) by item key; items without one stay empty. */
export const ERP_DESCRIPTIONS: Readonly<Record<string, string>> = {
  "ERP-142": [
    "Chuyển 48.200 hồ sơ khách hàng từ phần mềm bán hàng cũ. Giữ nguyên mã khách hàng, gộp bản ghi trùng theo mã số thuế, đối chiếu công nợ đầu kỳ với phòng Kế toán trước khi chốt. Quy tắc gộp chi tiết ở [Kế hoạch chuyển đổi dữ liệu](doc:mock-doc-data-migration-plan).",
    "**Xong khi:**",
    [
      "- Số khách hàng trên ERP khớp danh sách đã gộp, lệch không quá 0,1%.",
      "- Công nợ đầu kỳ của 200 khách lớn nhất khớp sổ Kế toán.",
      "- Chị Linh (Kế toán) ký xác nhận trên biên bản đối chiếu.",
    ].join("\n"),
  ].join("\n\n"),
  "ERP-144":
    "Chạy thử phân hệ công nợ với số liệu thực tháng 9: phát sinh, đối trừ và báo cáo tuổi nợ. Ghi lại mọi chênh lệch so với sổ Kế toán để xử lý trước khi chốt.",
  "ERP-146":
    "Đồng bộ số tồn giữa kho Bình Dương và kho Long An theo giờ, báo khi hai bên lệch quá 1% số lượng.",
  "ERP-138":
    "Thiết lập đăng nhập một lần qua Microsoft Entra cho toàn công ty, giữ lại tài khoản quản trị cục bộ để dự phòng.",
  "ERP-151":
    "Phân quyền truy cập phân hệ Kế toán theo phòng ban: Kế toán xem và sửa, các phòng khác chỉ xem báo cáo.",
  "ERP-149":
    "Viết hướng dẫn từng bước nhập hoá đơn đầu vào trên ERP, kèm ảnh chụp màn hình, cho nhân viên Kế toán.",
};

export interface ErpMockComment {
  readonly itemKey: string;
  readonly userId: string;
  readonly contentMd: string;
  readonly createdAt: string;
}

/** Comments in time order; the Detail mockup shows the three of ERP-142. */
export const ERP_COMMENTS: readonly ErpMockComment[] = [
  {
    itemKey: "ERP-138",
    userId: ERP_PEOPLE.MQ.id,
    contentMd:
      "Em chọn Microsoft Entra ID vì công ty đang dùng Microsoft 365, không phải mua thêm hệ thống đăng nhập.",
    createdAt: "2026-10-09T03:20:00.000Z",
  },
  {
    itemKey: "ERP-138",
    userId: ERP_PEOPLE.VT.id,
    contentMd:
      "Bên hạ tầng đã mở cổng đồng bộ danh bạ, em thử với phòng Kế toán trước.",
    createdAt: "2026-10-10T08:05:00.000Z",
  },
  {
    itemKey: "ERP-138",
    userId: ERP_PEOPLE.HA.id,
    contentMd:
      "Giữ lại tài khoản quản trị cục bộ để dự phòng, đừng tắt hẳn mật khẩu trong tuần đầu.",
    createdAt: "2026-10-11T02:30:00.000Z",
  },
  {
    itemKey: "ERP-138",
    userId: ERP_PEOPLE.MQ.id,
    contentMd:
      "Đã thử với 5 tài khoản phòng Kế toán, đăng nhập ổn. Cần duyệt trước thứ Tư để kịp thông báo toàn công ty, nhờ @Nguyễn Thu Hà.",
    createdAt: "2026-10-12T03:28:00.000Z",
  },
  {
    itemKey: "ERP-142",
    userId: ERP_COMMENTER.id,
    contentMd:
      "Bên mình chốt xong số dư công nợ đầu kỳ rồi, sáng mai mình gửi file để đối chiếu nhé @Trần Minh Quân.",
    createdAt: "2026-10-11T09:40:00.000Z",
  },
  {
    itemKey: "ERP-142",
    userId: ERP_PEOPLE.MQ.id,
    contentMd:
      "Đã gộp xong bản ghi trùng. Nhận được file của chị Linh rồi, em đối chiếu xong trong hôm nay, dự kiến nhập chính thức thứ Tư.",
    createdAt: "2026-10-12T03:05:00.000Z",
  },
  {
    itemKey: "ERP-144",
    userId: ERP_PEOPLE.HP.id,
    contentMd:
      "Số liệu tháng 9 đã nạp xong, mình chạy đối trừ công nợ chiều nay.",
    createdAt: "2026-10-12T02:15:00.000Z",
  },
];

export interface ErpMockChildItem {
  readonly parentKey: string;
  /** "ERP-142.1". */
  readonly key: string;
  readonly title: string;
  readonly done: boolean;
  /** "dd/MM" at 17:00 in Hanoi. */
  readonly due: string;
  readonly assignee: "VT" | "MQ" | "KL";
}

/** The child items of ERP-142 the Detail and Board mockups list under "Việc con" (3/5 done). */
export const ERP_CHILD_ITEMS: readonly ErpMockChildItem[] = [
  {
    parentKey: "ERP-142",
    key: "ERP-142.1",
    title: "Xuất dữ liệu từ hệ thống cũ",
    done: true,
    due: "25/09",
    assignee: "VT",
  },
  {
    parentKey: "ERP-142",
    key: "ERP-142.2",
    title: "Gộp 1.316 bản ghi trùng theo mã số thuế",
    done: true,
    due: "03/10",
    assignee: "MQ",
  },
  {
    parentKey: "ERP-142",
    key: "ERP-142.3",
    title: "Chuẩn hoá địa chỉ và số điện thoại",
    done: true,
    due: "07/10",
    assignee: "MQ",
  },
  {
    parentKey: "ERP-142",
    key: "ERP-142.4",
    title: "Đối chiếu công nợ đầu kỳ với Kế toán",
    done: false,
    due: "10/10",
    assignee: "KL",
  },
  {
    parentKey: "ERP-142",
    key: "ERP-142.5",
    title: "Nhập chính thức và kiểm tra mẫu 200 khách",
    done: false,
    due: "14/10",
    assignee: "MQ",
  },
];

/** History lines of ERP-142 the Board mockup shows under the comments. */
export const ERP_ACTIVITY = [
  {
    itemKey: "ERP-142",
    userId: "mock-user-erp-quan",
    action: "đổi hạn từ 08/10 sang 10/10",
    createdAt: "2026-10-07T03:40:00.000Z",
  },
] as const;

/**
 * Plan items of the ERP board that are not work items: three phases and the
 * go-live milestone the Calendar and the Timeline draw (Final-Calendar,
 * Final-Timeline). Days are Hanoi days "yyyy-MM-dd".
 */
export interface ErpPlanItem {
  readonly id: string;
  readonly kind: "phase" | "milestone";
  readonly title: string;
  readonly startDay: string;
  readonly endDay: string;
  readonly category: "todo" | "doing" | "review" | "done";
  /** The board group the Timeline lists it in (a group key of the board). */
  readonly groupKey: "prep" | "data" | "test" | "training";
  /** Completion, 0 to 100. */
  readonly percent: number;
  /** Row of the item among its group's rows (0 = first). */
  readonly index: number;
}

export const ERP_PLAN_ITEMS: readonly ErpPlanItem[] = [
  {
    id: "mock-erp-plan-uat",
    kind: "phase",
    title: "UAT phân hệ bán hàng với phòng Kinh doanh",
    startDay: "2026-10-12",
    endDay: "2026-10-16",
    category: "doing",
    groupKey: "test",
    percent: 10,
    index: 1,
  },
  {
    id: "mock-erp-plan-training",
    kind: "phase",
    title: "Đào tạo phòng Kế toán",
    startDay: "2026-10-22",
    endDay: "2026-10-23",
    category: "todo",
    groupKey: "training",
    percent: 0,
    index: 1,
  },
  {
    id: "mock-erp-plan-parallel",
    kind: "phase",
    title: "Chạy song song hệ thống cũ và ERP",
    startDay: "2026-10-26",
    endDay: "2026-10-31",
    category: "review",
    groupKey: "training",
    percent: 0,
    index: 2,
  },
  {
    id: "mock-erp-plan-golive",
    kind: "milestone",
    title: "Go-live ERP",
    startDay: "2026-11-01",
    endDay: "2026-11-01",
    category: "todo",
    groupKey: "training",
    percent: 0,
    index: 3,
  },
];

/**
 * Finish-to-start dependencies of the ERP board: an item key (ERP-142) or a
 * plan item id; `to` waits for `from` (Final-Timeline).
 */
export const ERP_PLAN_LINKS: readonly { from: string; to: string }[] = [
  { from: "ERP-129", to: "ERP-142" },
  { from: "ERP-142", to: "ERP-146" },
  { from: "ERP-146", to: "ERP-150" },
  { from: "ERP-142", to: "ERP-147" },
  { from: "ERP-147", to: "mock-erp-plan-training" },
  { from: "mock-erp-plan-training", to: "mock-erp-plan-parallel" },
  { from: "mock-erp-plan-parallel", to: "mock-erp-plan-golive" },
];

/** Completion of items whose progress is not the share of their child items. */
export const ERP_ITEM_PERCENT: Readonly<Record<string, number>> = {
  "ERP-138": 90,
};

/**
 * Where the Timeline lists an item whose group differs from the one the
 * Kanban lane order follows (Final-Timeline puts ERP-150 in Dữ liệu and
 * ERP-144 in Kiểm thử).
 */
export const ERP_PLAN_GROUPS: Readonly<
  Record<string, "prep" | "data" | "test" | "training">
> = {
  "ERP-150": "data",
  "ERP-144": "test",
};
