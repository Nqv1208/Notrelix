import { useEffect } from 'react';
import { useAppRuntime } from '@notrelix/runtime-web';
import { useAuth } from '@notrelix/features-auth';

export function RealtimeLifecycle({ children }: { children: React.ReactNode }) {
  const runtime = useAppRuntime();
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    if (isAuthenticated) {
      runtime.realtime.connect();
    } else {
      runtime.realtime.disconnect();
    }

    return () => {
      // Clean disconnect on unmount
    };
  }, [isAuthenticated, runtime.realtime]);

  return <>{children}</>;
}
