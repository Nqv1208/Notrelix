import type { ReactNode } from "react";
import {
  act,
  fireEvent,
  render,
  renderHook,
  screen,
  waitFor,
  within,
} from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
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
import { ChatCallStage } from "../web/calls/chat-call-surfaces";
import type {
  ChatCallDeviceFailure,
  ChatCallMediaConnector,
  ChatCallMediaOptions,
} from "../web/calls/chat-call-media";
import { en, vi as viCopy } from "../web/components/chat.copy";
import type { ChatCall } from "../core/types/call";
import type {
  ChatCallsController,
  ChatCurrentCall,
} from "../web/calls/use-chat-calls";

const WS = mockIds.workspaces.primary;
const AT = "2026-11-19T04:00:00.000Z";

type MockCallHook = {
  act: (call: string, person: string, action: string) => boolean;
};
const hook = () =>
  (globalThis as unknown as { __notrelixMockCall: MockCallHook })
    .__notrelixMockCall;

/** A media session with a faked screen picker: `pick` is what `getDisplayMedia` would answer. */
function fakeMedia(pick: ChatCallDeviceFailure | null = null) {
  const shares: boolean[] = [];
  let options!: ChatCallMediaOptions;
  let disconnected = 0;
  const connector: ChatCallMediaConnector = async (given) => {
    options = given;
    queueMicrotask(() => given.onState("connected"));
    return {
      setMicrophone: async () => null,
      setCamera: async () => null,
      switchDevice: async () => null,
      setScreenShare: async (on) => {
        shares.push(on);
        return on ? pick : null;
      },
      attachScreen: vi.fn(),
      disconnect: () => void (disconnected += 1),
    };
  };
  return {
    connector,
    shares,
    options: () => options,
    disconnected: () => disconnected,
  };
}

async function setup(
  connector: ChatCallMediaConnector,
  kind: "video" | "voice" = "video",
) {
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
    "dm-key-share",
  );
  const spaceId =
    (dm as unknown as { conversation: { id: string } }).conversation?.id ??
    (dm as unknown as { id: string }).id;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const adapter = createChatRealtimeAdapter({ queryClient, service, viewerId });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  const rendered = renderHook(
    () => useChatCalls({ deps, workspaceId: WS, viewerId, connector }),
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
  const current = () => rendered.result.current;
  act(() => current().start(spaceId, kind));
  await waitFor(() => expect(current().current?.call.state).toBe("ringing"));
  const id = current().current!.call.id;
  hook().act(id, other, "answer");
  await act(() => frame(id));
  await waitFor(() => {
    expect(current().current?.call.state).toBe("active");
    expect(current().current?.media).toBe("connected");
  });
  const record = () => store.chat.calls.get(id)!;
  const sharersOnServer = () =>
    record()
      .participants.filter((p) => p.sharingScreen)
      .map((p) => p.personId);
  /** The other person starts sharing on the server, and the call frame reaches this page. */
  const otherShares = async (replace = false) => {
    const them = record().participants.find((p) => p.personId === other)!;
    for (const p of record().participants) p.sharingScreen = false;
    them.sharingScreen = true;
    record().version += 1;
    if (replace) await act(() => frame(id));
  };
  return {
    store,
    viewerId,
    other,
    id,
    current,
    frame,
    record,
    sharersOnServer,
    otherShares,
    rendered,
  };
}

