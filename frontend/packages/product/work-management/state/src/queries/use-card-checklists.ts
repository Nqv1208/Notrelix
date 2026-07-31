import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import type { CreateChecklistInput, UpdateChecklistInput, CreateChecklistItemInput, UpdateChecklistItemInput } from "../api/checklist.api"
import { useWorkManagementServices } from "../services"

export function useCardChecklists(cardId: string, boardId?: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { checklists } = useWorkManagementServices()
  const detailKey = queryKeys.cards.detail(cardId)
  const fullBoardKey = boardId ? queryKeys.boards.fullBoard(boardId, workspaceId) : null

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: detailKey })
    if (fullBoardKey) {
      void queryClient.invalidateQueries({ queryKey: fullBoardKey })
    }
  }

  const createChecklistMutation = useMutation({
    mutationFn: (title: string) => checklists.createChecklist({ cardId, title }),
    onSuccess: () => {
      invalidate()
    },
  })

  const updateChecklistMutation = useMutation({
    mutationFn: (input: Omit<UpdateChecklistInput, "cardId">) => checklists.updateChecklist(input),
    onSuccess: () => {
      invalidate()
    },
  })

  const deleteChecklistMutation = useMutation({
    mutationFn: (checklistId: string) => checklists.deleteChecklist(checklistId),
    onSuccess: () => {
      invalidate()
    },
  })

  const createItemMutation = useMutation({
    mutationFn: (input: { checklistId: string; title: string }) => checklists.createChecklistItem(input),
    onSuccess: () => {
      invalidate()
    },
  })

  const updateItemMutation = useMutation({
    mutationFn: (input: UpdateChecklistItemInput) => checklists.updateChecklistItem(input),
    onSuccess: () => {
      invalidate()
    },
  })

  const deleteItemMutation = useMutation({
    mutationFn: (itemId: string) => checklists.deleteChecklistItem(itemId),
    onSuccess: () => {
      invalidate()
    },
  })

  return {
    createChecklist: createChecklistMutation.mutate,
    isCreatingChecklist: createChecklistMutation.isPending,
    updateChecklist: updateChecklistMutation.mutate,
    deleteChecklist: deleteChecklistMutation.mutate,
    createChecklistItem: createItemMutation.mutate,
    isCreatingChecklistItem: createItemMutation.isPending,
    updateChecklistItem: updateItemMutation.mutate,
    deleteChecklistItem: deleteItemMutation.mutate,
  }
}
