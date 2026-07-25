import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/work-management-core"
import { checklistApi } from "../api/checklist.api"
import type { CreateChecklistInput, UpdateChecklistInput, CreateChecklistItemInput, UpdateChecklistItemInput } from "../api/checklist.api"

export function useCardChecklists(cardId: string, boardId?: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const detailKey = queryKeys.cards.detail(cardId)
  const fullBoardKey = boardId ? queryKeys.boards.fullBoard(boardId, workspaceId) : null

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: detailKey })
    if (fullBoardKey) {
      void queryClient.invalidateQueries({ queryKey: fullBoardKey })
    }
  }

  const createChecklistMutation = useMutation({
    mutationFn: (title: string) => checklistApi.createChecklist({ cardId, title }),
    onSuccess: () => {
      toast.success("Checklist created.")
      invalidate()
    },
    onError: () => {
      toast.error("Failed to create checklist.")
    },
  })

  const updateChecklistMutation = useMutation({
    mutationFn: (input: Omit<UpdateChecklistInput, "cardId">) => checklistApi.updateChecklist(input),
    onSuccess: () => {
      invalidate()
    },
    onError: () => {
      toast.error("Failed to update checklist.")
    },
  })

  const deleteChecklistMutation = useMutation({
    mutationFn: (checklistId: string) => checklistApi.deleteChecklist(checklistId),
    onSuccess: () => {
      toast.success("Checklist deleted.")
      invalidate()
    },
    onError: () => {
      toast.error("Failed to delete checklist.")
    },
  })

  const createItemMutation = useMutation({
    mutationFn: (input: { checklistId: string; title: string }) => checklistApi.createChecklistItem(input),
    onSuccess: () => {
      invalidate()
    },
    onError: () => {
      toast.error("Failed to create checklist item.")
    },
  })

  const updateItemMutation = useMutation({
    mutationFn: (input: UpdateChecklistItemInput) => checklistApi.updateChecklistItem(input),
    onSuccess: () => {
      invalidate()
    },
    onError: () => {
      toast.error("Failed to update checklist item.")
    },
  })

  const deleteItemMutation = useMutation({
    mutationFn: (itemId: string) => checklistApi.deleteChecklistItem(itemId),
    onSuccess: () => {
      invalidate()
    },
    onError: () => {
      toast.error("Failed to delete checklist item.")
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
