import { useCallback, useMemo } from "react"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { defaultTableViewConfig } from "../api/board.api"
import type { ViewConfig, ViewMode } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

type BoardViewState = {
  viewMode: ViewMode
  viewConfig: ViewConfig
}

export function useBoardView(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { boards } = useWorkManagementServices()
  const queryKey = wmQueryKeys.view(workspaceId ?? "workspace", boardId)

  const query = useQuery({
    queryKey,
    queryFn: () => boards.getBoardView(boardId),
    enabled: Boolean(boardId),
    staleTime: 30_000,
  })

  const currentState = useMemo<BoardViewState>(
    () => query.data ?? { viewMode: "table", viewConfig: defaultTableViewConfig },
    [query.data]
  )

  const saveMutation = useMutation({
    mutationFn: (next: BoardViewState) => boards.saveBoardView(boardId, next),
    onMutate: async (next) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<BoardViewState>(queryKey)
      queryClient.setQueryData<BoardViewState>(queryKey, next)
      return { previous }
    },
    onError: (_error, _next, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })

  const setViewMode = useCallback(
    (viewMode: ViewMode) => {
      saveMutation.mutate({ ...currentState, viewMode })
    },
    [currentState, saveMutation]
  )

  const updateViewConfig = useCallback(
    (patch: Partial<ViewConfig>) => {
      saveMutation.mutate({
        ...currentState,
        viewConfig: { ...currentState.viewConfig, ...patch },
      })
    },
    [currentState, saveMutation]
  )

  return useMemo(
    () => ({
      viewMode: currentState.viewMode,
      viewConfig: currentState.viewConfig,
      setViewMode,
      updateViewConfig,
      isLoading: query.isLoading,
      error: query.error,
      isSaving: saveMutation.isPending,
    }),
    [currentState, query.error, query.isLoading, saveMutation.isPending, setViewMode, updateViewConfig]
  )
}
