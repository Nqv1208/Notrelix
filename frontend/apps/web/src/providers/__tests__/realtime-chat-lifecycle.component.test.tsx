import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RealtimeClient, type WebSocketLike } from "@notrelix/realtime";
import { chatQueryKeys } from "@notrelix/features-chat";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const state = vi.hoisted(() => ({
  pathname: "/workspaces/ws-a/chat/space-1",
  isAuthenticated: true,
  sessionGeneration: "user:1" as string | null,
  runtime: null as unknown,
}));

vi.mock("@tanstack/react-router", () => ({
  useLocation: () => ({ pathname: state.pathname }),
}));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => state.runtime,
}));
vi.mock("@notrelix/features-auth", () => ({
  useAuth: () => ({
    isAuthenticated: state.isAuthenticated,
    sessionGeneration: state.sessionGeneration,
    user: state.isAuthenticated ? { id: "1" } : null,
  }),
}));
vi.mock("@notrelix/features-workspace", () => ({
  workspaceRealtimeAdapter: {
    id: "workspace-adapter",
    supports: (envelope: { eventType: string }) =>
      envelope.eventType.startsWith("workspace."),
    validateAndHandle: async () => {},
  },
}));

import { RealtimeLifecycle } from "../realtime-lifecycle";

const WS = "ws-a";
const CH = "space-1";

function createSocket() {
  const socket: WebSocketLike & {
    sentFrames: () => Array<Record<string, unknown>>;
  } = {
    readyState: 1,
    sentFrames: () => sent,
    send: (data: string) => {
      sent.push(JSON.parse(data) as Record<string, unknown>);
    },
    close: vi.fn(),
    onopen: null,
    onmessage: null,
    onclose: null,
    onerror: null,
  };
  const sent: Array<Record<string, unknown>> = [];
  return socket;
}

function frame(body: Record<string, unknown>) {
  return { data: JSON.stringify(body) };
}

function channelSubscribed(channelId: string) {
  return frame({
    type: "subscribed",
    subscriptionId: `channel:${channelId}`,
    scope: { kind: "channel", id: channelId },
    streamKey: `chat:Channel:${channelId}`,
  });
}

function userSubscribed(userId: string) {
  return frame({
    type: "subscribed",
    subscriptionId: `user:${userId}`,
    scope: { kind: "user", id: userId },
    streamKey: `user:notifications:${userId}`,
  });
}

function messageFrame(
  channelId: string,
  eventId: string,
  streamVersion: number,
  payload: Record<string, unknown>,
) {
  return frame({
    type: "event",
    subscriptionId: `channel:${channelId}`,
    schemaVersion: 1,
    eventId,
    eventType: "chat.message.created",
    changeKind: "chat.message.created",
    streamKey: `chat:Channel:${channelId}`,
    streamVersion,
    resourceKind: "Channel",
    resourceId: channelId,
    workspaceId: WS,
    correlationId: eventId,
    occurredAt: "2026-11-10T04:00:00.000Z",
    payloadContract: "chat.message.v1",
    payload,
  });
}

function unreadFrame(
  userId: string,
  eventId: string,
  streamVersion: number,
  payload: Record<string, unknown>,
) {
  return frame({
    type: "event",
    subscriptionId: `user:${userId}`,
    schemaVersion: 1,
    eventId,
    eventType: "chat.unread.changed",
    changeKind: "chat.unread.changed",
    streamKey: `user:notifications:${userId}`,
    streamVersion,
    resourceKind: "notifications",
    resourceId: userId,
    workspaceId: WS,
    correlationId: eventId,
    occurredAt: "2026-11-10T04:00:00.000Z",
    payloadContract: "chat.unread.v1",
    payload,
  });
}

let root: Root | null = null;
let host: HTMLDivElement | null = null;
let sockets: ReturnType<typeof createSocket>[] = [];
let queryClient: QueryClient;
let realtime: RealtimeClient;
const telemetry = { track: vi.fn(), reportError: vi.fn() };

/** Endpoints of the chat service; the lifecycle only reads them when a backfill runs. */
const chatEndpoints = new Proxy({}, { get: () => () => "/chat" });
const restApi = {
  get: vi.fn(async () => ({ items: [], hasOlder: false, hasNewer: false })),
};

async function render() {
  await act(async () => {
    root?.render(
      <QueryClientProvider client={queryClient}>
        <RealtimeLifecycle>
          <span>child</span>
        </RealtimeLifecycle>
      </QueryClientProvider>,
    );
  });
}

async function flush() {
  await act(async () => {
    for (let i = 0; i < 6; i++) await Promise.resolve();
  });
}

const messagesKey = [...chatQueryKeys.messages(WS, CH), "latest"];

beforeEach(async () => {
  state.pathname = `/workspaces/${WS}/chat/${CH}`;
  state.isAuthenticated = true;
  state.sessionGeneration = "user:1";
  sockets = [];
  telemetry.track.mockClear();
  telemetry.reportError.mockClear();
  restApi.get.mockClear();
  queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  queryClient.setQueryData(messagesKey, {
    pageParams: [{}],
    pages: [
      {
        items: [
          {
            id: "m-1",
            spaceId: CH,
            sequence: 1,
            kind: "user",
            authorId: "u2",
            content: "one",
            clientMessageId: null,
            replyToId: null,
            edited: false,
            deleted: false,
            createdAt: "2026-11-10T03:00:00.000Z",
            editedAt: null,
            deletedAt: null,
            version: 1,
          },
        ],
        hasOlder: false,
        hasNewer: false,
      },
    ],
  });
  realtime = new RealtimeClient("wss://ws.test/api/v1/realtime", {
    socketFactory: () => {
      const socket = createSocket();
      sockets.push(socket);
      return socket;
    },
    telemetry,
  });
  state.runtime = {
    realtime,
    telemetry,
    api: { api: restApi, endpoints: { chat: chatEndpoints } },
  };
  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await render();
  await flush();
});

