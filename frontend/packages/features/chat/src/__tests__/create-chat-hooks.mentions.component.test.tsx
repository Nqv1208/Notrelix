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
import { createChatService } from "../core/api/chat.service";
import { chatFailureOf } from "../core/api/chat-failure";
import { createChatHooks } from "../web/query/create-chat-hooks";

const WORKSPACE = mockIds.workspaces.primary;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const hooks = createChatHooks(deps);
  const service = createChatService(deps.api, deps.endpoints);
  const viewer = store.getCurrentUser().id;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return { hooks, service, viewer, wrapper };
}

async function generalId(service: ReturnType<typeof setup>["service"]) {
  const channels = await service.listChannels(WORKSPACE);
  const found = channels.find((channel) => channel.name === "general");
  if (!found) throw new Error("no general channel");
  return found.id;
}

describe("useMentionables", () => {
  it("loads the people of the space, the broadcast permission and the confirmation threshold", async () => {
    const { hooks, service, viewer, wrapper } = setup();
    const general = await generalId(service);
    const { result } = renderHook(
      () => hooks.useMentionables(WORKSPACE, general),
      { wrapper },
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    const data = result.current.data!;
    expect(data.people.some((person) => person.userId === viewer)).toBe(true);
    expect(
      data.people.every((person) => typeof person.inSpace === "boolean"),
    ).toBe(true);
    expect(data.broadcastConfirmAbove).toBe(50);
    expect(data.memberCount).toBeGreaterThan(1);
    expect(typeof data.canBroadcast).toBe("boolean");
  });

  it("stays idle without a space or when disabled", () => {
    const { hooks, wrapper } = setup();
    const none = renderHook(() => hooks.useMentionables(WORKSPACE, undefined), {
      wrapper,
    });
    expect(none.result.current.fetchStatus).toBe("idle");
    const off = renderHook(() => hooks.useMentionables(WORKSPACE, "s", false), {
      wrapper,
    });
    expect(off.result.current.fetchStatus).toBe("idle");
  });

  it("is refused for a space the caller is not in, as a missing space", async () => {
    const { service } = setup();
    await expect(
      service.listMentionables(WORKSPACE, "missing-space"),
    ).rejects.toMatchObject({ status: 404 });
  });
});

describe("a send with mentions", () => {
  it("stores the person's mention, returns it on the message and keeps it in the pending retry", async () => {
    const { hooks, service, viewer, wrapper } = setup();
    const general = await generalId(service);
    const picker = await service.listMentionables(WORKSPACE, general);
    const target = picker.people.find(
      (person) => person.inSpace && person.userId !== viewer,
    )!;
    const content = "hello @Colleague";
    const { result } = renderHook(
      () => hooks.useSendMessage(WORKSPACE, general, viewer),
      { wrapper },
    );
    await act(async () => {
      result.current.send(content, null, {
        mentions: [
          { kind: "user", start: 6, length: 10, userId: target.userId },
        ],
      });
    });
    const page = await service.listMessages(WORKSPACE, general, {}, 5);
    const stored = page.items.find((message) => message.content === content);
    expect(stored?.mentions).toEqual([
      {
        kind: "user",
        start: 6,
        length: 10,
        userId: target.userId,
        spaceId: null,
      },
    ]);
  });

  it("answers a mention of someone outside the space with a cannot-view notice", async () => {
    const { service } = setup();
    // A new private channel holds only its creator: every colleague is outside it.
    const room = await service.createChannel(
      WORKSPACE,
      { name: "mention-room", isPrivate: true },
      crypto.randomUUID(),
    );
    const picker = await service.listMentionables(WORKSPACE, room.id);
    const outside = picker.people.find((person) => !person.inSpace)!;
    expect(outside).toBeDefined();
    const sent = await service.sendMessage(WORKSPACE, room.id, {
      content: "@Outsider hi",
      clientMessageId: crypto.randomUUID(),
      mentions: [{ kind: "user", start: 0, length: 9, userId: outside.userId }],
    });
    expect(sent.mentionNotices?.map((notice) => notice.code)).toEqual([
      "cannot-view",
    ]);
    expect(sent.mentions).toHaveLength(1);
  });

  it("keeps the mentions an edit sends again, with their new positions", async () => {
    const { hooks, service, viewer, wrapper } = setup();
    const general = await generalId(service);
    const picker = await service.listMentionables(WORKSPACE, general);
    const target = picker.people.find(
      (person) => person.inSpace && person.userId !== viewer,
    )!;
    const sent = await service.sendMessage(WORKSPACE, general, {
      content: "@Colleague hi",
      clientMessageId: crypto.randomUUID(),
      mentions: [{ kind: "user", start: 0, length: 10, userId: target.userId }],
    });
    const { result } = renderHook(
      () => hooks.useEditMessage(WORKSPACE, general),
      {
        wrapper,
      },
    );
    let edited!: Awaited<ReturnType<typeof service.editMessage>>;
    await act(async () => {
      edited = await result.current.mutateAsync({
        messageId: sent.id,
        content: "Hey @Colleague hi",
        expectedVersion: sent.version,
        mentions: [
          { kind: "user", start: 4, length: 10, userId: target.userId },
        ],
      });
    });
    expect(edited.mentions?.map((m) => [m.userId, m.start])).toEqual([
      [target.userId, 4],
    ]);
  });

  it("classifies the broadcast confirmation refusal", () => {
    expect(
      chatFailureOf({ status: 409, code: "broadcast-confirmation-required" })
        .kind,
    ).toBe("broadcast-confirmation");
  });
});
