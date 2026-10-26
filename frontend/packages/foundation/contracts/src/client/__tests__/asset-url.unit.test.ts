import { describe, expect, it } from "vitest";
import { configureApiAssetOrigin, resolveApiAssetUrl } from "../asset-url";

describe("resolveApiAssetUrl", () => {
  it("places an API path on the API origin", () => {
    configureApiAssetOrigin("https://api.notrelix.test/api/v1");
    expect(resolveApiAssetUrl("/api/v1/users/u1/avatar?v=a1")).toBe(
      "https://api.notrelix.test/api/v1/users/u1/avatar?v=a1",
    );
  });

  it("keeps absolute, data and blob addresses and an absent one", () => {
    configureApiAssetOrigin("https://api.notrelix.test/api/v1");
    expect(resolveApiAssetUrl("https://cdn.test/a.png")).toBe(
      "https://cdn.test/a.png",
    );
    expect(resolveApiAssetUrl("//cdn.test/a.png")).toBe("//cdn.test/a.png");
    expect(resolveApiAssetUrl("data:image/png;base64,AAA")).toBe(
      "data:image/png;base64,AAA",
    );
    expect(resolveApiAssetUrl(null)).toBeNull();
    expect(resolveApiAssetUrl(undefined)).toBeNull();
    expect(resolveApiAssetUrl("")).toBeNull();
  });

  it("leaves a path alone when the API base is relative (same origin)", () => {
    configureApiAssetOrigin("/api/v1");
    expect(resolveApiAssetUrl("/api/v1/users/u1/avatar?v=a1")).toBe(
      "/api/v1/users/u1/avatar?v=a1",
    );
  });
});

describe("a refused call", () => {
  it("carries the producer's dotted errorCode on the error", async () => {
    const { createNotrelixClient } = await import("../api-client");
    const fetchImpl = (async (url: string) =>
      String(url).includes("/auth/csrf")
        ? new Response(JSON.stringify({ token: "t" }), { status: 200 })
        : new Response(
            JSON.stringify({
              status: 409,
              errorCode: "workspaces.member.last-owner-cannot-leave",
              detail: "The last owner cannot leave.",
            }),
            {
              status: 409,
              headers: { "Content-Type": "application/problem+json" },
            },
          )) as unknown as typeof fetch;
    const { api } = createNotrelixClient({
      baseUrl: "http://api.test/api/v1",
      fetchImpl,
    });
    await expect(api.delete("/account/workspaces/w1")).rejects.toMatchObject({
      status: 409,
      code: "workspaces.member.last-owner-cannot-leave",
    });
  });
});
