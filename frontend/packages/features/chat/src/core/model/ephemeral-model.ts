/**
 * Typing and presence (UC-CHT-C07, C08): framework-neutral rules. Both are
 * ephemeral and in memory only: nothing here touches storage, and neither is
 * ever used to decide what a person may do.
 */

export type ChatPresenceState = "online" | "idle" | "offline";

/** A signal of another person in a space, as the host's realtime transport delivers it. */
export type ChatEphemeralEvent =
  | {
      readonly kind: "typing";
      readonly channelId: string;
      readonly userId: string;
      readonly expiresInMs: number;
    }
  | {
      readonly kind: "presence";
      readonly channelId: string;
      readonly userId: string;
      readonly state: ChatPresenceState;
      readonly ttlMs: number;
    };

/**
 * What the host gives the feature for typing and presence. The feature never
 * creates a transport (FE-RT-004); the host maps this onto its realtime client.
 */
export interface ChatEphemeralPort {
  /** Hears the other people's signals of one space; the host holds the space subscription while listening. */
  readonly subscribe: (
    spaceId: string,
    listener: (event: ChatEphemeralEvent) => void,
  ) => () => void;
  /** Tells the space's subscribers the caller is typing. */
  readonly sendTyping: (spaceId: string) => void;
  /** Reports the caller's presence (a heartbeat). */
  readonly sendPresence: (state: ChatPresenceState) => void;
}

/** One typing signal is sent at most this often per space. */
export const TYPING_THROTTLE_MS = 3_000;
/** A typing indicator lasts this long after the last signal when the frame does not say otherwise. */
export const TYPING_DEFAULT_EXPIRY_MS = 5_000;
/** Presence is reported this often while the page is open. */
export const PRESENCE_HEARTBEAT_MS = 30_000;
/** A person with no heartbeat for this long counts as offline when the frame does not say otherwise. */
export const PRESENCE_DEFAULT_TTL_MS = 75_000;
/** The stores keep at most this many people (the oldest go first): memory stays bounded. */
export const EPHEMERAL_MAX_ENTRIES = 500;

export interface EphemeralClock {
  readonly now: () => number;
  readonly setTimeout: (fn: () => void, ms: number) => unknown;
  readonly clearTimeout: (handle: unknown) => void;
}

export const systemEphemeralClock: EphemeralClock = {
  now: () => Date.now(),
  setTimeout: (fn, ms) => setTimeout(fn, ms),
  clearTimeout: (handle) =>
    clearTimeout(handle as ReturnType<typeof setTimeout>),
};

/** A tiny observable: components read a snapshot and re-render when it changes. */
class Observable {
  private readonly listeners = new Set<() => void>();
  private version = 0;

  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => {
      this.listeners.delete(listener);
    };
  };

  getVersion = (): number => this.version;

  protected changed(): void {
    this.version += 1;
    for (const listener of Array.from(this.listeners)) listener();
  }
}

/**
 * Who is typing, per space. An entry expires at the time its frame says (5 s
 * after the last signal); a message from the person, or leaving the space, clears it.
 */
export class TypingStore extends Observable {
  private readonly entries = new Map<string, Map<string, number>>();
  private timer: unknown = null;

  constructor(private readonly clock: EphemeralClock = systemEphemeralClock) {
    super();
  }

  signal(spaceId: string, userId: string, expiresInMs: number): void {
    const bySpace = this.entries.get(spaceId) ?? new Map<string, number>();
    // Re-inserting moves the person to the end: the oldest entries are dropped first.
    bySpace.delete(userId);
    bySpace.set(userId, this.clock.now() + Math.max(0, expiresInMs));
    this.entries.set(spaceId, bySpace);
    this.trim();
    this.schedule();
    this.changed();
  }

  clear(spaceId: string, userId: string): void {
    const bySpace = this.entries.get(spaceId);
    if (!bySpace?.delete(userId)) return;
    if (bySpace.size === 0) this.entries.delete(spaceId);
    this.schedule();
    this.changed();
  }

  clearSpace(spaceId: string): void {
    if (!this.entries.delete(spaceId)) return;
    this.schedule();
    this.changed();
  }

  /** The people typing in the space now, oldest signal first. */
  typingIn(spaceId: string): readonly string[] {
    const now = this.clock.now();
    const bySpace = this.entries.get(spaceId);
    if (!bySpace) return [];
    return [...bySpace].filter(([, until]) => until > now).map(([id]) => id);
  }