describe("useChatCalls: sharing the screen", () => {
  it("opens the picker, tells the call, and stops by the control", async () => {
    const media = fakeMedia();
    const t = await setup(media.connector);
    await waitFor(() =>
      expect(t.current().current?.screenSupported).toBe(true),
    );

    act(() => t.current().startScreenShare());
    await waitFor(() => expect(t.current().current?.screen).toBe("on"));
    expect(media.shares).toEqual([true]);
    expect(t.sharersOnServer()).toEqual([t.viewerId]);

    act(() => t.current().stopScreenShare());
    await waitFor(() => expect(t.sharersOnServer()).toEqual([]));
    expect(t.current().current?.screen).toBe("off");
    expect(media.shares).toEqual([true, false]);
  });

  it("says so when the permission is refused or the picker is closed, and tells the call nothing", async () => {
    const media = fakeMedia("denied");
    const t = await setup(media.connector);
    await waitFor(() =>
      expect(t.current().current?.screenSupported).toBe(true),
    );

    act(() => t.current().startScreenShare());
    await waitFor(() =>
      expect(t.current().current?.screenNotice).toBe("denied"),
    );
    expect(t.current().current?.screen).toBe("off");
    expect(t.sharersOnServer()).toEqual([]);
  });

  it("says so when the browser has no such device", async () => {
    const media = fakeMedia("unavailable");
    const t = await setup(media.connector);
    await waitFor(() =>
      expect(t.current().current?.screenSupported).toBe(true),
    );

    act(() => t.current().startScreenShare());
    await waitFor(() =>
      expect(t.current().current?.screenNotice).toBe("unavailable"),
    );
  });

  it("refuses a second share until the person confirms, then replaces the first", async () => {
    const media = fakeMedia();
    const t = await setup(media.connector);
    await waitFor(() =>
      expect(t.current().current?.screenSupported).toBe(true),
    );
    await t.otherShares();

    act(() => t.current().startScreenShare(false));
    await waitFor(() =>
      expect(t.current().current?.screenNotice).toBe("taken"),
    );
    expect(media.shares).toEqual([true, false]);
    expect(t.sharersOnServer()).toEqual([t.other]);

    act(() => t.current().startScreenShare(true));
    await waitFor(() => expect(t.current().current?.screen).toBe("on"));
    expect(t.sharersOnServer()).toEqual([t.viewerId]);
  });

  it("stops the capture when someone else replaces this person's share", async () => {
    const media = fakeMedia();
    const t = await setup(media.connector);
    await waitFor(() =>
      expect(t.current().current?.screenSupported).toBe(true),
    );
    act(() => t.current().startScreenShare());
    await waitFor(() => expect(t.current().current?.screen).toBe("on"));

    await t.otherShares(true);

    await waitFor(() => expect(t.current().current?.screen).toBe("off"));
    expect(t.current().current?.screenNotice).toBe("replaced");
    expect(media.shares).toEqual([true, false]);
  });

  it("says so when the browser's own stop bar ends the share, and clears it on the call", async () => {
    const media = fakeMedia();
    const t = await setup(media.connector);
    await waitFor(() =>
      expect(t.current().current?.screenSupported).toBe(true),
    );
    act(() => t.current().startScreenShare());
    await waitFor(() => expect(t.current().current?.screen).toBe("on"));

    act(() => media.options().onScreenEnded?.());

    expect(t.current().current?.screen).toBe("off");
    expect(t.current().current?.screenNotice).toBe("ended");
    await waitFor(() => expect(t.sharersOnServer()).toEqual([]));
  });

  it("keeps the share on when the microphone is muted", async () => {
    const media = fakeMedia();
    const t = await setup(media.connector);
    await waitFor(() =>
      expect(t.current().current?.screenSupported).toBe(true),
    );
    act(() => t.current().startScreenShare());
    await waitFor(() => expect(t.current().current?.screen).toBe("on"));

    act(() => t.current().toggleMicrophone());

    await waitFor(() =>
      expect(
        t.record().participants.find((p) => p.personId === t.viewerId)?.audioOn,
      ).toBe(false),
    );
    expect(t.sharersOnServer()).toEqual([t.viewerId]);
    expect(t.current().current?.screen).toBe("on");
  });

  it("ends the share with the call", async () => {
    const media = fakeMedia();
    const t = await setup(media.connector);
    await waitFor(() =>
      expect(t.current().current?.screenSupported).toBe(true),
    );
    act(() => t.current().startScreenShare());
    await waitFor(() => expect(t.current().current?.screen).toBe("on"));

    hook().act(t.id, t.other, "end");
    await act(() => t.frame(t.id));

    await waitFor(() => expect(t.current().current?.call.state).toBe("ended"));
    expect(t.current().current?.screen).toBe("off");
    expect(media.disconnected()).toBeGreaterThan(0);
    expect(t.sharersOnServer()).toEqual([]);
  });

  it("offers no share where the browser has no screen picker", async () => {
    const bare: ChatCallMediaConnector = async (options) => {
      queueMicrotask(() => options.onState("connected"));
      return {
        setMicrophone: async () => null,
        setCamera: async () => null,
        switchDevice: async () => null,
        disconnect: () => undefined,
      };
    };
    const t = await setup(bare);
    expect(t.current().current?.screenSupported).toBe(false);
    act(() => t.current().startScreenShare());
    expect(t.current().current?.screen).toBe("off");
    expect(t.sharersOnServer()).toEqual([]);
  });

  it("shares the screen in a voice call too, with the camera still off", async () => {
    const media = fakeMedia();
    const voice = await setup(media.connector, "voice");
    await waitFor(() =>
      expect(voice.current().current?.screenSupported).toBe(true),
    );

    act(() => voice.current().startScreenShare());
    await waitFor(() => expect(voice.current().current?.screen).toBe("on"));
    expect(media.shares).toEqual([true]);
    expect(voice.sharersOnServer()).toEqual([voice.viewerId]);
    expect(voice.current().current?.videoOn).toBe(false);

    act(() => voice.current().stopScreenShare());
    await waitFor(() => expect(voice.sharersOnServer()).toEqual([]));
  });
});

