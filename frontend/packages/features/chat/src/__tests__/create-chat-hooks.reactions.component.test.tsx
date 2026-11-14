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
import { createChatHooks, flattenPages } from "../web/query/create-chat-hooks";
import { chatQueryKeys } from "../query/keys";
import type { ChatMessage } from "../core/types/chat";

const WORKSPACE = mockIds.workspaces.primary;
const PALETTE_THUMBS_UP = "👍";

/**
 * A client whose reaction writes wait for `gate` (when given) and fail with a
 * 500 when `fail` is set, so the optimistic step and its rollback can be seen.
 */
function setup(options: { gate?: Promise<void>; fail?: boolean } = {}) {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      const url = new URL(String(input instanceof Request ? input.url : input));
      if (
        (init?.method ?? "GET") !== "GET" &&
        url.pathname.includes("/reactions/")
      ) {
        if (options.gate) await options.gate;
        if (options.fail) {
          return new Response(
            JSON.stringify({ title: "Internal", status: 500 }),
            {
              status: 500,
              headers: { "content-type": "application/problem+json" },
            },
          );
        }
      }
      return createMockFetch(store)(input, init);
    },
  });
  const hooks = createChatHooks({
    api: client.api,
    endpoints: client.endpoints,
  });
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { store, hooks, queryClient, wrapper };
}

async function generalChannelId(hooks: ReturnType<typeof setup>["hooks"]) {
  const channels = await hooks.service.listChannels(WORKSPACE);
  const found = channels.find((channel) => channel.name === "general");
  if (!found) throw new Error("no general channel in the mock");
  return found.id;
}

function summaryOf(
  queryClient: QueryClient,
  channelId: string,
  messageId: string,
): ChatMessage | undefined {
  const cache = queryClient.getQueriesData<{
    pages: { items: ChatMessage[] }[];
  }>({ queryKey: chatQueryKeys.messages(WORKSPACE, channelId) });
  for (const [, data] of cache) {
    const found = data?.pages
      .flatMap((page) => page.items)
      .find((item) => item.id === messageId);
    if (found) return found;
  }
  return undefined;
}

describe("useReaction (optimistic react and unreact)", () => {
  it("shows the reaction at once in the loaded history, then keeps the server's summary", async () => {
    let release!: () => void;
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    const { hooks, queryClient, wrapper } = setup({ gate });
    const channelId = await generalChannelId(hooks);
    const { result } = renderHook(
      () => ({
        list: hooks.useMessages(WORKSPACE, channelId, { kind: "latest" }),
        react: hooks.useReaction(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    const target = flattenPages(result.current.list.data).find(
      (message) => message.kind === "user" && !message.deleted,
    )!;
    const before =
      summaryOf(queryClient, channelId, target.id)?.reactions ?? [];
    const countBefore =
      before.find((entry) => entry.emoji === PALETTE_THUMBS_UP)?.count ?? 0;

    act(() => {
      result.current.react.mutate({
        messageId: target.id,
        kind: PALETTE_THUMBS_UP,
        add: true,
      });
    });

    // The write is still in flight: the summary already shows the caller's reaction.
    await waitFor(() => {
      const shown =
        summaryOf(queryClient, channelId, target.id)?.reactions ?? [];
      const mine = shown.find((entry) => entry.emoji === PALETTE_THUMBS_UP);
      expect(mine?.reactedByMe).toBe(true);
      expect(mine?.count).toBe(countBefore + 1);
    });

    await act(async () => {
      release();
    });
    await waitFor(() => expect(result.current.react.isSuccess).toBe(true));
    const settled =
      summaryOf(queryClient, channelId, target.id)?.reactions ?? [];
    expect(
      settled.find((entry) => entry.emoji === PALETTE_THUMBS_UP)?.reactedByMe,
    ).toBe(true);
  });

  it("puts the summary back when the server refuses the reaction", async () => {
    const { hooks, queryClient, wrapper } = setup({ fail: true });
    const channelId = await generalChannelId(hooks);
    const { result } = renderHook(
      () => ({
        list: hooks.useMessages(WORKSPACE, channelId, { kind: "latest" }),
        react: hooks.useReaction(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    const target = flattenPages(result.current.list.data).find(
      (message) => message.kind === "user" && !message.deleted,
    )!;
    const before = JSON.stringify(
      summaryOf(queryClient, channelId, target.id)?.reactions ?? [],
    );

    await act(async () => {
      result.current.react.mutate({
        messageId: target.id,
        kind: PALETTE_THUMBS_UP,
        add: true,
      });
    });
    await waitFor(() => expect(result.current.react.isError).toBe(true));
    expect(
      JSON.stringify(
        summaryOf(queryClient, channelId, target.id)?.reactions ?? [],
      ),
    ).toBe(before);
  });

  it("removes the caller's reaction optimistically and keeps the others", async () => {
    const { hooks, queryClient, wrapper } = setup();
    const channelId = await generalChannelId(hooks);
    const { result } = renderHook(
      () => ({
        list: hooks.useMessages(WORKSPACE, channelId, { kind: "latest" }),
        react: hooks.useReaction(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    const target = flattenPages(result.current.list.data).find(
      (message) => message.kind === "user" && !message.deleted,
    )!;
    // Give the message a reaction the caller holds, then remove it.
    await act(async () => {
      await result.current.react.mutateAsync({
        messageId: target.id,
        kind: PALETTE_THUMBS_UP,
        add: true,
      });
    });
    await act(async () => {
      result.current.react.mutate({
        messageId: target.id,
        kind: PALETTE_THUMBS_UP,
        add: false,
      });
    });
    await waitFor(() => expect(result.current.react.isSuccess).toBe(true));
    const after = summaryOf(queryClient, channelId, target.id)?.reactions ?? [];
    expect(
      after.find((entry) => entry.emoji === PALETTE_THUMBS_UP),
    ).toBeUndefined();
  });
});
