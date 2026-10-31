/**
 * Work Management — Cards, Checklists, and Comments context handlers.
 *
 * Operations:
 *   cards.byList            — GET /lists/:listId/cards
 *   cards.detail            — GET /board-items/:id
 *   cards.create            — POST /lists/:listId/cards (returns string cardId)
 *   cards.update            — PATCH /board-items/:id
 *   cards.delete            — DELETE /board-items/:id
 *   cards.archive           — POST /board-items/:id/archive
 *   cards.duplicate         — POST /board-items/:id/duplicate (returns string cardId)
 *   cards.move              — POST /board-items/:id/move

 *   cards.links             — GET/POST /board-items/:id/links, DELETE .../links/:linkId (gap)
 *   cards.childItems.add    — POST /board-items/:id/child-items
 *   cards.attachments.uploadFile — POST /board-items/:id/attachments/files (Collaboration.Attachments.UploadFile)
 *   cards.attachments.content    — GET /board-items/:id/attachments/:attachmentId/content (Collaboration.Attachments.GetContent)
 *   cards.moveToBoard       — POST /board-items/:id/move-to-board
 *   cards.trash             — GET /boards/:boardId/items/trash
 *   cards.restoreDeleted    — POST /board-items/:id/restore
 *   cards.fieldValues       — PATCH /board-items/:id/values/:fieldId
 *   cards.status            — PATCH /board-items/:id/status
 *   cards.bulkUpdate        — POST /boards/:boardId/items/bulk (one change for several items, all or nothing)
 *   cards.attachments       — GET /board-items/:id/attachments
 *   cards.activity          — GET /board-items/:id/activity
 *   cards.labels.list       — GET /board-items/:id/labels
 *   cards.labels.add        — POST /board-items/:id/labels
 *   cards.labels.remove     — DELETE /board-items/:id/labels/:labelId
 *   checklists.list         — GET /board-items/:cardId/checklists
 *   checklists.create       — POST /board-items/:cardId/checklists (returns string checklistId)
 *   checklists.update       — PATCH /checklists/:id
 *   checklists.delete       — DELETE /checklists/:id
 *   checklistItems.create   — POST /checklists/:id/items (returns string itemId)
 *   checklistItems.update   — PATCH /checklist-items/:id
 *   checklistItems.delete   — DELETE /checklist-items/:id
 *   checklistItems.toggle   — POST /checklist-items/:id/toggle
 *   checklists.move / checklistItems.move — POST .../move (neighbour ids)
 *   comments.list           — GET /board-items/:cardId/comments
 *   comments.create         — POST /board-items/:cardId/comments (returns string commentId)
 *   comments.update         — PATCH /comments/:id
 *   comments.delete         — DELETE /comments/:id
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 */

import { projectComputedValues } from "../../state/mock-computed-values";
import type {
  CardDtoApi,
  ChecklistDtoApi,
  CommentDtoApi,
} from "@notrelix/work-management-core";
import { defineMockOperation } from "../../operations/types";
import type { MockHttpResult } from "../../transport/create-response";
import {
  forbidden,
  noContent,
  notFound,
  ok,
} from "../../transport/create-response";
import { preconditionFailed } from "../workspace/space-target";
import {
  projectCardSettings,
  projectVoteValues,
} from "./item-capabilities.handlers";
import { isoOfMs } from "../../state/clock";
import type { MockStore } from "../../state/mock-store";
import { rollRepeatOnCompletion } from "./item-capabilities.handlers";

/** What a versioned item write answers: the item's version after it. */
const versionOf = (store: MockStore, itemId: string) => ({
  version: store.getCard(itemId)?.version ?? 1,
});
import type { MockAttachmentRecord, MockCardRecord } from "../../state/records";
import {
  assignMember,
  childItemDto,
  unassignMember,
} from "../../state/mock-work-feed";
import type { ChildItemDto } from "../../state/mock-work-feed";
import {
  canCommentOnBoard,
  canReadBoard,
  canWriteBoardItems,
  projectItemMembers,
  projectItemReadValues,
} from "./boards.handlers";

const REACTION_PALETTE: readonly string[] = [
  "👍",
  "❤️",
  "😄",
  "🎉",
  "👀",
  "🚀",
];

/** One summary per emoji in first-chosen order: count, whether `me` chose it, and who. */
function summariseReactions(
  reactions: readonly { emoji: string; userId: string }[],
  me: string,
  store: MockStore,
) {
  const byEmoji = new Map<string, string[]>();
  for (const r of reactions)
    byEmoji.set(r.emoji, [...(byEmoji.get(r.emoji) ?? []), r.userId]);
  return [...byEmoji].map(([emoji, userIds]) => ({
    emoji,
    count: userIds.length,
    reactedByMe: userIds.includes(me),
    users: userIds.map((userId) => ({
      userId,
      name: store.getUser(userId)?.name ?? "User",
    })),
  }));
}

/** The pages linked to each item, per store. */
const pageLinks = new WeakMap<MockStore, Map<string, Set<string>>>();
function linksOf(store: MockStore, itemId: string): Set<string> {
  const all = pageLinks.get(store) ?? new Map<string, Set<string>>();
  pageLinks.set(store, all);
  const links = all.get(itemId) ?? new Set<string>();
  all.set(itemId, links);
  return links;
}

/** 400 with field errors, as the real API answers a validation failure. */
function badRequest(errors: Record<string, string[]>) {
  return {
    status: 400,
    body: { message: "Validation failed", code: "VALIDATION_ERROR", errors },
  };
}

const MOCK_IMAGE_SVG =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 320 120" preserveAspectRatio="xMidYMid slice"><rect width="320" height="120" fill="#DCE9FA"/><g fill="#8DBBF3"><rect x="40" y="50" width="34" height="70"/><rect x="92" y="34" width="34" height="86"/><rect x="144" y="62" width="34" height="58"/><rect x="196" y="22" width="34" height="98"/><rect x="248" y="46" width="34" height="74"/></g><path d="M20 44 L110 28 L160 56 L212 14 L300 34" fill="none" stroke="#1E5FB5" stroke-width="3"/></svg>';

/** The limits and blocked types of `POST .../attachments/files`. */
export const MOCK_MAX_ATTACHMENT_BYTES = 100 * 1024 * 1024;
export const MOCK_MAX_ATTACHMENTS = 50;
const BLOCKED_ATTACHMENT_EXTENSIONS = new Set([
  "exe",
  "msi",
  "dll",
  "bat",
  "cmd",
  "com",
  "scr",
  "ps1",
  "vbs",
  "sh",
  "jar",
  "apk",
  "js",
  "mjs",
  "html",
  "htm",
]);