// ---- the stage ----------------------------------------------------------------------------------------------------------------

function callWith(
  over: Partial<ChatCall["participants"][number]>[] = [],
  kind: "voice" | "video" = "video",
): ChatCall {
  const base = (id: string) => ({
    personId: id,
    joinedAt: AT,
    leftAt: null,
    audioOn: true,
    videoOn: false,
    sharingScreen: false,
  });
  const me = { ...base("me"), ...over[0] };
  const quan = { ...base("quan"), ...over[1] };
  return {
    id: "call-1",
    spaceId: "space-1",
    kind,
    state: "active",
    endReason: null,
    initiatorId: "quan",
    startedAt: AT,
    answeredAt: AT,
    endedAt: null,
    region: "ap-southeast-1",
    version: 3,
    participants: [me, quan],
  };
}

function held(
  over: Partial<ChatCurrentCall> = {},
  call = callWith(),
): ChatCurrentCall {
  return {
    call,
    media: "connected",
    attempt: null,
    audioOn: true,
    videoOn: false,
    deviceNotice: null,
    devices: {},
    screenSupported: true,
    screen: "off",
    screenNotice: null,
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
    switchDevice: vi.fn(async () => null),
    startScreenShare: vi.fn(),
    stopScreenShare: vi.fn(),
    dismissScreenNotice: vi.fn(),
    retryConnect: vi.fn(),
    dismiss: vi.fn(),
    remoteVideoRef: vi.fn(),
    localVideoRef: vi.fn(),
    screenVideoRef: vi.fn(),
    join: vi.fn(),
    fullKind: null,
    speaking: new Set<string>(),
    participantVideoRef: () => vi.fn(),
    ...over,
  };
}

const NAMES: Record<string, string> = { me: "Thu Hà", quan: "Trần Minh Quân" };

function stage(
  c: ChatCallsController,
  h: ChatCurrentCall,
  copy = en,
  group?: { name: string },
) {
  return (
    <ChatCallStage
      group={group}
      controller={c}
      held={h}
      visible
      onMinimize={vi.fn()}
      viewerId="me"
      nameOf={(id) => NAMES[id] ?? "?"}
      peerOf={() => "quan"}
      copy={copy}
    />
  );
}

/** A channel call of three people: this person, Quân and Hà. */
function groupCall(
  sharing: "me" | "quan" | null,
  kind: "voice" | "video" = "video",
): ChatCall {
  const base = (id: string) => ({
    personId: id,
    joinedAt: AT,
    leftAt: null,
    audioOn: true,
    videoOn: false,
    sharingScreen: sharing === id,
  });
  return {
    ...callWith([], kind),
    participants: [base("me"), base("quan"), base("ha")],
  };
}

