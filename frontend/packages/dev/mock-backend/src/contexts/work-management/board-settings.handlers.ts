/**
 * Work Management: "Thiết lập board" handlers (Final-Settings.dc.html).
 *
 * Gap `CTR-GAP-WM-BOARD-SETTINGS` (docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Settings"): one read of everything the settings
 * page lists, and its writes. Only a manager of the board reads or writes
 * them; anyone else gets 403 and the page shows the no-access card. A write
 * may carry the `version` of the read it was made from: a stale one is 409.
 *
 * Operations:
 *   boards.settings.get             GET    /boards/:boardId/settings
 *   boards.settings.general         PATCH  /boards/:boardId/settings/general
 *   boards.settings.archive         POST   /boards/:boardId/settings/archive
 *   boards.settings.delete          POST   /boards/:boardId/settings/delete
 *   boards.settings.fields.create   POST   /boards/:boardId/settings/fields
 *   boards.settings.fields.reorder  POST   /boards/:boardId/settings/fields/reorder
 *   boards.settings.fields.update   PATCH  /boards/:boardId/settings/fields/:fieldId
 *   boards.settings.fields.archive  POST   /boards/:boardId/settings/fields/:fieldId/archive
 *   boards.settings.fields.restore  POST   /boards/:boardId/settings/fields/:fieldId/restore
 *   boards.settings.options.*       options of a field (add, update, delete, reorder)
 *   boards.settings.views.*         saved views (create, update, duplicate, delete, reorder)
 *   boards.settings.groups.*        groups (create, update, delete, reorder)
 *   boards.settings.automations.update  PATCH /boards/:boardId/settings/automations/:id
 */
import { defineMockOperation } from "../../operations/types";
import {
  conflict,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
  type MockHttpResult,
} from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import {
  ERP_SETTINGS_SYSTEM_FIELDS,
  deleteGroup,
  groupsOf,
  type SettingsFieldType,
  type SettingsWriteResult,
} from "../../state/mock-board-settings";
import { canManageBoard } from "./boards.handlers";

const GAP = { kind: "gap", gapId: "CTR-GAP-WM-BOARD-SETTINGS" } as const;

type Params = { boardId: string } & Record<string, string>;

/** Fields the settings page may add by type. */
const OPTION_TYPES: readonly SettingsFieldType[] = [
  "select",
  "multiselect",
  "status",
];

function typeLabel(type: SettingsFieldType): string {
  return type;
}

/** The read: everything the settings page lists. */
function project(store: MockStore, boardId: string) {
  const board = store.getBoard(boardId)!;
  const registry = store.settings.registry(boardId);
  const itemCount = groupsOf(store, boardId).reduce(
    (sum, group) => sum + group.itemCount,
    0,
  );
  const space = board.spaceId ? store.spaces.getSpace(board.spaceId) : null;
  const live = registry.fields.filter((field) => !field.archived);
  const fieldDto = (field: (typeof registry.fields)[number]) => {
    const options = store.settings.optionsOf(boardId, field);
    const hasOptions = OPTION_TYPES.includes(field.type) && options.length > 0;
    return {
      id: field.id,
      key: field.key,
      type: typeLabel(field.type),
      name: field.name,
      detail: hasOptions ? `${options.length} lựa chọn` : (field.detail ?? ""),
      usage: field.usage,
      accent: field.accent ?? false,
      unit: field.unit ?? null,
      hasColumn: field.columnId !== undefined,
      valueCount: store.settings.valueCount(boardId, field),
      options,
      defaultOptionId: field.defaultOptionId,
      required: field.required,
      guidance: field.guidance,
      usedIn: field.usedIn,
    };
  };
  return {
    boardId,
    version: registry.version,
    itemCount,
    general: {
      title: board.title,
      description: board.description ?? "",
      color: board.background.value,
      icon: registry.general.icon,
      sampleItemKey: registry.general.sampleItemKey ?? null,
      itemKeyPrefix: board.itemKeyPrefix ?? "",
      department: space ? { id: space.id, name: space.name } : null,
      timeZone: registry.general.timeZone,
      weekStart: registry.general.weekStart,
      isArchived: board.isArchived,
    },
    fields: live.map(fieldDto),
    systemFields: ERP_SETTINGS_SYSTEM_FIELDS.map((field) => ({
      ...field,
      type: typeLabel(field.type),
    })),
    archivedFields: registry.fields
      .filter((field) => field.archived)
      .map((field) => ({
        id: field.id,
        key: field.key,
        type: typeLabel(field.type),
        name: field.name,
        archivedAt: field.archived!.at,
        archivedByName: field.archived!.byName,
        valueCount: field.archived!.values,
      })),
    views: registry.views.map((view) => ({
      id: view.id,
      name: view.name,
      kind: view.kind,
      summary: view.summary,
      visibility: view.visibility,
      isDefault: view.isDefault,
      fieldId: view.fieldId,
      filters: view.filters,
      sort: view.sort,
      cardFieldIds: view.cardFieldIds,
      cardFieldChoices: view.cardFieldChoices,
      lastEditedBy: view.lastEditedBy,
      createdBy: view.createdBy,
      needsFix:
        registry.fields.find((field) => field.id === view.fieldId)?.archived !==
        undefined,
    })),
    groups: groupsOf(store, boardId),
    automations: store.automations.listFor(boardId),
  };
}

