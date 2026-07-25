import { type ReactNode } from 'react';
import { Navigate, useLocation } from '@tanstack/react-router';
import { useAuth } from '@notrelix/features-auth';
import { LoadingState } from '@notrelix/ui-web';

interface AuthGuardProps {
  children: ReactNode;
}

export function AuthGuard({ children }: AuthGuardProps) {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-background">
        <LoadingState title="Loading" description="Verifying session..." />
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <Navigate
        to="/sign-in"
        search={{ redirect: location.pathname + location.search }}
        replace
      />
    );
  }

  return <>{children}</>;
}
