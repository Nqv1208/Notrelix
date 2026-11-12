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
import { chatFailureOf } from "../core/api/chat-failure";
import { chatQueryKeys } from "../query/keys";

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

describe("channel permission hooks (UC-CHT-H01)", () => {
  it("reads the overrides of a channel and saves a whole set, seeding the cache with the new version", async () => {
    const { hooks, queryClient, wrapper } = setup();
    const channelId = await generalChannelId(hooks);
    const { result } = renderHook(
      () => ({
        read: hooks.useChannelPermissions(WORKSPACE, channelId),
        write: hooks.useSetChannelPermissions(WORKSPACE, channelId),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.read.isSuccess).toBe(true));
    expect(result.current.read.data?.entries).toEqual([]);
    const version = result.current.read.data!.version;

    await act(async () => {
      await result.current.write.mutateAsync({
        entries: [{ subject: "member", action: "post", effect: "deny" }],
        expectedVersion: version,
      });
    });

    const cached = queryClient.getQueryData<{
      version: number;
      entries: unknown[];
    }>(chatQueryKeys.permissions(WORKSPACE, channelId));
    expect(cached?.version).toBe(version + 1);
    expect(cached?.entries).toEqual([
      { subject: "member", action: "post", effect: "deny" },
    ]);
  });

  it("an empty set clears the overrides", async () => {
    const { hooks, wrapper } = setup();
    const channelId = await generalChannelId(hooks);
    const first = await hooks.service.getChannelPermissions(
      WORKSPACE,
      channelId,
    );
    const made = await hooks.service.setChannelPermissions(
      WORKSPACE,
      channelId,
      {
        entries: [{ subject: "guest", action: "post", effect: "deny" }],
        expectedVersion: first.version,
      },
    );
    const { result } = renderHook(
      () => hooks.useSetChannelPermissions(WORKSPACE, channelId),
      { wrapper },
    );

    let answer: Awaited<ReturnType<typeof result.current.mutateAsync>>;
    await act(async () => {
      answer = await result.current.mutateAsync({
        entries: [],
        expectedVersion: made.version,
      });
    });

    expect(answer!.entries).toEqual([]);
    expect(answer!.version).toBe(made.version + 1);
  });

  it("a stale version is classified as stale and the permissions are read again", async () => {
    const { hooks, wrapper } = setup();
    const channelId = await generalChannelId(hooks);
    const { result } = renderHook(
      () => hooks.useSetChannelPermissions(WORKSPACE, channelId),
      { wrapper },
    );

    let failure: unknown;
    await act(async () => {
      try {
        await result.current.mutateAsync({
          entries: [{ subject: "member", action: "post", effect: "deny" }],
          expectedVersion: 1,
        });
      } catch (error) {
        failure = error;
      }
    });

    expect(chatFailureOf(failure).kind).toBe("stale");
  });

  it("a refused allow above the role is classified as permission-exceeds-role", async () => {
    const { hooks, wrapper } = setup();
    const channelId = await generalChannelId(hooks);
    const current = await hooks.service.getChannelPermissions(
      WORKSPACE,
      channelId,
    );
    const { result } = renderHook(
      () => hooks.useSetChannelPermissions(WORKSPACE, channelId),
      { wrapper },
    );

    let failure: unknown;
    await act(async () => {
      try {
        await result.current.mutateAsync({
          entries: [{ subject: "guest", action: "pin", effect: "allow" }],
          expectedVersion: current.version,
        });
      } catch (error) {
        failure = error;
      }
    });

    expect(chatFailureOf(failure).kind).toBe("permission-exceeds-role");
  });
});

describe("useModerateMessage (UC-CHT-H03)", () => {
  it("removes another person's message and shows the tombstone as removed by a moderator", async () => {
    const { store, hooks, wrapper } = setup();
    const channelId = await generalChannelId(hooks);
    const { result } = renderHook(
      () => ({
        list: hooks.useMessages(WORKSPACE, channelId, { kind: "latest" }),
        moderate: hooks.useModerateMessage(WORKSPACE, channelId),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    const target = flattenPages(result.current.list.data).find(
      (message) =>
        message.kind === "user" &&
        !message.deleted &&
        message.authorId !== mockIds.users.owner,
    )!;

    await act(async () => {
      await result.current.moderate.mutateAsync({
        messageId: target.id,
        reason: "spam",
        expectedVersion: target.version,
      });
    });

    await waitFor(() => {
      const after = flattenPages(result.current.list.data).find(
        (message) => message.id === target.id,
      );
      expect(after?.deleted).toBe(true);
      expect(after?.removedByModerator).toBe(true);
    });
    expect(store.chat.messages.get(target.id)?.deletedBy).toBe(
      mockIds.users.owner,
    );
  });
});
