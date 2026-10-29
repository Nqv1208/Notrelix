import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createItemExtrasApi } from "../api/item-extras.api";

function build(api: Record<string, unknown>) {
  return createItemExtrasApi({ api } as unknown as NotrelixClient);
}

const linkOf = (id: string, relation: string, itemId: string) => ({
  id,
  relation,
  item: { id: itemId, key: null, title: "t" },
});

describe("timeline dependencies", () => {
  it("creates `from -> to` as `to` blocked by `from`", async () => {
    const post = vi.fn(async () => undefined);
    await build({ post }).addDependency("a", "b");
    expect(post).toHaveBeenCalledWith(
      "/board-items/b/links",
      { itemId: "a", relation: "blocked-by" },
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
  });

  it("removes the matching blocked-by link of the waiting item", async () => {
    const get = vi.fn(async () => ({
      links: [linkOf("l-1", "related", "a"), linkOf("l-2", "blocked-by", "a")],
    }));
    const del = vi.fn(async () => undefined);
    const removed = await build({ get, delete: del }).removeDependency(
      "a",
      "b",
    );
    expect(removed).toBe(true);
    expect(del).toHaveBeenCalledWith(
      "/board-items/b/links/l-2",
      expect.anything(),
    );
  });

  it("answers false and deletes nothing when the link is already gone", async () => {
    const get = vi.fn(async () => ({ links: [] }));
    const del = vi.fn();
    expect(await build({ get, delete: del }).removeDependency("a", "b")).toBe(
      false,
    );
    expect(del).not.toHaveBeenCalled();
  });
});
