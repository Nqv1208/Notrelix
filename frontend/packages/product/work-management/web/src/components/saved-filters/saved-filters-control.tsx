import { useCallback } from "react";
import { toast } from "sonner";
import {
  useSavedFilters,
  useSavedFilterWrites,
} from "@notrelix/work-management-state";
import type {
  SavedFilter,
  SavedFilterCriteria,
} from "@notrelix/work-management-core";
import { fillSavedFiltersCopy } from "./saved-filters.copy";
import { SavedFiltersMenu } from "./saved-filters-menu";
import { useSavedFiltersCopy } from "./use-saved-filters-copy";

export interface SavedFiltersControlProps {
  workspaceId: string;
  boardId: string;
  /** The route `filter` string the bar shows now (`serializeKanbanFilter` form). */
  filter: string | undefined;
  /** The grouping field the view shows now; absent for a view that has none. */
  groupBy?: string | null;
  /** The viewer may save and change filters. Shapes the controls only; the server decides. */
  canEdit: boolean;
  /**
   * Puts a saved filter on the bar: the route filter and, when it kept one,
   * the grouping. Never writes the board's shared view.
   */
  onApply: (criteria: SavedFilterCriteria) => void;
}

/** A saved filter is applied when the bar already shows its filter (and its grouping, if it kept one). */
function isApplied(
  saved: SavedFilter,
  filter: string | undefined,
  groupBy: string | null | undefined,
): boolean {
  if ((saved.criteria.filter ?? "") !== (filter ?? "")) return false;
  return !saved.criteria.groupBy || saved.criteria.groupBy === groupBy;
}

/**
 * The saved filters of a board in the filter bar of every view: reads and
 * writes them through the work-management state and applies one to the route
 * filter (and grouping) of the view. Deleting offers "Hoàn tác" (restore).
 */
export function SavedFiltersControl({
  workspaceId,
  boardId,
  filter,
  groupBy,
  canEdit,
  onApply,
}: SavedFiltersControlProps) {
  const copy = useSavedFiltersCopy();
  const query = useSavedFilters(boardId, workspaceId);
  const writes = useSavedFilterWrites(boardId, workspaceId);
  const filters = query.data ?? [];
  const applied = filters.find((saved) => isApplied(saved, filter, groupBy));
  const currentCriteria: SavedFilterCriteria = {
    ...(filter ? { filter } : {}),
    ...(groupBy ? { groupBy } : {}),
  };

  const tell = useCallback(
    (
      result: { ok: boolean; reason?: "forbidden" | "missing" | "failed" },
      name: string,
    ) => {
      if (result.ok) return;
      toast.error(
        fillSavedFiltersCopy(
          result.reason === "forbidden"
            ? copy.toast.forbidden
            : result.reason === "missing"
              ? copy.menu.gone
              : copy.toast.failed,
          { name },
        ),
      );
    },
    [copy],
  );

  return (
    <SavedFiltersMenu
      copy={copy}
      status={
        query.isError && filters.length === 0
          ? "error"
          : query.isLoading
            ? "loading"
            : "ready"
      }
      filters={filters}
      appliedId={applied?.id ?? null}
      canEdit={canEdit}
      canSaveCurrent={Boolean(filter) && applied === undefined}
      onApply={(saved) => onApply(saved.criteria)}
      onClearApplied={() => onApply({})}
      onRetry={() => void query.refetch()}
      onSave={async ({ name, visibility }) => {
        const result = await writes.create(name, currentCriteria, visibility);
        if (result.ok) {
          toast.success(
            fillSavedFiltersCopy(
              visibility === "board"
                ? copy.toast.savedShared
                : copy.toast.saved,
              { name },
            ),
          );
        }
        return result;
      }}
      onRename={async (saved, name) => {
        const result = await writes.rename(saved, name);
        if (result.ok)
          toast.success(fillSavedFiltersCopy(copy.toast.renamed, { name }));
        return result;
      }}
      onSetVisibility={(saved, visibility) =>
        void writes.setVisibility(saved, visibility).then((result) => {
          if (result.ok)
            toast.success(
              fillSavedFiltersCopy(
                visibility === "board"
                  ? copy.toast.shared
                  : copy.toast.unshared,
                { name: saved.name },
              ),
            );
          else tell(result, saved.name);
        })
      }
      onRemove={(saved) =>
        void writes.remove(saved).then((result) => {
          if (!result.ok) return tell(result, saved.name);
          toast.success(
            fillSavedFiltersCopy(copy.toast.deleted, { name: saved.name }),
            {
              action: {
                label: copy.toast.undo,
                onClick: () =>
                  void writes.restore(saved.id).then((restored) => {
                    if (restored.ok)
                      toast.success(
                        fillSavedFiltersCopy(copy.toast.restored, {
                          name: saved.name,
                        }),
                      );
                    else
                      toast.error(
                        fillSavedFiltersCopy(copy.toast.restoreFailed, {
                          name: saved.name,
                        }),
                      );
                  }),
              },
            },
          );
        })
      }
    />
  );
}
