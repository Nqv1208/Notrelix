"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"

export function useAcceptInvitation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (token: string) => workspaceService.acceptInvitation(token),
    onSuccess: (data) => {
      toast.success("Đã chấp nhận lời mời tham gia Workspace thành công!")
      // Invalidate danh sách workspaces để update sidebar
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.all })
    },
    onError: (error: any) => {
      const message = error?.response?.data?.detail || error?.message || "Không thể chấp nhận lời mời."
      toast.error(message)
    },
  })
}
