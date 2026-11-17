import type { ReactNode } from "react";
import { act, renderHook, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { CHAT_LIMITS } from "../core/types/chat";
import { createChatHooks, flattenPages } from "../web/query/create-chat-hooks";
import { createChatRealtimeAdapter } from "../web/realtime/chat-realtime-adapter";

const WORKSPACE = mockIds.workspaces.primary;
const AT = "2026-11-10T04:00:00.000Z";

async function setup() {
  const store = new MockStore({ ...defaultConfig });
  createMockFetch(store);
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const hooks = createChatHooks({
    api: client.api,
    endpoints: client.endpoints,
  });
  const viewer = store.getCurrentUser().id;
  const channels = await hooks.service.listChannels(WORKSPACE);
  const general = channels.find((c) => c.name === "general")!.id;
  const inject = (
    globalThis as unknown as {
      __notrelixMockInjectChat: (s: string, a: string, c: string) => boolean;
    }
  ).__notrelixMockInjectChat;
  // Enough history for more pages than the cap keeps.
  for (
    let i = 0;
    i < CHAT_LIMITS.pageSize * (CHAT_LIMITS.maxLoadedPages + 2);
    i++
  )
    inject(general, "mock-user-admin", `history ${i}`);
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  const adapter = createChatRealtimeAdapter({
    queryClient,
    service: hooks.service,
  });
  const frame = (sequence: number) =>
    adapter.validateAndHandle(
      {
        eventType: "chat.message.created",
        eventId: `evt-${sequence}`,
        resourceId: general,
        workspaceId: WORKSPACE,
        timestamp: AT,
        payload: {
          id: `incoming-${sequence}`,
          sequence,
          authorId: "mock-user-admin",
          kind: "user",
          content: `incoming ${sequence}`,
          contentOmitted: false,
          deleted: false,
          createdAt: AT,
          editedAt: null,
          replyToId: null,
          version: 1,
        },
      },
      {
        workspaceId: WORKSPACE,
        channelId: general,
        invalidateQueries: vi.fn(),
      },
    );
  return { hooks, viewer, general, wrapper, frame, inject };
}

/** The query only re-renders for the fields read in the render: the pages and the end flag are read here. */
function watch<T extends { data: unknown; hasNextPage: boolean }>(query: T): T {
  void query.data;
  void query.hasNextPage;
  return query;
}

const ordered = (sequences: number[]) =>
  sequences.every((s, i) => i === 0 || s > sequences[i - 1]!);

// 600 messages are written and paged through the mock, so the default 5 s is short on a busy machine.
vi.setConfig({ testTimeout: 30_000 });

// 600 messages are written and paged through the mock, so the default 5 s is short on a busy machine.
vi.setConfig({ testTimeout: 30_000 });

describe("the loaded history at its page cap", () => {
  it("keeps a new incoming message and an own send in order while the newest page is still loaded", async () => {
    const { hooks, viewer, general, wrapper, frame, inject } = await setup();
    const { result } = renderHook(
      () => ({
        list: watch(hooks.useMessages(WORKSPACE, general, { kind: "latest" })),
        send: hooks.useSendMessage(WORKSPACE, general, viewer),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    for (let i = 1; i < CHAT_LIMITS.maxLoadedPages; i++) {
      await act(async () => {
        await result.current.list.fetchPreviousPage();
      });
      await waitFor(() =>
        expect(result.current.list.data?.pages.length).toBe(i + 1),
      );
    }
    await waitFor(() =>
      expect(result.current.list.data?.pages.length).toBe(
        CHAT_LIMITS.maxLoadedPages,
      ),
    );
    const newest = flattenPages(result.current.list.data).at(-1)!.sequence;

    // The server stores the incoming message first (its sequence is the next one), then announces it.
    inject(general, "mock-user-admin", `incoming ${newest + 1}`);
    await act(async () => {
      await frame(newest + 1);
    });
    act(() => {
      result.current.send.send("own at the cap");
    });
    await waitFor(
      () =>
        expect(
          flattenPages(result.current.list.data).some(
            (m) => m.content === "own at the cap",
          ),
        ).toBe(true),
      { timeout: 10_000 },
    );
    const rows = flattenPages(result.current.list.data);
    expect(rows.map((m) => m.content).slice(-2)).toEqual([
      `incoming ${newest + 1}`,
      "own at the cap",
    ]);
    expect(ordered(rows.map((m) => m.sequence))).toBe(true);
    expect(new Set(rows.map((m) => m.id)).size).toBe(rows.length);
  });

  it("drops the newest page past the cap, and a frame then waits for the jump to the newest instead of landing out of order", async () => {
    const { hooks, general, wrapper, frame } = await setup();
    const { result } = renderHook(
      () => watch(hooks.useMessages(WORKSPACE, general, { kind: "latest" })),
      { wrapper },
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    for (let i = 1; i <= CHAT_LIMITS.maxLoadedPages; i++) {
      await act(async () => {
        await result.current.fetchPreviousPage();
      });
      await waitFor(() =>
        expect(result.current.data?.pages.length).toBe(
          Math.min(i + 1, CHAT_LIMITS.maxLoadedPages),
        ),
      );
    }
    const rows = flattenPages(result.current.data);
    expect(ordered(rows.map((m) => m.sequence))).toBe(true);
    expect(result.current.hasNextPage).toBe(true);

    await act(async () => {
      await frame(rows.at(-1)!.sequence + 1000);
    });
    const after = flattenPages(result.current.data);
    expect(after.length).toBe(rows.length);
    expect(ordered(after.map((m) => m.sequence))).toBe(true);
  });
});
