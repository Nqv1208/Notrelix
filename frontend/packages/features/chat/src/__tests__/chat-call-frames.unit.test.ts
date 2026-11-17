import { QueryClient } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { createChatRealtimeAdapter } from "../web/realtime/chat-realtime-adapter";
import { parseChatCallFrame } from "../core/realtime/chat-frames";
import {
  callPhaseOf,
  elapsedSeconds,
  formatCallTimer,
  parseStoredCallLine,
  type ChatIncomingCall,
} from "../core/model/call-model";
import { chatQueryKeys } from "../query/keys";
import type { ChatCall } from "../core/types/call";

const WS = "ws-1";
const payload = (over: Record<string, unknown> = {}) => ({
  callId: "call-1",
  spaceId: "space-1",
  state: "ringing",
  kind: "voice",
  initiatorId: "user-b",
  participants: ["user-b"],
  participantsOmitted: false,
  version: 1,
  ...over,
});

function adapterOf() {
  const queryClient = new QueryClient();
  const adapter = createChatRealtimeAdapter({
    queryClient,
    service: { listMessages: vi.fn() } as never,
    viewerId: "user-a",
  });
  const deliver = (body: Record<string, unknown> | undefined, over = {}) =>
    adapter.validateAndHandle(
      {
        eventType: "chat.call.changed",
        eventId: "evt",
        resourceId: "user-a",
        workspaceId: WS,
        timestamp: "2026-11-17T04:00:00Z",
        payload: body,
        ...over,
      },
      { workspaceId: WS, invalidateQueries: vi.fn() },
    );
  const incoming = () =>
    queryClient.getQueryData<ChatIncomingCall[]>(
      chatQueryKeys.incomingCalls(WS),
    );
  return { adapter, deliver, incoming, queryClient };
}

describe("chat.call.v1 frames", () => {
  it("parses a call frame and drops anything malformed", () => {
    expect(parseChatCallFrame(payload())).toMatchObject({
      callId: "call-1",
      state: "ringing",
      endReason: null,
      version: 1,
    });
    expect(parseChatCallFrame(payload({ state: "paused" }))).toBeNull();
    expect(parseChatCallFrame(payload({ kind: "screen" }))).toBeNull();
    expect(parseChatCallFrame(payload({ version: -1 }))).toBeNull();
    expect(parseChatCallFrame(payload({ callId: "" }))).toBeNull();
    expect(parseChatCallFrame(null)).toBeNull();
  });

  it("supports the call event and lists a ring for someone else's call until it leaves ringing", async () => {
    const t = adapterOf();
    expect(
      t.adapter.supports({
        eventType: "chat.call.changed",
        eventId: "e",
        resourceId: "r",
        workspaceId: WS,
        timestamp: "t",
      }),
    ).toBe(true);
    await t.deliver(payload());
    expect(t.incoming()).toHaveLength(1);
    await t.deliver(payload());
    expect(t.incoming()).toHaveLength(1);
    await t.deliver(payload({ state: "active", version: 2 }));
    expect(t.incoming()).toHaveLength(0);
  });

  it("does not ring for the viewer's own call, a malformed frame, an older version or another workspace", async () => {
    const t = adapterOf();
    await t.deliver(payload({ initiatorId: "user-a" }));
    await t.deliver(payload({ state: "bogus" }));
    await t.deliver(payload(), { workspaceId: "other-ws" });
    expect(t.incoming() ?? []).toHaveLength(0);

    await t.deliver(payload({ version: 3 }));
    await t.deliver(payload({ version: 2 }));
    expect(t.incoming()?.[0]?.version).toBe(3);
  });

  it("keeps the newest frame of a call for its end reason", async () => {
    const t = adapterOf();
    await t.deliver(
      payload({ state: "ended", endReason: "missed", version: 2 }),
    );
    await t.deliver(payload({ version: 1 }));
    expect(
      t.queryClient.getQueryData(chatQueryKeys.callFrame(WS, "call-1")),
    ).toMatchObject({ state: "ended", endReason: "missed", version: 2 });
  });
});

describe("call model", () => {
  const call = (over: Partial<ChatCall>): ChatCall => ({
    id: "c",
    spaceId: "s",
    kind: "voice",
    state: "ringing",
    endReason: null,
    initiatorId: "me",
    startedAt: "2026-11-17T04:00:00Z",
    answeredAt: null,
    endedAt: null,
    region: "r",
    version: 1,
    participants: [],
    ...over,
  });

  it("reads the phase from where the viewer stands", () => {
    expect(callPhaseOf(call({}), "me", false)).toBe("outgoing-ringing");
    expect(callPhaseOf(call({}), "you", false)).toBe("incoming-ringing");
    expect(callPhaseOf(call({ state: "active" }), "me", false)).toBe(
      "connecting",
    );
    expect(callPhaseOf(call({ state: "active" }), "me", true)).toBe(
      "connected",
    );
    expect(callPhaseOf(call({ state: "ended" }), "me", true)).toBe("ended");
  });

  it("formats the timer in minutes and hours", () => {
    expect(formatCallTimer(0)).toBe("0:00");
    expect(formatCallTimer(65.9)).toBe("1:05");
    expect(formatCallTimer(3725)).toBe("1:02:05");
    expect(formatCallTimer(-4)).toBe("0:00");
    expect(elapsedSeconds(null, 1)).toBe(0);
    expect(
      elapsedSeconds(
        "2026-11-17T04:00:00Z",
        Date.parse("2026-11-17T04:01:00Z"),
      ),
    ).toBe(60);
  });

  it("takes the stored call line apart", () => {
    expect(parseStoredCallLine("voice call ended, lasted 1h 2m")).toEqual({
      kind: "voice",
      outcome: "completed",
      duration: "1h 2m",
    });
    expect(parseStoredCallLine("missed video call")?.outcome).toBe("missed");
    expect(parseStoredCallLine("video call cancelled")?.outcome).toBe(
      "cancelled",
    );
    expect(parseStoredCallLine("pinned a message")).toBeNull();
    expect(parseStoredCallLine(null)).toBeNull();
  });
});
