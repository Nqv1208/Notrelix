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
import { describe, expect, it, vi } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { chatQueryKeys } from "../query/keys";
import { createChatRealtimeAdapter } from "../web/realtime/chat-realtime-adapter";
import { createChatService } from "../core/api/chat.service";
import { chatCatalog } from "../web/components/chat.catalog";
import { ChatWorkspace } from "../web/components/chat-workspace";
import { useChatCalls } from "../web/calls/use-chat-calls";
import { ChatCallStage } from "../web/calls/chat-call-surfaces";
import type {
  ChatCallMediaConnector,
  ChatCallMediaOptions,
} from "../web/calls/chat-call-media";
import { en } from "../web/components/chat.copy";
import { fillCopy } from "../web/components/use-chat-copy";

const WS = mockIds.workspaces.primary;
const AT = "2026-11-19T04:00:00.000Z";

type MockCallHook = {
  ring: (space: string, person: string, kind: "voice" | "video") => string;
  act: (call: string, person: string, action: string) => boolean;
};
const hook = () =>
  (globalThis as unknown as { __notrelixMockCall: MockCallHook })
    .__notrelixMockCall;

function world() {
  const store = new MockStore({ ...defaultConfig });
  // Creating the fetch also points the dev call hook (`__notrelixMockCall`) at this store.
  const mockFetch = createMockFetch(store);
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => mockFetch(input, init),
  });
  const deps = { api: client.api, endpoints: client.endpoints };
  const viewerId = store.getCurrentUser().id;
  const channel = Array.from(store.chat.spaces.values()).find(
    (space) =>
      space.name === "general" &&
      store.chat.activeMembershipOf(space.id, viewerId),
  )!;
  const others = store.chat
    .membershipsOfSpace(channel.id)
    .filter((row) => row.leftAt === null && row.userId !== viewerId)
    .map((row) => row.userId);
  const people = new Map(
    store.getWorkspaceMembers(WS).map((m) => [m.userId, `Person ${m.userId}`]),
  );
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return { store, deps, viewerId, channel, others, people, queryClient };
}

type World = ReturnType<typeof world>;

/** Fills a live call with fake people until `count` are in it (the server's own cap check is what the join then meets). */
function fill(store: MockStore, callId: string, count: number) {
  const call = store.chat.calls.get(callId)!;
  while (call.participants.length < count) {
    call.participants.push({
      personId: `crowd-${call.participants.length}`,
      joinedAt: AT,
      leftAt: null,
      audioOn: true,
      videoOn: false,
      sharingScreen: false,
    });
  }
  call.version += 1;
}

async function openChannel(
  w: World,
  role: "owner" | "guest" = "owner",
  before?: () => void,
) {
  before?.();
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={w.queryClient}>
      <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
        {children}
      </I18nProvider>
    </QueryClientProvider>
  );
  render(
    <ChatWorkspace
      deps={w.deps}
      workspaceId={WS}
      viewerId={w.viewerId}
      viewerRole={role}
      channelId={w.channel.id}
      conversationId={null}
      onOpenConversation={vi.fn()}
      onOpenChannel={vi.fn()}
      people={w.people}
    />,
    { wrapper },
  );
  await screen.findByRole("heading", { level: 1 });
}

