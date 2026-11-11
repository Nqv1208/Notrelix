import { describe, expect, it } from "vitest";
import {
  applyMessageCreated,
  applyMessageDeleted,
  applyMessageUpdated,
  applyUnreadFrame,
  lastKnownSequence,
  mergeHistoryPage,
  windowIsLive,
} from "../web/query/chat-realtime-reducers";
import type { ChatMessagesCache } from "../web/query/message-cache";
import type {
  ChatMessage,
  ChatMessagePage,
  ChatUnreadSummary,
} from "../core/types/chat";
import {
  parseChatMessageFrame,
  parseChatUnreadFrame,
  type ChatMessageFramePayload,
} from "../core/realtime/chat-frames";

const SPACE = "space-1";
const AT = "2026-11-10T04:00:00.000Z";

function message(
  sequence: number,
  overrides: Partial<ChatMessage> = {},
): ChatMessage {
  return {
    id: `m-${sequence}`,
    spaceId: SPACE,
    sequence,
    kind: "user",
    authorId: "user-a",
    content: `text ${sequence}`,
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: "2026-11-10T03:00:00.000Z",
    editedAt: null,
    deletedAt: null,
    version: 1,
    ...overrides,
  };
}

/** A cache of pages; the last page is live unless `hasNewer` says the window is behind. */
function cacheOf(pages: ChatMessage[][], hasNewer = false): ChatMessagesCache {
  return {
    pageParams: pages.map(() => ({})),
    pages: pages.map((items, index) => ({
      items,
      hasOlder: index === 0,
      hasNewer: index === pages.length - 1 ? hasNewer : false,
    })),
  };
}

function ids(cache: ChatMessagesCache): number[] {
  return cache.pages.flatMap((page) => page.items.map((item) => item.sequence));
}

function frame(
  overrides: Partial<ChatMessageFramePayload> & {
    id: string;
    sequence: number;
  },
): ChatMessageFramePayload {
  return {
    authorId: "user-b",
    kind: "user",
    content: `incoming ${overrides.sequence}`,
    contentOmitted: false,
    deleted: false,
    createdAt: AT,
    editedAt: null,
    replyToId: null,
    version: 1,
    clientMessageId: null,
    ...overrides,
  };
}

describe("applyMessageCreated", () => {
  it("appends the next sequence to the newest page, in order", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const out = applyMessageCreated(
      cache,
      frame({ id: "m-3", sequence: 3 }),
      SPACE,
      AT,
    );
    expect(out.kind).toBe("applied");
    if (out.kind !== "applied") return;
    expect(ids(out.cache)).toEqual([1, 2, 3]);
    expect(out.cache.pages[0]!.items[2]).toMatchObject({
      id: "m-3",
      spaceId: SPACE,
      content: "incoming 3",
      version: 1,
      edited: false,
      deleted: false,
    });
  });

  it("dedupes a frame of a message already loaded (same id, same version)", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const out = applyMessageCreated(
      cache,
      frame({ id: "m-2", sequence: 2 }),
      SPACE,
      AT,
    );
    expect(out).toEqual({ kind: "unchanged", cache, reason: "duplicate" });
  });

  it("reconciles an own optimistic send by clientMessageId without showing it twice", () => {
    const own = message(4, {
      id: "m-4",
      authorId: "me",
      clientMessageId: "cmid-1",
    });
    const cache = cacheOf([[message(1), own]]);
    const out = applyMessageCreated(
      cache,
      frame({
        id: "m-4b",
        sequence: 5,
        authorId: "me",
        clientMessageId: "cmid-1",
      }),
      SPACE,
      AT,
    );
    expect(out).toEqual({ kind: "unchanged", cache, reason: "duplicate" });
  });

  it("detects a gap: a sequence past last+1 is not applied and asks for the range after the last known", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const out = applyMessageCreated(
      cache,
      frame({ id: "m-5", sequence: 5 }),
      SPACE,
      AT,
    );
    expect(out).toEqual({ kind: "gap", cache, after: 2 });
  });

  it("inserts an out-of-order message in sequence order when its place is loaded", () => {
    const cache = cacheOf([[message(1), message(3)]]);
    const out = applyMessageCreated(
      cache,
      frame({ id: "m-2", sequence: 2 }),
      SPACE,
      AT,
    );
    expect(out.kind).toBe("applied");
    if (out.kind === "applied") expect(ids(out.cache)).toEqual([1, 2, 3]);
  });

  it("does not join a window that is behind the newest message (the paging brings it)", () => {
    const cache = cacheOf([[message(1), message(2)]], true);
    const out = applyMessageCreated(
      cache,
      frame({ id: "m-3", sequence: 3 }),
      SPACE,
      AT,
    );
    expect(out).toEqual({ kind: "unchanged", cache, reason: "outside-window" });
    expect(windowIsLive(cache)).toBe(false);
  });

  it("ignores everything while nothing is loaded yet", () => {
    const cache = cacheOf([]);
    expect(
      applyMessageCreated(cache, frame({ id: "m-1", sequence: 1 }), SPACE, AT),
    ).toMatchObject({
      kind: "unchanged",
      reason: "outside-window",
    });
  });
});

