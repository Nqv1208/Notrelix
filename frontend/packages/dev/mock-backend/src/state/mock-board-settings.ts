/**
 * "Thiết lập board" of the mock world (docs/design/mockups/2026-10/src/
 * Final-Settings.dc.html): the board's data fields with their keys, options
 * and where they are used, the saved views, the groups, the automations and
 * the general settings. The generated contract has none of these reads, so
 * they are one registered gap (`CTR-GAP-WM-BOARD-SETTINGS`) with the shapes the
 * frontend needs. Fields backed by a board column read and write that column
 * (name, options), so the board follows the settings; the rest of a field's
 * facts (key, usage, default, hint) live here.
 *
 * Pure state: `MockStore.seedErpBoard` seeds the ERP board; any other board
 * gets a default registry from its columns on first read.
 */
import type { MockStore } from "./mock-store";
import type { MockColumnRecord } from "./records";

export type SettingsFieldType =
  | "text"
  | "longtext"
  | "number"
  | "date"
  | "checkbox"
  | "file"
  | "select"
  | "multiselect"
  | "status"
  | "person"
  | "relation"
  | "formula"
  | "rollup"
  | "progress"
  | "autonumber"
  | "datetime-system";

export interface SettingsUsage {
  readonly kind: "view" | "card" | "filter" | "rule";
  readonly name: string;
  readonly how: string;
}

export interface SettingsFieldRecord {
  id: string;
  key: string;
  type: SettingsFieldType;
  name: string;
  columnId?: string;
  /** The words under the type ("nhiều người", "có giờ, GMT+7"). */
  detail?: string;
  /** The short "Đang dùng" cell ("5 view", "Chưa dùng"). */
  usage: string;
  /** Draw `usage` as a chip (the field that splits the Kanban). */
  accent?: boolean;
  usedIn: SettingsUsage[];
  defaultOptionId: string | null;
  required: boolean;
  guidance: string;
  unit?: string;
  archived?: { at: string; byName: string; values: number };
}

export interface SettingsSystemField {
  readonly id: string;
  readonly key: string;
  readonly name: string;
  readonly type: SettingsFieldType;
  readonly detail: string;
  readonly lock: "rename" | "locked";
}

export interface SettingsViewRecord {
  id: string;
  name: string;
  kind: "table" | "kanban" | "calendar" | "timeline";
  summary: string;
  visibility: "board" | "me";
  isDefault: boolean;
  /** The field the view splits or places by (Kanban group, Calendar date). */
  fieldId: string | null;
  filters: { fieldName: string; operator: string; values: string }[];
  sort: { fieldName: string; text: string } | null;
  cardFieldIds: string[];
  /** The fields a card of the view can show (the producer's choice). */
  cardFieldChoices: string[];
  lastEditedBy: string;
  createdBy: string;
}

export interface SettingsGroupColor {
  id: string;
  color: string;
}

export interface SettingsAutomationRecord {
  id: string;
  name: string;
  enabled: boolean;
  when: string;
  condition?: string;
  then: string[];
  stats: string;
  runAs: { name: string; initials: string };
  /** Why a paused rule is paused and the way to resume it. */
  pausedReason?: { lead: string; rest: string; action: string };
  /** "lỗi một phần" inside the stats line, drawn in danger colour. */
  partialFailure?: string;
  failureDetail?: string;
}

export interface SettingsGeneralRecord {
  icon: "board";
  /** An existing item key the key hint names ("ERP-142 vẫn mở được"). */
  sampleItemKey?: string;
  timeZone: string;
  weekStart: "monday" | "sunday";
}

export interface BoardSettingsRegistry {
  /** Bumped by every settings write; a write may carry the version it read. */
  version: number;
  fields: SettingsFieldRecord[];
  views: SettingsViewRecord[];
  automations: SettingsAutomationRecord[];
  general: SettingsGeneralRecord;
  /** Colours of the groups (a list has one colour, the mock keeps it here too). */
  nextViewNumber: number;
}

export type SettingsWriteResult<T = void> =
  | { readonly result: "ok"; readonly value: T }
  | {
      readonly result: "not-found" | "invalid" | "conflict";
      readonly message?: string;
    };

