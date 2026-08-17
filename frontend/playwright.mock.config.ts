import { defineConfig, devices } from "@playwright/test";

const mockEnv = {
  VITE_API_URL: "http://127.0.0.1:59999",
  VITE_WS_URL: "ws://127.0.0.1:59998",
  VITE_APP_URL: "http://127.0.0.1:5174",
  VITE_RELEASE_SHA: "mock-e2e",
  VITE_MOCK_API: "true",
  VITE_MOCK_PERSONA: process.env.VITE_MOCK_PERSONA ?? "owner",
  VITE_MOCK_SCENARIO: process.env.VITE_MOCK_SCENARIO ?? "default",
  VITE_MOCK_LATENCY_MS: "0",
};

export default defineConfig({
  testDir: "./e2e/mock",
  testMatch: "**/*.mock.e2e.spec.ts",
  fullyParallel: false,
  workers: 1,
  reporter: "list",
  use: { baseURL: mockEnv.VITE_APP_URL, trace: "on-first-retry" },
  webServer: {
    command: "pnpm --filter @notrelix/app-web dev --host 127.0.0.1 --port 5174",
    url: mockEnv.VITE_APP_URL,
    env: mockEnv,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
