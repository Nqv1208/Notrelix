import { useMemo } from 'react';
import { useNavigate as useTanStackNavigate, useLocation, Link } from '@tanstack/react-router';
import {
  NavigationProvider as PlatformNavigationProvider,
  type NavigationConfig,
} from '@notrelix/platform/navigation';

export function AppNavigationProvider({ children }: { children: React.ReactNode }) {
  const tanstackNavigate = useTanStackNavigate();
  const { search, pathname } = useLocation();

  const config: NavigationConfig = useMemo(
    () => ({
      adapter: {
        navigate: (options) => {
          tanstackNavigate({ to: options.to, search: options.search, replace: options.replace });
        },
        getSearchParams: () => {
          const params = new URLSearchParams();
          for (const [key, value] of Object.entries(search)) {
            if (value !== undefined && value !== null) {
              params.set(key, String(value));
            }
          }
          return params;
        },
        getPathname: () => pathname,
      },
      Link: ({ to, children: linkChildren, className, replace }) => (
        <Link to={to} className={className} replace={replace}>
          {linkChildren}
        </Link>
      ),
    }),
    [tanstackNavigate, search, pathname],
  );

  return (
    <PlatformNavigationProvider config={config}>
      {children}
    </PlatformNavigationProvider>
  );
}