describe("applyMessageUpdated", () => {
  it("replaces the content of the loaded message with a newer version", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const out = applyMessageUpdated(
      cache,
      frame({
        id: "m-2",
        sequence: 2,
        content: "edited",
        editedAt: AT,
        version: 2,
      }),
    );
    expect(out.kind).toBe("applied");
    if (out.kind !== "applied") return;
    expect(out.cache.pages[0]!.items[1]).toMatchObject({
      content: "edited",
      edited: true,
      editedAt: AT,
      version: 2,
    });
  });

  it("ignores a stale version and a repeated one", () => {
    const cache = cacheOf([[message(1, { version: 3 })]]);
    const stale = applyMessageUpdated(
      cache,
      frame({ id: "m-1", sequence: 1, content: "old", version: 2 }),
    );
    expect(stale).toMatchObject({ kind: "unchanged", reason: "stale" });
    const same = applyMessageUpdated(
      cache,
      frame({ id: "m-1", sequence: 1, content: "same", version: 3 }),
    );
    expect(same).toMatchObject({ kind: "unchanged", reason: "duplicate" });
  });

  it("ignores an edit of a message outside the loaded window", () => {
    const cache = cacheOf([[message(1)]]);
    const out = applyMessageUpdated(
      cache,
      frame({ id: "m-99", sequence: 99, version: 2 }),
    );
    expect(out).toMatchObject({ kind: "unchanged", reason: "outside-window" });
  });

  it("asks for the range when the text did not fit the frame (payloadOmitted content)", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const out = applyMessageUpdated(
      cache,
      frame({
        id: "m-2",
        sequence: 2,
        content: null,
        contentOmitted: true,
        version: 2,
      }),
    );
    expect(out).toEqual({ kind: "omitted", cache, after: 1 });
  });
});

describe("applyMessageDeleted", () => {
  it("turns the message into a tombstone keeping id, sequence and author, with the frame version", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const out = applyMessageDeleted(
      cache,
      frame({
        id: "m-2",
        sequence: 2,
        deleted: true,
        content: null,
        version: 2,
      }),
      AT,
    );
    expect(out.kind).toBe("applied");
    if (out.kind !== "applied") return;
    expect(out.cache.pages[0]!.items[1]).toMatchObject({
      id: "m-2",
      sequence: 2,
      authorId: "user-a",
      content: null,
      deleted: true,
      deletedAt: AT,
      version: 2,
    });
  });

  it("a delete with an older version does not bring the text back or replace a newer edit", () => {
    const cache = cacheOf([[message(1, { version: 4 })]]);
    const out = applyMessageDeleted(
      cache,
      frame({
        id: "m-1",
        sequence: 1,
        deleted: true,
        content: null,
        version: 3,
      }),
      AT,
    );
    expect(out).toMatchObject({ kind: "unchanged", reason: "stale" });
  });

  it("a repeated delete changes nothing", () => {
    const cache = cacheOf([
      [message(1, { deleted: true, content: null, deletedAt: AT, version: 2 })],
    ]);
    const out = applyMessageDeleted(
      cache,
      frame({
        id: "m-1",
        sequence: 1,
        deleted: true,
        content: null,
        version: 2,
      }),
      AT,
    );
    expect(out).toMatchObject({ kind: "unchanged", reason: "duplicate" });
  });
});

