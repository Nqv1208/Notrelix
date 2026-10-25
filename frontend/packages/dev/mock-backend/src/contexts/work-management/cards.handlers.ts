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
 *   cards.moveToBoard       — POST /board-items/:id/move-to-board (gap)
 *   cards.fieldValues       — PATCH /board-items/:id/values/:fieldId
 *   cards.status            — PATCH /board-items/:id/status
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
 *   comments.list           — GET /board-items/:cardId/comments
 *   comments.create         — POST /board-items/:cardId/comments (returns string commentId)
 *   comments.update         — PATCH /comments/:id
 *   comments.delete         — DELETE /comments/:id
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Work Management split
 */

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
import type { MockStore } from "../../state/mock-store";
import type { MockAttachmentRecord, MockCardRecord } from "../../state/records";
import { childItemDto } from "../../state/mock-work-feed";
import type { ChildItemDto } from "../../state/mock-work-feed";
import {
  canReadBoard,
  canWriteBoardItems,
  projectItemMembers,
  projectItemReadValues,
} from "./boards.handlers";

/** 400 with field errors, as the real API answers a validation failure. */
function badRequest(errors: Record<string, string[]>) {
  return {
    status: 400,
    body: { message: "Validation failed", code: "VALIDATION_ERROR", errors },
  };
}

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
    url: "",
    kind: "File",
    sizeBytes: file.sizeBytes,
    contentType: file.contentType,
    source: "upload",
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
    position: c.position,
    members: projectItemMembers(c, store),
    labels,
    checklists,
    commentCount: comments.length,
    attachmentCount: 0,
    fieldValues: Object.keys(fieldValues).length > 0 ? fieldValues : null,
    createdAt: c.createdAt,
    updatedAt: c.updatedAt ?? null,
    ...{
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
      };
      const list = store.getList(data.groupId || "fallback");
      if (!list) return notFound("List not found");
      const created = store.createCardByListId(data.groupId || "fallback", {
        title: data.title,
        position: data.position,
      });
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

  defineMockOperation<{ id: string }, never, void>({
    id: "wm.boardItems.archive",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Archive",
    } as any,
    method: "POST",
    route: "/board-items/:id/archive",
    async handle({ params, store, request }) {
      const missingKey = missingIdempotencyKey(request);
      if (missingKey) return missingKey;
      const item = store.getCard(params.id);
      if (!item) return notFound("Card not found");
      if (!canWriteBoardItems(item.boardId, store)) return forbidden();
      store.archiveCard(params.id);
      return ok<void>(undefined);
    },
  }),

  // ─── POST /board-items/:id/duplicate ────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "wm.boardItems.duplicate",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.BoardItems.Duplicate",
    } as any,
    method: "POST",
    route: "/board-items/:id/duplicate",
    async handle({ params, store }) {
      const duplicated = store.duplicateCard(params.id);
      if (!duplicated) return notFound("Card not found");
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
  // Gap CTR-GAP-WM-TABLE-BULK: the table's "Chuyển board".

  defineMockOperation<{ id: string }, { boardId?: string }, void>({
    id: "wm.boardItems.moveToBoard",
    contract: { kind: "gap", gapId: "CTR-GAP-WM-TABLE-BULK" },
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
      if (moved.result === "invalid")
        return badRequest({ boardId: ["Not another board with groups."] });
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
      const updated = store.updateFieldValue(
        params.id,
        (params as any).fieldId,
        data.value,
      );
      if (!updated) return notFound("Card not found");
      return ok<void>(undefined);
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
      const result = store.updateItemStatus(params.id, status);
      if (result === "not-found") return notFound("Card not found");
      if (result === "invalid") {
        return badRequest({
          status: ["Not an option of the board's Status field."],
        });
      }
      // The real endpoint answers 204 No Content.
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
          url: "",
          kind: "File",
          sizeBytes: file.sizeBytes,
          contentType: file.contentType,
          source: "upload",
          uploadedBy: file.uploadedBy,
          uploadedByName: store.getUser(file.uploadedBy)?.name ?? null,
          createdAt: file.createdAt,
        })),
      );
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
      return {
        status: 200,
        headers: {
          "Content-Type": "text/plain; charset=utf-8",
          "Content-Disposition": "attachment",
          "X-Content-Type-Options": "nosniff",
          "Cache-Control": "private, no-store",
        },
        rawBody: `Tệp mẫu của mock: ${file.filename}`,
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
          };
        }),
      );
    },
  }),

  // ─── POST /board-items/:cardId/comments ─────────────────────────────────────────

  defineMockOperation<{ cardId: string }, { contentMd: string }, void>({
    id: "comments.create",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Comments.CreateBoardItemComment",
    } as any,
    method: "POST",
    route: "/board-items/:cardId/comments",
    async handle({ params, body, store }) {
      const data = body as { contentMd: string };
      const card = store.getCard(params.cardId);
      if (!card) return notFound("Card not found");
      const user = store.getCurrentUser();
      const _cmt = store.createCardComment(
        params.cardId,
        user.id,
        data.contentMd,
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
];