describe("the group call stage with a screen share", () => {
  beforeEach(() => {
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        enumerateDevices: async () => [],
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
      },
    });
    Object.defineProperty(navigator, "permissions", {
      configurable: true,
      value: { query: async () => ({ state: "granted" }) },
    });
  });
  afterEach(() => {
    Reflect.deleteProperty(navigator, "mediaDevices");
    Reflect.deleteProperty(navigator, "permissions");
  });

  it("shows the shared screen as the main tile and keeps every person as a tile in the strip", () => {
    render(
      stage(controller(), held({}, groupCall("quan")), en, { name: "erp" }),
    );

    const main = screen.getByTestId("chat-call-share");
    expect(main.textContent).toContain(
      "Trần Minh Quân is sharing their screen",
    );
    const grid = screen.getByTestId("chat-call-grid");
    expect(within(grid).getAllByTestId("chat-call-tile")).toHaveLength(3);
    expect(grid.className).toContain("h-[104px]");
  });

  it("is the ordinary grid when nobody shares", () => {
    render(stage(controller(), held({}, groupCall(null)), en, { name: "erp" }));

    expect(screen.queryByTestId("chat-call-share")).toBeNull();
    expect(screen.getByTestId("chat-call-grid").className).not.toContain(
      "h-[104px]",
    );
  });

  it("shows the sharer the card with stop, and the control asks before replacing another share", () => {
    const c = controller();
    const { unmount } = render(
      stage(c, held({ screen: "on" }, groupCall("me")), en, { name: "erp" }),
    );
    fireEvent.click(
      within(screen.getByTestId("chat-call-share-own")).getByRole("button", {
        name: en.calls.shareStop,
      }),
    );
    expect(c.stopScreenShare).toHaveBeenCalledTimes(1);
    unmount();

    const d = controller();
    render(stage(d, held({}, groupCall("quan")), en, { name: "erp" }));
    fireEvent.click(screen.getByRole("button", { name: en.calls.shareStart }));
    expect(d.startScreenShare).not.toHaveBeenCalled();
    fireEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", {
        name: en.calls.shareReplaceConfirm,
      }),
    );
    expect(d.startScreenShare).toHaveBeenCalledWith(true);
  });

  it("offers the control in a group voice call as in a group video call", () => {
    const { unmount } = render(
      stage(controller(), held({}, groupCall(null)), en, { name: "erp" }),
    );
    expect(
      screen.getByRole("button", { name: en.calls.shareStart }),
    ).toBeTruthy();
    unmount();

    render(
      stage(controller(), held({}, groupCall(null, "voice")), en, {
        name: "erp",
      }),
    );
    expect(
      screen.getByRole("button", { name: en.calls.shareStart }),
    ).toBeTruthy();
  });

  it("shows a shared screen in a group voice call as the main tile over the voice cards, without camera pictures", () => {
    render(
      stage(controller(), held({}, groupCall("quan", "voice")), en, {
        name: "erp",
      }),
    );

    expect(screen.getByTestId("chat-call-share")).toBeTruthy();
    const strip = screen.getByTestId("chat-call-grid");
    expect(within(strip).getAllByTestId("chat-call-tile")).toHaveLength(3);
    expect(strip.querySelector("video")).toBeNull();
  });
});

