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
import {
  createWebMockRuntime,
  readMockRuntimeConfig,
} from "./dev/mock-runtime";

/**
 * Composition root: read normalized runtime environment and instantiate AppRuntime.
 */
const runtimeEnvironment = readWebRuntimeEnvironment(import.meta.env);
const mockRuntime = runtimeEnvironment.mockApi
  ? createWebMockRuntime(readMockRuntimeConfig(import.meta.env))
  : null;
const runtime = createAppRuntime(
  runtimeEnvironment,
  mockRuntime
    ? {
        createApiClient: () => mockRuntime.api,
        createRealtimeClient: () => mockRuntime.realtime,
        clock: mockRuntime.clock,
      }
    : {},
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
