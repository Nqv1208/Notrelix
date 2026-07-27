import { useEffect, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAppRuntime } from '@notrelix/runtime-web';
import { router } from '../router';

export function SessionLifecycle({ children }: { children: React.ReactNode }) {
  const runtime = useAppRuntime();
  const queryClient = useQueryClient();
  const isHandlingExpiry = useRef(false);

  useEffect(() => {
    const unsubscribe = runtime.sessionEvents.subscribe((event) => {
      if (event.type === 'session-expired') {
        if (isHandlingExpiry.current) return;
        isHandlingExpiry.current = true;

        console.warn('[SessionLifecycle] Session expired, clearing cache and redirecting to sign-in.');

        // Cancel all active queries and clear private cache
        queryClient.cancelQueries();
        queryClient.clear();

        // Redirect to sign-in page safely with current pathname as sanitized return URL
        const currentPath = window.location.pathname;
        const redirectPath = currentPath.startsWith('/') && !currentPath.startsWith('//') ? currentPath : '/';

        router.navigate({
          to: '/sign-in',
          search: { redirect: redirectPath },
        });

        // Reset debouncing flag after 2 seconds
        setTimeout(() => {
          isHandlingExpiry.current = false;
        }, 2000);
      }
    });

    return () => {
      unsubscribe();
    };
  }, [runtime.sessionEvents, queryClient]);

  return <>{children}</>;
}
