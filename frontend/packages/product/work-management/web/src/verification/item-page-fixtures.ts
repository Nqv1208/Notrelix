import type {
  CardActivity,
  CardDetail,
  CardFile,
  CardMember,
  CardUpdate,
} from "@notrelix/work-management-core";
import type { ItemSheetChildItem } from "../components/card-detail/task-detail-types";
import type {
  ItemPageAccess,
  ItemPageConflict,
  ItemPageNavigation,
  ItemPagePerson,
  ItemPageSpace,
} from "../components/item-page/item-page-types";
import {
  ERP_SHEET_NOW,
  ERP_SHEET_TIME_ZONE,
  erpSheetBoard,
  erpSheetDetailData,
  erpSheetItem,
} from "./item-sheet-fixtures";

/**
 * Mockup world for the item page (ERP-142 on "Triển khai ERP Q4", Final-Detail
 * `tong-quan`), for stories and tests only. Same board and item as the side
 * sheet fixtures, with the description, the sub-items, the files and the
 * activity the page shows. Deterministic: the clock is the sheet's
 * (12/10/2026 10:40, Asia/Ho_Chi_Minh) and every date is a literal.
 */
export const ERP_PAGE_NOW = ERP_SHEET_NOW;
export const ERP_PAGE_TIME_ZONE = ERP_SHEET_TIME_ZONE;

export const erpPageBoard = erpSheetBoard;

const member = (
  key: string,
  name: string,
  initials: string,
  color: string,
): CardMember => ({
  id: `member-${key}`,
  userId: `user-${key}`,
  name,
  initials,
  color,
});

const ha = member("ha", "Nguyễn Thu Hà", "HA", "#4E8574");
const quan = member("quan", "Trần Minh Quân", "MQ", "#1E6FD0");
const linh = member("linh", "Đỗ Khánh Linh", "KL", "#9A6A12");
const thanh = member("thanh", "Vũ Thanh", "VT", "#0E7490");

export const erpPageViewer: ItemPagePerson = {
  id: ha.id,
  userId: ha.userId,
  initials: ha.initials,
};

export const erpPageSpace: ItemPageSpace = {
  name: "Phòng Công nghệ",
  mark: { initials: "CN", tone: "blue" },
};

const DESCRIPTION = [
  "Chuyển 48.200 hồ sơ khách hàng từ phần mềm bán hàng cũ. Giữ nguyên mã khách hàng, gộp bản ghi trùng theo mã số thuế, đối chiếu công nợ đầu kỳ với phòng Kế toán trước khi chốt.",
  "Xong khi:",
  "- Số khách hàng trên ERP khớp danh sách đã gộp, lệch không quá 0,1%.\n- Công nợ đầu kỳ của 200 khách lớn nhất khớp sổ Kế toán.\n- Chị Linh (Kế toán) ký xác nhận trên biên bản đối chiếu.",
].join("\n\n");

const files: CardFile[] = [
  {
    id: "file-merged",
    cardId: "item-erp-142",
    name: "khach-hang-da-gop.xlsx",
    size: 18_400_000,
    contentType:
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    url: "https://files.example.test/khach-hang-da-gop.xlsx",
    source: "upload",
    createdBy: quan,
    createdAt: "2026-10-03T16:30:00+07:00",
  },
  {
    id: "file-balance",
    cardId: "item-erp-142",
    name: "cong-no-dau-ky-T9.xlsx",
    size: 2_100_000,
    contentType:
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    url: "https://files.example.test/cong-no-dau-ky-T9.xlsx",
    source: "upload",
    createdBy: linh,
    createdAt: "2026-10-12T09:12:00+07:00",
  },
  {
    id: "file-rules",
    cardId: "item-erp-142",
    name: "quy-tac-gop-trung-v2.pdf",
    size: 340_000,
    contentType: "application/pdf",
    url: "https://files.example.test/quy-tac-gop-trung-v2.pdf",
    source: "upload",
    createdBy: ha,
    createdAt: "2026-09-26T10:00:00+07:00",
  },
];

const updates: CardUpdate[] = [
  {
    id: "comment-linh-1",
    cardId: "item-erp-142",
    author: linh,
    body: "Bên mình chốt xong số dư công nợ đầu kỳ rồi, sáng mai mình gửi file để đối chiếu nhé @Trần Minh Quân.",
    mentionUserIds: [quan.userId],
    attachmentIds: [],
    createdAt: "2026-10-11T16:40:00+07:00",
  },
  {
    id: "comment-linh-2",
    cardId: "item-erp-142",
    author: linh,
    body: "Gửi file công nợ đầu kỳ tháng 9 để đối chiếu.",
    mentionUserIds: [],
    attachmentIds: ["file-balance"],
    createdAt: "2026-10-12T09:12:00+07:00",
  },
  {
    id: "comment-quan",
    cardId: "item-erp-142",
    author: quan,
    body: "Đã gộp xong bản ghi trùng. Nhận được file của chị Linh rồi, em đối chiếu xong trong hôm nay, dự kiến nhập chính thức thứ Tư.",
    mentionUserIds: [],
    attachmentIds: [],
    createdAt: "2026-10-12T10:05:00+07:00",
  },
];

