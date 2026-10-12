import type {
  Board,
  CardDetail,
  CardMember,
  FieldDefinition,
} from "@notrelix/work-management-core";
import type {
  ItemSheetChildItem,
  ItemSheetGroup,
  ItemSheetMark,
  TaskDetailCapabilities,
  TaskDetailData,
} from "./task-detail-types";

/**
 * Mockup world for the item side sheet (ERP-142 on "Triển khai ERP Q4"),
 * for stories and tests only. Deterministic: the clock is injected
 * (12/10/2026 10:40, Asia/Ho_Chi_Minh) and every date is a literal.
 */
export const ERP_SHEET_NOW = new Date("2026-10-12T10:40:00+07:00");
export const ERP_SHEET_TIME_ZONE = "Asia/Ho_Chi_Minh";

const BOARD_ID = "board-erp-q4";
const WORKSPACE_ID = "workspace-notrelix";

const quan: CardMember = {
  id: "member-quan",
  userId: "user-quan",
  name: "Trần Minh Quân",
  initials: "MQ",
  color: "#1E6FD0",
};

const linh: CardMember = {
  id: "member-linh",
  userId: "user-linh",
  name: "Đỗ Khánh Linh",
  initials: "KL",
  color: "#9A6A12",
};

const fields: FieldDefinition[] = [
  {
    id: "field-status",
    boardId: BOARD_ID,
    name: "Trạng thái",
    fieldType: "select",
    options: [
      { id: "status-todo", label: "Cần làm", color: "#8A94A6" },
      { id: "status-doing", label: "Đang làm", color: "#2F6FEB" },
      { id: "status-review", label: "Chờ duyệt", color: "#C98A0B" },
      { id: "status-done", label: "Hoàn thành", color: "#1F9D55" },
    ],
    position: 1,
    isHidden: false,
    isSystemField: true,
  },
  {
    id: "field-priority",
    boardId: BOARD_ID,
    name: "Ưu tiên",
    fieldType: "select",
    options: [
      { id: "urgent", label: "Khẩn", color: "#D92D20" },
      { id: "high", label: "Cao", color: "#B4500B" },
      { id: "medium", label: "Trung bình", color: "#8A94A6" },
      { id: "low", label: "Thấp", color: "#8A94A6" },
    ],
    position: 2,
    isHidden: false,
    isSystemField: true,
  },
  {
    id: "field-person",
    boardId: BOARD_ID,
    name: "Người phụ trách",
    fieldType: "person",
    options: [],
    position: 3,
    isHidden: false,
    isSystemField: true,
  },
  {
    id: "field-due-date",
    boardId: BOARD_ID,
    name: "Hạn",
    fieldType: "date",
    options: [],
    position: 4,
    isHidden: false,
    isSystemField: true,
  },
];

export function erpSheetBoard(overrides: Partial<Board> = {}): Board {
  return {
    id: BOARD_ID,
    workspaceId: WORKSPACE_ID,
    title: "Triển khai ERP Q4",
    background: { type: "color", value: "#1E6FD0" },
    visibility: "workspace",
    isArchived: false,
    fieldDefinitions: fields,
    members: [
      { ...quan, role: "editor" },
      { ...linh, role: "viewer" },
    ],
    createdAt: "2026-09-01T09:00:00+07:00",
    updatedAt: "2026-10-12T08:40:00+07:00",
    ...overrides,
  };
}

