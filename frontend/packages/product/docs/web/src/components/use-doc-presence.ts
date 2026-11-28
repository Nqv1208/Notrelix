import { useEffect, useState } from "react";
import {
  DOC_PRESENCE_HEARTBEAT_MS,
  type DocPresenceEvent,
  type DocPresenceState,
  type DocsRealtime,
} from "@notrelix/docs-collaboration";

/** A person counts as idle after this long without input while the page is open. */
const IDLE_AFTER_MS = 60_000;

/**
 * Who else has this page open, from the page's presence signals only.
 * Online and idle count as here; offline leaves at once; a person whose
 * heartbeat stopped leaves when the signal's time-to-live runs out. Nothing is
 * stored, and the list never grants anything.
 */
export class DocPresenceRoster {
  private readonly here = new Map<string, number>();

  constructor(private readonly now: () => number = Date.now) {}

  apply(event: DocPresenceEvent): void {
    if (event.state === "offline") this.here.delete(event.userId);
    else this.here.set(event.userId, this.now() + event.ttlMs);
  }

  /** The people still here, in the order they arrived; expired ones are dropped. */
  people(): string[] {
    const at = this.now();
    for (const [userId, until] of this.here) {
      if (until <= at) this.here.delete(userId);
    }
    return [...this.here.keys()];
  }
}

function sameList(a: readonly string[], b: readonly string[]): boolean {
  return a.length === b.length && a.every((id, i) => id === b[i]);
}

/**
 * Presence on the open page (UC-COL-E03): reports the caller as online, or idle
 * after a minute without input or while the tab is hidden, with a heartbeat
 * every 30 seconds, and returns the other people who have the page open.
 * Leaving the page ends the page subscription, which tells the others at once.
 */
export function useDocPresence(input: {
  realtime: DocsRealtime | undefined;
  workspaceId: string;
  pageId: string;
}): readonly string[] {
  const { realtime, workspaceId, pageId } = input;
  const [people, setPeople] = useState<readonly string[]>([]);

  useEffect(() => {
    setPeople([]);
    if (!realtime || typeof document === "undefined") return;
    const roster = new DocPresenceRoster();
    const publish = () =>
      setPeople((current) => {
        const next = roster.people();
        return sameList(current, next) ? current : next;
      });
    const stop = realtime.subscribePagePresence(
      { workspaceId, pageId },
      (event) => {
        roster.apply(event);
        publish();
      },
    );

    let lastInput = Date.now();
    let last: DocPresenceState | null = null;
    const current = (): DocPresenceState =>
      document.visibilityState === "hidden" ||
      Date.now() - lastInput > IDLE_AFTER_MS
        ? "idle"
        : "online";
    const report = () => {
      last = current();
      realtime.sendPresence(last);
    };
    const onInput = () => {
      lastInput = Date.now();
      if (last === "idle") report();
    };
    const onVisibility = () => report();

    report();
    const heartbeat = window.setInterval(() => {
      report();
      // Expiry is checked on the heartbeat too, so a silent person leaves.
      publish();
    }, DOC_PRESENCE_HEARTBEAT_MS);
    document.addEventListener("visibilitychange", onVisibility);
    window.addEventListener("pointerdown", onInput, { passive: true });
    window.addEventListener("keydown", onInput, { passive: true });
    return () => {
      window.clearInterval(heartbeat);
      document.removeEventListener("visibilitychange", onVisibility);
      window.removeEventListener("pointerdown", onInput);
      window.removeEventListener("keydown", onInput);
      stop();
    };
  }, [realtime, workspaceId, pageId]);

  return people;
}
