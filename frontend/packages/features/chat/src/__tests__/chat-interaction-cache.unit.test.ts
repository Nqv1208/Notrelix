import { describe, expect, it } from "vitest";
import type { ChatMessage, ChatReactionSummary } from "../core/types/chat";
import {
  appendThreadMessage,
  applyReactionChange,
  bumpThreadCount,
  containsMessage,
  patchMessageInData,
  patchThreadSummary,
  toggleReaction,
} from "../web/query/interaction-cache";

function message(
  id: string,
  overrides: Partial<ChatMessage> = {},
): ChatMessage {
  return {
    id,
    spaceId: "space-1",
    sequence: Number(id.replace(/\D/g, "")) || 1,
    kind: "user",
    authorId: "user-a",
    content: `text ${id}`,
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: "2026-11-10T09:00:00.000Z",
    editedAt: null,
    deletedAt: null,
    version: 1,
    ...overrides,
  };
}

const THUMB = "👍";
const HEART = "❤️";

describe("toggleReaction (optimistic)", () => {
  it("adds a new emoji with the caller's reaction, and raises an existing one", () => {
    const none: ChatReactionSummary[] = [];
    const one = toggleReaction(none, THUMB, true);
    expect(one).toEqual([{ emoji: THUMB, count: 1, reactedByMe: true }]);
    const other = [{ emoji: HEART, count: 2, reactedByMe: false }];
    expect(toggleReaction(other, THUMB, true)).toEqual([
      { emoji: HEART, count: 2, reactedByMe: false },
      { emoji: THUMB, count: 1, reactedByMe: true },
    ]);
    const existing = [{ emoji: THUMB, count: 3, reactedByMe: false }];
    expect(toggleReaction(existing, THUMB, true)).toEqual([
      { emoji: THUMB, count: 4, reactedByMe: true },
    ]);
  });

  it("removes the caller's reaction and drops an emoji nobody else uses", () => {
    const mine = [{ emoji: THUMB, count: 1, reactedByMe: true }];
    expect(toggleReaction(mine, THUMB, false)).toEqual([]);
    const shared = [{ emoji: THUMB, count: 3, reactedByMe: true }];
    expect(toggleReaction(shared, THUMB, false)).toEqual([
      { emoji: THUMB, count: 2, reactedByMe: false },
    ]);
  });

  it("returns the same array when the change is a no-op", () => {
    const mine = [{ emoji: THUMB, count: 1, reactedByMe: true }];
    expect(toggleReaction(mine, THUMB, true)).toBe(mine);
    expect(toggleReaction([], THUMB, false)).toEqual([]);
  });
});

describe("applyReactionChange (realtime)", () => {
  it("counts another person's reaction by one", () => {
    const start = [{ emoji: THUMB, count: 1, reactedByMe: true }];
    expect(
      applyReactionChange(start, {
        kind: THUMB,
        change: "added",
        actorIsViewer: false,
      }),
    ).toEqual([{ emoji: THUMB, count: 2, reactedByMe: true }]);
    expect(
      applyReactionChange(start, {
        kind: HEART,
        change: "added",
        actorIsViewer: false,
      }),
    ).toEqual([
      { emoji: THUMB, count: 1, reactedByMe: true },
      { emoji: HEART, count: 1, reactedByMe: false },
    ]);
  });

  it("does not count the caller's own reaction twice", () => {
    const start = [{ emoji: THUMB, count: 1, reactedByMe: true }];
    expect(
      applyReactionChange(start, {
        kind: THUMB,
        change: "added",
        actorIsViewer: true,
      }),
    ).toBe(start);
  });

  it("lowers another person's count and drops the emoji when it empties", () => {
    const start = [{ emoji: HEART, count: 1, reactedByMe: false }];
    expect(
      applyReactionChange(start, {
        kind: HEART,
        change: "removed",
        actorIsViewer: false,
      }),
    ).toEqual([]);
  });
});

