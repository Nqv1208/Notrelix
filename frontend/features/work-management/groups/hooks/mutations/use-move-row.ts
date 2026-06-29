"use client"

import { useMoveCard } from "@/features/work-management/items/hooks/mutations/use-move-card"

export function useMoveRow(boardId: string, workspaceId?: string) {
  return useMoveCard(boardId, workspaceId)
}
