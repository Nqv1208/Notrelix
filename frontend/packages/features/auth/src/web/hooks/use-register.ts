import { useMutation } from "@tanstack/react-query";
import {
  createAuthService,
  type AuthApiClient,
  type AuthEndpoints,
} from "../../core/api/auth.service";
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

    return useMutation({
      mutationFn: authService.register,
      onSuccess: () => {
        navigate({ to: redirect || "/home" });
      },
    });
  };
}
