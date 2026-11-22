import { useCallback, useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import type { CollaborationApiClient } from "../../../core/api/comment.service";
import {
  createMentionService,
  type MentionEndpoints,
  type MentionSuggestion,
  type MentionTargetKind,
} from "../../../core/api/mention.service";
import { collaborationQueryKeys } from "../../../core/query/keys";

export type MentionSuggestionsStatus = "loading" | "error" | "success";

export interface MentionSuggestionsResult {
  readonly people: readonly MentionSuggestion[];
  /** `loading`: nothing to show yet for this target; `error`: the read failed and `retry` repeats it. */
  readonly status: MentionSuggestionsStatus;
  readonly retry: () => void;
}

const DEBOUNCE_MS = 150;
const NONE: readonly MentionSuggestion[] = [];

/** The value, held back until it stops changing for `delay` ms. */
function useDebounced<T>(value: T, delay: number): T {
  const [held, setHeld] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setHeld(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);
  return held;
}

/**
 * The people a page or an item can mention, read from the server for the typed text.
 * `query` is `null` while no mention is being typed (nothing is read). The previous
 * answer stays on screen while the next one loads, but only for the same target.
 */
export function createUseMentionSuggestions(
  api: CollaborationApiClient,
  endpoints: MentionEndpoints,
) {
  const service = createMentionService(api, endpoints);
  return function useMentionSuggestions(
    workspaceId: string,
    kind: MentionTargetKind,
    targetId: string,
    query: string | null,
  ): MentionSuggestionsResult {
    const typed = query ?? "";
    const settled = useDebounced(typed, DEBOUNCE_MS);
    const result = useQuery({
      queryKey: collaborationQueryKeys.mentionSuggestions(
        workspaceId,
        kind,
        targetId,
        settled,
      ),
      queryFn: () => service.suggestions(kind, targetId, settled),
      enabled: query !== null && !!workspaceId && !!targetId,
      staleTime: 15_000,
      retry: false,
      // Another query of the same target keeps its people on screen; another target never does.
      placeholderData: (previous, previousQuery) =>
        previousQuery !== undefined &&
        previousQuery.queryKey[2] === workspaceId &&
        previousQuery.queryKey[3] === kind &&
        previousQuery.queryKey[4] === targetId
          ? previous
          : undefined,
    });
    const status: MentionSuggestionsStatus =
      result.data !== undefined
        ? "success"
        : result.isError
          ? "error"
          : "loading";
    const { refetch } = result;
    const retry = useCallback(() => void refetch(), [refetch]);
    return { people: result.data ?? NONE, status, retry };
  };
}
