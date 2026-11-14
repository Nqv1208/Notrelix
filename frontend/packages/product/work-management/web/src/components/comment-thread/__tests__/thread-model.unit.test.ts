import { describe, expect, it } from "vitest";
import type { CardUpdate } from "@notrelix/work-management-core";
import { splitThreads } from "../thread-model";

const comment = (
  id: string,
  createdAt: string,
  parentId?: string,
): CardUpdate => ({
  id,
  cardId: "card",
  author: {
    id: "u1",
    userId: "u1",
    name: "An",
    initials: "A",
    color: "#1666CC",
  },
  body: id,
  mentionUserIds: [],
  attachmentIds: [],
  createdAt,
  parentId,
});

describe("splitThreads", () => {
  it("puts replies under their comment, oldest first", () => {
    const { top, repliesOf, orphans } = splitThreads([
      comment("c1", "2026-10-01T09:00:00Z"),
      comment("r2", "2026-10-01T09:20:00Z", "c1"),
      comment("r1", "2026-10-01T09:10:00Z", "c1"),
    ]);
    expect(top.map((c) => c.id)).toEqual(["c1"]);
    expect(repliesOf("c1").map((c) => c.id)).toEqual(["r1", "r2"]);
    expect(orphans).toEqual([]);
  });

  it("keeps the replies of a deleted comment together under its id (COL-006)", () => {
    const { top, repliesOf, orphans } = splitThreads([
      comment("c2", "2026-10-01T08:00:00Z"),
      comment("r2", "2026-10-01T09:20:00Z", "gone"),
      comment("r1", "2026-10-01T09:10:00Z", "gone"),
    ]);
    expect(top.map((c) => c.id)).toEqual(["c2"]);
    expect(orphans).toHaveLength(1);
    expect(orphans[0]!.parentId).toBe("gone");
    expect(orphans[0]!.replies.map((c) => c.id)).toEqual(["r1", "r2"]);
    expect(repliesOf("gone").map((c) => c.id)).toEqual(["r1", "r2"]);
  });
});
