import { describe, expect, it, vi } from "vitest";
import {
  parseSsoDomain,
  parseSsoProviderSaved,
  parseSsoSettings,
  ssoFailureOf,
} from "../core/sso";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../core/api/account.service";

const LEAKED =
  String.fromCharCode(65, 97, 49, 33) + Math.random().toString(36).slice(2);
const SENT =
  String.fromCharCode(65, 97, 49, 33) + Math.random().toString(36).slice(2);

const domainWire = {
  id: "d1",
  domain: "minhan.vn",
  status: "Pending",
  txtHost: "minhan.vn",
  txtValue: "notrelix-verify=abc",
  verifiedAt: null,
  claimed: false,
};

const settingsWire = {
  canManage: true,
  redirectUri: "https://api.example/api/v1/auth/sso/callback",
  requireSso: false,
  provider: {
    name: "Entra",
    issuerUrl: "https://login.example/tenant/v2.0",
    clientId: "client-1",
    hasClientSecret: true,
    status: "Active",
    validationError: null,
    jitProvisioning: true,
  },
  domains: [domainWire],
};

describe("parseSsoSettings", () => {
  it("reads the wire shape of the contract field for field", () => {
    expect(parseSsoSettings(settingsWire)).toEqual(settingsWire);
  });

  it("reads an Account with no provider and no domain", () => {
    expect(
      parseSsoSettings({ ...settingsWire, provider: null, domains: [] }),
    ).toMatchObject({ provider: null, domains: [] });
  });

  it("never carries a secret, even when the wire sends one", () => {
    const parsed = parseSsoSettings({
      ...settingsWire,
      provider: { ...settingsWire.provider, clientSecret: LEAKED },
    });
    expect(JSON.stringify(parsed)).not.toContain(LEAKED);
  });

  it.each([
    ["not an object", null],
    ["no domains array", { ...settingsWire, domains: undefined }],
    [
      "an unknown provider status",
      { ...settingsWire, provider: { ...settingsWire.provider, status: "x" } },
    ],
    ["requireSso not a boolean", { ...settingsWire, requireSso: "yes" }],
  ])("refuses %s", (_name, wire) => {
    expect(() => parseSsoSettings(wire)).toThrow();
  });
});

describe("parseSsoDomain", () => {
  it("keeps the TXT value for a Pending row and drops it otherwise", () => {
    expect(parseSsoDomain(domainWire).txtValue).toBe("notrelix-verify=abc");
    expect(
      parseSsoDomain({
        ...domainWire,
        status: "Verified",
        txtValue: null,
        verifiedAt: "2026-11-01T00:00:00Z",
      }),
    ).toMatchObject({ status: "Verified", txtValue: null });
  });

  it("refuses an unknown status", () => {
    expect(() => parseSsoDomain({ ...domainWire, status: "?" })).toThrow();
  });
});

describe("parseSsoProviderSaved", () => {
  it("reads the status and the stated reason", () => {
    expect(
      parseSsoProviderSaved({ status: "Draft", validationError: "no doc" }),
    ).toEqual({ status: "Draft", validationError: "no doc" });
  });
});

describe("ssoFailureOf", () => {
  it.each([
    [{ status: 403 }, "forbidden"],
    [{ status: 409, code: "accounts.domain.claimed" }, "claimed"],
    [{ status: 409, code: "accounts.sso.not-ready" }, "not-ready"],
    [{ status: 409, code: "accounts.sso.required-in-use" }, "in-use"],
    [{ status: 422, code: "accounts.domain.invalid" }, "invalid-domain"],
    [
      { status: 422, code: "accounts.domain.public-provider" },
      "public-provider",
    ],
    [{ status: 422, code: "accounts.sso.invalid-issuer" }, "invalid-issuer"],
    [{ status: 422, code: "accounts.sso.invalid-name" }, "invalid-name"],
    [{ status: 422, code: "accounts.sso.invalid-client" }, "invalid-client"],
    [
      { status: 422, code: "accounts.sso.client-secret-required" },
      "client-secret-required",
    ],
    [{ status: 409, code: "accounts.sso.required-in-use" }, "in-use"],
    [{ status: 422, details: { errorCode: "accounts.sso.x" } }, "invalid"],
    [{ status: 500 }, "failed"],
    [undefined, "failed"],
  ])("maps %j to %s", (error, expected) => {
    expect(ssoFailureOf(error)).toBe(expected);
  });
});

