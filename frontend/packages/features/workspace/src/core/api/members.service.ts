import {
  resolveApiAssetUrl,
  type OperationRequestBody,
  type OperationResponse,
} from "@notrelix/contracts";
import { initialsFor } from "../model/navigation";
import { parseWorkspaceRole, roleWireValue } from "../model/members";
import type {
  WorkspaceMember,
  WorkspaceMembershipStatus,
  WorkspaceRole,
} from "../types/workspace";
import type { WorkspaceApiClient } from "./workspace.service";

/**
 * `Workspaces.Members.ListMembers` returns a cursor page of `WorkspaceMemberDto`
 * (`userId`, `name`, `avatar`, `role` and `status` as enum names, `joinedAt`,
 * `jobTitle`). Removed members are not listed by the producer; the adapter still
 * drops rows without an id or a known role, and any row marked removed.
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
  readonly jobTitle?: unknown;
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
      avatarUrl: resolveApiAssetUrl(avatar) ?? undefined,
      joinedAt: typeof raw.joinedAt === "string" ? raw.joinedAt : undefined,
      jobTitle:
        typeof raw.jobTitle === "string" && raw.jobTitle.trim()
          ? raw.jobTitle
          : undefined,
      membership: parseMembership(raw.status),
      initials: initialsFor(name),
    });
  }
  return members;
}

type UpdateRoleBody =
  OperationRequestBody<"Workspaces.Members.UpdateMemberRole">;

/** `Workspaces.Members.ListMembers`: `items` and `nextCursor`. */
type MembersPageDto = OperationResponse<"Workspaces.Members.ListMembers", 200>;

/**
 * `Workspaces.Members.GetMyMembership` returns one `WorkspaceMemberDto`: the
 * caller's own row, readable by every member including a Guest.
 */
export function parseMyMembership(payload: unknown): WorkspaceMember | null {
  return parseMembers([payload])[0] ?? null;
}

/** The most the producer returns per page. */
const MEMBERS_PAGE_SIZE = 200;
/** A guard against a cursor that never ends: 100 pages is 20,000 people. */
const MAX_MEMBER_PAGES = 100;

/** One page of `Workspaces.Members.ListMembers`: the rows and where the next page starts. */
function parseMembersPage(payload: unknown): {
  readonly items: readonly unknown[];
  readonly nextCursor: string | null;
} {
  const page = (payload ?? {}) as Partial<MembersPageDto>;
  return {
    items: Array.isArray(page.items) ? page.items : [],
    nextCursor:
      typeof page.nextCursor === "string" && page.nextCursor
        ? page.nextCursor
        : null,
  };
}

/** Adapter for `Workspaces.Members.*` (paths follow the producer). */
export function createMembersService(api: WorkspaceApiClient) {
  const base = (workspaceId: string) => `/workspaces/${workspaceId}/members`;
  return {
    /**
     * Everyone in the workspace, read a page at a time: the shell, the Board
     * share dialog and the Space and Team pages resolve names from the whole
     * roster, so the pages are joined here and each request stays bounded.
     */
    async getList(workspaceId: string): Promise<WorkspaceMember[]> {
      const rows: unknown[] = [];
      let cursor: string | null = null;
      for (let pages = 0; pages < MAX_MEMBER_PAGES; pages += 1) {
        const query = new URLSearchParams({ limit: String(MEMBERS_PAGE_SIZE) });
        if (cursor) query.set("cursor", cursor);
        const page = parseMembersPage(
          await api.get<unknown>(`${base(workspaceId)}?${query}`),
        );
        rows.push(...page.items);
        cursor = page.nextCursor;
        if (!cursor) break;
      }
      return parseMembers(rows);
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
