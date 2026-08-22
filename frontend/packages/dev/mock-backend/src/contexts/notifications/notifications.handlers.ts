/**
 * Notifications context handlers.
 *
 * Operations with official contracts:
 *   notifications.list    — GET /notifications
 *   notifications.read    — POST /notifications/:id/read
 *   notifications.readAll — POST /notifications/read-all
 *
 * Operations with CONTRACT-BLOCKED gaps:
 *   [CTR-GAP-NTF-UNREAD]    — no dedicated unread-count endpoint
 *   [CTR-GAP-NTF-ARCHIVE]   — no archive contract
 *   [CTR-GAP-NTF-PREFERENCES] — no preferences contract
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md, 10-CONTRACT-GAP-REGISTER.md
 */

import { defineMockOperation } from "../../operations/types";
import { ok, notFound } from "../../transport/create-response";

interface NotificationDtoApi {
  id: string;
  userId: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export const notificationsOperations = [
  // ─── GET /notifications ───────────────────────────────────────────────────

  defineMockOperation<Record<string, never>, never, NotificationDtoApi[]>({
    id: "notifications.list",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/notifications",
    async handle({ store }) {
      const user = store.getCurrentUser();
      return ok(
        store.getNotifications(user.id).map((n) => ({
          id: n.id,
          userId: n.userId,
          title: n.title,
          message: n.message,
          isRead: n.isRead,
          createdAt: n.createdAt,
        })),
      );
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
];
