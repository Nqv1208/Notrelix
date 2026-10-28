import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createTemplateApi } from "../../../../product/work-management/state/src/index";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, templates: createTemplateApi(client) };
}

const WORKSPACE = mockIds.workspaces.primary;
const BOARD = mockIds.boards.erp;

describe("WorkManagement.Templates on the mock backend", () => {
  it("offers the built-in starters first, published, with a key", async () => {
    const { templates } = setup();

    const listed = await templates.list(WORKSPACE);

    expect(listed.filter((entry) => entry.builtInKey !== null)).toHaveLength(3);
    expect(listed.slice(0, 3).map((entry) => entry.builtInKey)).toEqual([
      "blank",
      "project",
      "content",
    ]);
    expect(
      listed.slice(0, 3).every((entry) => entry.status === "published"),
    ).toBe(true);
  });

  it("saves a board as a draft that creates no board until it is published", async () => {
    const { templates } = setup();

    const id = await templates.saveBoard({
      boardId: BOARD,
      name: "ERP rollout",
      description: "Per branch",
    });
    const saved = (await templates.list(WORKSPACE)).find(
      (entry) => entry.id === id,
    );
    expect(saved).toMatchObject({
      name: "ERP rollout",
      description: "Per branch",
      status: "draft",
      builtInKey: null,
    });

    await expect(
      templates.createBoard({
        templateId: id,
        workspaceId: WORKSPACE,
        name: "Too early",
      }),
    ).rejects.toThrow();

    await templates.publish(id);
    const boardId = await templates.createBoard({
      templateId: id,
      workspaceId: WORKSPACE,
      name: "Branch Hanoi",
    });
    expect(boardId).toBeTruthy();
  });

  it("creates a board from a built-in starter and refuses an archived template, then deletes it", async () => {
    const { templates } = setup();
    const starter = (await templates.list(WORKSPACE))[0]!;
    expect(
      await templates.createBoard({
        templateId: starter.id,
        workspaceId: WORKSPACE,
        name: "From starter",
      }),
    ).toBeTruthy();

    const id = await templates.saveBoard({ boardId: BOARD, name: "Old" });
    await templates.publish(id);
    await templates.archive(id);
    await expect(
      templates.createBoard({
        templateId: id,
        workspaceId: WORKSPACE,
        name: "Archived",
      }),
    ).rejects.toThrow();
    await expect(templates.publish(id)).rejects.toThrow();

    await templates.remove(id);
    expect(
      (await templates.list(WORKSPACE)).some((entry) => entry.id === id),
    ).toBe(false);
  });
});
