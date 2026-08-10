import { defineConfig, devices } from "@playwright/test";

const productionEnv = {
  VITE_API_URL:
    process.env.VITE_API_URL ?? "https://api.example.invalid/api/v1",
  VITE_WS_URL: process.env.VITE_WS_URL ?? "wss://api.example.invalid/realtime",
  VITE_APP_URL: process.env.VITE_APP_URL ?? "http://127.0.0.1:4173",
  VITE_RELEASE_SHA: process.env.VITE_RELEASE_SHA ?? "local-e2e",
  VITE_MOCK_API: "false",
};

export default defineConfig({
  testDir: "./e2e/production",
  testMatch: "**/*.e2e.spec.ts",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI
    ? [
        ["list"],
        [
          "html",
          { open: "never", outputFolder: "playwright-report-production" },
        ],
      ]
    : "list",
  use: {
    baseURL: productionEnv.VITE_APP_URL,
    trace: "on-first-retry",
  },
  webServer: {
    command:
      "pnpm --filter @notrelix/app-web preview --host 127.0.0.1 --port 4173",
    url: "http://127.0.0.1:4173",
    env: productionEnv,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
