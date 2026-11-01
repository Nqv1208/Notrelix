/**
 * Table scenario with the custom field columns: one column of each type the
 * table edits, a formula, and cells pinned to a saving, failed or conflicting
 * write. Built on the ERP table scenario of `table-fixtures`.
 */
import {
  personInitials,
  type FieldDefinition,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";
import { ERP_PEOPLE } from "@notrelix/work-management-testing";
import type { MainTableSurfaceProps } from "../components/views/table/main-table-surface";
import type {
  CellWrite,
  TableFieldEditing,
} from "../components/views/table/table-field-cell";
import {
  ERP_TABLE_ITEM,
  erpTableLanes,
  erpTableProps,
  type TableScenarioOptions,
} from "./table-fixtures";

const noop = () => undefined;

const FIELD_BASE = {
  boardId: "board-erp",
  isHidden: false,
  isSystemField: false,
} as const;

const MODULES = [
  { id: "module-accounting", label: "Kế toán", color: "#1E6FD0" },
  { id: "module-debt", label: "Công nợ", color: "#7C4DDB" },
  { id: "module-purchasing", label: "Mua hàng", color: "#0E7490" },
  { id: "module-stock", label: "Kho", color: "#9A6A12" },
];

/** One custom field of each type the table edits, and a formula over the number. */
export const TABLE_CUSTOM_FIELDS: readonly FieldDefinition[] = [
  { ...FIELD_BASE, id: "field-note", name: "Ghi chú", fieldType: "long_text" },
  { ...FIELD_BASE, id: "field-link", name: "Liên kết", fieldType: "link" },
  { ...FIELD_BASE, id: "field-points", name: "Điểm", fieldType: "number" },
  {
    ...FIELD_BASE,
    id: "field-double",
    name: "Điểm gấp đôi",
    fieldType: "formula",
  },
  {
    ...FIELD_BASE,
    id: "field-accepted",
    name: "Nghiệm thu",
    fieldType: "checkbox",
  },
  {
    ...FIELD_BASE,
    id: "field-module",
    name: "Phân hệ",
    fieldType: "select",
    options: MODULES.slice(0, 3),
  },
  {
    ...FIELD_BASE,
    id: "field-related",
    name: "Phân hệ liên quan",
    fieldType: "multi_select",
    options: MODULES,
  },
  {
    ...FIELD_BASE,
    id: "field-deadline",
    name: "Hạn nghiệm thu",
    fieldType: "date",
  },
  {
    ...FIELD_BASE,
    id: "field-reviewer",
    name: "Người nghiệm thu",
    fieldType: "person",
  },
].map((field, index) => ({
  options: [],
  ...field,
  position: index + 1,
})) as FieldDefinition[];

/** The values the scenario's rows hold, by ERP key. */
const VALUES: Readonly<Record<string, Record<string, unknown>>> = {
  "ERP-142": {
    "field-note": "Cần xuất bản Excel",
    "field-link": "https://erp.minhan.vn/t/142",
    "field-points": 8,
    "field-accepted": true,
    "field-module": "module-accounting",
    "field-related": ["module-accounting", "module-debt", "module-purchasing"],
    "field-deadline": "2026-10-15",
    "field-reviewer": [ERP_PEOPLE.MQ.userId, ERP_PEOPLE.DT.userId],
  },
  "ERP-145": {
    "field-points": 13,
    "field-module": "module-debt",
    "field-related": ["module-stock"],
  },
};

/** The rows with their custom values and the formula's result. */
export function withTableFieldValues(
  lanes: readonly KanbanStatusLane[],
): KanbanStatusLane[] {
  return lanes.map((lane) => ({
    ...lane,
    cards: lane.cards.map((card) => {
      const values = VALUES[card.itemKey ?? ""] ?? {};
      const points = values["field-points"];
      return {
        ...card,
        fieldValues: values,
        computedValues: {
          "field-double":
            typeof points === "number"
              ? { state: "ok" as const, value: points * 2 }
              : { state: "empty" as const },
        },
      };
    }),
  }));
}

/** Where a write state is pinned: ERP key and field id. */
export function writeKey(erpKey: string, fieldId: string): string {
  return `${ERP_TABLE_ITEM(erpKey)}:${fieldId}`;
}

/** Editing of the custom cells: every write is a no-op; `writes` pins a cell to a write state. */
export function tableFieldEditing(
  canEdit: boolean,
  writes: Readonly<Record<string, CellWrite>> = {},
): TableFieldEditing {
  return {
    canEdit,
    members: Object.values(ERP_PEOPLE).map((person) => ({
      userId: person.userId,
      name: person.name,
      initials: personInitials(person.name),
    })),
    writeOf: (rowId, fieldId) => writes[`${rowId}:${fieldId}`],
    onWrite: noop,
    onRetry: noop,
    onKeepMine: noop,
    onUseTheirs: noop,
  };
}

/** The table with the custom field columns shown (the "Cột" menu turns them on). */
export function erpTableFieldProps(
  options: TableScenarioOptions & {
    writes?: Readonly<Record<string, CellWrite>>;
  } = {},
): MainTableSurfaceProps {
  const { writes, ...rest } = options;
  return erpTableProps({
    ...rest,
    lanes: withTableFieldValues(options.lanes ?? erpTableLanes()),
    customFields: TABLE_CUSTOM_FIELDS,
    fieldEditing: tableFieldEditing(options.canEdit ?? true, writes),
    hideDone: false,
  });
}
