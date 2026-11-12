import { QueryClient } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import {
  createChatRealtimeAdapter,
  type ChatRealtimeEnvelope,
} from "../web/realtime/chat-realtime-adapter";
import { chatQueryKeys } from "../query/keys";
import type {
  ChatMessage,
  ChatMessagePage,
  ChatUnreadSummary,
} from "../core/types/chat";
import type { ChatMessagesCache } from "../web/query/message-cache";
import type { PendingMessage } from "../core/model/chat-model";

const WS = "ws-1";
const CH = "space-1";
const AT = "2026-11-10T04:00:00.000Z";

function message(
  sequence: number,
  overrides: Partial<ChatMessage> = {},
): ChatMessage {
  return {
    id: `m-${sequence}`,
    spaceId: CH,
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

function page(items: ChatMessage[], hasNewer = false): ChatMessagePage {
  return { items, hasOlder: false, hasNewer };
}

function envelope(
  eventType: string,
  payload: Record<string, unknown> | undefined,
  over: Partial<ChatRealtimeEnvelope> = {},
): ChatRealtimeEnvelope {
  return {
    eventType,
    eventId: `evt-${Math.random()}`,
    resourceId: CH,
    workspaceId: WS,
    timestamp: AT,
    payload,
    ...over,
  };
}

function createMessagePayload(
  sequence: number,
  over: Record<string, unknown> = {},
) {
  return {
    id: `m-${sequence}`,
    sequence,
    authorId: "user-b",
    kind: "user",
    content: `incoming ${sequence}`,
    contentOmitted: false,
    deleted: false,
    createdAt: AT,
    editedAt: null,
    replyToId: null,
    version: 1,
    ...over,
  };
}

function setup(initial: ChatMessage[], hasNewer = false) {
  const queryClient = new QueryClient();
  const key = [...chatQueryKeys.messages(WS, CH), "latest"];
  queryClient.setQueryData<ChatMessagesCache>(key, {
    pageParams: [{}],
    pages: [{ items: initial, hasOlder: false, hasNewer }],
  });
  const listMessages = vi.fn(
    async (_ws: string, _ch: string, _cursor: { after?: number }) =>
      page([message(3), message(4)]),
  );
  const service = { listMessages } as never;
  const adapter = createChatRealtimeAdapter({ queryClient, service });
  const invalidateQueries = vi.fn();
  const context = { workspaceId: WS, channelId: CH, invalidateQueries };
  const read = () => queryClient.getQueryData<ChatMessagesCache>(key)!;
  const ids = () => read().pages.flatMap((p) => p.items.map((m) => m.sequence));
  return {
    queryClient,
    key,
    adapter,
    context,
    listMessages,
    invalidateQueries,
    read,
    ids,
  };
}

describe("ChatRealtimeAdapter: channel frames", () => {
  it("supports the message, reaction, pin and unread frames only", () => {
    const { adapter } = setup([]);
    expect(adapter.supports(envelope("chat.message.created", {}))).toBe(true);
    expect(adapter.supports(envelope("chat.unread.changed", {}))).toBe(true);
    expect(adapter.supports(envelope("chat.reaction.changed", {}))).toBe(true);
    expect(adapter.supports(envelope("chat.space.changed", {}))).toBe(true);
    expect(
      adapter.supports(envelope("chat.message.attachment.changed", {})),
    ).toBe(true);
    expect(adapter.supports(envelope("board.board", {}))).toBe(false);
  });

  it("a created frame of the open channel appends to the loaded history", async () => {
    const { adapter, context, ids } = setup([message(1), message(2)]);
    await adapter.validateAndHandle(
      envelope("chat.message.created", createMessagePayload(3)),
      context,
    );
    expect(ids()).toEqual([1, 2, 3]);
  });

  it("ignores frames of another channel or another workspace", async () => {
    const { adapter, context, listMessages, ids } = setup([message(1)]);
    await adapter.validateAndHandle(
      envelope("chat.message.created", createMessagePayload(2), {
        resourceId: "space-2",
      }),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.created", createMessagePayload(2), {
        workspaceId: "ws-2",
      }),
      context,
    );
    expect(ids()).toEqual([1]);
    expect(listMessages).not.toHaveBeenCalled();
  });

  it("a duplicate frame changes nothing (dedupe by id)", async () => {
    const { adapter, context, ids } = setup([message(1), message(2)]);
    await adapter.validateAndHandle(
      envelope("chat.message.created", createMessagePayload(2)),
      context,
    );
    expect(ids()).toEqual([1, 2]);
  });

  it("a gap fetches the range after the last known sequence, then merges it once", async () => {
    const { adapter, context, listMessages, ids } = setup([
      message(1),
      message(2),
    ]);
    await adapter.validateAndHandle(
      envelope("chat.message.created", createMessagePayload(5)),
      context,
    );
    expect(listMessages).toHaveBeenCalledTimes(1);
    expect(listMessages.mock.calls[0]![2]).toEqual({ after: 2 });
    expect(ids()).toEqual([1, 2, 3, 4]);
  });

  it("a payloadOmitted frame fetches the range after the last known sequence instead of using a payload", async () => {
    const { adapter, context, listMessages, ids } = setup([
      message(1),
      message(2),
    ]);
    await adapter.validateAndHandle(
      envelope("chat.message.created", undefined, { payloadOmitted: true }),
      context,
    );
    expect(listMessages.mock.calls[0]![2]).toEqual({ after: 2 });
    expect(ids()).toEqual([1, 2, 3, 4]);
  });

  it("a contentOmitted edit fetches the message's range and does not patch with null text", async () => {
    const { adapter, context, listMessages, read } = setup([
      message(1),
      message(2),
    ]);
    await adapter.validateAndHandle(
      envelope(
        "chat.message.updated",
        createMessagePayload(2, {
          content: null,
          contentOmitted: true,
          version: 2,
          editedAt: AT,
        }),
      ),
      context,
    );
    expect(listMessages.mock.calls[0]![2]).toEqual({ after: 1 });
    expect(read().pages[0]!.items[1]!.content).toBe("text 2");
  });

  it("an edit by a newer version patches the message in place; a stale one is ignored", async () => {
    const { adapter, context, read } = setup([message(1), message(2)]);
    await adapter.validateAndHandle(
      envelope(
        "chat.message.updated",
        createMessagePayload(2, {
          content: "edited",
          editedAt: AT,
          version: 2,
        }),
      ),
      context,
    );
    expect(read().pages[0]!.items[1]).toMatchObject({
      content: "edited",
      edited: true,
      version: 2,
    });
    await adapter.validateAndHandle(
      envelope(
        "chat.message.updated",
        createMessagePayload(2, { content: "old", version: 1 }),
      ),
      context,
    );
    expect(read().pages[0]!.items[1]!.content).toBe("edited");
  });

  it("a delete leaves a tombstone with the same id and sequence", async () => {
    const { adapter, context, read } = setup([message(1), message(2)]);
    await adapter.validateAndHandle(
      envelope(
        "chat.message.deleted",
        createMessagePayload(2, { content: null, deleted: true, version: 2 }),
      ),
      context,
    );
    expect(read().pages[0]!.items[1]).toMatchObject({
      id: "m-2",
      sequence: 2,
      deleted: true,
      content: null,
    });
  });

  it("a created frame of an own send confirms its pending entry by clientMessageId", async () => {
    const { adapter, context, queryClient } = setup([message(1)]);
    const pendingKey = chatQueryKeys.pending(WS, CH);
    queryClient.setQueryData<PendingMessage[]>(pendingKey, [
      {
        clientMessageId: "cmid-9",
        spaceId: CH,
        content: "mine",
        authorId: "me",
        createdAt: AT,
        status: "sending",
        failure: null,
      },
    ]);
    await adapter.validateAndHandle(
      envelope(
        "chat.message.created",
        createMessagePayload(2, {
          id: "m-2",
          authorId: "me",
          clientMessageId: "cmid-9",
        }),
      ),
      context,
    );
    expect(queryClient.getQueryData<PendingMessage[]>(pendingKey)).toEqual([]);
  });

  it("a window that is behind the newest message is not patched by a gap", async () => {
    const { adapter, context, listMessages, ids } = setup(
      [message(1), message(2)],
      true,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.created", createMessagePayload(7)),
      context,
    );
    expect(listMessages).not.toHaveBeenCalled();
    expect(ids()).toEqual([1, 2]);
  });

  it("a backfill that never catches up reloads the window instead (repeated gaps)", async () => {
    const {
      adapter,
      context,
      listMessages,
      invalidateQueries,
      queryClient,
      key,
    } = setup([message(1)]);
    listMessages.mockImplementation(
      async (_w, _c, cursor: { after?: number }) =>
        page([message((cursor.after ?? 0) + 1)], true),
    );
    await adapter.validateAndHandle(
      envelope("chat.message.created", createMessagePayload(50)),
      context,
    );
    expect(listMessages.mock.calls.length).toBe(5);
    expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true);
    expect(invalidateQueries).not.toHaveBeenCalled();
  });
});

