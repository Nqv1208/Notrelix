/**
 * Work Management — Cards, Checklists, and Comments context handlers.
 *
 * Operations:
 *   cards.byList            — GET /lists/:listId/cards
 *   cards.detail            — GET /cards/:id
 *   cards.create            — POST /lists/:listId/cards (returns string cardId)
 *   cards.update            — PATCH /cards/:id
 *   cards.delete            — DELETE /cards/:id
 *   cards.archive           — POST /cards/:id/archive
 *   cards.duplicate         — POST /cards/:id/duplicate (returns string cardId)
 *   cards.move              — POST /cards/:id/move
 *   cards.fieldValues       — PATCH /cards/:id/field-values
 *   cards.attachments       — GET /cards/:id/attachments
 *   cards.activity          — GET /cards/:id/activity
 *   cards.labels.list       — GET /cards/:id/labels
 *   cards.labels.add        — POST /cards/:id/labels
 *   cards.labels.remove     — DELETE /cards/:id/labels/:labelId
 *   checklists.list         — GET /cards/:cardId/checklists
 *   checklists.create       — POST /cards/:cardId/checklists (returns string checklistId)
 *   checklists.update       — PATCH /checklists/:id
 *   checklists.delete       — DELETE /checklists/:id
 *   checklistItems.create   — POST /checklists/:id/items (returns string itemId)
 *   checklistItems.update   — PATCH /checklist-items/:id
 *   checklistItems.delete   — DELETE /checklist-items/:id
 *   comments.list           — GET /cards/:cardId/comments
 *   comments.create         — POST /cards/:cardId/comments (returns string commentId)
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
import { ok, created, notFound } from "../../transport/create-response";
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
    id: "cards.byList",
    method: "GET",
    route: "/lists/:listId/cards",
    async handle({ params, store }) {
      const cards = store.getCards(params.listId);
      return ok<CardDtoApi[]>(cards.map((c) => projectCardDto(c, store)));
    },
  }),

  // ─── GET /cards/:id ───────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, CardDtoApi>({
    id: "cards.detail",
    method: "GET",
    route: "/cards/:id",
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
    id: "cards.create",
    method: "POST",
    route: "/lists/:listId/cards",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as {
        title?: string;
        position?: number;
        description?: string;
      };
      const list = store.getList(params.listId);
      if (!list) return notFound("List not found");

      const newCard = store.createCardByListId(params.listId, {
        title: data.title,
        position: data.position,
        description: data.description,
      });
      return created<string>(newCard.id);
    },
  }),

  // ─── PATCH /cards/:id ─────────────────────────────────────────────────────

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
    id: "cards.update",
    method: "PATCH",
    route: "/cards/:id",
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

  // ─── DELETE /cards/:id ────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "cards.delete",
    method: "DELETE",
    route: "/cards/:id",
    async handle({ params, store }) {
      const deleted = store.deleteCard(params.id);
      if (!deleted) return notFound("Card not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /cards/:id/archive ──────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "cards.archive",
    method: "POST",
    route: "/cards/:id/archive",
    async handle({ params, store }) {
      const archived = store.archiveCard(params.id);
      if (!archived) return notFound("Card not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /cards/:id/duplicate ────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, string>({
    id: "cards.duplicate",
    method: "POST",
    route: "/cards/:id/duplicate",
    async handle({ params, store }) {
      const duplicated = store.duplicateCard(params.id);
      if (!duplicated) return notFound("Card not found");
      return created<string>(duplicated.id);
    },
  }),

  // ─── POST /cards/:id/move ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, { listId?: string; position?: number }>({
    id: "cards.move",
    method: "POST",
    route: "/cards/:id/move",
    async handle({ params, body, store }) {
      const data = (body ?? {}) as { listId?: string; position?: number };
      if (!data.listId) {
        return {
          status: 400,
          body: { message: "listId required", code: "BAD_REQUEST" },
        };
      }
      const moved = store.moveCard(params.id, data.listId, data.position ?? 0);
      if (!moved) return notFound("Card not found");
      return ok({
        id: params.id,
        listId: data.listId,
        position: data.position ?? 0,
      });
    },
  }),

  // ─── PATCH /cards/:id/field-values ────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { fieldDefinitionId: string; value: unknown },
    void
  >({
    id: "cards.fieldValues",
    method: "PATCH",
    route: "/cards/:id/field-values",
    async handle({ params, body, store }) {
      const data = body as { fieldDefinitionId: string; value: unknown };
      const updated = store.updateFieldValue(
        params.id,
        data.fieldDefinitionId,
        data.value,
      );
      if (!updated) return notFound("Card not found");
      return ok<void>(undefined);
    },
  }),

  // ─── GET /cards/:id/attachments ───────────────────────────────────────────

  defineMockOperation<{ id: string }, never, unknown[]>({
    id: "cards.attachments",
    method: "GET",
    route: "/cards/:id/attachments",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      return ok([]);
    },
  }),

  // ─── GET /cards/:id/activity ──────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    never,
    { activities: unknown[]; total: number }
  >({
    id: "cards.activity",
    method: "GET",
    route: "/cards/:id/activity",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      return ok({ activities: [], total: 0 });
    },
  }),

  // ─── GET /cards/:id/labels ────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    never,
    { id: string; name: string; color: string }[]
  >({
    id: "cards.labels.list",
    method: "GET",
    route: "/cards/:id/labels",
    async handle({ params, store }) {
      const c = store.getCard(params.id);
      if (!c) return notFound("Card not found");
      const labels = store.getCardLabels(params.id);
      return ok(
        labels.map((l) => ({ id: l.id, name: l.name, color: l.color })),
      );
    },
  }),

  // ─── POST /cards/:id/labels ───────────────────────────────────────────────

  defineMockOperation<{ id: string }, { labelId: string }, void>({
    id: "cards.labels.add",
    method: "POST",
    route: "/cards/:id/labels",
    async handle({ params, body, store }) {
      const data = body as { labelId: string };
      const added = store.addLabelToCard(params.id, data.labelId);
      if (!added) return notFound("Card or Label not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /cards/:id/labels/:labelId ────────────────────────────────────

  defineMockOperation<{ id: string; labelId: string }, never, void>({
    id: "cards.labels.remove",
    method: "DELETE",
    route: "/cards/:id/labels/:labelId",
    async handle({ params, store }) {
      const removed = store.removeLabelFromCard(params.id, params.labelId);
      if (!removed) return notFound("Card not found");
      return ok<void>(undefined);
    },
  }),

  // ─── GET /cards/:cardId/checklists ────────────────────────────────────────

  defineMockOperation<{ cardId: string }, never, ChecklistDtoApi[]>({
    id: "checklists.list",
    method: "GET",
    route: "/cards/:cardId/checklists",
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

  // ─── POST /cards/:cardId/checklists ───────────────────────────────────────

  defineMockOperation<{ cardId: string }, { title: string }, string>({
    id: "checklists.create",
    method: "POST",
    route: "/cards/:cardId/checklists",
    async handle({ params, body, store }) {
      const data = body as { title: string };
      const card = store.getCard(params.cardId);
      if (!card) return notFound("Card not found");
      const chk = store.createChecklist(params.cardId, data.title);
      return created<string>(chk.id);
    },
  }),

  // ─── PATCH /checklists/:id ────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { title?: string; position?: number },
    void
  >({
    id: "checklists.update",
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
    method: "DELETE",
    route: "/checklists/:id",
    async handle({ params, store }) {
      const deleted = store.deleteChecklist(params.id);
      if (!deleted) return notFound("Checklist not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /checklists/:id/items ───────────────────────────────────────────

  defineMockOperation<{ id: string }, { title: string }, string>({
    id: "checklistItems.create",
    method: "POST",
    route: "/checklists/:id/items",
    async handle({ params, body, store }) {
      const data = body as { title: string };
      const chk = store.getChecklist(params.id);
      if (!chk) return notFound("Checklist not found");
      const item = store.createChecklistItem(params.id, data.title);
      return created<string>(item.id);
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
    method: "DELETE",
    route: "/checklist-items/:id",
    async handle({ params, store }) {
      const deleted = store.deleteChecklistItem(params.id);
      if (!deleted) return notFound("ChecklistItem not found");
      return ok<void>(undefined);
    },
  }),

  // ─── GET /cards/:cardId/comments ──────────────────────────────────────────

  defineMockOperation<{ cardId: string }, never, CommentDtoApi[]>({
    id: "comments.list",
    method: "GET",
    route: "/cards/:cardId/comments",
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

  // ─── POST /cards/:cardId/comments ─────────────────────────────────────────

  defineMockOperation<{ cardId: string }, { contentMd: string }, string>({
    id: "comments.create",
    method: "POST",
    route: "/cards/:cardId/comments",
    async handle({ params, body, store }) {
      const data = body as { contentMd: string };
      const card = store.getCard(params.cardId);
      if (!card) return notFound("Card not found");
      const user = store.getCurrentUser();
      const cmt = store.createCardComment(
        params.cardId,
        user.id,
        data.contentMd,
      );
      return created<string>(cmt.id);
    },
  }),

  // ─── PATCH /comments/:id ──────────────────────────────────────────────────

  defineMockOperation<{ id: string }, { contentMd: string }, void>({
    id: "comments.update",
    method: "PATCH",
    route: "/comments/:id",
    async handle({ params, body, store }) {
      const data = body as { contentMd: string };
      const updated = store.updateCardComment(params.id, data.contentMd);
      if (!updated) return notFound("Comment not found");
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /comments/:id ─────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "comments.delete",
    method: "DELETE",
    route: "/comments/:id",
    async handle({ params, store }) {
      const deleted = store.deleteCardComment(params.id);
      if (!deleted) return notFound("Comment not found");
      return ok<void>(undefined);
    },
  }),
];
