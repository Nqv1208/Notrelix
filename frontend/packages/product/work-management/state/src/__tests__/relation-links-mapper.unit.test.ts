import { describe, expect, it } from "vitest";
import { relationLinksOf } from "../api/board-api-mappers";

describe("relation links of an item read", () => {
  it("keeps the key and title of a link the reader can see", () => {
    const link = {
      itemId: "i1",
      itemKey: "CT-21",
      title: "Banner",
      locked: false,
    };
    expect(relationLinksOf({ relationLinks: { f1: [link] } })).toEqual({
      relationLinks: { f1: [link] },
    });
  });

  it("drops the key and title of a locked link, whatever the wire carries", () => {
    const { relationLinks } = relationLinksOf({
      relationLinks: {
        f1: [{ itemId: "i2", itemKey: "CT-99", title: "Secret", locked: true }],
      },
    });
    expect(relationLinks?.f1).toEqual([
      { itemId: "i2", itemKey: null, title: null, locked: true },
    ]);
  });

  it("leaves the key out of a read that carries no links", () => {
    expect(relationLinksOf({})).toEqual({});
    expect(relationLinksOf({ relationLinks: null })).toEqual({});
  });
});
