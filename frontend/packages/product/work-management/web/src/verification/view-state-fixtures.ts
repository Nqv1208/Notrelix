/**
 * Deterministic fixtures for the view controls (filter builder, sort, group,
 * swimlane, personal-state chip): the ERP board's fields and people, and a
 * static `ViewStateControl` the stories and tests drive. Option names are
 * board data, so they stay Vietnamese in both locales.
 */
import { createElement, type ReactNode } from "react";
import {
  buildFilterFields,
  emptyViewState,
  groupableFields,
  type FieldDefinition,
  type FilterField,
  type ViewState,
} from "@notrelix/work-management-core";
import {
  filtersCopy,
  type FiltersCopy,
} from "../components/filters/filters.copy";
import type { ViewStateControl } from "../components/filters/view-state-control";
import { ViewStateBar } from "../components/filters/view-state-bar";

/** Mon 12/10/2026, the day the mockups are drawn for. */
export const VIEW_NOW = new Date("2026-10-12T03:00:00.000Z");

export const VIEW_PEOPLE = [
  { id: "user-dang-minh-tuan", name: "Đặng Minh Tuấn" },
  { id: "user-phan-ngoc-anh", name: "Phan Ngọc Anh" },
  { id: "user-khanh-linh", name: "Đỗ Khánh Linh" },
] as const;

const definition = (
  id: string,
  fieldType: FieldDefinition["fieldType"],
  extra: Partial<FieldDefinition> = {},
): FieldDefinition => ({
  id,
  boardId: "board-erp",
  name: id,
  fieldType,
  options: [],
  position: 1,
  isHidden: false,
  isSystemField: false,
  ...extra,
});

export const VIEW_FIELD_DEFINITIONS: readonly FieldDefinition[] = [
  definition("field-erp-status", "select", {
    name: "Trạng thái",
    semanticRole: "status",
    options: [
      { id: "status-todo", label: "Cần làm", color: "" },
      { id: "status-doing", label: "Đang làm", color: "" },
      { id: "status-review", label: "Chờ duyệt", color: "" },
      { id: "status-done", label: "Hoàn thành", color: "" },
    ],
  }),
  definition("field-erp-priority", "select", {
    name: "Ưu tiên",
    semanticRole: "priority",
    options: [
      { id: "p-urgent", label: "Khẩn", color: "" },
      { id: "p-high", label: "Cao", color: "" },
      { id: "p-medium", label: "Trung bình", color: "" },
      { id: "p-low", label: "Thấp", color: "" },
    ],
  }),
  definition("field-erp-owner", "person", { name: "Người phụ trách" }),
  definition("field-erp-due", "date", { name: "Hạn" }),
  definition("field-erp-done", "checkbox", { name: "Đã duyệt" }),
  definition("field-erp-risk", "formula", { name: "Rủi ro" }),
];

export function viewFields(locale: "vi" | "en" = "vi"): FilterField[] {
  const copy = filtersCopy(locale);
  return buildFilterFields({
    fieldDefinitions: VIEW_FIELD_DEFINITIONS,
    members: VIEW_PEOPLE.map((p) => ({ userId: p.id, name: p.name })),
    labels: [
      { id: "label-security", name: "Bảo mật" },
      { id: "label-doc", name: "Tài liệu" },
    ],
    names: {
      title: copy.field.title,
      label: copy.field.label,
      overdue: copy.filter.presets.overdue,
    },
    isOverdue: () => false,
  });
}

export const viewMemberName = (userId: string): string =>
  VIEW_PEOPLE.find((p) => p.id === userId)?.name ?? userId;

const noop = () => undefined;

/** A static control over `state`; pass spies for the setters a test asserts on. */
export function viewControl(
  state: Partial<ViewState> = {},
  overrides: Partial<ViewStateControl> = {},
): ViewStateControl {
  const full: ViewState = { ...emptyViewState(), ...state };
  const personal =
    full.filter.conditions.length > 0 ||
    full.sorts.length > 0 ||
    Boolean(full.group) ||
    Boolean(full.lane);
  return {
    state: full,
    applied: full,
    loaded: true,
    personal,
    setFilter: noop,
    setSorts: noop,
    setGroup: noop,
    setLane: noop,
    applyCriteria: noop,
    reset: noop,
    persistent: true,
    saveFailed: false,
    retrySave: noop,
    ...overrides,
  } as ViewStateControl;
}

export interface ViewBarOptions {
  locale?: "vi" | "en";
  state?: Partial<ViewState>;
  control?: Partial<ViewStateControl>;
  /** Which view the bar is for: the Table has sort and group, the Kanban group and lane, the Calendar neither. */
  view?: "table" | "kanban" | "calendar";
  counts?: { shown: number; total: number };
  fields?: readonly FilterField[];
  saved?: ReactNode;
  canSave?: boolean;
  onSaveAsNew?: () => void;
}

/** The view bar of the ERP board as a toolbar slot. */
export function erpViewBar(options: ViewBarOptions = {}): ReactNode {
  const locale = options.locale ?? "vi";
  const copy: FiltersCopy = filtersCopy(locale);
  const fields = options.fields ?? viewFields(locale);
  const view = options.view ?? "table";
  const groupable = groupableFields(fields);
  return createElement(ViewStateBar, {
    copy,
    view: viewControl(options.state, options.control),
    fields,
    memberName: viewMemberName,
    people: VIEW_PEOPLE,
    counts: options.counts ?? { shown: 16, total: 16 },
    now: VIEW_NOW,
    sort: view === "table",
    ...(view === "calendar"
      ? {}
      : {
          groupFields:
            view === "kanban"
              ? groupable.filter((f) => f.field.fieldType === "select")
              : groupable,
        }),
    ...(view === "kanban"
      ? { laneFields: groupable, blockedLaneFieldId: "field-erp-status" }
      : {}),
    savedFilters: options.saved,
    ...(options.canSave === undefined ? {} : { canSave: options.canSave }),
    onSaveAsNew: options.onSaveAsNew,
  });
}
