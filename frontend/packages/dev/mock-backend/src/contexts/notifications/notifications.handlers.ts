/**
 * Notifications context handlers.
 *
 * Operations with official contracts:
 *   notifications.list    — GET /notifications
 *   notifications.read    — POST /notifications/:id/read
 *   notifications.readAll — POST /notifications/read-all
 *
 * Operations with canonical contracts:
 *   notifications.preferences.get/update — GET/PATCH /workspaces/:workspaceId/notification-preferences
 *                                           (Collaboration.Preferences.*, workspace-scoped, expected version)
 *
 * Operations with CONTRACT-BLOCKED gaps:
 *   [CTR-GAP-NTF-UNREAD]    — no dedicated unread-count endpoint
 *   [CTR-GAP-NTF-ARCHIVE]   — no archive contract
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md, 10-CONTRACT-GAP-REGISTER.md
 */

import { defineMockOperation } from "../../operations/types";
import type { MockStore } from "../../state/mock-store";
import type { MockNotificationRecord } from "../../state/records";
import {
  NOTIFICATION_EVENTS,
  type NotificationEvent,
} from "../../state/mock-account-pages";
import { ok, notFound, validationError } from "../../transport/create-response";

const CHANNELS = ["inApp", "email"] as const;

/** The wire shape `NotificationPreferencesDto`: rows in the order a client shows them. */
function preferencesBody(
  set: ReturnType<MockStore["accountPages"]["notificationsOf"]>,
) {
  return {
    version: set.version,
    rows: NOTIFICATION_EVENTS.map((type) => ({ type, ...set.rows[type] })),
  };
}

/** The app's own `Notification` type: the contract has none (gap register). */
type NotificationDtoApi = MockNotificationRecord;

export const notificationsOperations = [
  // ─── GET /notifications ───────────────────────────────────────────────────

  defineMockOperation<Record<string, never>, never, NotificationDtoApi[]>({
    id: "notifications.list",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/notifications",
    async handle({ store }) {
      const user = store.getCurrentUser();
      return ok(store.getNotifications(user.id).map((n) => ({ ...n })));
    },
  }),

  // ─── POST /notifications/:id/read ────────────────────────────────────────

  defineMockOperation<{ id: string }>({
    id: "notifications.read",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/notifications/:id/read",
    async handle({ params, store }) {
      const updated = store.markNotificationRead(params.id);
      if (!updated) return notFound("Notification not found");
      return ok({ id: params.id, isRead: true });
    },
  }),

  // ─── POST /notifications/read-all ────────────────────────────────────────

  defineMockOperation({
    id: "notifications.readAll",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/notifications/read-all",
    async handle({ store }) {
      const user = store.getCurrentUser();
      store.markAllNotificationsRead(user.id);
      return ok({ success: true });
    },
  }),

  // ─── GET /workspaces/:workspaceId/notification-preferences ────────────────

  defineMockOperation<{ workspaceId: string }>({
    id: "notifications.preferences.get",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Preferences.GetNotificationPreferences",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/notification-preferences",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      return ok(
        preferencesBody(
          store.accountPages.notificationsOf(
            store.getCurrentUser().id,
            params.workspaceId,
          ),
        ),
      );
    },
  }),

  // ─── PATCH /workspaces/:workspaceId/notification-preferences ──────────────

  defineMockOperation<
    { workspaceId: string },
    {
      expectedVersion?: number;
      rows?: { type?: string; inApp?: boolean; email?: boolean }[];
    }
  >({
    id: "notifications.preferences.update",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Preferences.UpdateNotificationPreferences",
    },
    method: "PATCH",
    route: "/workspaces/:workspaceId/notification-preferences",
    async handle({ params, body, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId)) return notFound();
      const set = store.accountPages.notificationsOf(
        store.getCurrentUser().id,
        params.workspaceId,
      );
      const rows = body?.rows ?? [];
      const errors: Record<string, string[]> = {};
      rows.forEach((row, index) => {
        if (!NOTIFICATION_EVENTS.includes(row.type as NotificationEvent))
          errors[`rows.${index}.type`] = ["Unknown notification type."];
      });
      if (typeof body?.expectedVersion !== "number")
        errors.expectedVersion = [
          "Quote the version the choices were read at.",
        ];
      if (Object.keys(errors).length > 0) return validationError(errors);
      if (body?.expectedVersion !== set.version)
        return {
          status: 412,
          body: {
            type: "https://notrelix.dev/problems/common.precondition-failed",
            title: "Precondition failed",
            status: 412,
            detail: "The choices changed since they were read.",
            errorCode: "common.precondition-failed",
          },
        };
      let changed = false;
      for (const row of rows) {
        const current = set.rows[row.type as NotificationEvent];
        for (const channel of CHANNELS) {
          const next = row[channel];
          if (typeof next === "boolean" && next !== current[channel]) {
            current[channel] = next;
            changed = true;
          }
        }
      }
      // An unchanged save is a no-op that keeps the version.
      if (changed) set.version += 1;
      return ok(preferencesBody(set));
    },
  }),
];
