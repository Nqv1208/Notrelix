import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { parseWorkspaceRole, roleWireValue } from "../model/members";
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
    /** `Workspaces.Invitations.ChangeInvitationRole` (PATCH). */
    invitationRole: (workspaceId: string, invitationId: string) => string;
    /** `Workspaces.Invitations.ResendInvitation` (POST). */
    resendInvitation: (workspaceId: string, invitationId: string) => string;
  };
}

/**
 * `ListWorkspaceInvitations` returns a cursor page of `WorkspaceInvitationDto`
 * (`id`, `email`, `role` and `status` as enum names, `expiresAt`, `isAccepted`,
 * `createdAt`, `version`). Revoked and declined invitations are dropped;
 * accepted and expired ones are kept with their state.
 */
interface InvitationWire {
  readonly id?: unknown;
  readonly email?: unknown;
  readonly role?: unknown;
  readonly expiresAt?: unknown;
  readonly isAccepted?: unknown;
  readonly createdAt?: unknown;
  readonly status?: unknown;
  readonly version?: unknown;
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
      version:
        typeof raw.version === "number" &&
        Number.isInteger(raw.version) &&
        raw.version >= 0
          ? raw.version
          : undefined,
    });
  }
  return out;
}

type InviteBody = OperationRequestBody<"Workspaces.Members.InviteMember">;
type ChangeRoleBody =
  OperationRequestBody<"Workspaces.Invitations.ChangeInvitationRole">;
type InvitationsPageDto = OperationResponse<
  "Workspaces.Invitations.ListWorkspaceInvitations",
  200
>;

/** The most the producer returns per page, and a guard against a cursor that never ends. */
const INVITATIONS_PAGE_SIZE = 200;
const MAX_INVITATION_PAGES = 100;

/** The producer's stable code for a second pending invitation to one address. */
export const PENDING_INVITATION_EXISTS_CODE =
  "workspaces.invitation.pending-exists";

const ROLE_NAME = {
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
    /**
     * The workspace's invitations (all states the producer lists), read a page at a time and joined:
     * the page shows every waiting invitation, so each request stays bounded.
     */
    async getList(workspaceId: string): Promise<WorkspaceInvitation[]> {
      const rows: unknown[] = [];
      let cursor: string | null = null;
      for (let pages = 0; pages < MAX_INVITATION_PAGES; pages += 1) {
        const query = new URLSearchParams({
          limit: String(INVITATIONS_PAGE_SIZE),
        });
        if (cursor) query.set("cursor", cursor);
        const page = (await api.get<unknown>(
          `${endpoints.workspaces.invitations(workspaceId)}?${query}`,
        )) as Partial<InvitationsPageDto> | null;
        if (Array.isArray(page?.items)) rows.push(...page.items);
        cursor =
          typeof page?.nextCursor === "string" && page.nextCursor
            ? page.nextCursor
            : null;
        if (!cursor) break;
      }
      return parseInvitations(rows);
    },

    /**
     * Invites by e-mail (`POST …/members`). Returns the new invitation id. The same
     * `idempotencyKey` replays the first attempt, so a retry never sends a second email; a second
     * pending invitation to the same address answers 409 with `PENDING_INVITATION_EXISTS_CODE`.
     */
    async create(
      workspaceId: string,
      input: CreateWorkspaceInvitationInput,
      idempotencyKey?: string,
    ): Promise<string | null> {
      const body: InviteBody = {
        email: input.email.trim(),
        role: ROLE_NAME[input.role],
      };
      const id = await api.post<unknown>(
        endpoints.workspaces.members(workspaceId),
        body,
        idempotencyKey ? { idempotencyKey } : undefined,
      );
      return typeof id === "string" ? id : null;
    },

    /**
     * Changes the role a waiting invitation offers (`PATCH …/invitations/{id}/role`), quoting the
     * version it was read at. An invitation with no known version is never written.
     */
    async changeRole(
      workspaceId: string,
      invitationId: string,
      role: CreateWorkspaceInvitationInput["role"],
      expectedVersion: number | undefined,
    ): Promise<void> {
      if (expectedVersion === undefined)
        throw new Error("The invitation version is not known");
      const body: ChangeRoleBody = {
        newRole: roleWireValue(role),
        expectedVersion,
      };
      await api.patch<unknown>(
        endpoints.workspaces.invitationRole(workspaceId, invitationId),
        body,
      );
    },

    /**
     * Sends the invitation again with a new link and a new expiry
     * (`POST …/invitations/{id}/resend`); the invitation keeps its id.
     */
    async resend(workspaceId: string, invitationId: string): Promise<void> {
      await api.post<unknown>(
        endpoints.workspaces.resendInvitation(workspaceId, invitationId),
        {},
      );
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
