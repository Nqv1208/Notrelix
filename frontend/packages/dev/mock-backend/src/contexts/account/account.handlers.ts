/**
 * Account context handlers.
 *
 * All operations in this context are CONTRACT-BLOCKED:
 *   account.preferences.get    [CTR-GAP-ACC-PREFERENCES]
 *   account.preferences.update [CTR-GAP-ACC-PREFERENCES]
 *   account.profile.update     [CTR-GAP-ACC-SECURITY]
 *
 * These are temporary legacy handlers.
 * Remove once official producer contracts land and close the gaps.
 *
 * Plan: 10-CONTRACT-GAP-REGISTER.md §CTR-GAP-ACC-PREFERENCES, §CTR-GAP-ACC-SECURITY
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";

/** @contractBlocked CTR-GAP-ACC-PREFERENCES */
const DEFAULT_PREFERENCES = {
  theme: "system" as const,
  notificationsEnabled: true,
  emailDigest: "daily" as const,
};

export const accountOperations = [
  defineMockOperation({
    id: "account.preferences.get",
    method: "GET",
    route: "/account/preferences",
    async handle() {
      return ok(DEFAULT_PREFERENCES);
    },
  }),

  defineMockOperation({
    id: "account.preferences.update",
    method: "PATCH",
    route: "/account/preferences",
    async handle({ body }) {
      return ok({ ...DEFAULT_PREFERENCES, ...((body as object | null) ?? {}) });
    },
  }),

  defineMockOperation({
    id: "account.profile.update",
    method: "PATCH",
    route: "/users/profile",
    async handle({ body, store }) {
      const data = (body ?? {}) as { name?: string; email?: string };
      const user = store.getCurrentUser();
      return ok({
        id: user.id,
        email: data.email ?? user.email,
        name: data.name ?? user.name,
        avatarUrl: user.avatarUrl,
      });
    },
  }),
];