const TYPE_FOR_COLUMN: Readonly<Record<string, SettingsFieldType>> = {
  status: "status",
  user: "person",
  select: "select",
  multiselect: "multiselect",
  number: "number",
  date: "date",
  text: "text",
};

const KEY_FOR_COLUMN_TYPE: Readonly<Record<string, string>> = {
  text: "text",
  number: "number",
  date: "date",
  select: "select",
  multiselect: "multi",
};

function slug(text: string): string {
  return (
    text
      .normalize("NFD")
      .replace(/[̀-ͯ]/g, "")
      .replace(/đ/gi, "d")
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "_")
      .replace(/^_+|_+$/g, "") || "field"
  );
}

/** One board's settings: reads project the board's own data, writes change it. */
export class MockBoardSettingsState {
  private readonly registries = new Map<string, BoardSettingsRegistry>();

  constructor(private readonly store: MockStore) {}

  clear(): void {
    this.registries.clear();
  }

  set(boardId: string, registry: BoardSettingsRegistry): void {
    this.registries.set(boardId, registry);
  }

  /** The registry of a board, built from its columns when none was seeded. */
  registry(boardId: string): BoardSettingsRegistry {
    const existing = this.registries.get(boardId);
    if (existing) return existing;
    const fields = this.store
      .getColumns(boardId)
      .filter((column) => TYPE_FOR_COLUMN[column.fieldType] !== undefined)
      .map((column): SettingsFieldRecord => ({
        id: `sf-${column.id}`,
        key: slug(column.name),
        type: TYPE_FOR_COLUMN[column.fieldType] ?? "text",
        name: column.name,
        columnId: column.id,
        usage: "Chưa dùng",
        usedIn: [],
        defaultOptionId: null,
        required: false,
        guidance: "",
      }));
    const registry: BoardSettingsRegistry = {
      fields,
      views: [
        {
          id: "sv-table",
          name: "Bảng",
          kind: "table",
          summary: "Nhóm theo Trạng thái",
          visibility: "board",
          isDefault: true,
          fieldId: null,
          filters: [],
          sort: null,
          cardFieldIds: [],
          cardFieldChoices: [],
          lastEditedBy: "",
          createdBy: "",
        },
        {
          id: "sv-kanban",
          name: "Kanban",
          kind: "kanban",
          summary: "Chia cột theo Trạng thái",
          visibility: "board",
          isDefault: false,
          fieldId: fields.find((field) => field.type === "status")?.id ?? null,
          filters: [],
          sort: null,
          cardFieldIds: [],
          cardFieldChoices: [],
          lastEditedBy: "",
          createdBy: "",
        },
      ],
      automations: [],
      general: {
        icon: "board",
        timeZone: "Asia/Ho_Chi_Minh",
        weekStart: "monday",
      },
      nextViewNumber: 1,
      version: 1,
    };
    this.registries.set(boardId, registry);
    return registry;
  }

  // ── Reads ──

  /** The number of top-level, unarchived items of a board. */
  private itemsOf(boardId: string) {
    return this.store
      .getLists(boardId)
      .flatMap((list) => this.store.getCards(list.id))
      .filter((card) => !card.parentItemId && !card.isArchived);
  }

  column(field: SettingsFieldRecord): MockColumnRecord | undefined {
    return field.columnId ? this.store.getColumn(field.columnId) : undefined;
  }

  /** The options of a field with how many items carry each. */
  optionsOf(boardId: string, field: SettingsFieldRecord) {
    const column = this.column(field);
    if (!column) return [];
    const items = this.itemsOf(boardId);
    return [...(column.options ?? [])]
      .sort((a, b) => (a.position < b.position ? -1 : 1))
      .map((option) => ({
        id: option.id,
        name: option.name,
        color: option.color,
        itemCount: items.filter((card) => {
          const value = this.store.getFieldValues(card.id)[column.id];
          return Array.isArray(value)
            ? value.includes(option.id)
            : value === option.id;
        }).length,
      }));
  }

  /** How many items have a value in a field. */
  valueCount(boardId: string, field: SettingsFieldRecord): number {
    const column = this.column(field);
    if (!column) return this.itemsOf(boardId).length;
    return this.itemsOf(boardId).filter((card) => {
      const value = this.store.getFieldValues(card.id)[column.id];
      return value !== undefined && value !== null && value !== "";
    }).length;
  }

