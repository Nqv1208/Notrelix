import React from "react";
import ReactDOM from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import { createAppRuntime } from "@notrelix/runtime-web";
import { readWebRuntimeEnvironment } from "./config/read-runtime-environment";
import { createWebApplicationServices } from "./composition/application-services";
import { AppProviders } from "./providers/app-providers";
import { router } from "./router";
import { sanitizeInternalReturnUrl } from "./routing/sanitize-return-url";
import "./styles/globals.css";

/**
 * Composition root.
 *
 * Production isolation (Plan: 07-IMPLEMENTATION-MIGRATION-PLAN.md §Phase 10):
 *   @notrelix/dev-mock-backend is imported via dynamic import ONLY when
 *   VITE_MOCK_API=true. The production bundle never bundles the mock backend.
 *
 * Env var-driven config (Plan: 05-SCENARIOS-DENSITY-FAULTS.md §Env parsing):
 *   VITE_MOCK_API, VITE_MOCK_PRESET, VITE_MOCK_PERSONA, VITE_MOCK_STATE,
 *   VITE_MOCK_DENSITY, VITE_MOCK_LATENCY, VITE_MOCK_SEED
 */
async function init(): Promise<void> {
  const runtimeEnvironment = readWebRuntimeEnvironment(import.meta.env);

  let fetchImpl: typeof fetch | undefined;

  if (runtimeEnvironment.mockApi) {
    // Dynamic import — zero production bundle cost
    const mockBackend = await import("@notrelix/dev-mock-backend");

    // Parse env-driven config overrides
    const envConfig = mockBackend.parseMockConfigFromEnv(
      import.meta.env as Record<string, string | undefined>,
    );

    // Reconfigure global store if any env overrides are provided
    if (Object.keys(envConfig).length > 0) {
      mockBackend.globalMockStore.updateConfig(envConfig);
    }

    fetchImpl = mockBackend.createMockFetch(mockBackend.globalMockStore);
  }

  const runtime = createAppRuntime(
    runtimeEnvironment,
    fetchImpl ? { fetchImpl } : {},
  );

  const services = createWebApplicationServices(runtime, {
    navigateToSignedOut: () => {
      const currentUrl = `${window.location.pathname}${window.location.search}${window.location.hash}`;
      const redirectPath = sanitizeInternalReturnUrl(currentUrl);
      void router.navigate({
        to: "/sign-in",
        search: { redirect: redirectPath },
        replace: true,
      });
    },
  });

  // Register HMR disposal and pagehide cleanup
  const teardown = () => {
    void services.dispose();
  };

  if (import.meta.hot) {
    import.meta.hot.dispose(teardown);
  }

  if (typeof window !== "undefined") {
    window.addEventListener("pagehide", teardown, { once: true });
  }

  function App() {
    return (
      <AppProviders services={services}>
        <RouterProvider router={router} context={{ services }} />
      </AppProviders>
    );
  }

  const rootElement = document.getElementById("root")!;
  ReactDOM.createRoot(rootElement).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>,
  );
}

void init();
