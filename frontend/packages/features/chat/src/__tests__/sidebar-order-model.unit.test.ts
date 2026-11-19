import { describe, expect, it } from "vitest";
import {
  POSITION_STEP,
  applyGroupMoves,
  applyPlacements,
  channelDropFor,
  edgeOf,
  groupDropFor,
  placeChannel,
  placeGroup,
  positionBetween,
  sidebarSections,
  stepChannel,
  stepGroup,
} from "../core/model/sidebar-order-model";
import type { ChatChannel, ChatChannelGroup } from "../core/types/chat";

function ch(
  id: string,
  groupId: string | null,
  position: number,
  version = 1,
): ChatChannel {
  return { id, name: id, groupId, position, version } as ChatChannel;
}

function grp(id: string, position: number, version = 1): ChatChannelGroup {
  return { id, name: id.toUpperCase(), position, version };
}

describe("positionBetween", () => {
  it("is the midpoint of two neighbours, a step past one, and 0 for none", () => {
    expect(positionBetween(1, 2)).toBe(1.5);
    expect(positionBetween(1, null)).toBe(1 + POSITION_STEP);
    expect(positionBetween(null, 5)).toBe(5 - POSITION_STEP);
    expect(positionBetween(null, null)).toBe(0);
  });
});

describe("placeChannel", () => {
  const channels = [
    ch("a", null, 1),
    ch("b", null, 2),
    ch("c", "g1", 1),
    ch("d", "g1", 2),
  ];

  it("takes the midpoint between the two neighbours inside a category", () => {
    expect(
      placeChannel(channels, "b", { groupId: null, beforeId: "a" }),
    ).toEqual([
      {
        channelId: "b",
        groupId: null,
        position: 1 - POSITION_STEP,
        expectedVersion: 1,
      },
    ]);
    expect(
      placeChannel(channels, "a", { groupId: "g1", beforeId: "d" }),
    ).toEqual([
      { channelId: "a", groupId: "g1", position: 1.5, expectedVersion: 1 },
    ]);
  });

  it("goes past the last row at the end and takes the first place before the first", () => {
    expect(
      placeChannel(channels, "a", { groupId: "g1", beforeId: null }),
    ).toEqual([
      {
        channelId: "a",
        groupId: "g1",
        position: 2 + POSITION_STEP,
        expectedVersion: 1,
      },
    ]);
  });

  it("moves into a category with no channels at position 0", () => {
    expect(
      placeChannel(channels, "a", { groupId: "empty", beforeId: null }),
    ).toEqual([
      { channelId: "a", groupId: "empty", position: 0, expectedVersion: 1 },
    ]);
  });

  it("sends the version the caller read", () => {
    const placed = placeChannel([ch("a", null, 1, 7)], "a", {
      groupId: "g",
      beforeId: null,
    });
    expect(placed?.[0]?.expectedVersion).toBe(7);
  });

  it("is a no-op where the channel already is", () => {
    expect(
      placeChannel(channels, "a", { groupId: null, beforeId: "b" }),
    ).toBeNull();
    expect(
      placeChannel(channels, "b", { groupId: null, beforeId: null }),
    ).toBeNull();
    expect(
      placeChannel(channels, "a", { groupId: null, beforeId: "a" }),
    ).toBeNull();
    expect(
      placeChannel(channels, "nope", { groupId: null, beforeId: null }),
    ).toBeNull();
  });

  it("renumbers the whole run when neighbours share a position, so the order is explicit", () => {
    const tied = [ch("a", "g", 0), ch("b", "g", 0), ch("c", "g", 0)];
    const placed = placeChannel(tied, "c", { groupId: "g", beforeId: "b" });
    expect(placed).toEqual([
      {
        channelId: "a",
        groupId: "g",
        position: POSITION_STEP,
        expectedVersion: 1,
      },
      {
        channelId: "c",
        groupId: "g",
        position: 2 * POSITION_STEP,
        expectedVersion: 1,
      },
      {
        channelId: "b",
        groupId: "g",
        position: 3 * POSITION_STEP,
        expectedVersion: 1,
      },
    ]);
  });

  it("renumbers when the doubles ran out between two neighbours", () => {
    const close = [
      ch("a", "g", 1),
      ch("b", "g", 1 + Number.EPSILON),
      ch("c", "g", 9),
    ];
    const placed = placeChannel(close, "c", { groupId: "g", beforeId: "b" });
    expect(placed?.map((p) => p.channelId)).toEqual(["a", "c", "b"]);
  });
});

describe("stepChannel", () => {
  const channels = [
    ch("a", null, 1),
    ch("b", null, 2),
    ch("c", "g1", 1),
    ch("d", "g1", 2),
    ch("e", "g2", 1),
  ];
  const groups = ["g1", "g2"];

  it("steps inside a category", () => {
    expect(stepChannel(channels, groups, "b", -1)).toEqual({
      groupId: null,
      beforeId: "a",
    });
    expect(stepChannel(channels, groups, "c", 1)).toEqual({
      groupId: "g1",
      beforeId: null,
    });
    expect(stepChannel(channels, groups, "a", 1)).toEqual({
      groupId: null,
      beforeId: null,
    });
  });

  it("crosses into the neighbouring category past either end", () => {
    // Up from the first of g1: the end of the ungrouped run.
    expect(stepChannel(channels, groups, "c", -1)).toEqual({
      groupId: null,
      beforeId: null,
    });
    // Down from the last of g1: the start of g2.
    expect(stepChannel(channels, groups, "d", 1)).toEqual({
      groupId: "g2",
      beforeId: "e",
    });
  });

  it("stops at the first and last places", () => {
    expect(stepChannel(channels, groups, "a", -1)).toBeNull();
    expect(stepChannel(channels, groups, "e", 1)).toBeNull();
  });
});

