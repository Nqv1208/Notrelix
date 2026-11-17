import { describe, expect, it } from "vitest";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import { actAs, chat, key, storeAs, USERS } from "./chat-test-kit";

type Store = ReturnType<typeof storeAs>;

async function directSpace(store: Store, label: string): Promise<string> {
  const res = await chat(store, "POST", "/conversations", {
    idempotencyKey: key(`dm-${label}`),
    body: { kind: "direct", userId: USERS.member },
  });
  expect(res.status).toBeLessThan(300);
  return res.body.conversation?.id ?? res.body.id;
}

async function start(
  store: Store,
  spaceId: string,
  label: string,
  kind = "voice",
) {
  return chat(store, "POST", `/spaces/${spaceId}/calls`, {
    idempotencyKey: key(`call-${label}`),
    body: { kind },
  });
}

describe("MFB-CHAT-CALLS: one-to-one call state in the mock", () => {
  it("starts a ringing call with a mock join token, replays the same key and refuses a second live call", async () => {
    const store = storeAs("owner");
    const space = await directSpace(store, "a");
    const first = await start(store, space, "a");
    expect(first.status).toBe(201);
    expect(first.body.call).toMatchObject({
      state: "ringing",
      kind: "voice",
      version: 1,
    });
    expect(first.body.joinToken.url).toBe("mock://sfu.invalid");

    const replay = await start(store, space, "a");
    expect(replay.status).toBe(201);
    expect(replay.body.call.id).toBe(first.body.call.id);

    const second = await start(store, space, "b");
    expect(second.status).toBe(409);
    expect(second.body.errorCode).toBe("call-already-active");

    const active = await chat(store, "GET", `/spaces/${space}/calls/active`);
    expect(active.body.id).toBe(first.body.call.id);
  });

  it("answers, bumps the version, writes the call line on end and refuses a stale version with 412", async () => {
    const store = storeAs("owner");
    const space = await directSpace(store, "b");
    const started = await start(store, space, "c");
    const id = started.body.call.id;

    actAs(store, "member");
    const stale = await chat(store, "POST", `/calls/${id}/answer`, {
      body: { expectedVersion: 7 },
    });
    expect(stale.status).toBe(412);
    const answered = await chat(store, "POST", `/calls/${id}/answer`, {
      body: { expectedVersion: 1 },
    });
    expect(answered.status).toBe(200);
    expect(answered.body.call).toMatchObject({ state: "active", version: 2 });
    expect(answered.body.joinToken.token).toContain(USERS.member);
    const again = await chat(store, "POST", `/calls/${id}/answer`, {
      body: { expectedVersion: 2 },
    });
    expect(again.body.call.version).toBe(2);

    const media = await chat(store, "PUT", `/calls/${id}/participants/me`, {
      body: {
        audioOn: false,
        videoOn: true,
        sharingScreen: false,
        expectedVersion: 2,
      },
    });
    expect(media.status).toBe(400);
    expect(media.body.errorCode).toBe("call-media-not-allowed");
    const muted = await chat(store, "PUT", `/calls/${id}/participants/me`, {
      body: {
        audioOn: false,
        videoOn: false,
        sharingScreen: false,
        expectedVersion: 2,
      },
    });
    expect(muted.body.version).toBe(3);

    const ended = await chat(store, "POST", `/calls/${id}/end`, {
      body: { expectedVersion: 3 },
    });
    expect(ended.body).toMatchObject({
      state: "ended",
      endReason: "completed",
      version: 4,
    });
    const gone = await chat(store, "POST", `/calls/${id}/end`, {
      body: { expectedVersion: 4 },
    });
    expect(gone.status).toBe(409);
    expect(gone.body.errorCode).toBe("call-ended");

    const messages = await chat(store, "GET", `/spaces/${space}/messages`);
    const lines = (messages.body.items as { kind: string; content: string }[])
      .filter((m) => m.kind === "system")
      .map((m) => m.content);
    expect(
      lines.some((line) => /^voice call ended, lasted \d+s$/.test(line)),
    ).toBe(true);
  });

  it("declines, cancels and misses with the matching reason, and only the starter cancels", async () => {
    const store = storeAs("owner");
    const space = await directSpace(store, "c");
    const a = (await start(store, space, "d")).body.call;
    actAs(store, "member");
    const notStarter = await chat(store, "POST", `/calls/${a.id}/cancel`, {
      body: { expectedVersion: 1 },
    });
    expect(notStarter.status).toBe(403);
    const declined = await chat(store, "POST", `/calls/${a.id}/decline`, {
      body: { expectedVersion: 1 },
    });
    expect(declined.body).toMatchObject({
      state: "ended",
      endReason: "declined",
    });

    actAs(store, "owner");
    const b = (await start(store, space, "e")).body.call;
    const cancelled = await chat(store, "POST", `/calls/${b.id}/cancel`, {
      body: { expectedVersion: 1 },
    });
    expect(cancelled.body.endReason).toBe("cancelled");
    const history = await chat(store, "GET", `/spaces/${space}/calls`);
    expect(
      history.body.items.map((c: { endReason: string }) => c.endReason),
    ).toEqual(["cancelled", "declined"]);
  });

  it("answers 404 to a person outside the space", async () => {
    const store = storeAs("owner");
    const space = await directSpace(store, "d");
    const call = (await start(store, space, "f")).body.call;
    actAs(store, "admin");
    const res = await chat(store, "POST", `/calls/${call.id}/answer`, {
      body: { expectedVersion: 1 },
    });
    expect(res.status).toBe(404);
  });

  it("sends the call frame on the channel stream and the ring on the other person's stream", async () => {
    const store = storeAs("owner");
    const space = await directSpace(store, "e");
    const transport = new MockRealtimeTransport();
    const channel: RealtimeEnvelope[] = [];
    const person: RealtimeEnvelope[] = [];
    const stops = [
      transport.subscribeScope(
        { kind: "channel", id: space },
        (e) => void channel.push(e),
      ),
      transport.subscribeScope(
        { kind: "user", id: USERS.member },
        (e) => void person.push(e),
      ),
    ];
    try {
      await start(store, space, "g");
      const ring = person.filter((e) => e.payloadContract === "chat.call.v1");
      expect(ring).toHaveLength(1);
      expect(ring[0]!.eventType).toBe("chat.call.changed");
      expect(ring[0]!.payload).toMatchObject({
        state: "ringing",
        kind: "voice",
        version: 1,
      });
      expect(channel.some((e) => e.payloadContract === "chat.call.v1")).toBe(
        true,
      );
    } finally {
      stops.forEach((stop) => stop());
      transport.dispose();
    }
  });
});
