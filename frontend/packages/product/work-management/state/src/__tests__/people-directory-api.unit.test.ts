import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createItemExtrasApi } from "../api/item-extras.api";

const member = (userId: string, name: string, status = "Active") => ({
  userId,
  name,
  status,
});

function apiOver(pages: Record<string, unknown>) {
  const get = vi.fn(async (url: string) => {
    if (url.includes("/boards/"))
      return { members: [{ userId: "u1", role: "Editor" }] };
    return pages[url.includes("cursor=2") ? "second" : "first"];
  });
  return {
    get,
    directory: createItemExtrasApi({
      api: { get },
    } as unknown as NotrelixClient).getPeopleDirectory("b-1", "ws-1"),
  };
}

describe("The people directory of an item", () => {
  it("lists only the board's members to a Guest, who may not list the workspace (UC-WM-R03)", async () => {
    const get = vi.fn(async (url: string) => {
      if (url.includes("/boards/"))
        return {
          members: [
            { userId: "u1", name: "Ada", role: "Editor" },
            { userId: "u5", name: "Em", role: "Viewer" },
          ],
        };
      throw Object.assign(new Error("Forbidden"), {
        kind: "forbidden",
        status: 403,
      });
    });
    const { people } = await createItemExtrasApi({
      api: { get },
    } as unknown as NotrelixClient).getPeopleDirectory("b-1", "ws-1");

    expect(people.map((p) => [p.name, p.onBoard, p.level])).toEqual([
      ["Ada", true, "editor"],
      ["Em", true, "viewer"],
    ]);
  });

  it("still fails on a members read that broke for another reason", async () => {
    const get = vi.fn(async (url: string) => {
      if (url.includes("/boards/")) return { members: [] };
      throw Object.assign(new Error("Server"), { kind: "server", status: 500 });
    });
    await expect(
      createItemExtrasApi({
        api: { get },
      } as unknown as NotrelixClient).getPeopleDirectory("b-1", "ws-1"),
    ).rejects.toThrow("Server");
  });

  it("joins every page of the workspace members (the producer lists them as a cursor page)", async () => {
    const { get, directory } = apiOver({
      first: {
        items: [member("u1", "Ada"), member("u2", "Bao")],
        nextCursor: "2",
      },
      second: {
        items: [member("u3", "Chi"), member("u4", "Dao", "Suspended")],
        nextCursor: null,
      },
    });

    const { people } = await directory;

    expect(people.map((p) => p.name)).toEqual(["Ada", "Bao", "Chi"]);
    expect(people.find((p) => p.userId === "u1")?.onBoard).toBe(true);
    expect(people.find((p) => p.userId === "u2")?.onBoard).toBe(false);
    expect(
      get.mock.calls.filter(([url]) => String(url).includes("/members?")),
    ).toHaveLength(2);
  });

  it("reads the board's own people for a Guest, never the workspace members", async () => {
    const get = vi.fn(async (url: string) => {
      if (url.endsWith("/members/me")) return { userId: "me", role: "Guest" };
      if (url.endsWith("/people"))
        return [
          { userId: "u1", name: "Ada", level: "editor" },
          { userId: "u2", name: "Bao", level: "viewer" },
        ];
      throw new Error(`unexpected ${url}`);
    });
    const { people, canInvite } = await createItemExtrasApi({
      api: { get },
    } as unknown as NotrelixClient).getPeopleDirectory("b-1", "ws-1");

    expect(people).toEqual([
      {
        userId: "u1",
        name: "Ada",
        department: null,
        openCount: 0,
        onBoard: true,
        level: "editor",
      },
      {
        userId: "u2",
        name: "Bao",
        department: null,
        openCount: 0,
        onBoard: true,
        level: "viewer",
      },
    ]);
    expect(canInvite).toBe(false);
    expect(
      get.mock.calls.some(([url]) => String(url).includes("/members?")),
    ).toBe(false);
  });
});
