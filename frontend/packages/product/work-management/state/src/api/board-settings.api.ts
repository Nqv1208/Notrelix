import type {
  NotrelixClient,
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  BoardSettingsDto,
  FormulaPreviewDraft,
  FormulaPreviewDto,
  BoardSettingsComputedDto,
  BoardSettingsFieldDto,
  BoardSettingsFieldPatch,
  BoardSettingsFieldType,
  BoardSettingsGeneralPatch,
  BoardSettingsGroupDto,
  BoardSettingsUsageDto,
  BoardSettingsSystemFieldDto,
  BoardSettingsUnavailable,
  BoardSettingsViewDto,
  BoardSettingsViewPatch,
  SavedBoardView,
  ViewConfig,
  ViewMode,
} from "@notrelix/work-management-core";
import { defaultTableViewConfig, createBoardApi } from "./board.api";

type Overview = OperationResponse<"WorkManagement.Boards.GetOverview">;
export type BoardLifecycleImpact = OperationResponse<
  "WorkManagement.Boards.GetLifecycleImpact",
  200
>;
export type RetiredBoard = OperationResponse<
  "WorkManagement.Boards.ListArchived",
  200
>[number];
export type DeletedBoardField = OperationResponse<
  "WorkManagement.BoardFields.Deleted",
  200
>[number];
/** The answer to a restore: `alreadyActive` when someone restored the field first, with their display name. */
export type RestoredBoardField = OperationResponse<
  "WorkManagement.BoardFields.Restore",
  200
>;
type OverviewField = NonNullable<Overview["columns"]>[number];
type OverviewGroup = NonNullable<Overview["lists"]>[number];

/** The field types the board field routes accept (the domain's `FieldType`). */
const CREATABLE_FIELD_TYPES: readonly BoardSettingsFieldType[] = [
  "text",
  "longtext",
  "number",
  "date",
  "checkbox",
  "select",
  "multiselect",
  "status",
  "person",
  "link",
  "formula",
  "rollup",
];

/** What the product has not defined yet: the page hides or locks these. */
const UNAVAILABLE: BoardSettingsUnavailable = {
  locale: true,
  fieldArchive: true,
  fieldDelete: false,
  views: false,
  viewSplit: true,
  automations: true,
  creatableFieldTypes: CREATABLE_FIELD_TYPES,
};

const DEFAULT_COLOR = "#1E6FD0";

/**
 * A new board stores its background as `{"type":"color","value":"#..."}`; the General page saves the
 * bare hex. Either reads back as the colour, anything else as the default.
 */
