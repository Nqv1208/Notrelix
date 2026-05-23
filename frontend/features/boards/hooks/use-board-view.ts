"use client"

import { useCallback, useMemo } from "react"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { boardApi, defaultTableViewConfig } from "../api/board.api"
import type { ViewConfig, ViewMode } from "../types"

type BoardViewState = {
  viewMode: ViewMode
  viewConfig: ViewConfig
}

export function useBoardView(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.view(workspaceId ?? "workspace", boardId)

  const query = useQuery({
    queryKey,
    queryFn: () => boardApi.getBoardView(boardId),
    enabled: Boolean(boardId),
    staleTime: 30_000,
  })

  const currentState = useMemo<BoardViewState>(
    () => query.data ?? { viewMode: "table", viewConfig: defaultTableViewConfig },
    [query.data]
  )

  const saveMutation = useMutation({
    mutationFn: (next: BoardViewState) => boardApi.saveBoardView(boardId, next),
    onMutate: async (next) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<BoardViewState>(queryKey)
      queryClient.setQueryData<BoardViewState>(queryKey, next)
      return { previous }
    },
    onError: (_error, _next, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to save table view. Changes reverted.")
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
