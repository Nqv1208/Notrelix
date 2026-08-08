import { useMutation } from '@tanstack/react-query';
import { createAuthService, type AuthApiClient, type AuthEndpoints } from '../../core/api/auth.service';
import type { NavigationDeps } from './use-login';

interface UseResetPasswordDeps extends NavigationDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseResetPassword({ api, endpoints, navigate }: UseResetPasswordDeps) {
  const authService = createAuthService(api, endpoints);

  return function useResetPassword() {
    return useMutation({
      mutationFn: authService.resetPassword,
      onSuccess: () => {
        navigate({ to: '/sign-in' });
      },
    });
  };
}
