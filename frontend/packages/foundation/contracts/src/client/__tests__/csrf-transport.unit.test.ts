import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "../api-client";
import { createCsrfProvider } from "../csrf";

const BOOTSTRAP_PATH = "/auth/csrf";
const TOKEN_A = "token-A";
const TOKEN_B = "token-B";

function bootstrapResponse(token: string) {
  return new Response(JSON.stringify({ token }), { status: 200 });
}

function csrfProblem() {
  return new Response(
    JSON.stringify({
      type: "https://docs.notrelix.com/problems/csrf-validation-failed",
      title: "CSRF validation failed",
      status: 403,
      errorCode: "security.csrf_validation_failed",
    }),
    { status: 403 },
  );
}

describe("createCsrfProvider — IA-TST-CSRF-CLIENT-001/002", () => {
  it("bootstraps from response body without reading any cookie or meta tag (IA-TST-CSRF-CLIENT-001)", async () => {
    // No API-host cookie is accessible by design; a bare environment suffices.
    const fetchImpl = vi
      .fn()
      .mockImplementation(() => bootstrapResponse(TOKEN_A));

    const provider = createCsrfProvider({
      fetchImpl: fetchImpl as unknown as typeof fetch,
      baseUrl: "http://api.test",
      bootstrapPath: BOOTSTRAP_PATH,
      createCorrelationId: () => "corr",
    });

    const token = await provider.ensureCsrfToken();

    expect(token).toBe(TOKEN_A);
    expect(fetchImpl).toHaveBeenCalledWith(
      "http://api.test/auth/csrf",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
      }),
    );

    if (typeof document !== "undefined") {
      expect(document.cookie).toBe("");
      expect(document.querySelector('meta[name="csrf-token"]')).toBeNull();
    }
  });

  it("single-flights concurrent bootstraps within one instance (IA-TST-CSRF-CLIENT-002)", async () => {
    const fetchImpl = vi
      .fn()
      .mockImplementation(() => bootstrapResponse(TOKEN_A));

    const provider = createCsrfProvider({
      fetchImpl: fetchImpl as unknown as typeof fetch,
      baseUrl: "http://api.test",
      bootstrapPath: BOOTSTRAP_PATH,
      createCorrelationId: () => "corr",
    });

    const [t1, t2, t3] = await Promise.all([
      provider.ensureCsrfToken(),
      provider.ensureCsrfToken(),
      provider.ensureCsrfToken(),
    ]);

    expect(fetchImpl).toHaveBeenCalledTimes(1);
    expect(t1).toBe(TOKEN_A);
    expect(t2).toBe(TOKEN_A);
    expect(t3).toBe(TOKEN_A);
  });

  it("reuses the in-memory token without re-bootstrapping (memory lifecycle)", async () => {
    const fetchImpl = vi
      .fn()
      .mockImplementation(() => bootstrapResponse(TOKEN_A));

    const provider = createCsrfProvider({
      fetchImpl: fetchImpl as unknown as typeof fetch,
      baseUrl: "http://api.test",
      bootstrapPath: BOOTSTRAP_PATH,
      createCorrelationId: () => "corr",
    });

    await provider.ensureCsrfToken();
    await provider.ensureCsrfToken();

    expect(fetchImpl).toHaveBeenCalledTimes(1);

    provider.clearToken();
    await provider.ensureCsrfToken();
    expect(fetchImpl).toHaveBeenCalledTimes(2);
  });
});

