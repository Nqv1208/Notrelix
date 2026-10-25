import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createWorkspaceAutomationsApi } from "../../../../product/work-management/state/src/api/workspace-automations.api";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

const workspaceId = mockIds.workspaces.primary;
const boardId = "board-1";

function setup(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return createWorkspaceAutomationsApi(client);
}

describe("workspace automation rules in the mock world", () => {
  it("creates a rule with conditions and ordered actions, turns it on and off, edits it into a new version and deletes it", async () => {
    const api = setup();
    expect(await api.list(workspaceId, boardId)).toEqual([]);

    const id = await api.create(workspaceId, {
      name: "Park the scope item",
      trigger: "ItemCreated",
      boardId,
      conditions: [{ type: "group", groupId: "group-0" }],
      actions: [
        { type: "ChangeStatus", optionId: "option-1" },
        { type: "AssignPerson", userId: "user-1" },
        { type: "NotifyMember", userId: "user-1", message: "Parked" },
      ],
    });
    const [rule] = await api.list(workspaceId, boardId);
    expect(rule).toMatchObject({
      id,
      name: "Park the scope item",
      trigger: "ItemCreated",
      boardId,
      enabled: false,
      version: 1,
      conditions: [{ type: "group", groupId: "group-0" }],
      actions: [
        { type: "ChangeStatus", optionId: "option-1" },
        { type: "AssignPerson", userId: "user-1" },
        { type: "NotifyMember", userId: "user-1", message: "Parked" },
      ],
    });

    await api.setEnabled(workspaceId, boardId, id, true);
    expect((await api.list(workspaceId, boardId))[0]?.enabled).toBe(true);
    await api.setEnabled(workspaceId, boardId, id, false);
    expect((await api.list(workspaceId, boardId))[0]?.enabled).toBe(false);
    expect(await api.runs(boardId, id)).toEqual([]);

    await api.update(workspaceId, boardId, id, {
      name: "Park it",
      trigger: "ItemMovedToGroup",
      triggerGroupId: "group-2",
      conditions: [],
      actions: [{ type: "MoveItem", groupId: "group-3" }],
    });
    expect((await api.list(workspaceId, boardId))[0]).toMatchObject({
      name: "Park it",
      version: 2,
      triggerGroupId: "group-2",
      conditions: [],
      actions: [{ type: "MoveItem", groupId: "group-3" }],
    });

    await api.remove(workspaceId, boardId, id);
    expect(await api.list(workspaceId, boardId)).toEqual([]);
  });

  it("refuses a rule with more than five actions or an action that cannot run", async () => {
    const api = setup();
    const base = {
      name: "Too much",
      trigger: "ItemCreated" as const,
      boardId,
      conditions: [],
    };
    await expect(
      api.create(workspaceId, {
        ...base,
        actions: Array.from({ length: 6 }, () => ({
          type: "AssignPerson" as const,
          userId: "user-1",
        })),
      }),
    ).rejects.toMatchObject({ kind: "validation" });
    await expect(
      api.create(workspaceId, { ...base, actions: [] }),
    ).rejects.toMatchObject({ kind: "validation" });
  });

  it("closes the rules to a member who does not manage the workspace", async () => {
    const api = setup("viewer");
    await expect(api.list(workspaceId, boardId)).rejects.toMatchObject({
      kind: "forbidden",
    });
  });
});
