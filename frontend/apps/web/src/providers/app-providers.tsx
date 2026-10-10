import { type ReactNode, useMemo } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";
import { createAuthProvider } from "@notrelix/features-auth";
import { endpoints } from "@notrelix/contracts";
import { AppRuntimeProvider } from "@notrelix/runtime-web";
import { WorkManagementServicesProvider } from "@notrelix/work-management-state";
import { I18nProvider } from "@notrelix/i18n";
import { ThemeProvider, useTheme } from "@notrelix/ui-web";
import type { WebApplicationServices } from "../composition/application-services";
import { ApplicationServicesProvider } from "../composition/application-services-context";
import { GlobalErrorBoundary } from "../components/global-error-boundary";
import { webCatalogs } from "../i18n/catalogs";

const browserLocales =
  typeof navigator === "undefined" ? [] : [...navigator.languages];

function applyDocumentLanguage(locale: string) {
  document.documentElement.lang = locale;
}

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
    [runtime.api],
  );

  return (
    <GlobalErrorBoundary
      telemetry={runtime.telemetry}
      releaseSha={runtime.env.releaseSha}
    >
      <ApplicationServicesProvider services={services}>
        <AppRuntimeProvider runtime={runtime}>
          <QueryClientProvider client={queryClient}>
            <FeatureAuthProvider>
              <I18nProvider
                catalogs={webCatalogs}
                storage={runtime.storage}
                browserLocales={browserLocales}
                onLocaleChange={applyDocumentLanguage}
              >
                <ThemeProvider storage={runtime.storage}>
                  <WorkManagementServicesProvider services={workManagement}>
                    {children}
                    <Toaster />
                  </WorkManagementServicesProvider>
                </ThemeProvider>
              </I18nProvider>
            </FeatureAuthProvider>
          </QueryClientProvider>
        </AppRuntimeProvider>
      </ApplicationServicesProvider>
    </GlobalErrorBoundary>
  );
}
