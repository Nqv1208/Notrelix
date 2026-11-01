import { type ReactNode, useLayoutEffect, useMemo } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { createAuthProvider, useAuth } from "@notrelix/features-auth";
import { AccountScopeProvider } from "@notrelix/query";
import { endpoints } from "@notrelix/contracts";
import { AppRuntimeProvider } from "@notrelix/runtime-web";
import { WorkManagementServicesProvider } from "@notrelix/work-management-state";
import { ColourBlindLabelsProvider } from "@notrelix/work-management-web";
import { createUsePreferences } from "@notrelix/features-account";
import { I18nProvider } from "@notrelix/i18n";
import { ThemeProvider, Toaster, useTheme } from "@notrelix/ui-web";
import type { WebApplicationServices } from "../composition/application-services";
import {
  ApplicationServicesProvider,
  useApplicationServices,
} from "../composition/application-services-context";
import { GlobalErrorBoundary } from "../components/global-error-boundary";
import { webCatalogs } from "../i18n/catalogs";
import { EmailConfirmationToastBridge } from "../shell/workspace/email-confirmation-toast-bridge";

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

/**
 * Gives the labels the person's colour-blind preference (an account preference) and a way to change it.
 * Only a signed-in person has one: without a session the labels keep their plain look.
 */
function ColourBlindBridge({ children }: { children: ReactNode }) {
  const { runtime } = useApplicationServices();
  const usePreferences = useMemo(
    () =>
      createUsePreferences({
        api: runtime.api.api,
        endpoints: runtime.api.endpoints,
      }),
    [runtime.api],
  );
  const { preferences, save } = usePreferences();
  return (
    <ColourBlindLabelsProvider
      enabled={preferences?.colourBlindLabels ?? false}
      setEnabled={(next) => void save({ colourBlindLabels: next })}
    >
      {children}
    </ColourBlindLabelsProvider>
  );
}

function SignedInColourBlind({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  return user ? (
    <ColourBlindBridge>{children}</ColourBlindBridge>
  ) : (
    <>{children}</>
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
                      <SignedInColourBlind>{children}</SignedInColourBlind>
                      <Toaster />
                      <EmailConfirmationToastBridge />
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
