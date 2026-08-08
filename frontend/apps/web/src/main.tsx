import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { createAppRuntime } from '@notrelix/runtime-web';
import { readWebRuntimeEnvironment } from './config/read-runtime-environment';
import { createWebApplicationServices } from './composition/application-services';
import { AppProviders } from './providers/app-providers';
import { router } from './router';
import { getActiveWorkspaceIdFromPathname } from './realtime/active-workspace';
import { sanitizeInternalReturnUrl } from './routing/sanitize-return-url';
import './styles/globals.css';

/**
 * Composition root: read normalized runtime environment and instantiate AppRuntime.
 */
const runtimeEnvironment = readWebRuntimeEnvironment(import.meta.env);
const runtime = createAppRuntime(runtimeEnvironment);
const services = createWebApplicationServices(runtime, {
  navigateToSignedOut: () => {
    const currentUrl = `${window.location.pathname}${window.location.search}${window.location.hash}`;
    const redirectPath = sanitizeInternalReturnUrl(currentUrl);
    void router.navigate({
      to: '/sign-in',
      search: { redirect: redirectPath },
      replace: true,
    });
  },
});

let activeWorkspaceId = getActiveWorkspaceIdFromPathname(router.state.location.pathname);
const unsubscribeWorkspaceEvents = router.subscribe('onResolved', ({ toLocation }) => {
  const nextWorkspaceId = getActiveWorkspaceIdFromPathname(toLocation.pathname);
  if (nextWorkspaceId !== activeWorkspaceId) {
    services.workspaceEvents.publish({
      previousWorkspaceId: activeWorkspaceId,
      nextWorkspaceId,
    });
    activeWorkspaceId = nextWorkspaceId;
  }
});

// Register HMR disposal and pagehide cleanup
const teardown = () => {
  unsubscribeWorkspaceEvents();
  void services.dispose();
};

if (import.meta.hot) {
  import.meta.hot.dispose(teardown);
}

if (typeof window !== 'undefined') {
  window.addEventListener('pagehide', teardown, { once: true });
}

function App() {
  return (
    <AppProviders services={services}>
      <RouterProvider router={router} context={{ services }} />
    </AppProviders>
  );
}

const rootElement = document.getElementById('root')!;
ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
