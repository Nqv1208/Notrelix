import { AppError } from "@notrelix/kernel";
import type { MockHandler } from "../transport/mock-handler";
import { projectFullBoard } from "../projections/work-management.projections";

export const workManagementHandlers: readonly MockHandler[] = [
  {
    id: "board.list",
    matches: (request) => request.method === "GET" && /^\/workspaces\/[^/]+\/boards$/.test(request.url),
    async handle(request, context) {
      const workspaceId = request.url.split("/")[2];
      return context.store.getSnapshot().boards.filter((board) => board.workspaceId === workspaceId);
    },
  },
  {
    id: "board.full",
    matches: (request) => request.method === "GET" && /^\/boards\/[^/]+\/full$/.test(request.url),
    async handle(request, context) {
      const boardId = request.url.split("/")[2];
      const board = projectFullBoard(context.store.getSnapshot(), boardId);
      if (!board) throw new AppError({ kind: "not_found", status: 404, message: "Board not found." });
      return board;
    },
  },
  {
    id: "board.view.get",
    matches: (request) => request.method === "GET" && /^\/boards\/[^/]+\/view$/.test(request.url),
    async handle(request, context) { return context.store.getSnapshot().boardViews[request.url.split("/")[2]] ?? { viewMode: "table", config: null }; },
  },
  {
    id: "board.view.save",
    matches: (request) => request.method === "PUT" && /^\/boards\/[^/]+\/view$/.test(request.url),
    async handle(request, context) {
      const boardId = request.url.split("/")[2];
      const body = request.body as { viewMode?: string; config?: string };
      context.store.update((draft) => { draft.boardViews[boardId] = { viewMode: body.viewMode, config: body.config }; });
    },
  },
  {
    id: "card.get",
    matches: (request) => request.method === "GET" && /^\/cards\/[^/]+$/.test(request.url),
    async handle(request, context) {
      const card = context.store.getSnapshot().cards.find(({ id }) => id === request.url.split("/")[2]);
      if (!card) throw new AppError({ kind: "not_found", status: 404, message: "Item not found." });
      return card;
    },
  },
  {
    id: "card.create",
    matches: (request) => request.method === "POST" && /^\/lists\/[^/]+\/cards$/.test(request.url),
    async handle(request, context) {
      const listId = request.url.split("/")[2];
      const body = request.body as { title: string; position?: number };
      const snapshot = context.store.getSnapshot();
      const list = snapshot.lists.find(({ id }) => id === listId);
      if (!list) throw new AppError({ kind: "not_found", status: 404, message: "Group not found." });
      const id = context.store.nextId("item");
      context.store.update((draft) => { draft.cards.push({ id, boardId: list.boardId, workspaceId: draft.boards.find(({ id }) => id === list.boardId)!.workspaceId, listId, title: body.title, status: "todo", position: body.position ?? draft.cards.filter((card) => card.listId === listId).length, members: [], labels: [], checklists: [], commentCount: 0, attachmentCount: 0, createdAt: context.now().toISOString() }); });
      return id;
    },
  },
  {
    id: "card.update",
    matches: (request) => request.method === "PATCH" && /^\/cards\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; context.store.update((draft) => { const card = draft.cards.find((candidate) => candidate.id === id); if (!card) throw new AppError({ kind: "not_found", status: 404, message: "Item not found." }); Object.assign(card, request.body); }); },
  },
  {
    id: "card.move",
    matches: (request) => request.method === "POST" && /^\/cards\/[^/]+\/move$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; const body = request.body as { listId: string; position: number }; context.store.update((draft) => { const card = draft.cards.find((candidate) => candidate.id === id); if (!card) throw new AppError({ kind: "not_found", status: 404, message: "Item not found." }); card.listId = body.listId; card.position = body.position; }); },
  },
  {
    id: "card.delete",
    matches: (request) => request.method === "DELETE" && /^\/cards\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; context.store.update((draft) => { draft.cards = draft.cards.filter((card) => card.id !== id); draft.cardComments = draft.cardComments.filter((comment) => comment.cardId !== id); }); },
  },
  {
    id: "card.archive",
    matches: (request) => request.method === "POST" && /^\/cards\/[^/]+\/archive$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; context.store.update((draft) => { const card = draft.cards.find((candidate) => candidate.id === id); if (card) card.status = "archived"; }); },
  },
  {
    id: "card.duplicate",
    matches: (request) => request.method === "POST" && /^\/cards\/[^/]+\/duplicate$/.test(request.url),
    async handle(request, context) { const sourceId = request.url.split("/")[2]; const source = context.store.getSnapshot().cards.find(({ id }) => id === sourceId); if (!source) throw new AppError({ kind: "not_found", status: 404, message: "Item not found." }); const id = context.store.nextId("item"); context.store.update((draft) => { draft.cards.push({ ...source, id, title: `${source.title} copy`, position: source.position + 1 }); }); return id; },
  },
  {
    id: "card.field-value",
    matches: (request) => request.method === "PATCH" && /^\/cards\/[^/]+\/field-values$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; const body = request.body as { fieldDefinitionId: string; value: unknown }; context.store.update((draft) => { const card = draft.cards.find((candidate) => candidate.id === id); if (card) card.fieldValues = { ...(typeof card.fieldValues === "object" && card.fieldValues ? card.fieldValues : {}), [body.fieldDefinitionId]: body.value }; }); },
  },
  { id: "card.files", matches: (request) => request.method === "GET" && /^\/cards\/[^/]+\/attachments$/.test(request.url), async handle() { return []; } },
  { id: "card.activity", matches: (request) => request.method === "GET" && /^\/cards\/[^/]+\/activity$/.test(request.url), async handle() { return { data: [], total: 0, page: 1, pageSize: 20 }; } },
  {
    id: "list.create",
    matches: (request) => request.method === "POST" && /^\/boards\/[^/]+\/lists$/.test(request.url),
    async handle(request, context) { const boardId = request.url.split("/")[2]; const body = request.body as { title: string; position?: number; color?: string }; const id = context.store.nextId("group"); context.store.update((draft) => { draft.lists.push({ boardId, id, title: body.title, color: body.color, position: body.position ?? draft.lists.filter((list) => list.boardId === boardId).length, isArchived: false, cards: [] }); }); return id; },
  },
  {
    id: "list.update",
    matches: (request) => request.method === "PATCH" && /^\/lists\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; context.store.update((draft) => { const list = draft.lists.find((candidate) => candidate.id === id); if (list) Object.assign(list, request.body); }); },
  },
  {
    id: "list.delete",
    matches: (request) => request.method === "DELETE" && /^\/lists\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; context.store.update((draft) => { draft.lists = draft.lists.filter((list) => list.id !== id); draft.cards = draft.cards.filter((card) => card.listId !== id); }); },
  },
  {
    id: "list.duplicate",
    matches: (request) => request.method === "POST" && /^\/lists\/[^/]+\/duplicate$/.test(request.url),
    async handle(request, context) { const source = context.store.getSnapshot().lists.find(({ id }) => id === request.url.split("/")[2]); if (!source) throw new AppError({ kind: "not_found", status: 404, message: "Group not found." }); const id = context.store.nextId("group"); context.store.update((draft) => { draft.lists.push({ ...source, id, title: `${source.title} copy`, cards: [] }); }); return id; },
  },
  {
    id: "list.reorder",
    matches: (request) => request.method === "POST" && /^\/boards\/[^/]+\/lists\/reorder$/.test(request.url),
    async handle(request, context) { const body = request.body as { items: Array<{ id: string; newPosition: number }> }; context.store.update((draft) => { for (const item of body.items) { const list = draft.lists.find((candidate) => candidate.id === item.id); if (list) list.position = item.newPosition; } }); },
  },
  {
    id: "card.comments.get",
    matches: (request) => request.method === "GET" && /^\/cards\/[^/]+\/comments$/.test(request.url),
    async handle(request, context) { const cardId = request.url.split("/")[2]; return context.store.getSnapshot().cardComments.filter((comment) => comment.cardId === cardId); },
  },
  {
    id: "card.comments.create",
    matches: (request) => request.method === "POST" && /^\/cards\/[^/]+\/comments$/.test(request.url),
    async handle(request, context) { const cardId = request.url.split("/")[2]; const body = request.body as { contentMd: string }; const user = context.store.getCurrentUser(); const id = context.store.nextId("comment"); context.store.update((draft) => { draft.cardComments.push({ cardId, id, userId: user.id, userName: user.name, contentMd: body.contentMd, isEdited: false, createdAt: context.now().toISOString() }); const card = draft.cards.find((candidate) => candidate.id === cardId); if (card) card.commentCount += 1; }); return id; },
  },
  {
    id: "card.comments.update",
    matches: (request) => request.method === "PATCH" && /^\/comments\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; const body = request.body as { contentMd: string }; context.store.update((draft) => { const comment = draft.cardComments.find((candidate) => candidate.id === id); if (comment) { comment.contentMd = body.contentMd; comment.isEdited = true; } }); },
  },
  {
    id: "card.comments.delete",
    matches: (request) => request.method === "DELETE" && /^\/comments\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = request.url.split("/")[2]; context.store.update((draft) => { draft.cardComments = draft.cardComments.filter((comment) => comment.id !== id); draft.pageComments = draft.pageComments.filter((comment) => comment.id !== id); }); },
  },
  {
    id: "board.columns.create",
    matches: (request) => request.method === "POST" && /^\/boards\/[^/]+\/columns$/.test(request.url),
    async handle(request, context) { const boardId = request.url.split("/")[2]; const body = request.body as { name: string; fieldType: string; settings?: string; position?: number }; const id = context.store.nextId("field"); context.store.update((draft) => { draft.columns.push({ id, boardId, name: body.name, fieldType: body.fieldType, settings: body.settings, position: body.position ?? draft.columns.filter((column) => column.boardId === boardId).length, isHidden: false, isSystemField: false }); }); return id; },
  },
  {
    id: "board.columns.update",
    matches: (request) => request.method === "PATCH" && /^\/boards\/[^/]+\/columns\/[^/]+$/.test(request.url),
    async handle(request, context) { const columnId = request.url.split("/")[4]; context.store.update((draft) => { const column = draft.columns.find(({ id }) => id === columnId); if (column) Object.assign(column, request.body); }); },
  },
  {
    id: "board.columns.delete",
    matches: (request) => request.method === "DELETE" && /^\/boards\/[^/]+\/columns\/[^/]+$/.test(request.url),
    async handle(request, context) { const columnId = request.url.split("/")[4]; context.store.update((draft) => { draft.columns = draft.columns.filter(({ id }) => id !== columnId); }); },
  },
  {
    id: "board.columns.reorder",
    matches: (request) => request.method === "POST" && /^\/boards\/[^/]+\/columns\/reorder$/.test(request.url),
    async handle(request, context) { const body = request.body as { items: Array<{ id: string; newPosition: number }> }; context.store.update((draft) => { for (const item of body.items) { const column = draft.columns.find(({ id }) => id === item.id); if (column) column.position = item.newPosition; } }); },
  },
  {
    id: "board.labels.get",
    matches: (request) => request.method === "GET" && /^\/boards\/[^/]+\/labels$/.test(request.url),
    async handle(request, context) { const boardId = request.url.split("/")[2]; return context.store.getSnapshot().labels.filter((label) => label.boardId === boardId).map(({ id, name, color }) => ({ id, name, color })); },
  },
  {
    id: "board.labels.create",
    matches: (request) => request.method === "POST" && /^\/boards\/[^/]+\/labels$/.test(request.url),
    async handle(request, context) { const boardId = request.url.split("/")[2]; const body = request.body as { name?: string; color: string }; const label = { id: context.store.nextId("label"), boardId, name: body.name ?? "Label", color: body.color }; context.store.update((draft) => { draft.labels.push(label); }); return label; },
  },
  {
    id: "board.labels.update",
    matches: (request) => request.method === "PATCH" && /^\/boards\/[^/]+\/labels\/[^/]+$/.test(request.url),
    async handle(request, context) { const labelId = request.url.split("/")[4]; context.store.update((draft) => { const label = draft.labels.find(({ id }) => id === labelId); if (label) Object.assign(label, request.body); }); },
  },
  {
    id: "board.labels.delete",
    matches: (request) => request.method === "DELETE" && /^\/boards\/[^/]+\/labels\/[^/]+$/.test(request.url),
    async handle(request, context) { const labelId = request.url.split("/")[4]; context.store.update((draft) => { draft.labels = draft.labels.filter(({ id }) => id !== labelId); for (const card of draft.cards) card.labels = card.labels.filter((label) => label.labelId !== labelId); }); },
  },
  {
    id: "card.labels.add",
    matches: (request) => request.method === "POST" && /^\/cards\/[^/]+\/labels$/.test(request.url),
    async handle(request, context) { const cardId = request.url.split("/")[2]; const body = request.body as { labelId: string }; const label = context.store.getSnapshot().labels.find(({ id }) => id === body.labelId); if (label) context.store.update((draft) => { const card = draft.cards.find(({ id }) => id === cardId); if (card && !card.labels.some(({ labelId }) => labelId === label.id)) card.labels.push({ labelId: label.id, name: label.name, color: label.color }); }); },
  },
  {
    id: "card.labels.remove",
    matches: (request) => request.method === "DELETE" && /^\/cards\/[^/]+\/labels\/[^/]+$/.test(request.url),
    async handle(request, context) { const parts = request.url.split("/"); context.store.update((draft) => { const card = draft.cards.find(({ id }) => id === parts[2]); if (card) card.labels = card.labels.filter(({ labelId }) => labelId !== parts[4]); }); },
  },
  {
    id: "card.checklists.get",
    matches: (request) => request.method === "GET" && /^\/cards\/[^/]+\/checklists$/.test(request.url),
    async handle(request, context) { return context.store.getSnapshot().cards.find(({ id }) => id === request.url.split("/")[2])?.checklists ?? []; },
  },
  {
    id: "card.checklists.create",
    matches: (request) => request.method === "POST" && /^\/cards\/[^/]+\/checklists$/.test(request.url),
    async handle(request, context) { const cardId = request.url.split("/")[2]; const body = request.body as { title: string }; const id = context.store.nextId("checklist"); context.store.update((draft) => { const card = draft.cards.find((candidate) => candidate.id === cardId); if (card) card.checklists.push({ id, title: body.title, position: card.checklists.length, items: [] }); }); return id; },
  },
  {
    id: "card.checklists.update",
    matches: (request) => request.method === "PATCH" && /^\/checklists\/[^/]+$/.test(request.url),
    async handle(request, context) { const checklistId = request.url.split("/")[2]; context.store.update((draft) => { for (const card of draft.cards) { const checklist = card.checklists.find(({ id }) => id === checklistId); if (checklist) Object.assign(checklist, request.body); } }); },
  },
  {
    id: "card.checklists.delete",
    matches: (request) => request.method === "DELETE" && /^\/checklists\/[^/]+$/.test(request.url),
    async handle(request, context) { const checklistId = request.url.split("/")[2]; context.store.update((draft) => { for (const card of draft.cards) card.checklists = card.checklists.filter(({ id }) => id !== checklistId); }); },
  },
  {
    id: "card.checklist-items.create",
    matches: (request) => request.method === "POST" && /^\/checklists\/[^/]+\/items$/.test(request.url),
    async handle(request, context) { const checklistId = request.url.split("/")[2]; const body = request.body as { title: string }; const id = context.store.nextId("checklist-item"); context.store.update((draft) => { for (const card of draft.cards) { const checklist = card.checklists.find((candidate) => candidate.id === checklistId); if (checklist) checklist.items.push({ id, title: body.title, isChecked: false, position: checklist.items.length }); } }); return id; },
  },
  {
    id: "card.checklist-items.update",
    matches: (request) => request.method === "PATCH" && /^\/checklist-items\/[^/]+$/.test(request.url),
    async handle(request, context) { const itemId = request.url.split("/")[2]; context.store.update((draft) => { for (const card of draft.cards) for (const checklist of card.checklists) { const item = checklist.items.find(({ id }) => id === itemId); if (item) Object.assign(item, request.body); } }); },
  },
  {
    id: "card.checklist-items.delete",
    matches: (request) => request.method === "DELETE" && /^\/checklist-items\/[^/]+$/.test(request.url),
    async handle(request, context) { const itemId = request.url.split("/")[2]; context.store.update((draft) => { for (const card of draft.cards) for (const checklist of card.checklists) checklist.items = checklist.items.filter(({ id }) => id !== itemId); }); },
  },
];
