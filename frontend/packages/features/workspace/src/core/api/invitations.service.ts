import type { OperationRequestBody } from "@notrelix/contracts";
import { parseWorkspaceRole } from "../model/members";
import type {
  CreateWorkspaceInvitationInput,
  WorkspaceInvitation,
} from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

export interface InvitationsEndpoints {
  workspaces: {
    invitationByToken: (token: string) => string;
    acceptInvitation: (token: string) => string;
    pendingInvitations: string;
  };
}

/** Workspace-scoped invitation endpoints (WX-MEM-001). */
export interface WorkspaceInvitationsEndpoints {
  workspaces: {
    /** `Workspaces.Members.InviteMember` (POST). */
    members: (workspaceId: string) => string;
    /** `Workspaces.Invitations.ListWorkspaceInvitations` (GET). */
    invitations: (workspaceId: string) => string;
    /** `Workspaces.Invitations.CancelInvitation` (DELETE). */
    invitation: (workspaceId: string, invitationId: string) => string;
  };
}

/**
 * `ListWorkspaceInvitations` returns `WorkspaceInvitationDto[]` (`id`,
 * `email`, `role` as the enum name, `expiresAt`, `isAccepted`, `createdAt`);
 * the generated contract types it as `System.Void`, so rows are parsed
 * defensively. A `status` field is read when the producer adds one (revoked
 * invitations are otherwise indistinguishable from pending ones).
 */
interface InvitationWire {
  readonly id?: unknown;
  readonly email?: unknown;
  readonly role?: unknown;
  readonly expiresAt?: unknown;
  readonly isAccepted?: unknown;
  readonly createdAt?: unknown;
  readonly status?: unknown;
}

export function parseInvitations(payload: unknown): WorkspaceInvitation[] {
  if (!Array.isArray(payload)) return [];
  const out: WorkspaceInvitation[] = [];
  for (const raw of payload as InvitationWire[]) {
    const role = parseWorkspaceRole(raw?.role);
    if (
      typeof raw?.id !== "string" ||
      typeof raw.email !== "string" ||
      typeof raw.expiresAt !== "string" ||
      typeof raw.createdAt !== "string" ||
      !role
    )
      continue;
    const status =
      typeof raw.status === "string" ? raw.status.toLowerCase() : null;
    if (status === "revoked" || status === "declined") continue;
    out.push({
      id: raw.id,
      email: raw.email,
      role,
      expiresAt: raw.expiresAt,
      createdAt: raw.createdAt,
      isAccepted: raw.isAccepted === true || status === "accepted",
      isExpired: status === "expired" ? true : undefined,
    });
  }
  return out;
}

type InviteBody = OperationRequestBody<"Workspaces.Members.InviteMember">;

const ROLE_NAME = {
  admin: "Admin",
  member: "Member",
  guest: "Guest",
} as const;

export function createInvitationsService(
  api: WorkspaceApiClient,
  endpoints: InvitationsEndpoints,
) {
  return {
    async getByToken(token: string): Promise<WorkspaceInvitation> {
      return api.get<WorkspaceInvitation>(
        endpoints.workspaces.invitationByToken(token),
      );
    },

    async accept(token: string): Promise<void> {
      return api.post<void>(endpoints.workspaces.acceptInvitation(token), {});
    },

    async getPending(): Promise<WorkspaceInvitation[]> {
      return api.get<WorkspaceInvitation[]>(
        endpoints.workspaces.pendingInvitations,
      );
    },
  };
}

/**
 * The workspace's invitations: list, invite and revoke
 * (`Workspaces.Invitations.*`, `Workspaces.Members.InviteMember`).
 */
export function createWorkspaceInvitationsService(
  api: WorkspaceApiClient,
  endpoints: WorkspaceInvitationsEndpoints,
) {
  return {
    /** The workspace's invitations (all states the producer lists). */
    async getList(workspaceId: string): Promise<WorkspaceInvitation[]> {
      return parseInvitations(
        await api.get<unknown>(endpoints.workspaces.invitations(workspaceId)),
      );
    },

    /**
     * Invites by e-mail (`POST …/members`). Returns the new invitation id.
     * Not idempotent on the producer: a second pending invitation to the same
     * address is rejected, so a retry cannot create a duplicate.
     */
    async create(
      workspaceId: string,
      input: CreateWorkspaceInvitationInput,
    ): Promise<string | null> {
      const body: InviteBody = {
        email: input.email.trim(),
        role: ROLE_NAME[input.role],
      };
      const id = await api.post<unknown>(
        endpoints.workspaces.members(workspaceId),
        body,
      );
      return typeof id === "string" ? id : null;
    },

    /** Revokes a pending invitation (an expired one is marked expired). */
    async revoke(workspaceId: string, invitationId: string): Promise<void> {
      await api.delete<void>(
        endpoints.workspaces.invitation(workspaceId, invitationId),
      );
    },
  };
}

export type WorkspaceInvitationsService = ReturnType<
  typeof createWorkspaceInvitationsService
>;
