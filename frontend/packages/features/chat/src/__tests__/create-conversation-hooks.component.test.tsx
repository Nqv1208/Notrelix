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
import { createChatHooks } from "../web/query/create-chat-hooks";
import { chatQueryKeys } from "../query/keys";
import type { ChatConversation } from "../core/types/chat";

const WORKSPACE = mockIds.workspaces.primary;

/** A client whose visibility and block writes wait for `gate` and fail with a 500 when `fail` is set. */
function setup(options: { gate?: Promise<void>; fail?: boolean } = {}) {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: async (input, init) => {
      const url = new URL(String(input instanceof Request ? input.url : input));
      if (
        (init?.method ?? "GET") === "PUT" &&
        (url.pathname.endsWith("/visibility") ||
          url.pathname.endsWith("/me/blocks"))
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
  const others = store
    .getWorkspaceMembers(WORKSPACE)
    .map((member) => member.userId)
    .filter((id) => id !== store.getCurrentUser().id);
  return { store, hooks, queryClient, wrapper, others };
}

function cached(queryClient: QueryClient): ChatConversation[] | undefined {
  return queryClient.getQueryData<ChatConversation[]>(
    chatQueryKeys.conversations(WORKSPACE),
  );
}

function blockedOf(queryClient: QueryClient): readonly string[] | undefined {
  return queryClient.getQueryData<readonly string[]>(
    chatQueryKeys.blocks(WORKSPACE),
  );
}

describe("conversation hooks", () => {
  it("starts a direct conversation and lists it, and starting it again returns the same one", async () => {
    const { hooks, wrapper, queryClient, others } = setup();
    const { result } = renderHook(() => hooks.useStartConversation(WORKSPACE), {
      wrapper,
    });
    let first!: ChatConversation;
    await act(async () => {
      first = (
        await result.current.mutateAsync({ kind: "direct", userId: others[0]! })
      ).conversation;
    });
    expect(cached(queryClient)?.map((entry) => entry.id)).toContain(first.id);
    let again!: { conversation: ChatConversation; created: boolean };
    await act(async () => {
      again = await result.current.mutateAsync({
        kind: "direct",
        userId: others[0]!,
      });
    });
    expect(again.created).toBe(false);
    expect(again.conversation.id).toBe(first.id);
  });

  it("hides a conversation at once and keeps it hidden when the server confirms", async () => {
    let release!: () => void;
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    const { hooks, wrapper, queryClient, others } = setup({ gate });
    const started = await hooks.service.startConversation(
      WORKSPACE,
      { kind: "direct", userId: others[0]! },
      "key-0001-abc",
    );
    const list = renderHook(() => hooks.useConversations(WORKSPACE), {
      wrapper,
    });
    await waitFor(() => expect(list.result.current.isSuccess).toBe(true));
    const hide = renderHook(
      () => hooks.useSetConversationVisibility(WORKSPACE),
      {
        wrapper,
      },
    );
    act(() => {
      hide.result.current.mutate({
        conversationId: started.conversation.id,
        hidden: true,
      });
    });
    await waitFor(() =>
      expect(
        cached(queryClient)?.find((e) => e.id === started.conversation.id)
          ?.isHidden,
      ).toBe(true),
    );
    release();
    await waitFor(() => expect(hide.result.current.isSuccess).toBe(true));
    await waitFor(() =>
      expect(
        cached(queryClient)?.some((e) => e.id === started.conversation.id),
      ).toBe(false),
    );
  });

  it("rolls the hide back when the server refuses", async () => {
    const { hooks, wrapper, queryClient, others } = setup({ fail: true });
    const started = await hooks.service.startConversation(
      WORKSPACE,
      { kind: "direct", userId: others[0]! },
      "key-0002-abc",
    );
    const list = renderHook(() => hooks.useConversations(WORKSPACE), {
      wrapper,
    });
    await waitFor(() => expect(list.result.current.isSuccess).toBe(true));
    const hide = renderHook(
      () => hooks.useSetConversationVisibility(WORKSPACE),
      {
        wrapper,
      },
    );
    await act(async () => {
      await hide.result.current
        .mutateAsync({ conversationId: started.conversation.id, hidden: true })
        .catch(() => undefined);
    });
    await waitFor(() => expect(hide.result.current.isError).toBe(true));
    expect(
      cached(queryClient)?.find((e) => e.id === started.conversation.id)
        ?.isHidden,
    ).toBe(false);
  });

  it("marks a person blocked at once, and unblocks them", async () => {
    const { hooks, wrapper, store, queryClient, others } = setup();
    const blocked = renderHook(() => hooks.useBlockedPeople(WORKSPACE), {
      wrapper,
    });
    const block = renderHook(() => hooks.useBlockPerson(WORKSPACE), {
      wrapper,
    });
    await act(async () => {
      await block.result.current.mutateAsync({
        userId: others[0]!,
        blocked: true,
      });
    });
    expect(blockedOf(queryClient)).toEqual([others[0]]);
    await waitFor(() =>
      expect(blocked.result.current.data).toEqual([others[0]]),
    );
    expect(store.chat.blocks.size).toBe(1);
    await act(async () => {
      await block.result.current.mutateAsync({
        userId: others[0]!,
        blocked: false,
      });
    });
    expect(blockedOf(queryClient)).toEqual([]);
  });

  it("reads the blocks the server already holds on load, so a reload keeps them", async () => {
    const { hooks, wrapper, store, others } = setup();
    await hooks.service.setPersonBlock(WORKSPACE, others[0]!, true);
    expect(store.chat.blocks.size).toBe(1);

    const blocked = renderHook(() => hooks.useBlockedPeople(WORKSPACE), {
      wrapper,
    });

    await waitFor(() =>
      expect(blocked.result.current.data).toEqual([others[0]]),
    );
  });

  it("rolls the block back when the server refuses", async () => {
    const { hooks, wrapper, queryClient, others } = setup({ fail: true });
    renderHook(() => hooks.useBlockedPeople(WORKSPACE), { wrapper });
    const block = renderHook(() => hooks.useBlockPerson(WORKSPACE), {
      wrapper,
    });
    await act(async () => {
      await block.result.current
        .mutateAsync({ userId: others[0]!, blocked: true })
        .catch(() => undefined);
    });
    expect(blockedOf(queryClient)).toEqual([]);
  });

  it("drops a group from the list when the caller leaves it", async () => {
    const { hooks, wrapper, queryClient, others } = setup();
    const started = await hooks.service.startConversation(
      WORKSPACE,
      { kind: "group", userIds: [others[0]!, others[1]!], name: "Trio" },
      "key-0003-abc",
    );
    const list = renderHook(() => hooks.useConversations(WORKSPACE), {
      wrapper,
    });
    await waitFor(() =>
      expect(
        list.result.current.data?.some((e) => e.id === started.conversation.id),
      ).toBe(true),
    );
    const leave = renderHook(() => hooks.useLeaveConversation(WORKSPACE), {
      wrapper,
    });
    await act(async () => {
      await leave.result.current.mutateAsync(started.conversation.id);
    });
    await waitFor(() =>
      expect(
        cached(queryClient)?.some((e) => e.id === started.conversation.id),
      ).toBe(false),
    );
  });
});
