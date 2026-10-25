import type {
  BoardSettingsAutomationDto,
  BoardSettingsDto,
  BoardSettingsFieldDto,
  BoardSettingsViewDto,
} from "@notrelix/work-management-core";
import type {
  BoardSettingsActions,
  BoardSettingsSpace,
  BoardSettingsWriteResult,
} from "../components/board-settings/board-settings-types";

/**
 * Mockup world for the board settings (Final-Settings, "Triển khai ERP Q4"),
 * for stories and tests only: 11 board fields, 5 system fields, 1 archived
 * field, 7 views, 4 groups and 4 automations. Deterministic: every date is a
 * literal.
 */
export const ERP_SETTINGS_TIME_ZONE = "Asia/Ho_Chi_Minh";

export const erpSettingsSpace: BoardSettingsSpace = {
  name: "Công nghệ",
  mark: { initials: "CN", tone: "blue" },
};

const field = (
  id: string,
  key: string,
  type: BoardSettingsFieldDto["type"],
  name: string,
  extra: Partial<BoardSettingsFieldDto> = {},
): BoardSettingsFieldDto => ({
  id,
  key,
  type,
  name,
  detail: "",
  usage: "Chưa dùng",
  accent: false,
  unit: null,
  hasColumn: false,
  valueCount: 16,
  options: [],
  defaultOptionId: null,
  required: false,
  guidance: "",
  usedIn: [],
  ...extra,
});

const priorityOptions = [
  { id: "p-urgent", name: "Khẩn", color: "#C42340", itemCount: 1 },
  { id: "p-high", name: "Cao", color: "#B4500B", itemCount: 5 },
  { id: "p-medium", name: "Trung bình", color: "#1E7BE0", itemCount: 7 },
  { id: "p-low", name: "Thấp", color: "#7A8391", itemCount: 3 },
] as const;

export const erpPriorityField = field(
  "sf-priority",
  "priority",
  "select",
  "Mức ưu tiên",
  {
    detail: "4 lựa chọn",
    usage: "3 view · 1 quy tắc",
    hasColumn: true,
    options: priorityOptions,
    defaultOptionId: "p-medium",
    guidance: "Chỉ chọn Khẩn khi việc chặn ngày go-live 01/11.",
    usedIn: [
      { kind: "view", name: "Bảng", how: "cột hiển thị" },
      { kind: "card", name: "Kanban", how: "hiện trên thẻ" },
      { kind: "filter", name: "Việc khẩn của tôi", how: "bộ lọc" },
      { kind: "rule", name: "Khẩn → báo Trưởng phòng", how: "quy tắc tự động" },
    ],
  },
);

const fields: BoardSettingsFieldDto[] = [
  field("sf-status", "status", "status", "Trạng thái", {
    detail: "4 lựa chọn",
    usage: "Chia cột Kanban",
    accent: true,
    hasColumn: true,
    options: [
      { id: "s-todo", name: "Cần làm", color: "#7A8391", itemCount: 6 },
      { id: "s-doing", name: "Đang làm", color: "#1E6FD0", itemCount: 3 },
      { id: "s-review", name: "Chờ duyệt", color: "#9A6A12", itemCount: 2 },
      { id: "s-done", name: "Hoàn thành", color: "#2C5E4F", itemCount: 5 },
    ],
    usedIn: [
      { kind: "view", name: "Kanban", how: "chia cột" },
      { kind: "view", name: "Bảng", how: "nhóm theo" },
    ],
  }),
  field("sf-owner", "assignee", "person", "Người phụ trách", {
    detail: "nhiều người",
    usage: "5 view",
  }),
  field("sf-start", "start_date", "date", "Ngày bắt đầu", { usage: "2 view" }),
  field("sf-due", "due_date", "date", "Hạn chót", {
    detail: "có giờ, GMT+7",
    usage: "6 view",
  }),
  erpPriorityField,
  field("sf-module", "erp_module", "multiselect", "Phân hệ ERP", {
    detail: "6 lựa chọn",
    usage: "2 view",
  }),
  field("sf-labels", "labels", "multiselect", "Nhãn", {
    detail: "Dữ liệu, Bảo mật, Đào tạo, Báo cáo…",
    usage: "Kanban, Bảng",
  }),
  field("sf-estimate", "estimate_hours", "number", "Ước lượng", {
    detail: "1 chữ số thập phân",
    usage: "1 view",
    unit: "giờ",
  }),
  field("sf-progress", "progress", "progress", "Tiến độ", {
    detail: "tự tính từ việc con",
    usage: "2 view",
  }),
  field("sf-depends", "depends_on", "relation", "Phụ thuộc", {
    detail: "Đang chờ, Đang chặn, Liên quan",
    usage: "Dòng thời gian, Chi tiết",
  }),
  field("sf-requester", "requester_dept", "select", "Phòng ban yêu cầu", {
    detail: "5 lựa chọn",
  }),
];

