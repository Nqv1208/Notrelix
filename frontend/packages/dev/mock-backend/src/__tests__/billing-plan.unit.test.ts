import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createBillingApi } from "../../../../../apps/web/src/routes/workspaces/$workspaceId/billing/billing-api";
import type { BillingApiClient } from "../../../../../apps/web/src/routes/workspaces/$workspaceId/billing/billing-api";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

const workspaceId = mockIds.workspaces.primary;

function setup(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return {
    store,
    api: createBillingApi(client.api as unknown as BillingApiClient),
  };
}

describe("billing plan and limits in the mock world", () => {
  it("reports the Pro plan and every capability with its limit and use", async () => {
    const { api } = setup();
    const plan = await api.plan(workspaceId);
    expect(plan.currentCode).toBe("pro");
    expect(plan.currentName).toBe("Pro");

    const capabilities = await api.capabilities(workspaceId);
    expect(capabilities.map((c) => c.code)).toEqual([
      "AUTOMATION_RULE",
      "MEMBER_SEAT",
      "GUEST_SEAT",
      "STORAGE_GB",
    ]);
    expect(capabilities.find((c) => c.code === "MEMBER_SEAT")).toMatchObject({
      limit: 500,
      isUnlimited: false,
    });
  });

  it("follows the plan the operator set for the account", async () => {
    const { api, store } = setup();
    store.billing.planCode = "enterprise";
    const seats = (await api.capabilities(workspaceId)).find(
      (c) => c.code === "MEMBER_SEAT",
    );
    expect(seats).toMatchObject({ isUnlimited: true, limit: null });

    store.billing.planCode = "free";
    const free = (await api.capabilities(workspaceId)).find(
      (c) => c.code === "AUTOMATION_RULE",
    );
    expect(free?.limit).toBe(5);
  });

  it("closes the plan to a member who does not manage the workspace", async () => {
    const { api } = setup("viewer");
    await expect(api.plan(workspaceId)).rejects.toMatchObject({
      kind: "forbidden",
    });
  });
});
