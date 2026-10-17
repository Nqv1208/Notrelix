/**
 * Account — the pages of the 2026-10 Account mockup (gap
 * `CTR-GAP-ACC-PAGES`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Account"):
 *
 *   account.details.get/update   — GET/PATCH /account/profile-details
 *   account.avatar.set           — PUT       /account/avatar
 *   account.security.details     — GET       /account/security-details
 *   account.twoFactor.set        — PUT       /account/security/two-factor
 *   account.backupCodes.renew    — POST      /account/security/backup-codes
 *   account.session.revoke       — DELETE    /account/security/sessions/:sessionId
 *   account.session.revokeOthers — POST      /account/security/sessions/revoke-others
 *   account.notificationMatrix.get/save — GET/PUT /account/notification-matrix
 *   account.workspaces.list      — GET       /account/workspaces
 *   account.workspaces.setDefault — PUT      /account/default-workspace
 *   account.workspaces.leave     — DELETE    /account/workspaces/:workspaceId
 *
 * The world the mockup draws: the second workspace lists the member as an
 * ordinary member and the third as a guest of one board, whatever the
 * persona's seeded role there is.
 */
import { defineMockOperation } from "../../operations/types";
import {
  conflict,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  CURRENT_SESSION_ID,
  DATE_FORMATS,
  NOTIFICATION_EVENTS,
  TIME_ZONES,
  type DateFormat,
  type NotificationEvent,
  type NotificationMatrix,
  type ProfileDetails,
} from "../../state/mock-account-pages";
import type { MockStore } from "../../state/mock-store";

const gap = { kind: "gap", gapId: "CTR-GAP-ACC-PAGES" } as const;
const TIME = /^([01]\d|2[0-3]):00$/;
const JOINED_AT = "2024-03-04T00:00:00.000Z";

const offsetMinutes = (store: MockStore) => (minutes: number) =>
  store.getClock().offsetMinutes(minutes);

function securityBody(store: MockStore) {
  const user = store.getCurrentUser();
  const security = store.accountPages.securityOf(user.id, offsetMinutes(store));
  return {
    serverTime: store.getClock().isoNow(),
    sso: { provider: "Microsoft Entra", email: user.email },
    twoFactorEnabled: security.twoFactorEnabled,
    backupCodesLeft: security.backupCodesLeft,
    sessions: security.sessions,
  };
}

function detailsBody(store: MockStore) {
  const user = store.getCurrentUser();
  const details = store.accountPages.detailsOf(user.id);
  return {
    jobTitle: details.jobTitle,
    phone: details.phone,
    timezone: details.timezone,
    dateFormat: details.dateFormat,
    avatarUrl: details.avatarUrl,
    joinedAt: JOINED_AT,
    managedBy: "Microsoft Entra" as string | null,
    workspaceName:
      store.getWorkspace(store.accountPages.defaultWorkspaceOf(user.id) ?? "")
        ?.name ??
      store.getWorkspaces()[0]?.name ??
      "",
  };
}

function workspacesBody(store: MockStore) {
  const user = store.getCurrentUser();
  const all = store
    .getWorkspaces()
    .filter(
      (workspace) =>
        !store.isWorkspaceArchived(workspace.id) &&
        store.isCurrentMemberOf(workspace.id) &&
        !store.accountPages.hasLeft(user.id, workspace.id),
    );
  const defaultId =
    store.accountPages.defaultWorkspaceOf(user.id) ?? all[0]?.id ?? null;
  return {
    workspaces: all.map((workspace, index) => {
      const seededRole = store.getCurrentMemberRole(workspace.id) ?? "member";
      const role = index === 1 ? "member" : index === 2 ? "guest" : seededRole;
      const owners = store
        .getWorkspaceMembers(workspace.id)
        .filter((member) => member.role === "owner")
        .map((member) => store.getUser(member.userId))
        .filter((u) => u && u.id !== user.id);
      const soleOwner = role === "owner" && owners.length === 0;
      return {
        id: workspace.id,
        name:
          index === 1
            ? "Minh An Đà Nẵng"
            : index === 2
              ? "Tân Lộc Logistics"
              : workspace.name,
        role,
        memberCount:
          index === 1 ? 18 : store.getWorkspaceMemberCount(workspace.id),
        departmentCount:
          index === 0
            ? store.spaces
                .spacesOf(workspace.id)
                .filter((space) => !space.parentId && !space.archived).length
            : null,
        unreadCount: index === 1 ? 2 : null,
        guestBoard: index === 2 ? "Điều phối xe tháng 10" : null,
        otherOwnerName: role === "admin" ? (owners[0]?.name ?? null) : null,
        isDefault: workspace.id === defaultId,
        canLeave: !soleOwner,
      };
    }),
  };
}

