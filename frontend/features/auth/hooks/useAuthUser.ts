"use client"

import { useEffect, useState } from "react"
import { useQuery } from "@tanstack/react-query"
import { authService } from "@/features/auth/api/auth.service"
import { tokenStorage } from "@/lib/auth/token-storage"
import { ApiError } from "@/lib/api/api-error"

export function useAuthUser() {
  const [accessToken, setAccessToken] = useState<string | null>(null)
  const [isReady, setIsReady] = useState(false)

  useEffect(() => {
    setAccessToken(tokenStorage.getAccessToken())
    setIsReady(true)
    return tokenStorage.onTokenChanged(() => {
      setAccessToken(tokenStorage.getAccessToken())
    })
  }, [])

  const profileQuery = useQuery({
    queryKey: ["auth", "profile", accessToken],
    queryFn: () => authService.profile(),
    enabled: Boolean(accessToken),
    retry: false,
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false
  })

  useEffect(() => {
    const isAuthError =
      profileQuery.error instanceof ApiError &&
      (profileQuery.error.status === 401 || profileQuery.error.status === 403)

    if (isAuthError && accessToken) {
      tokenStorage.clearTokens()
    }
  }, [profileQuery.error, accessToken])

  return {
    user: profileQuery.data,
    isAuthenticated: Boolean(accessToken),
    isLoading: Boolean(accessToken) && profileQuery.isLoading,
    isReady
  }
}
