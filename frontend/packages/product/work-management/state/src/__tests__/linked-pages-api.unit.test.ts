import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createItemExtrasApi, toLinkedPages } from "../api/item-extras.api";

describe("The linked pages of an item (UC-WM-R05)", () => {
  it("reads the typed operation and names only the pages the server named", async () => {
    const get = vi.fn(async () => ({
      pages: [
        {
          pageId: "p-1",
          title: "Launch plan",
          linkedAt: "2026-11-01T08:00:00Z",
        },
      ],
      hiddenCount: 2,
    }));
    const linked = await createItemExtrasApi({
      api: { get },
    } as unknown as NotrelixClient).getLinkedPages("item-1");

    expect(get).toHaveBeenCalledWith(
      "/board-items/item-1/linked-pages",
      expect.anything(),
    );
    expect(linked).toEqual({
      pages: [
        {
          pageId: "p-1",
          title: "Launch plan",
          linkedAt: "2026-11-01T08:00:00Z",
        },
      ],
      hiddenCount: 2,
    });
  });

  it("gives a page the caller cannot read no title: it is only counted", () => {
    const linked = toLinkedPages({
      pages: [{ title: "Salaries 2027" }],
      hiddenCount: 1,
    });

    expect(linked.pages).toEqual([]);
    expect(linked.hiddenCount).toBe(1);
    expect(JSON.stringify(linked)).not.toContain("Salaries");
  });

  it("reads an empty or odd answer as nothing linked", () => {
    expect(toLinkedPages(null)).toEqual({ pages: [], hiddenCount: 0 });
    expect(toLinkedPages({ pages: null, hiddenCount: -3 })).toEqual({
      pages: [],
      hiddenCount: 0,
    });
  });
});
