/**
 * Workspace context handlers.
 *
 * Operations with official contracts:
 *   workspace.list         — GET /workspaces
 *   workspace.create       — POST /workspaces
 *   workspace.get          — GET /workspaces/:id
 *
 * Operations with COMPATIBILITY-GAP (temporary legacy behavior):
 *   workspace.views.list   — GET /workspaces/:id/views   [CTR-GAP-WS-VIEWS]
 *   workspace.members.list — GET /workspaces/:id/members [CTR-GAP-WS-MEMBERS]
 *
 * Plan: 01-FREEZE-SPEC.md §FZ-S06, §FZ-S07, §FZ-S10, §FZ-S11
 *       02-IMPLEMENTATION-PLAN.md §MFB-FZ-03, §MFB-FZ-05
 */

import type {
  WorkspaceSummary,
  WorkspaceMember,
  WorkspaceView,
} from "@notrelix/features-workspace";
import { defineMockOperation } from "../../operations/types";
import {
  ok,
  created,
  noContent,
  notFound,
  validationError,
} from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";

const NAVIGATION_KINDS = new Set(["work-management.board", "documents.page"]);

/** Mirrors the producer: the resource must live in the caller's workspace. */
function navigationResourceExists(
  store: MockStore,
  workspaceId: string,
  kind: string,
  resourceId: string,
): boolean {
  if (kind === "work-management.board") {
    return store.getBoard(resourceId)?.workspaceId === workspaceId;
  }
  if (kind === "documents.page") {
    return store.getPage(resourceId)?.workspaceId === workspaceId;
  }
  return false;
}

