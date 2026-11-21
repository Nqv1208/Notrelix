import { describe, expect, it } from "vitest";
import { MOCK_AUTH } from "../state/mock-auth-directory";
import { MOCK_SSO } from "../state/mock-sso";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

/** The Account's single sign-on and the company sign-in, on the Contract's routes and shapes. */
function world() {
  const store = new MockStore();
  const fetchImpl = createMockFetch(store);
  const call = async (method: string, path: string, body?: unknown) => {
    const response = await fetchImpl(`http://localhost:8000/api/v1${path}`, {
      method,
      headers: { "Content-Type": "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    const text = await response.text();
    return {
      status: response.status,
      body: text ? (JSON.parse(text) as Record<string, any>) : null,
    };
  };
  return { store, call };
}

const SSO = "/accounts/security-policy/sso";
const SECRET_VALUE =
  String.fromCharCode(65, 97, 49, 33) + Math.random().toString(36).slice(2);
const provider = {
  name: "Entra",
  issuerUrl: "https://login.example/t/v2.0",
  clientId: "cid",
  clientSecret: SECRET_VALUE,
  jitProvisioning: true,
};

describe("the setup (Owner only)", () => {
  it("reads the seeded setup with no secret anywhere in the answer", async () => {
    const { call } = world();
    const answer = await call("GET", SSO);
    expect(answer.status).toBe(200);
    expect(answer.body).toMatchObject({
      canManage: true,
      redirectUri: MOCK_SSO.redirectUri,
      requireSso: false,
      provider: { status: "Active", hasClientSecret: true },
    });
    expect(answer.body?.domains[0]).toMatchObject({
      domain: "minhan.vn",
      status: "Verified",
      txtValue: null,
      claimed: false,
    });
    expect(JSON.stringify(answer.body)).not.toMatch(/secret"?\s*:\s*"/i);
  });

  it("answers 403 to every call when the caller is not the Owner", async () => {
    const { call, store } = world();
    store.auth.accountOwner = false;
    for (const [method, path, body] of [
      ["GET", SSO],
      ["PUT", `${SSO}/provider`, provider],
      ["DELETE", `${SSO}/provider`],
      ["POST", `${SSO}/domains`, { domain: "a.example" }],
      ["POST", `${SSO}/domains/mock-sso-domain-seed/verify`],
      ["DELETE", `${SSO}/domains/mock-sso-domain-seed`],
      ["PUT", `${SSO}/require`, { required: true }],
    ] as const)
      expect((await call(method, path, body)).status, `${method} ${path}`).toBe(
        403,
      );
  });

  it("saves a provider, keeps only the fact of a secret and never echoes it", async () => {
    const { call, store } = world();
    store.auth.sso.clear();
    const saved = await call("PUT", `${SSO}/provider`, provider);
    expect(saved).toEqual({
      status: 200,
      body: { status: "Active", validationError: null },
    });
    const read = await call("GET", SSO);
    expect(read.body?.provider).toEqual({
      name: "Entra",
      issuerUrl: "https://login.example/t/v2.0",
      clientId: "cid",
      hasClientSecret: true,
      status: "Active",
      validationError: null,
      jitProvisioning: true,
    });
    expect(JSON.stringify(saved)).not.toContain(SECRET_VALUE);
    expect(JSON.stringify(read)).not.toContain(SECRET_VALUE);
  });

  it("keeps the stored secret when an update leaves it out, and wants one the first time", async () => {
    const { call, store } = world();
    store.auth.sso.clear();
    const { clientSecret: _secret, ...withoutSecret } = provider;
    const first = await call("PUT", `${SSO}/provider`, withoutSecret);
    expect(first.status).toBe(422);
    expect(first.body?.errorCode).toBe("accounts.sso.client-secret-required");
    const noName = await call("PUT", `${SSO}/provider`, {
      ...provider,
      name: " ",
    });
    expect(noName.body?.errorCode).toBe("accounts.sso.invalid-name");
    const noClient = await call("PUT", `${SSO}/provider`, {
      ...provider,
      clientId: "",
    });
    expect(noClient.body?.errorCode).toBe("accounts.sso.invalid-client");
    await call("PUT", `${SSO}/provider`, provider);
    expect((await call("PUT", `${SSO}/provider`, withoutSecret)).status).toBe(
      200,
    );
    expect((await call("GET", SSO)).body?.provider.hasClientSecret).toBe(true);
  });

  it("refuses an http or private-address issuer with accounts.sso.invalid-issuer", async () => {
    const { call } = world();
    for (const issuerUrl of [
      "http://login.example",
      "https://10.0.0.5/x",
      "https://localhost/x",
      "https://169.254.169.254/",
      "nope",
    ]) {
      const refused = await call("PUT", `${SSO}/provider`, {
        ...provider,
        issuerUrl,
      });
      expect(refused.status, issuerUrl).toBe(422);
      expect(refused.body?.errorCode).toBe("accounts.sso.invalid-issuer");
    }
  });

  it("leaves a provider whose discovery document cannot be read as a Draft with a reason", async () => {
    const { call } = world();
    const saved = await call("PUT", `${SSO}/provider`, {
      ...provider,
      issuerUrl: "https://bad.example/t",
    });
    expect(saved.body?.status).toBe("Draft");
    expect(saved.body?.validationError).toBeTruthy();
  });
});

describe("domains", () => {
  it("adds a Pending domain with its TXT record, and the same domain again answers the same row", async () => {
    const { call } = world();
    const added = await call("POST", `${SSO}/domains`, { domain: " Acme.IO " });
    expect(added.status).toBe(201);
    expect(added.body).toMatchObject({
      domain: "acme.io",
      status: "Pending",
      txtHost: "acme.io",
      claimed: false,
      verifiedAt: null,
    });
    expect(added.body?.txtValue).toMatch(/^notrelix-verify=/);
    const again = await call("POST", `${SSO}/domains`, { domain: "acme.io" });
    expect(again.status).toBe(200);
    expect(again.body?.id).toBe(added.body?.id);
  });

  it("refuses a malformed domain, a public mail provider and a domain another account holds", async () => {
    const { call } = world();
    expect(
      (await call("POST", `${SSO}/domains`, { domain: "nodot" })).body
        ?.errorCode,
    ).toBe("accounts.domain.invalid");
    const pub = await call("POST", `${SSO}/domains`, { domain: "gmail.com" });
    expect(pub.status).toBe(422);
    expect(pub.body?.errorCode).toBe("accounts.domain.public-provider");
    const claimed = await call("POST", `${SSO}/domains`, {
      domain: MOCK_SSO.claimedDomain,
    });
    expect(claimed.status).toBe(409);
    expect(claimed.body?.errorCode).toBe("accounts.domain.claimed");
  });

  it("verifies when the TXT record is published, never errors on a miss", async () => {
    const { call, store } = world();
    const row = (await call("POST", `${SSO}/domains`, { domain: "acme.io" }))
      .body!;
    store.auth.sso.publishTxt("acme.io");
    const done = await call("POST", `${SSO}/domains/${row.id}/verify`);
    expect(done.status).toBe(200);
    expect(done.body).toMatchObject({
      status: "Verified",
      txtValue: null,
    });
    expect(done.body?.verifiedAt).toBeTruthy();

    const miss = (await call("POST", `${SSO}/domains`, { domain: "b.example" }))
      .body!;
    const first = await call("POST", `${SSO}/domains/${miss.id}/verify`);
    expect(first.status).toBe(200);
    expect(first.body?.status).toBe("Pending");
    // So the page can be tried by hand, the second press finds it.
    expect(
      (await call("POST", `${SSO}/domains/${miss.id}/verify`)).body?.status,
    ).toBe("Verified");
  });

  it("answers 409 accounts.domain.claimed when another Account got there first", async () => {
    const { call } = world();
    const row = (
      await call("POST", `${SSO}/domains`, { domain: "race.example" })
    ).body!;
    const lost = await call("POST", `${SSO}/domains/${row.id}/verify`);
    expect(lost.status).toBe(409);
    expect(lost.body?.errorCode).toBe("accounts.domain.claimed");
  });

  it("removes a domain, 404 for an unknown one", async () => {
    const { call } = world();
    const row = (await call("POST", `${SSO}/domains`, { domain: "acme.io" }))
      .body!;
    expect((await call("DELETE", `${SSO}/domains/${row.id}`)).status).toBe(204);
    expect((await call("DELETE", `${SSO}/domains/${row.id}`)).status).toBe(404);
  });
});

describe("the require switch", () => {
  it("answers 409 accounts.sso.not-ready until a provider is Active and a domain Verified", async () => {
    const { call, store } = world();
    store.auth.sso.clear();
    const refused = await call("PUT", `${SSO}/require`, { required: true });
    expect(refused.status).toBe(409);
    expect(refused.body?.errorCode).toBe("accounts.sso.not-ready");
    await call("PUT", `${SSO}/provider`, provider);
    expect(
      (await call("PUT", `${SSO}/require`, { required: true })).status,
    ).toBe(409);
    const row = (await call("POST", `${SSO}/domains`, { domain: "acme.io" }))
      .body!;
    store.auth.sso.publishTxt("acme.io");
    await call("POST", `${SSO}/domains/${row.id}/verify`);
    expect(
      (await call("PUT", `${SSO}/require`, { required: true })).status,
    ).toBe(204);
    expect((await call("GET", SSO)).body?.requireSso).toBe(true);
  });

  it("keeps the Active provider and the last Verified domain while it is on (409 accounts.sso.required-in-use)", async () => {
    const { call } = world();
    expect(
      (await call("PUT", `${SSO}/require`, { required: true })).status,
    ).toBe(204);
    const removeProvider = await call("DELETE", `${SSO}/provider`);
    expect(removeProvider.status).toBe(409);
    expect(removeProvider.body?.errorCode).toBe("accounts.sso.required-in-use");
    const removeDomain = await call(
      "DELETE",
      `${SSO}/domains/mock-sso-domain-seed`,
    );
    expect(removeDomain.status).toBe(409);
    expect(removeDomain.body?.errorCode).toBe("accounts.sso.required-in-use");
    // Turning it off frees both.
    await call("PUT", `${SSO}/require`, { required: false });
    expect((await call("DELETE", `${SSO}/provider`)).status).toBe(204);
  });
});

describe("how an email signs in", () => {
  it("routes only a Verified domain with an Active provider, the same shape for everything else", async () => {
    const { call, store } = world();
    const identify = (email: string) =>
      call("POST", "/auth/identify", { email }).then((answer) => answer.body);
    expect((await identify("ha@minhan.vn"))?.sso).toMatchObject({
      orgName: "Minh An Group",
      provider: "oidc",
      enforced: false,
    });
    // An unverified (Pending) domain looks exactly like no domain.
    await call("POST", `${SSO}/domains`, { domain: "acme.io" });
    const pending = await identify("ha@acme.io");
    expect(pending).toEqual(await identify("ha@nowhere.example"));
    expect(pending).toEqual({ method: "password", sso: null });
    // A provider that is not Active routes nowhere.
    store.auth.sso.provider!.status = "Draft";
    expect(await identify("ha@minhan.vn")).toEqual({
      method: "password",
      sso: null,
    });
  });

  it("says enforced once the Account requires single sign-on", async () => {
    const { call } = world();
    await call("PUT", `${SSO}/require`, { required: true });
    const answer = await call("POST", "/auth/identify", {
      email: "ha@minhan.vn",
    });
    expect(answer.body?.sso?.enforced).toBe(true);
  });
});

describe("the company sign-in", () => {
  const start = (call: ReturnType<typeof world>["call"], email: string) =>
    call("POST", "/auth/sso/start", { email, returnUrl: "/home" });
  const callbackOf = async (
    call: ReturnType<typeof world>["call"],
    email: string,
  ) => {
    const started = await start(call, email);
    const url = new URL(started.body!.authorizationUrl);
    return call(
      "GET",
      `/auth/sso/callback?code=${url.searchParams.get("code")}&state=${url.searchParams.get("state")}`,
    );
  };

  it("starts for a routable address with an authorization address carrying a state", async () => {
    const { call } = world();
    const started = await start(call, "ha@minhan.vn");
    expect(started.status).toBe(200);
    const url = new URL(started.body!.authorizationUrl);
    expect(url.searchParams.get("state")).toBeTruthy();
  });

  it("answers 404 identity.sso.unavailable the same for an unverified domain, no domain and no provider", async () => {
    const { call, store } = world();
    await call("POST", `${SSO}/domains`, { domain: "acme.io" });
    const unverified = await start(call, "ha@acme.io");
    const none = await start(call, "ha@nowhere.example");
    store.auth.sso.provider = null;
    const noProvider = await start(call, "ha@minhan.vn");
    for (const refused of [unverified, none, noProvider]) {
      expect(refused.status).toBe(404);
      expect(refused.body?.errorCode).toBe("identity.sso.unavailable");
    }
    expect(unverified.body).toEqual(none.body);
  });

  it("opens the session at the callback and lands on the app with the safe return path", async () => {
    const { call, store } = world();
    store.logoutCurrentUser();
    const landed = await callbackOf(call, "ha@minhan.vn");
    expect(landed.body?.redirectTo).toBe("/auth/callback?returnTo=%2Fhome");
    expect((await call("GET", "/auth/me")).status).toBe(200);
  });

  it("refuses a state that is unknown or replayed, with no session", async () => {
    const { call, store } = world();
    store.logoutCurrentUser();
    const unknown = await call("GET", "/auth/sso/callback?code=c&state=nope");
    expect(unknown.body?.redirectTo).toBe(
      "/sign-in?error=identity.sso.state-invalid",
    );
    const started = await start(call, "ha@minhan.vn");
    const state = new URL(started.body!.authorizationUrl).searchParams.get(
      "state",
    );
    await call("GET", `/auth/sso/callback?code=c&state=${state}`);
    store.logoutCurrentUser();
    const replay = await call(
      "GET",
      `/auth/sso/callback?code=c&state=${state}`,
    );
    expect(replay.body?.redirectTo).toBe(
      "/sign-in?error=identity.sso.state-invalid",
    );
    expect((await call("GET", "/auth/me")).status).toBe(401);
  });

  it("carries a provider denial and each refusal as one identity.sso.* code, with no session", async () => {
    const { call, store } = world();
    store.logoutCurrentUser();
    const started = await start(call, "ha@minhan.vn");
    const state = new URL(started.body!.authorizationUrl).searchParams.get(
      "state",
    );
    expect(
      (
        await call(
          "GET",
          `/auth/sso/callback?error=access_denied&state=${state}`,
        )
      ).body?.redirectTo,
    ).toBe("/sign-in?error=identity.sso.denied");
    for (const [local, code] of [
      ["sso-failed", "failed"],
      ["sso-email-unverified", "email-unverified"],
      ["sso-domain-mismatch", "domain-mismatch"],
      ["sso-identity-conflict", "identity-conflict"],
      ["sso-not-provisioned", "not-provisioned"],
    ])
      expect(
        (await callbackOf(call, `${local}@minhan.vn`)).body?.redirectTo,
      ).toBe(`/sign-in?error=identity.sso.${code}`);
    expect((await call("GET", "/auth/me")).status).toBe(401);
  });

  it("refuses an unknown person when first-sign-in creation is off, and lets them in when it is on", async () => {
    const { call } = world();
    expect(
      (await callbackOf(call, "new.person@minhan.vn")).body?.redirectTo,
    ).toBe("/auth/callback?returnTo=%2Fhome");
    await call("PUT", `${SSO}/provider`, {
      ...provider,
      jitProvisioning: false,
    });
    // The seeded Active provider was replaced by a new Active one on the same domain.
    expect(
      (await callbackOf(call, "new.person@minhan.vn")).body?.redirectTo,
    ).toBe("/sign-in?error=identity.sso.not-provisioned");
    expect(
      (await callbackOf(call, MOCK_AUTH.ssoMemberEmail)).body?.redirectTo,
    ).toBe("/auth/callback?returnTo=%2Fhome");
  });

  it("applies the second step exactly like the Google sign-in", async () => {
    const { call, store } = world();
    expect((await callbackOf(call, "sso-mfa@minhan.vn")).body?.redirectTo).toBe(
      "/sign-in?step=mfa&returnTo=%2Fhome",
    );
    store.auth.accountMfaRequired = true;
    expect((await callbackOf(call, "ha@minhan.vn")).body?.redirectTo).toBe(
      "/sign-in?step=mfa&enroll=1&returnTo=%2Fhome",
    );
  });
});

describe("password and social sign-in while single sign-on is required", () => {
  const login = (
    call: ReturnType<typeof world>["call"],
    email: string,
    password: string,
  ) => call("POST", "/auth/login", { email, password });

  it("changes nothing while it is off", async () => {
    const { call } = world();
    expect(
      (await login(call, MOCK_AUTH.ssoMemberEmail, MOCK_AUTH.personalPassword))
        .status,
    ).toBe(200);
  });

  it("refuses a correct password of a member with 403 identity.sso-required, and a wrong one as invalid credentials", async () => {
    const { call } = world();
    await call("PUT", `${SSO}/require`, { required: true });
    const correct = await login(
      call,
      MOCK_AUTH.ssoMemberEmail,
      MOCK_AUTH.personalPassword,
    );
    expect(correct.status).toBe(403);
    expect(correct.body?.errorCode).toBe("identity.auth.sso-required");
    const wrong = await login(call, MOCK_AUTH.ssoMemberEmail, "nope");
    expect(wrong.status).toBe(401);
    expect(wrong.body?.errorCode).toBe("identity.auth.invalid-credentials");
    // An address nobody has gets the same answer as a wrong password.
    const unknown = await login(call, "ghost@minhan.vn", "nope");
    expect(unknown.status).toBe(401);
  });

  it("keeps the Owner's password as the break-glass", async () => {
    const { call, store } = world();
    store.logoutCurrentUser();
    await call("PUT", `${SSO}/require`, { required: true });
    const owner = await login(
      call,
      MOCK_AUTH.ssoOwnerEmail,
      MOCK_AUTH.personalPassword,
    );
    expect(owner.status).toBe(200);
  });

  it("refuses the Google sign-in of the company's people, with the code on the redirect", async () => {
    const { call, store } = world();
    store.logoutCurrentUser();
    await call("PUT", `${SSO}/require`, { required: true });
    store.auth.sso.domains.push({
      id: "d-test",
      domain: store.getCurrentUser().email.split("@")[1]!,
      status: "Verified",
      token: "t",
      verifiedAt: null,
      claimed: false,
      verifyAttempts: 0,
    });
    const google = await call("GET", "/auth/oauth/google/start");
    expect(google.body?.redirectTo).toBe(
      "/sign-in?error=identity.oauth.sso-required",
    );
    expect((await call("GET", "/auth/me")).status).toBe(401);
  });
});
