import { describe, expect, it } from "vitest";
import {
  buildChatRows,
  previousDayKey,
  type ChatRow,
  type PendingMessage,
} from "../core/model/chat-model";
import {
  emojiOnlyCount,
  messageActionGrants,
} from "../core/model/message-display-model";
import type { ChatMessage } from "../core/types/chat";

const ME = "user-me";
const OTHER = "user-other";
const THIRD = "user-third";

function message(
  sequence: number,
  at: string,
  overrides: Partial<ChatMessage> = {},
): ChatMessage {
  return {
    id: `m-${sequence}`,
    spaceId: "space-1",
    sequence,
    kind: "user",
    authorId: OTHER,
    content: `text ${sequence}`,
    clientMessageId: null,
    replyToId: null,
    edited: false,
    deleted: false,
    createdAt: `2026-11-10T${at}:00.000Z`,
    editedAt: null,
    deletedAt: null,
    version: 1,
    ...overrides,
  };
}

function rows(
  messages: ChatMessage[],
  options: { firstUnread?: number | null; pending?: PendingMessage[] } = {},
): ChatRow[] {
  return buildChatRows({
    messages,
    pending: options.pending ?? [],
    firstUnread: options.firstUnread ?? null,
    timeZone: "UTC",
  });
}

/** Each message row as [id, continued, groupEnd]. */
function groups(list: ChatRow[]) {
  return list.flatMap((row) =>
    row.kind === "message"
      ? [[row.message.id, row.continued, row.groupEnd] as const]
      : row.kind === "pending"
        ? [[row.clientMessageId, row.continued, row.groupEnd] as const]
        : [],
  );
}

describe("buildChatRows grouping", () => {
  it("groups one author's messages within five minutes, ending the group at the last one", () => {
    const list = rows([
      message(1, "09:00"),
      message(2, "09:02"),
      message(3, "09:05"),
    ]);
    expect(groups(list)).toEqual([
      ["m-1", false, false],
      ["m-2", true, false],
      ["m-3", true, true],
    ]);
  });

  it("starts a new group for another author and after more than five minutes", () => {
    const list = rows([
      message(1, "09:00"),
      message(2, "09:01", { authorId: THIRD }),
      message(3, "09:02", { authorId: THIRD }),
      message(4, "09:08", { authorId: THIRD }),
    ]);
    expect(groups(list)).toEqual([
      ["m-1", false, true],
      ["m-2", false, false],
      ["m-3", true, true],
      ["m-4", false, true],
    ]);
  });

  it("breaks a group at a day separator, at the unread divider, at a reply and at a system line", () => {
    const dayBreak = rows([
      message(1, "09:00", { createdAt: "2026-11-09T23:59:00.000Z" }),
      message(2, "09:00", { createdAt: "2026-11-10T00:01:00.000Z" }),
    ]);
    expect(dayBreak.map((row) => row.kind)).toEqual([
      "day",
      "message",
      "day",
      "message",
    ]);
    expect(groups(dayBreak)).toEqual([
      ["m-1", false, true],
      ["m-2", false, true],
    ]);

    const unreadBreak = rows([message(1, "09:00"), message(2, "09:01")], {
      firstUnread: 2,
    });
    expect(groups(unreadBreak)).toEqual([
      ["m-1", false, true],
      ["m-2", false, true],
    ]);

    const replyBreak = rows([
      message(1, "09:00"),
      message(2, "09:01", { replyToId: "m-0" }),
      message(3, "09:02"),
    ]);
    expect(groups(replyBreak)).toEqual([
      ["m-1", false, true],
      ["m-2", false, false],
      ["m-3", true, true],
    ]);

    const systemBreak = rows([
      message(1, "09:00"),
      message(2, "09:01", { kind: "system" }),
      message(3, "09:02"),
    ]);
    expect(groups(systemBreak).map(([, continued]) => continued)).toEqual([
      false,
      false,
      false,
    ]);
  });

  it("puts a time separator after a pause of more than fifteen minutes in one day, not at exactly fifteen", () => {
    const list = rows([
      message(1, "09:00"),
      message(2, "09:15"),
      message(3, "09:31"),
    ]);
    expect(list.map((row) => row.kind)).toEqual([
      "day",
      "message",
      "message",
      "time",
      "message",
    ]);
    const time = list.find((row) => row.kind === "time");
    expect(time).toMatchObject({ at: "2026-11-10T09:31:00.000Z" });
  });

  it("lets a pending send continue the caller's own group", () => {
    const list = rows([message(1, "09:00", { authorId: ME })], {
      pending: [
        {
          clientMessageId: "c-1",
          spaceId: "space-1",
          content: "more",
          authorId: ME,
          createdAt: "2026-11-10T09:01:00.000Z",
          status: "sending",
          failure: null,
        },
      ],
    });
    expect(groups(list)).toEqual([
      ["m-1", false, false],
      ["c-1", true, true],
    ]);
  });
});

describe("previousDayKey", () => {
  it("steps back over month and year ends", () => {
    expect(previousDayKey("2026-11-10")).toBe("2026-11-09");
    expect(previousDayKey("2026-03-01")).toBe("2026-02-28");
    expect(previousDayKey("2027-01-01")).toBe("2026-12-31");
  });
});

describe("emojiOnlyCount", () => {
  it("counts one to three emoji with nothing else", () => {
    expect(emojiOnlyCount("👍")).toBe(1);
    expect(emojiOnlyCount(" 🎉 ")).toBe(1);
    expect(emojiOnlyCount("👍❤️😄")).toBe(3);
    expect(emojiOnlyCount("👍 ❤️")).toBe(2);
    expect(emojiOnlyCount("👨‍👩‍👧")).toBe(1);
    expect(emojiOnlyCount("👍🏽")).toBe(1);
    expect(emojiOnlyCount("🇻🇳")).toBe(1);
    expect(emojiOnlyCount("1️⃣")).toBe(1);
  });

  it("is 0 for text, more than three emoji, digits and empty content", () => {
    expect(emojiOnlyCount("👍👍👍👍")).toBe(0);
    expect(emojiOnlyCount("ok 👍")).toBe(0);
    expect(emojiOnlyCount("1")).toBe(0);
    expect(emojiOnlyCount("#")).toBe(0);
    expect(emojiOnlyCount("")).toBe(0);
    expect(emojiOnlyCount(null)).toBe(0);
    expect(emojiOnlyCount("   ")).toBe(0);
  });
});

describe("messageActionGrants", () => {
  it("offers everything in a live channel where the viewer may post (older servers send no capabilities)", () => {
    expect(
      messageActionGrants({ archived: false, postRefused: false }),
    ).toEqual({
      react: true,
      reply: true,
      pin: true,
      editOwn: true,
      deleteOwn: true,
      moderate: false,
    });
  });

  it("makes an archived channel read-only, but keeps moderation to the server's word", () => {
    expect(
      messageActionGrants({
        archived: true,
        postRefused: true,
        canReact: true,
        canPin: true,
        canModerate: true,
      }),
    ).toEqual({
      react: false,
      reply: false,
      pin: false,
      editOwn: false,
      deleteOwn: false,
      moderate: true,
    });
  });

  it("follows the capabilities of a read-only role or a Guest: no reply without posting, no pin or react when refused", () => {
    expect(
      messageActionGrants({
        archived: false,
        postRefused: true,
        canReact: false,
        canPin: false,
        canModerate: false,
      }),
    ).toEqual({
      react: false,
      reply: false,
      pin: false,
      editOwn: true,
      deleteOwn: true,
      moderate: false,
    });
  });
});
