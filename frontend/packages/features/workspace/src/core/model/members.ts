/**
 * Member and invitation projections (WX-MEM-001).
 *
 * Client projections of the Workspaces owner invariants (WSP-008): the UI
 * never offers demoting, suspending or removing the last owner, and never
 * offers assigning Owner directly (ownership moves through the transfer
 * workflow). The producer still decides; its errors are shown as returned.
 */

import type {
  WorkspaceInvitation,
  WorkspaceInvitationState,
  WorkspaceMember,
  WorkspaceRole,
} from "../types/workspace";

export const WORKSPACE_ROLES: readonly WorkspaceRole[] = [
  "owner",
  "admin",
  "member",
  "guest",
];

/** Roles an admin can assign in place (Owner moves by transfer only). */
export const ASSIGNABLE_ROLES: readonly Exclude<WorkspaceRole, "owner">[] = [
  "admin",
  "member",
  "guest",
];

/** The producer's role enum order (`Notrelix.Domain.Workspaces.Members.WorkspaceRole`). */
const ROLE_WIRE_VALUE: Readonly<Record<WorkspaceRole, 0 | 1 | 2 | 3>> = {
  guest: 0,
  member: 1,
  admin: 2,
  owner: 3,
};

export function roleWireValue(role: WorkspaceRole): 0 | 1 | 2 | 3 {
  return ROLE_WIRE_VALUE[role];
}

/** Parses a role name or enum value from the producer ("Owner", 3). */
export function parseWorkspaceRole(value: unknown): WorkspaceRole | null {
  if (typeof value === "number") {
    const match = WORKSPACE_ROLES.find(
      (role) => ROLE_WIRE_VALUE[role] === value,
    );
    return match ?? null;
  }
  if (typeof value !== "string") return null;
  const lower = value.trim().toLowerCase();
  return (WORKSPACE_ROLES as readonly string[]).includes(lower)
    ? (lower as WorkspaceRole)
    : null;
}

export function isWorkspaceAdmin(role: WorkspaceRole | null): boolean {
  return role === "owner" || role === "admin";
}

/** Owners who still hold the role actively (suspended owners do not count). */
export function countOwners(members: readonly WorkspaceMember[]): number {
  return members.filter(
    (member) => member.role === "owner" && member.membership !== "suspended",
  ).length;
}

export type MemberActionBlock =
  "not-admin" | "self" | "last-owner" | "owner-by-transfer" | "suspended";

export interface MemberCapabilities {
  /** Roles the viewer may pick for this member (empty = read-only). */
  readonly assignableRoles: readonly WorkspaceRole[];
  readonly roleBlockedBy: MemberActionBlock | null;
  readonly canRemove: boolean;
  readonly removeBlockedBy: MemberActionBlock | null;
  readonly canSuspend: boolean;
  readonly canActivate: boolean;
  readonly suspendBlockedBy: MemberActionBlock | null;
}

/**
 * What the viewer may do to `member`. Admins and owners manage members;
 * nobody changes their own membership here; the last owner keeps the role.
 */
export function memberCapabilities(
  member: WorkspaceMember,
  viewer: {
    readonly userId: string | null;
    readonly role: WorkspaceRole | null;
  },
  ownerCount: number,
): MemberCapabilities {
  const blocked = (reason: MemberActionBlock): MemberCapabilities => ({
    assignableRoles: [],
    roleBlockedBy: reason,
    canRemove: false,
    removeBlockedBy: reason,
    canSuspend: false,
    canActivate: false,
    suspendBlockedBy: reason,
  });

  if (!isWorkspaceAdmin(viewer.role)) return blocked("not-admin");

  const isOwner = member.role === "owner";
  // The last owner keeps the role whoever looks (WSP-008); say so first.
  if (isOwner && ownerCount <= 1) return blocked("last-owner");
  if (viewer.userId !== null && member.userId === viewer.userId)
    return blocked("self");
  // Only an owner may change another owner; admins manage non-owners.
  if (isOwner && viewer.role !== "owner") return blocked("owner-by-transfer");

  const suspended = member.membership === "suspended";
  return {
    // The producer rejects role changes of suspended members.
    assignableRoles: suspended
      ? []
      : isOwner
        ? ["owner", ...ASSIGNABLE_ROLES]
        : [...ASSIGNABLE_ROLES],
    roleBlockedBy: suspended ? "suspended" : null,
    canRemove: true,
    removeBlockedBy: null,
    // Driven by the producer's status: an active member can be suspended, a
    // suspended one reactivated. Without a reported status neither is offered.
    canSuspend: member.membership === "active",
    canActivate: suspended,
    suspendBlockedBy: null,
  };
}

/** Invitation state at `now` (ISO or epoch ms). */
export function invitationState(
  invitation: Pick<
    WorkspaceInvitation,
    "isAccepted" | "expiresAt" | "isExpired"
  >,
  now: number,
): WorkspaceInvitationState {
  if (invitation.isAccepted) return "accepted";
  const expires = Date.parse(invitation.expiresAt);
  if (invitation.isExpired || (!Number.isNaN(expires) && expires <= now))
    return "expired";
  return "pending";
}

/** Pending invitations, newest first. */
export function pendingInvitations(
  invitations: readonly WorkspaceInvitation[],
  now: number,
): WorkspaceInvitation[] {
  return invitations
    .filter((invitation) => invitationState(invitation, now) === "pending")
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

/**
 * The invitation already waiting for an answer at `email`, if any. Mirrors the
 * producer's rule (one pending, unexpired invitation per address, compared
 * case-insensitively) so the invite form can say so before sending, in the
 * member's language. The producer still decides.
 */
export function pendingInvitationFor(
  invitations: readonly WorkspaceInvitation[],
  email: string,
  now: number,
): WorkspaceInvitation | null {
  const address = email.trim().toLowerCase();
  if (!address) return null;
  return (
    invitations.find(
      (invitation) =>
        invitation.email.trim().toLowerCase() === address &&
        invitationState(invitation, now) === "pending",
    ) ?? null
  );
}

/** A permissive e-mail shape check for the invite form (the producer decides). */
export function isPlausibleEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}
