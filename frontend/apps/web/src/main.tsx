import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { configureApi } from '@notrelix/contracts';
import { createAppRuntime } from '@notrelix/runtime-web';
import { AppProviders } from './providers/app-providers';
import { router } from './router';
import './styles/globals.css';

const runtime = createAppRuntime(import.meta.env);
configureApi(runtime.env.apiUrl);

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
