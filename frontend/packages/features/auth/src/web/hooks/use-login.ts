'use client';

import { useMutation } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from '@notrelix/platform/navigation';
import { createAuthService, type AuthApiClient, type AuthEndpoints } from '../../core/api/auth.service';

interface UseLoginDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseLogin({ api, endpoints }: UseLoginDeps) {
  const authService = createAuthService(api, endpoints);

  return function useLogin() {
    const navigate = useNavigate();
    const searchParams = useSearchParams();
    const redirect = searchParams.get('redirect');

    return useMutation({
      mutationFn: authService.login,
      onSuccess: () => {
        navigate({ to: redirect || '/home' });
      },
    });
  };
}
