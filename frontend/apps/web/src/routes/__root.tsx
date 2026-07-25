import { Outlet } from '@tanstack/react-router';
import { AppNavigationProvider } from '../providers/navigation-provider';

export function RootLayout() {
  return (
    <div className="min-h-screen bg-background">
      <AppNavigationProvider>
        <Outlet />
      </AppNavigationProvider>
    </div>
  );
}