export const accountPagesOperations = [
  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.details.get",
    contract: gap,
    method: "GET",
    route: "/account/profile-details",
    async handle({ store }) {
      return ok(detailsBody(store));
    },
  }),

  defineMockOperation<
    Record<string, never>,
    Partial<Pick<ProfileDetails, "jobTitle" | "phone" | "timezone">> & {
      dateFormat?: string;
    },
    unknown
  >({
    id: "account.details.update",
    contract: gap,
    method: "PATCH",
    route: "/account/profile-details",
    async handle({ body, store }) {
      const errors: Record<string, string[]> = {};
      const patch: Partial<ProfileDetails> = {};
      if (body?.jobTitle !== undefined) {
        if (body.jobTitle.length > 80) errors.jobTitle = ["Too long."];
        patch.jobTitle = body.jobTitle.trim();
      }
      if (body?.phone !== undefined) {
        if (!/^[0-9 +().-]{0,20}$/.test(body.phone))
          errors.phone = ["Not a phone number."];
        patch.phone = body.phone.trim();
      }
      if (body?.timezone !== undefined) {
        if (!(TIME_ZONES as readonly string[]).includes(body.timezone))
          errors.timezone = ["Unknown time zone."];
        patch.timezone = body.timezone;
      }
      if (body?.dateFormat !== undefined) {
        if (!(DATE_FORMATS as readonly string[]).includes(body.dateFormat))
          errors.dateFormat = ["Unknown date format."];
        patch.dateFormat = body.dateFormat as DateFormat;
      }
      if (Object.keys(errors).length > 0) return validationError(errors);
      store.accountPages.updateDetails(store.getCurrentUser().id, patch);
      return ok(detailsBody(store));
    },
  }),

  defineMockOperation<Record<string, never>, { dataUrl?: string }, unknown>({
    id: "account.avatar.set",
    contract: gap,
    method: "PUT",
    route: "/account/avatar",
    async handle({ body, store }) {
      const dataUrl = body?.dataUrl ?? "";
      if (!/^data:image\/(png|jpeg|webp|gif);base64,/.test(dataUrl))
        return validationError({ dataUrl: ["Choose a PNG, JPEG or WebP."] });
      if (dataUrl.length > 2_000_000)
        return validationError({ dataUrl: ["The photo is too large."] });
      store.accountPages.updateDetails(store.getCurrentUser().id, {
        avatarUrl: dataUrl,
      });
      return ok(detailsBody(store));
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.security.details",
    contract: gap,
    method: "GET",
    route: "/account/security-details",
    async handle({ store }) {
      return ok(securityBody(store));
    },
  }),

  defineMockOperation<Record<string, never>, { enabled?: boolean }, unknown>({
    id: "account.twoFactor.set",
    contract: gap,
    method: "PUT",
    route: "/account/security/two-factor",
    async handle({ body, store }) {
      if (typeof body?.enabled !== "boolean")
        return validationError({ enabled: ["Say on or off."] });
      const security = store.accountPages.securityOf(
        store.getCurrentUser().id,
        offsetMinutes(store),
      );
      security.twoFactorEnabled = body.enabled;
      security.backupCodesLeft = body.enabled ? 10 : 0;
      return ok(securityBody(store));
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.backupCodes.renew",
    contract: gap,
    method: "POST",
    route: "/account/security/backup-codes",
    async handle({ store }) {
      const security = store.accountPages.securityOf(
        store.getCurrentUser().id,
        offsetMinutes(store),
      );
      if (!security.twoFactorEnabled) return conflict("Two-factor is off.");
      security.backupCodesLeft = 10;
      return ok(securityBody(store));
    },
  }),

  defineMockOperation<{ sessionId: string }, never, unknown>({
    id: "account.session.revoke",
    contract: gap,
    method: "DELETE",
    route: "/account/security/sessions/:sessionId",
    async handle({ params, store }) {
      const security = store.accountPages.securityOf(
        store.getCurrentUser().id,
        offsetMinutes(store),
      );
      const found = security.sessions.some(
        (session) => session.id === params.sessionId,
      );
      if (!found) return notFound();
      // Signing out this very device is the sign-out of the account menu.
      if (params.sessionId === CURRENT_SESSION_ID)
        return conflict("Use the sign-out of this device.");
      security.sessions = security.sessions.filter(
        (session) => session.id !== params.sessionId,
      );
      return noContent();
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.session.revokeOthers",
    contract: gap,
    method: "POST",
    route: "/account/security/sessions/revoke-others",
    async handle({ store }) {
      const security = store.accountPages.securityOf(
        store.getCurrentUser().id,
        offsetMinutes(store),
      );
      security.sessions = security.sessions.filter(
        (session) => session.id === CURRENT_SESSION_ID,
      );
      return noContent();
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.notificationMatrix.get",
    contract: gap,
    method: "GET",
    route: "/account/notification-matrix",
    async handle({ store }) {
      return ok(store.accountPages.matrixOf(store.getCurrentUser().id));
    },
  }),

  defineMockOperation<Record<string, never>, NotificationMatrix, unknown>({
    id: "account.notificationMatrix.save",
    contract: gap,
    method: "PUT",
    route: "/account/notification-matrix",
    async handle({ body, store }) {
      const errors: Record<string, string[]> = {};
      for (const event of NOTIFICATION_EVENTS) {
        const row = body?.rows?.[event as NotificationEvent];
        if (
          !row ||
          typeof row.app !== "boolean" ||
          typeof row.email !== "boolean" ||
          typeof row.mobile !== "boolean"
        )
          errors[`rows.${event}`] = ["Choose the channels."];
      }
      const quiet = body?.quietHours;
      if (
        !quiet ||
        typeof quiet.enabled !== "boolean" ||
        !TIME.test(quiet.from ?? "") ||
        !TIME.test(quiet.to ?? "") ||
        (quiet.days !== "weekdays" && quiet.days !== "everyday")
      )
        errors.quietHours = ["Set the quiet hours."];
      if (Object.keys(errors).length > 0) return validationError(errors);
      return ok(store.accountPages.saveMatrix(store.getCurrentUser().id, body));
    },
  }),

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "account.workspaces.list",
    contract: gap,
    method: "GET",
    route: "/account/workspaces",
    async handle({ store }) {
      return ok(workspacesBody(store));
    },
  }),

  defineMockOperation<Record<string, never>, { workspaceId?: string }, unknown>(
    {
      id: "account.workspaces.setDefault",
      contract: gap,
      method: "PUT",
      route: "/account/default-workspace",
      async handle({ body, store }) {
        const id = body?.workspaceId ?? "";
        if (!store.isCurrentMemberOf(id)) return notFound();
        store.accountPages.setDefaultWorkspace(store.getCurrentUser().id, id);
        return ok(workspacesBody(store));
      },
    },
  ),

  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "account.workspaces.leave",
    contract: gap,
    method: "DELETE",
    route: "/account/workspaces/:workspaceId",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const entry = workspacesBody(store).workspaces.find(
        (workspace) => workspace.id === params.workspaceId,
      );
      if (!entry?.canLeave)
        return conflict(
          "The last owner cannot leave; hand the workspace over first.",
        );
      store.accountPages.leave(store.getCurrentUser().id, params.workspaceId);
      return noContent();
    },
  }),
];