describe("patchMessageInData", () => {
  it("reaches the message in a channel page, a thread root and a wrapped pin", () => {
    const channel = {
      pageParams: [{}],
      pages: [
        {
          items: [message("m1"), message("m2")],
          hasOlder: false,
          hasNewer: false,
        },
      ],
    };
    const thread = {
      pageParams: [{}],
      pages: [
        {
          root: message("m1"),
          items: [message("r1")],
          hasNewer: false,
          thread: {},
        },
      ],
    };
    const pins = {
      pageParams: [null],
      pages: [
        {
          items: [{ message: message("m2"), pinnedBy: "u", pinnedAt: "t" }],
          nextCursor: null,
        },
      ],
    };
    const bump = (m: ChatMessage) => ({ ...m, version: 99 });
    const patchedChannel = patchMessageInData(channel, "m2", bump);
    expect(patchedChannel.pages[0]!.items[1]!.version).toBe(99);
    expect(channel.pages[0]!.items[1]!.version).toBe(1);
    expect(patchMessageInData(thread, "m1", bump).pages[0]!.root.version).toBe(
      99,
    );
    expect(
      patchMessageInData(pins, "m2", bump).pages[0]!.items[0]!.message.version,
    ).toBe(99);
  });

  it("returns the same object when the message is not in the cache", () => {
    const channel = {
      pageParams: [{}],
      pages: [{ items: [message("m1")], hasOlder: false, hasNewer: false }],
    };
    expect(patchMessageInData(channel, "missing", (m) => m)).toBe(channel);
    expect(containsMessage(channel, "m1")).toBe(true);
    expect(containsMessage(channel, "missing")).toBe(false);
  });
});

describe("thread reducers", () => {
  it("keeps the highest reply sequence and the latest reply time", () => {
    const counts = bumpThreadCount({}, "root", 2, "2026-11-10T09:00:00.000Z");
    expect(counts.root).toEqual({
      replyCount: 2,
      lastReplyAt: "2026-11-10T09:00:00.000Z",
    });
    expect(bumpThreadCount(counts, "root", 1, "2026-11-10T08:00:00.000Z")).toBe(
      counts,
    );
    const later = bumpThreadCount(
      counts,
      "root",
      3,
      "2026-11-10T10:00:00.000Z",
    );
    expect(later.root).toEqual({
      replyCount: 3,
      lastReplyAt: "2026-11-10T10:00:00.000Z",
    });
  });

  it("appends a reply once to the newest page of a live thread, in sequence order", () => {
    const data = {
      pageParams: [{}],
      pages: [
        {
          root: message("root"),
          items: [message("r1", { threadSequence: 1 })],
          hasNewer: false,
          thread: {},
        },
      ],
    };
    const reply = message("r2", { threadRootId: "root", threadSequence: 2 });
    const once = appendThreadMessage(data, reply);
    expect(once.pages[0]!.items.map((m) => m.id)).toEqual(["r1", "r2"]);
    expect(appendThreadMessage(once, reply)).toBe(once);
  });

  it("leaves a thread window that is not at its end alone", () => {
    const data = {
      pageParams: [{}],
      pages: [{ root: message("root"), items: [], hasNewer: true, thread: {} }],
    };
    expect(
      appendThreadMessage(data, message("r9", { threadSequence: 9 })),
    ).toBe(data);
  });

  it("patches the thread summary of every page", () => {
    const data = {
      pageParams: [{}, {}],
      pages: [
        {
          root: message("root"),
          items: [],
          hasNewer: true,
          thread: { following: false },
        },
        { items: [], hasNewer: false, thread: { following: false } },
      ],
    };
    const next = patchThreadSummary(
      data,
      (summary: { following: boolean }) => ({
        ...summary,
        following: true,
      }),
    );
    expect(next.pages.map((page) => page.thread.following)).toEqual([
      true,
      true,
    ]);
  });
});