describe("ChatRealtimeAdapter: unread frames", () => {
  const summary: ChatUnreadSummary = {
    spaces: [
      {
        spaceId: CH,
        name: "general",
        readSequence: 1,
        lastSequence: 2,
        unreadCount: 1,
        firstUnreadSequence: 2,
        mentionCount: 0,
        notificationLevel: "all",
        muted: false,
      },
    ],
    totalUnread: 1,
    totalMentions: 0,
  };

  it("updates the unread summary cache in place and the totals with it", async () => {
    const { adapter, context, queryClient } = setup([message(1)]);
    queryClient.setQueryData(chatQueryKeys.unread(WS), summary);
    await adapter.validateAndHandle(
      envelope(
        "chat.unread.changed",
        { spaceId: CH, unread: 3, mentions: 1 },
        { resourceId: "user-1" },
      ),
      context,
    );
    expect(
      queryClient.getQueryData<ChatUnreadSummary>(chatQueryKeys.unread(WS)),
    ).toMatchObject({
      totalUnread: 3,
      totalMentions: 1,
    });
  });

  it("a frame for a space the summary does not list refetches the summary and the channel list", async () => {
    const { adapter, context, queryClient } = setup([message(1)]);
    queryClient.setQueryData(chatQueryKeys.unread(WS), summary);
    const invalidate = vi.fn();
    await adapter.validateAndHandle(
      envelope("chat.unread.changed", {
        spaceId: "new-space",
        unread: 1,
        mentions: 0,
      }),
      { ...context, invalidateQueries: invalidate },
    );
    expect(invalidate).toHaveBeenCalledWith([
      [...chatQueryKeys.unread(WS)],
      [...chatQueryKeys.channels(WS)],
    ]);
  });
});

