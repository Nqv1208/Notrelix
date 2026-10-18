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
  const { api: runtimeClient } = useAppRuntime();
  const navigate = useNavigate();
  const service = useMemo(
    () =>
      createAuthFlowService(runtimeClient.api as unknown as AuthFlowApiClient),
    [runtimeClient],
  );
  const navigation = useMemo<AuthFlowNavigation>(
    () => ({
      navigate: (options) =>
        void navigate({
          to: options.to,
          replace: options.replace,
          search: options.search,
        } as Parameters<typeof navigate>[0]),
    }),
    [navigate],
  );
  return { service, navigation };
}
