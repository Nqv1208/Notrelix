import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from '@tanstack/react-router';
import { configureApi } from '@notrelix/contracts';
import { env } from './config/env';
import { AppProviders } from './providers/app-providers';
import { router } from './router';
import './styles/globals.css';

configureApi(env.apiUrl);

function App() {
  return (
    <AppProviders>
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
