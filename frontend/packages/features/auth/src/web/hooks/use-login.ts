import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAuthService,
  type AuthApiClient,
  type AuthEndpoints,
} from "../../core/api/auth.service";
import { authQueryKeys } from "../../core/query/keys";

export interface NavigationDeps {
  navigate: (options: { to: string; replace?: boolean }) => void;
  getSearchParams: () => URLSearchParams;
}

interface UseLoginDeps extends NavigationDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseLogin({
  api,
  endpoints,
  navigate,
  getSearchParams,
}: UseLoginDeps) {
  const authService = createAuthService(api, endpoints);

  return function useLogin() {
    const redirect = getSearchParams().get("redirect");
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: authService.login,
      onSuccess: async (response) => {
        await queryClient.cancelQueries({ queryKey: authQueryKeys.profile });
        queryClient.setQueryData(authQueryKeys.profile, response.user);
        navigate({ to: redirect || "/home" });
      },
    });
  };
}
