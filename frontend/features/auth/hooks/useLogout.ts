"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useRouter } from "next/navigation"
import { authService } from "@/features/auth/api/auth.service"
import { tokenStorage } from "@/lib/auth/token-storage"
import { routes } from "@/lib/routes"

export function useLogout() {
  const router = useRouter()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => {
      const refreshToken = tokenStorage.getRefreshToken()
      const accessToken = tokenStorage.getAccessToken()

      if (refreshToken) {
        try {
          await authService.logout({ refreshToken, accessToken: accessToken ?? undefined })
        } catch {
          // Ignore API failures; local sign-out still proceeds.
        }
      }

      tokenStorage.clearTokens()
    },
    onSettled: () => {
      queryClient.removeQueries({ queryKey: ["auth"] })
      router.push(routes.home)
      router.refresh()
    }
  })
}
