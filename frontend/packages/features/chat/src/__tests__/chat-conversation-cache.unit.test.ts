import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryObserver } from "@tanstack/react-query";
import {
  setConversationUnread,
  touchConversation,
} from "../web/query/conversation-cache";
import { chatQueryKeys } from "../query/keys";
import type {
  ChatConversation,
  ChatConversationPreview,
} from "../core/types/chat";

const WS = "ws-1";
const KEY = chatQueryKeys.conversations(WS);

function conversation(
  id: string,
  overrides: Partial<ChatConversation> = {},
): ChatConversation {
  return {
    id,
    kind: "direct",
    name: null,
    participants: [],
    isHidden: false,
    notificationLevel: "all",
    unreadCount: 0,
    lastMessage: null,
    lastActivityAt: "2026-11-10T09:00:00.000Z",
    version: 1,
    createdAt: "2026-11-01T09:00:00.000Z",
    ...overrides,
  } as ChatConversation;
}

const preview = (sequence: number): ChatConversationPreview => ({
  sequence,
  authorId: "u-1",
  kind: "user",
  text: `m${sequence}`,
  createdAt: "2026-11-10T10:00:00.000Z",
});

function setup(list: ChatConversation[]) {
  const queryClient = new QueryClient();
  queryClient.setQueryData(KEY, list);
  const read = () => queryClient.getQueryData<ChatConversation[]>(KEY)!;
  return { queryClient, read };
}

describe("conversation cache", () => {
  it("a stored message becomes the preview and moves its conversation to the top", () => {
    const { queryClient, read } = setup([
      conversation("a"),
      conversation("b"),
      conversation("c"),
    ]);
    touchConversation(queryClient, WS, "c", preview(7));
    expect(read().map((entry) => entry.id)).toEqual(["c", "a", "b"]);
    expect(read()[0]!.lastMessage?.text).toBe("m7");
    expect(read()[0]!.lastActivityAt).toBe("2026-11-10T10:00:00.000Z");
  });

  it("an older message does not replace a newer preview", () => {
    const { queryClient, read } = setup([
      conversation("a", { lastMessage: preview(9) }),
    ]);
    touchConversation(queryClient, WS, "a", preview(8));
    expect(read()[0]!.lastMessage?.sequence).toBe(9);
  });

  it("a higher unread count moves the conversation up and a lower one only changes the count", () => {
    const { queryClient, read } = setup([
      conversation("a", { unreadCount: 2 }),
      conversation("b"),
    ]);
    setConversationUnread(
      queryClient,
      WS,
      "b",
      1,
      () => "2026-11-10T11:00:00.000Z",
    );
    expect(read().map((entry) => entry.id)).toEqual(["b", "a"]);
    expect(read()[0]!.unreadCount).toBe(1);
    setConversationUnread(queryClient, WS, "a", 0);
    expect(read().map((entry) => entry.id)).toEqual(["b", "a"]);
    expect(read()[1]!.unreadCount).toBe(0);
  });

  it("a space the lists do not know asks the server once; a known channel does not", () => {
    const { queryClient } = setup([conversation("a")]);
    queryClient.setQueryData(chatQueryKeys.channels(WS), [{ id: "chan" }]);
    const state = () => queryClient.getQueryState(KEY)!.isInvalidated;
    touchConversation(queryClient, WS, "chan", preview(1));
    expect(state()).toBe(false);
    touchConversation(queryClient, WS, "new-conversation", preview(1));
    expect(state()).toBe(true);
  });

  it("many frames of a space in neither list ask the server once, not once per frame", async () => {
    vi.useFakeTimers();
    try {
      const queryClient = new QueryClient();
      const fetchList = vi.fn(async () => [conversation("a")]);
      const watch = new QueryObserver(queryClient, {
        queryKey: KEY,
        queryFn: fetchList,
        staleTime: Infinity,
      });
      const unsubscribe = watch.subscribe(() => undefined);
      await vi.advanceTimersByTimeAsync(0);
      fetchList.mockClear();
      for (let sequence = 1; sequence <= 20; sequence += 1) {
        touchConversation(queryClient, WS, "unknown", preview(sequence));
        setConversationUnread(queryClient, WS, "unknown", sequence);
        await vi.advanceTimersByTimeAsync(50);
      }
      expect(fetchList.mock.calls.length).toBeLessThanOrEqual(1);
      unsubscribe();
    } finally {
      vi.useRealTimers();
    }
  });
});
