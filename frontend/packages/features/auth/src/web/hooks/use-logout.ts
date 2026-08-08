import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createAuthService, type AuthApiClient, type AuthEndpoints } from '../../core/api/auth.service';
import type { NavigationDeps } from './use-login';

interface UseLogoutDeps extends NavigationDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseLogout({ api, endpoints, navigate }: UseLogoutDeps) {
  const authService = createAuthService(api, endpoints);

  return function useLogout() {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: async () => {
        try {
          await authService.logout({});
        } catch {
          // Ignore API failures; local sign-out still proceeds.
        }
      },
      onSettled: () => {
        queryClient.clear();
        navigate({ to: '/', replace: true });
      },
    });
  };
}
