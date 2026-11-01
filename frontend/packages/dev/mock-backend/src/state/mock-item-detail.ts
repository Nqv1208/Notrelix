/**
 * What the Detail mockup (docs/design/mockups/2026-10/src/Final-Detail.dc.html)
 * shows of ERP-142 beyond its fields: who created it and when, its files,
 * its history line by line (13 entries, one of them an automation), the
 * people following it, the three custom fields of the ERP board and a link
 * the Timeline does not draw. Pure data; `MockStore.seedErpBoard` inserts it.
 * Times are UTC instants of the Hanoi clock the mockups use (Monday
 * 12/10/2026 10:40).
 */
import { ERP_COMMENTER, ERP_PEOPLE } from "./mock-erp-board";

/** Creation of an item (the "Người tạo" and "Ngày tạo" of the system list). */
export const ERP_ITEM_CREATED: Readonly<
  Record<string, { readonly at: string; readonly userId: string }>
> = {
  "ERP-142": { at: "2026-09-23T08:10:00.000Z", userId: ERP_PEOPLE.HA.id },
};

export interface ErpMockFile {
  readonly id: string;
  readonly itemKey: string;
  readonly filename: string;
  readonly sizeBytes: number;
  readonly contentType: string;
  readonly userId: string;
  readonly createdAt: string;
  /** Index (in `ERP_COMMENTS` for the item) of the comment it came with. */
  readonly commentCreatedAt?: string;
}

const XLSX =
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

export const ERP_FILES: readonly ErpMockFile[] = [
  {
    id: "mock-erp-file-ledger",
    itemKey: "ERP-144",
    filename: "Bang-doi-chieu-cong-no-T9.xlsx",
    sizeBytes: 2_516_582,
    contentType: XLSX,
    userId: ERP_PEOPLE.HP.id,
    createdAt: "2026-10-10T08:20:00.000Z",
  },
  {
    id: "mock-erp-file-chart",
    itemKey: "ERP-144",
    filename: "So-do-doi-chieu.png",
    sizeBytes: 348_160,
    contentType: "image/png",
    userId: ERP_COMMENTER.id,
    createdAt: "2026-10-09T02:00:00.000Z",
  },
  {
    id: "mock-erp-file-merged",
    itemKey: "ERP-142",
    filename: "khach-hang-da-gop.xlsx",
    sizeBytes: 19_293_798,
    contentType: XLSX,
    userId: ERP_PEOPLE.MQ.id,
    createdAt: "2026-10-03T09:30:00.000Z",
  },
  {
    id: "mock-erp-file-balance",
    itemKey: "ERP-142",
    filename: "cong-no-dau-ky-T9.xlsx",
    sizeBytes: 2_202_009,
    contentType: XLSX,
    userId: ERP_COMMENTER.id,
    createdAt: "2026-10-12T02:12:00.000Z",
    commentCreatedAt: "2026-10-12T02:12:00.000Z",
  },
  {
    id: "mock-erp-file-rules",
    itemKey: "ERP-142",
    filename: "quy-tac-gop-trung-v2.pdf",
    sizeBytes: 348_160,
    contentType: "application/pdf",
    userId: ERP_PEOPLE.HA.id,
    createdAt: "2026-09-26T04:00:00.000Z",
  },
];

/** Comments of ERP-142 that the Detail mockup shows and the Board mockup does not. */
export const ERP_EXTRA_COMMENTS = [
  {
    itemKey: "ERP-142",
    userId: ERP_PEOPLE.MQ.id,
    contentMd:
      "Em lùi hạn sang 10/10 vì phòng Kế toán chưa chốt số dư công nợ đầu kỳ, chưa đối chiếu được.",
    createdAt: "2026-10-05T07:22:00.000Z",
  },
  {
    itemKey: "ERP-142",
    userId: ERP_COMMENTER.id,
    contentMd: "Gửi file công nợ đầu kỳ tháng 9 để đối chiếu.",
    createdAt: "2026-10-12T02:12:00.000Z",
    attachmentFileId: "mock-erp-file-balance",
  },
] as const;

/** People who follow an item besides the member (the assignee follows it too). */
export const ERP_FOLLOWERS: Readonly<Record<string, readonly string[]>> = {
  "ERP-142": [ERP_COMMENTER.id, ERP_PEOPLE.TT.id],
};

