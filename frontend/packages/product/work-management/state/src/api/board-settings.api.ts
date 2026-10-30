import type {
  NotrelixClient,
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  BoardSettingsDto,
  BoardSettingsComputedDto,
  BoardSettingsFieldDto,
  BoardSettingsFieldPatch,
  BoardSettingsFieldType,
  BoardSettingsGeneralPatch,
  BoardSettingsGroupDto,
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
  fieldRules: true,
  views: false,
  viewSplit: true,
  automations: true,
  creatableFieldTypes: CREATABLE_FIELD_TYPES,
};

const DEFAULT_COLOR = "#1E6FD0";

/** What a board inherits from its workspace (`Workspaces.Settings.GetWorkspaceSettings`). */
interface WorkspaceLocale {
  readonly timeZone: string;
  readonly weekStart: "monday" | "sunday";
}

const DEFAULT_LOCALE: WorkspaceLocale = {
  timeZone: "Asia/Ho_Chi_Minh",
  weekStart: "monday",
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

function byPosition<T extends { position?: string | null }>(
  rows: readonly T[],
): T[] {
  return [...rows].sort((a, b) => {
    const left = a.position ?? "";
    const right = b.position ?? "";
    return left < right ? -1 : left > right ? 1 : 0;
  });
}

/** A system field the page lists as locked, except Status, whose options are the board's to edit. */
function isLockedSystemField(column: OverviewField): boolean {
  return column.isSystem === true && fieldType(column.fieldType) !== "status";
}

/** The key a field is listed under: its name as a lowercase slug ("due-date"), or a short id when the name has no letters. */
function fieldKey(column: OverviewField): string {
  const slug = (column.name ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  return slug || (column.id ?? "").slice(0, 8);
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
    hasColumn: type === "status" || type === "select" || type === "multiselect",
    valueCount: column.valueCount ?? 0,
    options,
    defaultOptionId: null,
    required: false,
    guidance: "",
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
    cardFieldIds: [],
    cardFieldChoices: [],
    lastEditedBy: "",
    createdBy: "",
    needsFix: false,
  };
}

/** The settings page model of a board read. */
export function mapBoardSettings(
  boardId: string,
  board: Overview,
  inherited: WorkspaceLocale = DEFAULT_LOCALE,
  savedViews: readonly SavedBoardView[] = [],
): BoardSettingsDto {
  const columns = byPosition(
    (board.columns ?? []).filter((column) => column.isDeleted !== true),
  );
  const groups = byPosition(
    (board.lists ?? []).filter((group) => group.isArchived !== true),
  ).map(mapGroup);
  const archivedGroups = byPosition(
    (board.lists ?? []).filter((group) => group.isArchived === true),
  ).map(mapGroup);
  const fields = columns
    .filter((column) => !isLockedSystemField(column))
    .map(mapField);
  return {
    boardId,
    version: board.version ?? 0,
    itemCount: groups.reduce((sum, group) => sum + group.itemCount, 0),
    general: {
      title: board.title ?? "",
      description: board.description ?? "",
      color: board.background || DEFAULT_COLOR,
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

  return {
    async get(boardId: string): Promise<BoardSettingsDto> {
      const board = await api.get<Overview>(
        endpoints.boards.full(boardId),
        quiet,
      );
      // The workspace's locale is context, not the page's own data: without it the defaults show.
      const inherited = board.workspaceId
        ? await api
            .get<{ timeZone?: string | null; weekStart?: string | null }>(
              endpoints.workspaces.settings(board.workspaceId),
              quiet,
            )
            .then((workspace): WorkspaceLocale => ({
              timeZone: workspace.timeZone || DEFAULT_LOCALE.timeZone,
              weekStart: workspace.weekStart === "sunday" ? "sunday" : "monday",
            }))
            .catch(() => DEFAULT_LOCALE)
        : DEFAULT_LOCALE;
      const savedViews = await boardApi.listSavedViews(boardId).catch(() => []);
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
        ...mapBoardSettings(boardId, board, inherited, savedViews),
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
        { name, type, ...(settings ? { settings } : {}) },
        quiet,
      ),
    deleteField: (s: BoardSettingsDto, fieldId: string) =>
      api.delete<void>(endpoints.boardFields.detail(s.boardId, fieldId), quiet),
    reorderFields: async (s: BoardSettingsDto, ids: readonly string[]) => {
      const moved = movedId(
        s.fields.map((field) => field.id),
        ids,
      );
      if (!moved) return;
      // The page lists the custom fields only; the server orders all of them.
      const listed = new Set(s.fields.map((field) => field.id));
      let next = 0;
      const full = (s.fieldOrder ?? s.fields.map((field) => field.id)).map(
        (id) => (listed.has(id) ? ids[next++]! : id),
      );
      await api.post<void>(
        endpoints.boardFields.move(s.boardId, moved),
        {
          expectedVersion: fieldOf(s, moved).version ?? 0,
          ...neighbours(full, moved),
        },
        quiet,
      );
    },
    updateField: (
      s: BoardSettingsDto,
      fieldId: string,
      changes: BoardSettingsFieldPatch,
    ) =>
      api.patch<void>(
        endpoints.boardFields.detail(s.boardId, fieldId),
        {
          expectedVersion: fieldOf(s, fieldId).version ?? 0,
          name: changes.name,
          ...(changes.settings ? { settings: changes.settings } : {}),
        },
        quiet,
      ),
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
      if (changes.name !== undefined)
        await boardApi.renameSavedView(s.boardId, viewId, changes.name);
      if (changes.visibility !== undefined)
        await boardApi.setSavedViewVisibility(
          s.boardId,
          viewId,
          changes.visibility,
        );
    },
    setDefaultView: (s: BoardSettingsDto, viewId: string) =>
      boardApi.setDefaultSavedView(s.boardId, viewId),
    deleteView: (s: BoardSettingsDto, viewId: string) =>
      boardApi.deleteSavedView(s.boardId, viewId),

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
