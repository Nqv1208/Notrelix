import React, { useEffect, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAppRuntime, type SessionExpiredEvent } from '@notrelix/runtime-web';
import { router } from '../router';
import { sanitizeInternalReturnUrl } from '../routing/sanitize-return-url';

const MAX_HANDLED_EVENTS = 32;

export function SessionLifecycle({ children }: { children: React.ReactNode }) {
  const runtime = useAppRuntime();
  const queryClient = useQueryClient();
  const handledEventIdsRef = useRef<Set<string>>(new Set());

  useEffect(() => {
    const unsubscribe = runtime.sessionEvents.subscribe(async (event: SessionExpiredEvent) => {
      if (handledEventIdsRef.current.has(event.eventId)) {
        return;
      }

      // Add to bounded deduplication set
      handledEventIdsRef.current.add(event.eventId);
      if (handledEventIdsRef.current.size > MAX_HANDLED_EVENTS) {
        const first = handledEventIdsRef.current.values().next().value;
        if (first) handledEventIdsRef.current.delete(first);
      }

      // 1. Disconnect realtime connection
      runtime.realtime.disconnect('session-expired');

      // 2. Cancel ongoing query refetches
      await queryClient.cancelQueries();

      // 3. Clear private query cache
      queryClient.clear();

      // 4. Sanitize current location (pathname + search + hash)
      const currentUrl = `${window.location.pathname}${window.location.search}${window.location.hash}`;
      const redirectPath = sanitizeInternalReturnUrl(currentUrl);

      // 5. Navigate to sign-in page
      router.navigate({
        to: '/sign-in',
        search: { redirect: redirectPath },
        replace: true,
      });
    });

    return () => {
      unsubscribe();
    };
  }, [runtime, queryClient]);

  return <>{children}</>;
}