describe("ChatRealtimeAdapter: recovery", () => {
  it("a channel subscription that is acknowledged again backfills after the last known sequence", async () => {
    const { adapter, listMessages, ids } = setup([message(1), message(2)]);
    await adapter.recover({
      reason: "subscribed",
      workspaceId: WS,
      scope: { kind: "channel", id: CH },
    });
    expect(listMessages.mock.calls[0]![2]).toEqual({ after: 2 });
    expect(ids()).toEqual([1, 2, 3, 4]);
  });

  it("a lost channel leaves the sidebar cache and its detail is read again", async () => {
    const { adapter, queryClient } = setup([message(1)]);
    queryClient.setQueryData(chatQueryKeys.channels(WS), [
      { id: CH },
      { id: "other" },
    ]);
    const spy = vi.spyOn(queryClient, "invalidateQueries");
    await adapter.recover({
      reason: "permission-lost",
      workspaceId: WS,
      scope: { kind: "channel", id: CH },
    });
    expect(queryClient.getQueryData(chatQueryKeys.channels(WS))).toEqual([
      { id: "other" },
    ]);
    expect(spy).toHaveBeenCalledWith({
      queryKey: chatQueryKeys.channel(WS, CH),
    });
  });

  it("a gap of the per-user stream reads the unread counters again", async () => {
    const { adapter, queryClient } = setup([message(1)]);
    const spy = vi.spyOn(queryClient, "invalidateQueries");
    await adapter.recover({
      reason: "gap",
      workspaceId: WS,
      scope: { kind: "user", id: "me" },
    });
    expect(spy).toHaveBeenCalledWith({ queryKey: chatQueryKeys.unread(WS) });
  });
});

