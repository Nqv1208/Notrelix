/**
 * Deterministic fixtures for the Kanban toolbar surface: the ERP board's
 * statuses, people, labels and grouping fields as the mockup shows them.
 * Option names are board data, so they stay Vietnamese in both locales.
 */
import {
  kanbanToolbarCopy,
  type KanbanToolbarCopy,
} from "../components/views/kanban/kanban-toolbar.copy";
import {
  emptyKanbanFilter,
  type KanbanFilterState,
} from "../components/views/kanban/kanban-filter";
import type { KanbanToolbarFilterOptions } from "../components/views/kanban/kanban-filter-menu";
import type {
  KanbanGroupByField,
  KanbanToolbarProps,
} from "../components/views/kanban/kanban-toolbar";

export function kanbanToolbarCopyFixture(
  locale: "vi" | "en" = "vi",
): KanbanToolbarCopy {
  return kanbanToolbarCopy(locale);
}

export const KANBAN_TOOLBAR_FILTER_OPTIONS: KanbanToolbarFilterOptions = {
  statuses: [
    { id: "status-todo", name: "Cần làm" },
    { id: "status-doing", name: "Đang làm" },
    { id: "status-review", name: "Chờ duyệt" },
    { id: "status-done", name: "Hoàn thành" },
  ],
  assignees: [
    { id: "user-nguyen-an", name: "Nguyễn An" },
    { id: "user-khanh-linh", name: "Khánh Linh" },
    { id: "user-tran-thao", name: "Trần Thảo" },
  ],
  labels: [
    { id: "label-security", name: "Bảo mật" },
    { id: "label-doc", name: "Tài liệu" },
    { id: "label-training", name: "Đào tạo" },
    { id: "label-report", name: "Báo cáo" },
    { id: "label-data", name: "Dữ liệu" },
    { id: "label-test", name: "Kiểm thử" },
  ],
};

export const KANBAN_TOOLBAR_GROUP_BY_FIELDS: readonly KanbanGroupByField[] = [
  { id: "field-status", name: "Trạng thái", kind: "status" },
  { id: "field-priority", name: "Ưu tiên", kind: "select" },
  { id: "field-label", name: "Nhãn", kind: "select" },
];

/** The khong-ket-qua filter: "Quá hạn · Ưu tiên Thấp". */
export function kanbanToolbarOverdueLowFilter(): KanbanFilterState {
  return { ...emptyKanbanFilter(), priorities: ["low"], overdue: true };
}

export type KanbanToolbarScenario = Omit<
  KanbanToolbarProps,
  "onSearchChange" | "onFilterChange" | "onGroupByChange" | "onAddItem"
>;

export function kanbanToolbarDefaultScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return {
    copy: kanbanToolbarCopyFixture(locale),
    searchQuery: "",
    filter: emptyKanbanFilter(),
    filterOptions: KANBAN_TOOLBAR_FILTER_OPTIONS,
    groupByFields: KANBAN_TOOLBAR_GROUP_BY_FIELDS,
    groupByFieldId: "field-status",
    canAdd: true,
    noResults: null,
  };
}

export function kanbanToolbarFilteredScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return {
    ...kanbanToolbarDefaultScenario(locale),
    filter: {
      ...emptyKanbanFilter(),
      statusOptionIds: ["status-doing"],
      labelIds: ["label-security"],
    },
  };
}

/** Board khong-ket-qua: the filter hides all 16 items of the ERP board. */
export function kanbanToolbarNoResultsScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return {
    ...kanbanToolbarDefaultScenario(locale),
    filter: kanbanToolbarOverdueLowFilter(),
    noResults: { hiddenCount: 16 },
  };
}

/** Board chi-xem: a commenter cannot add items. */
export function kanbanToolbarReadOnlyScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  return { ...kanbanToolbarDefaultScenario(locale), canAdd: false };
}

/** Long names, every facet selected, a long search and a long field name. */
export function kanbanToolbarEdgeDataScenario(
  locale: "vi" | "en" = "vi",
): KanbanToolbarScenario {
  const longAssignee = {
    id: "user-long",
    name: "Nguyễn Hoàng Phương Thảo Nguyên (Phòng Kế toán – Chi nhánh Hà Nội)",
  };
  return {
    ...kanbanToolbarDefaultScenario(locale),
    searchQuery:
      "đối soát hoá đơn đầu vào quý 3 cho toàn bộ chi nhánh phía Bắc",
    filterOptions: {
      ...KANBAN_TOOLBAR_FILTER_OPTIONS,
      assignees: [...KANBAN_TOOLBAR_FILTER_OPTIONS.assignees, longAssignee],
    },
    groupByFields: [
      ...KANBAN_TOOLBAR_GROUP_BY_FIELDS,
      {
        id: "field-long",
        name: "Giai đoạn triển khai theo kế hoạch tổng thể năm tài chính",
        kind: "select",
      },
    ],
    groupByFieldId: "field-long",
    filter: {
      statusOptionIds: ["status-todo", "status-doing", "status-review"],
      priorities: ["urgent", "high", "medium", "low"],
      assigneeIds: [longAssignee.id, "user-nguyen-an"],
      labelIds: ["label-security", "label-doc", "label-report"],
      overdue: true,
    },
  };
}
