import { useQuery } from "@tanstack/react-query";
import {
  createAuthService,
  type AuthApiClient,
  type AuthEndpoints,
} from "../../core/api/auth.service";
import { authQueryKeys } from "../../core/query/keys";

interface UseBootstrapAccountDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

/**
 * The signed-in member's own Account, read from the session bootstrap. Keyed by
 * the person (`userId`), so a change of person can never reuse the previous
 * answer; with no person (`null`) nothing is read.
 */
export function createUseBootstrapAccount({
  api,
  endpoints,
}: UseBootstrapAccountDeps) {
  const authService = createAuthService(api, endpoints);

  return function useBootstrapAccount(userId: string | null) {
    const query = useQuery({
      queryKey: authQueryKeys.bootstrapAccount(userId ?? ""),
      queryFn: () => authService.bootstrapAccount(),
      enabled: userId !== null,
      // A failure is shown with a Retry; the Account does not change within a session.
      retry: false,
      staleTime: Infinity,
      refetchOnWindowFocus: false,
    });
    return {
      accountId: query.data?.accountId ?? null,
      /** Answered, whatever the answer (a member of no Account answers `null`). */
      isResolved: query.data !== undefined,
      isFailed: query.isError && query.data === undefined,
      retry: () => void query.refetch(),
    };
  };
}
