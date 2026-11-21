import { isoOfMs, wallClockMs } from "./clock";

/**
 * The Account's single sign-on in the mock world: one OIDC provider, its domains with a fake
 * DNS TXT check, the require switch, and the company sign-in round trip. In memory only; the
 * client secret is never kept (the mock remembers only that one was given), exactly as the
 * contract says the real backend never returns it.
 *
 * Test values for the mock; no real provider or DNS is behind any of them.
 */

export const MOCK_SSO = {
  /** The Account's name, shown on the sign-in page as "Continue with ...". */
  orgName: "Minh An Group",
  orgInitials: "MA",
  /** What the Owner registers at the provider; the real one is `{PublicApiBase}/api/v1/auth/sso/callback`. */
  redirectUri: "http://localhost:8000/api/v1/auth/sso/callback",
  /** The seeded, verified domain: a person on it is routed to the provider. */
  seededDomain: "minhan.vn",
  /** A domain another Account has already proved: adding it answers 409 `accounts.domain.claimed`. */
  claimedDomain: "taken.example",
  /** A domain whose verify loses the race to another Account: it comes back Pending with `claimed`. */
  raceDomainPrefix: "race.",
  /** An issuer host starting with this stays a Draft with a stated reason (the discovery document cannot be read). */
  badIssuerPrefix: "bad.",
  /** The Account Owner (the password break-glass) and a plain member, both on the seeded domain. */
  ownerEmail: "owner@minhan.vn",
  memberEmail: "member@minhan.vn",
  providerLabel: "OpenID Connect",
} as const;

/** Public mail providers a company cannot claim. */
const PUBLIC_PROVIDERS = new Set([
  "gmail.com",
  "outlook.com",
  "yahoo.com",
  "hotmail.com",
  "icloud.com",
  "proton.me",
  "protonmail.com",
  "live.com",
  "aol.com",
  "gmx.com",
  "yandex.com",
  "mail.com",
  "qq.com",
  "163.com",
  "126.com",
]);

const DOMAIN = /^(?!-)[a-z0-9-]{1,63}(?<!-)(\.(?!-)[a-z0-9-]{1,63}(?<!-))+$/;

export type MockSsoProviderStatus = "Draft" | "Active" | "Disabled";
export type MockSsoDomainStatus = "Pending" | "Verified" | "Rejected";

export interface MockSsoProvider {
  name: string;
  issuerUrl: string;
  clientId: string;
  hasClientSecret: boolean;
  status: MockSsoProviderStatus;
  validationError: string | null;
  jitProvisioning: boolean;
}

export interface MockSsoDomain {
  id: string;
  domain: string;
  status: MockSsoDomainStatus;
  token: string;
  verifiedAt: string | null;
  claimed: boolean;
  verifyAttempts: number;
}

export type MockSsoProviderInput = {
  name?: string;
  issuerUrl?: string;
  clientId?: string;
  clientSecret?: string;
  jitProvisioning?: boolean;
};

export type MockSsoProviderResult =
  | {
      kind: "saved";
      status: MockSsoProviderStatus;
      validationError: string | null;
    }
  | { kind: "invalid-issuer" }
  | { kind: "invalid" };

export type MockSsoDomainAdd =
  | { kind: "added"; domain: MockSsoDomain }
  | { kind: "existing"; domain: MockSsoDomain }
  | { kind: "invalid" }
  | { kind: "public-provider" }
  | { kind: "claimed" };

/** The hosts the real SSRF guard refuses; the mock mirrors the visible ones. */
function isPrivateHost(host: string): boolean {
  return (
    host === "localhost" ||
    host.endsWith(".local") ||
    host.endsWith(".internal") ||
    /^(127\.|10\.|192\.168\.|169\.254\.|0\.)/.test(host) ||
    /^172\.(1[6-9]|2\d|3[01])\./.test(host) ||
    host === "[::1]"
  );
}

export interface MockSsoRoute {
  readonly orgName: string;
  readonly orgInitials: string;
  readonly provider: "oidc";
  readonly providerLabel: string;
  readonly enforced: boolean;
}

export class MockSsoState {
  provider: MockSsoProvider | null = null;
  domains: MockSsoDomain[] = [];
  requireSso = false;
  private sequence = 0;
  /** The fake DNS: domains whose TXT record has been published (a mock-only helper, see `publishTxt`). */
  private dns = new Set<string>();
  /** Open authorization states: state -> who asked and where to land. One use each. */
  private states = new Map<
    string,
    { email: string; returnTo: string | null }
  >();

  constructor(
    private readonly nowIso: () => string = () => isoOfMs(wallClockMs()),
  ) {
    this.reset();
  }

  /** The seeded world: the company is set up and its domain is verified, so a person on it is routed to the provider. */
  reset(): void {
    this.sequence = 0;
    this.dns.clear();
    this.states.clear();
    this.requireSso = false;
    this.provider = {
      name: MOCK_SSO.orgName,
      issuerUrl: "https://login.minhan.example/tenant/v2.0",
      clientId: "mock-client-id",
      hasClientSecret: true,
      status: "Active",
      validationError: null,
      jitProvisioning: true,
    };
    this.domains = [
      {
        id: "mock-sso-domain-seed",
        domain: MOCK_SSO.seededDomain,
        status: "Verified",
        token: "seed",
        verifiedAt: "2026-10-01T08:00:00.000Z",
        claimed: false,
        verifyAttempts: 0,
      },
    ];
  }

  /** The empty world, for tests that start from nothing. */
  clear(): void {
    this.reset();
    this.provider = null;
    this.domains = [];
  }

  /** Mock-only: the Owner has published the TXT record at the DNS provider, so the next verify succeeds. */
  publishTxt(domain: string): void {
    this.dns.add(domain.trim().toLowerCase());
  }