export function boardColor(background: string | null | undefined): string {
  const text = (background ?? "").trim();
  if (/^#[0-9a-f]{6}$/i.test(text)) return text;
  try {
    const value: unknown = (JSON.parse(text) as { value?: unknown }).value;
    if (typeof value === "string" && /^#[0-9a-f]{6}$/i.test(value))
      return value;
  } catch {
    // Not JSON: the default applies.
  }
  return DEFAULT_COLOR;
}

/** What a board inherits from its workspace (`Workspaces.Settings.GetWorkspaceSettings`). */
interface WorkspaceLocale {
  readonly timeZone: string;
  readonly weekStart: "monday" | "sunday";
  /** True when the setting could not be read and the default stands in. */
  readonly isDefault?: boolean;
}

const DEFAULT_LOCALE: WorkspaceLocale = {
  timeZone: "Asia/Ho_Chi_Minh",
  weekStart: "monday",
  isDefault: true,
};

function fieldType(raw: string | null | undefined): BoardSettingsFieldType {
  const name = (raw ?? "").trim().toLowerCase();
  return (CREATABLE_FIELD_TYPES as readonly string[]).includes(name)
    ? (name as BoardSettingsFieldType)
    : "text";
}

function unitOf(settings: string | null | undefined): string | null {
  if (!settings) return null;
  try {
    const parsed: unknown = JSON.parse(settings);
    if (typeof parsed === "object" && parsed !== null && "unit" in parsed) {
      const unit = (parsed as { unit?: unknown }).unit;
      return typeof unit === "string" && unit !== "" ? unit : null;
    }
  } catch {
    return null;
  }
  return null;
}

function multipleOf(settings: string | null | undefined): boolean {
  if (!settings) return false;
  try {
    const parsed: unknown = JSON.parse(settings);
    return (
      typeof parsed === "object" &&
      parsed !== null &&
      (parsed as { multiple?: unknown }).multiple === true
    );
  } catch {
    return false;
  }
}

/** The rules a board owner sets on a field live in its settings JSON: `required` (bool) and `guidance` (text). */
function rulesOf(settings: string | null | undefined): {
  required: boolean;
  guidance: string;
} {
  try {
    const parsed = JSON.parse(settings || "{}") as {
      required?: unknown;
      guidance?: unknown;
    };
    return {
      required: parsed.required === true,
      guidance: typeof parsed.guidance === "string" ? parsed.guidance : "",
    };
  } catch {
    return { required: false, guidance: "" };
  }
}

/** The option a select-like field starts with: its `defaultValue` is an option id, or a list of one. */
function defaultOptionOf(value: string | null | undefined): string | null {
  try {
    const parsed: unknown = JSON.parse(value || "null");
    const id = Array.isArray(parsed) ? parsed[0] : parsed;
    return typeof id === "string" ? id : null;
  } catch {
    return null;
  }
}

/** The text or number a Text, Long text or Number field starts with, as typed. */
function defaultTextOf(value: string | null | undefined): string {
  try {
    const parsed: unknown = JSON.parse(value || "null");
    return typeof parsed === "string" || typeof parsed === "number"
      ? String(parsed)
      : "";
  } catch {
    return "";
  }
}

/** The settings JSON with the edited rules merged in; every other setting stays as it is. */
export function settingsWithRules(
  base: string | undefined,
  changes: BoardSettingsFieldPatch,
): string | undefined {
  if (changes.required === undefined && changes.guidance === undefined)
    return changes.settings;
  const merged: Record<string, unknown> = JSON.parse(
    changes.settings ?? (base || "{}"),
  ) as Record<string, unknown>;
  if (changes.required !== undefined) {
    if (changes.required) merged.required = true;
    else delete merged.required;
  }
  if (changes.guidance !== undefined) {
    if (changes.guidance.trim() !== "") merged.guidance = changes.guidance;
    else delete merged.guidance;
  }
  return JSON.stringify(merged);
}

const ordinal = (left: string, right: string) =>
  left < right ? -1 : left > right ? 1 : 0;

/** Rows in their ordering-key order, compared by code unit as the server's ordinal collation does ("Z" before "a"). */
function byPosition<T extends { position?: string | null }>(
  rows: readonly T[],
): T[] {
  return [...rows].sort((a, b) => ordinal(a.position ?? "", b.position ?? ""));
}

/**
 * The board's groups in key order; equal keys (never two live groups on the server, which holds the
 * key unique) fall back to the id, so every reader of the same groups sees one order whatever order
 * the response listed them in (UC-WM-F02).
 */
function groupsByPosition<
  T extends { position?: string | null; id?: string | null },
>(rows: readonly T[]): T[] {
  return [...rows].sort(
    (a, b) =>
      ordinal(a.position ?? "", b.position ?? "") ||
      ordinal(a.id ?? "", b.id ?? ""),
  );
}

/** A system field the page lists as locked, except Status, whose options are the board's to edit. */
function isLockedSystemField(column: OverviewField): boolean {
  return column.isSystem === true && fieldType(column.fieldType) !== "status";
}

/** The key a field is listed under: its name as a lowercase slug ("due-date"), or a short id when the name has no letters. */
export function fieldKeyOf(
  name: string | null | undefined,
  id?: string,
): string {
  const slug = (name ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  return slug || (id ?? "").slice(0, 8);
}

function fieldKey(column: OverviewField): string {
  return fieldKeyOf(column.name, column.id);
}

function mapField(column: OverviewField): BoardSettingsFieldDto {
  const type = fieldType(column.fieldType);
  const options = byPosition(column.options ?? []).map((option) => ({
    id: option.id ?? "",
    name: option.name ?? "",
    color: option.color ?? DEFAULT_COLOR,
    itemCount: option.itemCount ?? 0,
  }));
  return {
    id: column.id ?? "",
    version: column.version ?? 0,
    key: fieldKey(column),
    type,
    name: column.name ?? "",
    detail: "",
    usage: "",
    accent: type === "status",
    unit: unitOf(column.settings),
    ...(type === "person" && multipleOf(column.settings)
      ? { multiple: true }
      : {}),
    hasColumn: type === "status" || type === "select" || type === "multiselect",
    valueCount: column.valueCount ?? 0,
    options,
    defaultOptionId: defaultOptionOf(column.defaultValue),
    defaultText:
      type === "text" || type === "longtext" || type === "number"
        ? defaultTextOf(column.defaultValue)
        : "",
    ...rulesOf(column.settings),
    settingsJson: column.settings ?? "{}",
    usedIn: [],
    ...computedOf(type, column.settings),
  };
}

/** The definition of a Formula or Rollup from its settings JSON. */
function computedOf(
  type: BoardSettingsFieldType,
  settings: string | null | undefined,
): { computed: BoardSettingsComputedDto } | Record<string, never> {
  if ((type !== "formula" && type !== "rollup") || !settings) return {};
  try {
    const parsed = JSON.parse(settings) as Record<string, unknown>;
    return type === "formula"
      ? {
          computed: {
            ...(typeof parsed.expression === "string"
              ? { expression: parsed.expression }
              : {}),
          },
        }
      : {
          computed: {
            ...(typeof parsed.function === "string"
              ? {
                  rollupFunction:
                    parsed.function as BoardSettingsComputedDto["rollupFunction"],
                }
              : {}),
            ...(typeof parsed.fieldId === "string"
              ? { sourceFieldId: parsed.fieldId }
              : {}),
          },
        };
  } catch {
    return {};
  }
}

function mapSystemField(column: OverviewField): BoardSettingsSystemFieldDto {
  return {
    id: column.id ?? "",
    key: fieldKey(column),
    name: column.name ?? "",
    type: fieldType(column.fieldType),
    detail: "",
    lock: "locked",
  };
}

function mapGroup(group: OverviewGroup): BoardSettingsGroupDto {
  return {
    id: group.id ?? "",
    version: group.version ?? 0,
    name: group.title ?? "",
    color: group.color ?? DEFAULT_COLOR,
    // An archived group lists no items; the read carries its size.
    itemCount: group.itemCount ?? (group.boardItems ?? []).length,
  };
}

/** A saved view as the Views section lists it. */
export function mapSettingsView(
  view: SavedBoardView,
  fields: readonly BoardSettingsFieldDto[],
): BoardSettingsViewDto {
  const fieldName = (id: string) =>
    fields.find((field) => field.id === id)?.name ?? id;
  const config = view.viewConfig;
  const sort = config.sortBy[0];
  return {
    id: view.id,
    version: view.version,
    name: view.name,
    kind: view.viewMode,
    summary: [
      config.filters.length > 0
        ? config.filters.map((filter) => fieldName(filter.fieldId)).join(", ")
        : "",
      sort ? fieldName(sort.fieldId) : "",
    ]
      .filter((part) => part !== "")
      .join(" · "),
    visibility: view.visibility,
    isDefault: view.isDefault,
    fieldId: null,
    filters: config.filters.map((filter) => ({
      fieldName: fieldName(filter.fieldId),
      operator: filter.operator,
      values: Array.isArray(filter.value)
        ? filter.value.join(", ")
        : String(filter.value ?? ""),
    })),
    sort: sort
      ? { fieldName: fieldName(sort.fieldId), text: sort.direction }
      : null,
    cardFieldIds: config.cardFieldIds ?? [],
    cardFieldChoices: [],
    lastEditedBy: "",
    createdBy: "",
    needsFix: false,
  };
}

/**
 * Where a field is in use: the saved views that filter, sort or split by it, show it on a card, or (a table)
 * show it as a column. Read from the views the page already has, so it needs no extra request; `how` is left
 * for the page to word (`how` is a code: column, sort, group, card or filter).
 */
export function usedInOf(
  fieldId: string,
  views: readonly SavedBoardView[],
): BoardSettingsUsageDto[] {
  return views.flatMap((view): BoardSettingsUsageDto[] => {
    const config = view.viewConfig;
    const sorted = config.sortBy.some((sort) => sort.fieldId === fieldId);
    const how = config.filters.some((filter) => filter.fieldId === fieldId)
      ? "filter"
      : (config.cardFieldIds ?? []).includes(fieldId)
        ? "card"
        : sorted
          ? "sort"
          : config.groupBy === fieldId
            ? "group"
            : view.viewMode === "table" &&
                !(config.hiddenFields ?? []).includes(fieldId)
              ? "column"
              : null;
    const kind: BoardSettingsUsageDto["kind"] | null =
      how === "filter" || how === "card" ? how : how ? "view" : null;
    return kind && how ? [{ kind, name: view.name, how }] : [];
  });
}

/** The first item the board read carries: the one a draft formula is tried on. */
function previewItemOf(
  board: Overview,
): { previewItem: { id: string; key: string } } | Record<string, never> {
  for (const group of board.lists ?? []) {
    const item = (group.boardItems ?? []).find((entry) => entry.id);
    if (item?.id) {
      return { previewItem: { id: item.id, key: item.itemKey ?? "" } };
    }
  }
  return {};
}

/** The settings page model of a board read. */
export function mapBoardSettings(
  boardId: string,
  board: Overview,
  inherited: WorkspaceLocale = DEFAULT_LOCALE,
  savedViews: readonly SavedBoardView[] = [],
  archivedViews: readonly SavedBoardView[] = [],
): BoardSettingsDto {
  const columns = byPosition(
    (board.columns ?? []).filter((column) => column.isDeleted !== true),
  );
  const groups = groupsByPosition(
    (board.lists ?? []).filter((group) => group.isArchived !== true),
  ).map(mapGroup);
  const archivedGroups = groupsByPosition(
    (board.lists ?? []).filter((group) => group.isArchived === true),
  ).map(mapGroup);
  const fields = columns
    .filter((column) => !isLockedSystemField(column))
    .map((column) => ({
      ...mapField(column),
      usedIn: usedInOf(column.id ?? "", savedViews),
    }));
  return {
    boardId,
    version: board.version ?? 0,
    itemCount: groups.reduce((sum, group) => sum + group.itemCount, 0),
    ...previewItemOf(board),
    general: {
      title: board.title ?? "",
      description: board.description ?? "",
      color: boardColor(board.background),
      icon: "",
      sampleItemKey: null,
      itemKeyPrefix: board.itemKeyPrefix ?? "",
      department: null,
      // Time zone and first day of the week are the workspace's; the board inherits them.
      timeZone: inherited.timeZone,
      weekStart: inherited.weekStart,
      isArchived: board.isArchived === true,
    },
    fields,
    fieldOrder: columns.map((column) => column.id ?? ""),
    systemFields: columns.filter(isLockedSystemField).map(mapSystemField),
    archivedFields: [],
    views: savedViews.map((view) => mapSettingsView(view, fields)),
    archivedViews: archivedViews.map((view) => mapSettingsView(view, fields)),
    groups,
    archivedGroups,
    automations: [],
    unavailable: UNAVAILABLE,
  };
}

/** The id that must come before the moved one in `order`, and the one after it. */
function neighbours(order: readonly string[], movedId: string) {
  const index = order.indexOf(movedId);
  return {
    previousSiblingId: index > 0 ? order[index - 1]! : null,
    nextSiblingId:
      index >= 0 && index < order.length - 1 ? order[index + 1]! : null,
  };
}

/** The one id whose place differs between two orders (a drag moves one). */
function movedId(
  current: readonly string[],
  next: readonly string[],
): string | null {
  const stable = current.filter((id) => next.includes(id));
  const wanted = next.filter((id) => stable.includes(id));
  for (let index = 0; index < wanted.length; index += 1) {
    if (wanted[index] === stable[index]) continue;
    // The id at this place in `wanted` was moved here, or the one at `stable`
    // was moved away; the one whose removal makes the orders equal is it.
    const candidates = [wanted[index]!, stable[index]!];
    for (const candidate of candidates) {
      const left = stable.filter((id) => id !== candidate);
      const right = wanted.filter((id) => id !== candidate);
      if (left.every((id, at) => id === right[at])) return candidate;
    }
    return null;
  }
  return null;
}

function viewOf(s: BoardSettingsDto, viewId: string) {
  const view = s.views.find((entry) => entry.id === viewId);
  if (!view) throw new Error(`Unknown view ${viewId}`);
  return view;
}

/** The version of the view the page read; every write on a view names it. */
function viewVersion(s: BoardSettingsDto, viewId: string): number {
  const version = viewOf(s, viewId).version;
  if (!version) throw new Error(`The version of view ${viewId} is unknown`);
  return version;
}

export function createBoardSettingsApi(client: NotrelixClient) {
  const { api } = client;
  const boardApi = createBoardApi(client);
  const quiet = { skipGlobalErrorToast: true } as const;
  const find = <T extends { id: string; version?: number }>(
    rows: readonly T[],
    id: string,
  ) => {
    const row = rows.find((entry) => entry.id === id);
    if (!row) throw new Error(`Unknown id ${id}`);
    return row;
  };
  const fieldOf = (s: BoardSettingsDto, fieldId: string) =>
    find([...s.fields], fieldId);
  const optionVersion = (s: BoardSettingsDto, fieldId: string) =>
    fieldOf(s, fieldId).version ?? 0;

  const workspaceLocale = (workspaceId: string): Promise<WorkspaceLocale> =>
    api
      .get<{ timeZone?: string | null; weekStart?: string | null }>(
        endpoints.workspaces.settings(workspaceId),
        quiet,
      )
      .then((workspace): WorkspaceLocale => ({
        timeZone: workspace.timeZone || DEFAULT_LOCALE.timeZone,
        weekStart: workspace.weekStart === "sunday" ? "sunday" : "monday",
        isDefault: !workspace.timeZone,
      }))
      .catch(() => DEFAULT_LOCALE);

  return {
    /** The workspace's locale a board inherits (its time zone judges the days). */
    workspaceLocale,
    async get(boardId: string): Promise<BoardSettingsDto> {
      const board = await api.get<Overview>(
        endpoints.boards.full(boardId),
        quiet,
      );
      // The workspace's locale is context, not the page's own data: without it the defaults show.
      const inherited = board.workspaceId
        ? await workspaceLocale(board.workspaceId)
        : DEFAULT_LOCALE;
      const savedViews = await boardApi.listSavedViews(boardId).catch(() => []);
      const archivedViews = await boardApi
        .listSavedViews(boardId, true)
        .catch(() => []);
      // Only a manager reads the impact; the settings page is a manager's page, and without it the
      // confirmations name the effect without counts.
      const impact = await api
        .get<BoardLifecycleImpact>(
          endpoints.boards.lifecycleImpact(boardId),
          quiet,
        )
        .then((counts) => ({
          itemCount: counts.itemCount ?? 0,
          openItemCount: counts.openItemCount ?? 0,
          memberCount: counts.memberCount ?? 0,
          viewCount: counts.viewCount ?? 0,
        }))
        .catch(() => undefined);
      return {
        ...mapBoardSettings(
          boardId,
          board,
          inherited,
          savedViews,
          archivedViews,
        ),
        ...(impact ? { impact } : {}),
      };
    },

    updateGeneral: (s: BoardSettingsDto, changes: BoardSettingsGeneralPatch) =>
      api.patch<void>(
        endpoints.boards.detail(s.boardId),
        {
          expectedVersion: s.version,
          title: changes.title,
          description: changes.description,
          background: changes.color,
          itemKeyPrefix: changes.itemKeyPrefix,
        },
        quiet,
      ),
    /** `POST /boards/{id}/archive` with the board version the person saw: a stale one changes nothing. */
    archiveBoard: (s: BoardSettingsDto) => {
      const body: OperationRequestBody<"WorkManagement.Boards.Archive"> = {
        expectedVersion: s.version,
      };
      return api.post<void>(endpoints.boards.archive(s.boardId), body, {
        ...quiet,
        idempotencyKey: crypto.randomUUID(),
      });
    },
    unarchiveBoard: (s: BoardSettingsDto) => {
      const body: OperationRequestBody<"WorkManagement.Boards.Unarchive"> = {
        expectedVersion: s.version,
      };
      return api.post<void>(endpoints.boards.unarchive(s.boardId), body, {
        ...quiet,
        idempotencyKey: crypto.randomUUID(),
      });
    },
    /** What archiving or deleting the board touches, for the confirmation that names it. */
    lifecycleImpact: (boardId: string) =>
      api.get<BoardLifecycleImpact>(
        endpoints.boards.lifecycleImpact(boardId),
        quiet,
      ),
    /** The archived boards the person can see; they read, and a Manager unarchives them. */
    listArchivedBoards: (workspaceId: string) =>
      api.get<RetiredBoard[]>(
        endpoints.boards.archivedByWorkspaceId(workspaceId),
        quiet,
      ),
    /** The boards deleted in the last 30 days that the person can restore. */
    listDeletedBoards: (workspaceId: string) =>
      api.get<RetiredBoard[]>(
        endpoints.boards.deletedByWorkspaceId(workspaceId),
        quiet,
      ),
    unarchiveBoardById: (boardId: string, expectedVersion: number) => {
      const body: OperationRequestBody<"WorkManagement.Boards.Unarchive"> = {
        expectedVersion,
      };
      return api.post<void>(endpoints.boards.unarchive(boardId), body, {
        ...quiet,
        idempotencyKey: crypto.randomUUID(),
      });
    },
    /** Refused after the 30-day window with the rule code `WorkManagement_Board_RestoreWindowElapsed`. */
    restoreBoard: (boardId: string, expectedVersion: number) => {
      const body: OperationRequestBody<"WorkManagement.Boards.Restore"> = {
        expectedVersion,
      };
      return api.post<void>(endpoints.boards.restore(boardId), body, {
        ...quiet,
        idempotencyKey: crypto.randomUUID(),
      });
    },
    /** `DELETE /boards/{id}?expectedVersion=n`: soft delete, restorable by a Manager for 30 days. */
    deleteBoard: (s: BoardSettingsDto) =>
      api.delete<void>(
        `${endpoints.boards.detail(s.boardId)}?expectedVersion=${s.version}`,
        { ...quiet, idempotencyKey: crypto.randomUUID() },
      ),

    createField: (
      s: BoardSettingsDto,
      type: BoardSettingsFieldType,
      name: string,
      settings?: string,
    ) =>
      api.post<unknown>(
        endpoints.boardFields.create(s.boardId),
        { name, type, ...(settings ? { settingsJson: settings } : {}) },
        quiet,
      ),
    /** The fields deleted in the last 30 days, newest first (a Manager reads it). */
    listDeletedFields: (boardId: string) =>
      api.get<DeletedBoardField[]>(
        endpoints.boardFields.deleted(boardId),
        quiet,
      ),
    /** Brings a deleted field and its values back; refused after 30 days or when its name is taken. */
    restoreDeletedField: (boardId: string, fieldId: string) =>
      api.post<RestoredBoardField>(
        endpoints.boardFields.restore(boardId, fieldId),
        {},
        {
          ...quiet,
          idempotencyKey: crypto.randomUUID(),
        },
      ),
    // `DELETE /boards/{id}/fields/{fieldId}?expectedVersion=n`: a soft delete a Manager can restore for 30 days.
    deleteField: (s: BoardSettingsDto, fieldId: string) =>
      api.delete<void>(
        `${endpoints.boardFields.detail(s.boardId, fieldId)}?expectedVersion=${fieldOf(s, fieldId).version ?? 0}`,
        quiet,
      ),
    reorderFields: async (s: BoardSettingsDto, ids: readonly string[]) => {
      const moved = movedId(
        s.fields.map((field) => field.id),
        ids,
      );
      if (!moved) return;
      // The page lists the board's own fields only; the server orders all of
      // them, locked system fields included, and the neighbours named must be
      // adjacent in that order: the listed neighbour the field lands next to,
      // and what is on the other side of it on the server.
      const server = (s.fieldOrder ?? s.fields.map((field) => field.id)).filter(
        (id) => id !== moved,
      );
      const at = ids.indexOf(moved);
      const side =
        at < ids.length - 1
          ? { listed: ids[at + 1]!, before: true }
          : { listed: ids[at - 1]!, before: false };
      const place = server.indexOf(side.listed);
      const around = side.before
        ? {
            previousSiblingId: server[place - 1] ?? null,
            nextSiblingId: side.listed,
          }
        : {
            previousSiblingId: side.listed,
            nextSiblingId: server[place + 1] ?? null,
          };
      await api.post<void>(
        endpoints.boardFields.move(s.boardId, moved),
        {
          expectedVersion: fieldOf(s, moved).version ?? 0,
          ...around,
        },
        quiet,
      );
    },
    updateField: (
      s: BoardSettingsDto,
      fieldId: string,
      changes: BoardSettingsFieldPatch,
    ) => {
      const field = fieldOf(s, fieldId);
      const settings = settingsWithRules(field.settingsJson, changes);
      return api.patch<void>(
        endpoints.boardFields.detail(s.boardId, fieldId),
        {
          expectedVersion: field.version ?? 0,
          name: changes.name,
          ...(settings ? { settingsJson: settings } : {}),
          ...(changes.defaultOptionId !== undefined
            ? {
                defaultValueJson:
                  changes.defaultOptionId === null
                    ? "null"
                    : JSON.stringify(
                        field.type === "multiselect"
                          ? [changes.defaultOptionId]
                          : changes.defaultOptionId,
                      ),
              }
            : {}),
          ...(changes.defaultText !== undefined
            ? {
                defaultValueJson:
                  changes.defaultText.trim() === ""
                    ? "null"
                    : field.type === "number"
                      ? changes.defaultText.trim()
                      : JSON.stringify(changes.defaultText),
              }
            : {}),
        },
        quiet,
      );
    },
    /** What a draft formula shows on one item; nothing is saved. */
    previewFormula: async (
      boardId: string,
      itemId: string,
      draft: FormulaPreviewDraft,
    ): Promise<FormulaPreviewDto> => {
      const text = (value: unknown) =>
        value !== null && value !== undefined ? String(value) : null;
      const result = await api.get<{
        state?: string;
        value?: unknown;
        errorCode?: string | null;
        inputs?: {
          name: string;
          value?: { state?: string; value?: unknown };
        }[];
        subItemCount?: number | null;
      }>(endpoints.boardFields.previewFormula(boardId, itemId, draft), quiet);
      const state =
        result.state === "ok" || result.state === "empty"
          ? result.state
          : "error";
      return {
        state,
        value: state === "ok" ? text(result.value) : null,
        errorCode: result.errorCode ?? null,
        inputs: (result.inputs ?? []).map((input) => ({
          name: input.name,
          value: input.value?.state === "ok" ? text(input.value.value) : null,
        })),
        subItemCount: result.subItemCount ?? null,
      };
    },
    addOption: (
      s: BoardSettingsDto,
      fieldId: string,
      option: { name: string; color: string },
    ) =>
      api.post<unknown>(
        endpoints.boardFields.options(s.boardId, fieldId),
        { ...option, expectedVersion: optionVersion(s, fieldId) },
        quiet,
      ),
    updateOption: (
      s: BoardSettingsDto,
      fieldId: string,
      optionId: string,
      changes: { name?: string; color?: string },
    ) => {
      // The producer replaces name and colour together: send the one not edited too.
      const current = fieldOf(s, fieldId).options.find(
        (option) => option.id === optionId,
      );
      return api.patch<void>(
        endpoints.boardFields.option(s.boardId, fieldId, optionId),
        {
          name: current?.name,
          color: current?.color,
          ...changes,
          expectedVersion: optionVersion(s, fieldId),
        },
        quiet,
      );
    },
    deleteOption: (s: BoardSettingsDto, fieldId: string, optionId: string) =>
      api.delete<void>(
        `${endpoints.boardFields.option(s.boardId, fieldId, optionId)}?expectedVersion=${optionVersion(s, fieldId)}`,
        quiet,
      ),
    reorderOptions: async (
      s: BoardSettingsDto,
      fieldId: string,
      ids: readonly string[],
    ) => {
      const moved = movedId(
        fieldOf(s, fieldId).options.map((option) => option.id),
        ids,
      );
      if (!moved) return;
      await api.post<void>(
        `${endpoints.boardFields.option(s.boardId, fieldId, moved)}/move`,
        {
          expectedVersion: optionVersion(s, fieldId),
          ...neighbours(ids, moved),
        },
        quiet,
      );
    },

    createView: (
      s: BoardSettingsDto,
      kind: ViewMode,
      name: string,
      config: ViewConfig = defaultTableViewConfig,
      visibility: "board" | "me" = "board",
    ) =>
      boardApi.createSavedView(s.boardId, {
        name,
        viewMode: kind,
        viewConfig: config,
        visibility,
      }),
    updateView: async (
      s: BoardSettingsDto,
      viewId: string,
      changes: BoardSettingsViewPatch,
    ) => {
      // A write bumps the view's version: the second one names the next.
      let version = viewVersion(s, viewId);
      if (changes.name !== undefined) {
        await boardApi.renameSavedView(
          s.boardId,
          viewId,
          changes.name,
          version,
        );
        if (changes.name.trim() !== viewOf(s, viewId).name) version += 1;
      }
      if (changes.visibility !== undefined)
        await boardApi.setSavedViewVisibility(
          s.boardId,
          viewId,
          changes.visibility,
          version,
        );
      if (changes.cardFieldIds !== undefined) {
        // The card face fields live in the view's config, written whole like the card popover writes it. The
        // view is read again so the write names the version the earlier writes left.
        const current = (await boardApi.listSavedViews(s.boardId)).find(
          (view) => view.id === viewId,
        );
        if (!current) throw new Error(`View ${viewId} is gone`);
        await boardApi.updateSavedViewConfig(
          s.boardId,
          viewId,
          { ...current.viewConfig, cardFieldIds: [...changes.cardFieldIds] },
          current.version,
        );
      }
    },
    setDefaultView: (s: BoardSettingsDto, viewId: string) =>
      boardApi.setDefaultSavedView(s.boardId, viewId, viewVersion(s, viewId)),
    /** Takes the view out of the tab strip; the default view hands over to the oldest shared one. */
    archiveView: (s: BoardSettingsDto, viewId: string) =>
      boardApi.archiveSavedView(s.boardId, viewId, viewVersion(s, viewId)),
    unarchiveView: (s: BoardSettingsDto, viewId: string) => {
      const version = s.archivedViews?.find(
        (view) => view.id === viewId,
      )?.version;
      if (!version) throw new Error(`Unknown archived view ${viewId}`);
      return boardApi.unarchiveSavedView(s.boardId, viewId, version);
    },
    /** The default view cannot leave without a next one: the server takes `newDefaultViewId`, else the oldest shared view. */
    deleteView: (
      s: BoardSettingsDto,
      viewId: string,
      newDefaultViewId?: string,
    ) =>
      boardApi.deleteSavedView(
        s.boardId,
        viewId,
        viewVersion(s, viewId),
        newDefaultViewId,
      ),

    createGroup: (s: BoardSettingsDto, name: string) =>
      api.post<unknown>(
        endpoints.boardGroups.create(s.boardId),
        { title: name },
        quiet,
      ),
    reorderGroups: async (s: BoardSettingsDto, ids: readonly string[]) => {
      const moved = movedId(
        s.groups.map((group) => group.id),
        ids,
      );
      if (!moved) return;
      await api.post<void>(
        endpoints.boardGroups.move(moved),
        {
          expectedVersion: find([...s.groups], moved).version ?? 0,
          ...neighbours(ids, moved),
        },
        quiet,
      );
    },
    updateGroup: (
      s: BoardSettingsDto,
      groupId: string,
      changes: { name?: string; color?: string },
    ) =>
      api.patch<void>(
        endpoints.boardGroups.detail(groupId),
        {
          expectedVersion: find([...s.groups], groupId).version ?? 0,
          title: changes.name,
          color: changes.color,
        },
        quiet,
      ),
    duplicateGroup: (_s: BoardSettingsDto, groupId: string, title: string) =>
      api.post<unknown>(
        endpoints.boardGroups.duplicate(groupId),
        { title },
        quiet,
      ),
    archiveGroup: (s: BoardSettingsDto, groupId: string) =>
      api.post<void>(
        endpoints.boardGroups.archive(groupId),
        { expectedVersion: find([...s.groups], groupId).version ?? 0 },
        quiet,
      ),
    unarchiveGroup: (s: BoardSettingsDto, groupId: string) =>
      api.post<void>(
        endpoints.boardGroups.unarchive(groupId),
        { expectedVersion: find([...s.archivedGroups], groupId).version ?? 0 },
        quiet,
      ),
    /** Items of a group that has some move to `moveToGroupId` (a group of the same board). */
    deleteGroup: (
      s: BoardSettingsDto,
      groupId: string,
      moveToGroupId: string | null,
    ) =>
      api.delete<void>(
        `${endpoints.boardGroups.detail(groupId)}?expectedVersion=${find([...s.groups], groupId).version ?? 0}${moveToGroupId ? `&targetGroupId=${moveToGroupId}` : ""}`,
        quiet,
      ),
  };
}

export type BoardSettingsApi = ReturnType<typeof createBoardSettingsApi>;
