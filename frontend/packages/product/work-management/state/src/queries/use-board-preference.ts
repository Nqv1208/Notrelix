import { useCallback, useMemo, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type { SavedFilterCriteria } from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";
import {
  createPreferenceSaver,
  type PreferenceStatus,
  type PreferenceTarget,
} from "./preference-saver";

export type { PreferenceSaveIssue, PreferenceTarget } from "./preference-saver";

/**
 * The signed-in member's personal state of one view (filter, sort, grouping,
 * swimlane) as saved criteria. Any board viewer may read and write it: it is
 * theirs alone and never changes the shared view. `viewId` is absent while the
 * board has no saved view of the open layout: the state then lives on screen only.
 *
 * `save` takes the view it saves to, so a save made on one view and sent after
 * a switch still lands on that view. Saves are single-flight (see the saver).
 */
export function useBoardPreference(
  boardId: string,
  workspaceId: string,
  viewId: string | undefined,
  /** Called when a save made on another view (one the member left) fails: its notice would otherwise be lost. */
  onLeftViewFailure?: (status: PreferenceStatus) => void,
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

  const [status, setStatus] = useState<PreferenceStatus | null>(null);
  const lostRef = useRef(onLeftViewFailure);
  lostRef.current = onLeftViewFailure;
  const current = useRef({ workspaceId, boardId, viewId });
  current.current = { workspaceId, boardId, viewId };
  const saver = useMemo(
    () =>
      createPreferenceSaver({
        send: (target, json) =>
          boardPreferences.saveCriteria(target.boardId, target.viewId, json),
        cache: (target, criteria) =>
          queryClient.setQueryData(
            wmQueryKeys.boardPreference(
              target.workspaceId,
              target.boardId,
              target.viewId,
            ),
            criteria,
          ),
        report: (next) => {
          setStatus(next);
          const now = current.current;
          const here =
            next.target.workspaceId === now.workspaceId &&
            next.target.boardId === now.boardId &&
            next.target.viewId === now.viewId;
          if (!here && (next.failed || next.issue)) lostRef.current?.(next);
        },
      }),
    [boardPreferences, queryClient],
  );

  const save = useCallback(
    (
      criteria: SavedFilterCriteria,
      target: PreferenceTarget | undefined = viewId
        ? { workspaceId, boardId, viewId }
        : undefined,
    ) => {
      if (target && workspaceId) saver.save(criteria, target);
    },
    [boardId, saver, viewId, workspaceId],
  );

  const mine =
    status &&
    status.target.boardId === boardId &&
    status.target.viewId === viewId
      ? status
      : null;
  return {
    /** What was saved; `undefined` until read (or when there is no saved view to hold it). */
    criteria: query.data,
    isLoading: enabled && query.isLoading,
    persistent: enabled,
    save,
    saving: mine?.pending ?? false,
    saveFailed: mine?.failed ?? false,
    saveIssue: mine?.issue ?? null,
    retry: saver.retry,
  };
}
