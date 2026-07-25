import { Navigate } from '@tanstack/react-router';

export function IndexPage() {
  return <Navigate to="/home" replace />;
}
