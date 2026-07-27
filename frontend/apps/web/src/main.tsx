import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { createAppRuntime } from '@notrelix/runtime-web';
import { readWebRuntimeEnvironment } from './config/read-runtime-environment';
import { AppProviders } from './providers/app-providers';
import { router } from './router';
import './styles/globals.css';

/**
 * Composition root: read normalized runtime environment and instantiate AppRuntime.
 */
const runtimeEnvironment = readWebRuntimeEnvironment(import.meta.env);
const runtime = createAppRuntime(runtimeEnvironment);

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