function isBlockedAttachmentName(name: string): boolean {
  const dot = name.lastIndexOf(".");
  return (
    dot >= 0 &&
    BLOCKED_ATTACHMENT_EXTENSIONS.has(
      name
        .slice(dot + 1)
        .trim()
        .toLowerCase(),
    )
  );
}

/** A problem+json answer of the attachment endpoints (`errorCode` names the reason). */
function attachmentProblem(status: number, errorCode: string, detail: string) {
  return {
    status,
    body: {
      status,
      title: detail,
      detail,
      message: detail,
      errorCode,
      code: errorCode,
    },
  };
}

/** The AttachmentDto of a stored file (`Kind` "File", no public `Url`). */
function attachmentDto(file: MockAttachmentRecord, store: MockStore) {
  return {
    id: file.id,
    resourceId: file.cardId,
    filename: file.filename,
    url: file.url ?? "",
    kind: file.url ? "Link" : "File",
    sizeBytes: file.sizeBytes,
    contentType: file.contentType,
    source: file.url ? "link" : "upload",
    uploadedBy: file.uploadedBy,
    uploadedByName: store.getUser(file.uploadedBy)?.name ?? null,
    createdAt: file.createdAt,
  };
}

/** The real write endpoints require an `Idempotency-Key` header (else 400). */
function missingIdempotencyKey(request: { headers: Headers }) {
  return request.headers.get("Idempotency-Key")
    ? null
    : badRequest({ "Idempotency-Key": ["The header is required."] });
}

/**
 * The item detail: the hand-written fields plus the generated item read
 * values (`name`, key, Status/priority option ids, dates, child progress).
 */
function projectCardDto(c: MockCardRecord, store: MockStore): CardDtoApi {
  const board = store.getBoard(c.boardId);
  const labels = store.getCardLabels(c.id).map((l) => ({
    labelId: l.id,
    name: l.name ?? null,
    color: l.color,
  }));
  const checklists = store.getCardChecklists(c.id).map((chk) => ({
    id: chk.id,
    title: chk.title,
    position: chk.position,
    items: chk.items.map((item, idx) => ({
      id: item.id,
      checklistId: item.checklistId,
      title: item.title,
      isChecked: item.isChecked,
      dueDate: item.dueDate ?? null,
      assigneeId: item.assigneeId ?? null,
      dueAt: item.dueDate ?? null,
      assigneeUserId: item.assigneeId ?? null,
      position: item.position ?? idx + 1,
    })),
  }));
  const comments = store.getCardComments(c.id);
  const fieldValues = store.getFieldValues(c.id);
  const { completedAt, ...readValues } = projectItemReadValues(c, store);
  const nameOf = (userId: string | null | undefined) =>
    (userId ? store.getUser(userId)?.name : null) ?? null;

  return {
    id: c.id,
    boardId: c.boardId,
    workspaceId: board?.workspaceId ?? "",
    listId: c.listId,
    title: c.title,
    descriptionMd: c.description ?? null,
    linkedPageId: null,
    priority: null,
    status: "todo",
    dueDate: c.dueAt ?? null,
    startDate: c.startedAt ?? null,
    completedAt,
    cover: null,
    ...projectCardSettings(store, c.id),
    position: c.position,
    members: projectItemMembers(c, store),
    labels,
    checklists,
    commentCount: comments.length,
    attachmentCount: 0,
    fieldValues: (() => {
      const merged = {
        ...fieldValues,
        ...projectVoteValues(store, c.id, c.boardId),
      };
      return Object.keys(merged).length > 0 ? merged : null;
    })(),
    createdAt: c.createdAt,
    updatedAt: c.updatedAt ?? null,
    ...{
      computedValues: projectComputedValues(
        store.getColumns(c.boardId),
        fieldValues,
      ),
      name: c.title,
      groupId: c.listId,
      isArchived: c.isArchived === true,
      archivedAt: c.isArchived ? (c.archivedAt ?? null) : null,
      archivedByName: c.isArchived ? nameOf(c.archivedByUserId) : null,
      completedByName: completedAt ? nameOf(c.assigneeUserIds?.[0]) : null,
    },
    ...readValues,
  };
}

/** The status name and category of an item, for a link's chip. */
function describeItemStatus(store: MockStore, card: MockCardRecord) {
  const column = store
    .getColumns(card.boardId)
    .find((candidate) => candidate.fieldType === "status");
  const option = column?.options?.find(
    (candidate) => candidate.id === store.getFieldValues(card.id)[column.id],
  );
  return {
    name: option?.name ?? null,
    category: option?.category ?? null,
  };
}

