import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { act, renderHook, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import type { PendingMessage } from "../core/model/chat-model";
import { createChatHooks } from "../web/query/create-chat-hooks";
import { useChannelTimeline } from "../web/components/use-channel-timeline";
import { chatQueryKeys } from "../query/keys";

const WORKSPACE = mockIds.workspaces.primary;
/** The owner's first unread message in `general`. */
const FIRST_UNREAD = 301;

// `general` gets 8 newer messages, so the window around the first unread one is not the newest window.
async function setup() {
  const store = new MockStore({ ...defaultConfig });
  const net = { gate: null as Promise<void> | null, holdAll: false };
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      if (net.holdAll && net.gate) await net.gate;
      return createMockFetch(store)(input, init);
    },
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const hooks = createChatHooks(deps);
  const channels = await hooks.service.listChannels(WORKSPACE);
  const general = channels.find((entry) => entry.name === "general")!;
  for (let index = 1; index <= 8; index += 1) {
    await hooks.service.sendMessage(WORKSPACE, general.id, {
      content: `newer ${index}`,
      clientMessageId: `cid-newer-${index}`,
    });
  }
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { hooks, general, queryClient, wrapper, net, store };
}

type Setup = Awaited<ReturnType<typeof setup>>;

function open(
  s: Setup,
  options: {
    firstUnreadSequence?: number | null;
    linkedSequence?: number | null;
    hooks?: Setup["hooks"];
  } = {},
) {
  const onShowThread = vi.fn();
  const view = renderHook(
    () =>
      useChannelTimeline({
        hooks: options.hooks ?? s.hooks,
        workspaceId: WORKSPACE,
        channelId: s.general.id,
        linkedSequence: options.linkedSequence ?? null,
        unreadSettled: true,
        firstUnreadSequence:
          options.firstUnreadSequence === undefined
            ? FIRST_UNREAD
            : options.firstUnreadSequence,
        onShowThread,
      }),
    { wrapper: s.wrapper },
  );
  return { ...view, onShowThread };
}

function pendingSend(s: Setup, text: string): PendingMessage {
  return {
    clientMessageId: `cid-${text}`,
    spaceId: s.general.id,
    content: text,
    authorId: s.store.getCurrentUser().id,
    createdAt: new Date().toISOString(),
    status: "sending",
    failure: null,
  };
}

function seedPending(s: Setup, entry: PendingMessage) {
  s.queryClient.setQueryData(chatQueryKeys.pending(WORKSPACE, s.general.id), [
    entry,
  ]);
}

