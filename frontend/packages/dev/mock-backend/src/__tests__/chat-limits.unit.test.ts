import { describe, it, expect } from "vitest";
import { SPACES, chat, key, storeAs } from "./chat-test-kit";

const GENERAL = SPACES.general;

type Store = ReturnType<typeof storeAs>;

async function send(store: Store, label: string, content = `hello ${label}`) {
  return chat(store, "POST", `/spaces/${GENERAL}/messages`, {
    body: { content, clientMessageId: `cm-${label}` },
    idempotencyKey: key(label),
  });
}

async function setSlowMode(store: Store, seconds: number | undefined) {
  const version = store.chat.spaces.get(GENERAL)!.version;
  const channel = store.chat.spaces.get(GENERAL)!;
  return chat(store, "PATCH", `/channels/${GENERAL}`, {
    body: {
      name: channel.name,
      topic: channel.topic,
      expectedVersion: version,
      ...(seconds === undefined ? {} : { slowModeSeconds: seconds }),
    },
  });
}

describe("MFB-CHAT-S3.2: Chat mock limits and slow mode (UC-CHT-H08)", () => {
  describe("rate limits", () => {
    it("answers 429 with the retry seconds after the person limit, and recovers when the virtual clock moves on", async () => {
      const store = storeAs("member");
      for (let i = 0; i < 30; i += 1) {
        expect((await send(store, `a${i}`)).status).toBe(201);
      }
      const refused = await send(store, "over");
      expect(refused.status).toBe(429);
      expect(refused.body.errorCode).toBe("rate_limit.exceeded");
      expect(refused.body.ruleCode).toBe("rate-limited");
      expect(refused.body.policy).toBe("chat.send");
      expect(refused.body.scope).toBe("person");
      expect(refused.body.retryAfter).toBeGreaterThan(0);

      store.chat.advanceVirtualSeconds(120);
      expect((await send(store, "later")).status).toBe(201);
    });

    it("does not count the retry of a stored send", async () => {
      const store = storeAs("member");
      expect((await send(store, "same")).status).toBe(201);
      for (let i = 0; i < 40; i += 1) {
        expect((await send(store, "same")).status).toBe(200);
      }
    });

    it("limits @channel under its own policy", async () => {
      const store = storeAs("owner");
      let refused = 0;
      for (let i = 0; i < 5; i += 1) {
        const res = await chat(store, "POST", `/spaces/${GENERAL}/messages`, {
          body: {
            content: "@channel hi",
            clientMessageId: `cm-b${i}`,
            confirmBroadcast: true,
            mentions: [{ kind: "broadcast-channel", start: 0, length: 8 }],
          },
          idempotencyKey: key(`b${i}`),
        });
        if (res.status === 429) {
          refused += 1;
          expect(res.body.policy).toBe("chat.broadcast");
        }
      }
      expect(refused).toBeGreaterThan(0);
    });
  });

  describe("slow mode", () => {
    it("is set by a manager, shows on the channel, bumps the version and rejects a stale one with 412", async () => {
      const store = storeAs("owner");
      const before = store.chat.spaces.get(GENERAL)!.version;
      const res = await setSlowMode(store, 30);
      expect(res.status).toBe(200);
      expect(res.body.slowModeSeconds).toBe(30);
      expect(res.body.version).toBe(before + 1);

      const stale = await chat(store, "PATCH", `/channels/${GENERAL}`, {
        body: {
          name: res.body.name,
          topic: null,
          expectedVersion: before,
          slowModeSeconds: 0,
        },
      });
      expect(stale.status).toBe(412);
    });

    it("keeps the value when the request leaves it out, and refuses a value out of range", async () => {
      const store = storeAs("owner");
      await setSlowMode(store, 30);
      const kept = await setSlowMode(store, undefined);
      expect(kept.body.slowModeSeconds).toBe(30);
      expect((await setSlowMode(store, 3601)).status).toBe(400);
      expect((await setSlowMode(store, -1)).status).toBe(400);
    });

    it("refuses a plain member", async () => {
      const store = storeAs("member");
      expect((await setSlowMode(store, 30)).status).toBe(403);
    });

    it("makes the second message wait, with no exemption, until the virtual clock has moved", async () => {
      const store = storeAs("owner");
      await setSlowMode(store, 30);
      expect((await send(store, "one")).status).toBe(201);
      const wait = await send(store, "two");
      expect(wait.status).toBe(429);
      expect(wait.body.ruleCode).toBe("slow-mode");
      expect(wait.body.slowModeSeconds).toBe(30);
      expect(wait.body.retryAfter).toBeGreaterThan(0);
      expect(wait.body.retryAfter).toBeLessThanOrEqual(30);

      store.chat.advanceVirtualSeconds(wait.body.retryAfter);
      expect((await send(store, "two")).status).toBe(201);
    });

    it("turns off with 0", async () => {
      const store = storeAs("owner");
      await setSlowMode(store, 30);
      await send(store, "one");
      await setSlowMode(store, 0);
      expect((await send(store, "two")).status).toBe(201);
    });
  });
});

describe("MFB-CHAT-S3.2: a Guest sees only what they were added to (UC-CHT-H02)", () => {
  it("lists only the channels the Guest is a member of and cannot browse or create", async () => {
    const store = storeAs("viewer");
    const mine = await chat(store, "GET", "/channels");
    expect(mine.status).toBe(200);
    for (const channel of mine.body as { isMember: boolean }[]) {
      expect(channel.isMember).toBe(true);
    }
    const browse = await chat(store, "GET", "/channels", {
      query: "browse=true",
    });
    expect(browse.status).toBe(200);
    expect(browse.body).toEqual([]);
    const created = await chat(store, "POST", "/channels", {
      body: { name: "guest-room" },
      idempotencyKey: key("guest-room"),
    });
    expect(created.status).toBe(403);
  });

  it("answers 404, not 403, for a public channel the Guest was not added to", async () => {
    const store = storeAs("viewer");
    const mine = (await chat(store, "GET", "/channels")).body as {
      id: string;
    }[];
    const other = Object.values(SPACES).find(
      (id) => !mine.some((channel) => channel.id === id),
    );
    expect(other).toBeDefined();
    const res = await chat(store, "GET", `/channels/${other}`);
    expect(res.status).toBe(404);
  });
});