describe("the call stage with a screen share", () => {
  // The stage lists devices once the room is live; an empty, granted browser keeps these tests about the share.
  beforeEach(() => {
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        enumerateDevices: async () => [],
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
      },
    });
    Object.defineProperty(navigator, "permissions", {
      configurable: true,
      value: { query: async () => ({ state: "granted" }) },
    });
  });
  afterEach(() => {
    Reflect.deleteProperty(navigator, "mediaDevices");
    Reflect.deleteProperty(navigator, "permissions");
  });

  it("shows the shared screen as the main tile and the people as a strip, in both languages", () => {
    const call = callWith([{}, { sharingScreen: true }]);
    const { unmount } = render(stage(controller(), held({}, call)));

    const main = screen.getByTestId("chat-call-share");
    expect(
      within(main).getByLabelText("Trần Minh Quân's shared screen"),
    ).toBeTruthy();
    expect(main.textContent).toContain(
      "Trần Minh Quân is sharing their screen",
    );
    const strip = screen.getByRole("group", { name: en.calls.shareStrip });
    expect(within(strip).getByLabelText(en.calls.localVideo)).toBeTruthy();
    expect(within(strip).getByLabelText(/Trần Minh Quân/)).toBeTruthy();
    unmount();

    render(stage(controller(), held({}, call), viCopy));
    expect(screen.getByTestId("chat-call-share").textContent).toContain(
      "Trần Minh Quân đang chia sẻ màn hình",
    );
  });

  it("goes back to the ordinary stage when the sharer stops, leaves or the call ends", () => {
    const { rerender } = render(
      stage(controller(), held({}, callWith([{}, { sharingScreen: true }]))),
    );
    expect(screen.queryByTestId("chat-call-share")).not.toBeNull();

    rerender(
      stage(
        controller(),
        held({}, callWith([{}, { sharingScreen: true, leftAt: AT }])),
      ),
    );
    expect(screen.queryByTestId("chat-call-share")).toBeNull();
    expect(screen.getByTestId("chat-call-remote-tile")).toBeTruthy();

    rerender(
      stage(
        controller(),
        held(
          {},
          {
            ...callWith([{}, { sharingScreen: true }]),
            state: "ended",
            endReason: "completed",
            endedAt: AT,
          },
        ),
      ),
    );
    expect(screen.queryByTestId("chat-call-share")).toBeNull();
  });

  it("shows the sharer a card with who sees it and a stop button, not their own screen", () => {
    const c = controller();
    render(
      stage(c, held({ screen: "on" }, callWith([{ sharingScreen: true }]))),
    );

    const own = screen.getByTestId("chat-call-share-own");
    expect(own.textContent).toContain(en.calls.shareYouTitle);
    expect(screen.queryByTestId("chat-call-share")).toBeNull();
    fireEvent.click(
      within(own).getByRole("button", { name: en.calls.shareStop }),
    );
    expect(c.stopScreenShare).toHaveBeenCalledTimes(1);
  });

  it("offers the share control in a video call and in a voice call, and starts a share from it", () => {
    const c = controller();
    const { unmount } = render(stage(c, held()));
    const button = screen.getByRole("button", { name: en.calls.shareStart });
    expect(button.getAttribute("aria-pressed")).toBe("false");
    fireEvent.click(button);
    expect(c.startScreenShare).toHaveBeenCalledWith(false);
    unmount();

    const voice = controller();
    render(stage(voice, held({}, callWith([], "voice"))));
    fireEvent.click(screen.getByRole("button", { name: en.calls.shareStart }));
    expect(voice.startScreenShare).toHaveBeenCalledWith(false);
    expect(
      screen.queryByRole("button", { name: en.calls.cameraOn }),
    ).toBeNull();
  });

  it("shows a shared screen in a one-to-one voice call as the main tile over the two voice cards, and returns when it stops", () => {
    const { rerender } = render(
      stage(
        controller(),
        held({}, callWith([{}, { sharingScreen: true }], "voice")),
      ),
    );

    expect(screen.getByTestId("chat-call-share").textContent).toContain(
      "Trần Minh Quân is sharing their screen",
    );
    const strip = screen.getByTestId("chat-call-grid");
    expect(within(strip).getAllByTestId("chat-call-tile")).toHaveLength(2);
    expect(strip.querySelector("video")).toBeNull();

    rerender(stage(controller(), held({}, callWith([], "voice"))));
    expect(screen.queryByTestId("chat-call-share")).toBeNull();
    expect(screen.getByTestId("chat-call-halo")).toBeTruthy();
  });

  it("shows the sharer of a one-to-one voice call the own-share card with the stop button", () => {
    const c = controller();
    render(
      stage(
        c,
        held({ screen: "on" }, callWith([{ sharingScreen: true }], "voice")),
      ),
    );

    fireEvent.click(
      within(screen.getByTestId("chat-call-share-own")).getByRole("button", {
        name: en.calls.shareStop,
      }),
    );
    expect(c.stopScreenShare).toHaveBeenCalledTimes(1);
    expect(screen.queryByTestId("chat-call-share")).toBeNull();
  });

  it("hides the control where the browser cannot share (a phone), and while the browser's picker is open disables it", () => {
    const { unmount } = render(
      stage(controller(), held({ screenSupported: false })),
    );
    expect(
      screen.queryByRole("button", { name: en.calls.shareStart }),
    ).toBeNull();
    unmount();

    render(stage(controller(), held({ screen: "starting" })));
    expect(
      screen
        .getByRole("button", { name: en.calls.shareStart })
        .hasAttribute("disabled"),
    ).toBe(true);
  });

  it("asks before replacing another person's share, and replaces only on the confirmation", () => {
    const c = controller();
    render(stage(c, held({}, callWith([{}, { sharingScreen: true }]))));

    fireEvent.click(screen.getByRole("button", { name: en.calls.shareStart }));
    expect(c.startScreenShare).not.toHaveBeenCalled();
    const dialog = screen.getByRole("dialog");
    expect(dialog.textContent).toContain(en.calls.shareReplaceTitle);
    expect(dialog.textContent).toContain(
      "Trần Minh Quân is sharing their screen. If you share, their share stops.",
    );

    fireEvent.click(
      within(dialog).getByRole("button", { name: en.calls.shareReplaceCancel }),
    );
    expect(c.startScreenShare).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: en.calls.shareStart }));
    fireEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", {
        name: en.calls.shareReplaceConfirm,
      }),
    );
    expect(c.startScreenShare).toHaveBeenCalledWith(true);
  });

  it("names why a share did not start or stopped, with the way to try again", () => {
    const c = controller();
    const { rerender } = render(stage(c, held({ screenNotice: "denied" })));
    const denied = screen.getByRole("alert", { name: "" });
    expect(denied.textContent).toContain(en.calls.shareDenied);
    fireEvent.click(
      within(denied).getByRole("button", { name: en.calls.shareRetry }),
    );
    expect(c.startScreenShare).toHaveBeenCalledWith(false);

    rerender(stage(c, held({ screenNotice: "unavailable" })));
    expect(screen.getByTestId("chat-call-share-notice").textContent).toContain(
      en.calls.shareUnavailable,
    );

    rerender(stage(c, held({ screenNotice: "taken" })));
    expect(screen.getByTestId("chat-call-share-notice").textContent).toContain(
      en.calls.shareTaken,
    );
    expect(
      within(screen.getByTestId("chat-call-share-notice")).queryByRole(
        "button",
      ),
    ).toBeNull();

    rerender(stage(c, held({ screenNotice: "ended" })));
    const ended = screen.getByRole("status", { name: "" });
    expect(ended.textContent).toContain(en.calls.shareEnded);
    fireEvent.click(
      within(ended).getByRole("button", { name: en.calls.shareAgain }),
    );
    expect(c.startScreenShare).toHaveBeenCalledTimes(2);

    rerender(
      stage(
        c,
        held(
          { screenNotice: "replaced" },
          callWith([{}, { sharingScreen: true }]),
        ),
      ),
    );
    expect(screen.getByTestId("chat-call-share-notice").textContent).toContain(
      "Trần Minh Quân replaced your screen share.",
    );
  });

  it("speaks Vietnamese on every share line", () => {
    render(stage(controller(), held({ screenNotice: "denied" }), viCopy));
    expect(screen.getByTestId("chat-call-share-notice").textContent).toContain(
      viCopy.calls.shareDenied,
    );
    expect(
      screen.getByRole("button", { name: viCopy.calls.shareStart }),
    ).toBeTruthy();
  });
});
