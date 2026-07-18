import { useMutation } from '@tanstack/react-query';
import { useNavigate } from '@notrelix/platform/navigation';
import { createAuthService, type AuthApiClient, type AuthEndpoints } from '~/core/api/auth.service';

interface UseForgotPasswordDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseForgotPassword({ api, endpoints }: UseForgotPasswordDeps) {
  const authService = createAuthService(api, endpoints);

  return function useForgotPassword() {
    const navigate = useNavigate();

    return useMutation({
      mutationFn: authService.forgotPassword,
      onSuccess: () => {
        // Can navigate to sign-in or a recovery confirmation screen
        navigate({ to: '/sign-in' });
      },
    });
  };
}