function harness(answers: Record<string, unknown> = {}) {
  const calls: {
    method: string;
    url: string;
    body?: unknown;
    headers?: unknown;
  }[] = [];
  const reply = (
    method: string,
    url: string,
    body?: unknown,
    options?: unknown,
  ) => {
    calls.push({
      method,
      url,
      body,
      headers: (options as { headers?: unknown } | undefined)?.headers,
    });
    if (url === "/auth/bootstrap")
      return Promise.resolve({ accountId: "acc-1" });
    return Promise.resolve(answers[`${method} ${url}`]);
  };
  const api = {
    get: vi.fn((url: string, options?: unknown) =>
      reply("GET", url, undefined, options),
    ),
    post: vi.fn((url: string, body?: unknown, options?: unknown) =>
      reply("POST", url, body, options),
    ),
    put: vi.fn((url: string, body?: unknown, options?: unknown) =>
      reply("PUT", url, body, options),
    ),
    delete: vi.fn((url: string, options?: unknown) =>
      reply("DELETE", url, undefined, options),
    ),
    patch: vi.fn(),
    upload: vi.fn(),
  } as unknown as AccountApiClient;
  const endpoints = {
    auth: { bootstrap: "/auth/bootstrap" },
    accounts: {
      sso: "/sso",
      ssoProvider: "/sso/provider",
      ssoDomains: "/sso/domains",
      ssoDomain: (id: string) => `/sso/domains/${id}`,
      ssoDomainVerify: (id: string) => `/sso/domains/${id}/verify`,
      ssoRequire: "/sso/require",
    },
  } as unknown as AccountEndpoints;
  return { service: createAccountService(api, endpoints), calls };
}

describe("account service: single sign-on", () => {
  it("reads the settings under the Account header", async () => {
    const { service, calls } = harness({ "GET /sso": settingsWire });
    const settings = await service.getSso();
    expect(settings.provider?.hasClientSecret).toBe(true);
    expect(calls.at(-1)).toMatchObject({
      url: "/sso",
      headers: { "X-Account-Id": "acc-1" },
    });
  });

  it("saves the provider with the secret only when one was typed", async () => {
    const { service, calls } = harness({
      "PUT /sso/provider": { status: "Active", validationError: null },
    });
    await service.saveSsoProvider({
      name: "Entra",
      issuerUrl: "https://i.example",
      clientId: "c",
      jitProvisioning: true,
    });
    expect(calls.at(-1)?.body).toEqual({
      name: "Entra",
      issuerUrl: "https://i.example",
      clientId: "c",
      jitProvisioning: true,
    });
    await service.saveSsoProvider({
      name: "Entra",
      issuerUrl: "https://i.example",
      clientId: "c",
      clientSecret: SENT,
      jitProvisioning: false,
    });
    expect(calls.at(-1)?.body).toMatchObject({ clientSecret: SENT });
  });

  it("adds, verifies and removes a domain and flips the require switch", async () => {
    const { service, calls } = harness({
      "POST /sso/domains": domainWire,
      "POST /sso/domains/d1/verify": { ...domainWire, status: "Verified" },
    });
    expect((await service.addSsoDomain("minhan.vn")).id).toBe("d1");
    expect(calls.at(-1)?.body).toEqual({ domain: "minhan.vn" });
    expect((await service.verifySsoDomain("d1")).status).toBe("Verified");
    await service.removeSsoDomain("d1");
    expect(calls.at(-1)).toMatchObject({
      method: "DELETE",
      url: "/sso/domains/d1",
    });
    await service.setSsoRequired(true);
    expect(calls.at(-1)).toMatchObject({
      method: "PUT",
      url: "/sso/require",
      body: { required: true },
    });
    await service.removeSsoProvider();
    expect(calls.at(-1)).toMatchObject({
      method: "DELETE",
      url: "/sso/provider",
    });
  });
});
