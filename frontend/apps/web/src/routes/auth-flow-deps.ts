import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import {
  createAuthFlowService,
  type AuthFlowApiClient,
  type AuthFlowNavigation,
} from "@notrelix/features-auth";
import { useAppRuntime } from "@notrelix/runtime-web";

/** The sign-in calls and the router, for the pre-login pages. */
export function useAuthFlowDeps() {
  const { api: runtimeClient, env } = useAppRuntime();
  const navigate = useNavigate();
  const service = useMemo(
    () =>
      createAuthFlowService(runtimeClient.api as unknown as AuthFlowApiClient),
    [runtimeClient],
  );
  const navigation = useMemo<AuthFlowNavigation>(() => {
    const go: AuthFlowNavigation["navigate"] = (options) =>
      void navigate({
        to: options.to,
        replace: options.replace,
        search: options.search,
      } as Parameters<typeof navigate>[0]);
    return {
      navigate: go,
      startProviderSignIn: (path) => {
        if (env.mockApi) {
          // No browser leaves in the mock world: the mock plays the provider
          // round trip over the same route and says where the backend's
          // redirect would land (`/auth/callback`, or `/sign-in?step=mfa`).
          void (runtimeClient.api as unknown as AuthFlowApiClient)
            .get<{ redirectTo?: string }>(path, {
              skipAuthRefresh: true,
              skipGlobalErrorToast: true,
            })
            .then((answer) => {
              const landing = new URL(
                answer.redirectTo ?? "/auth/callback",
                window.location.origin,
              );
              go({
                to: landing.pathname,
                search: Object.fromEntries(landing.searchParams),
                replace: true,
              });
            })
            .catch(() =>
              go({
                to: "/sign-in",
                search: { error: "identity.oauth.provider-unavailable" },
                replace: true,
              }),
            );
          return;
        }
        window.location.assign(`${env.apiUrl}${path}`);
      },
    };
  }, [navigate, runtimeClient, env]);
  return { service, navigation };
}