  // ── Fields ──

  updateField(
    boardId: string,
    fieldId: string,
    patch: {
      name?: string;
      defaultOptionId?: string | null;
      required?: boolean;
      guidance?: string;
    },
  ): SettingsWriteResult {
    const field = this.registry(boardId).fields.find(
      (entry) => entry.id === fieldId,
    );
    if (!field) return { result: "not-found" };
    if (patch.name !== undefined) {
      const name = patch.name.trim();
      if (!name) return { result: "invalid", message: "name" };
      const clash = this.registry(boardId).fields.some(
        (entry) =>
          entry.id !== fieldId &&
          !entry.archived &&
          entry.name.toLowerCase() === name.toLowerCase(),
      );
      if (clash) return { result: "invalid", message: "name" };
      field.name = name;
      if (field.columnId) this.store.updateColumn(field.columnId, { name });
    }
    if (patch.defaultOptionId !== undefined)
      field.defaultOptionId = patch.defaultOptionId;
    if (patch.required !== undefined) field.required = patch.required;
    if (patch.guidance !== undefined) field.guidance = patch.guidance;
    return { result: "ok", value: undefined };
  }

  createField(
    boardId: string,
    type: SettingsFieldType,
  ): SettingsWriteResult<SettingsFieldRecord> {
    const supported = [
      "text",
      "longtext",
      "number",
      "date",
      "checkbox",
      "select",
      "multiselect",
    ];
    const registry = this.registry(boardId);
    const base = "Trường mới";
    let name = base;
    for (let n = 2; registry.fields.some((f) => f.name === name); n += 1)
      name = `${base} ${n}`;
    let columnId: string | undefined;
    if (supported.includes(type)) {
      const column = this.store.createColumn(boardId, {
        name,
        fieldType: type === "longtext" ? "text" : type,
      });
      columnId = column.id;
    }
    const keyBase = KEY_FOR_COLUMN_TYPE[type] ?? type;
    let key = `${keyBase}_${registry.fields.length + 1}`;
    while (registry.fields.some((f) => f.key === key)) key += "_";
    const field: SettingsFieldRecord = {
      id: `sf-${columnId ?? key}`,
      key,
      type,
      name,
      columnId,
      usage: "Chưa dùng",
      usedIn: [],
      defaultOptionId: null,
      required: false,
      guidance: "",
    };
    registry.fields.push(field);
    return { result: "ok", value: field };
  }

  reorderFields(boardId: string, ids: readonly string[]): SettingsWriteResult {
    const registry = this.registry(boardId);
    const live = registry.fields.filter((field) => !field.archived);
    if (
      ids.length !== live.length ||
      !ids.every((id) => live.some((field) => field.id === id))
    )
      return { result: "invalid" };
    const archived = registry.fields.filter((field) => field.archived);
    registry.fields = [
      ...ids.map((id) => live.find((field) => field.id === id)!),
      ...archived,
    ];
    return { result: "ok", value: undefined };
  }

  archiveField(
    boardId: string,
    fieldId: string,
  ): SettingsWriteResult<{ viewsAffected: number }> {
    const registry = this.registry(boardId);
    const field = registry.fields.find((entry) => entry.id === fieldId);
    if (!field || field.archived) return { result: "not-found" };
    if (field.type === "status")
      return { result: "invalid", message: "status" };
    field.archived = {
      at: this.store.getClock().isoNow(),
      byName: this.store.getCurrentUser().name,
      values: this.valueCount(boardId, field),
    };
    if (field.columnId)
      this.store.updateColumn(field.columnId, { isHidden: true });
    const affected = registry.views.filter(
      (view) =>
        view.fieldId === field.id || view.cardFieldIds.includes(field.id),
    );
    return { result: "ok", value: { viewsAffected: affected.length } };
  }

  restoreField(boardId: string, fieldId: string): SettingsWriteResult {
    const field = this.registry(boardId).fields.find(
      (entry) => entry.id === fieldId,
    );
    if (!field?.archived) return { result: "not-found" };
    field.archived = undefined;
    if (field.columnId)
      this.store.updateColumn(field.columnId, { isHidden: false });
    return { result: "ok", value: undefined };
  }

  // ── Options of a select field ──

