import { type ReactNode, useLayoutEffect, useMemo } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { createAuthProvider, useAuth } from "@notrelix/features-auth";
import { AccountScopeProvider } from "@notrelix/query";
import { endpoints } from "@notrelix/contracts";
import { AppRuntimeProvider } from "@notrelix/runtime-web";
import { WorkManagementServicesProvider } from "@notrelix/work-management-state";
import { I18nProvider } from "@notrelix/i18n";
import { ThemeProvider, Toaster, useTheme } from "@notrelix/ui-web";
import type { WebApplicationServices } from "../composition/application-services";
import {
  ApplicationServicesProvider,
  useApplicationServices,
} from "../composition/application-services-context";
import { GlobalErrorBoundary } from "../components/global-error-boundary";
import { webCatalogs } from "../i18n/catalogs";

const browserLocales =
  typeof navigator === "undefined" ? [] : [...navigator.languages];

function applyDocumentLanguage(locale: string) {
  document.documentElement.lang = locale;
}

export { useTheme };

/**
 * Feeds the active Account scope from the authenticated principal and runs the
 * principal-change cache transition. Temporary model: Account = principal
 * (`User.id`) until the backend exposes an Account ID and a switch signal.
 */
function AccountScopeBridge({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const { lifecycle } = useApplicationServices();
  const accountId = user?.id ?? null;

  // Layout effect: runs before children subscribe their queries, so the reset
  // never wipes a fetch the new principal just started.
  useLayoutEffect(() => {
    lifecycle.prepareAccountTransition(accountId);
  }, [lifecycle, accountId]);

  return (
    <AccountScopeProvider accountId={accountId}>
      {children}
    </AccountScopeProvider>
  );
}

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
              <AccountScopeBridge>
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
              </AccountScopeBridge>
            </FeatureAuthProvider>
          </QueryClientProvider>
        </AppRuntimeProvider>
      </ApplicationServicesProvider>
    </GlobalErrorBoundary>
  );
}
