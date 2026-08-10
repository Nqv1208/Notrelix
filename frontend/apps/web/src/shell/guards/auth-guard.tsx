import { useEffect, type ReactNode } from "react";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { useAuth } from "@notrelix/features-auth";
import { LoadingState } from "@notrelix/ui-web";

interface AuthGuardProps {
  children: ReactNode;
}

export function AuthGuard({ children }: AuthGuardProps) {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    if (isLoading || isAuthenticated) return;
    if (location.pathname.startsWith("/sign-in")) return;
    navigate({
      to: "/sign-in",
      search: { redirect: location.pathname + location.searchStr },
      replace: true,
    });
  }, [
    isAuthenticated,
    isLoading,
    location.pathname,
    location.searchStr,
    navigate,
  ]);

  if (isLoading) {
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
