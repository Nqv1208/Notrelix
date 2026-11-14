import { describe, expect, it } from "vitest";
import {
  buildChatRows,
  checkMessageContent,
  dayKeyOf,
  firstUnreadSequence,
  mergeMessages,
  nextReadBoundary,
  shouldReportRead,
  showsCounter,
  unreadAfterRead,
  unconfirmedPending,
  type PendingMessage,
} from "../core/model/chat-model";
import type {
  ChatMessage,
  ChatMessagePage,
  ChatUnreadRow,
} from "../core/types/chat";

const ME = "user-me";
const OTHER = "user-other";

function message(
  overrides: Partial<ChatMessage> & { sequence: number },
): ChatMessage {
  return {
    id: `m-${overrides.sequence}`,
    spaceId: "space-1",
    kind: "user",
    authorId: OTHER,
    content: `text ${overrides.sequence}`,
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

describe("checkMessageContent", () => {
  it("trims and accepts up to 4000 code points", () => {
    expect(checkMessageContent("  hi  ")).toEqual({ ok: true, content: "hi" });
    const atLimit = "a".repeat(4000);
    expect(checkMessageContent(atLimit)).toEqual({
      ok: true,
      content: atLimit,
    });
  });

  it("refuses blank text and text over 4000 characters", () => {
    expect(checkMessageContent("   ")).toEqual({ ok: false, reason: "empty" });
    expect(checkMessageContent("a".repeat(4001))).toEqual({
      ok: false,
      reason: "too-long",
    });
  });

  it("counts code points, so an emoji is one character", () => {
    const emoji = "😀".repeat(4000);
    expect(checkMessageContent(emoji).ok).toBe(true);
    expect(checkMessageContent(`${emoji}😀`)).toEqual({
      ok: false,
      reason: "too-long",
    });
  });

  it("shows the counter from 3800 characters", () => {
    expect(showsCounter("a".repeat(3799))).toBe(false);
    expect(showsCounter("a".repeat(3800))).toBe(true);
  });
});

describe("mergeMessages", () => {
  it("orders by sequence and keeps one copy per id, the higher version", () => {
    const older: ChatMessagePage = {
      items: [message({ sequence: 1 }), message({ sequence: 2, version: 1 })],
      hasOlder: false,
      hasNewer: true,
    };
    const newer: ChatMessagePage = {
      items: [
        message({ sequence: 2, version: 3, content: "edited" }),
        message({ sequence: 3 }),
      ],
      hasOlder: true,
      hasNewer: false,
    };
    const merged = mergeMessages([newer, older]);
    expect(merged.map((m) => m.sequence)).toEqual([1, 2, 3]);
    expect(merged[1]!.content).toBe("edited");
  });
});

describe("unconfirmedPending", () => {
  it("drops a pending send the server already stored, and keeps the rest oldest first", () => {
    const pending = (id: string, createdAt: string): PendingMessage => ({
      clientMessageId: id,
      spaceId: "space-1",
      content: id,
      authorId: ME,
      createdAt,
      status: "sending",
      failure: null,
    });
    const stored = [message({ sequence: 1, clientMessageId: "c-stored" })];
    const result = unconfirmedPending(
      [
        pending("c-later", "2026-11-10T04:00:00.000Z"),
        pending("c-stored", "2026-11-10T03:00:00.000Z"),
        pending("c-first", "2026-11-10T02:00:00.000Z"),
      ],
      stored,
    );
    expect(result.map((p) => p.clientMessageId)).toEqual([
      "c-first",
      "c-later",
    ]);
  });
});

describe("dayKeyOf", () => {
  it("reads the day in the viewer's zone", () => {
    // 17:30 UTC on 11 Nov is 00:30 on 12 Nov in Hanoi.
    expect(dayKeyOf("2026-11-11T17:30:00.000Z", "Asia/Ho_Chi_Minh")).toBe(
      "2026-11-12",
    );
    expect(dayKeyOf("2026-11-11T17:30:00.000Z", "UTC")).toBe("2026-11-11");
  });
});

describe("firstUnreadSequence", () => {
  const messages = [
    message({ sequence: 1 }),
    message({ sequence: 2, authorId: ME }),
    message({ sequence: 3 }),
    message({ sequence: 4, kind: "system" }),
  ];

  it("finds the first message after the read boundary that someone else wrote", () => {
    expect(firstUnreadSequence(messages, 0, ME)).toBe(1);
    expect(firstUnreadSequence(messages, 1, ME)).toBe(3);
  });

  it("is null when nothing after the boundary is someone else's", () => {
    expect(firstUnreadSequence(messages, 3, ME)).toBeNull();
  });
});

describe("buildChatRows", () => {
  const timeZone = "UTC";

  it("places a day separator before each new day and the divider before the first unread", () => {
    const messages = [
      message({ sequence: 1, createdAt: "2026-11-10T09:00:00.000Z" }),
      message({ sequence: 2, createdAt: "2026-11-10T09:01:00.000Z" }),
      message({ sequence: 3, createdAt: "2026-11-11T09:00:00.000Z" }),
    ];
    const rows = buildChatRows({
      messages,
      pending: [],
      firstUnread: 2,
      timeZone,
    });
    expect(rows.map((row) => row.kind)).toEqual([
      "day",
      "message",
      "unread",
      "message",
      "day",
      "message",
    ]);
  });

  it("continues an author run within five minutes without a new header", () => {
    const messages = [
      message({
        sequence: 1,
        authorId: OTHER,
        createdAt: "2026-11-10T09:00:00.000Z",
      }),
      message({
        sequence: 2,
        authorId: OTHER,
        createdAt: "2026-11-10T09:04:00.000Z",
      }),
      message({
        sequence: 3,
        authorId: OTHER,
        createdAt: "2026-11-10T09:20:00.000Z",
      }),
    ];
    const rows = buildChatRows({
      messages,
      pending: [],
      firstUnread: null,
      timeZone,
    });
    const continued = rows.flatMap((row) =>
      row.kind === "message" ? [row.continued] : [],
    );
    expect(continued).toEqual([false, true, false]);
  });

  it("appends pending sends after the stored messages", () => {
    const rows = buildChatRows({
      messages: [message({ sequence: 1 })],
      pending: [
        {
          clientMessageId: "c-1",
          spaceId: "space-1",
          content: "hello",
          authorId: ME,
          createdAt: "2026-11-10T10:00:00.000Z",
          status: "sending",
          failure: null,
        },
      ],
      firstUnread: null,
      timeZone,
    });
    expect(rows[rows.length - 1]!.kind).toBe("pending");
  });
});

describe("read boundary", () => {
  it("moves forward only", () => {
    expect(nextReadBoundary(5, 3)).toBe(5);
    expect(nextReadBoundary(5, 9)).toBe(9);
  });

  it("reports a read only when the candidate is past both known boundaries", () => {
    expect(shouldReportRead(7, { cached: 5, sent: 6 })).toBe(true);
    expect(shouldReportRead(6, { cached: 5, sent: 6 })).toBe(false);
    expect(shouldReportRead(4, { cached: 5, sent: 0 })).toBe(false);
  });

  it("drops the unread count to zero once the read reaches the last sequence", () => {
    const row: ChatUnreadRow = {
      spaceId: "space-1",
      name: "general",
      readSequence: 2,
      lastSequence: 6,
      unreadCount: 4,
      firstUnreadSequence: 3,
      mentionCount: 0,
      notificationLevel: "all",
      muted: false,
    };
    const partway = unreadAfterRead(row, 4);
    expect(partway.readSequence).toBe(4);
    expect(partway.unreadCount).toBe(4);
    const done = unreadAfterRead(row, 6);
    expect(done.unreadCount).toBe(0);
    expect(done.firstUnreadSequence).toBeNull();
    expect(unreadAfterRead(row, 1)).toBe(row);
  });
});
