import { describe, expect, it, vi } from "vitest";
import {
  EPHEMERAL_MAX_ENTRIES,
  PresenceStore,
  TypingStore,
  createTypingThrottle,
  type EphemeralClock,
} from "../core/model/ephemeral-model";
import { typingText } from "../web/components/chat-typing-indicator";
import { en, vi as viCopy } from "../web/components/chat.copy";

/** A clock the test advances; timers fire when it passes them. */
function fakeClock() {
  let now = 0;
  let nextId = 1;
  const timers = new Map<number, { at: number; fn: () => void }>();
  const clock: EphemeralClock = {
    now: () => now,
    setTimeout: (fn, ms) => {
      const id = nextId++;
      timers.set(id, { at: now + ms, fn });
      return id;
    },
    clearTimeout: (handle) => {
      timers.delete(handle as number);
    },
  };
  return {
    clock,
    pendingTimers: () => timers.size,
    advance(ms: number) {
      const target = now + ms;
      for (;;) {
        const due = [...timers.entries()]
          .filter(([, t]) => t.at <= target)
          .sort((a, b) => a[1].at - b[1].at)[0];
        if (!due) break;
        timers.delete(due[0]);
        now = due[1].at;
        due[1].fn();
      }
      now = target;
    },
  };
}

describe("TypingStore (UC-CHT-C07)", () => {
  it("shows a person for the time of the frame and drops them after it", () => {
    const t = fakeClock();
    const store = new TypingStore(t.clock);
    const changed = vi.fn();
    store.subscribe(changed);

    store.signal("space", "ann", 5000);
    expect(store.typingIn("space")).toEqual(["ann"]);

    t.advance(4999);
    expect(store.typingIn("space")).toEqual(["ann"]);
    t.advance(2);
    expect(store.typingIn("space")).toEqual([]);
    expect(changed).toHaveBeenCalledTimes(2);
    expect(t.pendingTimers()).toBe(0);
  });

  it("a new signal extends the indicator, it does not stack", () => {
    const t = fakeClock();
    const store = new TypingStore(t.clock);
    store.signal("space", "ann", 5000);
    t.advance(3000);
    store.signal("space", "ann", 5000);
    t.advance(3000);
    expect(store.typingIn("space")).toEqual(["ann"]);
    t.advance(2001);
    expect(store.typingIn("space")).toEqual([]);
  });

  it("keeps spaces apart and clears a person when they send", () => {
    const t = fakeClock();
    const store = new TypingStore(t.clock);
    store.signal("a", "ann", 5000);
    store.signal("b", "bob", 5000);
    store.clear("a", "ann");
    expect(store.typingIn("a")).toEqual([]);
    expect(store.typingIn("b")).toEqual(["bob"]);
    store.clearSpace("b");
    expect(store.size()).toBe(0);
  });

  it("stays bounded: the oldest entries go first", () => {
    const t = fakeClock();
    const store = new TypingStore(t.clock);
    for (let i = 0; i < EPHEMERAL_MAX_ENTRIES + 25; i++) {
      store.signal("space", `u${i}`, 5000);
    }
    expect(store.size()).toBe(EPHEMERAL_MAX_ENTRIES);
    expect(store.typingIn("space")).not.toContain("u0");
    expect(store.typingIn("space")).toContain(`u${EPHEMERAL_MAX_ENTRIES + 24}`);
  });
});

describe("PresenceStore (UC-CHT-C08)", () => {
  it("reports the state until its time to live, then offline", () => {
    const t = fakeClock();
    const store = new PresenceStore(t.clock);
    expect(store.stateOf("ann")).toBe("offline");

    store.report("ann", "online", 75_000);
    expect(store.stateOf("ann")).toBe("online");
    store.report("ann", "idle", 75_000);
    expect(store.stateOf("ann")).toBe("idle");

    t.advance(75_001);
    expect(store.stateOf("ann")).toBe("offline");
    expect(store.size()).toBe(0);
  });

  it("a heartbeat renews the time to live and an explicit offline wins at once", () => {
    const t = fakeClock();
    const store = new PresenceStore(t.clock);
    store.report("ann", "online", 75_000);
    t.advance(60_000);
    store.report("ann", "online", 75_000);
    t.advance(60_000);
    expect(store.stateOf("ann")).toBe("online");
    store.report("ann", "offline", 75_000);
    expect(store.stateOf("ann")).toBe("offline");
  });

  it("stays bounded", () => {
    const t = fakeClock();
    const store = new PresenceStore(t.clock);
    for (let i = 0; i < EPHEMERAL_MAX_ENTRIES + 10; i++) {
      store.report(`u${i}`, "online", 75_000);
    }
    expect(store.size()).toBe(EPHEMERAL_MAX_ENTRIES);
  });
});

describe("typing throttle", () => {
  it("lets one signal per 3 seconds per space through", () => {
    const t = fakeClock();
    const allow = createTypingThrottle(t.clock);
    expect(allow("a")).toBe(true);
    t.advance(1000);
    expect(allow("a")).toBe(false);
    expect(allow("b")).toBe(true);
    t.advance(2000);
    expect(allow("a")).toBe(true);
    t.advance(2999);
    expect(allow("a")).toBe(false);
  });
});

describe("typing text", () => {
  it("names one, two, or says several, in both languages", () => {
    expect(typingText([], en)).toBe("");
    expect(typingText(["Ann"], en)).toBe("Ann is typing…");
    expect(typingText(["Ann", "Bob"], en)).toBe("Ann and Bob are typing…");
    expect(typingText(["Ann", "Bob", "Cy"], en)).toBe(
      "Several people are typing…",
    );
    expect(typingText(["An"], viCopy)).toBe("An đang nhập…");
    expect(typingText(["An", "Bình"], viCopy)).toBe("An và Bình đang nhập…");
    expect(typingText(["a", "b", "c"], viCopy)).toBe("Nhiều người đang nhập…");
  });
});
