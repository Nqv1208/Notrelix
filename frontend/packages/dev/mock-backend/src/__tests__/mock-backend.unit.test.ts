import { describe, it, expect } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { createMockFetch } from "../transport/create-mock-fetch";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";

describe("@notrelix/dev-mock-backend — Canonical v3 Architecture", () => {
  it("exercises real createNotrelixClient via injected fetchImpl: MockFetch", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);

    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const profile = await client.api.get<{ id: string; email: string }>(
      endpoints.auth.profile,
    );

    expect(profile.id).toBe(mockIds.users.owner);
    expect(profile.email).toBe("ui-dev@notrelix.local");
  });

  it("fetches workspace list through real NotrelixClient and MockFetch", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);

    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const workspaces = await client.api.get<
      Array<{ id: string; name: string }>
    >(endpoints.workspaces.list);

    expect(workspaces.length).toBeGreaterThan(0);
    expect(workspaces[0]?.name).toBe("Notrelix Product Lab");
  });

  it("fails closed on unhandled operations with MockUnhandledOperationError", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);

    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    await expect(
      client.api.get("/unmapped-unknown-endpoint"),
    ).rejects.toThrow();
  });
});