type Mutation<Body> = {
  id: string;
  method: "POST" | "PATCH";
  route: string;
  run: (
    store: MockStore,
    params: Params,
    body: Body,
  ) => SettingsWriteResult<unknown> | "no-content";
};

function reply(result: SettingsWriteResult<unknown>): MockHttpResult<unknown> {
  if (result.result === "ok")
    return result.value === undefined ? noContent() : ok(result.value);
  if (result.result === "not-found") return notFound("Not found");
  if (result.result === "conflict")
    return conflict(result.message ?? "Conflict");
  return validationError({ [result.message ?? "request"]: ["invalid"] });
}

/** A guarded write: manager only, optional stale `version` check, bump on success. */
function write<Body extends { version?: number }>(m: Mutation<Body>) {
  return defineMockOperation<Params, Body, unknown>({
    id: m.id,
    contract: GAP,
    method: m.method,
    route: m.route,
    async handle({ params, body, store }) {
      if (!store.getBoard(params.boardId)) return notFound("Board not found");
      if (!canManageBoard(params.boardId, store)) return forbidden();
      const registry = store.settings.registry(params.boardId);
      const data = (body ?? {}) as Body;
      if (data.version !== undefined && data.version !== registry.version)
        return conflict("The settings changed since they were read");
      const result = m.run(store, params, data);
      if (result === "no-content") return noContent();
      if (result.result === "ok") registry.version += 1;
      return reply(result);
    },
  });
}

const base = "/boards/:boardId/settings";