const activity: CardActivity[] = [
  {
    id: "activity-created",
    cardId: "item-erp-142",
    actor: ha.name,
    action: "tạo công việc",
    type: "created",
    createdAt: "2026-09-23T15:10:00+07:00",
  },
  {
    id: "activity-status",
    cardId: "item-erp-142",
    actor: quan.name,
    action: "đổi Trạng thái từ Cần làm sang Đang làm",
    type: "updated",
    createdAt: "2026-10-01T08:45:00+07:00",
  },
  {
    id: "activity-due",
    cardId: "item-erp-142",
    actor: quan.name,
    action: "đổi Hạn chót từ 08/10 sang 10/10",
    type: "updated",
    createdAt: "2026-10-05T14:20:00+07:00",
  },
  {
    id: "activity-automation",
    cardId: "item-erp-142",
    actor: "Tự động hoá",
    action: "“Báo khi quá hạn” đổi Mức ưu tiên từ Cao sang Khẩn",
    type: "automation",
    createdAt: "2026-10-11T08:00:00+07:00",
  },
  {
    id: "activity-subitem",
    cardId: "item-erp-142",
    actor: quan.name,
    action: "đánh dấu xong Chuẩn hoá địa chỉ và số điện thoại",
    type: "updated",
    createdAt: "2026-10-12T10:06:00+07:00",
  },
];

/** ERP-142 as the page shows it: Đang làm, overdue 2 days, Khẩn, 3/5 sub-items. */
export function erpPageItem(overrides: Partial<CardDetail> = {}): CardDetail {
  return erpSheetItem({
    descriptionMd: DESCRIPTION,
    createdAt: "2026-09-23T15:10:00+07:00",
    updatedAt: "2026-10-12T10:06:00+07:00",
    watchers: [ha, quan, linh, thanh],
    isWatched: true,
    updates,
    files,
    activity,
    _count: { comments: 3, attachments: 3, checklistItems: 0 },
    ...overrides,
  });
}

export const erpPageChildItems: readonly ItemSheetChildItem[] = [
  {
    id: "child-export",
    title: "Xuất dữ liệu từ hệ thống cũ",
    done: true,
    itemKey: "ERP-142.1",
    dueAt: "2026-09-25T17:00:00+07:00",
    assignee: thanh,
  },
  {
    id: "child-dedupe",
    title: "Gộp 1.316 bản ghi trùng theo mã số thuế",
    done: true,
    itemKey: "ERP-142.2",
    dueAt: "2026-10-03T17:00:00+07:00",
    assignee: quan,
  },
  {
    id: "child-normalise",
    title: "Chuẩn hoá địa chỉ và số điện thoại",
    done: true,
    itemKey: "ERP-142.3",
    dueAt: "2026-10-07T17:00:00+07:00",
    assignee: quan,
  },
  {
    id: "child-reconcile",
    title: "Đối chiếu công nợ đầu kỳ với Kế toán",
    done: false,
    itemKey: "ERP-142.4",
    dueAt: "2026-10-10T17:00:00+07:00",
    assignee: linh,
  },
  {
    id: "child-import",
    title: "Nhập chính thức và kiểm tra mẫu 200 khách",
    done: false,
    itemKey: "ERP-142.5",
    dueAt: "2026-10-14T17:00:00+07:00",
    assignee: quan,
  },
];

export function erpPageDetailData(item: CardDetail = erpPageItem()) {
  return erpSheetDetailData(item, {
    childItems: erpPageChildItems,
    files: item.files,
    activity: item.activity,
  });
}

/** The server kept 14/10 while the viewer chose 12/10 (Final-Detail `xung-dot`). */
export const erpPageConflict: ItemPageConflict = {
  property: "due",
  fieldName: "Hạn chót",
  mine: "12/10",
  theirs: "14/10",
};

/** Linh on the ERP board: "Có thể bình luận", added by Phúc. */
export const erpPageCommenterAccess: ItemPageAccess = {
  level: "commenter",
  grantedBy: "Lê Hoàng Phúc",
};

export function erpPageNavigation(
  overrides: Partial<ItemPageNavigation> = {},
): ItemPageNavigation {
  const board = erpPageBoard();
  return {
    position: { index: 1, total: 3, laneName: "Đang làm" },
    boardHref: `/workspaces/${board.workspaceId}/boards/${board.id}`,
    viewHrefs: {
      kanban: `/workspaces/${board.workspaceId}/boards/${board.id}?view=kanban&item=item-erp-142`,
      table: `/workspaces/${board.workspaceId}/boards/${board.id}?view=table&item=item-erp-142`,
      calendar: `/workspaces/${board.workspaceId}/boards/${board.id}?view=calendar&item=item-erp-142`,
      timeline: `/workspaces/${board.workspaceId}/boards/${board.id}?view=timeline`,
    },
    onBack: () => undefined,
    onCollapse: () => undefined,
    ...overrides,
  };
}
