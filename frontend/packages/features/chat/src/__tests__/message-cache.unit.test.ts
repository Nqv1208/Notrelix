import { describe, expect, it } from "vitest";
import {
  appendToNewest,
  replaceMessage,
  tombstoneMessage,
  type ChatMessagesCache,
} from "../web/query/message-cache";
import type { ChatMessage } from "../core/types/chat";

function message(
  sequence: number,
  overrides: Partial<ChatMessage> = {},
): ChatMessage {
  return {
    id: `m-${sequence}`,
    spaceId: "space-1",
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

describe("appendToNewest", () => {
  it("adds a stored message to the newest page once", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const next = appendToNewest(cache, message(3));
    expect(next.pages[0]!.items.map((m) => m.sequence)).toEqual([1, 2, 3]);
    expect(appendToNewest(next, message(3))).toBe(next);
  });

  it("does not add when the loaded window is not the newest", () => {
    const cache = cacheOf([[message(1), message(2)]], true);
    expect(appendToNewest(cache, message(3))).toBe(cache);
  });
});

describe("replaceMessage", () => {
  it("takes the server copy when it is as new or newer", () => {
    const cache = cacheOf([[message(1), message(2, { version: 2 })]]);
    const next = replaceMessage(
      cache,
      message(2, { version: 3, content: "edited", edited: true }),
    );
    expect(next.pages[0]!.items[1]!.content).toBe("edited");
  });

  it("never moves a version back", () => {
    const cache = cacheOf([[message(1), message(2, { version: 4 })]]);
    expect(
      replaceMessage(cache, message(2, { version: 3, content: "old" })),
    ).toBe(cache);
  });
});

describe("tombstoneMessage", () => {
  it("keeps the sequence, author and time, and drops the content", () => {
    const cache = cacheOf([[message(1), message(2)]]);
    const next = tombstoneMessage(cache, "m-2", "2026-11-10T05:00:00.000Z");
    const tomb = next.pages[0]!.items[1]!;
    expect(tomb).toMatchObject({
      sequence: 2,
      authorId: "user-a",
      content: null,
      deleted: true,
      deletedAt: "2026-11-10T05:00:00.000Z",
      version: 2,
    });
  });

  it("changes nothing for a message that is already a tombstone", () => {
    const cache = tombstoneMessage(
      cacheOf([[message(1), message(2)]]),
      "m-2",
      "2026-11-10T05:00:00.000Z",
    );
    expect(tombstoneMessage(cache, "m-2", "2026-11-10T06:00:00.000Z")).toBe(
      cache,
    );
  });
});
