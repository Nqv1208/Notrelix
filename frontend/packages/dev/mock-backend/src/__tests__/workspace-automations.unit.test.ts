import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createWorkspaceAutomationsApi } from "../../../../product/work-management/state/src/api/workspace-automations.api";
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
  return createWorkspaceAutomationsApi(client);
}

describe("workspace automation rules in the mock world", () => {
  it("creates a rule that starts off, names its item and group, and turns it on and off", async () => {
    const api = setup();
    expect(await api.list(workspaceId)).toEqual([]);

    const id = await api.create(workspaceId, {
      name: "Park the scope item",
      trigger: "ItemCreated",
      boardId: "board-1",
      targetGroupId: "group-1",
    });
    const [rule] = await api.list(workspaceId);
    expect(rule).toMatchObject({
      id,
      name: "Park the scope item",
      trigger: "ItemCreated",
      action: "MoveItem",
      boardId: "board-1",
      targetGroupId: "group-1",
      enabled: false,
    });

    await api.setEnabled(workspaceId, id, true);
    expect((await api.list(workspaceId))[0]?.enabled).toBe(true);
    await api.setEnabled(workspaceId, id, false);
    expect((await api.list(workspaceId))[0]?.enabled).toBe(false);
    expect(await api.runs(id)).toEqual([]);
  });

  it("closes the rules to a member who does not manage the workspace", async () => {
    const api = setup("viewer");
    await expect(api.list(workspaceId)).rejects.toMatchObject({
      kind: "forbidden",
    });
  });
});
