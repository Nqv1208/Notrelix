/**
 * Workspace context handlers.
 *
 * Operations with official contracts:
 *   workspace.list         — GET /workspaces
 *   workspace.get          — GET /workspaces/:id
 *
 * Operations with COMPATIBILITY-GAP (temporary legacy behavior):
 *   workspace.views.list   — GET /workspaces/:id/views   [CTR-GAP-WS-VIEWS]
 *
 * Members, invitations, activity, settings and the versioned profile,
 * defaults and archive writes mirror the producer shapes
 * (Workspaces.Members.*, Workspaces.Invitations.*, WX-MEM-001/WX-SET-001).
 *
 * Plan: 01-FREEZE-SPEC.md §FZ-S06, §FZ-S07, §FZ-S10, §FZ-S11
 *       02-IMPLEMENTATION-PLAN.md §MFB-FZ-03, §MFB-FZ-05
 */

import type {
  WorkspaceSummary,
  WorkspaceView,
} from "@notrelix/features-workspace";
import { defineMockOperation } from "../../operations/types";
import { spacesOperations } from "./spaces.handlers";
import { onboardingOperations } from "./onboarding.handlers";
import {
  ok,
  created,
  noContent,
  notFound,
  forbidden,
  validationError,
  type MockHttpResult,
} from "../../transport/create-response";
import type {
  MockMemberChange,
  MockStore,
  MockVersionedWrite,
  MockWorkspaceSettingsRecord,
} from "../../state/mock-store";
import type { MockMembershipRecord } from "../../state/records";

const NAVIGATION_KINDS = new Set(["work-management.board", "documents.page"]);

/** `WorkspaceInvitationStatus` enum names. */
const INVITATION_STATUS = {
  pending: "Pending",
  accepted: "Accepted",
  revoked: "Revoked",
} as const;

type MemberRole = "owner" | "admin" | "member" | "guest";

/** The producer serializes roles as enum names (JsonStringEnumConverter). */
const ROLE_NAME: Readonly<Record<MemberRole, string>> = {
  owner: "Owner",
  admin: "Admin",
  member: "Member",
  guest: "Guest",
};

/** Accepts the enum name or value, like the producer's role binding. */
function parseRole(value: unknown): MemberRole | null {
  if (typeof value === "number") {
    return (["guest", "member", "admin", "owner"] as const)[value] ?? null;
  }
  if (typeof value !== "string") return null;
  const lower = value.toLowerCase();
  return lower === "owner" ||
    lower === "admin" ||
    lower === "member" ||
    lower === "guest"
    ? lower
    : null;
}

/** One `WorkspaceMemberDto` row. */
function memberWire(store: MockStore, m: MockMembershipRecord) {
  const user = store.getUser(m.userId);
  return {
    userId: m.userId,
    name: user ? user.name : "Unknown",
    avatar: user?.avatarUrl ?? null,
    role: ROLE_NAME[m.role],
    joinedAt: m.joinedAt,
    status: store.isMembershipSuspended(m.id) ? "Suspended" : "Active",
    // The member's own profile in this workspace (D15): job title and version.
    jobTitle: store.accountPages.memberProfileOf(m.workspaceId, m.userId)
      .jobTitle,
    version: store.accountPages.memberProfileOf(m.workspaceId, m.userId)
      .version,
  };
}

/** `ManageMembers` is held by owners and admins. */
function canManageMembers(store: MockStore, workspaceId: string): boolean {
  const role = store.getCurrentMemberRole(workspaceId);
  return role === "owner" || role === "admin";
}

/** A business-rule failure as the producer's ProblemDetails. */
function ruleViolation(detail: string): MockHttpResult<unknown> {
  return {
    status: 400,
    body: {
      type: "https://docs.notrelix.com/problems/business_rule-violation",
      title: "Business rule violation",
      status: 400,
      detail,
      errorCode: "business_rule.violation",
    },
  };
}