const view = (
  id: string,
  name: string,
  kind: BoardSettingsViewDto["kind"],
  summary: string,
  visibility: BoardSettingsViewDto["visibility"],
  extra: Partial<BoardSettingsViewDto> = {},
): BoardSettingsViewDto => ({
  id,
  name,
  kind,
  summary,
  visibility,
  isDefault: false,
  fieldId: null,
  filters: [],
  sort: null,
  cardFieldIds: [],
  cardFieldChoices: [],
  lastEditedBy: "Trần Minh Quân",
  createdBy: "Trần Minh Quân",
  needsFix: false,
  ...extra,
});

const views: BoardSettingsViewDto[] = [
  view(
    "sv-table",
    "Bảng",
    "table",
    "Nhóm theo Trạng thái · 9/12 cột · Hạn ↑",
    "board",
    {
      isDefault: true,
      fieldId: "sf-status",
    },
  ),
  view(
    "sv-kanban",
    "Kanban",
    "kanban",
    "Chia cột theo Trạng thái · lọc Phân hệ Kế toán, Kho",
    "board",
    {
      fieldId: "sf-status",
      filters: [
        {
          fieldName: "Phân hệ ERP",
          operator: "có một trong",
          values: "Kế toán, Kho",
        },
      ],
      sort: { fieldName: "Hạn chót", text: "tăng dần · ô trống xếp cuối" },
      cardFieldIds: [
        "sf-owner",
        "sf-due",
        "sf-priority",
        "sf-module",
        "sf-progress",
      ],
      cardFieldChoices: [
        "sf-owner",
        "sf-due",
        "sf-priority",
        "sf-module",
        "sf-progress",
        "sf-estimate",
        "sf-depends",
      ],
    },
  ),
  view(
    "sv-calendar",
    "Lịch",
    "calendar",
    "Từ Ngày bắt đầu tới Hạn chót",
    "board",
    {
      fieldId: "sf-start",
    },
  ),
  view(
    "sv-timeline",
    "Dòng thời gian",
    "timeline",
    "Theo giai đoạn · có đường phụ thuộc",
    "board",
  ),
  view(
    "sv-handover",
    "Lịch bàn giao",
    "calendar",
    "Theo Ngày bàn giao (đã lưu trữ)",
    "board",
    {
      fieldId: "sf-handover",
      needsFix: true,
      createdBy: "Lê Hoàng Phúc",
      lastEditedBy: "Lê Hoàng Phúc",
      filters: [
        {
          fieldName: "Trạng thái",
          operator: "không phải",
          values: "Hoàn thành",
        },
      ],
    },
  ),
  view(
    "sv-urgent",
    "Việc khẩn của tôi",
    "table",
    "Người phụ trách là tôi · Mức ưu tiên Khẩn, Cao",
    "me",
    {
      createdBy: "Nguyễn Thu Hà",
      lastEditedBy: "Nguyễn Thu Hà",
    },
  ),
  view(
    "sv-week",
    "Tuần này",
    "table",
    "Lịch tuần · Hạn chót trong tuần",
    "me",
    {
      createdBy: "Nguyễn Thu Hà",
      lastEditedBy: "Nguyễn Thu Hà",
    },
  ),
];

const automations: BoardSettingsAutomationDto[] = [
  {
    id: "sa-review",
    name: "Gửi duyệt khi chuyển sang Chờ duyệt",
    enabled: true,
    when: "Trạng thái chuyển sang Chờ duyệt",
    then: ["Giao Nguyễn Thu Hà duyệt", "Gửi thông báo cho người duyệt"],
    stats: "Chạy 3 lần trong 7 ngày · lần cuối 10:28 hôm nay, thành công",
    runAs: { name: "Lê Hoàng Phúc", initials: "HP" },
  },
  {
    id: "sa-overdue",
    name: "Nhắc việc quá hạn",
    enabled: true,
    when: "Hạn chót đã qua",
    condition: "Trạng thái không phải Hoàn thành",
    then: ["Nhắc người phụ trách lúc 09:00 hằng ngày"],
    stats: "Chạy 7 lần trong 7 ngày · lần cuối 09:00 hôm nay, thành công",
    runAs: { name: "Lê Hoàng Phúc", initials: "HP" },
  },
  {
    id: "sa-urgent",
    name: "Khẩn → báo Trưởng phòng",
    enabled: true,
    when: "Mức ưu tiên đổi thành Khẩn",
    then: [
      "Gửi thông báo cho Lê Hoàng Phúc",
      'Đăng tin vào kênh Microsoft Teams "ERP – Điều hành"',
    ],
    stats: "Chạy 1 lần trong 7 ngày · lần cuối Thứ Bảy 10/10 18:05, ",
    partialFailure: "lỗi một phần",
    failureDetail: " — kết nối Microsoft Teams cần uỷ quyền lại",
    runAs: { name: "Lê Hoàng Phúc", initials: "HP" },
  },
  {
    id: "sa-warehouse",
    name: "Gắn nhóm Dữ liệu cho việc kho",
    enabled: false,
    when: "Có việc mới",
    condition: "Phân hệ ERP có Kho",
    then: ["Chuyển vào nhóm Dữ liệu"],
    stats: "Lần cuối chạy 07/10, thành công",
    runAs: { name: "Hồ Quang Minh", initials: "HM" },
    pausedReason: {
      lead: "Tạm dừng: Hồ Quang Minh đã rời Phòng Công nghệ, không còn quyền Sửa trên board.",
      rest: " Quy tắc không chạy từ 08/10. Đổi người chạy rồi bật lại.",
      action: "Đổi người chạy",
    },
  },
];

