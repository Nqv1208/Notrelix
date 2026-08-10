/**
 * App-level environment configuration.
 *
 * IMPORTANT: For API URL and WS URL, prefer runtime.env (from useAppRuntime())
 * which is the single source of truth parsed by @notrelix/kernel parseEnv().
 * This file is retained only for app-specific non-runtime config (mockApi, marketingUrl).
 *
 * @deprecated apiUrl and wsUrl here are unused; consume runtime.env instead.
 */
export const env = {
  /** @deprecated Use runtime.env.apiUrl from useAppRuntime() */
  apiUrl: import.meta.env.VITE_API_URL || "http://localhost:8000/api/v1",
  /** @deprecated Use runtime.env.wsUrl from useAppRuntime() */
  wsUrl: import.meta.env.VITE_WS_URL || "ws://localhost:8000/realtime",
  appUrl: import.meta.env.VITE_APP_URL || "http://localhost:5173",
  marketingUrl: import.meta.env.VITE_MARKETING_URL || "http://localhost:3001",
  mockApi: import.meta.env.VITE_MOCK_API === "true",
} as const;
