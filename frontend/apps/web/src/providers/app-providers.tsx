import { type ReactNode, useMemo } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { createAuthProvider, useAuth } from "@notrelix/features-auth";
import { endpoints } from "@notrelix/contracts";
import { AppRuntimeProvider } from "@notrelix/runtime-web";
import { WorkManagementServicesProvider } from "@notrelix/work-management-state";
import {
  ColourBlindLabelsProvider,
  WorkManagementUiPortsHost,
} from "@notrelix/work-management-web";
import { createUsePreferences } from "@notrelix/features-account";
import { createUseMentionSuggestions } from "@notrelix/features-collaboration";
import { I18nProvider } from "@notrelix/i18n";
import { ThemeProvider, Toaster, useTheme } from "@notrelix/ui-web";
import type { WebApplicationServices } from "../composition/application-services";
import {
  ApplicationServicesProvider,
  useApplicationServices,
} from "../composition/application-services-context";
import { GlobalErrorBoundary } from "../components/global-error-boundary";
import { webCatalogs } from "../i18n/catalogs";
import { AccountScopeBridge } from "./account-scope-bridge";
import { AccountSuspensionBridge } from "../shell/workspace/account-suspension-bridge";
import { EmailConfirmationToastBridge } from "../shell/workspace/email-confirmation-toast-bridge";

const browserLocales =
  typeof navigator === "undefined" ? [] : [...navigator.languages];

function applyDocumentLanguage(locale: string) {
  document.documentElement.lang = locale;
}

export { useTheme };

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

  // The people an item comment can @mention, read from the server (only people who can view the item).
  const useItemMentionSuggestions = useMemo(() => {
    const useSuggestions = createUseMentionSuggestions(
      runtime.api.api,
      runtime.api.endpoints,
    );
    return (workspaceId: string, itemId: string, query: string | null) =>
      useSuggestions(workspaceId, "item", itemId, query);
  }, [runtime.api]);

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
                  {/* Inside the language and theme providers: its loading and failure screens use both. */}
                  <AccountScopeBridge>
                    <WorkManagementServicesProvider services={workManagement}>
                      <WorkManagementUiPortsHost
                        useMentionSuggestions={useItemMentionSuggestions}
                      >
                        <SignedInColourBlind>{children}</SignedInColourBlind>
                      </WorkManagementUiPortsHost>
                      <Toaster />
                      <EmailConfirmationToastBridge />
                      <AccountSuspensionBridge />
                    </WorkManagementServicesProvider>
                  </AccountScopeBridge>
                </ThemeProvider>
              </I18nProvider>
            </FeatureAuthProvider>
          </QueryClientProvider>
        </AppRuntimeProvider>
      </ApplicationServicesProvider>
    </GlobalErrorBoundary>
  );
}
