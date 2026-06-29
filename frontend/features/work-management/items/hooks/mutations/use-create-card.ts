"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { cardApi } from "@/features/work-management/items/api/item.api"
import type { CreateCardInput } from "@/features/work-management/schemas"
import type { Card, FullBoardResponse } from "@/features/work-management/types"

type CreateCardContext = {
  previous?: FullBoardResponse
  optimisticId: string
}

export function useCreateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<Card, Error, CreateCardInput, CreateCardContext>({
    mutationFn: (payload) => cardApi.createCard(boardId, payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      const optimisticId = `optimistic-${Date.now()}`

      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        const targetGroup = old.groups.find((group) => group.id === payload.listId)
        if (!targetGroup) return old
        const optimisticCard = createOptimisticCard(old, payload, optimisticId)
        return {
          ...old,
          groups: old.groups.map((group) =>
            group.id === payload.listId
              ? {
                  ...group,
                  cards: [...group.cards, optimisticCard].sort((a, b) => a.position - b.position),
                }
              : group
          ),
        }
      })

      return { previous, optimisticId }
    },
    onError: (_error, _payload, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to add task. Changes reverted.")
    },
    onSuccess: (card, _payload, context) => {
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        return {
          ...old,
          groups: old.groups.map((group) => ({
            ...group,
            cards: group.cards
              .map((item) => (item.id === context?.optimisticId ? card : item))
              .sort((a, b) => a.position - b.position),
          })),
        }
      })
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}

function createOptimisticCard(fullBoard: FullBoardResponse, payload: CreateCardInput, id: string): Card {
  const targetGroup = fullBoard.groups.find((group) => group.id === payload.listId)
  const title = payload.title.trim()
  const position = payload.position ?? ((targetGroup?.cards.at(-1)?.position ?? 0) + 1)
  const status = statusForGroup(targetGroup?.title ?? "")
  const now = new Date().toISOString()

  return {
    id,
    listId: payload.listId,
    boardId: fullBoard.board.id,
    workspaceId: fullBoard.board.workspaceId,
    title,
    descriptionMd: "",
    position,
    priority: "medium",
    status,
    dueDate: undefined,
    startDate: undefined,
    completedAt: status === "status-done" || status === "status-completed" ? now : undefined,
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {
      [`${fullBoard.board.id}-field-title`]: title,
      [`${fullBoard.board.id}-field-person`]: [],
      [`${fullBoard.board.id}-field-status`]: status,
      [`${fullBoard.board.id}-field-priority`]: "medium",
      [`${fullBoard.board.id}-field-due-date`]: undefined,
      [`${fullBoard.board.id}-field-linked-page`]: undefined,
      [`${fullBoard.board.id}-field-progress`]: 0,
    },
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: now,
    updatedAt: now,
  }
}

function statusForGroup(groupTitle: string) {
  const normalized = groupTitle.toLowerCase()
  if (normalized.includes("working")) return "status-working"
  if (normalized.includes("stuck")) return "status-stuck"
  if (normalized.includes("completed")) return "status-completed"
  if (normalized.includes("done")) return "status-done"
  return "status-not-started"
}
