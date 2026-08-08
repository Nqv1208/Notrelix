import { type ReactNode, useMemo } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { createAuthProvider } from '@notrelix/features-auth';
import { endpoints } from '@notrelix/contracts';
import { AppRuntimeProvider } from '@notrelix/runtime-web';
import { WorkManagementServicesProvider } from '@notrelix/work-management-state';
import { ThemeProvider, useTheme } from '@notrelix/ui-web';
import type { WebApplicationServices } from '../composition/application-services';
import { ApplicationServicesProvider } from '../composition/application-services-context';
import { GlobalErrorBoundary } from '../components/global-error-boundary';

export { useTheme };

export function AppProviders({
  services,
  children,
}: {
  services: WebApplicationServices;
  children: ReactNode;
}) {
  const { runtime, queryClient, workManagement } = services;

  const FeatureAuthProvider = useMemo(
    () => createAuthProvider({ api: runtime.api.api, endpoints }),
    [runtime.api]
  );

  return (
    <GlobalErrorBoundary telemetry={runtime.telemetry} releaseSha={runtime.env.releaseSha}>
      <ApplicationServicesProvider services={services}>
        <AppRuntimeProvider runtime={runtime}>
          <QueryClientProvider client={queryClient}>
            <FeatureAuthProvider>
              <ThemeProvider storageKey="theme">
                <WorkManagementServicesProvider services={workManagement}>
                  {children}
                  <Toaster />
                </WorkManagementServicesProvider>
              </ThemeProvider>
            </FeatureAuthProvider>
          </QueryClientProvider>
        </AppRuntimeProvider>
      </ApplicationServicesProvider>
    </GlobalErrorBoundary>
  );
}
