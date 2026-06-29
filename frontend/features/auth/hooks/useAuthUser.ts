"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { authService } from "@/features/auth/api/auth.service"


export function useAuthUser() {
  // const [accessToken, setAccessToken] = useState<string | null>(null)
  // const [isReady, setIsReady] = useState(false)

  // useEffect(() => {
  //   setAccessToken(tokenStorage.getAccessToken())
  //   setIsReady(true)
  //   return tokenStorage.onTokenChanged(() => {
  //     setAccessToken(tokenStorage.getAccessToken())
  //   })
  // }, [])

  const profileQuery = useQuery({
    queryKey: queryKeys.auth.profile,
    queryFn: () => authService.profile(),
    // enabled: Boolean(accessToken),
    retry: false,
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false
  })

  const isAuthenticated = profileQuery.isSuccess && !!profileQuery.data

  const isLoading = profileQuery.isLoading

  // useEffect(() => {
  //   const isAuthError =
  //     profileQuery.error instanceof ApiError &&
  //     (profileQuery.error.status === 401 || profileQuery.error.status === 403)

  //   if (isAuthError && accessToken) {
  //     tokenStorage.clearTokens()
  //   }
  // }, [profileQuery.error, accessToken])

  return {
    user: profileQuery.data,
    isAuthenticated,
    isLoading,
    isReady: !profileQuery.isLoading
  }
}
