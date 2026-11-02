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
      // With a person already read nothing changes on reconnect; with none (the session could
      // not be read) the unavailable screen resolves itself when the network is back.
      refetchOnReconnect: (query) => query.state.data === undefined,
      refetchOnMount: false,
    });

    // Only a rejected session means "signed out". A network or server failure says
    // nothing about the session: with a person already read they stay signed in, and
    // with none to show the guard offers a retry instead of the sign-in page.
    const sessionRejected =
      profileQuery.isError &&
      (profileQuery.error as { kind?: string } | null)?.kind === "auth";
    const isAuthenticated = !!profileQuery.data && !sessionRejected;
    const isLoading = profileQuery.isLoading;
    const isUnavailable =
      profileQuery.isError && !profileQuery.data && !sessionRejected;

    return {
      user: profileQuery.data,
      isAuthenticated,
      isLoading,
      isReady: !profileQuery.isLoading,
      isUnavailable,
      retry: () => void profileQuery.refetch(),
    };
  };
}
