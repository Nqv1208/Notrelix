import { useEffect, useState } from "react";
import { onlineManager, useMutationState } from "@tanstack/react-query";

/**
 * Whether the page can reach the server, and the variables of the writes the
 * network holds back (paused mutations): the Detail page counts and marks
 * them while it is offline.
 */
export function useOfflineWrites(): {
  readonly online: boolean;
  readonly paused: readonly unknown[];
} {
  const [online, setOnline] = useState(() => onlineManager.isOnline());
  useEffect(() => onlineManager.subscribe(setOnline), []);
  const paused = useMutationState({
    filters: { predicate: (mutation) => mutation.state.isPaused },
    select: (mutation) => mutation.state.variables as unknown,
  });
  return { online, paused };
}