describe("ChatRealtimeAdapter: threads, reactions and pins", () => {
  function threadFrame(
    sequence: number,
    threadSequence: number,
    over: Record<string, unknown> = {},
  ) {
    return createMessagePayload(sequence, {
      id: `reply-${threadSequence}`,
      threadRootId: "m-1",
      threadSequence,
      ...over,
    });
  }

  it("a thread reply is not inserted into the channel history and moves its root's count", async () => {
    const { adapter, context, queryClient, read } = setup([
      message(1),
      message(2),
    ]);
    const counts = chatQueryKeys.threadCounts(WS, CH);
    await adapter.validateAndHandle(
      envelope("chat.message.created", threadFrame(3, 1)),
      context,
    );
    expect(
      queryClient.getQueryData<Record<string, { replyCount: number }>>(
        counts,
      )?.["m-1"]?.replyCount,
    ).toBe(1);
    // The reply itself stays out of the main flow; the channel backfill (after the sequence before it) brings only what is in the main flow.
    expect(read().pages[0]!.items.some((item) => item.id === "reply-1")).toBe(
      false,
    );
  });

  it("a thread reply joins a loaded thread once, in order", async () => {
    const { adapter, context, queryClient } = setup([message(1)]);
    const threadKey = chatQueryKeys.thread(WS, "m-1");
    queryClient.setQueryData(threadKey, {
      pageParams: [{}],
      pages: [
        {
          root: message(1),
          thread: { rootMessageId: "m-1", replyCount: 0 },
          items: [],
          hasNewer: false,
        },
      ],
    });
    await adapter.validateAndHandle(
      envelope("chat.message.created", threadFrame(3, 1)),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.created", threadFrame(3, 1)),
      context,
    );
    const pages = queryClient.getQueryData<{
      pages: { items: ChatMessage[] }[];
    }>(threadKey)!.pages;
    expect(pages[0]!.items.map((item) => item.id)).toEqual(["reply-1"]);
  });

  it("a reaction frame of another person moves the summary of the message in every loaded cache", async () => {
    const { adapter, context, queryClient, read } = setup([
      message(1),
      message(2),
    ]);
    await adapter.validateAndHandle(
      envelope("chat.reaction.changed", {
        messageId: "m-2",
        kind: "👍",
        change: "added",
        actorId: "someone-else",
      }),
      context,
    );
    const target = read().pages[0]!.items.find((item) => item.id === "m-2")!;
    expect(target.reactions).toEqual([
      { emoji: "👍", count: 1, reactedByMe: false },
    ]);
    expect(
      queryClient.getQueryData(chatQueryKeys.pins(WS, CH)),
    ).toBeUndefined();
  });

  it("a reaction frame of the viewer does not count the caller's own write twice", async () => {
    const { queryClient, context, read } = setup([
      message(1, { reactions: [{ emoji: "👍", count: 1, reactedByMe: true }] }),
    ]);
    const adapterWithViewer = createChatRealtimeAdapter({
      queryClient,
      service: { listMessages: vi.fn() } as never,
      viewerId: "me",
    });
    await adapterWithViewer.validateAndHandle(
      envelope("chat.reaction.changed", {
        messageId: "m-1",
        kind: "👍",
        change: "added",
        actorId: "me",
      }),
      context,
    );
    expect(read().pages[0]!.items[0]!.reactions).toEqual([
      { emoji: "👍", count: 1, reactedByMe: true },
    ]);
  });

  it("a pin frame invalidates the pin list of the channel", async () => {
    const { adapter, context, invalidateQueries, queryClient } = setup([]);
    const spy = vi.spyOn(queryClient, "invalidateQueries");
    await adapter.validateAndHandle(
      envelope("chat.space.changed", { messageId: "m-1", change: "pinned" }),
      context,
    );
    expect(spy).toHaveBeenCalledWith({ queryKey: chatQueryKeys.pins(WS, CH) });
    expect(invalidateQueries).not.toHaveBeenCalled();
  });

  it("a permissions frame reads the channel, the channel list and the permissions again, not the pins", async () => {
    const { adapter, context, queryClient } = setup([]);
    const spy = vi.spyOn(queryClient, "invalidateQueries");
    await adapter.validateAndHandle(
      envelope("chat.space.changed", {
        messageId: "00000000-0000-0000-0000-000000000000",
        change: "permissions",
      }),
      context,
    );
    expect(spy).toHaveBeenCalledWith({
      queryKey: chatQueryKeys.channel(WS, CH),
    });
    expect(spy).toHaveBeenCalledWith({ queryKey: chatQueryKeys.channels(WS) });
    expect(spy).toHaveBeenCalledWith({
      queryKey: chatQueryKeys.permissions(WS, CH),
    });
    expect(spy).not.toHaveBeenCalledWith({
      queryKey: chatQueryKeys.pins(WS, CH),
    });
  });

  it("a slow-mode frame reads the channel and the channel list again, not the pins or the permissions", async () => {
    const { adapter, context, queryClient } = setup([]);
    const spy = vi.spyOn(queryClient, "invalidateQueries");
    await adapter.validateAndHandle(
      envelope("chat.space.changed", {
        messageId: "00000000-0000-0000-0000-000000000000",
        change: "slow-mode",
      }),
      context,
    );
    expect(spy).toHaveBeenCalledWith({
      queryKey: chatQueryKeys.channel(WS, CH),
    });
    expect(spy).toHaveBeenCalledWith({ queryKey: chatQueryKeys.channels(WS) });
    expect(spy).not.toHaveBeenCalledWith({
      queryKey: chatQueryKeys.pins(WS, CH),
    });
  });

  it("a retention frame reads the messages, the pins and the channel views of the space again", async () => {
    const { adapter, context, queryClient } = setup([]);
    const spy = vi.spyOn(queryClient, "invalidateQueries");
    await adapter.validateAndHandle(
      envelope("chat.space.changed", {
        messageId: "00000000-0000-0000-0000-000000000000",
        change: "retention",
      }),
      context,
    );
    expect(spy).toHaveBeenCalledWith({
      queryKey: chatQueryKeys.messages(WS, CH),
    });
    expect(spy).toHaveBeenCalledWith({ queryKey: chatQueryKeys.pins(WS, CH) });
    expect(spy).toHaveBeenCalledWith({
      queryKey: chatQueryKeys.channel(WS, CH),
    });
    expect(spy).toHaveBeenCalledWith({ queryKey: chatQueryKeys.channels(WS) });
  });

  it("a space frame with an unknown change is ignored", async () => {
    const { adapter, context, queryClient } = setup([]);
    const spy = vi.spyOn(queryClient, "invalidateQueries");
    await adapter.validateAndHandle(
      envelope("chat.space.changed", { messageId: "m-1", change: "renamed" }),
      context,
    );
    expect(spy).not.toHaveBeenCalled();
  });
});