export const workspaceOperations = [
  // ─── GET /workspaces ──────────────────────────────────────────────────────

  defineMockOperation<Record<string, never>, never, WorkspaceSummary[]>({
    id: "workspace.list",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/workspaces",
    async handle({ store }) {
      return ok(
        store.getWorkspaces().map((w) => ({
          id: w.id,
          name: w.name,
          slug: w.slug,
          plan: w.plan,
          icon: w.icon,
          memberCount: store.getWorkspaceMembers(w.id).length,
          isPersonal: w.isPersonal,
        })),
      );
    },
  }),

  // ─── POST /workspaces ─────────────────────────────────────────────────────

  defineMockOperation<
    Record<string, never>,
    { name?: string; slug?: string; isPersonal?: boolean },
    WorkspaceSummary
  >({
    id: "workspace.create",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/workspaces",
    async handle({ body, store }) {
      const data = (body ?? {}) as {
        name?: string;
        slug?: string;
        isPersonal?: boolean;
      };
      const { workspace } = store.createWorkspaceForCurrentUser({
        name: data.name,
        slug: data.slug,
        isPersonal: data.isPersonal,
      });
      return created<WorkspaceSummary>({
        id: workspace.id,
        name: workspace.name,
        slug: workspace.slug,
        plan: workspace.plan,
        icon: workspace.icon,
        memberCount: store.getWorkspaceMembers(workspace.id).length,
        isPersonal: workspace.isPersonal,
      });
    },
  }),

  // ─── GET /workspaces/:id ──────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, WorkspaceSummary>({
    id: "workspace.get",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/workspaces/:id",
    async handle({ params, store }) {
      const w = store.getWorkspace(params.id);
      if (!w) return notFound(`Workspace "${params.id}" not found`);
      return ok<WorkspaceSummary>({
        id: w.id,
        name: w.name,
        slug: w.slug,
        plan: w.plan,
        icon: w.icon,
        memberCount: store.getWorkspaceMembers(w.id).length,
        isPersonal: w.isPersonal,
      });
    },
  }),

  // ─── GET /workspaces/:id/views ────────────────────────────────────────────
  // COMPATIBILITY-GAP: CTR-GAP-WS-VIEWS
  // Temporary legacy handler — remove when official producer contract lands.

  defineMockOperation<{ id: string }, never, WorkspaceView[]>({
    id: "workspace.views.list",
    contract: { kind: "gap", gapId: "CTR-GAP-WS-VIEWS" } as any,
    method: "GET",
    route: "/workspaces/:id/views",
    async handle({ params, store }) {
      const records = store.getWorkspaceViews(params.id);
      return ok<WorkspaceView[]>(
        records.map((v) => ({
          id: v.id,
          workspaceId: v.workspaceId,
          name: v.name,
          type: v.type,
          icon: v.icon,
          description: v.description,
          target: {},
          config: {},
          visibility: v.visibility,
          isDefault: v.isDefault,
          position: v.position,
          createdAt: v.createdAt,
        })),
      );
    },
  }),

  // ─── GET /workspaces/:id/members ──────────────────────────────────────────
  // COMPATIBILITY-GAP: CTR-GAP-WS-MEMBERS
  // Temporary legacy handler — resolves actual user records for memberships.

  defineMockOperation<{ id: string }, never, WorkspaceMember[]>({
    id: "workspace.members.list",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.ListMembers",
    } as any,
    method: "GET",
    route: "/workspaces/:id/members",
    async handle({ params, store }) {
      const memberRecords = store.getWorkspaceMembers(params.id);
      return ok<WorkspaceMember[]>(
        memberRecords.map((m) => {
          const user = store.getUser(m.userId);
          const name = user ? user.name : `User ${m.userId}`;
          const initials = name
            .split(" ")
            .map((part) => part[0])
            .join("")
            .slice(0, 2)
            .toUpperCase();

          return {
            id: m.id,
            userId: m.userId,
            name,
            initials: initials || "U",
            role: m.role,
            status: m.status,
            workload: m.workload,
            color: m.color,
            avatarUrl: user?.avatarUrl ?? undefined,
          };
        }),
      );
    },
  }),

  defineMockOperation<{ workspaceId: string; userId: string }, never, void>({
    id: "workspace.members.remove",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.RemoveMember",
    } as any,
    method: "DELETE",
    route: "/workspaces/:workspaceId/members/:userId",
    async handle({ store: _store }) {
      return ok<void>(undefined);
    },
  }),

  defineMockOperation<
    { workspaceId: string; userId: string },
    { role: string },
    void
  >({
    id: "workspace.members.update",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.UpdateMemberRole",
    } as any,
    method: "PATCH", // the OpenAPI is PATCH for UpdateMemberRole, but the client does PUT? Wait, I will use PATCH as per OpenAPI
    route: "/workspaces/:workspaceId/members/:userId",
    async handle({ store: _store }) {
      return ok<void>(undefined);
    },
  }),

  // ─── Navigation state (WX-BE-001/002) ─────────────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "workspace.navigation.recents.list",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Navigation.ListRecents",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/recents",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(
        store.getRecentResources(params.workspaceId).map((entry) => ({
          kind: entry.kind,
          resourceId: entry.resourceId,
          lastOpenedAt: entry.at,
        })),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { kind?: string; resourceId?: string },
    void
  >({
    id: "workspace.navigation.recents.record",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Navigation.RecordRecent",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/recents",
    async handle({ params, body, store, request }) {
      if (!request.headers.get("Idempotency-Key")) {
        return validationError({
          "Idempotency-Key": ["The header is required."],
        });
      }
      const kind = body?.kind ?? "";
      const resourceId = body?.resourceId ?? "";
      if (!NAVIGATION_KINDS.has(kind)) {
        return validationError({ kind: ["Unsupported resource kind."] });
      }
      if (
        !store.isCurrentMemberOf(params.workspaceId) ||
        !navigationResourceExists(store, params.workspaceId, kind, resourceId)
      ) {
        return notFound();
      }
      store.recordRecentResource(params.workspaceId, kind, resourceId);
      return noContent();
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "workspace.navigation.pins.list",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Navigation.ListPins",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/pins",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(
        store.getPinnedResources(params.workspaceId).map((entry) => ({
          kind: entry.kind,
          resourceId: entry.resourceId,
          pinnedAt: entry.at,
        })),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string; kind: string; resourceId: string },
    never,
    void
  >({
    id: "workspace.navigation.pins.pin",
    contract: { kind: "openapi", operationId: "Workspaces.Navigation.Pin" },
    method: "PUT",
    route: "/workspaces/:workspaceId/pins/:kind/:resourceId",
    async handle({ params, store }) {
      const kind = decodeURIComponent(params.kind);
      if (!NAVIGATION_KINDS.has(kind)) {
        return validationError({ kind: ["Unsupported resource kind."] });
      }
      if (
        !store.isCurrentMemberOf(params.workspaceId) ||
        !navigationResourceExists(
          store,
          params.workspaceId,
          kind,
          params.resourceId,
        )
      ) {
        return notFound();
      }
      if (!store.pinResource(params.workspaceId, kind, params.resourceId)) {
        return {
          status: 422,
          body: {
            message: "Pin limit reached",
            code: "Workspaces_Navigation_PinLimitReached",
          },
        };
      }
      return noContent();
    },
  }),

  defineMockOperation<
    { workspaceId: string; kind: string; resourceId: string },
    never,
    void
  >({
    id: "workspace.navigation.pins.unpin",
    contract: { kind: "openapi", operationId: "Workspaces.Navigation.Unpin" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/pins/:kind/:resourceId",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      store.unpinResource(
        params.workspaceId,
        decodeURIComponent(params.kind),
        params.resourceId,
      );
      return noContent();
    },
  }),
];
