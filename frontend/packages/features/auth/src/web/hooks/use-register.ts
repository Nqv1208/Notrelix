import { useMutation } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from '@notrelix/platform/navigation';
import { createAuthService, type AuthApiClient, type AuthEndpoints } from '~/core/api/auth.service';

interface UseRegisterDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseRegister({ api, endpoints }: UseRegisterDeps) {
  const authService = createAuthService(api, endpoints);

  return function useRegister() {
    const navigate = useNavigate();
    const searchParams = useSearchParams();
    const redirect = searchParams.get('redirect');

    return useMutation({
      mutationFn: authService.register,
      onSuccess: () => {
        navigate({ to: redirect || '/home' });
      },
    });
  };
}
