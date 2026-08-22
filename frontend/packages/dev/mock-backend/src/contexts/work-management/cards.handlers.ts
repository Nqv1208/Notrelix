/* eslint-disable @typescript-eslint/no-unused-vars */
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
 *   cards.fieldValues       — PATCH /board-items/:id/field-values
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
import { ok, notFound } from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";

function projectCardDto(
  c: {
    id: string;
    boardId: string;
    listId: string;
    title: string;
    description?: string;
    position: number;
    createdAt: string;
    updatedAt: string;
  },
  store: MockStore,
): CardDtoApi {
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
    dueDate: null,
    startDate: null,
    completedAt: null,
    cover: null,
    position: c.position,
    members: [],
    labels,
    checklists,
    commentCount: comments.length,
    attachmentCount: 0,
    fieldValues: Object.keys(fieldValues).length > 0 ? fieldValues : null,
    createdAt: c.createdAt,
    updatedAt: c.updatedAt ?? null,
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
      const _newCard = store.createCardByListId(data.groupId || "fallback", {
        title: data.title,
        position: data.position,
      });
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
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        title?: string;
        description?: string;
        descriptionMd?: string;
        listId?: string;
      };
      const description =
        data.descriptionMd !== undefined
          ? data.descriptionMd
          : data.description;
      const updated = store.updateCard(params.id, {
        title: data.title,
        description,
        listId: data.listId,
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
    async handle({ params, store }) {
      const archived = store.archiveCard(params.id);
      if (!archived) return notFound("Card not found");
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
    async handle({ params, body, store }) {
      const data = (body ?? {}) as { groupId?: string; position?: number };
      if (!data.groupId) {
        return {
          status: 400,
          body: { message: "groupId required", code: "BAD_REQUEST" },
        };
      }
      const moved = store.moveCard(params.id, data.groupId, data.position ?? 0);
      if (!moved) return notFound("Card not found");
      return ok({
        id: params.id,
        listId: data.groupId,
        position: data.position ?? 0,
      });
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
    route: "/board-items/:id/field-values/:fieldId",
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

  // ─── GET /board-items/:id/attachments ───────────────────────────────────────────

  defineMockOperation<{ id: string }, never, unknown[]>({
    id: "cards.attachments",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/board-items/:id/attachments",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      return ok([]);
    },
  }),

  // ─── GET /board-items/:id/activity ──────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    never,
    { activities: unknown[]; total: number }
  >({
    id: "cards.activity",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/board-items/:id/activity",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      return ok({ activities: [], total: 0 });
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
