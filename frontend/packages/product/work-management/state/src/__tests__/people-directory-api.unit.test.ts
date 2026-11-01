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
      get.mock.calls.filter(([url]) => String(url).includes("/members")),
    ).toHaveLength(2);
  });
});
