import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAuthService,
  type AuthApiClient,
  type AuthEndpoints,
} from "../../core/api/auth.service";
import type { NavigationDeps } from "./use-login";

interface UseLogoutDeps extends NavigationDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
  /** Runs once the session ended, to drop user content kept outside the query cache. */
  onSignedOut?: () => void;
}

export function createUseLogout({ api, endpoints, navigate, onSignedOut }: UseLogoutDeps) {
  const authService = createAuthService(api, endpoints);

  return function useLogout() {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: async () => {
        try {
          await authService.logout({});
        } catch (error) {
          // A 401 says the server already ended this session: signed out all the same.
          // Any other failure leaves the session alive, so the person is not told otherwise.
          if ((error as { status?: number }).status !== 401) throw error;
        }
      },
      onSuccess: () => {
        queryClient.clear();
        onSignedOut?.();
        navigate({ to: "/sign-in", replace: true });
      },
    });
  };
}
