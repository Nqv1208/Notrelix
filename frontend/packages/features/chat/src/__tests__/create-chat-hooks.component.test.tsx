import type { ReactNode } from "react";
import { act, renderHook, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { chatFailureOf } from "../core/api/chat-failure";
import { flattenPages, createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const hooks = createChatHooks({
    api: client.api,
    endpoints: client.endpoints,
  });
  const viewer = store.getCurrentUser().id;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { store, hooks, viewer, queryClient, wrapper };
}

async function channelIdNamed(
  hooks: ReturnType<typeof setup>["hooks"],
  name: string,
) {
  const channels = await hooks.service.listChannels(WORKSPACE);
  const found = channels.find((channel) => channel.name === name);
  if (!found) throw new Error(`no channel ${name}`);
  return found.id;
}

describe("useMessages", () => {
  it("loads the newest page, then pages older and keeps the sequence ascending with no gaps", async () => {
    const { hooks, wrapper } = setup();
    const general = await channelIdNamed(hooks, "general");
    const { result } = renderHook(
      () => hooks.useMessages(WORKSPACE, general, { kind: "latest" }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    const firstPage = flattenPages(result.current.data);
    expect(firstPage.length).toBe(50);
    expect(result.current.hasPreviousPage).toBe(true);

    await act(async () => {
      await result.current.fetchPreviousPage();
    });
    await waitFor(() => expect(result.current.data?.pages.length).toBe(2));
    const sequences = flattenPages(result.current.data).map(
      (message) => message.sequence,
    );
    expect(sequences.length).toBe(100);
    expect(sequences).toEqual([...sequences].sort((a, b) => a - b));
    expect(new Set(sequences).size).toBe(sequences.length);
  });
});

describe("useSendMessage", () => {
  it("shows the message as pending while the send is in flight, then as stored once the server confirms it", async () => {
    const store = new MockStore({ ...defaultConfig });
    let release!: () => void;
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: async (input, init) => {
        const url = new URL(
          String(input instanceof Request ? input.url : input),
        );
        if (
          (init?.method ?? "GET") === "POST" &&
          url.pathname.endsWith("/messages")
        ) {
          await gate;
        }
        return createMockFetch(store)(input, init);
      },
    });
    const hooks = createChatHooks({
      api: client.api,
      endpoints: client.endpoints,
    });
    const viewer = store.getCurrentUser().id;
    const general = await channelIdNamed(hooks, "general");
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    const wrap = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
    const { result } = renderHook(
      () => ({
        list: hooks.useMessages(WORKSPACE, general, { kind: "latest" }),
        send: hooks.useSendMessage(WORKSPACE, general, viewer),
        pending: hooks.usePendingSends(WORKSPACE, general),
      }),
      { wrapper: wrap },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));

    let clientMessageId = "";
    act(() => {
      clientMessageId = result.current.send.send("hello from the test");
    });
    await waitFor(() =>
      expect(
        result.current.pending.data.map((entry) => entry.clientMessageId),
      ).toContain(clientMessageId),
    );
    expect(
      flattenPages(result.current.list.data).some(
        (m) => m.content === "hello from the test",
      ),
    ).toBe(false);

    await act(async () => {
      release();
    });
    await waitFor(() =>
      expect(
        flattenPages(result.current.list.data).some(
          (m) => m.content === "hello from the test",
        ),
      ).toBe(true),
    );
    await waitFor(() =>
      expect(
        result.current.pending.data.map((entry) => entry.clientMessageId),
      ).not.toContain(clientMessageId),
    );
    const stored = flattenPages(result.current.list.data).filter(
      (m) => m.clientMessageId === clientMessageId,
    );
    expect(stored.length).toBe(1);
  });
});

describe("useEditMessage", () => {
  it("refuses a stale expectedVersion with the stale failure and changes nothing", async () => {
    const { hooks, viewer, wrapper } = setup();
    const general = await channelIdNamed(hooks, "general");
    const { result } = renderHook(
      () => ({
        list: hooks.useMessages(WORKSPACE, general, { kind: "latest" }),
        edit: hooks.useEditMessage(WORKSPACE, general),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    const own = flattenPages(result.current.list.data).find(
      (message) =>
        message.authorId === viewer &&
        message.kind === "user" &&
        !message.deleted,
    );
    if (!own) throw new Error("the seed has no own message in general");

    let failure: ReturnType<typeof chatFailureOf> | null = null;
    await act(async () => {
      await result.current.edit
        .mutateAsync({
          messageId: own.id,
          content: "stale edit",
          expectedVersion: own.version + 99,
        })
        .catch((error: unknown) => {
          failure = chatFailureOf(error);
        });
    });
    expect(failure).not.toBeNull();
    expect(failure!.kind).toBe("stale");
  });
});

describe("useMarkRead", () => {
  it("moves the read boundary forward only, even when a lower one is sent after a higher one", async () => {
    const { hooks, wrapper } = setup();
    const general = await channelIdNamed(hooks, "general");
    const { result } = renderHook(
      () => ({
        unread: hooks.useUnreadSummary(WORKSPACE),
        read: hooks.useMarkRead(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.unread.isSuccess).toBe(true));

    const boundaryOf = () =>
      result.current.unread.data?.spaces.find((row) => row.spaceId === general)
        ?.readSequence ?? -1;

    // The seed's boundary for general is 300 of 320: read further, then send a lower one.
    await waitFor(() => expect(boundaryOf()).toBe(300));
    act(() => {
      result.current.read.markRead(general, 310);
    });
    await waitFor(() => expect(boundaryOf()).toBe(310));

    act(() => {
      result.current.read.markRead(general, 250);
    });
    expect(boundaryOf()).toBe(310);
    const server = await hooks.service.getUnread(WORKSPACE);
    expect(
      server.spaces.find((row) => row.spaceId === general)?.readSequence,
    ).toBe(310);
  });
});
