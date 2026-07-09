'use client';

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@notrelix/platform/navigation';
import { createAuthService, type AuthApiClient, type AuthEndpoints } from '../../core/api/auth.service';

interface UseLogoutDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseLogout({ api, endpoints }: UseLogoutDeps) {
  const authService = createAuthService(api, endpoints);

  return function useLogout() {
    const navigate = useNavigate();
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