  addOption(
    boardId: string,
    fieldId: string,
    input: { name: string; color: string },
  ): SettingsWriteResult {
    const field = this.registry(boardId).fields.find((f) => f.id === fieldId);
    const column = field ? this.column(field) : undefined;
    if (!field || !column) return { result: "not-found" };
    const name = input.name.trim();
    if (!name) return { result: "invalid" };
    if (
      (column.options ?? []).some(
        (option) => option.name.toLowerCase() === name.toLowerCase(),
      )
    )
      return { result: "invalid", message: "name" };
    if (column.fieldType === "select" || column.fieldType === "multiselect") {
      const options = [...(column.options ?? [])];
      const last = options.at(-1)?.position;
      this.store.setColumnOptions(column.id, [
        ...options,
        {
          id: this.store.nextId("opt"),
          name,
          color: input.color,
          position: last === undefined ? "a0" : `${last}0`,
        },
      ]);
      return { result: "ok", value: undefined };
    }
    const added = this.store.addFieldOption(column.id, {
      name,
      color: input.color,
      expectedVersion: column.version ?? 1,
    });
    return added.result === "ok"
      ? { result: "ok", value: undefined }
      : { result: "invalid" };
  }

  updateOption(
    boardId: string,
    fieldId: string,
    optionId: string,
    patch: { name?: string; color?: string },
  ): SettingsWriteResult {
    const field = this.registry(boardId).fields.find((f) => f.id === fieldId);
    const column = field ? this.column(field) : undefined;
    if (!field || !column) return { result: "not-found" };
    const options = column.options ?? [];
    if (!options.some((option) => option.id === optionId))
      return { result: "not-found" };
    const name = patch.name?.trim();
    if (
      name !== undefined &&
      (name === "" ||
        options.some(
          (option) =>
            option.id !== optionId &&
            option.name.toLowerCase() === name.toLowerCase(),
        ))
    )
      return { result: "invalid", message: "name" };
    this.store.setColumnOptions(
      column.id,
      options.map((option) =>
        option.id === optionId
          ? {
              ...option,
              name: name ?? option.name,
              color: patch.color ?? option.color,
            }
          : option,
      ),
    );
    return { result: "ok", value: undefined };
  }

  deleteOption(
    boardId: string,
    fieldId: string,
    optionId: string,
  ): SettingsWriteResult {
    const field = this.registry(boardId).fields.find((f) => f.id === fieldId);
    const column = field ? this.column(field) : undefined;
    if (!field || !column) return { result: "not-found" };
    if (column.fieldType === "status") return { result: "invalid" };
    const options = column.options ?? [];
    if (options.length <= 1) return { result: "invalid" };
    if (!options.some((option) => option.id === optionId))
      return { result: "not-found" };
    this.store.setColumnOptions(
      column.id,
      options.filter((option) => option.id !== optionId),
    );
    if (field.defaultOptionId === optionId) field.defaultOptionId = null;
    return { result: "ok", value: undefined };
  }

  reorderOptions(
    boardId: string,
    fieldId: string,
    ids: readonly string[],
  ): SettingsWriteResult {
    const field = this.registry(boardId).fields.find((f) => f.id === fieldId);
    const column = field ? this.column(field) : undefined;
    if (!field || !column) return { result: "not-found" };
    const options = column.options ?? [];
    if (
      ids.length !== options.length ||
      !ids.every((id) => options.some((option) => option.id === id))
    )
      return { result: "invalid" };
    this.store.setColumnOptions(
      column.id,
      ids.map((id, index) => ({
        ...options.find((option) => option.id === id)!,
        position: `a${String(index).padStart(3, "0")}`,
      })),
    );
    return { result: "ok", value: undefined };
  }

  // ── Views ──

  updateView(
    boardId: string,
    viewId: string,
    patch: {
      name?: string;
      fieldId?: string | null;
      cardFieldIds?: string[];
      visibility?: "board" | "me";
    },
  ): SettingsWriteResult {
    const view = this.registry(boardId).views.find((v) => v.id === viewId);
    if (!view) return { result: "not-found" };
    const name = patch.name?.trim();
    if (name !== undefined && name === "") return { result: "invalid" };
    if (name !== undefined) view.name = name;
    if (patch.fieldId !== undefined) view.fieldId = patch.fieldId;
    if (patch.cardFieldIds !== undefined)
      view.cardFieldIds = patch.cardFieldIds;
    if (patch.visibility !== undefined) view.visibility = patch.visibility;
    view.lastEditedBy = this.store.getCurrentUser().name;
    view.summary = this.summaryOf(boardId, view);
    return { result: "ok", value: undefined };
  }

