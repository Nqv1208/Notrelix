/**
 * Documents — Pages context handlers.
 *
 * Operations:
 *   pages.list   — GET /workspaces/:workspaceId/pages
 *   pages.tree   — GET /workspaces/:workspaceId/pages/tree
 *   pages.detail — GET /pages/:id
 *   pages.blocks — GET /pages/:id/blocks
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Documents split
 * Note: contracts for page/block operations are from endpoints.pages.*
 */

import { defineMockOperation } from "../../operations/types";
import { ok, notFound } from "../../transport/create-response";

// Inline DTO — no official @notrelix/docs-core type package available yet
// These shapes align with the endpoints.pages contract
interface PageDtoApi {
  id: string;
  workspaceId: string;
  title: string;
  icon: string | null;
  parentId: string | null;
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

  // ─── GET /pages/:id/blocks ────────────────────────────────────────────────

  defineMockOperation<{ id: string }>({
    id: "pages.blocks",
    method: "GET",
    route: "/pages/:id/blocks",
    async handle({ params, store }) {
      const page = store.getPage(params.id);
      if (!page) return notFound("Page not found");
      return ok([]); // Empty blocks — implement when Docs block contract lands
    },
  }),
];