/** The producer's 412 for a stale `expectedVersion` (common.precondition-failed). */
function preconditionFailed(): MockHttpResult<never> {
  const result: MockHttpResult<unknown> = {
    status: 412,
    body: {
      type: "https://docs.notrelix.com/problems/common-precondition-failed",
      title: "Precondition failed",
      status: 412,
      detail: "The resource was modified by another request. Reload and retry.",
      errorCode: "common.precondition-failed",
    },
  };
  return result as MockHttpResult<never>;
}

function versionedWriteResult(
  write: MockVersionedWrite,
): MockHttpResult<never> {
  switch (write) {
    case "ok":
      return noContent();
    case "not-found":
      return notFound() as MockHttpResult<never>;
    case "stale":
      return preconditionFailed();
  }
}

/** Gap field `color` (CTR-GAP-WS-COLOR): the mockups' workspace mark colours. */
function workspaceColor(id: string): string | null {
  return id === "mock-workspace-primary"
    ? "blue"
    : id === "mock-workspace-secondary"
      ? "sage"
      : id === "ws-0002" || id.endsWith("0002")
        ? "gold"
        : null;
}

/** `WorkspaceDto` as the producer serializes it (version included). */
function workspaceDto(store: MockStore, workspaceId: string) {
  const w = store.getWorkspace(workspaceId)!;
  return {
    id: w.id,
    name: w.name,
    slug: w.slug,
    description: store.getWorkspaceDescription(w.id),
    plan: w.plan,
    icon: w.icon,
    memberCount: store.getWorkspaceMemberCount(w.id),
    color: workspaceColor(w.id),
    // Gap field `departmentCount` (CTR-GAP-WS-COLOR family): the workspace's
    // departments, whether or not the caller can see each of them.
    departmentCount: store.spaces
      .spacesOf(w.id)
      .filter(
        (space) =>
          space.type === "Department" && !space.parentId && !space.archived,
      ).length,
    isPersonal: w.isPersonal,
    isArchived: false,
    version: store.getWorkspaceVersion(w.id),
  };
}

/** The time zones a workspace may use, as on the producer. */
const WORKSPACE_TIME_ZONES = [
  "Asia/Ho_Chi_Minh",
  "Asia/Singapore",
  "Asia/Tokyo",
] as const;

const SETTINGS_ROLE: Readonly<
  Record<string, MockWorkspaceSettingsRecord["defaultMemberRole"]>
> = { admin: "Admin", member: "Member", guest: "Guest" };

