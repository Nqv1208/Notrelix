/**
 * Account context handlers.
 *
 * Operations:
 *   account.preferences.get    [CTR-GAP-ACC-PREFERENCES] — GET /account/preferences
 *   account.preferences.update [CTR-GAP-ACC-PREFERENCES] — PATCH /account/preferences
 *   account.profile.update     — PATCH /users/profile
 *   account.security.get       [CTR-GAP-ACC-SECURITY]    — GET /users/security
 *
 * Plan: 06-HANDLERS-PROJECTIONS.md §Account context
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";

export const accountOperations = [
  defineMockOperation({
    id: "account.preferences.get",
    method: "GET",
    route: "/account/preferences",
    async handle({ store }) {
      const user = store.getCurrentUser();
      const prefs = store.getUserPreferences(user.id);
      return ok(prefs);
    },
  }),

  defineMockOperation({
    id: "account.preferences.update",
    method: "PATCH",
    route: "/account/preferences",
    async handle({ body, store }) {
      const user = store.getCurrentUser();
      const patch = (body ?? {}) as object;
      const updated = store.updateUserPreferences(user.id, patch);
      return ok(updated);
    },
  }),

  defineMockOperation({
    id: "account.profile.update",
    method: "PATCH",
    route: "/users/profile",
    async handle({ body, store }) {
      const user = store.getCurrentUser();
      const data = (body ?? {}) as { name?: string; email?: string };
      const updated = store.updateUserProfile(user.id, data);
      return ok({
        id: updated?.id ?? user.id,
        email: updated?.email ?? user.email,
        name: updated?.name ?? user.name,
        avatarUrl: updated?.avatarUrl ?? user.avatarUrl,
      });
    },
  }),

  defineMockOperation({
    id: "account.security.get",
    method: "GET",
    route: "/users/security",
    async handle({ store }) {
      const user = store.getCurrentUser();
      return ok({
        userId: user.id,
        twoFactorEnabled: false,
        lastPasswordChange: store.getClock().offsetDays(-30),
        activeSessions: 1,
      });
    },
  }),
];
