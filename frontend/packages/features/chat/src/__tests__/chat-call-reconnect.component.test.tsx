import type { ReactNode } from "react";
import { act, render, renderHook, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { createChatRealtimeAdapter } from "../web/realtime/chat-realtime-adapter";
import { createChatService } from "../core/api/chat.service";
import {
  CALL_RECONNECT_LIMIT_MS,
  type ChatCallMediaConnector,
  type ChatCallMediaOptions,
} from "../web/calls/chat-call-media";
import { useChatCalls } from "../web/calls/use-chat-calls";
import type {
  ChatCallsController,
  ChatCurrentCall,
} from "../web/calls/use-chat-calls";
import { ChatCallStage, ChatCallStrip } from "../web/calls/chat-call-surfaces";
import { en, vi as viCopy } from "../web/components/chat.copy";
import type { ChatCall } from "../core/types/call";

const WS = mockIds.workspaces.primary;
const AT = "2026-11-17T04:00:00.000Z";

type MockCallHook = {
  act: (call: string, person: string, action: string) => boolean;
};
const mockCall = () =>
  (globalThis as unknown as { __notrelixMockCall: MockCallHook })
    .__notrelixMockCall;

/** A media plane that never touches a network: the test says when it drops, retries and recovers. */
function fakeMedia() {
  const connects: ChatCallMediaOptions[] = [];
  const disconnects = vi.fn();
  const connector: ChatCallMediaConnector = async (options) => {
    connects.push(options);
    queueMicrotask(() => options.onState("connected"));
    return {
      setMicrophone: async () => null,
      setCamera: async () => null,
      disconnect: disconnects,
    };
  };
  return {
    connector,
    connects,
    disconnects,
    latest: () => connects[connects.length - 1]!,
  };
}

async function setup() {
  const media = fakeMedia();
  const store = new MockStore({ ...defaultConfig });
  const posts: string[] = [];
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => {
      if (init?.method === "POST") posts.push(String(input));
      return createMockFetch(store)(input, init);
    },
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const viewerId = store.getCurrentUser().id;
  const other = store
    .getWorkspaceMembers(WS)
    .map((m) => m.userId)
    .find((id) => id !== viewerId)!;
  const service = createChatService(deps.api, deps.endpoints);
  const dm = await service.startConversation(
    WS,
    { kind: "direct", userId: other },
    "dm-key-calls-reconnect",
  );
  const spaceId =
    (dm as unknown as { conversation: { id: string } }).conversation?.id ??
    (dm as unknown as { id: string }).id;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const adapter = createChatRealtimeAdapter({
    queryClient,
    service,
    viewerId,
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  const rendered = renderHook(
    () => useChatCalls({ deps, workspaceId: WS, connector: media.connector }),
    { wrapper },
  );
  const frame = async (callId: string) => {
    const call = store.chat.calls.get(callId)!;
    await adapter.validateAndHandle(
      {
        eventType: "chat.call.changed",
        eventId: `evt-${Math.random()}`,
        resourceId: spaceId,
        workspaceId: WS,
        timestamp: AT,
        payload: {
          callId,
          spaceId,
          state: call.state,
          kind: call.kind,
          initiatorId: call.initiatorId,
          ...(call.endReason ? { endReason: call.endReason } : {}),
          participants: [],
          participantsOmitted: false,
          version: call.version,
        },
      },
      { workspaceId: WS, invalidateQueries: vi.fn() },
    );
  };
  const current = () => rendered.result.current.current;
  const ctl = () => rendered.result.current;

  // A connected one-to-one call: rung out, answered by the other person, media joined.
  act(() => ctl().start(spaceId, "voice"));
  await waitFor(() => expect(current()?.call.state).toBe("ringing"));
  const callId = current()!.call.id;
  mockCall().act(callId, other, "answer");
  await act(() => frame(callId));
  await waitFor(() => {
    expect(current()?.call.state).toBe("active");
    expect(current()?.media).toBe("connected");
  });
  return { media, store, other, spaceId, callId, frame, posts, current, ctl };
}

afterEach(() => {
  vi.useRealTimers();
  window.dispatchEvent(new Event("online"));
});

describe("a call whose connection drops", () => {
  it("shows reconnecting with the try number, then recovers by itself and never gives up", async () => {
    const t = await setup();
    vi.useFakeTimers();

    act(() => t.media.latest().onState("reconnecting"));
    expect(t.current()?.media).toBe("reconnecting");
    expect(t.current()?.attempt).toBeNull();
    act(() => t.media.latest().onAttempt?.(2));
    expect(t.current()?.attempt).toBe(2);

    act(() => vi.advanceTimersByTime(CALL_RECONNECT_LIMIT_MS - 1_000));
    act(() => t.media.latest().onState("connected"));
    expect(t.current()?.media).toBe("connected");
    expect(t.current()?.attempt).toBeNull();

    // The give-up timer went with the recovery: waiting past the limit changes nothing.
    act(() => vi.advanceTimersByTime(CALL_RECONNECT_LIMIT_MS * 2));
    expect(t.current()?.media).toBe("connected");
    expect(t.current()?.call.state).toBe("active");
    expect(t.media.disconnects).not.toHaveBeenCalled();
  });

  it("shows reconnecting while the browser is offline and goes back to connected when it is online again", async () => {
    const t = await setup();
    vi.useFakeTimers();

    act(() => {
      window.dispatchEvent(new Event("offline"));
    });
    expect(t.current()?.media).toBe("reconnecting");

    act(() => vi.advanceTimersByTime(5_000));
    act(() => {
      window.dispatchEvent(new Event("online"));
    });
    // The media plane never dropped: it is connected again without waiting for it to say so.
    expect(t.current()?.media).toBe("connected");
    act(() => vi.advanceTimersByTime(CALL_RECONNECT_LIMIT_MS * 2));
    expect(t.current()?.media).toBe("connected");
    expect(t.media.disconnects).not.toHaveBeenCalled();
  });

  it("gives up after the limit: the media session is closed once and the person can retry or leave", async () => {
    const t = await setup();
    vi.useFakeTimers();

    act(() => t.media.latest().onState("reconnecting"));
    act(() => vi.advanceTimersByTime(CALL_RECONNECT_LIMIT_MS - 1));
    expect(t.current()?.media).toBe("reconnecting");
    act(() => vi.advanceTimersByTime(1));
    expect(t.current()?.media).toBe("lost");
    expect(t.media.disconnects).toHaveBeenCalledTimes(1);
    // The call itself is not ended by the client: the media server's own leave event and the sweep settle it.
    expect(t.current()?.call.state).toBe("active");
    expect(t.posts.filter((p) => p.endsWith("/end"))).toHaveLength(0);
    // A late report of the closed session changes nothing.
    act(() => t.media.connects[0]!.onState("connected"));
    expect(t.current()?.media).toBe("lost");
  });

  it("gives up when the browser stays offline for the whole limit", async () => {
    const t = await setup();
    vi.useFakeTimers();

    act(() => {
      window.dispatchEvent(new Event("offline"));
    });
    act(() => vi.advanceTimersByTime(CALL_RECONNECT_LIMIT_MS));
    expect(t.current()?.media).toBe("lost");
    expect(t.media.disconnects).toHaveBeenCalledTimes(1);
  });

  it("treats a room that closes under an active call as lost at once", async () => {
    const t = await setup();
    act(() => t.media.latest().onState("disconnected"));
    expect(t.current()?.media).toBe("lost");
    expect(t.current()?.call.state).toBe("active");
  });

  it("retries after a give-up with a fresh token and is connected again", async () => {
    const t = await setup();
    act(() => t.media.latest().onState("disconnected"));
    expect(t.current()?.media).toBe("lost");

    act(() => t.ctl().retryConnect());
    expect(t.current()?.media).toBe("reconnecting");
    await waitFor(() => expect(t.media.connects).toHaveLength(2));
    await waitFor(() => expect(t.current()?.media).toBe("connected"));
    expect(t.media.connects[1]!.token).toMatch(/^mock-token-/);
    expect(t.current()?.call.state).toBe("active");
  });

  it("shows the result when the call ended while the person was offline, and a retry finds it ended", async () => {
    const t = await setup();
    act(() => t.media.latest().onState("disconnected"));
    mockCall().act(t.callId, t.other, "end");

    act(() => t.ctl().retryConnect());
    await waitFor(() => expect(t.current()?.call.state).toBe("ended"));
    expect(t.current()?.call.endReason).toBe("completed");
    expect(t.current()?.media).toBe("idle");
    // No second connection was opened for a call that is over.
    expect(t.media.connects).toHaveLength(1);
  });

  it("leaves once however often Leave is pressed while the connection is down", async () => {
    const t = await setup();
    act(() => t.media.latest().onState("reconnecting"));

    act(() => {
      t.ctl().hangUp();
      t.ctl().hangUp();
      t.ctl().hangUp();
    });
    await waitFor(() => expect(t.current()?.call.state).toBe("ended"));
    expect(t.posts.filter((p) => p.endsWith("/end"))).toHaveLength(1);
    expect(t.media.disconnects).toHaveBeenCalledTimes(1);
  });
});

function call(over: Partial<ChatCall> = {}): ChatCall {
  return {
    id: "call-1",
    spaceId: "space-1",
    kind: "voice",
    state: "active",
    endReason: null,
    initiatorId: "quan",
    startedAt: AT,
    answeredAt: AT,
    endedAt: null,
    region: "ap-southeast-1",
    version: 3,
    participants: [],
    ...over,
  };
}

function held(over: Partial<ChatCurrentCall> = {}): ChatCurrentCall {
  return {
    call: call(),
    media: "reconnecting",
    attempt: null,
    audioOn: true,
    videoOn: false,
    deviceNotice: null,
    ...over,
  };
}

function controller(current: ChatCurrentCall): ChatCallsController {
  return {
    enabled: true,
    current,
    incoming: [],
    error: null,
    start: vi.fn(),
    answer: vi.fn(),
    decline: vi.fn(),
    hangUp: vi.fn(),
    toggleMicrophone: vi.fn(),
    toggleCamera: vi.fn(),
    retryConnect: vi.fn(),
    dismiss: vi.fn(),
    remoteVideoRef: vi.fn(),
    localVideoRef: vi.fn(),
  };
}

const people = {
  viewerId: "me",
  nameOf: (id: string) => (id === "quan" ? "Trần Minh Quân" : "Thu Hà"),
  peerOf: () => "quan",
};

function stage(c: ChatCallsController, copy = en) {
  return render(
    <ChatCallStage
      controller={c}
      held={c.current!}
      visible
      onMinimize={vi.fn()}
      copy={copy}
      {...people}
    />,
  );
}

describe("the call screen while the connection is down", () => {
  it("shows reconnecting with the try number and Leave, and nothing under it can be reached", () => {
    const c = controller(held({ attempt: 2 }));
    stage(c);
    const overlay = screen.getByTestId("chat-call-reconnect");
    expect(overlay.getAttribute("data-state")).toBe("reconnecting");
    expect(overlay.textContent).toContain("Reconnecting…");
    expect(overlay.textContent).toContain("Try 2 of 5");
    expect(screen.queryByRole("button", { name: en.calls.retry })).toBeNull();
    // The mute and hang-up controls are inert under the overlay.
    expect(screen.getByTestId("chat-call-controls").hasAttribute("inert")).toBe(
      true,
    );
    screen.getByRole("button", { name: en.calls.leaveCall }).click();
    expect(c.hangUp).toHaveBeenCalledTimes(1);
  });

  it("says only to wait when the try number is not known (offline)", () => {
    stage(controller(held({ attempt: null })));
    const text = screen.getByTestId("chat-call-reconnect").textContent ?? "";
    expect(text).toContain(en.calls.reconnectWait);
    expect(text).not.toContain("Try ");
  });

  it("shows could-not-reconnect with Try again and Leave once the limit passed", () => {
    const c = controller(held({ media: "lost" }));
    stage(c);
    const overlay = screen.getByTestId("chat-call-reconnect");
    expect(overlay.getAttribute("data-state")).toBe("lost");
    expect(screen.getByRole("alert").textContent).toContain(en.calls.lostTitle);
    expect(overlay.textContent).toContain(en.calls.lostBody);
    screen.getByRole("button", { name: en.calls.retry }).click();
    expect(c.retryConnect).toHaveBeenCalledTimes(1);
    screen.getByRole("button", { name: en.calls.leaveCall }).click();
    expect(c.hangUp).toHaveBeenCalledTimes(1);
  });

  it("shows nothing of it while connected or once the call has ended", () => {
    const { rerender } = stage(controller(held({ media: "connected" })));
    expect(screen.queryByTestId("chat-call-reconnect")).toBeNull();
    const endedHeld = held({
      media: "idle",
      call: call({ state: "ended", endReason: "completed", endedAt: AT }),
    });
    rerender(
      <ChatCallStage
        controller={controller(endedHeld)}
        held={endedHeld}
        visible
        onMinimize={vi.fn()}
        copy={en}
        {...people}
      />,
    );
    expect(screen.queryByTestId("chat-call-reconnect")).toBeNull();
  });

  it("speaks Vietnamese", () => {
    stage(controller(held({ attempt: 2 })), viCopy as unknown as typeof en);
    const text = screen.getByTestId("chat-call-reconnect").textContent ?? "";
    expect(text).toContain("Đang kết nối lại…");
    expect(text).toContain("Lần thử 2/5");
    expect(text).toContain("Rời cuộc gọi");
  });

  it("names the state in the minimized bar", () => {
    const c = controller(held({ media: "lost" }));
    render(
      <ChatCallStrip
        controller={c}
        held={c.current!}
        onReturn={vi.fn()}
        copy={en}
        {...people}
      />,
    );
    expect(screen.getByTestId("chat-call-strip").textContent).toContain(
      en.calls.lostShort,
    );
  });
});
