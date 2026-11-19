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
  type MockPersona,
} from "@notrelix/dev-mock-backend";
import { createChatHooks } from "../web/query/create-chat-hooks";
import { chatFailureOf } from "../core/api/chat-failure";
import { chatQueryKeys } from "../query/keys";
import type { ChatChannel, ChatChannelGroup } from "../core/types/chat";

const WORKSPACE = mockIds.workspaces.primary;

function setup(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
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

async function failureOf(run: () => Promise<unknown>) {
  try {
    await run();
  } catch (error) {
    return chatFailureOf(error);
  }
  throw new Error("expected the call to fail");
}

describe("category hooks (UC-CHT-A02)", () => {
  it("creates a category that shows in the list and the cache at its position", async () => {
    const { hooks, queryClient, wrapper } = setup();
    const { result } = renderHook(
      () => ({
        list: hooks.useChannelGroups(WORKSPACE),
        create: hooks.useCreateChannelGroup(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));

    await act(async () => {
      await result.current.create.mutateAsync({ name: "Ops" });
    });

    await waitFor(() =>
      expect(
        queryClient
          .getQueryData<ChatChannelGroup[]>(
            chatQueryKeys.channelGroups(WORKSPACE),
          )
          ?.map((group) => group.name),
      ).toContain("Ops"),
    );
    // The server put it last.
    const names = (await hooks.service.listChannelGroups(WORKSPACE)).map(
      (group) => group.name,
    );
    expect(names[names.length - 1]).toBe("Ops");
  });

  it("refuses a duplicate name as a name conflict and stores nothing", async () => {
    const { hooks, store, wrapper } = setup();
    const { result } = renderHook(
      () => hooks.useCreateChannelGroup(WORKSPACE),
      {
        wrapper,
      },
    );
    const before = store.chat.groups.size;
    let failure: unknown;
    await act(async () => {
      failure = await result.current
        .mutateAsync({ name: "Team" })
        .catch((error: unknown) => error);
    });
    expect(chatFailureOf(failure).kind).toBe("name-conflict");
    expect(store.chat.groups.size).toBe(before);
  });

  it("is refused for a Member as a forbidden failure", async () => {
    const { hooks } = setup("member");
    const failure = await failureOf(() =>
      hooks.service.createChannelGroup(
        WORKSPACE,
        { name: "Mine" },
        crypto.randomUUID(),
      ),
    );
    expect(failure.kind).toBe("forbidden");
  });

  it("renames at once, and a stale version puts the old name back and reads the server again", async () => {
    const { hooks, store, queryClient, wrapper } = setup();
    const { result } = renderHook(
      () => ({
        list: hooks.useChannelGroups(WORKSPACE),
        update: hooks.useUpdateChannelGroups(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.list.isSuccess).toBe(true));
    const team = result.current.list.data!.find((g) => g.name === "Team")!;

    await act(async () => {
      await result.current.update.mutateAsync([
        { group: team, name: "Crew", position: team.position },
      ]);
    });
    expect(store.chat.groups.get(team.id)?.name).toBe("Crew");

    // Someone else renamed it first: the version this tab holds is behind.
    let failure: unknown;
    await act(async () => {
      failure = await result.current.update
        .mutateAsync([{ group: team, name: "Mine", position: team.position }])
        .catch((error: unknown) => error);
    });
    expect(chatFailureOf(failure).kind).toBe("stale");
    await waitFor(() =>
      expect(
        queryClient
          .getQueryData<ChatChannelGroup[]>(
            chatQueryKeys.channelGroups(WORKSPACE),
          )
          ?.find((g) => g.id === team.id)?.name,
      ).toBe("Crew"),
    );
  });

  it("deletes a category, leaving its channels in place and ungrouped", async () => {
    const { hooks, store, queryClient, wrapper } = setup();
    const { result } = renderHook(
      () => ({
        groups: hooks.useChannelGroups(WORKSPACE),
        channels: hooks.useChannels(WORKSPACE),
        remove: hooks.useDeleteChannelGroup(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => {
      expect(result.current.groups.isSuccess).toBe(true);
      expect(result.current.channels.isSuccess).toBe(true);
    });
    const team = result.current.groups.data!.find((g) => g.name === "Team")!;
    const held = result.current.channels.data!.filter(
      (c) => c.groupId === team.id,
    );
    expect(held.length).toBeGreaterThan(0);

    await act(async () => {
      await result.current.remove.mutateAsync(team);
    });

    expect(store.chat.groups.has(team.id)).toBe(false);
    await waitFor(() => {
      const channels = queryClient.getQueryData<ChatChannel[]>(
        chatQueryKeys.channels(WORKSPACE),
      )!;
      for (const channel of held) {
        expect(channels.find((c) => c.id === channel.id)?.groupId).toBeNull();
      }
    });
  });

  it("a stale delete leaves the category and its channels as they were", async () => {
    const { hooks, store, wrapper } = setup();
    const { result } = renderHook(
      () => hooks.useDeleteChannelGroup(WORKSPACE),
      {
        wrapper,
      },
    );
    const team = (await hooks.service.listChannelGroups(WORKSPACE)).find(
      (g) => g.name === "Team",
    )!;
    let failure: unknown;
    await act(async () => {
      failure = await result.current
        .mutateAsync({ ...team, version: team.version + 5 })
        .catch((error: unknown) => error);
    });
    expect(chatFailureOf(failure).kind).toBe("stale");
    expect(store.chat.groups.has(team.id)).toBe(true);
  });
});

describe("channel reorder hook (UC-CHT-A04)", () => {
  it("moves a channel into a category at once and the server keeps it there", async () => {
    const { hooks, store, queryClient, wrapper } = setup();
    const { result } = renderHook(
      () => ({
        channels: hooks.useChannels(WORKSPACE),
        reorder: hooks.useReorderChannels(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.channels.isSuccess).toBe(true));
    const [moved] = result.current.channels.data!.filter(
      (c) => c.groupId === null && c.role === "manager" && !c.isArchived,
    );
    const team = [...store.chat.groups.values()].find(
      (g) => g.name === "Team",
    )!;

    await act(async () => {
      await result.current.reorder.mutateAsync([
        {
          channelId: moved!.id,
          groupId: team.id,
          position: 99,
          expectedVersion: moved!.version,
        },
      ]);
    });

    expect(store.chat.spaces.get(moved!.id)).toMatchObject({
      groupId: team.id,
      position: 99,
    });
    await waitFor(() =>
      expect(
        queryClient
          .getQueryData<ChatChannel[]>(chatQueryKeys.channels(WORKSPACE))!
          .find((c) => c.id === moved!.id),
      ).toMatchObject({ groupId: team.id, position: 99 }),
    );
  });

  it("a stale placement refuses the whole request and the sidebar goes back to the server order", async () => {
    const { hooks, store, queryClient, wrapper } = setup();
    const { result } = renderHook(
      () => ({
        channels: hooks.useChannels(WORKSPACE),
        reorder: hooks.useReorderChannels(WORKSPACE),
      }),
      { wrapper },
    );
    await waitFor(() => expect(result.current.channels.isSuccess).toBe(true));
    const [moved] = result.current.channels.data!.filter(
      (c) => c.groupId === null && c.role === "manager" && !c.isArchived,
    );
    const before = store.chat.spaces.get(moved!.id)!.position;

    let failure: unknown;
    await act(async () => {
      failure = await result.current.reorder
        .mutateAsync([
          {
            channelId: moved!.id,
            groupId: null,
            position: before + 50,
            expectedVersion: moved!.version + 9,
          },
        ])
        .catch((error: unknown) => error);
    });

    expect(chatFailureOf(failure).kind).toBe("stale");
    expect(store.chat.spaces.get(moved!.id)!.position).toBe(before);
    await waitFor(() =>
      expect(
        queryClient
          .getQueryData<ChatChannel[]>(chatQueryKeys.channels(WORKSPACE))!
          .find((c) => c.id === moved!.id)?.position,
      ).toBe(before),
    );
  });

  it("is refused for a person who manages no channel as forbidden", async () => {
    const { hooks } = setup("member");
    const channels = await hooks.service.listChannels(WORKSPACE);
    const row = channels.find((c) => c.role !== "manager")!;
    const failure = await failureOf(() =>
      hooks.service.reorderChannels(WORKSPACE, [
        {
          channelId: row.id,
          groupId: null,
          position: 5,
          expectedVersion: row.version,
        },
      ]),
    );
    expect(failure.kind).toBe("forbidden");
  });
});
