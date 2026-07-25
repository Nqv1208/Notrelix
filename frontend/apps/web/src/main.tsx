import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
// @deprecated — configureApi will be removed once all components migrate to useAppRuntime()
import { configureApi } from '@notrelix/contracts';
import { createAppRuntime } from '@notrelix/runtime-web';
import { AppProviders } from './providers/app-providers';
import { router } from './router';
import './styles/globals.css';

/**
 * Composition root: build the application runtime from environment variables
 * and inject it into the provider tree. NO new code should use `api` directly;
 * new components must use `useAppRuntime()` and `runtime.api`.
 *
 * The `configureApi()` call below is a DEPRECATED bridge for legacy module-level
 * component factories that have not yet migrated to the AppRuntime pattern.
 * Track migration progress: see `MIGRATION_TRACKER.md`
 */
const runtime = createAppRuntime(import.meta.env);
configureApi(runtime.env.apiUrl); // bridge: synchronizes legacy api singleton URL

function App() {
  return (
    <AppProviders runtime={runtime}>
      <RouterProvider router={router} />
    </AppProviders>
  );
}

const rootElement = document.getElementById('root')!;
ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
