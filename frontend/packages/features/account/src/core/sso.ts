/**
 * The Account's single sign-on (OIDC), as the Owner sets it up: one provider, the
 * domains that route sign-in to it and the switch that makes it the only way in.
 *
 * Wire shapes are written by hand here until the generated contracts carry the
 * operations; the parse functions are the one place a response is read, and a
 * malformed answer throws so the page shows a request failure rather than a
 * half-drawn form. The client secret is write-only: no type here has a field for it
 * on the read side.
 */

export type SsoProviderStatus = "Draft" | "Active" | "Disabled";
export type SsoDomainStatus = "Pending" | "Verified" | "Rejected";

export interface SsoProvider {
  readonly name: string;
  readonly issuerUrl: string;
  readonly clientId: string;
  /** A secret is stored; its value is never read back. */
  readonly hasClientSecret: boolean;
  readonly status: SsoProviderStatus;
  /** Why the provider stayed a Draft (the discovery document could not be read, ...). */
  readonly validationError: string | null;
  readonly jitProvisioning: boolean;
}

export interface SsoDomain {
  readonly id: string;
  readonly domain: string;
  readonly status: SsoDomainStatus;
  /** The DNS name the TXT record goes on. */
  readonly txtHost: string;
  /** The TXT record's value; only while the row is Pending. */
  readonly txtValue: string | null;
  readonly verifiedAt: string | null;
  /** Another Account has proved this domain first. */
  readonly claimed: boolean;
}

export interface SsoSettings {
  /** The caller is the Account Owner. */
  readonly canManage: boolean;
  /** The address to register at the provider as the redirect (callback) URI. */
  readonly redirectUri: string;
  readonly requireSso: boolean;
  readonly provider: SsoProvider | null;
  readonly domains: readonly SsoDomain[];
}

/** `PUT .../sso/provider` body; `clientSecret` is omitted to keep the stored one. */
export interface SsoProviderInput {
  readonly name: string;
  readonly issuerUrl: string;
  readonly clientId: string;
  readonly clientSecret?: string;
  readonly jitProvisioning: boolean;
}

export interface SsoProviderSaved {
  readonly status: SsoProviderStatus;
  readonly validationError: string | null;
}

const malformed = (what: string): never => {
  throw new Error(`sso.malformed: ${what}`);
};

const record = (wire: unknown, what: string): Record<string, unknown> =>
  typeof wire === "object" && wire !== null && !Array.isArray(wire)
    ? (wire as Record<string, unknown>)
    : malformed(what);

const text = (value: unknown, what: string): string =>
  typeof value === "string" ? value : malformed(what);

const flag = (value: unknown, what: string): boolean =>
  typeof value === "boolean" ? value : malformed(what);

const textOrNull = (value: unknown, what: string): string | null =>
  value === null || value === undefined ? null : text(value, what);

function oneOf<T extends string>(
  value: unknown,
  allowed: readonly T[],
  what: string,
): T {
  return typeof value === "string" &&
    (allowed as readonly string[]).includes(value)
    ? (value as T)
    : malformed(what);
}

const PROVIDER_STATUS = ["Draft", "Active", "Disabled"] as const;
const DOMAIN_STATUS = ["Pending", "Verified", "Rejected"] as const;

export function parseSsoDomain(wire: unknown): SsoDomain {
  const row = record(wire, "domain");
  const status = oneOf(row.status, DOMAIN_STATUS, "domain.status");
  return {
    id: text(row.id, "domain.id"),
    domain: text(row.domain, "domain.domain"),
    status,
    txtHost: text(row.txtHost, "domain.txtHost"),
    txtValue:
      status === "Pending" ? textOrNull(row.txtValue, "domain.txtValue") : null,
    verifiedAt: textOrNull(row.verifiedAt, "domain.verifiedAt"),
    claimed: row.claimed === true,
  };
}

export function parseSsoProviderSaved(wire: unknown): SsoProviderSaved {
  const answer = record(wire, "provider answer");
  return {
    status: oneOf(answer.status, PROVIDER_STATUS, "provider.status"),
    validationError: textOrNull(answer.validationError, "validationError"),
  };
}

function parseSsoProvider(wire: unknown): SsoProvider {
  const row = record(wire, "provider");
  return {
    name: text(row.name, "provider.name"),
    issuerUrl: text(row.issuerUrl, "provider.issuerUrl"),
    clientId: text(row.clientId, "provider.clientId"),
    hasClientSecret: flag(row.hasClientSecret, "provider.hasClientSecret"),
    status: oneOf(row.status, PROVIDER_STATUS, "provider.status"),
    validationError: textOrNull(
      row.validationError,
      "provider.validationError",
    ),
    jitProvisioning: flag(row.jitProvisioning, "provider.jitProvisioning"),
  };
}

export function parseSsoSettings(wire: unknown): SsoSettings {
  const settings = record(wire, "settings");
  if (!Array.isArray(settings.domains)) malformed("domains");
  return {
    canManage: flag(settings.canManage, "canManage"),
    redirectUri: text(settings.redirectUri, "redirectUri"),
    requireSso: flag(settings.requireSso, "requireSso"),
    provider:
      settings.provider === null || settings.provider === undefined
        ? null
        : parseSsoProvider(settings.provider),
    domains: (settings.domains as unknown[]).map(parseSsoDomain),
  };
}

/** What went wrong with a refused SSO setup call, as the page tells it. */
export type SsoFailure =
  | "forbidden"
  | "claimed"
  | "invalid-domain"
  | "public-provider"
  | "invalid-issuer"
  | "invalid-name"
  | "invalid-client"
  | "client-secret-required"
  | "invalid"
  | "not-ready"
  | "in-use"
  | "failed";

interface RefusalLike {
  readonly status?: number;
  readonly code?: unknown;
  readonly details?: { readonly errorCode?: unknown } | null;
}

const FAILURE_BY_CODE: Readonly<Record<string, SsoFailure>> = {
  "accounts.domain.claimed": "claimed",
  "accounts.domain.invalid": "invalid-domain",
  "accounts.domain.public-provider": "public-provider",
  "accounts.sso.invalid-issuer": "invalid-issuer",
  "accounts.sso.invalid-name": "invalid-name",
  "accounts.sso.invalid-client": "invalid-client",
  "accounts.sso.client-secret-required": "client-secret-required",
  "accounts.sso.not-ready": "not-ready",
  "accounts.sso.required-in-use": "in-use",
};

export function ssoFailureOf(error: unknown): SsoFailure {
  const refusal = (error ?? {}) as RefusalLike;
  if (refusal.status === 403) return "forbidden";
  const code =
    typeof refusal.code === "string"
      ? refusal.code
      : typeof refusal.details?.errorCode === "string"
        ? refusal.details.errorCode
        : "";
  const known = FAILURE_BY_CODE[code];
  if (known) return known;
  // Any other 422 is a malformed field the page could not see itself.
  return refusal.status === 422 ? "invalid" : "failed";
}
