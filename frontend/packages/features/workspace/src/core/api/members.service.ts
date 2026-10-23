import type { OperationRequestBody } from "@notrelix/contracts";
import { initialsFor } from "../model/navigation";
import { parseWorkspaceRole, roleWireValue } from "../model/members";
import type {
  WorkspaceMember,
  WorkspaceMembershipStatus,
  WorkspaceRole,
} from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

/**
 * `Workspaces.Members.ListMembers` returns `WorkspaceMemberDto[]` (`userId`,
 * `name`, `avatar`, `role` and `status` as enum names, `joinedAt`). Removed
 * members are not listed by the producer; the adapter still drops rows without
 * an id or a known role, and any row marked removed.
 */
interface MemberWire {
  readonly userId?: unknown;
  readonly id?: unknown;
  readonly name?: unknown;
  readonly avatar?: unknown;
  readonly avatarUrl?: unknown;
  readonly role?: unknown;
  readonly joinedAt?: unknown;
  readonly status?: unknown;
}

function parseMembership(
  value: unknown,
): WorkspaceMembershipStatus | undefined {
  if (typeof value !== "string") return undefined;
  const lower = value.toLowerCase();
  return lower === "active" || lower === "suspended" ? lower : undefined;
}

export function parseMembers(payload: unknown): WorkspaceMember[] {
  if (!Array.isArray(payload)) return [];
  const members: WorkspaceMember[] = [];
  const seen = new Set<string>();
  for (const raw of payload as MemberWire[]) {
    const userId =
      typeof raw?.userId === "string"
        ? raw.userId
        : typeof raw?.id === "string"
          ? raw.id
          : null;
    const role = parseWorkspaceRole(raw?.role);
    // Removed members are not listed as members (WSP lifecycle).
    if (
      !userId ||
      !role ||
      seen.has(userId) ||
      (typeof raw.status === "string" && raw.status.toLowerCase() === "removed")
    )
      continue;
    seen.add(userId);
    const name =
      typeof raw.name === "string" && raw.name.trim() ? raw.name : userId;
    const avatar =
      typeof raw.avatar === "string"
        ? raw.avatar
        : typeof raw.avatarUrl === "string"
          ? raw.avatarUrl
          : undefined;
    members.push({
      id: userId,
      userId,
      name,
      role,
      avatarUrl: avatar || undefined,
      joinedAt: typeof raw.joinedAt === "string" ? raw.joinedAt : undefined,
      membership: parseMembership(raw.status),
      initials: initialsFor(name),
    });
  }
  return members;
}

type UpdateRoleBody =
  OperationRequestBody<"Workspaces.Members.UpdateMemberRole">;

/**
 * `Workspaces.Members.GetMyMembership` returns one `WorkspaceMemberDto`: the
 * caller's own row, readable by every member including a Guest.
 */
export function parseMyMembership(payload: unknown): WorkspaceMember | null {
  return parseMembers([payload])[0] ?? null;
}

/** Adapter for `Workspaces.Members.*` (paths follow the producer). */
export function createMembersService(api: WorkspaceApiClient) {
  const base = (workspaceId: string) => `/workspaces/${workspaceId}/members`;
  return {
    async getList(workspaceId: string): Promise<WorkspaceMember[]> {
      return parseMembers(await api.get<unknown>(base(workspaceId)));
    },

    /** GET …/members/me: the caller's own row; the only member read a Guest may make. */
    async getMine(workspaceId: string): Promise<WorkspaceMember | null> {
      return parseMyMembership(
        await api.get<unknown>(`${base(workspaceId)}/me`),
      );
    },

    /** PATCH …/members/{userId} with the producer's role enum value. */
    async updateRole(
      workspaceId: string,
      userId: string,
      role: WorkspaceRole,
    ): Promise<void> {
      const body: UpdateRoleBody = { role: roleWireValue(role) };
      await api.patch<void>(`${base(workspaceId)}/${userId}`, body);
    },

    async removeMember(workspaceId: string, userId: string): Promise<void> {
      await api.delete<void>(`${base(workspaceId)}/${userId}`);
    },

    /** Idempotent on the producer: suspending a suspended member is a no-op. */
    async suspend(workspaceId: string, userId: string): Promise<void> {
      await api.post<void>(`${base(workspaceId)}/${userId}/suspend`, undefined);
    },

    /** Idempotent on the producer: activating an active member is a no-op. */
    async activate(workspaceId: string, userId: string): Promise<void> {
      await api.post<void>(
        `${base(workspaceId)}/${userId}/activate`,
        undefined,
      );
    },
  };
}

export type MembersService = ReturnType<typeof createMembersService>;
