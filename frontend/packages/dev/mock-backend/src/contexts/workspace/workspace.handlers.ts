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
import { ok, created, notFound } from "../../transport/create-response";

export const workspaceOperations = [
  // ─── GET /workspaces ──────────────────────────────────────────────────────

  defineMockOperation<Record<string, never>, never, WorkspaceSummary[]>({
    id: "workspace.list",
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
];