describe("ChatWorkspace: a call in a channel", () => {
  it("starts from the channel header and shows the starter alone in the grid, waiting for others, with the cap", async () => {
    const w = world();
    await openChannel(w);
    const header = document.querySelector("header") as HTMLElement;
    fireEvent.click(
      await within(header).findByRole("button", { name: en.panel.voiceCall }),
    );
    const stage = await screen.findByTestId("chat-call-stage");
    expect(await screen.findByTestId("chat-call-group")).toBeTruthy();
    expect(screen.getAllByTestId("chat-call-tile")).toHaveLength(1);
    expect(stage.textContent).toContain("general");
    expect(stage.textContent).toContain(
      fillCopy(en.calls.groupPeople, { count: "1", cap: "25" }),
    );
    expect(stage.textContent).toContain(en.calls.ringingDots);
    expect(Array.from(w.store.chat.calls.values())[0]).toMatchObject({
      spaceId: w.channel.id,
      kind: "voice",
    });
  });

  it("shows a banner for a call somebody else started, and joining puts the person in a grid of everyone", async () => {
    const w = world();
    let callId = "";
    await openChannel(w, "owner", () => {
      callId = hook().ring(w.channel.id, w.others[0]!, "voice");
    });
    const banner = await screen.findByTestId("chat-call-banner");
    expect(banner.textContent).toContain(en.calls.groupBannerVoice);
    expect(banner.textContent).toContain(
      fillCopy(en.calls.groupBannerCount, { count: "1", cap: "25" }),
    );
    // A channel call rings nobody: no incoming toast.
    expect(screen.queryByTestId("chat-call-incoming")).toBeNull();

    fireEvent.click(screen.getByTestId("chat-call-join"));
    await screen.findByTestId("chat-call-group");
    await waitFor(() =>
      expect(screen.getAllByTestId("chat-call-tile")).toHaveLength(2),
    );
    expect(
      w.store.chat.calls.get(callId)!.participants.map((p) => p.personId),
    ).toContain(w.viewerId);
    expect(w.store.chat.calls.get(callId)!.state).toBe("active");
    await waitFor(() =>
      expect(screen.queryByTestId("chat-call-banner")).toBeNull(),
    );
  });

  it.each([["voice", 25] as const, ["video", 12] as const])(
    "explains a full %s call (%i people) in a dialog and keeps the person out",
    async (kind, cap) => {
      const w = world();
      let callId = "";
      await openChannel(w, "owner", () => {
        callId = hook().ring(w.channel.id, w.others[0]!, kind);
        hook().act(callId, w.others[1]!, "join");
        fill(w.store, callId, cap);
      });
      const banner = await screen.findByTestId("chat-call-banner");
      expect(banner.textContent).toContain(
        fillCopy(en.calls.groupBannerFull, {
          count: String(cap),
          cap: String(cap),
        }),
      );
      fireEvent.click(screen.getByTestId("chat-call-join"));
      const dialog = await screen.findByTestId("chat-call-full");
      expect(dialog.textContent).toContain(en.calls.groupFullTitle);
      expect(dialog.textContent).toContain(
        fillCopy(
          kind === "video" ? en.calls.groupFullVideo : en.calls.groupFullVoice,
          {
            cap: String(cap),
          },
        ),
      );
      expect(screen.queryByTestId("chat-call-stage")).toBeNull();
      expect(
        w.store.chat.calls
          .get(callId)!
          .participants.some((p) => p.personId === w.viewerId),
      ).toBe(false);
      // The dialog's corner button and its footer button both close it.
      fireEvent.click(
        within(dialog).getAllByRole("button", { name: en.calls.close }).at(-1)!,
      );
      await waitFor(() =>
        expect(screen.queryByTestId("chat-call-full")).toBeNull(),
      );
    },
    20_000,
  );

  it("offers a Guest no call button and no banner in a channel", async () => {
    const w = world();
    await openChannel(w, "guest", () => {
      hook().ring(w.channel.id, w.others[0]!, "voice");
    });
    const header = document.querySelector("header") as HTMLElement;
    expect(
      within(header).queryByRole("button", { name: en.panel.voiceCall }),
    ).toBeNull();
    expect(screen.queryByTestId("chat-call-banner")).toBeNull();
  });
});

// ─── The controller and the stage with a fake media plane ────────────────────

interface Plane {
  connector: ChatCallMediaConnector;
  options: () => ChatCallMediaOptions;
}

function fakePlane(): Plane {
  let held: ChatCallMediaOptions | null = null;
  return {
    connector: async (options) => {
      held = options;
      queueMicrotask(() => options.onState("connected"));
      return {
        setMicrophone: async () => null,
        setCamera: async () => null,
        switchDevice: async () => null,
        disconnect: () => undefined,
      };
    },
    options: () => held!,
  };
}

function GroupStage({
  c,
  w,
}: {
  c: ReturnType<typeof useChatCalls>;
  w: World;
}) {
  return c.current ? (
    <ChatCallStage
      controller={c}
      held={c.current}
      visible
      onMinimize={vi.fn()}
      viewerId={w.viewerId}
      nameOf={(id) => w.people.get(id) ?? id}
      group={{ name: "general" }}
      copy={en}
    />
  ) : null;
}