describe("useChannelTimeline", () => {
  it("shows an own send while the first window is still loading", async () => {
    const s = await setup();
    let release!: () => void;
    s.net.gate = new Promise<void>((resolve) => (release = resolve));
    s.net.holdAll = true;
    seedPending(s, pendingSend(s, "typed early"));
    const { result } = open(s);

    // No window yet, and the bubble is already a row.
    expect(result.current.messages.isPending).toBe(true);
    expect(result.current.flat).toHaveLength(0);
    expect(result.current.pendingHere.map((entry) => entry.content)).toEqual([
      "typed early",
    ]);
    expect(result.current.rows.map((row) => row.kind)).toContain("pending");

    s.net.holdAll = false;
    release();
    await waitFor(() => expect(result.current.messages.isSuccess).toBe(true));
    expect(result.current.rows.map((row) => row.kind)).toContain("pending");
  });

  it("keeps the bubble of a send from an older window, then moves to the newest window", async () => {
    const s = await setup();
    seedPending(s, pendingSend(s, "from older"));
    const { result } = open(s);
    await waitFor(() => expect(result.current.messages.isSuccess).toBe(true));
    // The window around the first unread message: the newest ones are not in it.
    expect(result.current.hasNewer).toBe(true);
    expect(result.current.flat.some((m) => m.content === "newer 8")).toBe(
      false,
    );
    const before = result.current.scrollRequest?.nonce ?? 0;

    act(() => result.current.afterSend());

    // At once: a scroll request to the bottom, the same window, the bubble still drawn.
    expect(result.current.scrollRequest?.target).toBe("bottom");
    expect(result.current.scrollRequest!.nonce).toBeGreaterThan(before);
    expect(result.current.hasNewer).toBe(true);
    expect(result.current.pendingHere).toHaveLength(1);

    // After two frames and a task: the newest window, with no divider.
    await waitFor(() =>
      expect(result.current.flat.some((m) => m.content === "newer 8")).toBe(
        true,
      ),
    );
    expect(result.current.hasNewer).toBe(false);
    expect(result.current.rows.map((row) => row.kind)).not.toContain("unread");
  });

  it("highlights a linked message once its window has loaded", async () => {
    const s = await setup();
    const { result } = open(s, { linkedSequence: FIRST_UNREAD });
    await waitFor(() => expect(result.current.highlightId).not.toBeNull());
    expect(
      result.current.flat.find((m) => m.id === result.current.highlightId)
        ?.sequence,
    ).toBe(FIRST_UNREAD);
    // A linked open shows no first-unread divider.
    expect(result.current.rows.map((row) => row.kind)).not.toContain("unread");
  });

  it("highlights nothing for a linked message that is missing", async () => {
    const s = await setup();
    const { result } = open(s, { linkedSequence: 999_999 });
    await waitFor(() => expect(result.current.messages.isSuccess).toBe(true));
    expect(result.current.flat.length).toBeGreaterThan(0);
    // The effect ran after the window loaded: it found nothing and shows no detail.
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 20));
    });
    expect(result.current.highlightId).toBeNull();
  });

  it("marks read once per new last-visible message", async () => {
    const s = await setup();
    const markRead = vi.fn();
    const hooks = {
      ...s.hooks,
      useMarkRead: (workspaceId: string) => ({
        ...s.hooks.useMarkRead(workspaceId),
        markRead,
      }),
    } as unknown as Setup["hooks"];
    // No unread: the newest window opens and its last message is on screen.
    const { result, rerender } = open(s, { firstUnreadSequence: null, hooks });
    await waitFor(() => expect(markRead).toHaveBeenCalledTimes(1));
    const newest = result.current.flat[result.current.flat.length - 1]!;
    expect(markRead).toHaveBeenLastCalledWith(s.general.id, newest.sequence);

    // A re-render with the same last message does not report again.
    rerender();
    act(() => result.current.setAtBottom(true));
    expect(markRead).toHaveBeenCalledTimes(1);

    // Scrolled away: a new message arrives but is not seen, so nothing is reported.
    act(() => result.current.setAtBottom(false));
    await s.hooks.service.sendMessage(WORKSPACE, s.general.id, {
      content: "arrives while away",
      clientMessageId: "cid-away",
    });
    await act(async () => {
      await s.queryClient.invalidateQueries({
        queryKey: chatQueryKeys.messages(WORKSPACE, s.general.id),
      });
    });
    await waitFor(() =>
      expect(
        result.current.flat.some((m) => m.content === "arrives while away"),
      ).toBe(true),
    );
    expect(markRead).toHaveBeenCalledTimes(1);

    // Back at the bottom: the new last message is reported, once.
    act(() => result.current.setAtBottom(true));
    await waitFor(() => expect(markRead).toHaveBeenCalledTimes(2));
    const last = result.current.flat[result.current.flat.length - 1]!;
    expect(markRead).toHaveBeenLastCalledWith(s.general.id, last.sequence);
  });

  it("jumps to the newest window and drops the first-unread marker", async () => {
    const s = await setup();
    const { result } = open(s);
    await waitFor(() => expect(result.current.messages.isSuccess).toBe(true));
    expect(result.current.scrollRequest?.target).toBe("divider");
    expect(result.current.rows.map((row) => row.kind)).toContain("unread");

    act(() => result.current.jumpToLatest());

    expect(result.current.scrollRequest?.target).toBe("bottom");
    await waitFor(() =>
      expect(result.current.flat.some((m) => m.content === "newer 8")).toBe(
        true,
      ),
    );
    expect(result.current.hasNewer).toBe(false);
    expect(result.current.rows.map((row) => row.kind)).not.toContain("unread");
  });
});
