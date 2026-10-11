/**
 * Team projections (Workspaces owner; GOV-026).
 *
 * Client projections of the Team invariants: a Team always keeps one Lead,
 * an archived Team accepts no membership edits, and only workspace admins
 * manage Teams. The producer still decides; its errors are shown as returned.
 */

import type { WorkspaceMember, WorkspaceRole } from "../types/workspace";
import { isWorkspaceAdmin } from "./members";

export const TEAM_ROLES = ["Lead", "Member"] as const;
export type TeamRole = (typeof TEAM_ROLES)[number];

export type TeamStatus = "Active" | "Archived";

export interface Team {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly status: TeamStatus;
  readonly memberCount: number;
  /** Concurrency token echoed back as `expectedVersion` on every write. */
  readonly version: number;
  readonly createdAt: string;
}

export interface TeamMember {
  readonly userId: string;
  readonly displayName: string;
  /** The producer sends null today; the web fills it from workspace members. */
  readonly email: string | null;
  readonly role: TeamRole;
}

export function parseTeamRole(value: unknown): TeamRole | null {
  return (TEAM_ROLES as readonly unknown[]).includes(value)
    ? (value as TeamRole)
    : null;
}

interface RawTeam {
  id?: string;
  name?: string | null;
  description?: string | null;
  status?: string | null;
  memberCount?: number;
  version?: number;
  createdAt?: string;
}

interface RawTeamMember {
  userId?: string;
  displayName?: string | null;
  email?: string | null;
  role?: string | null;
}

/** Callers drop rows without an id; this maps one row that has it. */
export function parseTeam(raw: unknown): Team {
  const row = (raw ?? {}) as RawTeam;
  return {
    id: row.id ?? "",
    name: row.name ?? "",
    description: row.description ?? null,
    status: row.status === "Archived" ? "Archived" : "Active",
    memberCount: typeof row.memberCount === "number" ? row.memberCount : 0,
    version: typeof row.version === "number" ? row.version : 0,
    createdAt: row.createdAt ?? "",
  };
}

/** An unknown role degrades to the least-privileged one, never to Lead. */
export function parseTeamMember(raw: unknown): TeamMember {
  const row = (raw ?? {}) as RawTeamMember;
  return {
    userId: row.userId ?? "",
    displayName: row.displayName ?? "",
    email: row.email ?? null,
    role: parseTeamRole(row.role) ?? "Member",
  };
}

export function countLeads(members: readonly TeamMember[]): number {
  return members.filter((member) => member.role === "Lead").length;
}

export type TeamBlock = "archived" | "not-admin";

export interface TeamCapabilities {
  /** Archive and unarchive (the only writes an archived Team accepts). */
  readonly canManage: boolean;
  /** Rename and describe; the Domain rejects both on an archived Team. */
  readonly canEditDetails: boolean;
  /** Add, remove and change roles of Team members. */
  readonly canEditMembers: boolean;
  readonly blockedBy: TeamBlock | null;
}

/**
 * Workspace admins manage Teams. An archived Team stays manageable (it can
 * be unarchived) but its name, description and membership are frozen.
 */
export function teamCapabilities(
  team: Team,
  viewerRole: WorkspaceRole | null,
): TeamCapabilities {
  if (!isWorkspaceAdmin(viewerRole)) {
    return {
      canManage: false,
      canEditDetails: false,
      canEditMembers: false,
      blockedBy: "not-admin",
    };
  }
  if (team.status === "Archived") {
    return {
      canManage: true,
      canEditDetails: false,
      canEditMembers: false,
      blockedBy: "archived",
    };
  }
  return {
    canManage: true,
    canEditDetails: true,
    canEditMembers: true,
    blockedBy: null,
  };
}

export type TeamMemberBlock = "last-lead" | TeamBlock;

export interface TeamMemberCapabilities {
  readonly roles: readonly TeamRole[];
  readonly canRemove: boolean;
  readonly blockedBy: TeamMemberBlock | null;
}

/**
 * The last Lead cannot be removed or demoted; the reason is shown instead of
 * offering the action. `canManage` is the viewer's membership-edit right.
 */
export function teamMemberCapabilities(
  member: TeamMember,
  members: readonly TeamMember[],
  team: Team,
  canManage: boolean,
): TeamMemberCapabilities {
  if (team.status === "Archived") {
    return { roles: [], canRemove: false, blockedBy: "archived" };
  }
  if (!canManage) {
    return { roles: [], canRemove: false, blockedBy: "not-admin" };
  }
  if (member.role === "Lead" && countLeads(members) <= 1) {
    return { roles: ["Lead"], canRemove: false, blockedBy: "last-lead" };
  }
  return { roles: TEAM_ROLES, canRemove: true, blockedBy: null };
}

/** Active workspace members who are not yet in the Team. */
export function addableTeamMembers(
  workspaceMembers: readonly WorkspaceMember[],
  members: readonly TeamMember[],
): WorkspaceMember[] {
  const present = new Set(members.map((member) => member.userId));
  return workspaceMembers.filter(
    (member) =>
      member.membership !== "suspended" && !present.has(member.userId),
  );
}
