/**
 * The Account's single sign-on (Account Owner only), next to `/accounts/security-policy`:
 *
 *   GET    /accounts/security-policy/sso                       the setup, never any secret
 *   PUT    /accounts/security-policy/sso/provider              save the one provider
 *   DELETE /accounts/security-policy/sso/provider
 *   POST   /accounts/security-policy/sso/domains               add a domain (201; an existing one answers its row)
 *   POST   /accounts/security-policy/sso/domains/:id/verify    the fake DNS check; a miss is not an error
 *   DELETE /accounts/security-policy/sso/domains/:id
 *   PUT    /accounts/security-policy/sso/require               409 until the provider is Active and a domain Verified
 *
 * The OpenAPI operations are not generated yet, so each is a contract gap until `pnpm codegen` carries them.
 */
import { defineMockOperation } from "../../operations/types";
import { MOCK_SSO, type MockSsoDomain } from "../../state/mock-sso";
import {
  created,
  noContent,
  notFound,
  ok,
} from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import { identityProblem } from "../identity/auth-flow.handlers";

const GAP = { kind: "gap", gapId: "CTR-GAP-IDN-SSO" } as const;
const BASE = "/accounts/security-policy/sso";

/** Only the Account Owner reads or changes it: everyone else answers 403 with the usual problem body. */
const forbidden = () => ({
  status: 403,
  body: {
    title: "Forbidden",
    status: 403,
    detail: "Only an account Owner can change this.",
  },
});

const problem = (status: number, code: string, detail: string) =>
  identityProblem(status, code, detail);

function domainWire(row: MockSsoDomain) {
  return {
    id: row.id,
    domain: row.domain,
    status: row.status,
    txtHost: row.domain,
    // The record's value is only shown while it is still to be added.
    txtValue: row.status === "Pending" ? `notrelix-verify=${row.token}` : null,
    verifiedAt: row.verifiedAt,
    claimed: row.claimed,
  };
}

function settingsWire(store: MockStore) {
  const sso = store.auth.sso;
  return {
    canManage: true,
    redirectUri: MOCK_SSO.redirectUri,
    requireSso: sso.requireSso,
    provider: sso.provider
      ? {
          name: sso.provider.name,
          issuerUrl: sso.provider.issuerUrl,
          clientId: sso.provider.clientId,
          hasClientSecret: sso.provider.hasClientSecret,
          status: sso.provider.status,
          validationError: sso.provider.validationError,
          jitProvisioning: sso.provider.jitProvisioning,
        }
      : null,
    domains: sso.domains.map(domainWire),
  };
}

export const accountSsoOperations = [
  defineMockOperation({
    id: "account.sso.get",
    contract: GAP,
    method: "GET",
    route: BASE,
    async handle({ store }) {
      if (!store.auth.accountOwner) return forbidden();
      return ok(settingsWire(store));
    },
  }),

  defineMockOperation<
    Record<string, never>,
    {
      name?: string;
      issuerUrl?: string;
      clientId?: string;
      clientSecret?: string;
      jitProvisioning?: boolean;
    },
    unknown
  >({
    id: "account.sso.provider.save",
    contract: GAP,
    method: "PUT",
    route: `${BASE}/provider`,
    async handle({ body, store }) {
      if (!store.auth.accountOwner) return forbidden();
      const result = store.auth.sso.saveProvider(body ?? {});
      if (result.kind === "invalid-issuer")
        return problem(
          422,
          "accounts.sso.invalid-issuer",
          "The provider address cannot be used.",
        );
      if (result.kind === "invalid")
        return problem(
          422,
          "accounts.sso.invalid",
          "The details are not valid.",
        );
      return ok({
        status: result.status,
        validationError: result.validationError,
      });
    },
  }),

  defineMockOperation({
    id: "account.sso.provider.remove",
    contract: GAP,
    method: "DELETE",
    route: `${BASE}/provider`,
    async handle({ store }) {
      if (!store.auth.accountOwner) return forbidden();
      if (!store.auth.sso.removeProvider())
        return problem(
          409,
          "accounts.sso.required-in-use",
          "Single sign-on is required, so the provider cannot be removed.",
        );
      return noContent();
    },
  }),

  defineMockOperation<Record<string, never>, { domain?: string }, unknown>({
    id: "account.sso.domains.add",
    contract: GAP,
    method: "POST",
    route: `${BASE}/domains`,
    async handle({ body, store }) {
      if (!store.auth.accountOwner) return forbidden();
      const result = store.auth.sso.addDomain(body?.domain ?? "");
      if (result.kind === "invalid")
        return problem(422, "accounts.domain.invalid", "Enter a valid domain.");
      if (result.kind === "public-provider")
        return problem(
          422,
          "accounts.domain.public-provider",
          "A public email provider cannot be claimed.",
        );
      if (result.kind === "claimed")
        return problem(
          409,
          "accounts.domain.claimed",
          "Another account has already verified this domain.",
        );
      const wire = domainWire(result.domain);
      return result.kind === "added" ? created(wire) : ok(wire);
    },
  }),

  defineMockOperation<{ id: string }, never, unknown>({
    id: "account.sso.domains.verify",
    contract: GAP,
    method: "POST",
    route: `${BASE}/domains/:id/verify`,
    async handle({ params, store }) {
      if (!store.auth.accountOwner) return forbidden();
      const row = store.auth.sso.verifyDomain(params.id);
      return row ? ok(domainWire(row)) : notFound("Domain not found");
    },
  }),

  defineMockOperation<{ id: string }, never, unknown>({
    id: "account.sso.domains.remove",
    contract: GAP,
    method: "DELETE",
    route: `${BASE}/domains/:id`,
    async handle({ params, store }) {
      if (!store.auth.accountOwner) return forbidden();
      const outcome = store.auth.sso.removeDomain(params.id);
      if (outcome === "not-found") return notFound("Domain not found");
      if (outcome === "in-use")
        return problem(
          409,
          "accounts.sso.required-in-use",
          "Single sign-on is required, so the last verified domain cannot be removed.",
        );
      return noContent();
    },
  }),

  defineMockOperation<Record<string, never>, { required?: boolean }, unknown>({
    id: "account.sso.require",
    contract: GAP,
    method: "PUT",
    route: `${BASE}/require`,
    async handle({ body, store }) {
      if (!store.auth.accountOwner) return forbidden();
      if (store.auth.sso.setRequired(body?.required === true) === "not-ready")
        return problem(
          409,
          "accounts.sso.not-ready",
          "An active provider and a verified domain are needed first.",
        );
      return noContent();
    },
  }),
];
