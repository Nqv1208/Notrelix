import { describe, expect, it, vi } from "vitest";
import {
  createAccountService,
  type AccountApiClient,
  type AccountEndpoints,
} from "../core";

const endpoints = {
  auth: { bootstrap: "/auth/bootstrap" },
  accounts: {
    deletedWorkspaces: (accountId: string) =>
      `/accounts/${accountId}/workspaces/deleted`,
  },
  workspaces: { restore: (id: string) => `/workspaces/${id}/restore` },
} as unknown as AccountEndpoints;

function service(api: Partial<Record<"get" | "post", unknown>>) {
  return createAccountService(api as unknown as AccountApiClient, endpoints);
}

describe("the Recently deleted adapter", () => {
  it("reads the Owner's list from the Account and keeps only well-formed rows", async () => {
    const get = vi
      .fn()
      .mockResolvedValueOnce({ accountId: "acc-1" })
      .mockResolvedValueOnce([
        {
          id: "w1",
          name: "Sales",
          slug: "sales",
          deletedAt: "2026-11-02T00:00:00Z",
          purgeAfter: "2026-12-02T00:00:00Z",
          version: 4,
        },
        { id: "w2", name: "No version", deletedAt: "x", purgeAfter: "y" },
        { id: "w3", name: "Zero", deletedAt: "x", purgeAfter: "y", version: 0 },
      ]);

    const rows = await service({ get }).listDeletedWorkspaces();

    expect(get).toHaveBeenNthCalledWith(
      1,
      "/auth/bootstrap",
      expect.anything(),
    );
    expect(get).toHaveBeenNthCalledWith(
      2,
      "/accounts/acc-1/workspaces/deleted",
      expect.anything(),
    );
    expect(rows).toEqual([
      {
        id: "w1",
        name: "Sales",
        deletedAt: "2026-11-02T00:00:00Z",
        purgeAfter: "2026-12-02T00:00:00Z",
        version: 4,
      },
    ]);
  });

  it("answers an empty list when the producer sends something else", async () => {
    const get = vi
      .fn()
      .mockResolvedValueOnce({ accountId: "a" })
      .mockResolvedValueOnce(null);
    await expect(service({ get }).listDeletedWorkspaces()).resolves.toEqual([]);
  });

  it("lists nothing, and asks nothing more, when the member has no Account", async () => {
    const get = vi.fn().mockResolvedValue({});
    await expect(service({ get }).listDeletedWorkspaces()).resolves.toEqual([]);
    expect(get).toHaveBeenCalledTimes(1);
  });

  it("restores with the version the list carried, nothing else", async () => {
    const post = vi.fn().mockResolvedValue(undefined);

    await service({ post }).restoreWorkspace("w1", 5);

    expect(post).toHaveBeenCalledWith(
      "/workspaces/w1/restore",
      { expectedVersion: 5 },
      expect.anything(),
    );
  });
});