  /** Ready for the require switch: an Active provider and a Verified domain. */
  isReady(): boolean {
    return (
      this.provider?.status === "Active" &&
      this.domains.some((domain) => domain.status === "Verified")
    );
  }

  saveProvider(input: MockSsoProviderInput): MockSsoProviderResult {
    const name = input.name?.trim() ?? "";
    const clientId = input.clientId?.trim() ?? "";
    let host: string;
    let secure: boolean;
    try {
      const url = new URL(input.issuerUrl?.trim() ?? "");
      host = url.hostname.toLowerCase();
      secure = url.protocol === "https:";
    } catch {
      return { kind: "invalid-issuer" };
    }
    if (!secure || isPrivateHost(host)) return { kind: "invalid-issuer" };
    const secret = input.clientSecret?.trim() ?? "";
    // The secret is optional on an update (keep the stored one) but needed for the first save.
    if (!name || !clientId || (!this.provider?.hasClientSecret && !secret))
      return { kind: "invalid" };
    const draft = host.startsWith(MOCK_SSO.badIssuerPrefix);
    this.provider = {
      name,
      issuerUrl: input.issuerUrl!.trim(),
      clientId,
      // Only the fact is kept: the value is dropped here, never stored, never returned.
      hasClientSecret: secret !== "" || this.provider?.hasClientSecret === true,
      status: draft ? "Draft" : "Active",
      validationError: draft
        ? "The provider's discovery document could not be read."
        : null,
      jitProvisioning: input.jitProvisioning !== false,
    };
    return {
      kind: "saved",
      status: this.provider.status,
      validationError: this.provider.validationError,
    };
  }

  /** `false` while the Active provider is in use by the require switch. */
  removeProvider(): boolean {
    if (this.requireSso) return false;
    this.provider = null;
    return true;
  }

  addDomain(raw: string): MockSsoDomainAdd {
    const domain = raw.trim().toLowerCase();
    if (!DOMAIN.test(domain)) return { kind: "invalid" };
    if (PUBLIC_PROVIDERS.has(domain)) return { kind: "public-provider" };
    if (domain === MOCK_SSO.claimedDomain) return { kind: "claimed" };
    const existing = this.domains.find((entry) => entry.domain === domain);
    if (existing) return { kind: "existing", domain: existing };
    const row: MockSsoDomain = {
      id: `mock-sso-domain-${(this.sequence += 1)}`,
      domain,
      status: "Pending",
      token: `mock-token-${this.sequence}`,
      verifiedAt: null,
      claimed: false,
      verifyAttempts: 0,
    };
    this.domains.push(row);
    return { kind: "added", domain: row };
  }

  domain(id: string): MockSsoDomain | undefined {
    return this.domains.find((entry) => entry.id === id);
  }

  /**
   * The fake DNS check. It finds the record when `publishTxt` was called for the domain, or, so
   * the page can be tried by hand, on the second press; never an error for a miss. A domain whose
   * label starts with `race.` loses to another Account: still Pending, marked claimed.
   */
  verifyDomain(id: string): MockSsoDomain | undefined {
    const row = this.domain(id);
    if (!row || row.status === "Verified") return row;
    row.verifyAttempts += 1;
    if (row.domain.startsWith(MOCK_SSO.raceDomainPrefix)) {
      row.claimed = true;
      return row;
    }
    if (this.dns.has(row.domain) || row.verifyAttempts >= 2) {
      row.status = "Verified";
      row.verifiedAt = this.nowIso();
    }
    return row;
  }

  /** `false` while the last Verified domain is in use by the require switch. */
  removeDomain(id: string): "removed" | "not-found" | "in-use" {
    const row = this.domain(id);
    if (!row) return "not-found";
    const verified = this.domains.filter(
      (entry) => entry.status === "Verified",
    );
    if (this.requireSso && row.status === "Verified" && verified.length === 1)
      return "in-use";
    this.domains = this.domains.filter((entry) => entry.id !== id);
    return "removed";
  }

  setRequired(required: boolean): "ok" | "not-ready" {
    if (required && !this.isReady()) return "not-ready";
    this.requireSso = required;
    return "ok";
  }

  /** The route of an email: a function of its domain only. Not routable = `null`. */
  routeOf(email: string): MockSsoRoute | null {
    const domain = email.trim().toLowerCase().split("@")[1] ?? "";
    const verified = this.domains.some(
      (entry) => entry.domain === domain && entry.status === "Verified",
    );
    if (!verified || this.provider?.status !== "Active") return null;
    return {
      orgName: MOCK_SSO.orgName,
      orgInitials: MOCK_SSO.orgInitials,
      provider: "oidc",
      providerLabel: MOCK_SSO.providerLabel,
      enforced: this.requireSso,
    };
  }

  /** Whether a password (or social) sign-in of this email is refused: its domain requires single sign-on. */
  isEnforcedFor(email: string): boolean {
    return this.routeOf(email)?.enforced === true;
  }

  isOwner(email: string): boolean {
    return email.trim().toLowerCase() === MOCK_SSO.ownerEmail;
  }

  /** Opens an authorization state for a routable email; `null` when it does not route. */
  start(email: string, returnTo: string | null): string | null {
    if (!this.routeOf(email)) return null;
    const state = `mock-sso-state-${(this.sequence += 1)}`;
    this.states.set(state, { email: email.trim().toLowerCase(), returnTo });
    return state;
  }

  /** Takes an authorization state; it works once (a replay finds nothing). */
  take(state: string): { email: string; returnTo: string | null } | undefined {
    const entry = this.states.get(state);
    this.states.delete(state);
    return entry;
  }
}