/** A link that is not a dependency: shown as "Liên quan". */
export const ERP_RELATED_LINKS: readonly { from: string; to: string }[] = [
  { from: "ERP-142", to: "ERP-151" },
  { from: "ERP-144", to: "ERP-131" },
];

/**
 * What one history entry changed, beyond its sentence: a before/after (the
 * "Cao → Khẩn" row), a link, a file or an edited description.
 */
export type ErpHistoryDetail =
  | {
      readonly kind: "diff";
      readonly label?: string;
      readonly from: string;
      readonly to: string;
      readonly note?: string;
    }
  | {
      readonly kind: "link";
      readonly relation: string;
      readonly key: string;
      readonly title: string;
    }
  | { readonly kind: "file"; readonly name: string }
  | {
      readonly kind: "description";
      readonly label: string;
      readonly added: string;
    };

export interface ErpHistoryEntry {
  readonly itemKey: string;
  /** `null` = an automation. */
  readonly userId: string | null;
  /** "created": the line that records the item's creation. */
  readonly kind?: "created";
  readonly automationName?: string;
  /** The sentence the side sheet shows after the actor. */
  readonly action: string;
  /** The short verb the page shows after the actor ("đổi Hạn chót"). */
  readonly verb: string;
  /** The thing the verb names, drawn in the mockup's accent ("ERP-142.3"). */
  readonly target?: string;
  /** Text after the verb on the "Tất cả" tab only ("và nhắc Trần Minh Quân"). */
  readonly notified?: string;
  readonly detail?: ErpHistoryDetail;
  /** How the "Tất cả" tab words the line when it differs from the history tab's. */
  readonly compact?: ErpHistoryCompact;
  readonly createdAt: string;
}

export interface ErpHistoryCompact {
  readonly verb: string;
  readonly target?: string;
  /** The line shows no before/after on that tab. */
  readonly hideDetail?: boolean;
}

const KEY = "ERP-142";
const { HA, MQ, VT } = ERP_PEOPLE;

/** The 13 history entries of ERP-142, oldest first. */
export const ERP_HISTORY: readonly ErpHistoryEntry[] = [
  {
    itemKey: KEY,
    userId: HA.id,
    action: "tạo việc",
    verb: "tạo việc",
    kind: "created",
    createdAt: "2026-09-23T08:10:00.000Z",
  },
  {
    itemKey: KEY,
    userId: HA.id,
    action: "đổi Người phụ trách sang Trần Minh Quân",
    verb: "đổi Người phụ trách",
    detail: { kind: "diff", from: "—", to: "Trần Minh Quân" },
    createdAt: "2026-09-23T08:12:00.000Z",
  },
  {
    itemKey: KEY,
    userId: HA.id,
    action: "đặt Hạn chót 08/10",
    verb: "đặt Hạn chót",
    detail: { kind: "diff", from: "—", to: "08/10" },
    createdAt: "2026-09-24T02:00:00.000Z",
  },
  {
    itemKey: KEY,
    userId: VT.id,
    action: "đánh dấu xong việc con ERP-142.1",
    verb: "đánh dấu xong việc con",
    target: "ERP-142.1",
    detail: {
      kind: "diff",
      label: "Tiến độ",
      from: "0%",
      to: "20%",
      note: "tự tính",
    },
    createdAt: "2026-09-25T04:20:00.000Z",
  },
  {
    itemKey: KEY,
    userId: HA.id,
    action: "đổi Mức ưu tiên từ Trung bình sang Cao",
    verb: "đổi Mức ưu tiên",
    detail: { kind: "diff", from: "Trung bình", to: "Cao" },
    createdAt: "2026-09-30T03:00:00.000Z",
  },
  {
    itemKey: KEY,
    userId: HA.id,
    action: "sửa Mô tả",
    verb: "sửa Mô tả",
    detail: {
      kind: "description",
      label: "Thêm mục",
      added: "Xong khi: 3 tiêu chí",
    },
    createdAt: "2026-10-01T01:30:00.000Z",
  },
  {
    itemKey: KEY,
    userId: MQ.id,
    action: "đổi Trạng thái từ Cần làm sang Đang làm",
    verb: "đổi Trạng thái",
    detail: {
      kind: "diff",
      from: "Cần làm",
      to: "Đang làm",
      note: "kéo trên Kanban",
    },
    createdAt: "2026-10-01T01:45:00.000Z",
  },
  {
    itemKey: KEY,
    userId: MQ.id,
    action: "đánh dấu xong việc con ERP-142.2",
    verb: "đánh dấu xong việc con",
    target: "ERP-142.2",
    detail: {
      kind: "diff",
      label: "Tiến độ",
      from: "20%",
      to: "40%",
      note: "tự tính",
    },
    createdAt: "2026-10-03T09:25:00.000Z",
  },
  {
    itemKey: KEY,
    userId: MQ.id,
    action: "thêm tệp khach-hang-da-gop.xlsx",
    verb: "thêm tệp",
    detail: { kind: "file", name: "khach-hang-da-gop.xlsx" },
    createdAt: "2026-10-03T09:30:00.000Z",
  },
  {
    itemKey: KEY,
    userId: HA.id,
    action: "thêm liên kết Đang chặn ERP-147",
    verb: "thêm liên kết",
    detail: {
      kind: "link",
      relation: "Đang chặn",
      key: "ERP-147",
      title: "Chuẩn bị dữ liệu mẫu cho buổi đào tạo người dùng",
    },
    createdAt: "2026-10-05T04:02:00.000Z",
  },
  {
    itemKey: KEY,
    userId: MQ.id,
    action: "đổi Hạn chót từ 08/10 sang 10/10",
    verb: "đổi Hạn chót",
    detail: { kind: "diff", from: "08/10", to: "10/10" },
    createdAt: "2026-10-05T07:20:00.000Z",
  },
  {
    itemKey: KEY,
    userId: null,
    automationName: "Báo khi quá hạn",
    action: "“Báo khi quá hạn” đổi Mức ưu tiên từ Cao sang Khẩn",
    verb: "“Báo khi quá hạn” đổi Mức ưu tiên",
    notified: "và nhắc Trần Minh Quân",
    detail: { kind: "diff", from: "Cao", to: "Khẩn" },
    createdAt: "2026-10-11T01:00:00.000Z",
  },
  {
    itemKey: KEY,
    userId: MQ.id,
    action: "đánh dấu xong việc con ERP-142.3",
    verb: "đánh dấu xong việc con",
    target: "ERP-142.3",
    detail: {
      kind: "diff",
      label: "Tiến độ",
      from: "40%",
      to: "60%",
      note: "tự tính",
    },
    compact: {
      verb: "đánh dấu xong",
      target: "Chuẩn hoá địa chỉ và số điện thoại",
      hideDetail: true,
    },
    createdAt: "2026-10-12T03:06:00.000Z",
  },
];

