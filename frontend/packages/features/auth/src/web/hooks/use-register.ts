import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createAuthService,
  type AuthApiClient,
  type AuthEndpoints,
} from "../../core/api/auth.service";
import { authQueryKeys } from "../../core/query/keys";
import type { NavigationDeps } from "./use-login";

export type { NavigationDeps };

interface UseRegisterDeps extends NavigationDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseRegister({
  api,
  endpoints,
  navigate,
  getSearchParams,
}: UseRegisterDeps) {
  const authService = createAuthService(api, endpoints);

  return function useRegister() {
    const redirect = getSearchParams().get("redirect");
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: authService.register,
      onSuccess: async (response) => {
        // Registering opens the session: the app learns who signed in like a login does.
        if (response.user) {
          await queryClient.cancelQueries({ queryKey: authQueryKeys.profile });
          queryClient.setQueryData(authQueryKeys.profile, response.user);
        }
        navigate({ to: redirect || "/home" });
      },
    });
  };
}
