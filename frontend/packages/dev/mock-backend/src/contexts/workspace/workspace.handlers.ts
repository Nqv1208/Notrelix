/**
 * Workspace context handlers.
 *
 * Operations with official contracts:
 *   workspace.list         — GET /workspaces
 *   workspace.create       — POST /workspaces
 *   workspace.get          — GET /workspaces/:id
 *
 * Operations with CONTRACT-BLOCKED gaps (temporary legacy behavior):
 *   workspace.views.list   — GET /workspaces/:id/views   [CTR-GAP-WS-VIEWS]
 *   workspace.members.list — GET /workspaces/:id/members [CTR-GAP-WS-MEMBERS]
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md, 10-CONTRACT-GAP-REGISTER.md
 */

import type {
  WorkspaceSummary,
  WorkspaceMember,
  WorkspaceView,
} from "@notrelix/features-workspace";
import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";
import { mockIds } from "../../state/mock-ids";

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

  defineMockOperation<Record<string, never>, { name?: string; slug?: string; isPersonal?: boolean }, WorkspaceSummary>({
    id: "workspace.create",
    method: "POST",
    route: "/workspaces",
    async handle({ body, store }) {
      const data = (body ?? {}) as { name?: string; slug?: string; isPersonal?: boolean };
      const newId = `ws-${store.getWorkspaces().length + 1}`;
      const ws = {
        id: newId,
        name: data.name ?? "New Workspace",
        slug: data.slug ?? newId,
        plan: "free" as const,
        icon: "Layout",
        isPersonal: data.isPersonal ?? false,
      };
      store.addWorkspace(ws);
      return created<WorkspaceSummary>({ ...ws, memberCount: 1 });
    },
  }),

  // ─── GET /workspaces/:id ──────────────────────────────────────────────────

  defineMockOperation<{ id: string }, never, WorkspaceSummary>({
    id: "workspace.get",
    method: "GET",
    route: "/workspaces/:id",
    async handle({ params, store }) {
      const w = store.getWorkspace(params.id) ?? store.getWorkspaces()[0];
      if (!w) return notFound("Workspace not found");
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
  // CONTRACT-BLOCKED: CTR-GAP-WS-VIEWS
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
  // CONTRACT-BLOCKED: CTR-GAP-WS-MEMBERS
  // Temporary legacy handler — remove when official producer contract lands.

  defineMockOperation<{ id: string }, never, WorkspaceMember[]>({
    id: "workspace.members.list",
    method: "GET",
    route: "/workspaces/:id/members",
    async handle({ params, store }) {
      const memberRecords = store.getWorkspaceMembers(params.id);
      const currentUser = store.getCurrentUser();
      return ok<WorkspaceMember[]>(
        memberRecords.map((m) => {
          const isOwner = m.userId === mockIds.users.owner;
          return {
            id: m.id,
            userId: m.userId,
            name: isOwner ? currentUser.name : "Alex Rivera",
            initials: isOwner ? currentUser.name.substring(0, 2).toUpperCase() : "AR",
            role: m.role,
            status: m.status,
            workload: m.workload,
            color: m.color,
          };
        }),
      );
    },
  }),
];
