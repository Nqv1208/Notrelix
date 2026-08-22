import React from "react";
import ReactDOM from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import {
  createAppRuntime,
  type AppRuntimeFactories,
} from "@notrelix/runtime-web";
import { readWebRuntimeEnvironment } from "./config/read-runtime-environment";
import { createWebApplicationServices } from "./composition/application-services";
import { AppProviders } from "./providers/app-providers";
import { router } from "./router";
import { sanitizeInternalReturnUrl } from "./routing/sanitize-return-url";
import "./styles/globals.css";

/**
 * Composition root.
 *
 * Production isolation (Plan: 01-FREEZE-SPEC.md §FZ-S14, 02-IMPLEMENTATION-PLAN.md §MFB-FZ-08):
 *   Using `import.meta.env.DEV && runtimeEnvironment.mockApi` allows Vite/Rolldown to
 *   statically evaluate `import.meta.env.DEV` as false during production build,
 *   completely tree-shaking the mock backend import and preventing any mock chunk emission.
 */
async function init(): Promise<void> {
  const runtimeEnvironment = readWebRuntimeEnvironment(import.meta.env);

  let runtimeFactories: AppRuntimeFactories = {};

  if (import.meta.env.DEV && runtimeEnvironment.mockApi) {
    // Dynamic import in dev only — completely omitted from production build
    const mockBackend = await import("@notrelix/dev-mock-backend");

    // Parse env-driven config overrides (fail-fast on invalid values)
    const envConfig = mockBackend.parseMockConfigFromEnv(
      import.meta.env as Record<string, string | undefined>,
    );

    const resolvedConfig =
      Object.keys(envConfig).length > 0
        ? { ...mockBackend.defaultConfig, ...envConfig }
        : mockBackend.defaultConfig;

    const store = new mockBackend.MockStore(resolvedConfig);
    const fetchImpl = mockBackend.createMockFetch(store);

    runtimeFactories = {
      fetchImpl,
      createRealtimeClient: () => mockBackend.createMockRealtimeTransport(),
    };
  }

  const runtime = createAppRuntime(runtimeEnvironment, runtimeFactories);

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
