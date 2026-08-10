import React, { useEffect, useMemo, type ReactNode } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import {
  createMobileRuntime,
  MobileRuntimeProvider,
  MobileApplicationServicesProvider,
  createMobileApplicationServices,
} from "@notrelix/runtime-mobile";
import { env } from "../config/env";

export function MobileAppProviders({ children }: { children: ReactNode }) {
  const services = useMemo(() => {
    const runtime = createMobileRuntime(env);
    return createMobileApplicationServices(runtime);
  }, []);

  useEffect(() => {
    return () => {
      void services.dispose();
    };
  }, [services]);

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
