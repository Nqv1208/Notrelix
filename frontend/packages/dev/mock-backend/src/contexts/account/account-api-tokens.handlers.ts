/**
 * Workspace API tokens on the Identity contracts (`Identity.ApiTokens.*`):
 *
 *   GET    /workspaces/:workspaceId/api-tokens             metadata only, newest first, cursor page (default 50, max 200)
 *   POST   /workspaces/:workspaceId/api-tokens             201 with the secret, once; needs an IssueApiToken proof
 *   DELETE /workspaces/:workspaceId/api-tokens/:tokenId    204; a second revoke is a no-op
 *
 * Only an owner or admin of the workspace may use them; anyone else gets the NotFound of a missing workspace.
 */
import { defineMockOperation } from "../../operations/types";
import type { MockStore } from "../../state/mock-store";
import {
  created,
  noContent,
  notFound,
  ok,
} from "../../transport/create-response";
import { identityProblem } from "../identity/auth-flow.handlers";

const DEFAULT_LIMIT = 50;
const MAX_LIMIT = 200;
const MAX_NAME = 256;
const MAX_DAYS = 365;
const DAY_MS = 24 * 60 * 60 * 1000;

const allowed = (store: MockStore, workspaceId: string) => {
  const role = store.getCurrentMemberRole(workspaceId);
  return role === "owner" || role === "admin";
};

const tokensOf = (store: MockStore, workspaceId: string) =>
  store.accountPages.apiTokensOf(workspaceId, (minutes) =>
    store.getClock().offsetMinutes(minutes),
  );

const invalid = (code: string, detail: string) =>
  identityProblem(400, code, detail);

export const accountApiTokenOperations = [
  defineMockOperation<
    { workspaceId: string },
    never,
    { cursor?: string; limit?: string }
  >({
    id: "account.apiTokens.list",
    contract: { kind: "openapi", operationId: "Identity.ApiTokens.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/api-tokens",
    async handle({ params, query, store }) {
      if (!allowed(store, params.workspaceId)) return notFound();
      const limit =
        query.limit === undefined ? DEFAULT_LIMIT : Number(query.limit);
      if (!Number.isInteger(limit) || limit < 1 || limit > MAX_LIMIT)
        return invalid("common.validation", "Limit is out of range.");
      const start = query.cursor === undefined ? 0 : Number(query.cursor);
      if (!Number.isInteger(start) || start < 0)
        return invalid("common.validation", "Cursor is not valid.");
      const all = tokensOf(store, params.workspaceId);
      const items = all.slice(start, start + limit).map((t) => ({
        ...t,
        scopes: null,
      }));
      return ok({
        items,
        nextCursor: start + limit < all.length ? String(start + limit) : null,
      });
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { name?: string; expiresAt?: string | null; stepUpToken?: string }
  >({
    id: "account.apiTokens.create",
    contract: { kind: "openapi", operationId: "Identity.ApiTokens.Create" },
    method: "POST",
    route: "/workspaces/:workspaceId/api-tokens",
    async handle({ body, params, store }) {
      if (!allowed(store, params.workspaceId)) return notFound();
      const name = (body?.name ?? "").trim();
      if (name.length === 0 || name.length > MAX_NAME)
        return invalid(
          "identity.api-tokens.invalid-name",
          "Token name must be between 1 and 256 characters.",
        );
      const now = Date.parse(store.getClock().isoNow());
      if (!body?.expiresAt)
        return invalid(
          "identity.api-tokens.expiration-required",
          "An API token needs an expiration date.",
        );
      const expires = Date.parse(body.expiresAt);
      if (Number.isNaN(expires) || expires <= now)
        return invalid(
          "identity.api-tokens.invalid-expiration",
          "Token expiration must be in the future.",
        );
      if (expires > now + MAX_DAYS * DAY_MS)
        return invalid(
          "identity.api-tokens.lifetime-exceeded",
          `An API token cannot live longer than ${MAX_DAYS} days.`,
        );
      const security = store.accountPages.securityOf(
        store.getCurrentUser().id,
        (minutes) => store.getClock().offsetMinutes(minutes),
      );
      const proof = body.stepUpToken ?? "";
      if (!proof)
        return identityProblem(
          412,
          "identity.security.step-up-required",
          "Strong verification is required for this action.",
        );
      if (security.proofs.get(proof) !== "IssueApiToken")
        return invalid(
          "identity.security.step-up-invalid",
          "Step-up verification is invalid or expired.",
        );
      security.proofs.delete(proof);
      const all = tokensOf(store, params.workspaceId);
      const serial = String(all.length + 1).padStart(4, "0");
      const token = {
        id: `00000000-0000-4000-8000-0000000${String(all.length + 1).padStart(5, "0")}`,
        name,
        status: "Active" as const,
        lastUsedAt: null,
        expiresAt: body.expiresAt,
        createdAt: store.getClock().isoNow(),
        revokedAt: null,
        revokedReason: null,
        suffix: serial,
      };
      all.unshift(token);
      return created({
        id: token.id,
        rawSecret: `ntk_v1.mockSecretValue${serial}`,
        name: token.name,
        expiresAt: token.expiresAt,
        createdAt: token.createdAt,
      });
    },
  }),

  defineMockOperation<{ workspaceId: string; tokenId: string }>({
    id: "account.apiTokens.revoke",
    contract: { kind: "openapi", operationId: "Identity.ApiTokens.Revoke" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/api-tokens/:tokenId",
    async handle({ params, store }) {
      if (!allowed(store, params.workspaceId)) return notFound();
      const token = tokensOf(store, params.workspaceId).find(
        (t) => t.id === params.tokenId,
      );
      if (!token) return notFound();
      if (token.status !== "Revoked") {
        token.status = "Revoked";
        token.revokedAt = store.getClock().isoNow();
        token.revokedReason = "Manual";
      }
      return noContent();
    },
  }),
];