  private summaryOf(boardId: string, view: SettingsViewRecord): string {
    const field = this.registry(boardId).fields.find(
      (entry) => entry.id === view.fieldId,
    );
    if (view.kind === "kanban") {
      const rest = view.summary.split(" · ").slice(1).join(" · ");
      return `Chia cột theo ${field?.name ?? "Trạng thái"}${rest ? ` · ${rest}` : ""}`;
    }
    if (view.kind === "calendar" && field)
      return field.archived
        ? `Theo ${field.name} (đã lưu trữ)`
        : `Theo ${field.name}`;
    return view.summary;
  }

  createView(
    boardId: string,
    kind: SettingsViewRecord["kind"],
  ): SettingsViewRecord {
    const registry = this.registry(boardId);
    const view: SettingsViewRecord = {
      id: `sv-new-${registry.nextViewNumber}`,
      name: "Chế độ xem mới",
      kind,
      summary: "Chưa có bộ lọc",
      visibility: "me",
      isDefault: false,
      fieldId: null,
      filters: [],
      sort: null,
      cardFieldIds: [],
      cardFieldChoices: [],
      lastEditedBy: this.store.getCurrentUser().name,
      createdBy: this.store.getCurrentUser().name,
    };
    registry.nextViewNumber += 1;
    registry.views.push(view);
    return view;
  }

  duplicateView(
    boardId: string,
    viewId: string,
  ): SettingsWriteResult<SettingsViewRecord> {
    const registry = this.registry(boardId);
    const view = registry.views.find((v) => v.id === viewId);
    if (!view) return { result: "not-found" };
    const copy: SettingsViewRecord = {
      ...view,
      id: `sv-new-${registry.nextViewNumber}`,
      name: `${view.name} (bản sao)`,
      isDefault: false,
      visibility: "me",
      filters: view.filters.map((filter) => ({ ...filter })),
      cardFieldIds: [...view.cardFieldIds],
      createdBy: this.store.getCurrentUser().name,
      lastEditedBy: this.store.getCurrentUser().name,
    };
    registry.nextViewNumber += 1;
    registry.views.splice(registry.views.indexOf(view) + 1, 0, copy);
    return { result: "ok", value: copy };
  }

  deleteView(boardId: string, viewId: string): SettingsWriteResult {
    const registry = this.registry(boardId);
    const view = registry.views.find((v) => v.id === viewId);
    if (!view) return { result: "not-found" };
    if (view.isDefault) return { result: "invalid", message: "default" };
    registry.views = registry.views.filter((v) => v.id !== viewId);
    return { result: "ok", value: undefined };
  }

  reorderViews(boardId: string, ids: readonly string[]): SettingsWriteResult {
    const registry = this.registry(boardId);
    if (
      ids.length !== registry.views.length ||
      !ids.every((id) => registry.views.some((view) => view.id === id))
    )
      return { result: "invalid" };
    registry.views = ids.map((id) => registry.views.find((v) => v.id === id)!);
    return { result: "ok", value: undefined };
  }

  // ── Automations ──

  setAutomationEnabled(
    boardId: string,
    id: string,
    enabled: boolean,
  ): SettingsWriteResult {
    const rule = this.registry(boardId).automations.find((a) => a.id === id);
    if (!rule) return { result: "not-found" };
    if (enabled && rule.pausedReason)
      return { result: "invalid", message: "paused" };
    rule.enabled = enabled;
    return { result: "ok", value: undefined };
  }

  // ── General ──

