/**
 * Documents — Pages context handlers.
 *
 * Operations:
 *   pages.list       — GET /workspaces/:workspaceId/pages
 *   pages.tree       — GET /workspaces/:workspaceId/pages/tree
 *   pages.detail     — GET /pages/:id
 *   pages.breadcrumb — GET /pages/:id/breadcrumb
 *   pages.blocks     — GET /pages/:id/blocks
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Documents split
 * Note: contracts for page/block operations are from endpoints.pages.*
 */

import { defineMockOperation } from "../../operations/types";
import { ok, notFound } from "../../transport/create-response";

// Inline DTO — aligns with endpoints.pages contract and @notrelix/docs-state
interface PageDtoApi {
  id: string;
  workspaceId: string;
  title: string;
  icon: string | null;
  parentId: string | null;
  createdAt: string;
  updatedAt: string;
}

interface BreadcrumbDtoApi {
  id: string;
  title: string;
  icon: string | null;
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

export const pagesOperations = [
  // ─── GET /workspaces/:workspaceId/pages ───────────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, PageDtoApi[]>({
    id: "pages.list",
    method: "GET",
    route: "/workspaces/:workspaceId/pages",
    async handle({ params, store }) {
      const pages = store.getPages(params.workspaceId);
      return ok<PageDtoApi[]>(
        pages.map((p) => ({
          id: p.id,
          workspaceId: p.workspaceId,
          title: p.title,
          icon: p.icon ?? null,
          parentId: p.parentId ?? null,
          createdAt: p.createdAt,
          updatedAt: p.updatedAt,
        })),
      );
    },
  }),

  // ─── GET /workspaces/:workspaceId/pages/tree ──────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, PageDtoApi[]>({
    id: "pages.tree",
    method: "GET",
    route: "/workspaces/:workspaceId/pages/tree",
    async handle({ params, store }) {
      const pages = store.getPages(params.workspaceId);
      return ok<PageDtoApi[]>(
        pages.map((p) => ({
          id: p.id,
          workspaceId: p.workspaceId,
          title: p.title,
          icon: p.icon ?? null,
          parentId: p.parentId ?? null,
          createdAt: p.createdAt,
          updatedAt: p.updatedAt,
        })),
      );
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
      return ok<PageDtoApi>({
        id: page.id,
        workspaceId: page.workspaceId,
        title: page.title,
        icon: page.icon ?? null,
        parentId: page.parentId ?? null,
        createdAt: page.createdAt,
        updatedAt: page.updatedAt,
      });
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
          icon: page.icon ?? null,
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
          id: `block-${page.id}-0001`,
          pageId: page.id,
          type: "paragraph",
          position: 0,
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
