import { useMemo } from "react";
import {
  isVersionConflict,
  useSavedViews,
} from "@notrelix/work-management-state";
import type { ViewTabActions, ViewWriteOutcome } from "./view-tab-menu";

async function outcomeOf(
  send: () => Promise<unknown>,
): Promise<ViewWriteOutcome> {
  try {
    await send();
    return "ok";
  } catch (error) {
    return isVersionConflict(error) ? "conflict" : "failed";
  }
}

/**
 * The tab strip's writes on the saved views. Each names the version the list last
 * read, so a stale one changes nothing and answers "conflict"; the list is read
 * again after every write. `onLeave` runs when the opened view is archived or
 * deleted: at the start, so the screen does not take its going for a vanished
 * address, and when done, so the person lands on the default view.
 */
export function useViewTabActions(
  boardId: string,
  workspaceId: string,
  activeViewId: string | undefined,
  onLeave: (viewId: string, phase: "start" | "done") => void,
): ViewTabActions {
  const saved = useSavedViews(boardId, workspaceId, { withArchived: true });
  const { setDefault, archive, remove, unarchive, archivedViews } = saved;
  return useMemo(
    () => ({
      archivedViews,
      onSetDefault: (viewId) => outcomeOf(() => setDefault(viewId)),
      onArchive: async (viewId, newDefaultViewId) => {
        if (viewId === activeViewId) onLeave(viewId, "start");
        const outcome = await outcomeOf(() =>
          archive({ viewId, newDefaultViewId }),
        );
        if (outcome === "ok" && viewId === activeViewId)
          onLeave(viewId, "done");
        return outcome;
      },
      onDelete: async (viewId, newDefaultViewId) => {
        if (viewId === activeViewId) onLeave(viewId, "start");
        const outcome = await outcomeOf(() =>
          remove({ viewId, newDefaultViewId }),
        );
        if (outcome === "ok" && viewId === activeViewId)
          onLeave(viewId, "done");
        return outcome;
      },
      onRestore: (viewId) => outcomeOf(() => unarchive(viewId)),
    }),
    [
      archivedViews,
      setDefault,
      archive,
      remove,
      unarchive,
      activeViewId,
      onLeave,
    ],
  );
}