function memberChangeResult(change: MockMemberChange): MockHttpResult<never> {
  switch (change) {
    case "ok":
      return noContent();
    case "not-found":
      return notFound() as MockHttpResult<never>;
    case "last-owner":
      return ruleViolation(
        "Cannot downgrade the last owner of the workspace.",
      ) as MockHttpResult<never>;
    case "owner-direct":
      return ruleViolation(
        "Ownership must be transferred through the ownership transfer workflow.",
      ) as MockHttpResult<never>;
    case "inactive":
      return ruleViolation(
        "Cannot change role of an inactive or suspended member.",
      ) as MockHttpResult<never>;
  }
}

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
  ...spacesOperations,
  ...onboardingOperations,
  // ─── GET /workspaces ──────────────────────────────────────────────────────

  defineMockOperation<Record<string, never>, never, WorkspaceSummary[]>({
    id: "workspace.list",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/workspaces",
    async handle({ store }) {
      return ok(
        store
          .getWorkspaces()
          .filter((w) => !store.isWorkspaceArchived(w.id))
          .map((w) => workspaceDto(store, w.id)),
      );
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
      if (!w || store.isWorkspaceArchived(w.id))
        return notFound(`Workspace "${params.id}" not found`);
      return ok(workspaceDto(store, w.id) as WorkspaceSummary);
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

  // ─── Profile, defaults and archive (WX-SET-001) ────────────────────────────
  // Versioned like the producer: each write carries `expectedVersion`; a stale
  // one is a 412 and changes nothing; success advances the workspace version.

  defineMockOperation<
    { workspaceId: string },
    {
      name?: string | null;
      description?: string | null;
      expectedVersion?: number;
    },
    void
  >({
    id: "workspace.profile.update",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Workspaces.UpdateWorkspaceProfile",
    },
    method: "PATCH",
    route: "/workspaces/:workspaceId/profile",
    async handle({ params, body, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      const name = body?.name;
      if (typeof name === "string" && !name.trim()) {
        return validationError({ Name: ["Name must not be empty."] });
      }
      return versionedWriteResult(
        store.updateWorkspaceProfile(
          params.workspaceId,
          body?.expectedVersion,
          {
            name: typeof name === "string" ? name.trim() : null,
            description: body?.description ?? null,
          },
        ),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    {
      allowPublicSharing?: boolean;
      enforceMfa?: boolean;
      allowGuestInvites?: boolean;
      defaultMemberRole?: string;
      invitationExpiryDays?: number;
      expectedVersion?: number;
      timeZone?: string | null;
      weekStart?: string | null;
    },
    void
  >({
    id: "workspace.settings.update",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Settings.UpdateWorkspaceSettings",
    },
    method: "PUT",
    route: "/workspaces/:workspaceId/settings",
    async handle({ params, body, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      const role =
        SETTINGS_ROLE[String(body?.defaultMemberRole ?? "").toLowerCase()];
      const days = body?.invitationExpiryDays;
      if (!role) {
        return validationError({ DefaultMemberRole: ["Unknown role."] });
      }
      if (typeof days !== "number" || days < 1 || days > 30) {
        return validationError({
          InvitationExpiryDays: ["Must be between 1 and 30 days."],
        });
      }
      const kept = store.getWorkspaceSettings(params.workspaceId);
      const timeZone = body?.timeZone
        ? [
            ...WORKSPACE_TIME_ZONES,
            ...Intl.supportedValuesOf("timeZone"),
            "UTC",
          ].find((zone) => zone.toLowerCase() === body.timeZone?.toLowerCase())
        : kept.timeZone;
      if (!timeZone)
        return validationError({ TimeZone: ["Unknown time zone."] });
      const weekStart = (body?.weekStart ?? kept.weekStart).toLowerCase();
      if (weekStart !== "monday" && weekStart !== "sunday")
        return validationError({ WeekStart: ["Must be monday or sunday."] });
      return versionedWriteResult(
        store.updateWorkspaceSettings(
          params.workspaceId,
          body?.expectedVersion,
          {
            allowPublicSharing: body?.allowPublicSharing === true,
            enforceMfa: body?.enforceMfa === true,
            allowGuestInvites: body?.allowGuestInvites === true,
            defaultMemberRole: role,
            invitationExpiryDays: days,
            timeZone,
            weekStart,
          },
        ),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { expectedVersion?: number },
    void
  >({
    id: "workspace.archive",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Workspaces.ArchiveWorkspace",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/archive",
    async handle({ params, body, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      return versionedWriteResult(
        store.archiveWorkspace(params.workspaceId, body?.expectedVersion),
      );
    },
  }),

  // ─── Members (Workspaces.Members.*, WX-MEM-001) ────────────────────────────
  // Shapes mirror the producer: `WorkspaceMemberDto` (userId, name, avatar,
  // role and status as enum names, joinedAt; removed members are not listed)
  // and ProblemDetails failures.

  defineMockOperation<{ id: string }, never, unknown>({
    id: "workspace.members.list",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.ListMembers",
    },
    method: "GET",
    route: "/workspaces/:id/members",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.id)) return notFound();
      // `ViewMembers` is denied to a Guest (WG-DEBT-002): the Guest reads only
      // their own row through `members/me`.
      if (store.getCurrentMemberRole(params.id) === "guest") return forbidden();
      return ok(
        store.getWorkspaceMembers(params.id).map((m) => memberWire(store, m)),
      );
    },
  }),

  defineMockOperation<{ id: string }, never, unknown>({
    id: "workspace.members.me",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.GetMyMembership",
    },
    method: "GET",
    route: "/workspaces/:id/members/me",
    async handle({ params, store }) {
      // Any member, a Guest included, reads exactly their own row.
      const userId = store.getCurrentUser().id;
      const mine = store
        .getWorkspaceMembers(params.id)
        .find((m) => m.userId === userId);
      if (!mine) return notFound();
      return ok(memberWire(store, mine));
    },
  }),

  // `Workspaces.Members.UpdateMyJobTitle`: the member's own title, guarded by
  // the version of their membership (412 when stale; unchanged = no-op).
  defineMockOperation<
    { workspaceId: string },
    { jobTitle?: string | null; expectedVersion?: number },
    unknown
  >({
    id: "workspace.members.myJobTitle",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.UpdateMyJobTitle",
    },
    method: "PUT",
    route: "/workspaces/:workspaceId/members/me/job-title",
    async handle({ params, body, store }) {
      const userId = store.getCurrentUser().id;
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const profile = store.accountPages.memberProfileOf(
        params.workspaceId,
        userId,
      );
      const title = (body?.jobTitle ?? "").replace(/\s+/g, " ").trim();
      if (title.length > 100)
        return ruleViolation("A job title has at most 100 characters.");
      if (body?.expectedVersion !== profile.version)
        return {
          status: 412,
          body: {
            type: "https://notrelix.dev/problems/common.precondition-failed",
            title: "Precondition failed",
            status: 412,
            detail: "The profile changed since it was read.",
            errorCode: "common.precondition-failed",
          },
        };
      const next = title === "" ? null : title;
      if (next !== profile.jobTitle) {
        profile.jobTitle = next;
        profile.version += 1;
      }
      return ok({
        userId,
        jobTitle: profile.jobTitle,
        version: profile.version,
      });
    },
  }),

  defineMockOperation<{ workspaceId: string; userId: string }, never, void>({
    id: "workspace.members.remove",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.RemoveMember",
    },
    method: "DELETE",
    route: "/workspaces/:workspaceId/members/:userId",
    async handle({ params, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      return memberChangeResult(
        store.removeMember(params.workspaceId, params.userId),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string; userId: string },
    { role?: unknown },
    void
  >({
    id: "workspace.members.update",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.UpdateMemberRole",
    },
    method: "PATCH",
    route: "/workspaces/:workspaceId/members/:userId",
    async handle({ params, body, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      const role = parseRole(body?.role);
      if (!role) return validationError({ role: ["Unknown role."] });
      return memberChangeResult(
        store.changeMemberRole(params.workspaceId, params.userId, role),
      );
    },
  }),

  defineMockOperation<{ workspaceId: string; userId: string }, never, void>({
    id: "workspace.members.suspend",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.SuspendMember",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/members/:userId/suspend",
    async handle({ params, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      return memberChangeResult(
        store.setMemberSuspended(params.workspaceId, params.userId, true),
      );
    },
  }),

  defineMockOperation<{ workspaceId: string; userId: string }, never, void>({
    id: "workspace.members.activate",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.ActivateMember",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/members/:userId/activate",
    async handle({ params, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      return memberChangeResult(
        store.setMemberSuspended(params.workspaceId, params.userId, false),
      );
    },
  }),

  // ─── Invitations (Workspaces.Invitations.*, WX-MEM-001) ──────────────────

  defineMockOperation<
    { workspaceId: string },
    { email?: string; role?: string },
    unknown
  >({
    id: "workspace.members.invite",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Members.InviteMember",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/members",
    async handle({ params, body, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      const email = (body?.email ?? "").trim();
      const role = parseRole(body?.role);
      if (!/^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/.test(email)) {
        return validationError({ Email: ["Email is not a valid address."] });
      }
      if (!role || role === "owner") {
        return validationError({ Role: ["Unknown role."] });
      }
      // WSP-026: an invitation offers Member or Guest only; the producer rejects
      // Admin with Workspaces_Invitation_CannotInviteAsAdmin.
      if (role === "admin") {
        return ruleViolation(
          "A workspace invitation offers Member or Guest only; promote the member after they join.",
        );
      }
      if (store.isActiveMemberEmail(params.workspaceId, email)) {
        return {
          status: 409,
          body: {
            type: "https://docs.notrelix.com/problems/conflict",
            title: "Conflict",
            status: 409,
            detail: "This person is already a member of this workspace.",
            errorCode: "workspaces.invitation.already-member",
          },
        };
      }
      const id = store.createInvitation(params.workspaceId, email, role);
      if (!id) {
        return validationError({
          "": ["A pending invitation already exists for this email address."],
        });
      }
      return created(id);
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "workspace.invitations.list",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Invitations.ListWorkspaceInvitations",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/invitations",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      // Like the producer's DTO, each invitation carries its status.
      return ok(
        store.getInvitations(params.workspaceId).map((invitation) => ({
          id: invitation.id,
          email: invitation.email,
          role: ROLE_NAME[invitation.role],
          expiresAt: invitation.expiresAt,
          isAccepted: invitation.status === "accepted",
          createdAt: invitation.createdAt,
          status: INVITATION_STATUS[invitation.status],
        })),
      );
    },
  }),

  defineMockOperation<
    { workspaceId: string; invitationId: string },
    never,
    void
  >({
    id: "workspace.invitations.cancel",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Invitations.CancelInvitation",
    },
    method: "DELETE",
    route: "/workspaces/:workspaceId/invitations/:invitationId",
    async handle({ params, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      return store.revokeInvitation(params.workspaceId, params.invitationId)
        ? noContent()
        : notFound();
    },
  }),

  defineMockOperation<
    { workspaceId: string; invitationId: string },
    { newRole?: string },
    void
  >({
    id: "workspace.invitations.changeRole",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Invitations.ChangeInvitationRole",
    },
    method: "PATCH",
    route: "/workspaces/:workspaceId/invitations/:invitationId/role",
    async handle({ params, body, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      const role = parseRole(body?.newRole);
      if (!role || role === "owner") {
        return validationError({ NewRole: ["Unknown role."] });
      }
      if (role === "admin") {
        return ruleViolation(
          "A workspace invitation offers Member or Guest only; promote the member after they join.",
        );
      }
      const changed = store.changeInvitationRole(
        params.workspaceId,
        params.invitationId,
        role,
      );
      if (changed === null) return notFound();
      if (!changed)
        return ruleViolation("Only a waiting invitation can change its role.");
      return noContent();
    },
  }),

  defineMockOperation<
    { workspaceId: string; invitationId: string },
    never,
    unknown
  >({
    id: "workspace.invitations.resend",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Invitations.ResendInvitation",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/invitations/:invitationId/resend",
    async handle({ params, store }) {
      if (!canManageMembers(store, params.workspaceId)) return forbidden();
      const resent = store.resendInvitation(
        params.workspaceId,
        params.invitationId,
      );
      if (resent === null) return notFound();
      if (!resent)
        return ruleViolation("Only a waiting invitation can be sent again.");
      return ok(params.invitationId);
    },
  }),

  // ─── Activity and settings ────────────────────────────────────────────────

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "workspace.activity.list",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Activity.GetWorkspaceActivity",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/activity",
    async handle({ params, store, request }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const query = request.searchParams;
      // The producer records no activity entries yet; mirror its envelope.
      return ok({
        data: [],
        total: 0,
        page: Number(query.get("page") ?? 1),
        pageSize: Number(query.get("pageSize") ?? 20),
      });
    },
  }),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "workspace.settings.get",
    contract: {
      kind: "openapi",
      operationId: "Workspaces.Settings.GetWorkspaceSettings",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/settings",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok({ ...store.getWorkspaceSettings(params.workspaceId) });
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
