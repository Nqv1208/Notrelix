import { AppError } from "@notrelix/kernel";
import type { BlockDtoApi, CreateBlockPayload, CreateCommentPayload, CreatePagePayload, UpdateBlockPayload, UpdatePagePayload } from "@notrelix/docs-state";
import type { MockHandler } from "../transport/mock-handler";

function idAt(url: string, index: number) { return url.split("/")[index]; }

export const documentHandlers: readonly MockHandler[] = [
  {
    id: "docs.page.list",
    matches: (request) => request.method === "GET" && /^\/workspaces\/[^/]+\/pages$/.test(request.url),
    async handle(request, context) { const workspaceId = idAt(request.url, 2); return context.store.getSnapshot().pages.filter((page) => page.workspaceId === workspaceId); },
  },
  {
    id: "docs.page.search",
    matches: (request) => request.method === "GET" && /^\/workspaces\/[^/]+\/pages\/search\?/.test(request.url),
    async handle(request, context) { const url = new URL(request.url, "http://mock.notrelix.local"); const workspaceId = idAt(url.pathname, 2); const query = (url.searchParams.get("query") ?? "").toLowerCase(); return context.store.getSnapshot().pages.filter((page) => page.workspaceId === workspaceId && page.title.toLowerCase().includes(query)); },
  },
  {
    id: "docs.page.get",
    matches: (request) => request.method === "GET" && /^\/pages\/[^/]+$/.test(request.url),
    async handle(request, context) { const page = context.store.getSnapshot().pages.find(({ id }) => id === idAt(request.url, 2)); if (!page) throw new AppError({ kind: "not_found", status: 404, message: "Page not found." }); return page; },
  },
  {
    id: "docs.page.create",
    matches: (request) => request.method === "POST" && /^\/workspaces\/[^/]+\/pages$/.test(request.url),
    async handle(request, context) { const workspaceId = idAt(request.url, 2); const body = request.body as CreatePagePayload; const id = context.store.nextId("page"); context.store.update((draft) => { draft.pages.push({ id, workspaceId, parentId: body.parentId, title: body.title, position: draft.pages.filter((page) => page.workspaceId === workspaceId).length, depth: 0, isTemplate: false, isArchived: false, createdAt: context.now().toISOString(), updatedAt: context.now().toISOString() }); }); return id; },
  },
  {
    id: "docs.page.update",
    matches: (request) => request.method === "PATCH" && /^\/pages\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = idAt(request.url, 2); const body = request.body as UpdatePagePayload & { iconValue?: string | null }; context.store.update((draft) => { const page = draft.pages.find((candidate) => candidate.id === id); if (!page) throw new AppError({ kind: "not_found", status: 404, message: "Page not found." }); if (body.title !== undefined) page.title = body.title; if (body.iconValue !== undefined) page.iconValue = body.iconValue; if (body.coverUrl !== undefined) page.coverUrl = body.coverUrl; page.updatedAt = context.now().toISOString(); }); },
  },
  {
    id: "docs.page.delete",
    matches: (request) => request.method === "DELETE" && /^\/pages\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = idAt(request.url, 2); context.store.update((draft) => { draft.pages = draft.pages.filter((page) => page.id !== id); draft.blocks = draft.blocks.filter((block) => block.pageId !== id); }); },
  },
  {
    id: "docs.breadcrumb",
    matches: (request) => request.method === "GET" && /^\/pages\/[^/]+\/breadcrumb$/.test(request.url),
    async handle(request, context) { const page = context.store.getSnapshot().pages.find(({ id }) => id === idAt(request.url, 2)); return page ? [{ id: page.id, title: page.title, iconValue: page.iconValue }] : []; },
  },
  {
    id: "docs.block.list",
    matches: (request) => request.method === "GET" && /^\/pages\/[^/]+\/blocks$/.test(request.url),
    async handle(request, context) { return context.store.getSnapshot().blocks.filter((block) => block.pageId === idAt(request.url, 2)); },
  },
  {
    id: "docs.block.create",
    matches: (request) => request.method === "POST" && /^\/pages\/[^/]+\/blocks$/.test(request.url),
    async handle(request, context) { const pageId = idAt(request.url, 2); const body = request.body as CreateBlockPayload; const id = context.store.nextId("block"); const block = { id, pageId, parentBlockId: body.parentId, type: body.type, properties: body.properties ?? {}, position: body.position ?? context.store.getSnapshot().blocks.filter((candidate) => candidate.pageId === pageId).length, version: 1, createdByUserId: context.store.getCurrentUser().id, createdAt: context.now().toISOString(), updatedAt: context.now().toISOString() }; context.store.update((draft) => { draft.blocks.push(block); }); return block; },
  },
  {
    id: "docs.block.update",
    matches: (request) => request.method === "PATCH" && /^\/blocks\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = idAt(request.url, 2); const body = request.body as UpdateBlockPayload; let result; context.store.update((draft) => { const block = draft.blocks.find((candidate) => candidate.id === id); if (!block) throw new AppError({ kind: "not_found", status: 404, message: "Block not found." }); if (body.type) block.type = body.type; if (body.properties) block.properties = body.properties; if (body.position !== undefined) block.position = body.position; block.version += 1; block.updatedAt = context.now().toISOString(); result = block; }); return result; },
  },
  {
    id: "docs.block.delete",
    matches: (request) => request.method === "DELETE" && /^\/blocks\/[^/]+$/.test(request.url),
    async handle(request, context) { const id = idAt(request.url, 2); context.store.update((draft) => { draft.blocks = draft.blocks.filter((block) => block.id !== id); }); },
  },
  {
    id: "docs.block.reorder",
    matches: (request) => request.method === "POST" && request.url === "/blocks/reorder",
    async handle(request, context) { const body = request.body as { orderedBlockIds: string[] }; context.store.update((draft) => { body.orderedBlockIds.forEach((id, position) => { const block = draft.blocks.find((candidate) => candidate.id === id); if (block) block.position = position; }); }); },
  },
  {
    id: "docs.block.batch",
    matches: (request) => request.method === "POST" && /^\/pages\/[^/]+\/blocks\/batch$/.test(request.url),
    async handle(request, context) { const pageId = idAt(request.url, 2); const body = request.body as { blocks: UpdateBlockPayload[] }; const updated: BlockDtoApi[] = []; context.store.update((draft) => { const pageBlocks = draft.blocks.filter((block) => block.pageId === pageId); body.blocks.forEach((payload, index) => { const block = pageBlocks[index]; if (!block) return; if (payload.type) block.type = payload.type; if (payload.properties) block.properties = payload.properties; if (payload.position !== undefined) block.position = payload.position; block.version += 1; block.updatedAt = context.now().toISOString(); updated.push(structuredClone(block)); }); }); return updated; },
  },
  {
    id: "docs.comments.list",
    matches: (request) => request.method === "GET" && /^\/pages\/[^/]+\/comments$/.test(request.url),
    async handle(request, context) { return context.store.getSnapshot().pageComments.filter(({ pageId }) => pageId === idAt(request.url, 2)).map(({ pageId: _pageId, ...comment }) => comment); },
  },
  {
    id: "docs.comments.create",
    matches: (request) => request.method === "POST" && /^\/pages\/[^/]+\/comments$/.test(request.url),
    async handle(request, context) { const pageId = idAt(request.url, 2); const body = request.body as CreateCommentPayload; const comment = { id: context.store.nextId("page-comment"), pageId, userId: context.store.getCurrentUser().id, contentMd: body.body, createdAt: context.now().toISOString(), isEdited: false }; context.store.update((draft) => { draft.pageComments.push(comment); }); const { pageId: _pageId, ...result } = comment; return result; },
  },
  {
    id: "docs.comments.delete",
    matches: (request) => request.method === "DELETE" && /^\/comments\/[^/]+$/.test(request.url),
    async handle(request, context) { const commentId = idAt(request.url, 2); context.store.update((draft) => { draft.pageComments = draft.pageComments.filter(({ id }) => id !== commentId); }); },
  },
  { id: "docs.history.list", matches: (request) => request.method === "GET" && /^\/pages\/[^/]+\/history$/.test(request.url), async handle() { return []; } },
];
