import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

/** The pre-login calls on the backend's own routes and shapes. */
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

describe("identify", () => {
  it("answers the method and the sso block, and 400 on a bad email", async () => {
    const { call } = world();
    expect(
      (await call("POST", "/auth/identify", { email: "ai@example.com" })).body,
    ).toEqual({ method: "password", sso: null });
    const company = await call("POST", "/auth/identify", {
      email: "ha@minhan.vn",
    });
    expect(company.body?.method).toBe("sso");
    expect(company.body?.sso?.providerLabel).toBe("Microsoft Entra");
    const bad = await call("POST", "/auth/identify", { email: "x" });
    expect(bad.status).toBe(400);
    expect(bad.body?.errorCode).toBe("validation.failed");
    expect(bad.body?.errors?.Email).toBeTruthy();
  });
});

describe("provider sign-in", () => {
  it("opens the session on the backend's start route", async () => {
    const { call, store } = world();
    store.logoutCurrentUser();
    expect((await call("GET", "/auth/me")).status).toBe(401);
    expect((await call("GET", "/auth/oauth/google/start")).status).toBe(200);
    expect((await call("GET", "/auth/me")).status).toBe(200);
  });
});

describe("invitations", () => {
  it("previews with the backend's shape and never leaks a used invitation's guest", async () => {
    const { call } = world();
    const valid = await call("POST", "/invitations/preview", {
      token: "invite-erp-guest",
    });
    expect(valid.body).toMatchObject({
      state: "valid",
      workspaceName: "Minh An Group",
      resourceKind: "work-management.board",
      resourceTitle: "Triển khai ERP Q4",
      resourceLevel: "Editor",
      expiresOn: "2027-01-31",
      email: "tuan@doitac-erp.vn",
      hasAccount: false,
    });
    const used = await call("POST", "/invitations/preview", {
      token: "invite-erp-used",
    });
    expect(used.body).toMatchObject({
      state: "used",
      email: null,
      hasAccount: false,
    });
    expect(
      (await call("POST", "/invitations/preview", { token: "nope" })).status,
    ).toBe(404);
  });

  it("accepts with registration once; a second press adds nobody and opens no session", async () => {
    const { call, store } = world();
    store.logoutCurrentUser();
    const short = await call("POST", "/invitations/accept-with-registration", {
      token: "invite-erp-guest",
      name: "Tuấn",
      password: "ngan",
    });
    expect(short.status).toBe(400);
    expect(short.body?.errors?.Password).toBeTruthy();
    const first = await call("POST", "/invitations/accept-with-registration", {
      token: "invite-erp-guest",
      name: "Tuấn",
      password: "mot-mat-khau",
    });
    expect(first.status).toBe(200);
    expect(first.body).toMatchObject({
      workspaceId: "mock-workspace-primary",
      resourceKind: "work-management.board",
      resourceId: "mock-board-erp",
      alreadyAccepted: false,
      user: { email: "tuan@doitac-erp.vn", name: "Tuấn" },
    });
    expect((await call("GET", "/auth/me")).status).toBe(200);
    store.logoutCurrentUser();
    const second = await call("POST", "/invitations/accept-with-registration", {
      token: "invite-erp-guest",
      name: "Tuấn",
      password: "mot-mat-khau",
    });
    expect(second.body).toMatchObject({ alreadyAccepted: true, user: null });
    expect((await call("GET", "/auth/me")).status).toBe(401);
  });

  it("answers 409 for an email that has an account and 400 for an expired link", async () => {
    const { call } = world();
    expect(
      (
        await call("POST", "/invitations/accept-with-registration", {
          token: "invite-erp-existing",
          name: "Hà",
          password: "mot-mat-khau",
        })
      ).status,
    ).toBe(409);
    expect(
      (
        await call("POST", "/invitations/accept-with-registration", {
          token: "invite-erp-expired",
          name: "Khách",
          password: "mot-mat-khau",
        })
      ).status,
    ).toBe(400);
  });

  it("the signed-in accept needs the session", async () => {
    const { call, store } = world();
    store.logoutCurrentUser();
    expect(
      (
        await call("POST", "/invitations/accept", {
          token: "invite-erp-existing",
        })
      ).status,
    ).toBe(401);
    store.loginCurrentUser();
    const accepted = await call("POST", "/invitations/accept", {
      token: "invite-erp-existing",
    });
    expect(accepted.status).toBe(200);
    expect(accepted.body?.resourceId).toBe("mock-board-erp");
  });

  it("declines with 204, twice, and refuses an expired or accepted one", async () => {
    const { call } = world();
    const decline = (token: string) =>
      call("POST", "/invitations/decline", { token });
    expect((await decline("invite-erp-guest")).status).toBe(204);
    expect((await decline("invite-erp-guest")).status).toBe(204);
    expect((await decline("invite-erp-expired")).status).toBe(400);
    expect((await decline("nope")).status).toBe(404);
  });
});

describe("CSRF on the anonymous calls", () => {
  it("bootstraps the pair once and sends it on every unsafe call of the flow", async () => {
    const store = new MockStore();
    const inner = createMockFetch(store);
    const seen: Array<{ method: string; path: string; csrf: string | null }> =
      [];
    const fetchImpl: typeof fetch = (input, init) => {
      const headers = new Headers(init?.headers);
      seen.push({
        method: (init?.method ?? "GET").toUpperCase(),
        path: new URL(String(input)).pathname,
        csrf: headers.get("X-CSRF-Token"),
      });
      return inner(input, init);
    };
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl,
    });
    await client.api.post("/auth/identify", { email: "ai@example.com" });
    await client.api.post("/invitations/preview", {
      token: "invite-erp-guest",
    });
    await client.api.post("/invitations/decline", {
      token: "invite-erp-guest",
    });
    const bootstraps = seen.filter((call) => call.path.endsWith("/auth/csrf"));
    expect(bootstraps).toHaveLength(1);
    const unsafe = seen.filter((call) => call.method === "POST");
    expect(unsafe).toHaveLength(3);
    for (const call of unsafe) expect(call.csrf).toBe("mock-csrf-token");
  });
});
