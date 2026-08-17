/**
 * Identity context handlers.
 *
 * Operations:
 *   identity.profile        — GET /auth/me
 *   identity.login          — POST /auth/login
 *   identity.register       — POST /auth/register
 *   identity.refresh        — POST /auth/refresh
 *   identity.logout         — POST /auth/logout
 *   identity.forgotPassword — POST /auth/forgot-password
 *   identity.resetPassword  — POST /auth/reset-password
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
      if (store.isCurrentUserLoggedOut()) {
        return unauthorized();
      }
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
    id: "identity.login",
    method: "POST",
    route: "/auth/login",
    async handle({ store }) {
      const user = store.getCurrentUser();
      return ok({
        accessToken: "mock-access-token",
        user: {
          id: user.id,
          email: user.email,
          name: user.name,
          avatarUrl: user.avatarUrl,
        },
      });
    },
  }),

  defineMockOperation({
    id: "identity.register",
    method: "POST",
    route: "/auth/register",
    async handle({ store }) {
      const user = store.getCurrentUser();
      return ok({
        accessToken: "mock-access-token",
        user: {
          id: user.id,
          email: user.email,
          name: user.name,
          avatarUrl: user.avatarUrl,
        },
      });
    },
  }),

  defineMockOperation({
    id: "identity.refresh",
    method: "POST",
    route: "/auth/refresh",
    async handle({ store }) {
      // expired-session state or logged out exercises the session-expired lifecycle
      if (store.isCurrentUserLoggedOut()) {
        return unauthorized();
      }
      return ok({
        accessToken: "mock-access-token-refreshed",
        success: true,
      });
    },
  }),

  defineMockOperation({
    id: "identity.logout",
    method: "POST",
    route: "/auth/logout",
    async handle({ store }) {
      store.logoutCurrentUser();
      return ok({ success: true });
    },
  }),

  defineMockOperation({
    id: "identity.forgotPassword",
    method: "POST",
    route: "/auth/forgot-password",
    async handle() {
      return ok({ success: true });
    },
  }),

  defineMockOperation({
    id: "identity.resetPassword",
    method: "POST",
    route: "/auth/reset-password",
    async handle() {
      return ok({ success: true });
    },
  }),
];
