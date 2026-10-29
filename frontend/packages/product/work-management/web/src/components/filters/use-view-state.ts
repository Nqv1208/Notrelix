import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  useBoardPreference,
  useBoardView,
} from "@notrelix/work-management-state";
import {
  effectiveFilter,
  emptyCompoundFilter,
  emptyViewState,
  isViewStateEmpty,
  legacyFacetsToCompound,
  parseCompoundFilter,
  serializeCompoundFilter,
  viewStateFromCriteria,
  viewStateToCriteria,
  type CompoundFilter,
  type FieldDefinition,
  type FieldRef,
  type SavedFilterCriteria,
  type SortLevel,
  type ViewState,
} from "@notrelix/work-management-core";
import { parseKanbanFilter } from "../views/kanban/kanban-filter";

type ParamChange = (
  value: string | undefined,
  options?: { replace?: boolean },
) => void;

export interface UseViewStateOptions {
  boardId: string;
  workspaceId: string;
  /** The route `filter` parameter: a compound (`c2:`) string or an older facet string. */
  routeFilter: string | undefined;
  onRouteFilterChange: ParamChange;
  /** The route `groupBy` parameter (a field id) and its writer; absent for a view without one. */
  routeGroupBy?: string | undefined;
  onRouteGroupByChange?: ((fieldId: string) => void) | undefined;
  /** The board's field definitions; the older facet filter is converted once they are known. */
  fieldDefinitions: readonly FieldDefinition[] | undefined;
}

const SAVE_DELAY_MS = 400;

/**
 * The member's own state of the open view: the compound filter, up to three
 * sort levels, the grouping and the swimlane field.
 *
 * Sources: the route `filter` and `groupBy` parameters (so a link or reload
 * keeps them), else what the member last saved for this view (BoardPreference
 * criteria, any viewer may). A change updates the screen at once, rewrites the
 * route parameters and saves the criteria after a short pause; a failed save
 * leaves the screen as it is and offers "Thử lại". "Về view chung" clears all
 * of it, including the saved criteria. The shared view is never written.
 */
export function useViewState(options: UseViewStateOptions) {
  const {
    boardId,
    workspaceId,
    routeFilter,
    onRouteFilterChange,
    routeGroupBy,
    onRouteGroupByChange,
    fieldDefinitions,
  } = options;
  const { viewId } = useBoardView(boardId, workspaceId);
  const preference = useBoardPreference(boardId, workspaceId, viewId);
  const personal = useMemo(
    () =>
      preference.criteria ? viewStateFromCriteria(preference.criteria) : null,
    [preference.criteria],
  );
  const loaded = !preference.isLoading;

  // The route filter as a compound filter (an older facet string is converted).
  const routeCompound = useMemo<CompoundFilter | null>(() => {
    if (routeFilter === undefined || routeFilter === "") return null;
    const compound = parseCompoundFilter(routeFilter);
    if (compound) return compound;
    if (!fieldDefinitions) return null;
    return legacyFacetsToCompound(
      parseKanbanFilter(routeFilter),
      fieldDefinitions,
    );
  }, [routeFilter, fieldDefinitions]);

  const [local, setLocal] = useState<ViewState | null>(null);

  const state = useMemo<ViewState>(() => {
    if (local) return local;
    const base = personal ?? emptyViewState();
    const group = routeGroupBy
      ? {
          fieldId: routeGroupBy,
          ...(base.group?.fieldId === routeGroupBy && base.group.bucket
            ? { bucket: base.group.bucket }
            : {}),
        }
      : base.group;
    return {
      filter: routeCompound ?? (routeFilter ? emptyCompoundFilter() : base.filter),
      sorts: base.sorts,
      ...(group ? { group } : {}),
      ...(base.lane ? { lane: base.lane } : {}),
    };
  }, [local, personal, routeCompound, routeFilter, routeGroupBy]);

  // An older facet string in the route is rewritten once to its compound form.
  useEffect(() => {
    if (!routeFilter || parseCompoundFilter(routeFilter) || !routeCompound)
      return;
    onRouteFilterChange(serializeCompoundFilter(routeCompound) || undefined, {
      replace: true,
    });
  }, [routeFilter, routeCompound, onRouteFilterChange]);

  const timer = useRef<number | null>(null);
  const pending = useRef<ViewState | null>(null);
  const { save } = preference;
  const flush = useCallback(() => {
    if (timer.current !== null) window.clearTimeout(timer.current);
    timer.current = null;
    const next = pending.current;
    pending.current = null;
    if (next) save(viewStateToCriteria(next, { withSort: true }));
  }, [save]);
  useEffect(() => flush, [flush]);

  const commit = useCallback(
    (next: ViewState) => {
      setLocal(next);
      onRouteFilterChange(serializeCompoundFilter(next.filter) || undefined);
      if (onRouteGroupByChange && next.group?.fieldId !== state.group?.fieldId)
        onRouteGroupByChange(next.group?.fieldId ?? "");
      pending.current = next;
      if (timer.current !== null) window.clearTimeout(timer.current);
      timer.current = window.setTimeout(flush, SAVE_DELAY_MS);
    },
    [flush, onRouteFilterChange, onRouteGroupByChange, state.group?.fieldId],
  );

  const setFilter = useCallback(
    (filter: CompoundFilter) => commit({ ...state, filter }),
    [commit, state],
  );
  const setSorts = useCallback(
    (sorts: SortLevel[]) => commit({ ...state, sorts }),
    [commit, state],
  );
  const setGroup = useCallback(
    (group: FieldRef | undefined) => {
      const { group: _old, ...rest } = state;
      commit(group ? { ...rest, group } : rest);
    },
    [commit, state],
  );
  const setLane = useCallback(
    (lane: FieldRef | undefined) => {
      const { lane: _old, ...rest } = state;
      commit(lane ? { ...rest, lane } : rest);
    },
    [commit, state],
  );
  /** Puts a saved filter on the bar: its filter, grouping and swimlane; the member's own sort stays. */
  const applyCriteria = useCallback(
    (criteria: SavedFilterCriteria) => {
      const next = viewStateFromCriteria(criteria);
      const legacy =
        criteria.filter && !parseCompoundFilter(criteria.filter)
          ? fieldDefinitions
            ? legacyFacetsToCompound(
                parseKanbanFilter(criteria.filter),
                fieldDefinitions,
              )
            : emptyCompoundFilter()
          : next.filter;
      commit({
        filter: legacy,
        sorts: state.sorts,
        ...(next.group ? { group: next.group } : {}),
        ...(next.lane ? { lane: next.lane } : {}),
      });
    },
    [commit, fieldDefinitions, state.sorts],
  );
  const reset = useCallback(() => {
    commit(emptyViewState());
    flush();
  }, [commit, flush]);

  const applied = useMemo<ViewState>(
    () => ({ ...state, filter: effectiveFilter(state.filter) }),
    [state],
  );

  return {
    /** The state with every condition, also incomplete rows (what the builder edits). */
    state,
    /** The state that applies: incomplete or invalid conditions left out. */
    applied,
    /** True once the saved criteria were read (or there is no saved view to read them from). */
    loaded,
    /** The member has a view of their own: any filter, sort, grouping or swimlane. */
    personal: !isViewStateEmpty(state),
    setFilter,
    setSorts,
    setGroup,
    setLane,
    applyCriteria,
    reset,
    persistent: preference.persistent,
    saveFailed: preference.saveFailed,
    retrySave: preference.retry,
  };
}

export type ViewStateControl = ReturnType<typeof useViewState>;