afterEach(() => {
  act(() => root?.unmount());
  realtime.dispose();
  host?.remove();
  root = null;
  host = null;
});

async function openSocket(index: number) {
  const socket = sockets[index];
  if (!socket) throw new Error(`no socket ${index}`);
  await act(async () => {
    socket.onopen?.({});
    socket.onmessage?.(
      frame({ type: "ready", schemaVersion: 1, connectionId: "c" }),
    );
  });
  return socket;
}

function subscribeFrames(socket: ReturnType<typeof createSocket>) {
  return socket
    .sentFrames()
    .filter((f) => f.type === "subscribe")
    .map((f) => f.subscriptionId);
}

describe("RealtimeLifecycle: Chat streams (UC-CHT-C01, C02, C06, C09)", () => {
  it("subscribes only the open channel and the caller's per-user stream", async () => {
    const socket = await openSocket(0);
    expect(subscribeFrames(socket).sort()).toEqual([
      "channel:space-1",
      "user:1",
      "workspace:ws-a",
    ]);
  });

  it("switching channels unsubscribes the old one and subscribes the new one; leaving Chat releases it", async () => {
    const socket = await openSocket(0);
    state.pathname = `/workspaces/${WS}/chat/space-2`;
    await render();
    await flush();
    const frames = socket.sentFrames();
    expect(frames).toContainEqual({
      type: "unsubscribe",
      subscriptionId: "channel:space-1",
    });
    expect(frames).toContainEqual({
      type: "subscribe",
      subscriptionId: "channel:space-2",
      scope: { kind: "channel", id: "space-2" },
    });
    expect(subscribeFrames(socket)).not.toContain("channel:space-3");

    state.pathname = `/workspaces/${WS}/boards`;
    await render();
    await flush();
    expect(socket.sentFrames()).toContainEqual({
      type: "unsubscribe",
      subscriptionId: "channel:space-2",
    });
  });

  it("a new message of the open channel reaches the history cache once, in order", async () => {
    const socket = await openSocket(0);
    await act(async () => {
      socket.onmessage?.(channelSubscribed(CH));
      socket.onmessage?.(
        messageFrame(CH, "e-2", 1, {
          id: "m-2",
          sequence: 2,
          authorId: "u2",
          kind: "user",
          content: "two",
          contentOmitted: false,
          deleted: false,
          createdAt: "2026-11-10T04:00:00.000Z",
          editedAt: null,
          replyToId: null,
          version: 1,
        }),
      );
      // Duplicate delivery of the same event is dropped by the transport; the cache holds it once.
      socket.onmessage?.(
        messageFrame(CH, "e-2", 1, {
          id: "m-2",
          sequence: 2,
          authorId: "u2",
          kind: "user",
          content: "two",
          contentOmitted: false,
          deleted: false,
          createdAt: "2026-11-10T04:00:00.000Z",
          editedAt: null,
          replyToId: null,
          version: 1,
        }),
      );
    });
    await flush();
    const cache = queryClient.getQueryData<{
      pages: { items: { id: string }[] }[];
    }>(messagesKey);
    expect(cache?.pages[0]!.items.map((m) => m.id)).toEqual(["m-1", "m-2"]);
  });

  it("a frame on the per-user stream updates the unread summary in place", async () => {
    const unreadKey = chatQueryKeys.unread(WS);
    queryClient.setQueryData(unreadKey, {
      spaces: [
        {
          spaceId: "space-9",
          name: "other",
          readSequence: 0,
          lastSequence: 3,
          unreadCount: 0,
          firstUnreadSequence: null,
          mentionCount: 0,
          notificationLevel: "all",
          muted: false,
        },
      ],
      totalUnread: 0,
      totalMentions: 0,
    });
    const socket = await openSocket(0);
    await act(async () => {
      socket.onmessage?.(userSubscribed("1"));
      socket.onmessage?.(
        unreadFrame("1", "u-1", 1, {
          spaceId: "space-9",
          unread: 2,
          mentions: 1,
        }),
      );
    });
    await flush();
    expect(queryClient.getQueryData(unreadKey)).toMatchObject({
      totalUnread: 2,
      totalMentions: 1,
    });
  });

  it("access revoked for the open channel: the channel leaves the sidebar, no retry, and a regained access resubscribes", async () => {
    queryClient.setQueryData(chatQueryKeys.channels(WS), [
      { id: CH },
      { id: "space-2" },
    ]);
    const socket = await openSocket(0);
    await act(async () => {
      socket.onmessage?.(channelSubscribed(CH));
      socket.onmessage?.(
        frame({
          type: "subscription-error",
          subscriptionId: `channel:${CH}`,
          code: "subscription_revoked",
        }),
      );
    });
    await flush();
    expect(queryClient.getQueryData(chatQueryKeys.channels(WS))).toEqual([
      { id: "space-2" },
    ]);
    expect(telemetry.track).toHaveBeenCalledWith(
      "realtime.permission_lost",
      expect.objectContaining({ workspaceId: WS }),
    );
    // The denied subscription is not retried by the transport.
    const subscribesOfChannel = subscribeFrames(socket).filter(
      (id) => id === `channel:${CH}`,
    );
    expect(subscribesOfChannel).toHaveLength(1);

    // The person's access changes back: the host subscribes the open channel again.
    await act(async () => {
      socket.onmessage?.(frame({ type: "access-changed", workspaceId: WS }));
    });
    await flush();
    expect(
      subscribeFrames(socket).filter((id) => id === `channel:${CH}`).length,
    ).toBeGreaterThanOrEqual(2);
  });
});
