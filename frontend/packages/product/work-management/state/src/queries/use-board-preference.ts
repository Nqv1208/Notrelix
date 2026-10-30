import { useCallback, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  serializeSavedFilterCriteria,
  type SavedFilterCriteria,
} from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

/**
 * The signed-in member's personal state of one view (filter, sort, grouping,
 * swimlane) as saved criteria. Any board viewer may read and write it: it is
 * theirs alone and never changes the shared view. `viewId` is absent while the
 * board has no saved view of the open layout: the state then lives on screen only.
 */
export function useBoardPreference(
  boardId: string,
  workspaceId: string,
  viewId: string | undefined,
) {
  const queryClient = useQueryClient();
  const { boardPreferences } = useWorkManagementServices();
  const enabled = Boolean(boardId && workspaceId && viewId);
  const queryKey = wmQueryKeys.boardPreference(
    workspaceId,
    boardId,
    viewId ?? "none",
  );
  const query = useQuery<SavedFilterCriteria>({
    queryKey,
    queryFn: () => boardPreferences.get(boardId, viewId!),
    enabled,
    staleTime: 60_000,
  });

  const lastSent = useRef<SavedFilterCriteria | null>(null);
  const [failed, setFailed] = useState(false);
  const mutation = useMutation({
    mutationFn: (criteria: SavedFilterCriteria) =>
      boardPreferences.saveCriteria(
        boardId,
        viewId!,
        Object.keys(criteria).length === 0
          ? ""
          : serializeSavedFilterCriteria(criteria),
      ),
    onMutate: (criteria) => {
      lastSent.current = criteria;
      setFailed(false);
      queryClient.setQueryData(queryKey, criteria);
    },
    // No silent revert: the member keeps what they see and gets "Thử lại".
    onError: () => setFailed(true),
  });
  const { mutate } = mutation;

  const save = useCallback(
    (criteria: SavedFilterCriteria) => {
      if (enabled) mutate(criteria);
    },
    [enabled, mutate],
  );
  const retry = useCallback(() => {
    if (lastSent.current) save(lastSent.current);
  }, [save]);

  return {
    /** What was saved; `undefined` until read (or when there is no saved view to hold it). */
    criteria: query.data,
    isLoading: enabled && query.isLoading,
    persistent: enabled,
    save,
    saving: mutation.isPending,
    saveFailed: failed,
    retry,
  };
}