/** The custom fields of the ERP board and ERP-142's values (Final-Detail's right column). */
export const ERP_MODULE_COLUMN_ID = "mock-erp-col-module";
export const ERP_ESTIMATE_COLUMN_ID = "mock-erp-col-estimate";
export const ERP_REQUESTER_COLUMN_ID = "mock-erp-col-requester";
/** Custom fields the Table shows as columns: a long text, a link, a number and a formula over it. */
export const ERP_NOTE_COLUMN_ID = "mock-erp-col-note";
export const ERP_LINK_COLUMN_ID = "mock-erp-col-link";
export const ERP_POINTS_COLUMN_ID = "mock-erp-col-points";
export const ERP_DOUBLE_COLUMN_ID = "mock-erp-col-double";
/** Fields the card popup artboards pin on the card face: a checkbox, a date and a vote. */
export const ERP_READY_COLUMN_ID = "mock-erp-col-ready";
export const ERP_ACCEPT_COLUMN_ID = "mock-erp-col-accept";
export const ERP_VOTE_COLUMN_ID = "mock-erp-col-vote";
/** Board fields no card fills: they make "Xem tất cả" appear in the add menu (ERP-144 has nine empty ones). */
export const ERP_EMPTY_FIELDS = [
  { id: "mock-erp-col-task", name: "Task", fieldType: "text" },
  { id: "mock-erp-col-customer", name: "Khách hàng", fieldType: "text" },
  { id: "mock-erp-col-contract", name: "Mã hợp đồng", fieldType: "text" },
  { id: "mock-erp-col-golive", name: "Ngày go-live", fieldType: "date" },
  { id: "mock-erp-col-budget", name: "Ngân sách", fieldType: "number" },
  { id: "mock-erp-col-actual", name: "Số giờ thực tế", fieldType: "number" },
] as const;

