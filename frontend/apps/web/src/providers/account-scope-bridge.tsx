import { type ReactNode, useLayoutEffect, useMemo } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  authQueryKeys,
  createUseBootstrapAccount,
  createUseLogout,
  useAuth,
} from "@notrelix/features-auth";
import { useLocale } from "@notrelix/i18n";
import { AccountScopeProvider, runSessionReset } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import { Button, ErrorState, LoadingState } from "@notrelix/ui-web";
import { useApplicationServices } from "../composition/application-services-context";
import { en, vi } from "./account-scope-bridge.copy";

/** The router is not mounted here, so leaving for the sign-in page is a full navigation. */
const leaveForSignIn = () => window.location.assign("/sign-in");

/**
 * Feeds the active Account scope with the signed-in person's own Account (the
 * `accountId` of `GET /auth/bootstrap`, never `User.id`) and runs the
 * Account-change cache transition.
 *
 * A signed-in person's tree renders only once that Account is known: pending,
 * failed and "belongs to no Account" each show their own screen, because
 * `useAccountId()` throws without one. Signed-out visitors are not gated.
 */
export function AccountScopeBridge({
  children,
  navigateToSignIn = leaveForSignIn,
}: {
  children: ReactNode;
  /** Where Sign out ends up; the host default is a full navigation to the sign-in page. */
  navigateToSignIn?: () => void;
}) {
  const { user } = useAuth();
  const userId = user?.id ?? null;
  const { lifecycle } = useApplicationServices();
  const { api } = useAppRuntime();
  const queryClient = useQueryClient();
  const { locale } = useLocale();
  const copy = locale === "vi" ? vi : en;

  const { useBootstrapAccount, useLogout } = useMemo(
    () => ({
      useBootstrapAccount: createUseBootstrapAccount({
        api: api.api,
        endpoints: api.endpoints,
      }),
      useLogout: createUseLogout({
        api: api.api,
        endpoints: api.endpoints,
        navigate: navigateToSignIn,
        getSearchParams: () => new URLSearchParams(window.location.search),
        onSignedOut: runSessionReset,
      }),
    }),
    [api, navigateToSignIn],
  );
  const bootstrap = useBootstrapAccount(userId);
  const logout = useLogout();
  const accountId = bootstrap.accountId;

  // Layout effect: runs before children subscribe their queries, so the reset
  // never wipes a fetch the new Account just started.
  useLayoutEffect(() => {
    lifecycle.prepareAccountTransition(accountId);
    // The reset drops every cached query, this person's bootstrap answer included;
    // putting it back keeps the gate open instead of reading it a second time.
    if (accountId !== null && userId !== null) {
      queryClient.setQueryData(authQueryKeys.bootstrapAccount(userId), {
        accountId,
      });
    }
  }, [lifecycle, queryClient, userId, accountId]);

  if (userId === null) {
    return (
      <AccountScopeProvider accountId={null}>{children}</AccountScopeProvider>
    );
  }

  if (accountId !== null) {
    return (
      <AccountScopeProvider accountId={accountId}>
        {children}
      </AccountScopeProvider>
    );
  }

  if (bootstrap.isFailed) {
    return (
      <div
        role="alert"
        className="h-screen w-screen flex items-center justify-center bg-background p-4"
      >
        <ErrorState
          title={copy.unavailable.title}
          description={copy.unavailable.description}
          action={
            <Button
              type="button"
              className="max-md:h-touch!"
              onClick={bootstrap.retry}
            >
              {copy.unavailable.retry}
            </Button>
          }
        />
      </div>
    );
  }

  if (bootstrap.isResolved) {
    return (
      <div
        role="alert"
        className="h-screen w-screen flex items-center justify-center bg-background p-4"
      >
        <ErrorState
          title={copy.noAccount.title}
          description={copy.noAccount.description}
          action={
            <div className="flex flex-wrap items-center justify-center gap-2">
              <Button
                type="button"
                className="max-md:h-touch!"
                onClick={bootstrap.retry}
              >
                {copy.noAccount.retry}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="max-md:h-touch!"
                disabled={logout.isPending}
                onClick={() => logout.mutate()}
              >
                {logout.isPending
                  ? copy.noAccount.signingOut
                  : copy.noAccount.signOut}
              </Button>
            </div>
          }
        />
      </div>
    );
  }

  return (
    <div
      role="status"
      aria-live="polite"
      className="h-screen w-screen flex items-center justify-center bg-background"
    >
      <LoadingState
        title={copy.loading.title}
        description={copy.loading.description}
      />
    </div>
  );
}