export function erpBoardSettings(): BoardSettingsDto {
  return {
    boardId: "board-erp",
    version: 1,
    itemCount: 16,
    general: {
      title: "Triển khai ERP Q4",
      description:
        "Đưa phân hệ Kế toán, Kho và Mua hàng lên ERP mới, go-live 01/11/2026.",
      color: "#1E6FD0",
      icon: "board",
      sampleItemKey: "ERP-142",
      itemKeyPrefix: "ERP",
      department: { id: "space-engineering", name: "Công nghệ" },
      timeZone: ERP_SETTINGS_TIME_ZONE,
      weekStart: "monday",
      isArchived: false,
    },
    fields,
    systemFields: [
      {
        id: "sys-title",
        key: "title",
        name: "Tên việc",
        type: "text",
        detail: "trường chính",
        lock: "rename",
      },
      {
        id: "sys-item-key",
        key: "item_key",
        name: "Mã việc",
        type: "autonumber",
        detail: "ERP-1, ERP-2…",
        lock: "locked",
      },
      {
        id: "sys-created-by",
        key: "created_by",
        name: "Người tạo",
        type: "person",
        detail: "",
        lock: "locked",
      },
      {
        id: "sys-created-at",
        key: "created_at",
        name: "Ngày tạo",
        type: "datetime-system",
        detail: "",
        lock: "locked",
      },
      {
        id: "sys-updated-at",
        key: "updated_at",
        name: "Cập nhật lần cuối",
        type: "datetime-system",
        detail: "",
        lock: "locked",
      },
    ],
    archivedFields: [
      {
        id: "sf-handover",
        key: "handover_date",
        type: "date",
        name: "Ngày bàn giao",
        archivedAt: "2026-10-02T04:00:00.000Z",
        archivedByName: "Trần Minh Quân",
        valueCount: 9,
      },
    ],
    views,
    groups: [
      { id: "g-prep", name: "Chuẩn bị", color: "#1E6FD0", itemCount: 6 },
      { id: "g-data", name: "Dữ liệu", color: "#0E7490", itemCount: 3 },
      { id: "g-test", name: "Kiểm thử", color: "#5532A8", itemCount: 2 },
      {
        id: "g-training",
        name: "Đào tạo và vận hành",
        color: "#9A6A12",
        itemCount: 5,
      },
    ],
    automations,
  };
}

/** A board with nothing configured yet: no field, one view, one group. */
export function emptyBoardSettings(): BoardSettingsDto {
  const base = erpBoardSettings();
  return {
    ...base,
    itemCount: 0,
    fields: [],
    archivedFields: [],
    views: [views[0]!],
    groups: [
      { id: "g-prep", name: "Chuẩn bị", color: "#1E6FD0", itemCount: 0 },
    ],
    automations: [],
  };
}

const done: BoardSettingsWriteResult = { ok: true };

/** Actions that answer ok and do nothing; tests override the ones they watch. */
export function noopSettingsActions(
  overrides: Partial<BoardSettingsActions> = {},
): BoardSettingsActions {
  const ok = () => Promise.resolve(done);
  return {
    updateGeneral: ok,
    archiveBoard: ok,
    deleteBoard: ok,
    createField: ok,
    reorderFields: ok,
    updateField: ok,
    archiveField: ok,
    deleteField: ok,
    restoreField: ok,
    addOption: ok,
    updateOption: ok,
    deleteOption: ok,
    reorderOptions: ok,
    createView: ok,
    reorderViews: ok,
    updateView: ok,
    duplicateView: ok,
    setDefaultView: ok,
    deleteView: ok,
    createGroup: ok,
    reorderGroups: ok,
    updateGroup: ok,
    deleteGroup: ok,
    setAutomationEnabled: ok,
    ...overrides,
  };
}