export const boardSettingsOperations = [
  defineMockOperation<Params, never, unknown>({
    id: "boards.settings.get",
    contract: GAP,
    method: "GET",
    route: base,
    async handle({ params, store }) {
      if (!store.getBoard(params.boardId)) return notFound("Board not found");
      if (!canManageBoard(params.boardId, store)) return forbidden();
      return ok(project(store, params.boardId));
    },
  }),

  write<{ version?: number } & Record<string, unknown>>({
    id: "boards.settings.general",
    method: "PATCH",
    route: `${base}/general`,
    run: (store, { boardId }, body) =>
      store.settings.updateGeneral(boardId, body as never),
  }),
  write<{ version?: number }>({
    id: "boards.settings.archive",
    method: "POST",
    route: `${base}/archive`,
    run: (store, { boardId }) => {
      store.patchBoard(boardId, { isArchived: true });
      return { result: "ok", value: undefined };
    },
  }),
  write<{ version?: number; confirmTitle?: string }>({
    id: "boards.settings.delete",
    method: "POST",
    route: `${base}/delete`,
    run: (store, { boardId }, body) => {
      if (store.getBoard(boardId)?.title !== body.confirmTitle)
        return { result: "invalid", message: "confirmTitle" };
      store.deleteBoard(boardId);
      return { result: "ok", value: undefined };
    },
  }),

  // Fields
  write<{ version?: number; type: SettingsFieldType }>({
    id: "boards.settings.fields.create",
    method: "POST",
    route: `${base}/fields`,
    run: (store, { boardId }, body) =>
      store.settings.createField(boardId, body.type),
  }),
  write<{ version?: number; ids: string[] }>({
    id: "boards.settings.fields.reorder",
    method: "POST",
    route: `${base}/fields/reorder`,
    run: (store, { boardId }, body) =>
      store.settings.reorderFields(boardId, body.ids ?? []),
  }),
  write<{
    version?: number;
    name?: string;
    defaultOptionId?: string | null;
    required?: boolean;
    guidance?: string;
  }>({
    id: "boards.settings.fields.update",
    method: "PATCH",
    route: `${base}/fields/:fieldId`,
    run: (store, { boardId, fieldId }, body) =>
      store.settings.updateField(boardId, fieldId!, body),
  }),
  write<{ version?: number }>({
    id: "boards.settings.fields.archive",
    method: "POST",
    route: `${base}/fields/:fieldId/archive`,
    run: (store, { boardId, fieldId }) =>
      store.settings.archiveField(boardId, fieldId!),
  }),
  write<{ version?: number }>({
    id: "boards.settings.fields.restore",
    method: "POST",
    route: `${base}/fields/:fieldId/restore`,
    run: (store, { boardId, fieldId }) =>
      store.settings.restoreField(boardId, fieldId!),
  }),
  write<{ version?: number; name: string; color: string }>({
    id: "boards.settings.options.add",
    method: "POST",
    route: `${base}/fields/:fieldId/options`,
    run: (store, { boardId, fieldId }, body) =>
      store.settings.addOption(boardId, fieldId!, body),
  }),
  write<{ version?: number; ids: string[] }>({
    id: "boards.settings.options.reorder",
    method: "POST",
    route: `${base}/fields/:fieldId/options/reorder`,
    run: (store, { boardId, fieldId }, body) =>
      store.settings.reorderOptions(boardId, fieldId!, body.ids ?? []),
  }),
  write<{ version?: number; name?: string; color?: string }>({
    id: "boards.settings.options.update",
    method: "PATCH",
    route: `${base}/fields/:fieldId/options/:optionId`,
    run: (store, { boardId, fieldId, optionId }, body) =>
      store.settings.updateOption(boardId, fieldId!, optionId!, body),
  }),
  write<{ version?: number }>({
    id: "boards.settings.options.delete",
    method: "POST",
    route: `${base}/fields/:fieldId/options/:optionId/delete`,
    run: (store, { boardId, fieldId, optionId }) =>
      store.settings.deleteOption(boardId, fieldId!, optionId!),
  }),

  // Views
  write<{
    version?: number;
    kind: "table" | "kanban" | "calendar" | "timeline";
  }>({
    id: "boards.settings.views.create",
    method: "POST",
    route: `${base}/views`,
    run: (store, { boardId }, body) => ({
      result: "ok",
      value: store.settings.createView(boardId, body.kind ?? "table"),
    }),
  }),
  write<{ version?: number; ids: string[] }>({
    id: "boards.settings.views.reorder",
    method: "POST",
    route: `${base}/views/reorder`,
    run: (store, { boardId }, body) =>
      store.settings.reorderViews(boardId, body.ids ?? []),
  }),
  write<{
    version?: number;
    name?: string;
    fieldId?: string | null;
    cardFieldIds?: string[];
    visibility?: "board" | "me";
  }>({
    id: "boards.settings.views.update",
    method: "PATCH",
    route: `${base}/views/:viewId`,
    run: (store, { boardId, viewId }, body) =>
      store.settings.updateView(boardId, viewId!, body),
  }),
  write<{ version?: number }>({
    id: "boards.settings.views.duplicate",
    method: "POST",
    route: `${base}/views/:viewId/duplicate`,
    run: (store, { boardId, viewId }) =>
      store.settings.duplicateView(boardId, viewId!),
  }),
  write<{ version?: number }>({
    id: "boards.settings.views.delete",
    method: "POST",
    route: `${base}/views/:viewId/delete`,
    run: (store, { boardId, viewId }) =>
      store.settings.deleteView(boardId, viewId!),
  }),

  // Groups
  write<{ version?: number; name: string }>({
    id: "boards.settings.groups.create",
    method: "POST",
    route: `${base}/groups`,
    run: (store, { boardId }, body) => {
      const name = body.name?.trim();
      if (!name) return { result: "invalid", message: "name" };
      const palette = [
        "#1E6FD0",
        "#0E7490",
        "#5532A8",
        "#9A6A12",
        "#C2410C",
        "#2C5E4F",
      ];
      const count = store.getLists(boardId).length;
      store.createList(boardId, {
        title: name,
        color: palette[count % palette.length],
        position: count + 1,
      });
      return { result: "ok", value: undefined };
    },
  }),
  write<{ version?: number; ids: string[] }>({
    id: "boards.settings.groups.reorder",
    method: "POST",
    route: `${base}/groups/reorder`,
    run: (store, { boardId }, body) => {
      const lists = store.getLists(boardId);
      const ids = body.ids ?? [];
      if (
        ids.length !== lists.length ||
        !ids.every((id) => lists.some((list) => list.id === id))
      )
        return { result: "invalid" };
      ids.forEach((id, index) => {
        const list = store.getList(id);
        if (list) store.setListPosition(id, index + 1);
      });
      return { result: "ok", value: undefined };
    },
  }),
  write<{ version?: number; name?: string; color?: string }>({
    id: "boards.settings.groups.update",
    method: "PATCH",
    route: `${base}/groups/:groupId`,
    run: (store, { groupId }, body) => {
      const name = body.name?.trim();
      if (name === "") return { result: "invalid", message: "name" };
      return store.updateList(groupId!, { title: name, color: body.color })
        ? { result: "ok", value: undefined }
        : { result: "not-found" };
    },
  }),
  write<{ version?: number; moveToGroupId?: string | null }>({
    id: "boards.settings.groups.delete",
    method: "POST",
    route: `${base}/groups/:groupId/delete`,
    run: (store, { boardId, groupId }, body) =>
      deleteGroup(store, boardId, groupId!, body.moveToGroupId ?? null),
  }),

  // Automations
  write<{ version?: number; enabled: boolean }>({
    id: "boards.settings.automations.update",
    method: "PATCH",
    route: `${base}/automations/:automationId`,
    run: (store, { boardId, automationId }, body) =>
      store.settings.setAutomationEnabled(boardId, automationId!, body.enabled),
  }),
];
