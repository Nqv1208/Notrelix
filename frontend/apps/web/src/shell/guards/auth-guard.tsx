import { useEffect, type ReactNode } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { authQueryKeys, useAuth } from "@notrelix/features-auth";
import { useLocale } from "@notrelix/i18n";
import { Button, ErrorState, LoadingState } from "@notrelix/ui-web";
import { en, vi } from "./auth-guard.copy";

interface AuthGuardProps {
  children: ReactNode;
}

export function AuthGuard({ children }: AuthGuardProps) {
  const { isAuthenticated, isLoading, isUnavailable, retry } = useAuth();
  const { locale } = useLocale();
  const copy = locale === "vi" ? vi : en;
  const location = useLocation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  // Sign-in writes the profile and navigates in one tick; the provider's state
  // catches up a render later. A cached profile is a session, not a sign-out.
  const profileCached =
    queryClient.getQueryData(authQueryKeys.profile) !== undefined;

  useEffect(() => {
    // A session that could not be read is not a session that ended: stay put.
    if (isLoading || isAuthenticated || profileCached || isUnavailable) return;
    if (location.pathname.startsWith("/sign-in")) return;
    navigate({
      to: "/sign-in",
      search: { redirect: location.pathname + location.searchStr },
      replace: true,
    });
  }, [
    isAuthenticated,
    isLoading,
    isUnavailable,
    profileCached,
    location.pathname,
    location.searchStr,
    navigate,
  ]);

  if (isLoading || (!isAuthenticated && profileCached)) {
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

  if (isUnavailable) {
    return (
      <div
        role="alert"
        className="h-screen w-screen flex items-center justify-center bg-background"
      >
        <ErrorState
          title={copy.unavailable.title}
          description={copy.unavailable.description}
          action={
            <Button type="button" onClick={retry}>
              {copy.unavailable.retry}
            </Button>
          }
        />
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return <>{children}</>;
}
