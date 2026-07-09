import { type ReactNode } from 'react';
import { Navigate } from '@tanstack/react-router';

interface AuthGuardProps {
  children: ReactNode;
  isAuthenticated?: boolean;
}

export function AuthGuard({ children, isAuthenticated = false }: AuthGuardProps) {
  if (!isAuthenticated) {
    return <Navigate to="/sign-in" replace />;
  }
  return <>{children}</>;
}
