"use client"

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { notificationsService } from "../api/notifications.service"
import { useAuthUser } from "@/features/auth/hooks/useAuthUser"
import { toast } from "sonner"

export function useNotifications() {
  const { isAuthenticated } = useAuthUser()

  return useQuery({
    queryKey: queryKeys.notifications.all,
    queryFn: () => notificationsService.list(),
    enabled: isAuthenticated,
    staleTime: 15 * 1000, // 15 seconds
    // refetchInterval: 20 * 1000, // Refetch every 20 seconds
    // refetchIntervalInBackground: false,
  })
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => notificationsService.read(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all })
    },
  })
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => notificationsService.readAll(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all })
      toast.success("Đã đánh dấu đọc tất cả thông báo.")
    },
    onError: (err: any) => {
      toast.error(err?.message || "Không thể đánh dấu đọc tất cả.")
    },
  })
}