describe("ChatRealtimeAdapter: message attachments (UC-CHT-G08)", () => {
  const added = (id = "f-1", over: Record<string, unknown> = {}) => ({
    messageId: "m-2",
    change: "added",
    attachmentId: id,
    fileName: `${id}.pdf`,
    sizeBytes: 12,
    contentType: "application/pdf",
    uploadedBy: "user-a",
    createdAt: AT,
    downloadPath: `/api/v1/workspaces/${WS}/chat/messages/m-2/attachments/${id}/content`,
    detailsOmitted: false,
    ...over,
  });
  const removed = (id = "f-1") => ({
    messageId: "m-2",
    change: "removed",
    attachmentId: id,
    detailsOmitted: false,
  });
  const files = (read: () => ChatMessagesCache) =>
    read().pages[0]!.items.find((item) => item.id === "m-2")!.attachments;

  it("an added frame puts the file on the message once, and a replay changes nothing", async () => {
    const { adapter, context, read } = setup([message(1), message(2)]);
    const frame = envelope("chat.message.attachment.changed", added());
    await adapter.validateAndHandle(frame, context);
    const first = read().pages[0]!.items;
    await adapter.validateAndHandle(frame, context);

    expect(files(read)?.map((file) => file.id)).toEqual(["f-1"]);
    expect(files(read)?.[0]).toMatchObject({
      messageId: "m-2",
      fileName: "f-1.pdf",
      downloadPath: expect.stringContaining("/attachments/f-1/content"),
    });
    expect(read().pages[0]!.items[1]).toBe(first[1]);
  });

  it("a removed frame takes the file off, and a removal of an unknown file is a no-op", async () => {
    const { adapter, context, read } = setup([message(1), message(2)]);
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", added("f-1")),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", added("f-2")),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", removed("f-1")),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", removed("f-1")),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", removed("nope")),
      context,
    );
    expect(files(read)?.map((file) => file.id)).toEqual(["f-2"]);
  });

  it("patches a message in a loaded thread and in the pins as well", async () => {
    const { adapter, context, queryClient } = setup([message(1)]);
    const threadKey = chatQueryKeys.thread(WS, "m-1");
    queryClient.setQueryData(threadKey, {
      pageParams: [{}],
      pages: [{ root: message(1), items: [message(2)], hasNewer: false }],
    });
    const pinsKey = chatQueryKeys.pins(WS, CH);
    queryClient.setQueryData(pinsKey, {
      pageParams: [{}],
      pages: [
        {
          items: [{ message: message(2), pinnedBy: "user-a", pinnedAt: AT }],
        },
      ],
    });
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", added()),
      context,
    );
    const thread = queryClient.getQueryData<{
      pages: { items: ChatMessage[] }[];
    }>(threadKey)!;
    const pins = queryClient.getQueryData<{
      pages: { items: { message: ChatMessage }[] }[];
    }>(pinsKey)!;
    expect(thread.pages[0]!.items[0]!.attachments).toHaveLength(1);
    expect(pins.pages[0]!.items[0]!.message.attachments).toHaveLength(1);
  });

  it("ignores a message that is not loaded, a malformed frame, another channel and an omitted payload", async () => {
    const { adapter, context, read } = setup([message(1), message(2)]);
    const before = read();
    await adapter.validateAndHandle(
      envelope(
        "chat.message.attachment.changed",
        added("f-1", { messageId: "unknown" }),
      ),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", { messageId: "m-2" }),
      context,
    );
    await adapter.validateAndHandle(
      envelope(
        "chat.message.attachment.changed",
        added("f-3", { fileName: null }),
      ),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", added(), {
        resourceId: "other-space",
      }),
      context,
    );
    await adapter.validateAndHandle(
      envelope("chat.message.attachment.changed", undefined, {
        payloadOmitted: true,
      }),
      context,
    );
    expect(read()).toEqual(before);
  });

  it("a frame whose details did not fit reads the message's files over REST", async () => {
    const { queryClient, context, read } = setup([message(1), message(2)]);
    const listAttachments = vi.fn().mockResolvedValue([
      {
        id: "f-9",
        messageId: "m-2",
        fileName: "long.pdf",
        sizeBytes: 3,
        contentType: "application/pdf",
        uploadedBy: "user-a",
        createdAt: AT,
        downloadPath: "/x",
      },
    ]);
    const adapterWithList = createChatRealtimeAdapter({
      queryClient,
      service: { listMessages: vi.fn(), listAttachments } as never,
    });
    await adapterWithList.validateAndHandle(
      envelope(
        "chat.message.attachment.changed",
        added("f-9", {
          fileName: null,
          sizeBytes: null,
          contentType: null,
          uploadedBy: null,
          createdAt: null,
          downloadPath: null,
          detailsOmitted: true,
        }),
      ),
      context,
    );
    expect(listAttachments).toHaveBeenCalledWith(WS, "m-2");
    expect(files(read)?.map((file) => file.id)).toEqual(["f-9"]);
  });
});
