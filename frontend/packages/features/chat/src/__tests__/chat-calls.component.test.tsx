import type { ReactNode } from "react";
import { act, render, renderHook, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { createChatRealtimeAdapter } from "../web/realtime/chat-realtime-adapter";
import { createChatService } from "../core/api/chat.service";
import { useChatCalls } from "../web/calls/use-chat-calls";
import {
  ChatCallIncoming,
  ChatCallStage,
} from "../web/calls/chat-call-surfaces";
import type { ChatCallMediaConnector } from "../web/calls/chat-call-media";
import { systemTextOf } from "../web/components/use-chat-copy";
import { en, vi as viCopy } from "../web/components/chat.copy";
import type { ChatCall } from "../core/types/call";
import type {
  ChatCallsController,
  ChatCurrentCall,
} from "../web/calls/use-chat-calls";

const WS = mockIds.workspaces.primary;
const AT = "2026-11-17T04:00:00.000Z";

type MockCallHook = {
  ring: (space: string, person: string, kind: "voice" | "video") => string;
  act: (call: string, person: string, action: string) => boolean;
  miss: (call: string) => boolean;
};
const hook = () =>
  (globalThis as unknown as { __notrelixMockCall: MockCallHook })
    .__notrelixMockCall;

const okConnector: ChatCallMediaConnector = async (options) => {
  queueMicrotask(() => options.onState("connected"));
  return {
    setMicrophone: async () => null,
    setCamera: async () => null,
    disconnect: () => undefined,
  };
};

async function setup(connector: ChatCallMediaConnector = okConnector) {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
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
    "dm-key-calls-ui",
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
    () => useChatCalls({ deps, workspaceId: WS, connector }),
    { wrapper },
  );
  /** Delivers the call frame the server would send (the version the mock holds now). */
  const frame = async (callId: string, userScope = false) => {
    const call = store.chat.calls.get(callId)!;
    await adapter.validateAndHandle(
      {
        eventType: "chat.call.changed",
        eventId: `evt-${Math.random()}`,
        resourceId: userScope ? viewerId : spaceId,
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
  return { store, other, viewerId, spaceId, rendered, frame, queryClient };
}

describe("useChatCalls: the one-to-one call from both ends", () => {
  it("rings out, connects when the other person answers, and ends for both", async () => {
    const t = await setup();
    act(() => t.rendered.result.current.start(t.spaceId, "voice"));
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call.state).toBe("ringing"),
    );
    const id = t.rendered.result.current.current!.call.id;

    hook().act(id, t.other, "answer");
    await act(() => t.frame(id));
    await waitFor(() => {
      expect(t.rendered.result.current.current?.call.state).toBe("active");
      expect(t.rendered.result.current.current?.media).toBe("connected");
    });

    act(() => t.rendered.result.current.hangUp());
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call).toMatchObject({
        state: "ended",
        endReason: "completed",
      }),
    );
    expect(t.store.chat.calls.get(id)?.state).toBe("ended");
  });

  it("shows declined and missed from the end frame, and cancel while ringing", async () => {
    const t = await setup();
    act(() => t.rendered.result.current.start(t.spaceId, "voice"));
    await waitFor(() =>
      expect(t.rendered.result.current.current).not.toBeNull(),
    );
    const id = t.rendered.result.current.current!.call.id;
    hook().act(id, t.other, "decline");
    await act(() => t.frame(id));
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call.endReason).toBe(
        "declined",
      ),
    );

    act(() => t.rendered.result.current.dismiss());
    act(() => t.rendered.result.current.start(t.spaceId, "voice"));
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call.state).toBe("ringing"),
    );
    const second = t.rendered.result.current.current!.call.id;
    hook().miss(second);
    await act(() => t.frame(second));
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call.endReason).toBe("missed"),
    );

    act(() => t.rendered.result.current.dismiss());
    act(() => t.rendered.result.current.start(t.spaceId, "voice"));
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call.state).toBe("ringing"),
    );
    act(() => t.rendered.result.current.hangUp());
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call.endReason).toBe(
        "cancelled",
      ),
    );
  });

  it("answers an incoming ring from the per-person frame and declines another", async () => {
    const t = await setup();
    const id = hook().ring(t.spaceId, t.other, "video");
    await act(() => t.frame(id, true));
    await waitFor(() =>
      expect(t.rendered.result.current.incoming).toHaveLength(1),
    );
    expect(t.rendered.result.current.incoming[0]).toMatchObject({
      kind: "video",
      initiatorId: t.other,
    });
    act(() => t.rendered.result.current.answer(id));
    await waitFor(() => {
      expect(t.rendered.result.current.current?.call.state).toBe("active");
      expect(t.rendered.result.current.incoming).toHaveLength(0);
    });
    act(() => t.rendered.result.current.hangUp());
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call.state).toBe("ended"),
    );
  });

  it("drops the ring when the caller cancels, and declines through the server", async () => {
    const t = await setup();
    const id = hook().ring(t.spaceId, t.other, "voice");
    await act(() => t.frame(id, true));
    await waitFor(() =>
      expect(t.rendered.result.current.incoming).toHaveLength(1),
    );
    hook().act(id, t.other, "cancel");
    await act(() => t.frame(id, true));
    await waitFor(() =>
      expect(t.rendered.result.current.incoming).toHaveLength(0),
    );

    const next = hook().ring(t.spaceId, t.other, "voice");
    await act(() => t.frame(next, true));
    await waitFor(() =>
      expect(t.rendered.result.current.incoming).toHaveLength(1),
    );
    act(() => t.rendered.result.current.decline(next));
    await waitFor(() =>
      expect(t.store.chat.calls.get(next)?.endReason).toBe("declined"),
    );
    expect(t.rendered.result.current.incoming).toHaveLength(0);
  });

  it("stores a busy call as ended when the other person is in a call, and refuses a second start", async () => {
    const t = await setup();
    // The other person is busy in a channel call of their own.
    const channel = Array.from(t.store.chat.spaces.values()).find(
      (space) =>
        space.kind === "channel_public" &&
        t.store.chat.activeMembershipOf(space.id, t.other),
    )!;
    hook().ring(channel.id, t.other, "voice");
    act(() => t.rendered.result.current.start(t.spaceId, "voice"));
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call).toMatchObject({
        state: "ended",
        endReason: "busy",
      }),
    );
    expect(t.rendered.result.current.current?.media).toBe("idle");

    act(() => t.rendered.result.current.dismiss());
    hook().act(Array.from(t.store.chat.calls.keys())[0]!, t.other, "cancel");
    act(() => t.rendered.result.current.start(t.spaceId, "voice"));
    await waitFor(() =>
      expect(t.rendered.result.current.current?.call.state).toBe("ringing"),
    );
    act(() => t.rendered.result.current.start(t.spaceId, "voice"));
    expect(t.rendered.result.current.error).toBe("you-busy");
  });

  it("goes on without a device the browser refused, and offers a retry when the room cannot be joined", async () => {
    const denied: ChatCallMediaConnector = async (options) => {
      queueMicrotask(() => options.onState("connected"));
      return {
        setMicrophone: async () => "denied",
        setCamera: async () => null,
        disconnect: () => undefined,
      };
    };
    const t = await setup(denied);
    act(() => t.rendered.result.current.start(t.spaceId, "voice"));
    await waitFor(() =>
      expect(t.rendered.result.current.current).not.toBeNull(),
    );
    const id = t.rendered.result.current.current!.call.id;
    hook().act(id, t.other, "answer");
    await act(() => t.frame(id));
    await waitFor(() =>
      expect(t.rendered.result.current.current?.deviceNotice).toBe(
        "mic-denied",
      ),
    );
    expect(t.rendered.result.current.current?.audioOn).toBe(false);

    const failing: ChatCallMediaConnector = async () => {
      throw new Error("no route");
    };
    const f = await setup(failing);
    act(() => f.rendered.result.current.start(f.spaceId, "voice"));
    await waitFor(() =>
      expect(f.rendered.result.current.current?.media).toBe("failed"),
    );
  });
});

