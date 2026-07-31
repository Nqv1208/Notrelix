import { useMoveCard } from "./use-move-card"

export function useMoveRow(boardId: string, workspaceId?: string) {
  return useMoveCard(boardId, workspaceId)
}
