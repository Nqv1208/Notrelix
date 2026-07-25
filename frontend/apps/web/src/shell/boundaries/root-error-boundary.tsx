import { type ReactNode } from 'react';

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
}

export function RootErrorBoundary({ children, fallback }: ErrorBoundaryProps) {
  return (
    <div>
      {children}
      {fallback && <div>{fallback}</div>}
    </div>
  );
}