function call(over: Partial<ChatCall> = {}): ChatCall {
  return {
    id: "call-1",
    spaceId: "space-1",
    kind: "voice",
    state: "ringing",
    endReason: null,
    initiatorId: "me",
    startedAt: AT,
    answeredAt: null,
    endedAt: null,
    region: "ap-southeast-1",
    version: 1,
    participants: [
      {
        personId: "me",
        joinedAt: AT,
        leftAt: null,
        audioOn: true,
        videoOn: false,
        sharingScreen: false,
      },
    ],
    ...over,
  };
}

function held(over: Partial<ChatCurrentCall> = {}): ChatCurrentCall {
  return {
    call: call(),
    media: "connected",
    attempt: null,
    audioOn: true,
    videoOn: false,
    deviceNotice: null,
    ...over,
  };
}

function controller(
  over: Partial<ChatCallsController> = {},
): ChatCallsController {
  return {
    enabled: true,
    current: null,
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
    ...over,
  };
}

const NAMES: Record<string, string> = { me: "Thu Hà", quan: "Trần Minh Quân" };
const nameOf = (id: string) => NAMES[id] ?? "?";

function Surfaces({
  c,
  copy = en,
  isPhone = false,
}: {
  c: ChatCallsController;
  copy?: typeof en;
  isPhone?: boolean;
}) {
  return (
    <div>
      {c.current ? (
        <ChatCallStage
          controller={c}
          held={c.current}
          visible
          onMinimize={vi.fn()}
          viewerId="me"
          nameOf={nameOf}
          peerOf={() => "quan"}
          copy={copy}
        />
      ) : null}
      <ChatCallIncoming
        controller={c}
        nameOf={nameOf}
        isPhone={isPhone}
        copy={copy}
      />
    </div>
  );
}

