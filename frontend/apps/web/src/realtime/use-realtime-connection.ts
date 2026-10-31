import { useCallback, useEffect, useRef, useState, useSyncExternalStore } from "react";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useAuth } from "@notrelix/features-auth";
import type { BoardConnectionState } from "@notrelix/work-management-web";

/** A reconnect that has not worked for this long reads as a lost connection with stale data. */
const LOST_AFTER_MS = 10_000;

/** The live connection state, when it was lost, and a way to connect again. */
export function useRealtimeConnection(): {
  readonly state: BoardConnectionState;
  readonly lostAt: Date | null;
  readonly reconnect: () => void;
} {
  const runtime = useAppRuntime();
  const { sessionGeneration } = useAuth();
  const state = useSyncExternalStore(
    (notify) => runtime.realtime.subscribeState(() => notify()),
    () => runtime.realtime.getState(),
  );
  const lostAt = useRef<Date | null>(null);
  if (state === "connected") lostAt.current = null;
  else if (lostAt.current === null && state !== "idle" && state !== "connecting")
    lostAt.current = new Date();
  // The state flips between reconnecting and connecting on every attempt, so the time since the loss decides.
  const [lost, setLost] = useState(false);
  const down = state !== "connected" && state !== "idle";
  useEffect(() => {
    if (!down) {
      setLost(false);
      return;
    }
    const timer = window.setInterval(() => {
      if (lostAt.current && Date.now() - lostAt.current.getTime() >= LOST_AFTER_MS)
        setLost(true);
    }, 1000);
    return () => window.clearInterval(timer);
  }, [down]);
  const reconnect = useCallback(() => {
    if (sessionGeneration) void runtime.realtime.connect({ sessionGeneration });
  }, [runtime.realtime, sessionGeneration]);
  return {
    state: lost && down ? "offline" : state,
    lostAt: lostAt.current,
    reconnect,
  };
}
