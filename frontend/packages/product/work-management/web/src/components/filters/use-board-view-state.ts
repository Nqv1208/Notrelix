import { useMemo } from "react";
import {
  kanbanFilterChoices,
  useFullBoard,
  type UseBoardKanbanView,
} from "@notrelix/work-management-state";
import { bucketLabel } from "./condition-label";
import { useFiltersCopy } from "./use-filters-copy";
import { useViewState, type UseViewStateOptions } from "./use-view-state";

/**
 * A view's personal state plus what the shared board hook needs to apply it:
 * the state hook, the filter-field names from the active locale and the
 * date-bucket labels. Pass `kanbanView` to `useBoardTable` / `useBoardKanban`.
 */
export function useBoardViewState(
  options: Omit<UseViewStateOptions, "fieldDefinitions">,
) {
  const copy = useFiltersCopy();
  const { board, groups } = useFullBoard(options.boardId, options.workspaceId);
  const filterChoices = useMemo(
    () => (board ? kanbanFilterChoices(board, groups) : undefined),
    [board, groups],
  );
  const view = useViewState({
    ...options,
    fieldDefinitions: board?.fieldDefinitions,
    filterChoices,
  });
  const kanbanView = useMemo<UseBoardKanbanView>(
    () => ({
      state: view.applied,
      names: {
        title: copy.field.title,
        label: copy.field.label,
        assignee: copy.field.assignee,
        overdue: copy.filter.presets.overdue,
      },
      bucketLabel: (key, bucket) => bucketLabel(copy, key, bucket),
    }),
    [view.applied, copy],
  );
  return { copy, view, kanbanView };
}