export function erpSheetItem(overrides: Partial<CardDetail> = {}): CardDetail {
  return {
    id: "item-erp-142",
    listId: "group-erp-migration",
    boardId: BOARD_ID,
    workspaceId: WORKSPACE_ID,
    title: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
    descriptionMd:
      "Chuyển 48.200 hồ sơ khách hàng từ phần mềm bán hàng cũ. Giữ nguyên mã khách hàng, gộp bản ghi trùng theo mã số thuế, đối chiếu công nợ đầu kỳ với phòng Kế toán trước khi chốt.",
    position: 1,
    priority: "urgent",
    status: "status-doing",
    statusOptionId: "status-doing",
    statusCategory: "in_progress",
    itemKey: "ERP-142",
    parentItemId: null,
    startDate: "2026-10-01T08:00:00+07:00",
    dueAt: "2026-10-10T17:00:00+07:00",
    dueDate: "2026-10-10T17:00:00+07:00",
    childProgress: { done: 3, total: 5 },
    isArchived: false,
    isDeleted: false,
    members: [quan],
    labels: [{ id: "label-data", name: "Dữ liệu", color: "#1E6FD0" }],
    checklists: [],
    fieldValues: {},
    _count: { comments: 2, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T08:00:00+07:00",
    updatedAt: "2026-10-12T08:40:00+07:00",
    boardTitle: "Triển khai ERP Q4",
    watchers: [quan],
    isWatched: true,
    updates: [
      {
        id: "comment-linh",
        cardId: "item-erp-142",
        author: linh,
        body: "Bên mình cần file công nợ đầu kỳ trước thứ Tư để đối chiếu, anh Quân gửi giúp nhé.",
        mentionUserIds: ["user-quan"],
        attachmentIds: [],
        createdAt: "2026-10-11T16:40:00+07:00",
      },
      {
        id: "comment-quan",
        cardId: "item-erp-142",
        author: quan,
        body: "Đã gộp xong bản ghi trùng. Còn chờ file đối chiếu từ Kế toán, dự kiến chốt thứ Tư.",
        mentionUserIds: [],
        attachmentIds: [],
        createdAt: "2026-10-12T08:40:00+07:00",
      },
    ],
    files: [],
    activity: [
      {
        id: "activity-due",
        cardId: "item-erp-142",
        actor: "Trần Minh Quân",
        action: "đổi hạn từ 08/10 sang 10/10",
        type: "updated",
        createdAt: "2026-10-07T09:15:00+07:00",
      },
    ],
    ...overrides,
  };
}

export const erpSheetChildItems: readonly ItemSheetChildItem[] = [
  { id: "child-export", title: "Xuất dữ liệu từ hệ thống cũ", done: true },
  { id: "child-dedupe", title: "Gộp 1.316 bản ghi trùng", done: true },
  {
    id: "child-normalise",
    title: "Chuẩn hoá địa chỉ và số điện thoại",
    done: true,
  },
  {
    id: "child-reconcile",
    title: "Đối chiếu công nợ đầu kỳ với Kế toán",
    done: false,
  },
  { id: "child-import", title: "Nhập chính thức và kiểm tra mẫu", done: false },
];

export const erpSheetMark: ItemSheetMark = { initials: "CN", tone: "blue" };

export const erpSheetGroups: readonly ItemSheetGroup[] = [
  { id: "group-erp-migration", title: "Chuyển đổi dữ liệu" },
  { id: "group-erp-rollout", title: "Triển khai" },
];

export function erpSheetDetailData(
  item: CardDetail = erpSheetItem(),
  overrides: Partial<TaskDetailData> = {},
): TaskDetailData {
  return {
    updates: item.updates,
    updatesLoading: false,
    files: item.files,
    filesLoading: false,
    activity: item.activity,
    activityLoading: false,
    activityFetching: false,
    childItems: erpSheetChildItems,
    ...overrides,
  };
}

/** Linh on the ERP board: "Có thể bình luận" — comments only, no property edits. */
export const commenterSheetCapabilities: TaskDetailCapabilities = {
  canCreateCard: false,
  canMoveCard: false,
  canRenameGroup: false,
  canDelete: false,
  canEditFields: false,
  canComment: true,
};

export const editorSheetCapabilities: TaskDetailCapabilities = {
  canCreateCard: true,
  canMoveCard: true,
  canRenameGroup: true,
  canDelete: true,
  canEditFields: true,
  canComment: true,
};
