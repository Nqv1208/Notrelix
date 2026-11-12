import { describe, expect, it } from "vitest";
import {
  activeFilterCount,
  canonicalFilters,
  cleanSearchText,
  dayEndExclusiveIso,
  dayStartIso,
  hitIsConversation,
  hitTarget,
  nextHitIndex,
  pushRecentSearch,
  rangeIsValid,
  searchTextState,
  segmentSnippet,
} from "../core/model/search-model";
import type { ChatSearchHit } from "../core/types/chat";

const snippet = (text: string, ranges: [number, number][]) => ({
  text,
  ranges: ranges.map(([start, length]) => ({ start, length })),
  cutBefore: false,
  cutAfter: false,
});

describe("segmentSnippet", () => {
  it("cuts the text into plain and matched segments", () => {
    expect(
      segmentSnippet(
        snippet("a hoa don b hoa don", [
          [2, 7],
          [12, 7],
        ]),
      ),
    ).toEqual([
      { text: "a ", match: false },
      { text: "hoa don", match: true },
      { text: " b ", match: false },
      { text: "hoa don", match: true },
    ]);
  });

  it("keeps markup-looking text as text", () => {
    expect(segmentSnippet(snippet("<b>x</b>", [[3, 1]]))).toEqual([
      { text: "<b>", match: false },
      { text: "x", match: true },
      { text: "</b>", match: false },
    ]);
  });

  it("drops a range that is outside, empty, fractional, negative or overlapping", () => {
    const bad = snippet("abcdef", [
      [4, 5],
      [0, 0],
      [-1, 2],
      [1.5, 1],
      [0, 3],
      [2, 2],
    ]);
    expect(segmentSnippet(bad)).toEqual([
      { text: "abc", match: true },
      { text: "def", match: false },
    ]);
  });

  it("returns the whole text when there is no range", () => {
    expect(segmentSnippet(snippet("hello", []))).toEqual([
      { text: "hello", match: false },
    ]);
    expect(segmentSnippet(snippet("", []))).toEqual([]);
  });
});

describe("searchTextState", () => {
  it("is empty, too short, too long or ready", () => {
    expect(searchTextState("   ").kind).toBe("empty");
    expect(searchTextState(" a ").kind).toBe("too-short");
    expect(searchTextState("a".repeat(101)).kind).toBe("too-long");
    expect(searchTextState("  hoa   don ")).toEqual({
      kind: "ready",
      q: "hoa don",
    });
  });

  it("collapses white space like the server", () => {
    expect(cleanSearchText("  a \t b\n c ")).toBe("a b c");
  });
});

describe("filters", () => {
  it("leaves empty values out so equal searches share a key", () => {
    expect(
      canonicalFilters({
        q: " hoa  don ",
        spaceId: "",
        hasAttachment: false,
        authorId: "u",
      }),
    ).toEqual({ q: "hoa don", authorId: "u" });
  });

  it("counts the filters that are on", () => {
    expect(activeFilterCount({ q: "x" })).toBe(0);
    expect(
      activeFilterCount({
        q: "x",
        spaceId: "s",
        hasAttachment: true,
        from: "d",
      }),
    ).toBe(3);
  });
});

describe("days", () => {
  it("turns a calendar day into the start of the day and the start of the next", () => {
    const start = dayStartIso("2026-03-31")!;
    const end = dayEndExclusiveIso("2026-03-31")!;
    expect(new Date(start).getTime()).toBe(new Date(2026, 2, 31).getTime());
    expect(new Date(end).getTime()).toBe(new Date(2026, 3, 1).getTime());
  });

  it("refuses what is not a date", () => {
    expect(dayStartIso("")).toBeNull();
    expect(dayStartIso("2026-02-30")).toBeNull();
    expect(dayStartIso("31/03/2026")).toBeNull();
    expect(dayEndExclusiveIso("nope")).toBeNull();
  });

  it("a range that ends before it starts is not valid", () => {
    expect(rangeIsValid("2026-02-10", "2026-02-01")).toBe(false);
    expect(rangeIsValid("2026-02-01", "2026-02-01")).toBe(true);
    expect(rangeIsValid("", "2026-02-01")).toBe(true);
  });
});

describe("nextHitIndex", () => {
  it("moves and wraps, and has nothing for an empty list", () => {
    expect(nextHitIndex(-1, 3, "ArrowDown")).toBe(0);
    expect(nextHitIndex(2, 3, "ArrowDown")).toBe(0);
    expect(nextHitIndex(-1, 3, "ArrowUp")).toBe(2);
    expect(nextHitIndex(0, 3, "ArrowUp")).toBe(2);
    expect(nextHitIndex(1, 3, "Home")).toBe(0);
    expect(nextHitIndex(1, 3, "End")).toBe(2);
    expect(nextHitIndex(0, 0, "ArrowDown")).toBe(-1);
  });
});

describe("pushRecentSearch", () => {
  it("keeps the newest first, once each, at most five, and ignores a text that is too short", () => {
    let recent: readonly string[] = [];
    for (const q of ["aa", "bb", "cc", "dd", "ee", "ff", "bb"])
      recent = pushRecentSearch(recent, q);
    expect(recent).toEqual(["bb", "ff", "ee", "dd", "cc"]);
    expect(pushRecentSearch(recent, "x")).toBe(recent);
  });
});

describe("hits", () => {
  const hit = (over: Partial<ChatSearchHit>): ChatSearchHit => ({
    messageId: "m",
    spaceId: "s",
    spaceName: "n",
    spaceKind: "channel_public",
    spaceArchived: false,
    authorId: "u",
    createdAt: "2026-01-01T00:00:00Z",
    sequence: 1,
    threadRootId: null,
    alsoSentToChannel: false,
    snippet: snippet("x", []),
    ...over,
  });

  it("a reply that was not sent to the channel lives in its thread", () => {
    expect(hitTarget(hit({}))).toBe("channel");
    expect(hitTarget(hit({ threadRootId: "r" }))).toBe("thread");
    expect(hitTarget(hit({ threadRootId: "r", alsoSentToChannel: true }))).toBe(
      "channel",
    );
  });

  it("knows a conversation", () => {
    expect(hitIsConversation(hit({ spaceKind: "direct" }))).toBe(true);
    expect(hitIsConversation(hit({ spaceKind: "group" }))).toBe(true);
    expect(hitIsConversation(hit({}))).toBe(false);
  });
});