describe("applyPlacements", () => {
  it("moves the channel, bumps its version and keeps the list in position order", () => {
    const next = applyPlacements(
      [ch("a", null, 1), ch("b", null, 2)],
      [{ channelId: "a", groupId: "g", position: 5, expectedVersion: 1 }],
    );
    expect(next.map((c) => c.id)).toEqual(["b", "a"]);
    expect(next[1]).toMatchObject({ groupId: "g", position: 5, version: 2 });
  });
});

describe("categories", () => {
  const groups = [grp("g1", 1), grp("g2", 2), grp("g3", 3)];

  it("places a category between two others", () => {
    expect(placeGroup(groups, "g3", "g2")).toEqual([
      { group: groups[2], position: 1.5 },
    ]);
  });

  it("puts a category last, and ignores a drop where it is", () => {
    expect(placeGroup(groups, "g1", null)).toEqual([
      { group: groups[0], position: 3 + POSITION_STEP },
    ]);
    expect(placeGroup(groups, "g2", "g3")).toBeNull();
    expect(placeGroup(groups, "g2", "g2")).toBeNull();
  });

  it("renumbers every category when positions tie", () => {
    const tied = [grp("a", 0), grp("b", 0), grp("c", 0)];
    expect(placeGroup(tied, "c", "b")).toEqual([
      { group: tied[0], position: POSITION_STEP },
      { group: tied[2], position: 2 * POSITION_STEP },
      { group: tied[1], position: 3 * POSITION_STEP },
    ]);
  });

  it("steps one place and stops at the ends", () => {
    expect(stepGroup(groups, "g2", -1)).toEqual({ beforeId: "g1" });
    expect(stepGroup(groups, "g2", 1)).toEqual({ beforeId: null });
    expect(stepGroup(groups, "g1", -1)).toBeUndefined();
    expect(stepGroup(groups, "g3", 1)).toBeUndefined();
  });

  it("applies moves in position order with the version bumped", () => {
    const next = applyGroupMoves(groups, [{ group: groups[2]!, position: 0 }]);
    expect(next.map((g) => g.id)).toEqual(["g3", "g1", "g2"]);
    expect(next[0]?.version).toBe(2);
  });
});

describe("sidebarSections", () => {
  it("lists every category, empty ones included, after the ungrouped run", () => {
    const sections = sidebarSections(
      [ch("a", null, 1), ch("c", "g1", 1)],
      [grp("g1", 1), grp("g2", 2)],
    );
    expect(sections.map((s) => [s.groupId, s.channels.length])).toEqual([
      [null, 1],
      ["g1", 1],
      ["g2", 0],
    ]);
    expect(sections[2]).toMatchObject({ name: "G2", version: 1 });
  });

  it("leaves out an empty ungrouped run and keeps a group the list does not name", () => {
    const sections = sidebarSections([ch("c", "gx", 1)], []);
    expect(sections).toEqual([
      {
        groupId: "gx",
        name: null,
        version: null,
        channels: [ch("c", "gx", 1)],
      },
    ]);
  });

  it("falls back to the groups the channels name while the list is unread", () => {
    const sections = sidebarSections([ch("c", "g1", 1)], undefined);
    expect(sections.map((s) => s.groupId)).toEqual(["g1"]);
  });
});

describe("what a drag resolves to", () => {
  const channels = [ch("a", "g", 1), ch("b", "g", 2), ch("c", "g", 3)];

  it("reads the half of the target the dragged item's centre is in", () => {
    const rect = { top: 100, height: 32 };
    expect(edgeOf(110, rect)).toBe("before");
    expect(edgeOf(116, rect)).toBe("after");
    expect(edgeOf(null, rect)).toBe("before");
  });

  it("drops before a row, after a row, or at the end of a category", () => {
    const over = { type: "channel", channelId: "b", groupId: "g" } as const;
    expect(channelDropFor(over, "before", channels, "c")).toEqual({
      groupId: "g",
      beforeId: "b",
    });
    expect(channelDropFor(over, "after", channels, "a")).toEqual({
      groupId: "g",
      beforeId: "c",
    });
    // After the row that is followed by the dragged one: the dragged one is skipped.
    expect(channelDropFor(over, "after", channels, "c")).toEqual({
      groupId: "g",
      beforeId: null,
    });
    expect(
      channelDropFor(
        { type: "section", groupId: "x" },
        "before",
        channels,
        "a",
      ),
    ).toEqual({ groupId: "x", beforeId: null });
  });

  it("drops a category before or after another", () => {
    const groups = [grp("g1", 1), grp("g2", 2), grp("g3", 3)];
    expect(groupDropFor("g2", "before", groups, "g3")).toBe("g2");
    expect(groupDropFor("g2", "after", groups, "g1")).toBe("g3");
    expect(groupDropFor("g3", "after", groups, "g1")).toBeNull();
  });
});
