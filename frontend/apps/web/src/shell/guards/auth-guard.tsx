import { useEffect, type ReactNode } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { authQueryKeys, useAuth } from "@notrelix/features-auth";
import { LoadingState } from "@notrelix/ui-web";

interface AuthGuardProps {
  children: ReactNode;
}

export function AuthGuard({ children }: AuthGuardProps) {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  // Sign-in writes the profile and navigates in one tick; the provider's state
  // catches up a render later. A cached profile is a session, not a sign-out.
  const profileCached =
    queryClient.getQueryData(authQueryKeys.profile) !== undefined;

  useEffect(() => {
    if (isLoading || isAuthenticated || profileCached) return;
    if (location.pathname.startsWith("/sign-in")) return;
    navigate({
      to: "/sign-in",
      search: { redirect: location.pathname + location.searchStr },
      replace: true,
    });
  }, [
    isAuthenticated,
    isLoading,
    profileCached,
    location.pathname,
    location.searchStr,
    navigate,
  ]);

  if (isLoading || (!isAuthenticated && profileCached)) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-background">
        <LoadingState title="Loading" description="Verifying session..." />
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return <>{children}</>;
}
