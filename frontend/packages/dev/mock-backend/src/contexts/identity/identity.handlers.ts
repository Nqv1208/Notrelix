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
import { MOCK_AUTH } from "../../state/mock-auth-directory";
import { ok, unauthorized } from "../../transport/create-response";
import {
  authFlowOperations,
  identityProblem,
  sessionOf,
} from "./auth-flow.handlers";

export const identityOperations = [
  ...authFlowOperations,
  defineMockOperation<Record<string, never>, never, User>({
    id: "identity.profile",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
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

  defineMockOperation<Record<string, never>, never, unknown>({
    id: "identity.bootstrap",
    contract: { kind: "openapi", operationId: "Identity.Auth.GetBootstrap" },
    method: "GET",
    route: "/auth/bootstrap",
    async handle({ store }) {
      if (store.isCurrentUserLoggedOut()) {
        return unauthorized();
      }
      const user = store.getCurrentUser();
      // The mock world has one Account per member.
      const accountId = `account-${user.id}`;
      const workspaces = store
        .getWorkspaces()
        .filter(
          (workspace) =>
            !store.isWorkspaceArchived(workspace.id) &&
            !store.isWorkspaceDeleted(workspace.id),
        );
      const personal = workspaces.find((workspace) => workspace.isPersonal);
      return ok({
        user: {
          id: user.id,
          email: user.email,
          name: user.name,
          avatarUrl: user.avatarUrl,
          emailConfirmed: true,
        },
        workspaces: workspaces
          .filter((workspace) => !workspace.isPersonal)
          .map((workspace) => ({
            id: workspace.id,
            name: workspace.name,
            slug: workspace.slug,
            role: "Member",
            accountId,
          })),
        personalWorkspace: personal
          ? { status: "ready", workspaceId: personal.id }
          : { status: "pending", workspaceId: null },
        accountId,
      });
    },
  }),

  defineMockOperation<
    Record<string, never>,
    { email?: string; password?: string },
    unknown
  >({
    id: "identity.login",
    contract: { kind: "openapi", operationId: "Identity.Auth.Login" },
    method: "POST",
    route: "/auth/login",
    async handle({ body, store }) {
      // Without an email the call is the old stub: the persona signs in.
      if (!body?.email) {
        store.loginCurrentUser();
        return ok(sessionOf(store));
      }
      if (store.auth.identify(body.email).method === "sso")
        return {
          status: 400,
          body: {
            message: "This company signs in through its own provider.",
            code: "SSO_REQUIRED",
          },
        };
      const outcome = store.auth.login(body.email, body.password ?? "");
      if (outcome.kind === "locked")
        return identityProblem(
          429,
          "identity.auth.too-many-attempts",
          "Too many attempts. Try again later.",
          outcome.retryAfterSeconds,
        );
      if (outcome.kind === "invalid")
        return identityProblem(
          401,
          "identity.auth.invalid-credentials",
          "Invalid credentials",
        );
      if (outcome.kind === "otp")
        return ok({
          ...sessionOf(store),
          user: undefined,
          expiresAt: null,
          mfaRequired: true,
          mfaChallengeToken: outcome.challengeToken,
          mfaMethod: "totp",
          mfaExpiresAt: store.getClock().offsetMinutes(5),
        });
      store.loginCurrentUser();
      return ok(sessionOf(store));
    },
  }),

  defineMockOperation({
    id: "identity.register",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/auth/register",
    async handle({ store }) {
      const user = store.getCurrentUser();
      return ok({
        ...sessionOf(store),
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
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/auth/refresh",
    async handle({ store }) {
      // expired-session state or logged out exercises the session-expired lifecycle
      if (store.isCurrentUserLoggedOut()) {
        return unauthorized();
      }
      return ok(sessionOf(store));
    },
  }),

  defineMockOperation({
    id: "identity.logout",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/auth/logout",
    async handle({ store }) {
      store.logoutCurrentUser();
      return ok({ success: true });
    },
  }),

  defineMockOperation({
    id: "identity.forgotPassword",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/auth/forgot-password",
    async handle() {
      return ok({ success: true });
    },
  }),

  defineMockOperation({
    id: "identity.resetPassword",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "POST",
    route: "/auth/reset-password",
    async handle({ body }) {
      const { code } = (body ?? {}) as { code?: string };
      if (code !== MOCK_AUTH.resetCode)
        return identityProblem(
          400,
          "identity.auth.invalid-code",
          "Invalid or expired code.",
        );
      return ok({ success: true });
    },
  }),

  defineMockOperation({
    id: "identity.csrf",
    contract: { kind: "gap", gapId: "CTR-GAP-TODO" } as any,
    method: "GET",
    route: "/auth/csrf",
    async handle() {
      return ok({ token: "mock-csrf-token" });
    },
  }),
];