/** Checklists of ERP cards (the card popup artboards): `done` marks the first n items as checked. */
export const ERP_CHECKLISTS: readonly {
  readonly itemKey: string;
  readonly title: string;
  readonly done: number;
  readonly items: readonly string[];
}[] = [
  {
    itemKey: "ERP-144",
    title: "Chuẩn bị dữ liệu tháng 9",
    done: 4,
    items: [
      "Xuất công nợ phải thu tháng 9",
      "Xuất công nợ phải trả tháng 9",
      "Chốt danh sách khách hàng nhóm A",
      "Nạp dữ liệu vào môi trường thử",
      "Đối chiếu số dư đầu kỳ với kế toán",
    ],
  },
  {
    itemKey: "ERP-144",
    title: "Nghiệm thu với kế toán",
    done: 2,
    items: [
      "Gửi bảng đối chiếu cho kế toán trưởng",
      "Kế toán trưởng xác nhận số liệu",
      "Ghi nhận các khoản lệch",
      "Ký biên bản nghiệm thu",
    ],
  },
  {
    itemKey: "ERP-140",
    title: "Danh sách go-live",
    done: 3,
    items: [
      "Khoá dữ liệu cũ",
      "Sao lưu cơ sở dữ liệu",
      "Nạp số dư đầu kỳ",
      "Kiểm tra phân quyền",
      "Chạy thử hoá đơn",
      "Thông báo người dùng",
      "Mở hệ thống mới",
      "Theo dõi ngày đầu",
    ],
  },
  {
    itemKey: "ERP-131",
    title: "Mẫu bàn giao",
    done: 6,
    items: [
      "Tài liệu cấu hình",
      "Quy trình vận hành",
      "Danh sách tài khoản",
      "Lịch sao lưu",
      "Đầu mối hỗ trợ",
      "Biên bản bàn giao",
    ],
  },
];

/** Who opened a card before the member did: with the member, ERP-144 reads "Đã xem 3". */
export const ERP_VIEWERS: Readonly<Record<string, readonly string[]>> = {
  "ERP-144": [ERP_PEOPLE.HP.id, ERP_PEOPLE.MQ.id],
};

/** Who voted on a card's Vote field: ERP-144 starts with three votes, none of them the member's. */
export const ERP_VOTES: Readonly<Record<string, readonly string[]>> = {
  "ERP-144": [ERP_PEOPLE.HP.id, ERP_PEOPLE.MQ.id, ERP_PEOPLE.NA.id],
};

export const ERP_MODULE_OPTIONS = [
  { id: "mock-erp-module-accounting", name: "Kế toán" },
  { id: "mock-erp-module-sales", name: "Bán hàng" },
  { id: "mock-erp-module-inventory", name: "Kho" },
  { id: "mock-erp-module-hr", name: "Nhân sự" },
  { id: "mock-erp-module-purchasing", name: "Mua hàng" },
  { id: "mock-erp-module-production", name: "Sản xuất" },
] as const;

export const ERP_REQUESTER_OPTIONS = [
  { id: "mock-erp-requester-accounting", name: "Kế toán", color: "#9A6A12" },
  { id: "mock-erp-requester-sales", name: "Kinh doanh", color: "#1E6FD0" },
  { id: "mock-erp-requester-warehouse", name: "Kho vận", color: "#0E7490" },
  { id: "mock-erp-requester-hr", name: "Nhân sự", color: "#5532A8" },
  { id: "mock-erp-requester-marketing", name: "Marketing", color: "#C2410C" },
] as const;

export const ERP_CUSTOM_VALUES: Readonly<
  Record<string, Readonly<Record<string, unknown>>>
> = {
  "ERP-142": {
    [ERP_MODULE_COLUMN_ID]: "mock-erp-module-accounting",
    [ERP_ESTIMATE_COLUMN_ID]: 40,
    [ERP_REQUESTER_COLUMN_ID]: "mock-erp-requester-accounting",
  },
  "ERP-144": {
    [ERP_NOTE_COLUMN_ID]: "Cần xuất bản Excel cho kế toán trưởng",
    [ERP_LINK_COLUMN_ID]: "https://erp.minhan.vn/t/144",
    [ERP_POINTS_COLUMN_ID]: 13,
    [ERP_READY_COLUMN_ID]: true,
    [ERP_ACCEPT_COLUMN_ID]: "2026-10-23T00:00:00.000Z",
  },
  "ERP-147": {
    [ERP_POINTS_COLUMN_ID]: 5,
  },
};
