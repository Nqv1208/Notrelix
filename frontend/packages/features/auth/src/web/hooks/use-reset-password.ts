import { useMutation } from '@tanstack/react-query';
import { useNavigate } from '@notrelix/platform/navigation';
import { createAuthService, type AuthApiClient, type AuthEndpoints } from '../../core/api/auth.service';

interface UseResetPasswordDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseResetPassword({ api, endpoints }: UseResetPasswordDeps) {
  const authService = createAuthService(api, endpoints);

  return function useResetPassword() {
    const navigate = useNavigate();

    return useMutation({
      mutationFn: authService.resetPassword,
      onSuccess: () => {
        navigate({ to: '/sign-in' });
      },
    });
  };
}
