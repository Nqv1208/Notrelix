import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import {
  createUseLogout,
  safeReturnPath,
  useAuth,
} from "@notrelix/features-auth";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * The account signed in on an invitation page, and the way to leave it: sign out,
 * then sign in again and come back to this same invitation link.
 */
export function useInviteAccount() {
  const { user } = useAuth();
  const { api } = useAppRuntime();
  const navigate = useNavigate();
  const useLogout = useMemo(
    () =>
      createUseLogout({
        api: api.api,
        endpoints: api.endpoints,
        // The invitation link, token included, is where the sign-in comes back to.
        navigate: () => {
          const back = safeReturnPath(
            window.location.pathname + window.location.hash,
          );
          void navigate({
            to: "/sign-in",
            search: back ? { redirect: back } : undefined,
            replace: true,
          } as Parameters<typeof navigate>[0]);
        },
        getSearchParams: () => new URLSearchParams(window.location.search),
      }),
    [api, navigate],
  );
  const logout = useLogout();
  return {
    signedInEmail: user?.email ?? null,
    onSwitchAccount: () => logout.mutate(),
  };
}