describe("mergeHistoryPage (gap and reconnect backfill)", () => {
  it("merges the missing range after the last known sequence and keeps order", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const page: ChatMessagePage = {
      items: [message(3), message(4)],
      hasOlder: false,
      hasNewer: false,
    };
    const merged = mergeHistoryPage(cache, page);
    expect(ids(merged)).toEqual([1, 2, 3, 4]);
    expect(lastKnownSequence(merged)).toBe(4);
  });

  it("never shows a message twice: a loaded id is replaced only by a newer version", () => {
    const cache = cacheOf([
      [message(1), message(2, { version: 2, content: "edited" })],
    ]);
    const page: ChatMessagePage = {
      items: [message(2, { version: 1, content: "old copy" }), message(3)],
      hasOlder: false,
      hasNewer: false,
    };
    const merged = mergeHistoryPage(cache, page);
    expect(ids(merged)).toEqual([1, 2, 3]);
    expect(merged.pages[0]!.items[1]!.content).toBe("edited");
  });

  it("keeps the window open when the backfill itself has more pages", () => {
    const cache = cacheOf([[message(1)]]);
    const merged = mergeHistoryPage(cache, {
      items: [message(2)],
      hasOlder: false,
      hasNewer: true,
    });
    expect(windowIsLive(merged)).toBe(false);
    expect(ids(merged)).toEqual([1, 2]);
  });

  it("leaves a window that is behind the newest message alone", () => {
    const cache = cacheOf([[message(1)]], true);
    expect(
      mergeHistoryPage(cache, {
        items: [message(2)],
        hasOlder: false,
        hasNewer: false,
      }),
    ).toBe(cache);
  });
});

describe("frame parsing", () => {
  it("accepts the chat.message.v1 payload and rejects a malformed one", () => {
    expect(
      parseChatMessageFrame(frame({ id: "m-1", sequence: 1 })),
    ).toMatchObject({ id: "m-1", sequence: 1 });
    expect(
      parseChatMessageFrame({
        ...frame({ id: "m-1", sequence: 1 }),
        sequence: 0,
      }),
    ).toBeNull();
    expect(
      parseChatMessageFrame({
        ...frame({ id: "m-1", sequence: 1 }),
        kind: "bot",
      }),
    ).toBeNull();
    expect(parseChatMessageFrame("not an object")).toBeNull();
  });

  it("accepts the chat.unread.v1 payload and rejects negative counts", () => {
    expect(
      parseChatUnreadFrame({ spaceId: SPACE, unread: 2, mentions: 0 }),
    ).toEqual({
      spaceId: SPACE,
      unread: 2,
      mentions: 0,
    });
    expect(
      parseChatUnreadFrame({ spaceId: SPACE, unread: -1, mentions: 0 }),
    ).toBeNull();
  });
});

describe("applyUnreadFrame", () => {
  const row = (
    spaceId: string,
    unreadCount: number,
    mentionCount: number,
    muted = false,
  ) => ({
    spaceId,
    name: spaceId,
    readSequence: 5,
    lastSequence: 9,
    unreadCount,
    firstUnreadSequence: unreadCount > 0 ? 6 : null,
    mentionCount,
    notificationLevel: muted ? ("none" as const) : ("all" as const),
    muted,
  });
  const summary: ChatUnreadSummary = {
    spaces: [row("a", 1, 0), row("b", 2, 1), row("muted", 4, 0, true)],
    totalUnread: 3,
    totalMentions: 1,
  };

  it("updates one space in place and recomputes the totals without muted spaces", () => {
    const out = applyUnreadFrame(summary, {
      spaceId: "a",
      unread: 4,
      mentions: 2,
    });
    expect(out.kind).toBe("applied");
    if (out.kind !== "applied") return;
    expect(out.summary.spaces.map((entry) => entry.unreadCount)).toEqual([
      4, 2, 4,
    ]);
    expect(out.summary.totalUnread).toBe(6);
    expect(out.summary.totalMentions).toBe(3);
  });

  it("a space read on another device drops its count to zero", () => {
    const out = applyUnreadFrame(summary, {
      spaceId: "b",
      unread: 0,
      mentions: 0,
    });
    if (out.kind !== "applied") throw new Error("expected applied");
    expect(out.summary.spaces[1]).toMatchObject({
      unreadCount: 0,
      firstUnreadSequence: null,
    });
  });

  it("a space the summary does not list asks for a refetch; an identical frame changes nothing", () => {
    expect(
      applyUnreadFrame(summary, { spaceId: "new", unread: 1, mentions: 0 })
        .kind,
    ).toBe("unknown-space");
    expect(
      applyUnreadFrame(summary, { spaceId: "a", unread: 1, mentions: 0 }),
    ).toEqual({
      kind: "unchanged",
      summary,
    });
  });
});
