/**
 * Deterministic table scenarios for stories and tests: the ERP board of the
 * Final-Table mockup (Cần làm 6, Đang làm 3, Chờ duyệt 2, Hoàn thành 5), the
 * default "hide finished items" chip and the variants the mockup draws
 * (selected rows, an open status list, no results, the archive notice,
 * compact rows) plus edge, dense, empty and read-only data. Rows are built
 * with the same functions the container uses. The clock is injected
 * (12/10/2026 10:40 Asia/Ho_Chi_Minh).
 */
import {
  DEFAULT_TABLE_SORT,
  findStatusField,
  groupCardsByStatus,
  orderByKey,
  projectTableLanes,
  type Card,
  type KanbanStatusLane,
  type TableDensity,
  type TableSort,
} from "@notrelix/work-management-core";
import {
  ERP_PEOPLE,
  erpBoardScenario,
  erpClock,
  erpItemId,
} from "@notrelix/work-management-testing";
import { isKanbanCardOverdue } from "../components/views/kanban/kanban-filter";
import type { ViewState } from "@notrelix/work-management-core";
import { erpViewBar } from "./view-state-fixtures";
import {
  mainTableCopy,
  type MainTableCopy,
} from "../components/views/table/main-table.copy";
import { statusOptionFormCopy } from "../components/status-option/status-option-form.copy";
import type { MainTableSurfaceProps } from "../components/views/table/main-table-surface";
import type { MainTableToolbarProps } from "../components/views/table/table-toolbar";
import {
  buildTableGroup,
  tableStatusOptions,
  type TableGroupModel,
  type TableStatusOption,
} from "../components/views/table/table-model";

export const TABLE_STORY_NOW = erpClock();

/** The item id of an ERP key (the ERP fixture names items `item-erp-142`). */
export const ERP_TABLE_ITEM = erpItemId;
export const TABLE_STORY_TIME_ZONE = "Asia/Ho_Chi_Minh";

const noop = () => undefined;

/** Every callback of the surface as a no-op (stories wrap the ones they use). */
export const tableNoopCallbacks = {
  onRowSelectedChange: noop,
  onSelectAllChange: noop,
  onClearSelection: noop,
  onGroupToggle: noop,
  onOpenItem: noop,
  onStatusChange: noop,
  onCreateItem: noop,
  onClearFilter: noop,
  onShowDone: noop,
  onBulkStatus: noop,
  onBulkDue: noop,
  onBulkArchive: noop,
} satisfies Pick<
  MainTableSurfaceProps,
  | "onRowSelectedChange"
  | "onSelectAllChange"
  | "onClearSelection"
  | "onGroupToggle"
  | "onOpenItem"
  | "onStatusChange"
  | "onCreateItem"
  | "onClearFilter"
  | "onShowDone"
  | "onBulkStatus"
  | "onBulkDue"
  | "onBulkArchive"
>;

/** The ERP board's status lanes (every item, in option order). */
export function erpTableLanes(): KanbanStatusLane[] {
  const { board, groups } = erpBoardScenario();
  return groupCardsByStatus({
    fieldDefinitions: board.fieldDefinitions,
    groups,
  });
}

export function erpTableStatusOptions(): TableStatusOption[] {
  const { board } = erpBoardScenario();
  const field = findStatusField(board.fieldDefinitions);
  return field
    ? tableStatusOptions(orderByKey(field.options, (option) => option.position))
    : [];
}

export interface TableScenarioOptions {
  locale?: "vi" | "en";
  lanes?: readonly KanbanStatusLane[];
  hideDone?: boolean;
  sort?: TableSort;
  density?: TableDensity;
  hiddenColumnIds?: readonly string[];
  /** Saved column widths by column id. */
  columnWidths?: Readonly<Record<string, number>>;
  /** The member's own filter, sort, group and swimlane shown by the view bar. */
  viewState?: Partial<ViewState>;
  searchQuery?: string;
  selected?: readonly string[];
  collapsed?: readonly string[];
  canEdit?: boolean;
  canCreate?: boolean;
  openItemId?: string | null;
  archivedNotice?: MainTableSurfaceProps["archivedNotice"];
  initialEditing?: MainTableSurfaceProps["initialEditing"];
  /** Search or filter narrow the board (lanes with no cards disappear). */
  narrowed?: boolean;
  boardTitle?: string;
}

/** The default hidden columns of a new view: start, created, completed, checklist, comments, files. */
export const TABLE_DEFAULT_HIDDEN_COLUMNS = [
  "start",
  "created",
  "completed",
  "checklist",
  "comments",
  "files",
];

/**
 * Props of the table surface for an ERP scenario. The default is the
 * mockup's `binh-thuong`: finished items hidden, sorted by due date, comfortable
 * rows.
 */
