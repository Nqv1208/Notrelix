"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { boardsApi } from "../api/boards-api"

export function useFullBoard(boardId: string) {
  const query = useQuery({
    queryKey: queryKeys.boards.fullBoard(boardId),
    queryFn: () => boardsApi.getFullBoard(boardId),
    enabled: Boolean(boardId),
    staleTime: 10_000,
  })

  return {
    board: query.data?.board,
    groups: query.data?.groups ?? [],
    fieldDefinitions: query.data?.fieldDefinitions ?? [],
    isLoading: query.isLoading,
    error: query.error,
  }
}