  size(): number {
    let total = 0;
    for (const bySpace of this.entries.values()) total += bySpace.size;
    return total;
  }

  dispose(): void {
    if (this.timer !== null) this.clock.clearTimeout(this.timer);
    this.timer = null;
    this.entries.clear();
  }

  private trim(): void {
    while (this.size() > EPHEMERAL_MAX_ENTRIES) {
      const firstSpace = this.entries.entries().next().value;
      if (!firstSpace) return;
      const [spaceId, bySpace] = firstSpace;
      const oldest = bySpace.keys().next().value;
      if (oldest !== undefined) bySpace.delete(oldest);
      if (bySpace.size === 0) this.entries.delete(spaceId);
    }
  }

  /** One timer for the soonest expiry: it drops the expired entries and tells the readers. */
  private schedule(): void {
    if (this.timer !== null) this.clock.clearTimeout(this.timer);
    this.timer = null;
    let soonest = Infinity;
    for (const bySpace of this.entries.values()) {
      for (const until of bySpace.values()) soonest = Math.min(soonest, until);
    }
    if (soonest === Infinity) return;
    this.timer = this.clock.setTimeout(
      () => {
        this.timer = null;
        const now = this.clock.now();
        for (const [spaceId, bySpace] of this.entries) {
          for (const [userId, until] of bySpace) {
            if (until <= now) bySpace.delete(userId);
          }
          if (bySpace.size === 0) this.entries.delete(spaceId);
        }
        this.schedule();
        this.changed();
      },
      Math.max(0, soonest - this.clock.now()),
    );
  }
}

/**
 * The presence of other people, by person (a person is one dot wherever they
 * appear). A state with no newer frame for its time to live counts as offline.
 */
export class PresenceStore extends Observable {
  private readonly entries = new Map<
    string,
    { state: ChatPresenceState; until: number }
  >();
  private timer: unknown = null;

  constructor(private readonly clock: EphemeralClock = systemEphemeralClock) {
    super();
  }

  report(userId: string, state: ChatPresenceState, ttlMs: number): void {
    this.entries.delete(userId);
    this.entries.set(userId, {
      state,
      until: this.clock.now() + Math.max(0, ttlMs),
    });
    while (this.entries.size > EPHEMERAL_MAX_ENTRIES) {
      const oldest = this.entries.keys().next().value;
      if (oldest === undefined) break;
      this.entries.delete(oldest);
    }
    this.schedule();
    this.changed();
  }

  stateOf(userId: string): ChatPresenceState {
    const entry = this.entries.get(userId);
    return entry && entry.until > this.clock.now() ? entry.state : "offline";
  }

  size(): number {
    return this.entries.size;
  }

  dispose(): void {
    if (this.timer !== null) this.clock.clearTimeout(this.timer);
    this.timer = null;
    this.entries.clear();
  }

  private schedule(): void {
    if (this.timer !== null) this.clock.clearTimeout(this.timer);
    this.timer = null;
    let soonest = Infinity;
    for (const entry of this.entries.values()) {
      soonest = Math.min(soonest, entry.until);
    }
    if (soonest === Infinity) return;
    this.timer = this.clock.setTimeout(
      () => {
        this.timer = null;
        const now = this.clock.now();
        for (const [userId, entry] of this.entries) {
          if (entry.until <= now) this.entries.delete(userId);
        }
        this.schedule();
        this.changed();
      },
      Math.max(0, soonest - this.clock.now()),
    );
  }
}

/**
 * A throttle for the typing signal: the first call goes through, the ones
 * within the interval are dropped, then the next goes through again.
 */
export function createTypingThrottle(
  clock: Pick<EphemeralClock, "now"> = systemEphemeralClock,
  intervalMs: number = TYPING_THROTTLE_MS,
): (spaceId: string) => boolean {
  const lastSent = new Map<string, number>();
  return (spaceId) => {
    const now = clock.now();
    const last = lastSent.get(spaceId);
    if (last !== undefined && now - last < intervalMs) return false;
    // Only the spaces the caller typed in are kept; the map stays small, and is capped anyway.
    if (lastSent.size >= 64) {
      const oldest = lastSent.keys().next().value;
      if (oldest !== undefined) lastSent.delete(oldest);
    }
    lastSent.set(spaceId, now);
    return true;
  };
}