  updateGeneral(
    boardId: string,
    patch: {
      title?: string;
      description?: string;
      color?: string;
      itemKeyPrefix?: string;
      timeZone?: string;
      weekStart?: "monday" | "sunday";
    },
  ): SettingsWriteResult {
    const board = this.store.getBoard(boardId);
    if (!board) return { result: "not-found" };
    const title = patch.title?.trim();
    if (title !== undefined && title === "") return { result: "invalid" };
    const prefix = patch.itemKeyPrefix?.trim().toUpperCase();
    if (prefix !== undefined && !/^[A-Z]{1,6}$/.test(prefix))
      return { result: "invalid", message: "prefix" };
    this.store.patchBoard(boardId, {
      ...(title !== undefined ? { title } : {}),
      ...(patch.description !== undefined
        ? { description: patch.description }
        : {}),
      ...(patch.color !== undefined
        ? { background: { type: "color" as const, value: patch.color } }
        : {}),
      ...(prefix !== undefined ? { itemKeyPrefix: prefix } : {}),
    });
    const general = this.registry(boardId).general;
    if (patch.timeZone !== undefined) general.timeZone = patch.timeZone;
    if (patch.weekStart !== undefined) general.weekStart = patch.weekStart;
    return { result: "ok", value: undefined };
  }
}

// ── The ERP board's seed ──

const U = (
  kind: SettingsUsage["kind"],
  name: string,
  how: string,
): SettingsUsage => ({ kind, name, how });

export const ERP_SETTINGS_SYSTEM_FIELDS: readonly SettingsSystemField[] = [
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
];

