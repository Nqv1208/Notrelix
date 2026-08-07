import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { createAppRuntime } from '@notrelix/runtime-web';
import { readWebRuntimeEnvironment } from './config/read-runtime-environment';
import { createWebApplicationServices } from './composition/application-services';
import { AppProviders } from './providers/app-providers';
import { router } from './router';
import './styles/globals.css';

/**
 * Composition root: read normalized runtime environment and instantiate AppRuntime.
 */
const runtimeEnvironment = readWebRuntimeEnvironment(import.meta.env);
const runtime = createAppRuntime(runtimeEnvironment);
const services = createWebApplicationServices(runtime);

// Register HMR disposal and pagehide cleanup
if (import.meta.hot) {
  import.meta.hot.dispose(() => {
    void services.dispose();
  });
}

if (typeof window !== 'undefined') {
  window.addEventListener('pagehide', () => void services.dispose(), { once: true });
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
