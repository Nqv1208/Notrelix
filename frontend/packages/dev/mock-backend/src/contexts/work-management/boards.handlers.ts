/**
 * Work Management — Boards context handlers.
 *
 * Operations:
 *   boards.listByWorkspace — GET /workspaces/:workspaceId/boards
 *   boards.detail          — GET /boards/:id
 *   boards.full            — GET /boards/:id/full
 *   boards.access          — GET /boards/:boardId/access
 *   boards.create          — POST /workspaces/:workspaceId/boards
 *   boards.view.get        — GET /boards/:id/view
 *   boards.view.save       — PUT /boards/:id/view
 *   boards.columns.list    — GET /boards/:boardId/fields
 *   boards.columns.create  — POST /boards/:boardId/fields
 *   boards.columns.update  — PATCH /boards/:boardId/fields/:columnId
 *   boards.columns.delete  — DELETE /boards/:boardId/fields/:columnId
 *   boards.columns.move — POST /boards/:boardId/fields/:fieldId/move
 *   boards.labels.list     — GET /boards/:boardId/labels
 *   boards.labels.create   — POST /boards/:boardId/labels
 *   boards.labels.update   — PATCH /boards/:boardId/labels/:labelId
 *   boards.labels.delete   — DELETE /boards/:boardId/labels/:labelId
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 */

import type { OperationResponse } from "@notrelix/contracts";
import type {
  BoardDtoApi,
  BoardViewDtoApi,
} from "@notrelix/work-management-core";
import { defineMockOperation } from "../../operations/types";
import {
  ok,
  created,
  forbidden,
  noContent,
  notFound,
  validationError,
} from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import type { MockBoardRecord, MockCardRecord } from "../../state/records";
import {
  preconditionFailed,
  targetSpaceDenied,
} from "../workspace/space-target";

/** The Board's Space id, masked when the caller cannot see it (GOV-029). */
function visibleSpaceId(
  store: MockStore,
  workspaceId: string,
  spaceId: string | null | undefined,
): string | null {
  if (!spaceId) return null;
  return store.spaces.accessFor(spaceId, store.spaceViewer(workspaceId)) ===
    null
    ? null
    : spaceId;
}

type GeneratedFullBoardDto =
  OperationResponse<"WorkManagement.Boards.GetOverview">;
type GeneratedBoardAccessDto =
  OperationResponse<"WorkManagement.Boards.GetAccess">;

type FullBoardField = NonNullable<GeneratedFullBoardDto["columns"]>[number];
type FullBoardGroup = NonNullable<GeneratedFullBoardDto["lists"]>[number];
type FullBoardItem = NonNullable<FullBoardGroup["boardItems"]>[number];

/** Backend field type names (`FieldType` enum) for the mock column types. */
const FIELD_TYPE_NAMES: Record<string, string> = {
  status: "Status",
  select: "Select",
  multi_select: "MultiSelect",
  user: "Person",
  person: "Person",
  date: "Date",
  number: "Number",
  checkbox: "Checkbox",
  text: "Text",
};

/** Fractional-index style keys that sort in board order ("a00" < "a01"). */
function positionKey(index: number): string {
  return `a${index.toString(36).padStart(2, "0")}`;
}

/** Same rule as the backend read: the first Select named like a priority. */
function isPriorityName(name: string): boolean {
  const normalized = name
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
  return ["priority", "uu_tien", "do_uu_tien", "muc_uu_tien"].includes(
    normalized,
  );
}

