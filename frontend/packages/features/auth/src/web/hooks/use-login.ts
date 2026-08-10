import { useMutation } from "@tanstack/react-query";
import {
  createAuthService,
  type AuthApiClient,
  type AuthEndpoints,
} from "../../core/api/auth.service";

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

    return useMutation({
      mutationFn: authService.login,
      onSuccess: () => {
        navigate({ to: redirect || "/home" });
      },
    });
  };
}