function dock(c: ChatCallsController, copy = en, isPhone = false) {
  return render(<Surfaces c={c} copy={copy} isPhone={isPhone} />);
}

describe("the call surfaces: every state of a call", () => {
  it("renders nothing without a call, a ring or an error", () => {
    const { container } = dock(controller());
    expect(container.firstElementChild?.innerHTML).toBe("");
  });

  it("rings out with Cancel, the person named once and the controls ready", () => {
    const c = controller({
      current: held({ media: "connecting", call: call({ initiatorId: "me" }) }),
    });
    dock(c);
    expect(
      screen.getByTestId("chat-call-stage").getAttribute("data-phase"),
    ).toBe("outgoing-ringing");
    expect(screen.getByText(en.calls.ringingDots)).toBeTruthy();
    // It names the person being called, not the caller.
    expect(
      screen.getByRole("heading", { level: 3, name: "Trần Minh Quân" }),
    ).toBeTruthy();
    screen.getByRole("button", { name: en.calls.cancel }).click();
    expect(c.hangUp).toHaveBeenCalled();
    expect(screen.getByRole("button", { name: en.calls.mute })).toBeTruthy();
  });

  it("offers Answer and Decline for an incoming ring and names the caller", () => {
    const c = controller({
      incoming: [
        {
          callId: "call-9",
          spaceId: "space-1",
          kind: "video",
          initiatorId: "quan",
          version: 1,
          receivedAt: AT,
        },
      ],
    });
    dock(c);
    expect(
      screen.getByRole("alertdialog", {
        name: /Trần Minh Quân is video calling you/,
      }),
    ).toBeTruthy();
    screen.getByRole("button", { name: en.calls.answer }).click();
    screen.getByRole("button", { name: en.calls.decline }).click();
    screen.getByRole("button", { name: en.calls.answerVoice }).click();
    expect(c.answer).toHaveBeenCalledWith("call-9");
    expect(c.answer).toHaveBeenCalledWith("call-9", true);
    expect(c.decline).toHaveBeenCalledWith("call-9");
  });

  it("shows connecting, then the timer, mute, camera and hang up in a video call", () => {
    const active = call({
      kind: "video",
      state: "active",
      initiatorId: "quan",
      answeredAt: new Date(Date.now() - 65_000).toISOString(),
      participants: [
        {
          personId: "quan",
          joinedAt: AT,
          leftAt: null,
          audioOn: true,
          videoOn: false,
          sharingScreen: false,
        },
        {
          personId: "me",
          joinedAt: AT,
          leftAt: null,
          audioOn: true,
          videoOn: true,
          sharingScreen: false,
        },
      ],
    });
    const c = controller({
      current: held({ call: active, videoOn: true }),
    });
    dock(c);
    expect(
      screen.getByRole("timer", { name: en.calls.timerLabel }).textContent ??
        "",
    ).toMatch(/^1:0\d$/);
    expect(screen.getByTestId("chat-call-remote-tile")).toBeTruthy();
    screen.getByRole("button", { name: en.calls.mute }).click();
    screen.getByRole("button", { name: en.calls.cameraOff }).click();
    screen.getByRole("button", { name: en.calls.hangUp }).click();
    expect(c.toggleMicrophone).toHaveBeenCalled();
    expect(c.toggleCamera).toHaveBeenCalled();
    expect(c.hangUp).toHaveBeenCalled();
  });

  it("shows connecting, reconnecting, a failed room with retry, and the device notices", () => {
    const active = call({
      state: "active",
      initiatorId: "quan",
      answeredAt: AT,
    });
    const connecting = controller({
      current: held({ call: active, media: "connecting" }),
    });
    const { rerender } = dock(connecting);
    expect(screen.getByText(en.calls.connecting)).toBeTruthy();

    rerender(
      <Surfaces
        c={controller({
          current: held({ call: active, media: "reconnecting" }),
        })}
      />,
    );
    expect(screen.getByText(en.calls.reconnecting)).toBeTruthy();

    const failed = controller({
      current: held({ call: active, media: "failed" }),
    });
    rerender(<Surfaces c={failed} />);
    expect(screen.getByRole("alert").textContent ?? "").toContain(
      en.calls.failedConnect,
    );
    screen.getByRole("button", { name: en.calls.retry }).click();
    expect(failed.retryConnect).toHaveBeenCalled();

    rerender(
      <Surfaces
        c={controller({
          current: held({ call: active, deviceNotice: "mic-denied" }),
        })}
      />,
    );
    expect(screen.getByRole("status").textContent ?? "").toContain(
      en.calls.micDenied,
    );
    rerender(
      <Surfaces
        c={controller({
          current: held({ call: active, deviceNotice: "camera-denied" }),
        })}
      />,
    );
    expect(screen.getByRole("status").textContent ?? "").toContain(
      en.calls.cameraDenied,
    );
  });

  it.each([
    ["declined", /declined the call/],
    ["missed", /did not answer/],
    ["cancelled", /Call cancelled/],
    ["busy", /in another call/],
    ["failed", /could not connect/],
    ["completed", /Call ended/],
  ] as const)(
    "shows the ended card for %s with a Close button",
    (reason, text) => {
      const c = controller({
        current: held({
          call: call({
            state: "ended",
            endReason: reason,
            answeredAt: AT,
            endedAt: "2026-11-17T04:03:02.000Z",
            initiatorId: "quan",
          }),
          media: "idle",
        }),
      });
      dock(c);
      expect(screen.getByTestId("chat-call-stage").textContent ?? "").toMatch(
        text,
      );
      screen.getByRole("button", { name: en.calls.close }).click();
      expect(c.dismiss).toHaveBeenCalled();
      if (reason === "completed") expect(screen.getByText("3:02")).toBeTruthy();
    },
  );
});

describe("the call line in the timeline", () => {
  it.each([
    [
      "voice call ended, lasted 3m 2s",
      "cuộc gọi thoại đã kết thúc, kéo dài 3m 2s",
    ],
    ["missed video call", "cuộc gọi video nhỡ"],
    ["voice call declined", "cuộc gọi thoại bị từ chối"],
    ["video call cancelled", "cuộc gọi video đã bị hủy"],
    [
      "voice call not answered, the other person is in a call",
      "cuộc gọi thoại chưa được trả lời, người kia đang trong cuộc gọi khác",
    ],
    ["voice call failed", "cuộc gọi thoại không kết nối được"],
  ])("says %s in Vietnamese", (stored, expected) => {
    expect(systemTextOf(viCopy, stored)).toBe(expected);
  });

  it("keeps an unknown line as stored and says the call lines in English", () => {
    expect(systemTextOf(en, "voice call ended, lasted 9s")).toBe(
      "voice call ended, lasted 9s",
    );
    expect(systemTextOf(en, "something else")).toBe("something else");
  });
});