function optionValue(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

/**
 * The item read values (`BoardItemSummaryDto` / `BoardItemDto` additions):
 * key, parent, Status and priority option ids, dates and child progress.
 */
export function projectItemReadValues(card: MockCardRecord, store: MockStore) {
  const columns = store.getColumns(card.boardId);
  const statusColumn = columns.find((c) => c.fieldType === "status");
  const priorityColumn = columns.find(
    (c) => c.fieldType === "select" && isPriorityName(c.name),
  );
  const values = store.getFieldValues(card.id);
  const statusOptionId = statusColumn
    ? optionValue(values[statusColumn.id])
    : null;
  const children = store.getChildCards(card.id);
  const doneIds = new Set(
    (statusColumn?.options ?? [])
      .filter((option) => option.category === "done")
      .map((option) => option.id),
  );
  const childProgress =
    children.length > 0
      ? {
          done: children.filter((child) =>
            doneIds.has(
              optionValue(store.getFieldValues(child.id)[statusColumn!.id]) ??
                "",
            ),
          ).length,
          total: children.length,
        }
      : (card.childProgress ?? { done: 0, total: 0 });
  return {
    itemKey: card.itemKey ?? null,
    parentItemId: card.parentItemId ?? null,
    statusOptionId,
    priorityOptionId: priorityColumn
      ? optionValue(values[priorityColumn.id])
      : null,
    dueAt: card.dueAt ?? null,
    startedAt: card.startedAt ?? null,
    completedAt: card.completedAt ?? null,
    childProgress,
  };
}

function projectItemMembers(card: MockCardRecord, store: MockStore) {
  return (card.assigneeUserIds ?? []).flatMap((userId) => {
    const user = store.getUser(userId);
    return user
      ? [
          {
            userId,
            name: user.name,
            avatar: user.avatarUrl,
            assignedAt: card.createdAt,
          },
        ]
      : [];
  });
}

function projectItemSummary(
  card: MockCardRecord,
  index: number,
  store: MockStore,
): FullBoardItem {
  const members = projectItemMembers(card, store);
  const checklistItems = store
    .getCardChecklists(card.id)
    .flatMap((checklist) => checklist.items);
  return {
    id: card.id,
    name: card.title,
    memberCount: members.length,
    members,
    labels: store.getCardLabels(card.id).map((label) => ({
      labelId: label.id,
      name: label.name,
      color: label.color,
    })),
    checklistProgress: checklistItems.filter((item) => item.isChecked).length,
    checklistTotal: checklistItems.length,
    commentCount: store.getCardComments(card.id).length,
    attachmentCount: 0,
    position: positionKey(index),
    createdAt: card.createdAt,
    updatedAt: card.updatedAt,
    version: 1,
    ...projectItemReadValues(card, store),
  };
}

function projectField(
  column: ReturnType<MockStore["getColumns"]>[number],
  index: number,
): FullBoardField {
  const fieldType =
    FIELD_TYPE_NAMES[column.fieldType.trim().toLowerCase()] ?? "Text";
  return {
    id: column.id,
    boardId: column.boardId,
    name: column.name,
    fieldType,
    settings: column.settings ?? "{}",
    defaultValue: null,
    position: positionKey(index + 1),
    isSystem: fieldType === "Status",
    isDeleted: false,
    version: 1,
    ...(fieldType === "Status" || fieldType === "Select"
      ? {
          options: (column.options ?? []).map((option) => ({
            id: option.id,
            name: option.name,
            color: option.color,
            position: option.position,
            category: option.category ?? null,
          })),
        }
      : {}),
  };
}

/** `GET /boards/{id}/full` in the shape of the generated `FullBoardDto`. */
function projectFullBoard(
  board: MockBoardRecord,
  store: MockStore,
): GeneratedFullBoardDto {
  const titleField: FullBoardField = {
    // Like the backend: a synthetic, read-only title column with the Board id.
    id: board.id,
    boardId: board.id,
    name: "Task",
    fieldType: "text",
    settings: '{"system":"title"}',
    defaultValue: null,
    position: positionKey(0),
    isSystem: true,
    isDeleted: false,
    version: 0,
  };
  return {
    id: board.id,
    workspaceId: board.workspaceId,
    title: board.title,
    description: board.description ?? null,
    background: JSON.stringify(board.background),
    visibility: board.visibility === "private" ? "Private" : "Workspace",
    columns: [titleField, ...store.getColumns(board.id).map(projectField)],
    lists: store.getLists(board.id).map((list, index) => ({
      id: list.id,
      title: list.title,
      color: list.color ?? null,
      position: positionKey(index),
      isArchived: false,
      version: 1,
      boardItems: store
        .getCards(list.id)
        .filter((card) => !card.parentItemId)
        .map((card, cardIndex) => projectItemSummary(card, cardIndex, store)),
    })),
    members: store.getBoardMembers(board.id).flatMap((member) => {
      const user = store.getUser(member.userId);
      return user
        ? [
            {
              userId: member.userId,
              name: user.name,
              avatar: user.avatarUrl,
              role: member.role,
              joinedAt: member.joinedAt,
            },
          ]
        : [];
    }),
    version: board.version ?? 1,
    spaceId: visibleSpaceId(store, board.workspaceId, board.spaceId),
    itemKeyPrefix: board.itemKeyPrefix ?? null,
  };
}

type AccessLevel = "viewer" | "commenter" | "editor" | "manager";
const LEVEL_RANK: Record<AccessLevel, number> = {
  viewer: 0,
  commenter: 1,
  editor: 2,
  manager: 3,
};
const SPACE_LEVEL: Record<string, AccessLevel> = {
  Manager: "manager",
  Editor: "editor",
  Viewer: "viewer",
};

/**
 * The caller's board access, after the backend's order: workspace Owner;
 * then the highest of the explicit entry and the Space or workspace default
 * (ties go to the explicit entry). Guests reach a board only through an
 * entry, except the mock's unplaced boards (which every member can open).
 * `null` = no access, answered NotFound like a missing board.
 */
function projectBoardAccess(
  board: MockBoardRecord,
  store: MockStore,
): GeneratedBoardAccessDto | null {
  const role = store.getCurrentMemberRole(board.workspaceId);
  if (role === null) return null;
  const me = store.getCurrentUser().id;
  const grant = store.getBoardGrant(board.id, me);
  const grantor = grant ? store.getUser(grant.grantedByUserId) : undefined;
  const fromGrant = grant
    ? {
        level: grant.level as AccessLevel,
        kind: role === "guest" ? "guest" : "explicit",
        grantedBy: grantor ? { userId: grantor.id, name: grantor.name } : null,
      }
    : null;

  let fromRole: { level: AccessLevel; kind: string } | null = null;
  if (role === "owner") {
    fromRole = { level: "manager", kind: "owner" };
  } else if (board.spaceId) {
    if (role === "admin")
      fromRole = { level: "manager", kind: "workspace-admin" };
    else if (role === "member") {
      const access = store.spaces.accessFor(
        board.spaceId,
        store.spaceViewer(board.workspaceId),
      );
      if (access)
        fromRole = { level: SPACE_LEVEL[access] ?? "viewer", kind: "space" };
    }
  } else {
    fromRole =
      role === "guest"
        ? { level: "viewer", kind: "guest" }
        : { level: "editor", kind: "workspace" };
  }

  const chosen =
    role === "owner"
      ? fromRole
      : fromGrant &&
          (!fromRole ||
            LEVEL_RANK[fromGrant.level] >= LEVEL_RANK[fromRole.level])
        ? fromGrant
        : fromRole;
  if (!chosen) return null;

  const isManager = chosen.level === "manager";
  const audience = store.getBoardAudience(board.id);
  const sources = audience
    ? audience.sources.map((source) => {
        if (source.kind === "space" && source.id) {
          const visible = visibleSpaceId(store, board.workspaceId, source.id);
          return {
            kind: source.kind,
            id: visible,
            name: visible
              ? (store.spaces.getSpace(visible)?.name ?? null)
              : null,
            count: source.count,
          };
        }
        const named = isManager && source.kind !== "guest";
        return {
          kind: source.kind,
          id: named ? source.id : null,
          name: named ? source.name : null,
          count: source.count,
        };
      })
    : [
        {
          kind: "user",
          id: null,
          name: null,
          count: store
            .getWorkspaceMembers(board.workspaceId)
            .filter((member) => member.role !== "guest").length,
        },
      ];
  return {
    boardId: board.id,
    version: board.version ?? 1,
    myLevel: chosen.level,
    mySource: {
      kind: chosen.kind,
      ...("grantedBy" in chosen && chosen.grantedBy
        ? { grantedBy: chosen.grantedBy }
        : {}),
    },
    peopleCount: sources.reduce((sum, source) => sum + source.count, 0),
    guestCount: audience?.guestCount ?? 0,
    peopleCountComplete: false,
    sources,
  };
}

export const boardsOperations = [
  // ─── GET /workspaces/:workspaceId/boards ──────────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, BoardDtoApi[]>({
    id: "boards.listByWorkspace",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/workspaces/:workspaceId/boards",
    async handle({ params, store }) {
      const boards = store.getBoards(params.workspaceId);
      return ok<BoardDtoApi[]>(
        boards.map((b) => ({
          id: b.id,
          workspaceId: b.workspaceId,
          title: b.title,
          description: b.description ?? null,
          background: JSON.stringify(b.background),
          visibility: b.visibility,
          isArchived: b.isArchived,
          memberCount: 1,
          listCount: store.getLists(b.id).length,
          createdAt: b.createdAt,
          version: b.version ?? 1,
          spaceId: visibleSpaceId(store, params.workspaceId, b.spaceId),
        })),
      );
    },
  }),

  // ─── GET /boards/:id ─────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, BoardDtoApi>({
    id: "boards.detail",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/boards/:id",
    async handle({ params, store }) {
      const b = store.getBoard(params.id);
      if (!b) return notFound("Board not found");
      return ok<BoardDtoApi>({
        id: b.id,
        workspaceId: b.workspaceId,
        title: b.title,
        description: b.description ?? null,
        background: JSON.stringify(b.background),
        visibility: b.visibility,
        isArchived: b.isArchived,
        memberCount: 1,
        listCount: store.getLists(b.id).length,
        createdAt: b.createdAt,
      });
    },
  }),

  // ─── GET /boards/:id/full ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, GeneratedFullBoardDto>({
    id: "boards.full",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/boards/:id/full",
    async handle({ params, store }) {
      const b = store.getBoard(params.id);
      if (!b) return notFound("Board not found");
      return ok<GeneratedFullBoardDto>(projectFullBoard(b, store));
    },
  }),

  // ─── GET /boards/:boardId/access ─────────────────────────────────────────

  defineMockOperation<{ boardId: string }, never, GeneratedBoardAccessDto>({
    id: "boards.access",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Boards.GetAccess",
    },
    method: "GET",
    route: "/boards/:boardId/access",
    async handle({ params, store }) {
      const board = store.getBoard(params.boardId);
      // No access answers exactly like a missing board.
      const access = board ? projectBoardAccess(board, store) : null;
      if (!board || !access) return notFound("Board not found");
      return ok<GeneratedBoardAccessDto>(access);
    },
  }),

  // ─── POST /workspaces/:workspaceId/boards ─────────────────────────────────

  defineMockOperation<
    { workspaceId: string },
    { title?: string; description?: string },
    string
  >({
    id: "boards.create",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/workspaces/:workspaceId/boards",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        title?: string;
        description?: string;
        spaceId?: string | null;
      };
      const denied = targetSpaceDenied(
        store,
        params.workspaceId,
        data.spaceId ?? null,
      );
      if (denied) return denied;
      const newBoard = store.createBoard(params.workspaceId, {
        title: data.title,
        description: data.description,
        spaceId: data.spaceId ?? null,
      });
      // Like the producer (`ToCreatedResult`): 201 with the new Board id.
      return created<string>(newBoard.id);
    },
  }),

  // ─── POST /boards/:boardId/move-to-space (GOV-028) ─────────────────────────

  defineMockOperation<
    { boardId: string },
    { spaceId?: string | null; expectedVersion?: number },
    void
  >({
    id: "boards.moveToSpace",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Boards.MoveToSpace",
    },
    method: "POST",
    route: "/boards/:boardId/move-to-space",
    async handle({ params, body, store, request }) {
      if (!request.headers.get("Idempotency-Key")) {
        return validationError({
          "Idempotency-Key": ["The header is required."],
        });
      }
      const board = store.getBoard(params.boardId);
      if (!board || !store.isCurrentMemberOf(board.workspaceId))
        return notFound();
      const role = store.getCurrentMemberRole(board.workspaceId);
      // ManageBoard on the source (guests never manage Boards here).
      if (role === "guest") return forbidden();
      const target = body?.spaceId ?? null;
      const denied = targetSpaceDenied(store, board.workspaceId, target);
      if (denied) return denied;
      switch (store.placeBoard(board.id, target, body?.expectedVersion)) {
        case "ok":
          return noContent();
        case "not-found":
          return notFound();
        case "stale":
          return preconditionFailed();
      }
    },
  }),

  // ─── GET /boards/:id/view ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, BoardViewDtoApi>({
    id: "boards.view.get",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.Get",
    } as any,
    method: "GET",
    route: "/boards/:id/views",
    async handle({ params, store }) {
      const b = store.getBoard(params.id);
      if (!b) return notFound("Board not found");
      const v = store.getBoardView(params.id);
      return ok<BoardViewDtoApi>({
        viewMode: v?.viewMode ?? "table",
        config: v?.viewConfig ?? "{}",
        filters: v?.filters ?? "{}",
      });
    },
  }),

  // ─── PUT /boards/:id/view ─────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { viewMode?: string; config?: string; filters?: string },
    void
  >({
    id: "boards.view.save",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardViews.Save",
    } as any,
    method: "PUT",
    route: "/boards/:id/views",
    async handle({ params, body, store }) {
      const b = store.getBoard(params.id);
      if (!b) return notFound("Board not found");
      const data = (body ?? {}) as {
        viewMode?: string;
        config?: string;
        filters?: string;
      };
      store.saveBoardView(
        params.id,
        data.viewMode ?? "table",
        data.config ?? "{}",
        data.filters,
      );
      return ok<void>(undefined);
    },
  }),

  // ─── GET /boards/:boardId/fields ─────────────────────────────────────────

  defineMockOperation<{ boardId: string }, never, unknown[]>({
    id: "boards.columns.list",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/boards/:boardId/fields",
    async handle({ params, store }) {
      const cols = store.getColumns(params.boardId);
      return ok(
        cols.map((c) => ({
          id: c.id,
          boardId: c.boardId,
          name: c.name,
          fieldType: c.fieldType,
          settings: c.settings ? JSON.parse(c.settings) : undefined,
          position: c.position,
          isHidden: c.isHidden ?? false,
        })),
      );
    },
  }),

  // ─── POST /boards/:boardId/fields ────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { name: string; fieldType: string; settings?: string; position?: number },
    string
  >({
    id: "boards.columns.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardFields.Create",
    } as any,
    method: "POST",
    route: "/boards/:boardId/fields",
    async handle({ params, body, store }) {
      const data = body as {
        name: string;
        fieldType: string;
        settings?: string;
        position?: number;
      };
      const col = store.createColumn(params.boardId, {
        name: data.name,
        fieldType: data.fieldType,
        settings: data.settings,
        position: data.position,
      });
      return created<string>(col.id);
    },
  }),

  // ─── PATCH /boards/:boardId/fields/:columnId ─────────────────────────────

  defineMockOperation<
    { boardId: string; columnId: string },
    {
      name?: string;
      fieldType?: string;
      settings?: string;
      isHidden?: boolean;
    },
    void
  >({
    id: "boards.columns.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardFields.Update",
    } as any,
    method: "PATCH",
    route: "/boards/:boardId/fields/:columnId",
    async handle({ params, body, store }) {
      const updated = store.updateColumn(params.columnId, body ?? {});
      if (!updated) return notFound("Column not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /boards/:boardId/fields/:columnId ────────────────────────────

  defineMockOperation<{ boardId: string; columnId: string }, never, void>({
    id: "boards.columns.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardFields.Delete",
    } as any,
    method: "DELETE",
    route: "/boards/:boardId/fields/:columnId",
    async handle({ params, store }) {
      const deleted = store.deleteColumn(params.columnId);
      if (!deleted) return notFound("Column not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /boards/:boardId/fields/:fieldId/move ─────────────────────────

  defineMockOperation<
    { boardId: string; fieldId: string },
    { previousSiblingId?: string | null; nextSiblingId?: string | null },
    void
  >({
    id: "boards.columns.move",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardFields.Move",
    } as any,
    method: "POST",
    route: "/boards/:boardId/fields/:fieldId/move",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        previousSiblingId?: string | null;
        nextSiblingId?: string | null;
      };
      const moved = store.moveColumn(
        params.boardId,
        params.fieldId,
        data.previousSiblingId,
        data.nextSiblingId,
      );
      if (!moved) return notFound("Column not found");
      return ok<void>(undefined);
    },
  }),

  // ─── GET /boards/:boardId/labels ──────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    never,
    { id: string; name: string; color: string }[]
  >({
    id: "boards.labels.list",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Labels.List",
    } as any,
    method: "GET",
    route: "/boards/:boardId/labels",
    async handle({ params, store }) {
      const labels = store.getBoardLabels(params.boardId);
      return ok(
        labels.map((l) => ({
          id: l.id,
          name: l.name,
          color: l.color,
        })),
      );
    },
  }),

  // ─── POST /boards/:boardId/labels ─────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    { name?: string; color: string },
    { id: string; name: string; color: string }
  >({
    id: "boards.labels.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Labels.Create",
    } as any,
    method: "POST",
    route: "/boards/:boardId/labels",
    async handle({ params, body, store }) {
      const data = (body ?? { color: "#1E90FF" }) as {
        name?: string;
        color: string;
      };
      const label = store.createLabel(params.boardId, data);
      return created<{ id: string; name: string; color: string }>({
        id: label.id,
        name: label.name,
        color: label.color,
      });
    },
  }),

  // ─── PATCH /boards/:boardId/labels/:labelId ───────────────────────────────

  defineMockOperation<
    { boardId: string; labelId: string },
    { name?: string; color?: string },
    void
  >({
    id: "boards.labels.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Labels.Update",
    } as any,
    method: "PATCH",
    route: "/boards/:boardId/labels/:labelId",
    async handle({ params, body, store }) {
      const updated = store.updateLabel(params.labelId, body ?? {});
      if (!updated) return notFound("Label not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /boards/:boardId/labels/:labelId ──────────────────────────────

  defineMockOperation<{ boardId: string; labelId: string }, never, void>({
    id: "boards.labels.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Labels.Delete",
    } as any,
    method: "DELETE",
    route: "/boards/:boardId/labels/:labelId",
    async handle({ params, store }) {
      const deleted = store.deleteLabel(params.labelId);
      if (!deleted) return notFound("Label not found");
      return ok<void>(undefined);
    },
  }),
];