function mount(w: World, plane: Plane) {
  const service = createChatService(w.deps.api, w.deps.endpoints);
  const adapter = createChatRealtimeAdapter({
    queryClient: w.queryClient,
    service,
    viewerId: w.viewerId,
  });
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={w.queryClient}>{children}</QueryClientProvider>
  );
  const rendered = renderHook(
    () =>
      useChatCalls({
        deps: w.deps,
        workspaceId: WS,
        connector: plane.connector,
        isGroupSpace: () => true,
      }),
    { wrapper },
  );
  const frame = async (callId: string) => {
    const call = w.store.chat.calls.get(callId)!;
    await adapter.validateAndHandle(
      {
        eventType: "chat.call.changed",
        eventId: `evt-${Math.random()}`,
        resourceId: w.channel.id,
        workspaceId: WS,
        timestamp: AT,
        payload: {
          callId,
          spaceId: w.channel.id,
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
  return { rendered, frame, adapter };
}

describe("a group call with a fake media plane", () => {
  it("draws a tile for everyone with who is speaking and whose microphone is off", async () => {
    const w = world();
    const plane = fakePlane();
    const { rendered, frame } = mount(w, plane);
    act(() => rendered.result.current.start(w.channel.id, "voice"));
    await waitFor(() => expect(rendered.result.current.current).not.toBeNull());
    const callId = rendered.result.current.current!.call.id;
    hook().act(callId, w.others[0]!, "join");
    hook().act(callId, w.others[1]!, "join");
    const quiet = w.store.chat.calls
      .get(callId)!
      .participants.find((p) => p.personId === w.others[1])!;
    quiet.audioOn = false;
    await act(() => frame(callId));
    await waitFor(() =>
      expect(rendered.result.current.current?.call.participants).toHaveLength(
        3,
      ),
    );
    const view = render(<GroupStage c={rendered.result.current} w={w} />);
    expect(screen.getAllByTestId("chat-call-tile")).toHaveLength(3);

    act(() => plane.options().onSpeakers?.([w.others[0]!]));
    view.rerender(<GroupStage c={rendered.result.current} w={w} />);
    const tiles = screen.getAllByTestId("chat-call-tile");
    const byPerson = (id: string) =>
      tiles.find((t) => t.getAttribute("data-person") === id)!;
    expect(byPerson(w.others[0]!).getAttribute("data-speaking")).toBe("true");
    expect(byPerson(w.viewerId).getAttribute("data-speaking")).toBe("false");
    expect(byPerson(w.others[1]!).getAttribute("data-audio")).toBe("off");
    expect(byPerson(w.others[1]!).getAttribute("aria-label")).toContain(
      en.calls.groupMuted,
    );
    expect(byPerson(w.others[0]!).getAttribute("aria-label")).toContain(
      en.calls.groupSpeaking,
    );
    // The participants list says the same, with the starter named.
    const list = screen.getByTestId("chat-call-people");
    expect(within(list).getAllByTestId("chat-call-person")).toHaveLength(3);
    expect(list.textContent).toContain(en.calls.groupStarter);
  });

  it("lets the starter leave: the call goes on for the others and the starter's room closes", async () => {
    const w = world();
    const plane = fakePlane();
    const { rendered, frame } = mount(w, plane);
    act(() => rendered.result.current.start(w.channel.id, "voice"));
    await waitFor(() => expect(rendered.result.current.current).not.toBeNull());
    const callId = rendered.result.current.current!.call.id;
    hook().act(callId, w.others[0]!, "join");
    // The frame of the join reaches the starter, whose call turns active with two people in it.
    await act(() => frame(callId));
    await waitFor(() =>
      expect(rendered.result.current.current?.call.state).toBe("active"),
    );
    act(() => rendered.result.current.hangUp());
    await waitFor(() => expect(rendered.result.current.current).toBeNull());
    const call = w.store.chat.calls.get(callId)!;
    expect(call.state).toBe("active");
    expect(
      call.participants.filter((p) => p.leftAt === null).map((p) => p.personId),
    ).toEqual([w.others[0]]);
  });

  it("shows the ended card when the call ends for everyone while the person is in it", async () => {
    const w = world();
    const plane = fakePlane();
    const { rendered, frame } = mount(w, plane);
    act(() => rendered.result.current.start(w.channel.id, "voice"));
    await waitFor(() => expect(rendered.result.current.current).not.toBeNull());
    const callId = rendered.result.current.current!.call.id;
    hook().act(callId, w.others[0]!, "join");
    await act(() => frame(callId));
    // The server's sweep ends a call nobody is in; here it ends while this person's room is still open.
    const call = w.store.chat.calls.get(callId)!;
    call.state = "ended";
    call.endReason = "completed";
    call.endedAt = AT;
    call.version += 1;
    await act(() => frame(callId));
    await waitFor(() =>
      expect(rendered.result.current.current?.call.state).toBe("ended"),
    );
    render(<GroupStage c={rendered.result.current} w={w} />);
    const stage = screen.getByTestId("chat-call-stage");
    expect(stage.getAttribute("data-phase")).toBe("ended-completed");
    expect(stage.textContent).toContain(en.calls.endedCompleted);
    expect(screen.queryByTestId("chat-call-grid")).toBeNull();
  });
});

describe("the ring of a call frame", () => {
  const ringFrame = (spaceId: string, initiatorId: string) => ({
    eventType: "chat.call.changed",
    eventId: `evt-${Math.random()}`,
    resourceId: spaceId,
    workspaceId: WS,
    timestamp: AT,
    payload: {
      callId: `call-${spaceId}`,
      spaceId,
      state: "ringing",
      kind: "voice",
      initiatorId,
      participants: [initiatorId],
      participantsOmitted: false,
      version: 1,
    },
  });

  it("rings nobody for a channel call and rings for a conversation", async () => {
    const w = world();
    const service = createChatService(w.deps.api, w.deps.endpoints);
    const adapter = createChatRealtimeAdapter({
      queryClient: w.queryClient,
      service,
      viewerId: w.viewerId,
    });
    w.queryClient.setQueryData(chatQueryKeys.channels(WS), [
      { id: w.channel.id },
    ]);
    const context = { workspaceId: WS, invalidateQueries: vi.fn() };
    const incoming = () =>
      w.queryClient.getQueryData<unknown[]>(chatQueryKeys.incomingCalls(WS)) ??
      [];

    await adapter.validateAndHandle(
      ringFrame(w.channel.id, w.others[0]!),
      context,
    );
    expect(incoming()).toHaveLength(0);

    await adapter.validateAndHandle(
      ringFrame("a-direct-conversation", w.others[0]!),
      context,
    );
    expect(incoming()).toHaveLength(1);
  });
});
