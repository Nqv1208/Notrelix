/**
 * Documents — Pages context handlers.
 *
 * Operations:
 *   pages.list       — GET /workspaces/:workspaceId/pages
 *   pages.tree       — GET /workspaces/:workspaceId/pages/tree
 *   pages.detail     — GET /pages/:id
 *   pages.create     — POST /workspaces/:workspaceId/pages
 *   pages.update     — PATCH /pages/:id
 *   pages.delete     — DELETE /pages/:id
 *   pages.breadcrumb — GET /pages/:id/breadcrumb
 *   pages.blocks     — GET /pages/:id/blocks
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Documents split
 * Note: contracts for page/block operations are from endpoints.pages.*
 */

import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";
import type { MockPageRecord } from "../../state/records";

// Inline DTO — aligns with endpoints.pages contract and @notrelix/docs-state
interface PageDtoApi {
  id: string;
  workspaceId: string;
  parentId?: string | null;
  title: string;
  iconType?: string | null;
  iconValue?: string | null;
  coverUrl?: string | null;
  position: number;
  depth: number;
  isTemplate: boolean;
  isArchived: boolean;
  publishedAt?: string | null;
  deadline?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

interface BreadcrumbDtoApi {
  id: string;
  title: string;
  iconType?: string | null;
  iconValue?: string | null;
}

interface BlockDtoApi {
  id: string;
  pageId: string;
  type: string;
  position: number;
  properties: {
    text?: string;
    checked?: boolean;
    language?: string;
  };
  createdAt: string;
  updatedAt: string;
}

function mapPageDto(p: MockPageRecord): PageDtoApi {
  return {
    id: p.id,
    workspaceId: p.workspaceId,
    parentId: p.parentId ?? null,
    title: p.title,
    iconType: null,
    iconValue: p.icon ?? null,
    coverUrl: null,
    position: 0,
    depth: 0,
    isTemplate: false,
    isArchived: false,
    publishedAt: null,
    deadline: null,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt,
  };
}

export const pagesOperations = [
  // ─── GET /workspaces/:workspaceId/pages ───────────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, PageDtoApi[]>({
    id: "pages.list",
    method: "GET",
    route: "/workspaces/:workspaceId/pages",
    async handle({ params, store }) {
      const pages = store.getPages(params.workspaceId);
      return ok<PageDtoApi[]>(pages.map(mapPageDto));
    },
  }),

  // ─── GET /workspaces/:workspaceId/pages/tree ──────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, PageDtoApi[]>({
    id: "pages.tree",
    method: "GET",
    route: "/workspaces/:workspaceId/pages/tree",
    async handle({ params, store }) {
      const pages = store.getPages(params.workspaceId);
      return ok<PageDtoApi[]>(pages.map(mapPageDto));
    },
  }),

  // ─── GET /pages/:id ───────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, PageDtoApi>({
    id: "pages.detail",
    method: "GET",
    route: "/pages/:id",
    async handle({ params, store }) {
      const page = store.getPage(params.id);
      if (!page) return notFound("Page not found");
      return ok<PageDtoApi>(mapPageDto(page));
    },
  }),

  // ─── POST /workspaces/:workspaceId/pages ──────────────────────────────────

  defineMockOperation<
    { workspaceId: string },
    { title?: string; icon?: string; parentId?: string },
    PageDtoApi
  >({
    id: "pages.create",
    method: "POST",
    route: "/workspaces/:workspaceId/pages",
    async handle({ params, body, store }) {
      const data = body ?? {};
      const page = store.createPage(params.workspaceId, data);
      return created<PageDtoApi>(mapPageDto(page));
    },
  }),

  // ─── PATCH /pages/:id ─────────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { title?: string; icon?: string; parentId?: string },
    PageDtoApi
  >({
    id: "pages.update",
    method: "PATCH",
    route: "/pages/:id",
    async handle({ params, body, store }) {
      const updated = store.updatePage(params.id, body ?? {});
      if (!updated) return notFound("Page not found");
      const page = store.getPage(params.id)!;
      return ok<PageDtoApi>(mapPageDto(page));
    },
  }),

  // ─── DELETE /pages/:id ────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, void>({
    id: "pages.delete",
    method: "DELETE",
    route: "/pages/:id",
    async handle({ params, store }) {
      const deleted = store.deletePage(params.id);
      if (!deleted) return notFound("Page not found");
      return ok<void>(undefined);
    },
  }),

  // ─── GET /pages/:id/breadcrumb ────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, BreadcrumbDtoApi[]>({
    id: "pages.breadcrumb",
    method: "GET",
    route: "/pages/:id/breadcrumb",
    async handle({ params, store }) {
      const page = store.getPage(params.id);
      if (!page) return notFound("Page not found");
      return ok<BreadcrumbDtoApi[]>([
        {
          id: page.id,
          title: page.title,
          iconType: null,
          iconValue: page.icon ?? null,
        },
      ]);
    },
  }),

  // ─── GET /pages/:id/blocks ────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, BlockDtoApi[]>({
    id: "pages.blocks",
    method: "GET",
    route: "/pages/:id/blocks",
    async handle({ params, store }) {
      const page = store.getPage(params.id);
      if (!page) return notFound("Page not found");
      return ok<BlockDtoApi[]>([
        {
          id: `block-${page.id}-0`,
          pageId: page.id,
          type: "heading_1",
          position: 0,
          properties: {
            text: page.title,
          },
          createdAt: page.createdAt,
          updatedAt: page.updatedAt,
        },
        {
          id: `block-${page.id}-1`,
          pageId: page.id,
          type: "paragraph",
          position: 1,
          properties: {
            text: "Notrelix mock runtime specification.",
          },
          createdAt: page.createdAt,
          updatedAt: page.updatedAt,
        },
      ]);
    },
  }),
];
