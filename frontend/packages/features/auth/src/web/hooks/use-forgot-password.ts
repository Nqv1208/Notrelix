import { useMutation } from "@tanstack/react-query";
import {
  createAuthService,
  type AuthApiClient,
  type AuthEndpoints,
} from "../../core/api/auth.service";
import type { NavigationDeps } from "./use-login";

interface UseForgotPasswordDeps extends NavigationDeps {
  api: AuthApiClient;
  endpoints: AuthEndpoints;
}

export function createUseForgotPassword({
  api,
  endpoints,
  navigate,
}: UseForgotPasswordDeps) {
  const authService = createAuthService(api, endpoints);

  return function useForgotPassword() {
    return useMutation({
      mutationFn: authService.forgotPassword,
      onSuccess: () => {
        // Can navigate to sign-in or a recovery confirmation screen
        navigate({ to: "/sign-in" });
      },
    });
  };
}
