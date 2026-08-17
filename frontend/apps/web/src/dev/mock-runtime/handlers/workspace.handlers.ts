import { endpoints } from "@notrelix/contracts";
import { AppError } from "@notrelix/kernel";
import type { CreateWorkspaceInput, UpdateWorkspaceInput } from "@notrelix/features-workspace";
import type { MockHandler } from "../transport/mock-handler";

export const workspaceHandlers: readonly MockHandler[] = [
  {
    id: "workspace.invitation.by-token",
    matches: (request) => request.method === "GET" && /^\/workspaces\/invitations\/by-token\/[^/]+$/.test(request.url),
    async handle(request, context) {
      const token = request.url.split("/").at(-1);
      const invitation = context.store.getSnapshot().invitations.find((candidate) => candidate.token === token);
      if (!invitation) throw new AppError({ kind: "not_found", status: 404, message: "Invitation not found." });
      return invitation;
    },
  },
  {
    id: "workspace.invitation.pending",
    matches: (request) => request.method === "GET" && request.url === endpoints.workspaces.pendingInvitations,
    async handle(_request, context) { return context.store.getSnapshot().invitations.filter(({ isAccepted }) => !isAccepted); },
  },
  {
    id: "workspace.invitation.accept",
    matches: (request) => request.method === "POST" && /^\/workspaces\/invitations\/accept\/[^/]+$/.test(request.url),
    async handle(request, context) { const token = request.url.split("/").at(-1); context.store.update((draft) => { const invitation = draft.invitations.find((candidate) => candidate.token === token); if (invitation) invitation.isAccepted = true; }); },
  },
  {
    id: "workspace.list",
    matches: (request) => request.method === "GET" && request.url === endpoints.workspaces.list,
    async handle(_request, context) {
      return context.store.getVisibleWorkspaces();
    },
  },
  {
    id: "workspace.create",
    matches: (request) => request.method === "POST" && request.url === endpoints.workspaces.list,
    async handle(request, context) {
      const input = request.body as CreateWorkspaceInput;
      const id = context.store.nextWorkspaceId();
      const workspace = { id, name: input.name, slug: input.slug, icon: "Layout", plan: "free" as const, memberCount: 1, isPersonal: input.isPersonal };
      const userId = context.store.getCurrentUser().id;
      context.store.update((draft) => {
        draft.workspaces.push(workspace);
        draft.memberships.push({ userId, workspaceId: id, role: "owner" });
      });
      return workspace;
    },
  },
  {
    id: "workspace.detail",
    matches: (request) => request.method === "GET" && /^\/workspaces\/[^/]+$/.test(request.url),
    async handle(request, context) {
      const id = request.url.slice("/workspaces/".length);
      const workspace = context.store.getVisibleWorkspaces().find((candidate) => candidate.id === id);
      if (!workspace) throw new AppError({ kind: "not_found", status: 404, message: "Workspace not found." });
      return workspace;
    },
  },
  {
    id: "workspace.update",
    matches: (request) => request.method === "PATCH" && /^\/workspaces\/[^/]+$/.test(request.url),
    async handle(request, context) {
      const id = request.url.slice("/workspaces/".length);
      const input = request.body as UpdateWorkspaceInput;
      context.store.update((draft) => {
        const workspace = draft.workspaces.find((candidate) => candidate.id === id);
        if (!workspace) throw new AppError({ kind: "not_found", status: 404, message: "Workspace not found." });
        Object.assign(workspace, input);
      });
      return context.store.getVisibleWorkspaces().find((candidate) => candidate.id === id);
    },
  },
];