export const cardsOperations = [
  // ─── GET /lists/:listId/cards ─────────────────────────────────────────────

  defineMockOperation<{ listId: string }, never, CardDtoApi[]>({
    id: "wm.boardItems.byGroup",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.List",
    } as any,
    method: "GET",
    route: "/board-groups/:listId/items",
    async handle({ params, store }) {
      const cards = store.getCards(params.listId);
      return ok<CardDtoApi[]>(cards.map((c) => projectCardDto(c, store)));
    },
  }),

  // ─── GET /board-items/:id ───────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, CardDtoApi>({
    id: "cards.detail",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/board-items/:id",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      return ok<CardDtoApi>(projectCardDto(c, store));
    },
  }),

  // ─── POST /lists/:listId/cards ────────────────────────────────────────────

  defineMockOperation<
    { listId: string },
    { title?: string; position?: number; description?: string },
    string
  >({
    id: "wm.boardItems.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Create",
    } as any,
    method: "POST",
    route: "/boards/:boardId/items",
    async handle({ params: _params, body, store }) {
      const data = (body ?? {}) as {
        groupId: string;
        title?: string;
        position?: number;
        descriptionMd?: string | null;
        startDate?: string | null;
        dueDate?: string | null;
      };
      const list = store.getList(data.groupId || "fallback");
      if (!list) return notFound("List not found");
      const created = store.createCardByListId(data.groupId || "fallback", {
        title: data.title,
        position: data.position,
      });
      // The item's own facts may arrive with the create (description, dates).
      if (data.descriptionMd || data.startDate || data.dueDate) {
        if (data.startDate && data.dueDate && data.dueDate < data.startDate) {
          store.deleteCard(created.id);
          return badRequest({
            dueDate: ["Due date must be after start date."],
          });
        }
        store.updateCard(created.id, {
          ...(data.descriptionMd ? { description: data.descriptionMd } : {}),
          ...(data.startDate ? { startedAt: data.startDate } : {}),
          ...(data.dueDate ? { dueAt: data.dueDate } : {}),
        });
      }
      // BoardItemSlimDto: the client reads the new item's id.
      return ok({
        id: created.id,
        groupId: created.listId,
        name: created.title,
        position: String(created.position),
        memberIds: created.assigneeUserIds ?? [],
        labelIds: [],
        version: 1,
      });
    },
  }),

  // ─── POST /board-items/:itemId/link-page ──────────────────────────────────

  defineMockOperation<{ itemId: string }, { pageId?: string }, void>({
    id: "wm.boardItems.linkPage",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.LinkPage",
    } as any,
    method: "POST",
    route: "/board-items/:itemId/link-page",
    async handle({ params, body, store }) {
      if (!store.getCard(params.itemId)) return notFound("Item not found");
      const page = body?.pageId ? store.getPage(body.pageId) : undefined;
      if (!page || page.trashedAt) return notFound("Page not found");
      linksOf(store, params.itemId).add(page.id);
      return ok<void>(undefined);
    },
  }),

  // ─── GET /board-items/:itemId/linked-pages ────────────────────────────────

  defineMockOperation<{ itemId: string }>({
    id: "wm.boardItems.linkedPages",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.ListLinkedPages",
    },
    method: "GET",
    route: "/board-items/:itemId/linked-pages",
    async handle({ params, store }) {
      if (!store.getCard(params.itemId)) return notFound("Item not found");
      const pages = [...linksOf(store, params.itemId)].flatMap((pageId) => {
        const page = store.getPage(pageId);
        return page && !page.trashedAt
          ? [
              {
                pageId,
                title: page.title ?? "",
                linkedAt: store.getClock().isoNow(),
              },
            ]
          : [];
      });
      return ok({ pages, hiddenCount: 0 });
    },
  }),

  // ─── DELETE /board-items/:itemId/link-page/:pageId ────────────────────────

  defineMockOperation<{ itemId: string; pageId: string }>({
    id: "wm.boardItems.unlinkPage",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.UnlinkPage",
    },
    method: "DELETE",
    route: "/board-items/:itemId/link-page/:pageId",
    async handle({ params, store }) {
      if (!store.getCard(params.itemId)) return notFound("Item not found");
      linksOf(store, params.itemId).delete(params.pageId);
      return ok<void>(undefined);
    },
  }),

  // ─── PATCH /board-items/:id ─────────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    {
      title?: string;
      description?: string;
      descriptionMd?: string;
      listId?: string;
      dueDate?: string | null;
      startDate?: string | null;
    },
    void
  >({
    id: "wm.boardItems.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Update",
    } as any,
    method: "PATCH",
    route: "/board-items/:id",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        title?: string;
        description?: string;
        descriptionMd?: string;
        listId?: string;
        dueDate?: string | null;
        startDate?: string | null;
        clearDueDate?: boolean;
        clearStartDate?: boolean;
      };
      const description =
        data.descriptionMd !== undefined
          ? data.descriptionMd
          : data.description;
      // Only the fields the request names change: a spread of `undefined`
      // would wipe the title of a date-only patch.
      const updated = store.updateCard(params.id, {
        ...(data.title !== undefined ? { title: data.title } : {}),
        ...(description !== undefined ? { description } : {}),
        ...(data.listId !== undefined ? { listId: data.listId } : {}),
        // The Calendar moves an item by its dates (null clears one).
        ...(data.dueDate !== undefined ? { dueAt: data.dueDate } : {}),
        ...(data.startDate !== undefined ? { startedAt: data.startDate } : {}),
        ...(data.clearDueDate ? { dueAt: null } : {}),
        ...(data.clearStartDate ? { startedAt: null } : {}),
      });
      if (!updated) return notFound("Card not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /board-items/:id ────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "wm.boardItems.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Delete",
    } as any,
    method: "DELETE",
    route: "/board-items/:id",
    async handle({ params, store }) {
      const deleted = store.deleteCard(params.id);
      if (!deleted) return notFound("Card not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /board-items/:id/archive ──────────────────────────────────────────────

  defineMockOperation<{ id: string }, { expectedVersion?: number }, void>({
    id: "wm.boardItems.archive",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Archive",
    } as any,
    method: "POST",
    route: "/board-items/:id/archive",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const expectedVersion = body?.expectedVersion;
      if (!expectedVersion || expectedVersion <= 0) {
        return badRequest({
          expectedVersion: ["ExpectedVersion must be a positive value."],
        });
      }
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      // An already archived item is a no-op whatever the version.
      if (!item.isArchived && (item.version ?? 1) !== expectedVersion)
        return preconditionFailed();
      store.archiveCard(params.id);
      return ok(versionOf(store, params.id));
    },
  }),

  // ─── POST /board-items/:id/duplicate ────────────────────────────────────────────

  defineMockOperation<{ id: string }, { name?: string } | undefined, void>({
    id: "wm.boardItems.duplicate",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Duplicate",
    } as any,
    method: "POST",
    route: "/board-items/:id/duplicate",
    async handle({ params, body, store }) {
      const duplicated = store.duplicateCard(params.id);
      if (!duplicated) return notFound("Card not found");
      if (body?.name) store.updateCard(duplicated.id, { title: body.name });
      return ok<void>(undefined);
    },
  }),

  // ─── POST /board-items/:id/move ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, { listId?: string; position?: number }>({
    id: "wm.boardItems.move",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Move",
    } as any,
    method: "POST",
    route: "/board-items/:id/move",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const data = (body ?? {}) as {
        groupId?: string;
        expectedVersion?: number;
        previousSiblingId?: string | null;
        nextSiblingId?: string | null;
      };
      if (!data.groupId)
        return badRequest({ groupId: ["The group is required."] });
      if (!data.expectedVersion || data.expectedVersion <= 0) {
        return badRequest({
          expectedVersion: ["ExpectedVersion must be a positive value."],
        });
      }
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      // MoveItem: editors and managers only.
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const moved = store.moveItem(params.id, data.groupId, {
        expectedVersion: data.expectedVersion,
        previousSiblingId: data.previousSiblingId ?? null,
        nextSiblingId: data.nextSiblingId ?? null,
      });
      switch (moved.result) {
        case "not-found":
          return notFound("Card not found");
        case "invalid":
          return badRequest({
            groupId: ["Not a group or neighbour of this board."],
          });
        case "stale":
          return preconditionFailed();
        case "ok":
          // BoardItemSlimDto with the new version.
          return ok({
            id: moved.card.id,
            groupId: moved.card.listId,
            name: moved.card.title,
            position: String(moved.card.position),
            memberIds: moved.card.assigneeUserIds ?? [],
            labelIds: [],
            version: moved.version,
          });
      }
    },
  }),

  // ─── POST /board-items/:id/move-to-board ────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { boardId?: string },
    {
      boardId: string;
      groupId: string;
      itemKey: string;
      droppedFields: string[];
      droppedLabels: number;
      droppedAssignees: number;
    }
  >({
    id: "wm.boardItems.moveToBoard",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.MoveToBoard",
    },
    method: "POST",
    route: "/board-items/:id/move-to-board",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const targetBoardId = (body ?? {}).boardId;
      if (!targetBoardId)
        return badRequest({ boardId: ["The target board is required."] });
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      // Editors and managers of both boards.
      if (
        !canWriteBoardItems(item.boardId, store) ||
        !canWriteBoardItems(targetBoardId, store)
      )
        return forbidden();
      const moved = store.moveItemToBoard(params.id, targetBoardId);
      if (moved.result === "not-found") return notFound("Card not found");
      if (moved.result !== "ok")
        return badRequest({ boardId: ["Not another board with groups."] });
      return ok({
        boardId: targetBoardId,
        groupId: moved.card.listId,
        itemKey: moved.card.itemKey ?? "",
        droppedFields: [],
        droppedLabels: 0,
        droppedAssignees: 0,
      });
    },
  }),

  // ─── GET /boards/:boardId/items/trash ───────────────────────────────────────────

  defineMockOperation<
    { boardId: string },
    never,
    {
      id: string;
      name: string;
      itemKey: string | null;
      groupId: string;
      deletedAt: string;
      deletedBy: string | null;
      restorableUntil: string;
    }[]
  >({
    id: "wm.boardItems.trash",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Trash",
    },
    method: "GET",
    route: "/boards/:boardId/items/trash",
    async handle({ params, store }) {
      if (!canWriteBoardItems(params.boardId, store)) return forbidden();
      return ok(
        store
          .getTrashedCards(params.boardId)
          .map(({ card, deletedAt, deletedBy }) => ({
            id: card.id,
            name: card.title,
            itemKey: card.itemKey ?? null,
            groupId: card.listId,
            deletedAt,
            deletedBy,
            restorableUntil: isoOfMs(
              Date.parse(deletedAt) + 30 * 24 * 60 * 60 * 1000,
            ),
          })),
      );
    },
  }),

  // ─── POST /board-items/:id/restore ──────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "wm.boardItems.restoreDeleted",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Restore",
    },
    method: "POST",
    route: "/board-items/:id/restore",
    async handle({ params, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const restored = store.restoreDeletedCard(params.id);
      if (!restored) return notFound("Card not found");
      return noContent();
    },
  }),

  // ─── PATCH /board-items/:id/field-values ────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { fieldDefinitionId: string; value: unknown },
    void
  >({
    id: "wm.boardItems.fieldValues.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.UpdateFieldValue",
    } as any,
    method: "PATCH",
    route: "/board-items/:id/values/:fieldId",
    async handle({ params, body, store }) {
      const data = body as { value: unknown };
      const target = store.getCard(params.id)?.boardId
        ? store
            .getColumns(store.getCard(params.id)!.boardId)
            .find((column) => column.id === (params as any).fieldId)
        : undefined;
      // A Formula is evaluated by the backend and never written.
      if (target?.fieldType === "formula")
        return badRequest({ value: ["Cannot write a computed field."] });
      const updated = store.updateFieldValue(
        params.id,
        (params as any).fieldId,
        data.value,
      );
      if (!updated) return notFound("Card not found");
      return ok(versionOf(store, params.id));
    },
  }),

  // ─── PATCH /board-items/:id/status ──────────────────────────────────────────────

  defineMockOperation<{ id: string }, { status?: string | null }, void>({
    id: "wm.boardItems.status.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.UpdateStatus",
    } as any,
    method: "PATCH",
    route: "/board-items/:id/status",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      // UpdateItem: editors and managers only.
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const status = (body as { status?: string | null } | undefined)?.status;
      if (!status) return badRequest({ status: ["The status is required."] });
      // A repeating item moved to a done status rolls to its next occurrence and keeps its status.
      if (rollRepeatOnCompletion(store, params.id, status))
        return ok(versionOf(store, params.id));
      const result = store.updateItemStatus(params.id, status);
      if (result === "not-found") return notFound("Card not found");
      if (result === "invalid") {
        return badRequest({
          status: ["Not an option of the board's Status field."],
        });
      }
      // The real endpoint answers the item's new version.
      return ok(versionOf(store, params.id));
    },
  }),

  // ─── POST /board-items/:itemId/assignees ───────────────────────────────────────

  defineMockOperation<{ id: string }, { userId?: string }, void>({
    id: "wm.boardItems.assignMember",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.AssignMember",
    },
    method: "POST",
    route: "/board-items/:id/assignees",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      if (!body?.userId) return badRequest({ userId: ["Choose a member."] });
      const result = assignMember(store, params.id, body.userId);
      if (result === "invalid") {
        return badRequest({ userId: ["That person is not a member."] });
      }
      return ok(versionOf(store, params.id));
    },
  }),

  // ─── DELETE /board-items/:itemId/assignees/:userId ─────────────────────────────

  defineMockOperation<{ id: string; userId: string }, never, void>({
    id: "wm.boardItems.unassignMember",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.UnassignMember",
    },
    method: "DELETE",
    route: "/board-items/:id/assignees/:userId",
    async handle({ params, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      unassignMember(store, params.id, params.userId);
      return ok(versionOf(store, params.id));
    },
  }),

  // ─── POST /boards/:boardId/items/bulk ──────────────────────────────────────────

  // One change for several items of a board, all or nothing: an unknown or
  // foreign item, or a refused status, writes nothing.
  defineMockOperation<
    { boardId: string },
    {
      operation?: string;
      itemIds?: string[];
      status?: string | null;
      dueDate?: string | null;
      assigneeUserId?: string | null;
      priority?: string | null;
    },
    { updated: number }
  >({
    id: "wm.boardItems.bulkUpdate",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.BulkUpdate",
    },
    method: "POST",
    route: "/boards/:boardId/items/bulk",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      if (!canWriteBoardItems(params.boardId, store)) return forbidden();
      const operation = body?.operation ?? "";
      const ids = [...new Set(body?.itemIds ?? [])];
      if (
        ![
          "set-status",
          "set-due-date",
          "clear-due-date",
          "assign",
          "unassign-all",
          "archive",
          "set-priority",
          "delete",
        ].includes(operation)
      ) {
        return badRequest({ operation: ["Choose a change to apply."] });
      }
      if (ids.length === 0 || ids.length > 200) {
        return badRequest({ itemIds: ["Choose between 1 and 200 items."] });
      }
      if (ids.some((id) => store.getCard(id)?.boardId !== params.boardId)) {
        return notFound("Card not found");
      }
      if (operation === "set-status") {
        if (!body?.status) {
          return badRequest({ status: ["The status is required."] });
        }
        const first = store.updateItemStatus(ids[0]!, body.status);
        if (first === "invalid") {
          return badRequest({
            status: ["Not an option of the board's Status field."],
          });
        }
        for (const id of ids.slice(1)) store.updateItemStatus(id, body.status);
      } else if (operation === "assign") {
        if (!body?.assigneeUserId) {
          return badRequest({ assigneeUserId: ["Choose a member."] });
        }
        for (const id of ids) {
          const others = store.getCard(id)?.assigneeUserIds ?? [];
          for (const other of others) {
            if (other !== body.assigneeUserId) unassignMember(store, id, other);
          }
          if (assignMember(store, id, body.assigneeUserId) === "invalid") {
            return badRequest({
              assigneeUserId: ["That person is not a member."],
            });
          }
        }
      } else if (operation === "unassign-all") {
        for (const id of ids) {
          for (const other of store.getCard(id)?.assigneeUserIds ?? []) {
            unassignMember(store, id, other);
          }
        }
      } else if (operation === "archive") {
        for (const id of ids) store.archiveCard(id);
      } else if (operation === "delete") {
        for (const id of ids) store.deleteCard(id);
      } else if (operation === "set-priority") {
        const field = store
          .getColumns(params.boardId)
          .find(
            (column) =>
              column.fieldType === "select" &&
              /^(priority|ưu tiên|uu tien)$/i.test((column.name ?? "").trim()),
          );
        if (!field) {
          return badRequest({
            priority: ["This board has no Priority field."],
          });
        }
        if (!body?.priority) {
          return badRequest({ priority: ["The priority is required."] });
        }
        for (const id of ids)
          store.updateFieldValue(id, field.id, body.priority);
      } else {
        const dueAt =
          operation === "clear-due-date" ? null : (body?.dueDate ?? null);
        for (const id of ids) store.updateCard(id, { dueAt });
      }
      return ok({ updated: ids.length });
    },
  }),

  // ─── PATCH /board-items/:id/due-date ───────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { dueDate?: string | null; startDate?: string | null },
    void
  >({
    id: "wm.boardItems.setDueDate",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.SetDueDate",
    },
    method: "PATCH",
    route: "/board-items/:id/due-date",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      store.updateCard(params.id, { dueAt: body?.dueDate ?? null });
      return noContent();
    },
  }),

  // ─── GET /board-items/:id/attachments ───────────────────────────────────────────

  defineMockOperation<{ id: string }, never, unknown[]>({
    id: "cards.attachments",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/board-items/:id/attachments",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      return ok(
        store.getAttachments(params.id).map((file) => ({
          id: file.id,
          resourceId: file.cardId,
          filename: file.filename,
          // A stored file has no public address: it downloads through the
          // content route (`GET .../attachments/:attachmentId/content`).
          url: file.url ?? "",
          kind: file.url ? "Link" : "File",
          sizeBytes: file.sizeBytes,
          contentType: file.contentType,
          source: file.url ? "link" : "upload",
          uploadedBy: file.uploadedBy,
          uploadedByName: store.getUser(file.uploadedBy)?.name ?? null,
          createdAt: file.createdAt,
        })),
      );
    },
  }),

  // ─── POST /board-items/:id/attachments ──────────────────────────────────────────
  // Collaboration.Attachments.Create: attaches a web link (name and http(s) address).

  defineMockOperation<
    { id: string },
    { filename?: string | null; url?: string | null },
    void
  >({
    id: "cards.attachments.createLink",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Attachments.Create",
    },
    method: "POST",
    route: "/board-items/:id/attachments",
    async handle({ params, body, store, request }) {
      const key = request.headers.get("Idempotency-Key");
      if (!key)
        return attachmentProblem(
          400,
          "validation.failed",
          "Idempotency-Key is required.",
        );
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      if (store.getUploadByKey(params.id, key)) return noContent();
      const name = body?.filename?.trim();
      const url = body?.url?.trim();
      if (!name || !url || !/^https?:\/\/\S+$/i.test(url))
        return attachmentProblem(
          400,
          "validation.failed",
          "A name and an http or https address are required.",
        );
      if (store.getAttachments(params.id).length >= MOCK_MAX_ATTACHMENTS)
        return attachmentProblem(
          409,
          "attachment.limit_exceeded",
          "The item has the most attachments it can hold.",
        );
      const record = store.addAttachment(params.id, {
        filename: name,
        sizeBytes: 0,
        contentType: "text/uri-list",
        url,
      });
      if (!record) return notFound("Card not found");
      store.rememberUpload(params.id, key, record);
      return noContent();
    },
  }),

  // ─── POST /board-items/:id/attachments/files ────────────────────────────────────
  // Collaboration.Attachments.UploadFile: multipart, part `file`. The mock keeps
  // no bytes, only the metadata and the limits of the real endpoint.

  defineMockOperation<{ id: string }, never, unknown>({
    id: "cards.attachments.uploadFile",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Attachments.UploadFile",
    },
    method: "POST",
    route: "/board-items/:id/attachments/files",
    async handle({ params, store, request }): Promise<MockHttpResult<unknown>> {
      const key = request.headers.get("Idempotency-Key");
      if (!key)
        return attachmentProblem(
          400,
          "validation.failed",
          "Idempotency-Key is required.",
        );
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const replay = store.getUploadByKey(params.id, key);
      if (replay)
        return {
          status: 201,
          headers: { "Idempotency-Replayed": "true" },
          body: attachmentDto(replay, store),
        };
      const part = request.formData?.get("file");
      if (!part || typeof part === "string" || !part.name)
        return attachmentProblem(
          400,
          "validation.failed",
          "A file part with a file name is required.",
        );
      if (part.size === 0)
        return attachmentProblem(
          400,
          "validation.failed",
          "The file is empty.",
        );
      if (part.size > MOCK_MAX_ATTACHMENT_BYTES)
        return attachmentProblem(
          413,
          "attachment.payload_too_large",
          "The file is over the size limit.",
        );
      if (isBlockedAttachmentName(part.name))
        return attachmentProblem(
          415,
          "attachment.unsupported_media_type",
          "This type of file is not allowed.",
        );
      if (store.getAttachments(params.id).length >= MOCK_MAX_ATTACHMENTS)
        return attachmentProblem(
          409,
          "attachment.limit_exceeded",
          "The item has the most attachments it can hold.",
        );
      const record = store.addAttachment(params.id, {
        filename: part.name,
        sizeBytes: part.size,
        contentType: part.type || "application/octet-stream",
      });
      if (!record) return notFound("Card not found");
      store.rememberUpload(params.id, key, record);
      return {
        status: 201,
        headers: {
          Location: `/api/v1/board-items/${params.id}/attachments/${record.id}/content`,
        },
        body: attachmentDto(record, store),
      };
    },
  }),

  // ─── DELETE /board-items/:id/attachments/:attachmentId ──────────────────────────
  // Collaboration.Attachments.Delete: the item's editors remove a stored file.

  defineMockOperation<{ id: string; attachmentId: string }, never, unknown>({
    id: "cards.attachments.delete",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Attachments.Delete",
    },
    method: "DELETE",
    route: "/board-items/:id/attachments/:attachmentId",
    async handle({ params, store }) {
      const item = store.getCard(params.id);
      if (!item || !canReadBoard(item.boardId, store))
        return notFound("Attachment not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      if (!store.removeAttachment(params.id, params.attachmentId))
        return notFound("Attachment not found");
      return noContent();
    },
  }),

  // ─── GET /board-items/:id/attachments/:attachmentId/content ─────────────────────
  // Collaboration.Attachments.GetContent: a small stub body (no stored bytes).

  defineMockOperation<{ id: string; attachmentId: string }, never, unknown>({
    id: "cards.attachments.content",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Attachments.GetContent",
    },
    method: "GET",
    route: "/board-items/:id/attachments/:attachmentId/content",
    async handle({ params, store }) {
      const item = store.getCard(params.id);
      if (!item || !canReadBoard(item.boardId, store))
        return notFound("Attachment not found");
      const file = store.getAttachment(params.id, params.attachmentId);
      if (!file) return notFound("Attachment not found");
      // An image stub is a small drawing, so a cover made from it has something to show.
      const isImage = file.contentType.startsWith("image/");
      return {
        status: 200,
        headers: {
          "Content-Type": isImage
            ? "image/svg+xml"
            : "text/plain; charset=utf-8",
          "Content-Disposition": isImage ? "inline" : "attachment",
          "X-Content-Type-Options": "nosniff",
          "Cache-Control": "private, no-store",
        },
        rawBody: isImage ? MOCK_IMAGE_SVG : `Tệp mẫu của mock: ${file.filename}`,
      };
    },
  }),

  // ─── Links between items ────────────────────────────────────────────────────────
  // Gap CTR-GAP-WM-ITEM-DETAIL.

  defineMockOperation<{ id: string }, never, unknown>({
    id: "cards.links.list",
    contract: { kind: "gap", gapId: "CTR-GAP-WM-ITEM-DETAIL" },
    method: "GET",
    route: "/board-items/:id/links",
    async handle({ params, store }) {
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      return ok({
        links: store.getItemLinks(params.id).map((link) => {
          const status = describeItemStatus(store, link.card);
          return {
            id: link.id,
            relation: link.relation,
            item: {
              id: link.card.id,
              key: link.card.itemKey ?? null,
              title: link.card.title,
              statusName: status.name,
              statusCategory: status.category,
            },
          };
        }),
      });
    },
  }),
  defineMockOperation<
    { id: string },
    { itemId?: string; relation?: string },
    void
  >({
    id: "cards.links.add",
    contract: { kind: "gap", gapId: "CTR-GAP-WM-ITEM-DETAIL" },
    method: "POST",
    route: "/board-items/:id/links",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const relation = body?.relation;
      if (
        !body?.itemId ||
        (relation !== "blocked-by" &&
          relation !== "blocks" &&
          relation !== "related")
      )
        return badRequest({ itemId: ["Pick an item and a relation."] });
      const result = store.addItemLink(params.id, body.itemId, relation);
      if (result === "not-found") return notFound("Card not found");
      if (result === "invalid")
        return badRequest({ itemId: ["Not another item of this board."] });
      return noContent();
    },
  }),
  defineMockOperation<{ id: string; linkId: string }, never, void>({
    id: "cards.links.remove",
    contract: { kind: "gap", gapId: "CTR-GAP-WM-ITEM-DETAIL" },
    method: "DELETE",
    route: "/board-items/:id/links/:linkId",
    async handle({ params, store }) {
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      return store.removeItemLink(params.linkId)
        ? noContent()
        : notFound("Link not found");
    },
  }),

  // ─── POST /board-items/:id/child-items ──────────────────────────────────────────
  // Adds a child item (sub-item) under an item.

  defineMockOperation<{ id: string }, { title?: string }, ChildItemDto>({
    id: "cards.childItems.add",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.CreateChild",
    },
    method: "POST",
    route: "/board-items/:id/child-items",
    async handle({ params, body, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      const title = body?.title?.trim();
      if (!title) return badRequest({ title: ["The title is required."] });
      const child = store.createChildItem(params.id, title);
      return child
        ? ok(childItemDto(store, child))
        : badRequest({ title: ["A child item cannot have child items."] });
    },
  }),

  // ─── GET /board-items/:id/activity ──────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    never,
    { data: unknown[]; total: number; page: number; pageSize: number }
  >({
    id: "cards.activity",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/board-items/:id/activity",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      const data = store.getActivity(params.id).map((entry) => ({
        id: entry.id,
        actorName: entry.actorId
          ? (store.getUser(entry.actorId)?.name ?? null)
          : null,
        action: entry.action,
        resourceTitle: null,
        createdAt: entry.createdAt,
        // Gap fields (CTR-GAP-WM-ITEM-DETAIL): the history as structured lines.
        kind: entry.kind ?? null,
        automationName: entry.automationName ?? null,
        verb: entry.verb ?? null,
        target: entry.target ?? null,
        notified: entry.notified ?? null,
        detail: entry.detail ?? null,
        compact: entry.compact ?? null,
        // The real read's fields: the stable type and the language-neutral change.
        type: entry.type ?? null,
        change: entry.change ?? null,
        actorAvatarUrl: null,
      }));
      return ok({ data, total: data.length, page: 1, pageSize: data.length });
    },
  }),

  // ─── GET /board-items/:id/labels ────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    never,
    { id: string; name: string; color: string }[]
  >({
    id: "cards.labels.list",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/board-items/:id/labels",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      const labels = store.getCardLabels(params.id);
      return ok(
        labels.map((l) => ({ id: l.id, name: l.name, color: l.color })),
      );
    },
  }),

  // ─── POST /board-items/:id/labels ───────────────────────────────────────────────

  defineMockOperation<{ id: string }, { labelId: string }, void>({
    id: "wm.boardItems.labels.add",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.AddLabel",
    } as any,
    method: "POST",
    route: "/board-items/:id/labels",
    async handle({ params, body, store }) {
      const data = body as { labelId: string };
      const added = store.addLabelToCard(params.id, data.labelId);
      if (!added) return notFound("Card or Label not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /board-items/:id/labels/:labelId ────────────────────────────────────

  defineMockOperation<{ id: string; labelId: string }, never, void>({
    id: "wm.boardItems.labels.remove",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.RemoveLabel",
    } as any,
    method: "DELETE",
    route: "/board-items/:id/labels/:labelId",
    async handle({ params, store }) {
      const removed = store.removeLabelFromCard(params.id, params.labelId);
      if (!removed) return notFound("Card not found");
      return ok<void>(undefined);
    },
  }),

  // ─── GET /board-items/:cardId/checklists ────────────────────────────────────────

  defineMockOperation<{ cardId: string }, never, ChecklistDtoApi[]>({
    id: "checklists.list",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.List",
    } as any,
    method: "GET",
    route: "/board-items/:cardId/checklists",
    async handle({ params, store }) {
      const c = store.getCard(params.cardId);
      if (!c) return notFound("Card not found");
      const checklists = store.getCardChecklists(params.cardId);
      return ok<ChecklistDtoApi[]>(
        checklists.map((chk) => ({
          id: chk.id,
          title: chk.title,
          position: chk.position,
          items: chk.items.map((item, idx) => ({
            id: item.id,
            checklistId: item.checklistId,
            title: item.title,
            isChecked: item.isChecked,
            dueDate: item.dueDate ?? null,
            assigneeId: item.assigneeId ?? null,
            dueAt: item.dueDate ?? null,
            assigneeUserId: item.assigneeId ?? null,
            position: item.position ?? idx + 1,
          })),
        })),
      );
    },
  }),

  // ─── POST /board-items/:cardId/checklists ───────────────────────────────────────

  defineMockOperation<{ cardId: string }, { title: string }, void>({
    id: "checklists.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.Create",
    } as any,
    method: "POST",
    route: "/board-items/:cardId/checklists",
    async handle({ params, body, store }) {
      const data = body as { title: string };
      const card = store.getCard(params.cardId);
      if (!card) return notFound("Card not found");
      const _chk = store.createChecklist(params.cardId, data.title);
      return ok<void>(undefined);
    },
  }),

  // ─── PATCH /checklists/:id ────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { title?: string; position?: number },
    void
  >({
    id: "checklists.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.Update",
    } as any,
    method: "PATCH",
    route: "/checklists/:id",
    async handle({ params, body, store }) {
      const updated = store.updateChecklist(params.id, body ?? {});
      if (!updated) return notFound("Checklist not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /checklists/:id ───────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "checklists.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.Delete",
    } as any,
    method: "DELETE",
    route: "/checklists/:id",
    async handle({ params, store }) {
      const deleted = store.deleteChecklist(params.id);
      if (!deleted) return notFound("Checklist not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /checklists/:id/items ───────────────────────────────────────────

  defineMockOperation<{ id: string }, { title: string }, void>({
    id: "checklistItems.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.CreateItemByChecklist",
    } as any,
    method: "POST",
    route: "/checklists/:id/items",
    async handle({ params, body, store }) {
      const data = body as { title: string };
      const chk = store.getChecklist(params.id);
      if (!chk) return notFound("Checklist not found");
      const _item = store.createChecklistItem(params.id, data.title);
      return ok<void>(undefined);
    },
  }),

  // ─── PATCH /checklist-items/:id ───────────────────────────────────────────

  defineMockOperation<
    { id: string },
    {
      title?: string;
      isChecked?: boolean;
      dueDate?: string | null;
      assigneeId?: string | null;
    },
    void
  >({
    id: "checklistItems.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.UpdateItem",
    } as any,
    method: "PATCH",
    route: "/checklist-items/:id",
    async handle({ params, body, store }) {
      const updated = store.updateChecklistItem(params.id, body ?? {});
      if (!updated) return notFound("ChecklistItem not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /checklist-items/:id ──────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "checklistItems.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.DeleteItem",
    } as any,
    method: "DELETE",
    route: "/checklist-items/:id",
    async handle({ params, store }) {
      const deleted = store.deleteChecklistItem(params.id);
      if (!deleted) return notFound("ChecklistItem not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /checklist-items/:id/toggle ─────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "checklistItems.toggle",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.ToggleItem",
    } as any,
    method: "POST",
    route: "/checklist-items/:id/toggle",
    async handle({ params, store }) {
      if (!store.toggleChecklistItem(params.id))
        return notFound("ChecklistItem not found");
      return ok<void>(undefined);
    },
  }),

  // ─── PUT /checklist-items/:id/assignee ────────────────────────────────────
  // WorkManagement.Checklists.SetItemAssignee: a person of the workspace, or nobody.

  defineMockOperation<
    { id: string },
    { assigneeUserId?: string | null; expectedVersion?: number },
    void
  >({
    id: "checklistItems.setAssignee",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.SetItemAssignee",
    } as any,
    method: "PUT",
    route: "/checklist-items/:id/assignee",
    async handle({ params, body, store, request }) {
      const missing = missingIdempotencyKey(request);
      if (missing) return missing;
      const assignee = body?.assigneeUserId ?? null;
      if (assignee !== null && !store.getUser(assignee))
        return badRequest({ assigneeUserId: ["Unknown person."] });
      if (!store.updateChecklistItem(params.id, { assigneeId: assignee }))
        return notFound("ChecklistItem not found");
      return noContent();
    },
  }),

  // ─── PUT /checklist-items/:id/due ─────────────────────────────────────────
  // WorkManagement.Checklists.SetItemDue: the item's own due time, or none.

  defineMockOperation<
    { id: string },
    { dueAt?: string | null; expectedVersion?: number },
    void
  >({
    id: "checklistItems.setDue",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.SetItemDue",
    } as any,
    method: "PUT",
    route: "/checklist-items/:id/due",
    async handle({ params, body, store, request }) {
      const missing = missingIdempotencyKey(request);
      if (missing) return missing;
      const due = body?.dueAt ?? null;
      if (due !== null && Number.isNaN(Date.parse(due)))
        return badRequest({ dueAt: ["Not a date and time."] });
      if (!store.updateChecklistItem(params.id, { dueDate: due }))
        return notFound("ChecklistItem not found");
      return noContent();
    },
  }),

  // ─── POST /checklists/:id/move ────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { previousSiblingId?: string | null; nextSiblingId?: string | null },
    void
  >({
    id: "checklists.move",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.Move",
    } as any,
    method: "POST",
    route: "/checklists/:id/move",
    async handle({ params, body, store }) {
      const placement = body ?? {};
      const moved = store.moveChecklist(
        params.id,
        placement.previousSiblingId ?? null,
        placement.nextSiblingId ?? null,
      );
      if (!moved) return notFound("Checklist not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /checklist-items/:id/move ───────────────────────────────────────

  defineMockOperation<
    { id: string },
    { previousSiblingId?: string | null; nextSiblingId?: string | null },
    void
  >({
    id: "checklistItems.move",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Checklists.MoveItem",
    } as any,
    method: "POST",
    route: "/checklist-items/:id/move",
    async handle({ params, body, store }) {
      const placement = body ?? {};
      const moved = store.moveChecklistItem(
        params.id,
        placement.previousSiblingId ?? null,
        placement.nextSiblingId ?? null,
      );
      if (!moved) return notFound("ChecklistItem not found");
      return ok<void>(undefined);
    },
  }),

  // ─── GET /board-items/:cardId/comments ──────────────────────────────────────────

  defineMockOperation<{ cardId: string }, never, CommentDtoApi[]>({
    id: "comments.list",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Comments.GetBoardItemComments",
    } as any,
    method: "GET",
    route: "/board-items/:cardId/comments",
    async handle({ params, store }) {
      const c = store.getCard(params.cardId);
      if (!c) return notFound("Card not found");
      const comments = store.getCardComments(params.cardId);
      const me = store.getCurrentUser().id;
      return ok<CommentDtoApi[]>(
        comments.map((cmt) => {
          const user = store.getUser(cmt.userId);
          return {
            id: cmt.id,
            cardId: cmt.cardId,
            userId: cmt.userId,
            userName: user?.name ?? "User",
            userAvatar: user?.avatarUrl ?? null,
            contentMd: cmt.contentMd,
            createdAt: cmt.createdAt,
            updatedAt: cmt.updatedAt ?? null,
            isEdited: Boolean(cmt.updatedAt),
            attachmentIds: cmt.attachmentIds ?? [],
            parentCommentId: cmt.parentId ?? null,
            reactions: summariseReactions(cmt.reactions ?? [], me, store),
          };
        }),
      );
    },
  }),

  // ─── POST /board-items/:cardId/comments ─────────────────────────────────────────

  defineMockOperation<
    { cardId: string },
    { contentMd: string; parentCommentId?: string | null },
    void
  >({
    id: "comments.create",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Comments.CreateBoardItemComment",
    } as any,
    method: "POST",
    route: "/board-items/:cardId/comments",
    async handle({ params, body, store }) {
      const data = body as { contentMd: string; parentCommentId?: string };
      const card = store.getCard(params.cardId);
      if (!card) return notFound("Card not found");
      const user = store.getCurrentUser();
      const _cmt = store.createCardComment(
        params.cardId,
        user.id,
        data.contentMd,
        data.parentCommentId ?? undefined,
      );
      return ok<void>(undefined);
    },
  }),

  // ─── PATCH /comments/:id ──────────────────────────────────────────────────

  defineMockOperation<{ id: string }, { contentMd: string }, void>({
    id: "comments.update",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Comments.Update",
    } as any,
    method: "PATCH",
    route: "/comments/:id",
    async handle({ params, body, store }) {
      const data = body as { contentMd: string };
      let updated = store.updateCardComment(params.id, data.contentMd);
      if (!updated)
        updated = store.updatePageComment(params.id, data.contentMd) !== null;
      if (!updated) return notFound("Comment not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /comments/:id ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "comments.delete",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Comments.Delete",
    } as any,
    method: "DELETE",
    route: "/comments/:id",
    async handle({ params, store }) {
      let deleted = store.deleteCardComment(params.id);
      if (!deleted) deleted = store.deletePageComment(params.id);
      if (!deleted) return notFound("Comment not found");
      return ok<void>(undefined);
    },
  }),

  // ─── PUT / DELETE /board-items/:cardId/comments/:commentId/reactions/:emoji ───

  ...(["PUT", "DELETE"] as const).map((method) =>
    defineMockOperation<
      { cardId: string; commentId: string; emoji: string },
      never,
      void
    >({
      id:
        method === "PUT" ? "comments.reaction.add" : "comments.reaction.remove",
      contract: {
        kind: "openapi",
        operationId:
          method === "PUT"
            ? "Collaboration.Comments.AddReaction"
            : "Collaboration.Comments.RemoveReaction",
      } as any,
      method,
      route: "/board-items/:cardId/comments/:commentId/reactions/:emoji",
      async handle({ params, store }) {
        const card = store.getCard(params.cardId);
        if (!card) return notFound("Card not found");
        // Reacting needs what commenting needs: a Commenter or more on the board.
        if (!canCommentOnBoard(card.boardId, store)) return forbidden();
        const emoji = decodeURIComponent(params.emoji);
        if (!REACTION_PALETTE.includes(emoji))
          return badRequest({ emoji: ["Emoji is not in the palette."] });
        const user = store.getCurrentUser();
        const done = store.setCardCommentReaction(
          params.cardId,
          params.commentId,
          user.id,
          emoji,
          method === "PUT",
        );
        if (!done) return notFound("Comment not found");
        return ok<void>(undefined);
      },
    }),
  ),
];