export function erpTableProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  const locale = options.locale ?? "vi";
  const copy: MainTableCopy = mainTableCopy(locale);
  const lanes = options.lanes ?? erpTableLanes();
  const statusOptions = erpTableStatusOptions();
  const statuses = new Map(statusOptions.map((o) => [o.id, o]));
  const hideDone = options.hideDone ?? true;
  const sort = options.sort ?? DEFAULT_TABLE_SORT;
  const projection = projectTableLanes({
    lanes,
    sort,
    hideDone,
    statusRank: (card: Card) =>
      statusOptions.findIndex((o) => o.id === card.statusOptionId),
    isOverdue: (card) => isKanbanCardOverdue(card, TABLE_STORY_NOW),
    narrowed: options.narrowed ?? false,
  });
  const groups: TableGroupModel[] = projection.groups.map((lane) =>
    buildTableGroup(lane, lane.name || copy.group.noStatus, {
      now: TABLE_STORY_NOW,
      copy,
      timeZone: TABLE_STORY_TIME_ZONE,
      statuses,
    }),
  );
  const canEdit = options.canEdit ?? true;
  const canCreate = options.canCreate ?? true;
  return {
    boardTitle: "Triển khai ERP Q4",
    toolbar: {
      copy,
      searchQuery: options.searchQuery ?? "",
      onSearchChange: noop,
      hideDone,
      onShowDone: noop,
      viewBar: erpViewBar({
        locale,
        view: "table",
        ...(options.viewState ? { state: options.viewState } : {}),
      }),
      sort,
      hiddenColumnIds: options.hiddenColumnIds ?? TABLE_DEFAULT_HIDDEN_COLUMNS,
      onColumnVisibleChange: noop,
      density: options.density ?? "comfortable",
      onDensityChange: noop,
      canAdd: canCreate,
    },
    groups,
    statusOptions,
    selectedIds: new Set(options.selected ?? []),
    collapsedGroupIds: new Set(options.collapsed ?? []),
    openItemId: options.openItemId ?? null,
    canEdit,
    canCreate,
    canArchive: canEdit,
    addGroup: canEdit
      ? {
          formCopy: statusOptionFormCopy(locale),
          kind: "status",
          existingNames: statusOptions.map((option) => option.name),
          onSubmit: noop,
        }
      : undefined,
    columnWidths: options.columnWidths ?? {},
    shown: projection.visibleCount,
    hidden: projection.hiddenCount,
    canShowDone: hideDone && projection.doneHiddenCount > 0,
    noResults: groups.length === 0 && projection.totalCount > 0,
    archivedNotice: options.archivedNotice ?? null,
    initialEditing: options.initialEditing,
    ...tableNoopCallbacks,
  };
}

/** Columns the person widened: the title and the assignee. */
export const TABLE_RESIZED_COLUMN_WIDTHS = { title: 440, assignee: 232 };

/** The saved view has custom column widths. */
export function erpTableResizedProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  return erpTableProps({
    ...options,
    columnWidths: TABLE_RESIZED_COLUMN_WIDTHS,
  });
}

/** `chon-nhieu`: ERP-142 and ERP-146 are selected. */
export function erpTableSelectedProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  return erpTableProps({
    ...options,
    selected: [ERP_TABLE_ITEM("ERP-142"), ERP_TABLE_ITEM("ERP-146")],
  });
}

/** `sua-o`: the status list of ERP-144 is open. */
export function erpTableEditingProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  return erpTableProps({
    ...options,
    initialEditing: {
      rowId: ERP_TABLE_ITEM("ERP-144"),
      highlightStatusId: "status-review",
    },
  });
}

/** `khong-ket-qua`: the done chip and a person who has no open item. */
export function erpTableNoResultsProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  const props = erpTableProps({
    ...options,
    narrowed: true,
    viewState: {
      filter: {
        join: "and",
        conditions: [
          {
            fieldId: "field-erp-owner",
            operator: "has_any",
            values: [ERP_PEOPLE.DT.userId],
          },
        ],
      },
    },
    lanes: erpTableLanes().map((lane) => ({ ...lane, cards: [] })),
  });
  return props;
}

/** `da-luu-tru`: two items were archived and "Hoàn tác" is offered. */
export function erpTableArchivedProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  const lanes = erpTableLanes().map((lane) => {
    const cards = lane.cards.filter(
      (card) =>
        card.id !== ERP_TABLE_ITEM("ERP-142") &&
        card.id !== ERP_TABLE_ITEM("ERP-146"),
    );
    return { ...lane, cards, total: cards.length };
  });
  return erpTableProps({
    ...options,
    lanes,
    archivedNotice: { count: 2, onUndo: noop, onClose: noop },
  });
}

/** A new board: the lanes exist and hold no item. */
export function erpTableEmptyProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  return erpTableProps({
    ...options,
    lanes: erpTableLanes().map((lane) => ({ ...lane, cards: [], total: 0 })),
  });
}

