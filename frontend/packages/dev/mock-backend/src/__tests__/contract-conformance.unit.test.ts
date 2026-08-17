import { describe, it, expect } from "vitest";
import { buildOperationRegistry } from "../operations/build-registry";
import { createMockFetch } from "../transport/create-mock-fetch";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";

const CONFORMANCE_CATALOG = [
  "identity.profile",
  "identity.refresh",
  "identity.logout",
  "workspace.list",
  "workspace.create",
  "workspace.get",
  "workspace.views.list",
  "workspace.members.list",
  "account.preferences.get",
  "account.preferences.update",
  "account.profile.update",
  "notifications.list",
  "notifications.read",
  "notifications.readAll",
  "boards.listByWorkspace",
  "boards.detail",
  "boards.full",
  "boards.create",
  "lists.byBoard",
  "lists.create",
  "lists.reorder",
  "cards.byList",
  "cards.detail",
  "cards.create",
  "cards.move",
  "pages.list",
  "pages.tree",
  "pages.detail",
  "pages.breadcrumb",
  "pages.blocks",
] as const;

describe("MFB-FZ-07: Contract Behavior and Conformance Matrix", () => {
  it("T-MFB-001: real createNotrelixClient loads endpoints via mockFetch", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const user = await client.api.get<{ id: string }>(endpoints.auth.profile);
    expect(user.id).toBe(store.getCurrentUser().id);
  });

  it("T-MFB-002: unmapped operation fails closed with error and never falls back to network", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    await expect(
      client.api.get("/api/v1/completely/unmapped/endpoint"),
    ).rejects.toThrow();
  });

  it("T-MFB-023: contract catalog closure — all registered operations match the conformance catalog", () => {
    const registry = buildOperationRegistry();
    const registeredIds = new Set(registry.operationIds());
    const catalogIds = new Set(CONFORMANCE_CATALOG);

    expect(registeredIds).toEqual(catalogIds);
  });

  it("T-MFB-024: GET operations do not mutate store state", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const getWorkspacesCount = () => store.getWorkspaces().length;
    const getBoardsCount = () =>
      store.getBoards(mockIds.workspaces.primary).length;

    const initialWorkspaces = getWorkspacesCount();
    const initialBoards = getBoardsCount();

    // Call various GET endpoints
    await client.api.get(endpoints.auth.profile);
    await client.api.get(endpoints.workspaces.list);
    await client.api.get(
      endpoints.workspaces.detail(mockIds.workspaces.primary),
    );
    await client.api.get(
      endpoints.boards.listByWorkspaceId(mockIds.workspaces.primary),
    );
    await client.api.get(endpoints.notifications.list);

    expect(getWorkspacesCount()).toBe(initialWorkspaces);
    expect(getBoardsCount()).toBe(initialBoards);
  });

  it("T-MFB-025: Search contract is CONTRACT-BLOCKED and not registered in registry", () => {
    const registry = buildOperationRegistry();
    const searchOps = registry
      .operationIds()
      .filter((id) => id.toLowerCase().includes("search"));

    expect(searchOps).toHaveLength(0);
  });
});