/** The registry of the ERP board (Final-Settings.dc.html). */
export function erpSettingsRegistry(columns: {
  status: string;
  priority: string;
  owner: string;
  module: string;
  estimate: string;
  requester: string;
}): BoardSettingsRegistry {
  const fields: SettingsFieldRecord[] = [
    {
      id: "sf-status",
      key: "status",
      type: "status",
      name: "Trạng thái",
      columnId: columns.status,
      detail: "4 lựa chọn",
      usage: "Chia cột Kanban",
      accent: true,
      usedIn: [U("view", "Kanban", "chia cột"), U("view", "Bảng", "nhóm theo")],
      defaultOptionId: "mock-erp-status-todo",
      required: false,
      guidance: "",
    },
    {
      id: "sf-owner",
      key: "assignee",
      type: "person",
      name: "Người thực hiện",
      columnId: columns.owner,
      detail: "nhiều người",
      usage: "5 view",
      usedIn: [
        U("view", "Bảng", "cột hiển thị"),
        U("card", "Kanban", "hiện trên thẻ"),
        U("view", "Dòng thời gian", "nhãn thanh"),
      ],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-start",
      key: "start_date",
      type: "date",
      name: "Ngày bắt đầu",
      detail: "",
      usage: "2 view",
      usedIn: [
        U("view", "Lịch", "ngày bắt đầu"),
        U("view", "Dòng thời gian", "đầu thanh"),
      ],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-due",
      key: "due_date",
      type: "date",
      name: "Hạn chót",
      detail: "có giờ, GMT+7",
      usage: "6 view",
      usedIn: [
        U("view", "Bảng", "cột hiển thị"),
        U("card", "Kanban", "hiện trên thẻ"),
        U("view", "Lịch", "ngày kết thúc"),
        U("view", "Dòng thời gian", "cuối thanh"),
      ],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-priority",
      key: "priority",
      type: "select",
      name: "Ưu tiên",
      columnId: columns.priority,
      detail: "4 lựa chọn",
      usage: "3 view · 1 quy tắc",
      usedIn: [
        U("view", "Bảng", "cột hiển thị"),
        U("card", "Kanban", "hiện trên thẻ"),
        U("filter", "Việc khẩn của tôi", "bộ lọc"),
        U("rule", "Khẩn → báo Trưởng phòng", "quy tắc tự động"),
      ],
      defaultOptionId: "mock-erp-priority-medium",
      required: false,
      guidance: "Chỉ chọn Khẩn khi việc chặn ngày go-live 01/11.",
    },
    {
      id: "sf-module",
      key: "erp_module",
      type: "multiselect",
      name: "Phân hệ ERP",
      columnId: columns.module,
      detail: "6 lựa chọn",
      usage: "2 view",
      usedIn: [
        U("view", "Bảng", "cột hiển thị"),
        U("filter", "Kanban", "bộ lọc"),
      ],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-labels",
      key: "labels",
      type: "multiselect",
      name: "Nhãn",
      detail: "Dữ liệu, Bảo mật, Đào tạo, Báo cáo…",
      usage: "Kanban, Bảng",
      usedIn: [
        U("card", "Kanban", "hiện trên thẻ"),
        U("view", "Bảng", "cột hiển thị"),
      ],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-estimate",
      key: "estimate_hours",
      type: "number",
      name: "Ước lượng",
      columnId: columns.estimate,
      detail: "1 chữ số thập phân",
      usage: "1 view",
      unit: "giờ",
      usedIn: [U("view", "Bảng", "cột hiển thị")],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-progress",
      key: "progress",
      type: "progress",
      name: "Tiến độ",
      detail: "tự tính từ việc con",
      usage: "2 view",
      usedIn: [
        U("view", "Bảng", "cột hiển thị"),
        U("card", "Kanban", "hiện trên thẻ"),
      ],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-depends",
      key: "depends_on",
      type: "relation",
      name: "Phụ thuộc",
      detail: "Đang chờ, Đang chặn, Liên quan",
      usage: "Dòng thời gian, Chi tiết",
      usedIn: [
        U("view", "Dòng thời gian", "đường phụ thuộc"),
        U("view", "Chi tiết việc", "phần liên kết"),
      ],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-requester",
      key: "requester_dept",
      type: "select",
      name: "Phòng ban yêu cầu",
      columnId: columns.requester,
      detail: "5 lựa chọn",
      usage: "Chưa dùng",
      usedIn: [],
      defaultOptionId: null,
      required: false,
      guidance: "",
    },
    {
      id: "sf-handover",
      key: "handover_date",
      type: "date",
      name: "Ngày bàn giao",
      detail: "",
      usage: "Chưa dùng",
      usedIn: [U("view", "Lịch bàn giao", "ngày hiển thị")],
      defaultOptionId: null,
      required: false,
      guidance: "",
      archived: {
        at: "2026-10-02T04:00:00.000Z",
        byName: "Trần Minh Quân",
        values: 9,
      },
    },
  ];
  const view = (
    id: string,
    name: string,
    kind: SettingsViewRecord["kind"],
    summary: string,
    visibility: "board" | "me",
    extra: Partial<SettingsViewRecord> = {},
  ): SettingsViewRecord => ({
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
    ...extra,
  });
  return {
    fields,
    views: [
      view(
        "sv-table",
        "Bảng",
        "table",
        "Nhóm theo Trạng thái · 9/12 cột · Hạn ↑",
        "board",
        { isDefault: true, fieldId: "sf-status" },
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
        { fieldId: "sf-start" },
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
          fieldId: "sf-priority",
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
    ],
    automations: [
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
    ],
    general: {
      icon: "board",
      sampleItemKey: "ERP-142",
      timeZone: "Asia/Ho_Chi_Minh",
      weekStart: "monday",
    },
    nextViewNumber: 1,
    version: 1,
  };
}

// ── Groups (the board's lists) ──

export interface SettingsGroupRecord {
  id: string;
  name: string;
  color: string;
  itemCount: number;
}

export function groupsOf(
  store: MockStore,
  boardId: string,
): SettingsGroupRecord[] {
  return store.getLists(boardId).map((list) => ({
    id: list.id,
    name: list.title,
    color: list.color ?? "#1E6FD0",
    itemCount: store
      .getCards(list.id)
      .filter((card) => !card.parentItemId && !card.isArchived).length,
  }));
}

/** Deletes a group; its items move to `moveToGroupId` first (never deleted). */
export function deleteGroup(
  store: MockStore,
  boardId: string,
  groupId: string,
  moveToGroupId: string | null,
): SettingsWriteResult {
  const lists = store.getLists(boardId);
  const list = lists.find((entry) => entry.id === groupId);
  if (!list) return { result: "not-found" };
  if (lists.length <= 1) return { result: "invalid", message: "last" };
  const cards = store.getCards(groupId);
  if (cards.length > 0) {
    const target = lists.find((entry) => entry.id === moveToGroupId);
    if (!target || target.id === groupId)
      return { result: "invalid", message: "target" };
    const end = Math.max(
      0,
      ...store.getCards(target.id).map((card) => card.position),
    );
    cards.forEach((card, index) =>
      store.moveCard(card.id, target.id, end + index + 1),
    );
  }
  store.deleteList(groupId);
  return { result: "ok", value: undefined };
}
