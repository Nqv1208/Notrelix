import { useQuery } from "@tanstack/react-query";
import {
  createAuthService,
  type AuthApiClient,
  type AuthEndpoints,
} from "../../core/api/auth.service";
import { authQueryKeys } from "../../core/query/keys";

interface UseAuthUserDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseAuthUser({ api, endpoints }: UseAuthUserDeps) {
  const authService = createAuthService(api, endpoints);

  return function useAuthUser() {
    const profileQuery = useQuery({
      queryKey: authQueryKeys.profile,
      queryFn: () => authService.profile(),
      retry: false,
      staleTime: 5 * 60 * 1000,
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
      refetchOnMount: false,
    });

    const isAuthenticated = profileQuery.isSuccess && !!profileQuery.data;
    const isLoading = profileQuery.isLoading;

    return {
      user: profileQuery.data,
      isAuthenticated,
      isLoading,
      isReady: !profileQuery.isLoading,
    };
  };
}
