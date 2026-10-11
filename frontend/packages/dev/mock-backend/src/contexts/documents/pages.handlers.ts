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
import type { MockStore } from "../../state/mock-store";
import { accessAtLeast } from "../../state/mock-spaces";

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
  /** Masked when the caller cannot see the Space (GOV-029). */
  spaceId?: string | null;
}

interface BreadcrumbDtoApi {
  id: string;
  title: string;
  iconType?: string | null;
  iconValue?: string | null;
}

function mapPageDto(p: MockPageRecord, store?: MockStore): PageDtoApi {
  const spaceId =
    p.spaceId &&
    store &&
    store.spaces.accessFor(p.spaceId, store.spaceViewer(p.workspaceId)) !== null
      ? p.spaceId
      : null;
  return {
    spaceId,
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
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/workspaces/:workspaceId/pages",
    async handle({ params, store }) {
      const pages = store.getPages(params.workspaceId);
      return ok<PageDtoApi[]>(pages.map((page) => mapPageDto(page, store)));
    },
  }),

  // ─── GET /workspaces/:workspaceId/pages/tree ──────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, PageDtoApi[]>({
    id: "pages.tree",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/workspaces/:workspaceId/pages/tree",
    async handle({ params, store }) {
      const pages = store.getPages(params.workspaceId);
      return ok<PageDtoApi[]>(pages.map((page) => mapPageDto(page, store)));
    },
  }),

  // ─── GET /pages/:id ───────────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, PageDtoApi>({
    id: "pages.detail",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/pages/:id",
    async handle({ params, store }) {
      const page = store.getPage(params.id);
      if (!page) return notFound("Page not found");
      return ok<PageDtoApi>(mapPageDto(page, store));
    },
  }),

  // ─── POST /workspaces/:workspaceId/pages ──────────────────────────────────

  defineMockOperation<
    { workspaceId: string },
    {
      title?: string;
      icon?: string;
      parentId?: string;
      spaceId?: string | null;
    },
    PageDtoApi
  >({
    id: "pages.create",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/workspaces/:workspaceId/pages",
    async handle({ params, body, store }) {
      const data = body ?? {};
      const spaceId = data.spaceId ?? null;
      if (spaceId) {
        // GOV-028: CreatePage on the Space (Editor); a nested page follows
        // its parent's Space instead (SpaceId with ParentId is invalid).
        if (data.parentId) {
          return {
            status: 422,
            body: {
              message: "Validation failed",
              code: "VALIDATION_ERROR",
              errors: {
                SpaceId: ["A nested page follows its parent's space."],
              },
            },
          };
        }
        const access = store.spaces.getSpace(spaceId)
          ? store.spaces.accessFor(
              spaceId,
              store.spaceViewer(params.workspaceId),
            )
          : null;
        if (access === null) return notFound();
        if (!accessAtLeast(access, "Editor"))
          return {
            status: 403,
            body: { message: "Forbidden", code: "FORBIDDEN" },
          };
      }
      const page = store.createPage(params.workspaceId, { ...data, spaceId });
      return created<PageDtoApi>(mapPageDto(page, store));
    },
  }),

  // ─── PATCH /pages/:id ─────────────────────────────────────────────────────

  defineMockOperation<
    { id: string },
    { title?: string; icon?: string; parentId?: string },
    PageDtoApi
  >({
    id: "pages.update",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
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
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
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
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
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
];