/** `chi-xem`: a viewer can read, select and open but not change anything. */
export function erpTableReadOnlyProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  return erpTableProps({ ...options, canEdit: false, canCreate: false });
}

/**
 * Edge data: a very long title, an item without key, label, date or person,
 * a long group name and the group of items without a status.
 */
export function erpTableEdgeProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  const lanes = erpTableLanes();
  const [todo, doing, review, done] = lanes as [
    KanbanStatusLane,
    KanbanStatusLane,
    KanbanStatusLane,
    KanbanStatusLane,
  ];
  const first = todo.cards[0]!;
  const longTitle: Card = {
    ...first,
    id: "edge-long-title",
    title:
      "Đối soát toàn bộ số dư công nợ phải thu và phải trả của mười hai chi nhánh trước khi khoá sổ kế toán quý IV và chuyển số liệu sang hệ thống ERP mới",
    members: [
      {
        id: "edge-member",
        userId: "edge-user",
        name: "Nguyễn Hoàng Phương Thảo Nguyên (Phòng Kế toán – Chi nhánh Hà Nội)",
        initials: "NT",
        color: "#1E6FD0",
      },
    ],
  };
  const bare: Card = {
    ...first,
    id: "edge-bare",
    title: "Việc chưa có mã, nhãn, hạn hay người phụ trách",
    itemKey: null,
    labels: [],
    members: [],
    dueAt: null,
    dueDate: undefined,
    childProgress: null,
    priority: undefined,
    updatedAt: undefined,
  };
  const withoutStatus: KanbanStatusLane = {
    optionId: null,
    name: "",
    category: null,
    color: null,
    cards: [{ ...bare, id: "edge-no-status", statusOptionId: null }],
    total: 1,
  };
  return erpTableProps({
    ...options,
    lanes: [
      {
        ...todo,
        cards: [longTitle, bare, ...todo.cards.slice(1, 3)],
        total: 4,
      },
      {
        ...doing,
        name: "Đang triển khai tại các chi nhánh miền Nam và miền Trung",
      },
      review,
      done,
      withoutStatus,
    ],
    hideDone: false,
  });
}

/** High density: six times the cards in every lane, compact rows. */
export function erpTableDenseProps(
  options: TableScenarioOptions = {},
): MainTableSurfaceProps {
  const lanes = erpTableLanes().map((lane) => {
    const cards = Array.from({ length: 6 }, (_, round) =>
      lane.cards.map((card) => ({
        ...card,
        id: `${card.id}-${round}`,
        itemKey: card.itemKey ? `${card.itemKey}.${round}` : card.itemKey,
      })),
    ).flat();
    return { ...lane, cards, total: cards.length };
  });
  return erpTableProps({
    ...options,
    lanes,
    density: "compact",
    hideDone: false,
  });
}

/** Toolbar props for the toolbar stories and tests. */
export function erpTableToolbarProps(
  variant: "default" | "filtered" | "read-only" | "edge" = "default",
  locale: "vi" | "en" = "vi",
): MainTableToolbarProps {
  const base: MainTableToolbarProps = {
    ...erpTableProps({ locale }).toolbar,
    onAddItem: noop,
  };
  if (variant === "filtered")
    return {
      ...base,
      viewBar: erpViewBar({
        locale,
        view: "table",
        state: {
          filter: {
            join: "and",
            conditions: [
              {
                fieldId: "field-erp-priority",
                operator: "has_any",
                values: ["p-urgent", "p-high"],
              },
              {
                fieldId: "field-erp-owner",
                operator: "has_any",
                values: [ERP_PEOPLE.DT.userId],
              },
              { fieldId: "$overdue", operator: "is_true", values: [] },
            ],
          },
        },
        counts: { shown: 4, total: 16 },
      }),
    };
  if (variant === "read-only") return { ...base, canAdd: false };
  if (variant === "edge") {
    return {
      ...base,
      searchQuery:
        "đối soát hoá đơn đầu vào quý 3 cho toàn bộ chi nhánh phía Bắc",
      viewBar: erpViewBar({
        locale,
        view: "table",
        state: {
          filter: {
            join: "or",
            conditions: [
              {
                fieldId: "field-erp-priority",
                operator: "has_any",
                values: ["p-urgent", "p-high", "p-medium", "p-low"],
              },
              {
                fieldId: "field-erp-owner",
                operator: "has_any",
                values: [ERP_PEOPLE.HA.userId, ERP_PEOPLE.DT.userId],
              },
              {
                fieldId: "$title",
                operator: "contains",
                values: ["đối soát hoá đơn đầu vào quý 3 chi nhánh phía Bắc"],
              },
            ],
          },
          sorts: [
            { fieldId: "field-erp-owner", direction: "desc" },
            { fieldId: "field-erp-due", direction: "asc" },
          ],
          group: { fieldId: "field-erp-owner" },
        },
      }),
      sort: { key: "assignee", direction: "desc" },
      hiddenColumnIds: [],
    };
  }
  return base;
}
