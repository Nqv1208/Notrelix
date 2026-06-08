"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useRouter } from "next/navigation"
import { authService } from "@/features/auth/api/auth.service"
import { routes } from "@/lib/routes"
import { queryKeys } from "@/lib/query/query-keys"

export function useLogout() {
  const router = useRouter()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => {
      try {
        await authService.logout({})
      } catch {
        // Ignore API failures; local sign-out still proceeds.
      }
    },
    onSettled: () => {
      queryClient.removeQueries({ queryKey: queryKeys.auth.all })
      router.push(routes.home)
      router.refresh()
    }
  })
}
