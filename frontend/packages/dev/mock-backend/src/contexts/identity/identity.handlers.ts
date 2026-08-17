/**
 * Identity context handlers.
 *
 * Operations:
 *   identity.profile  — GET /auth/me
 *   identity.refresh  — POST /auth/refresh
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Context layout, 04-TRANSPORT-PROTOCOL.md §Auth refresh
 */

import type { User } from "@notrelix/features-auth";
import { defineMockOperation } from "../../operations/types";
import { ok, unauthorized } from "../../transport/create-response";

export const identityOperations = [
  defineMockOperation<Record<string, never>, never, User>({
    id: "identity.profile",
    method: "GET",
    route: "/auth/me",
    async handle({ store }) {
      const user = store.getCurrentUser();
      return ok<User>({
        id: user.id,
        email: user.email,
        name: user.name,
        avatarUrl: user.avatarUrl,
      });
    },
  }),

  defineMockOperation({
    id: "identity.refresh",
    method: "POST",
    route: "/auth/refresh",
    async handle({ store }) {
      // expired-session state exercises the real session-expired lifecycle:
      // auth/refresh → 401 → real NotrelixClient fires SessionExpiredEvent
      if (store.getConfig().state === "expired-session") {
        return unauthorized();
      }
      return ok({ success: true });
    },
  }),

  defineMockOperation({
    id: "identity.logout",
    method: "POST",
    route: "/auth/logout",
    async handle() {
      return ok({ success: true });
    },
  }),
];
