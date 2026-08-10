import React, { useMemo, type ReactNode } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import {
  createMobileRuntime,
  MobileRuntimeProvider,
  MobileApplicationServicesProvider,
  createMobileApplicationServices,
} from "@notrelix/runtime-mobile";

export function MobileAppProviders({ children }: { children: ReactNode }) {
  const services = useMemo(() => {
    const runtime = createMobileRuntime({
      apiUrl: process.env.EXPO_PUBLIC_API_URL || "https://api.notrelix.com",
      realtimeUrl:
        process.env.EXPO_PUBLIC_REALTIME_URL || "wss://realtime.notrelix.com",
      releaseSha: process.env.EXPO_PUBLIC_RELEASE_SHA || "dev",
    });
    return createMobileApplicationServices(runtime);
  }, []);

  return (
    <MobileApplicationServicesProvider services={services}>
      <QueryClientProvider client={services.queryClient}>
        <MobileRuntimeProvider runtime={services.runtime}>
          {children}
        </MobileRuntimeProvider>
      </QueryClientProvider>
    </MobileApplicationServicesProvider>
  );
}
