import {
  createContext,
  createElement,
  useCallback,
  useContext,
  useMemo,
  useRef,
  type ReactNode,
} from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { defaultTableViewConfig } from "../api/board.api";
import type {
  SavedBoardView,
  ViewConfig,
  ViewMode,
} from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";

/** Which saved view of the board the screen shows. */
export interface BoardViewSelection {
  /** The saved view picked in the tabs; absent: the first one of `mode`, else the default. */
  readonly viewId?: string | undefined;
  /** The layout the screen shows. */
  readonly mode?: ViewMode | undefined;
}

const BoardViewSelectionContext = createContext<BoardViewSelection>({});

/** Gives the board's screens the saved view they read and write. */
export function BoardViewSelectionProvider({
  viewId,
  mode,
  children,
}: BoardViewSelection & { readonly children: ReactNode }) {
  const value = useMemo(() => ({ viewId, mode }), [viewId, mode]);
  return createElement(BoardViewSelectionContext.Provider, { value }, children);
}

/** The saved view a selection names (see `BoardViewSelection`). */
export function pickSavedView(
  views: readonly SavedBoardView[],
  selection: BoardViewSelection,
): SavedBoardView | undefined {
  const byId = selection.viewId
    ? views.find((view) => view.id === selection.viewId)
    : undefined;
  if (byId) return byId;
  if (selection.mode)
    return views.find((view) => view.viewMode === selection.mode);
  return views[0];
}

const LAYOUT_NAME: Record<ViewMode, string> = {
  table: "Table",
  kanban: "Kanban",
  calendar: "Calendar",
  timeline: "Timeline",
};

/** The saved views of a board and the writes that change them. */
export function useSavedViews(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { boards } = useWorkManagementServices();
  const queryKey = wmQueryKeys.view(workspaceId ?? "workspace", boardId);
  const refresh = () => queryClient.invalidateQueries({ queryKey });

  const query = useQuery({
    queryKey,
    queryFn: () => boards.listSavedViews(boardId),
    enabled: Boolean(boardId),
    staleTime: 30_000,
  });

  const create = useMutation({
    mutationFn: (input: {
      name?: string;
      viewMode: ViewMode;
      viewConfig?: ViewConfig;
      visibility?: "board" | "me";
    }) =>
      boards.createSavedView(boardId, {
        name: input.name ?? LAYOUT_NAME[input.viewMode],
        viewMode: input.viewMode,
        viewConfig: input.viewConfig ?? defaultTableViewConfig,
        visibility: input.visibility ?? "board",
      }),
    onSuccess: (created) => {
      // The new view is the screen's right away: a second change must save to
      // it, not make another.
      queryClient.setQueryData<SavedBoardView[]>(queryKey, (views) =>
        views?.some((view) => view.id === created.id)
          ? views
          : [...(views ?? []), created],
      );
    },
    onSettled: refresh,
  });
  const rename = useMutation({
    mutationFn: (input: { viewId: string; name: string }) =>
      boards.renameSavedView(boardId, input.viewId, input.name),
    onSettled: refresh,
  });
  const setVisibility = useMutation({
    mutationFn: (input: { viewId: string; visibility: "board" | "me" }) =>
      boards.setSavedViewVisibility(boardId, input.viewId, input.visibility),
    onSettled: refresh,
  });
  const setDefault = useMutation({
    mutationFn: (viewId: string) => boards.setDefaultSavedView(boardId, viewId),
    onSettled: refresh,
  });
  const remove = useMutation({
    mutationFn: (viewId: string) => boards.deleteSavedView(boardId, viewId),
    onSettled: refresh,
  });
  const saveConfig = useMutation({
    mutationFn: (input: { viewId: string; viewConfig: ViewConfig }) =>
      boards.updateSavedViewConfig(boardId, input.viewId, input.viewConfig),
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<SavedBoardView[]>(queryKey);
      queryClient.setQueryData<SavedBoardView[]>(queryKey, (views) =>
        views?.map((view) =>
          view.id === input.viewId
            ? { ...view, viewConfig: input.viewConfig }
            : view,
        ),
      );
      return { previous };
    },
    onError: (_error, _input, context) => {
      queryClient.setQueryData(queryKey, context?.previous);
    },
    onSettled: refresh,
  });

  return {
    views: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    refetch: query.refetch,
    create: create.mutateAsync,
    rename: rename.mutateAsync,
    setVisibility: setVisibility.mutateAsync,
    setDefault: setDefault.mutateAsync,
    remove: remove.mutateAsync,
    saveConfig: saveConfig.mutate,
    isSaving: saveConfig.isPending || create.isPending,
  };
}

/**
 * The saved view a board screen reads and writes: its layout and config. The
 * view comes from the selection (the tabs) given here or by
 * `BoardViewSelectionProvider`. A board with no view of that layout shows the
 * defaults, and the first change saves a view of that layout.
 */
export function useBoardView(
  boardId: string,
  workspaceId?: string,
  selection?: BoardViewSelection,
) {
  const inherited = useContext(BoardViewSelectionContext);
  const wanted = selection ?? inherited;
  const saved = useSavedViews(boardId, workspaceId);
  const active = pickSavedView(saved.views, wanted);
  const viewMode: ViewMode = active?.viewMode ?? wanted.mode ?? "table";
  const viewConfig = active?.viewConfig ?? defaultTableViewConfig;
  const { create, saveConfig } = saved;

  const creating = useRef<Promise<SavedBoardView> | null>(null);

  const writeConfig = useCallback(
    (next: ViewConfig) => {
      if (active) {
        saveConfig({ viewId: active.id, viewConfig: next });
        return;
      }
      if (creating.current) {
        // The view this change belongs to is being made: save to it after.
        void creating.current
          .then((made) => saveConfig({ viewId: made.id, viewConfig: next }))
          .catch(() => undefined);
        return;
      }
      const made = create({ viewMode, viewConfig: next });
      creating.current = made;
      void made
        .catch(() => undefined)
        .finally(() => {
          creating.current = null;
        });
    },
    [active, create, saveConfig, viewMode],
  );

  const updateViewConfig = useCallback(
    (patch: Partial<ViewConfig>) => writeConfig({ ...viewConfig, ...patch }),
    [viewConfig, writeConfig],
  );

  return useMemo(
    () => ({
      viewId: active?.id,
      viewMode,
      viewConfig,
      updateViewConfig,
      isLoading: saved.isLoading,
      error: saved.error,
      isSaving: saved.isSaving,
    }),
    [
      active?.id,
      saved.error,
      saved.isLoading,
      saved.isSaving,
      updateViewConfig,
      viewConfig,
      viewMode,
    ],
  );
}