describe("createNotrelixClient — CSRF transport", () => {
  function makeClient(handlers: {
    post?: (token: string | undefined) => Response;
  }) {
    const calls: Array<{ url: string; init: RequestInit }> = [];

    const fetchImpl = vi
      .fn()
      .mockImplementation(async (url: string, init: RequestInit) => {
        calls.push({ url, init });

        if (url.endsWith(BOOTSTRAP_PATH)) {
          return bootstrapResponse(TOKEN_A);
        }

        if (init.method === "POST") {
          const token =
            new Headers(init.headers).get("X-CSRF-Token") ?? undefined;
          return handlers.post
            ? handlers.post(token)
            : new Response(null, { status: 200 });
        }

        return new Response(JSON.stringify({ ok: true }), { status: 200 });
      });

    const client = createNotrelixClient({
      baseUrl: "http://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
      createCorrelationId: () => "corr",
    });

    return { client, calls };
  }

  it("unsafe request attaches X-CSRF-Token and credentials include, never X-XSRF-TOKEN (IA-TST-CSRF-CLIENT-003)", async () => {
    const { client, calls } = makeClient({
      post: (token) =>
        token === TOKEN_A
          ? new Response(JSON.stringify({ ok: true }), { status: 200 })
          : new Response(null, { status: 500 }),
    });

    await client.api.post("/commands", { title: "x" });

    const postCall = calls.find((c) => c.init.method === "POST");
    expect(postCall).toBeDefined();

    const headers = new Headers(postCall!.init.headers);
    expect(headers.get("X-CSRF-Token")).toBe(TOKEN_A);
    expect(headers.has("X-XSRF-TOKEN")).toBe(false);
    expect(postCall!.init.credentials).toBe("include");
  });

  it("safe GET does not trigger a bootstrap request (IA-TST-CSRF-CLIENT-004)", async () => {
    const { client, calls } = makeClient({});

    await client.api.get("/items");

    expect(calls.some((c) => c.url.includes(BOOTSTRAP_PATH))).toBe(false);
    expect(calls.some((c) => c.init.method !== "GET")).toBe(false);
  });

  it("auth refresh goes through the shared CSRF-aware primitive (IA-TST-CSRF-CLIENT-005)", async () => {
    const calls: Array<{ url: string; init: RequestInit }> = [];

    const fetchImpl = vi
      .fn()
      .mockImplementation(async (url: string, init: RequestInit) => {
        calls.push({ url, init });

        if (url.endsWith(BOOTSTRAP_PATH)) {
          return bootstrapResponse(TOKEN_A);
        }
        if (url.endsWith("/auth/refresh")) {
          return new Response(JSON.stringify({ ok: true }), { status: 200 });
        }

        // First resource call is unauthorized and triggers the refresh path.
        return calls.filter((c) => c.url.endsWith("/auth/refresh")).length > 0
          ? new Response(JSON.stringify({ success: true }), { status: 200 })
          : new Response(null, { status: 401 });
      });

    const client = createNotrelixClient({
      baseUrl: "http://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
      createCorrelationId: () => "corr",
    });

    const result = await client.api.get<{ success: boolean }>("/resource");

    expect(result.success).toBe(true);

    const refreshCall = calls.find((c) => c.url.endsWith("/auth/refresh"));
    expect(refreshCall).toBeDefined();

    const refreshHeaders = new Headers(refreshCall!.init.headers);
    expect(refreshHeaders.get("X-CSRF-Token")).toBe(TOKEN_A);
    expect(refreshCall!.init.credentials).toBe("include");
    expect(refreshHeaders.has("X-XSRF-TOKEN")).toBe(false);
  });

  it("does not persist the CSRF token into web storage (IA-TST-CSRF-CLIENT-006, runtime aspect)", async () => {
    const { client } = makeClient({
      post: () => new Response(null, { status: 200 }),
    });

    await client.api.post("/commands", {});

    if (typeof Storage !== "undefined") {
      expect(sessionStorage.getItem("csrf")).toBeNull();
      expect(localStorage.getItem("csrf")).toBeNull();
    }
  });

  it("stale-token recovery is bounded: clear → one bootstrap → one retry (IA-TST-CSRF-REL-001)", async () => {
    let bootstrapCount = 0;
    let postAttempts = 0;
    const seenTokens: Array<string | undefined> = [];

    const fetchImpl = vi
      .fn()
      .mockImplementation(async (url: string, init: RequestInit) => {
        if (url.endsWith(BOOTSTRAP_PATH)) {
          bootstrapCount++;
          return bootstrapResponse(bootstrapCount === 1 ? TOKEN_A : TOKEN_B);
        }

        if (init.method === "POST") {
          postAttempts++;
          seenTokens.push(
            new Headers(init.headers).get("X-CSRF-Token") ?? undefined,
          );
          if (postAttempts === 1) return csrfProblem(); // stale token rejected
          return new Response(JSON.stringify({ ok: true }), { status: 200 });
        }

        return new Response(null, { status: 200 });
      });

    const client = createNotrelixClient({
      baseUrl: "http://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
      createCorrelationId: () => "corr",
    });

    const result = await client.api.post<{ ok: boolean }>("/commands", {});

    expect(result.ok).toBe(true);
    expect(seenTokens).toEqual([TOKEN_A, TOKEN_B]);
    expect(postAttempts).toBe(2);
    expect(bootstrapCount).toBe(2);
  });

  it("a second consecutive CSRF rejection surfaces instead of looping (bounded failure)", async () => {
    let postAttempts = 0;

    const fetchImpl = vi
      .fn()
      .mockImplementation(async (url: string, init: RequestInit) => {
        if (url.endsWith(BOOTSTRAP_PATH)) {
          return bootstrapResponse(`token-${bootstrapCounter++}`);
        }

        if (init.method === "POST") {
          postAttempts++;
          return csrfProblem();
        }

        return new Response(null, { status: 200 });
      });
    let bootstrapCounter = 0;

    const client = createNotrelixClient({
      baseUrl: "http://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
      createCorrelationId: () => "corr",
    });

    await expect(client.api.post("/commands", {})).rejects.toThrow();
    expect(postAttempts).toBe(2);
  });
});

/**
 * IA-TST-X-CSRF-001 / IAREQ126 / IAREQ140 — the production client transport
 * carries no active legacy XSRF contract and no cookie/storage CSRF discovery.
 * Source-level gate: deterministic in every host environment.
 */
describe("CSRF source gate", () => {
  const forbidden = [
    "document.cookie",
    'meta[name="csrf-token"]',
    "XSRF-TOKEN",
    "X-XSRF-TOKEN",
    "getCsrfToken",
    "localStorage.getItem",
    "localStorage.setItem",
    "sessionStorage.getItem",
    "sessionStorage.setItem",
  ] as const;

  it("client transport sources contain no legacy XSRF/cookie/storage CSRF contract", async () => {
    const { readFile } = await import("node:fs/promises");
    const { join, dirname } = await import("node:path");
    const dir = dirname(new URL(import.meta.url).pathname);

    const sources = ["../csrf.ts", "../api-client.ts", "../index.ts"];

    for (const file of sources) {
      const raw = await readFile(join(dir, file), "utf8");
      // Strip comments: documentation of forbidden conventions is not usage.
      const content = raw
        .replace(/\/\*[\s\S]*?\*\//g, "")
        .replace(/^\s*\/\/.*$/gm, "");

      for (const token of forbidden) {
        expect(
          content.includes(token),
          `${file} must not contain forbidden CSRF contract token '${token}'`,
        ).toBe(false);
      }
    }
  });
});
