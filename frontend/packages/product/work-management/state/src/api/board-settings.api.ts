import type { NotrelixClient, OperationResponse } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  BoardSettingsDto,
  BoardSettingsFieldDto,
  BoardSettingsFieldPatch,
  BoardSettingsFieldType,
  BoardSettingsGeneralPatch,
  BoardSettingsGroupDto,
  BoardSettingsSystemFieldDto,
  BoardSettingsUnavailable,
} from "@notrelix/work-management-core";

type Overview = OperationResponse<"WorkManagement.Boards.GetOverview">;
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
  itemKeyPrefix: true,
  locale: true,
  fieldArchive: true,
  fieldRules: true,
  views: true,
  automations: true,
  nonEmptyGroupDelete: true,
  creatableFieldTypes: CREATABLE_FIELD_TYPES,
};

const DEFAULT_COLOR = "#1E6FD0";

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

function mapField(column: OverviewField): BoardSettingsFieldDto {
  const type = fieldType(column.fieldType);
  const options = byPosition(column.options ?? []).map((option) => ({
    id: option.id ?? "",
    name: option.name ?? "",
    color: option.color ?? DEFAULT_COLOR,
    itemCount: 0,
  }));
  return {
    id: column.id ?? "",
    version: column.version ?? 0,
    key: (column.id ?? "").slice(0, 8),
    type,
    name: column.name ?? "",
    detail: "",
    usage: "",
    accent: type === "status",
    unit: unitOf(column.settings),
    hasColumn: type === "status" || type === "select" || type === "multiselect",
    valueCount: 0,
    options,
    defaultOptionId: null,
    required: false,
    guidance: "",
    usedIn: [],
  };
}

function mapSystemField(column: OverviewField): BoardSettingsSystemFieldDto {
  return {
    id: column.id ?? "",
    key: (column.id ?? "").slice(0, 8),
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
    itemCount: (group.boardItems ?? []).length,
  };
}

/** The settings page model of a board read. */
export function mapBoardSettings(
  boardId: string,
  board: Overview,
): BoardSettingsDto {
  const columns = byPosition(
    (board.columns ?? []).filter((column) => column.isDeleted !== true),
  );
  const groups = byPosition(
    (board.lists ?? []).filter((group) => group.isArchived !== true),
  ).map(mapGroup);
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
      timeZone: "Asia/Ho_Chi_Minh",
      weekStart: "monday",
      isArchived: false,
    },
    fields: columns
      .filter((column) => !isLockedSystemField(column))
      .map(mapField),
    systemFields: columns.filter(isLockedSystemField).map(mapSystemField),
    archivedFields: [],
    views: [],
    groups,
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
      return mapBoardSettings(boardId, board);
    },

    updateGeneral: (s: BoardSettingsDto, changes: BoardSettingsGeneralPatch) =>
      api.patch<void>(
        endpoints.boards.detail(s.boardId),
        {
          expectedVersion: s.version,
          title: changes.title,
          description: changes.description,
          background: changes.color,
        },
        quiet,
      ),
    archiveBoard: (s: BoardSettingsDto) =>
      api.post<void>(
        `${endpoints.boards.detail(s.boardId)}/archive`,
        {},
        quiet,
      ),
    deleteBoard: (s: BoardSettingsDto) =>
      api.delete<void>(endpoints.boards.detail(s.boardId), quiet),

    createField: (
      s: BoardSettingsDto,
      type: BoardSettingsFieldType,
      name: string,
    ) =>
      api.post<unknown>(
        endpoints.boardFields.create(s.boardId),
        { name, type },
        quiet,
      ),
    reorderFields: async (s: BoardSettingsDto, ids: readonly string[]) => {
      const moved = movedId(
        s.fields.map((field) => field.id),
        ids,
      );
      if (!moved) return;
      await api.post<void>(
        endpoints.boardFields.move(s.boardId, moved),
        {
          expectedVersion: fieldOf(s, moved).version ?? 0,
          ...neighbours(ids, moved),
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
    ) =>
      api.patch<void>(
        endpoints.boardFields.option(s.boardId, fieldId, optionId),
        { ...changes, expectedVersion: optionVersion(s, fieldId) },
        quiet,
      ),
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
    deleteGroup: (s: BoardSettingsDto, groupId: string) =>
      api.delete<void>(
        `${endpoints.boardGroups.detail(groupId)}?expectedVersion=${find([...s.groups], groupId).version ?? 0}`,
        quiet,
      ),
  };
}

export type BoardSettingsApi = ReturnType<typeof createBoardSettingsApi>;
