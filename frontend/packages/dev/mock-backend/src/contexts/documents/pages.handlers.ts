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
 *   pages.moveToSpace — POST /pages/:pageId/move-to-space (Documents.Pages.MoveToSpace)
 *   pages.breadcrumb — GET /pages/:id/breadcrumb
 *   pages.blocks     — GET /pages/:id/blocks
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Documents split
 * Note: contracts for page/block operations are from endpoints.pages.*
 */

import { defineMockOperation } from "../../operations/types";
import {
  ok,
  created,
  forbidden,
  noContent,
  notFound,
} from "../../transport/create-response";
import type { MockPageRecord } from "../../state/records";
import type { MockStore } from "../../state/mock-store";
import { accessAtLeast } from "../../state/mock-spaces";
import {
  preconditionFailed,
  targetSpaceDenied,
} from "../workspace/space-target";

// Inline DTO — aligns with endpoints.pages contract and @notrelix/docs-state
interface PageDtoApi {
  id: string;
  workspaceId: string;
  parentId?: string | null;
  title: string;
  /** Gap `CTR-GAP-DOCS-PAGE-EXT`: the short name the tree and the breadcrumb use. */
  navTitle?: string;
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
  /** The Page aggregate version (`PageDto.version`). */
  version: number;
  /** Masked when the caller cannot see the Space (GOV-029). */
  spaceId?: string | null;
  /** Gap `CTR-GAP-DOCS-PAGE-EXT`: the review state and who is on the page now. */
  status?: "draft" | "review" | "published";
  activeUserIds?: string[];
  /** Names of the people on the page: who is on it, edited it or commented. */
  people?: { id: string; name: string }[];
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
    ...(p.navTitle ? { navTitle: p.navTitle } : {}),
    iconType: null,
    iconValue: p.icon ?? null,
    coverUrl: null,
    position: p.position ?? 0,
    depth: 0,
    isTemplate: false,
    isArchived: false,
    publishedAt: null,
    deadline: p.deadline ?? null,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt,
    version: p.version ?? 1,
    status: p.status ?? "draft",
    activeUserIds: p.activeUserIds ?? [],
    people: store ? peopleOf(p, store) : [],
  };
}

/** The people a page names (active, editors, commenters) with their names. */
function peopleOf(
  page: MockPageRecord,
  store: MockStore,
): { id: string; name: string }[] {
  const ids = new Set<string>([
    ...(page.activeUserIds ?? []),
    ...store.getPageHistory(page.id).map((entry) => entry.actorId),
    ...store.getPageComments(page.id).map((comment) => comment.userId),
  ]);
  return [...ids].flatMap((id) => {
    const user = store.getUser(id);
    return user ? [{ id, name: user.name }] : [];
  });
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
      return ok<PageDtoApi[]>(
        pages.map((page) => mapPageDto(page, store)),
      );
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
      return ok<PageDtoApi[]>(
        pages.map((page) => mapPageDto(page, store)),
      );
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
    string
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
      // Like the producer (`ToCreatedResult`): 201 with the new page id; the
      // client then reads the page (`@notrelix/docs-state` `create`).
      return created<string>(page.id);
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

  // ─── POST /pages/:pageId/move-to-space (GOV-028) ─────────────────────────

  defineMockOperation<
    { pageId: string },
    { spaceId?: string | null; expectedVersion?: number },
    void
  >({
    id: "pages.moveToSpace",
    contract: { kind: "openapi", operationId: "Documents.Pages.MoveToSpace" },
    method: "POST",
    route: "/pages/:pageId/move-to-space",
    async handle({ params, body, store }) {
      const page = store.getPage(params.pageId);
      if (!page || !store.isCurrentMemberOf(page.workspaceId))
        return notFound("Page not found");
      // UpdatePage on the source (guests never update pages here).
      if (store.getCurrentMemberRole(page.workspaceId) === "guest")
        return forbidden();
      const target = body?.spaceId ?? null;
      const denied = targetSpaceDenied(store, page.workspaceId, target);
      if (denied) return denied;
      switch (store.placePage(page.id, target, body?.expectedVersion)) {
        case "ok":
          return noContent();
        case "not-found":
          return notFound("Page not found");
        case "nested":
          // The producer's Documents_Page_NestedPageFollowsRootSpace rule.
          return {
            status: 400,
            body: {
              type: "https://docs.notrelix.com/problems/business_rule-violation",
              title: "Business rule violation",
              status: 400,
              detail: "A nested page follows the Space of its root page.",
              errorCode: "business_rule.violation",
            },
          };
        case "stale":
          return preconditionFailed();
      }
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
      // From the root page down to this one, by their navigation labels.
      const chain: MockPageRecord[] = [];
      for (
        let current: MockPageRecord | undefined = page;
        current && chain.length < 12;
        current = current.parentId ? store.getPage(current.parentId) : undefined
      ) {
        chain.unshift(current);
      }
      return ok<BreadcrumbDtoApi[]>(
        chain.map((entry) => ({
          id: entry.id,
          title: entry.navTitle ?? entry.title,
          iconType: null,
          iconValue: entry.icon ?? null,
        })),
      );
    },
  }),
];
