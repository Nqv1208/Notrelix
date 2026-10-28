import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createTemplateApi } from "../api/template.api";

function apiWith(methods: {
  get?: unknown;
  post?: unknown;
  patch?: unknown;
  delete?: unknown;
}) {
  return createTemplateApi({ api: methods } as unknown as NotrelixClient);
}

describe("createTemplateApi", () => {
  it("reads the workspace's templates with their status and the built-in key", async () => {
    const get = vi.fn(async () => [
      {
        id: "b-1",
        name: "Blank",
        description: "x",
        status: "Published",
        builtInKey: "blank",
      },
      {
        id: "t-1",
        name: "Sprint",
        description: null,
        status: "Draft",
        builtInKey: null,
      },
      { name: "no id" },
    ]);

    const templates = await apiWith({ get }).list("w-1");

    expect(get).toHaveBeenCalledWith("/workspaces/w-1/templates");
    expect(templates).toEqual([
      {
        id: "b-1",
        name: "Blank",
        description: "x",
        status: "published",
        builtInKey: "blank",
      },
      {
        id: "t-1",
        name: "Sprint",
        description: null,
        status: "draft",
        builtInKey: null,
      },
    ]);
  });

  it("saves a board as a template: the structure only unless items are asked for", async () => {
    const post = vi.fn(async () => "t-9");

    const id = await apiWith({ post }).saveBoard({
      boardId: "b-1",
      name: "ERP",
    });

    expect(id).toBe("t-9");
    expect(post).toHaveBeenCalledWith("/boards/b-1/templates", {
      name: "ERP",
      description: undefined,
      includeItems: false,
    });
  });

  it("manages a template by its own id, never by a board id", async () => {
    const patch = vi.fn(async () => undefined);
    const del = vi.fn(async () => undefined);
    const api = apiWith({ patch, delete: del });

    await api.publish("t-1");
    await api.archive("t-1");
    await api.remove("t-1");

    expect(patch).toHaveBeenNthCalledWith(1, "/templates/t-1/publish");
    expect(patch).toHaveBeenNthCalledWith(2, "/templates/t-1/archive");
    expect(del).toHaveBeenCalledWith("/templates/t-1");
  });

  it("creates a board from a template in a workspace and optionally a space", async () => {
    const post = vi.fn(async () => "b-new");
    const api = apiWith({ post });

    expect(
      await api.createBoard({
        templateId: "t-1",
        workspaceId: "w-1",
        name: "Copy",
        spaceId: "s-1",
      }),
    ).toBe("b-new");
    expect(post).toHaveBeenCalledWith("/templates/t-1/create-board", {
      workspaceId: "w-1",
      name: "Copy",
      spaceId: "s-1",
    });

    await api.createBoard({
      templateId: "t-1",
      workspaceId: "w-1",
      name: "Root",
    });
    expect(post).toHaveBeenLastCalledWith("/templates/t-1/create-board", {
      workspaceId: "w-1",
      name: "Root",
    });
  });
});
