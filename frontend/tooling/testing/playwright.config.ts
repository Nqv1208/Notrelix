import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "../../apps/web/e2e",
  timeout: 30000,
  retries: 2,
  use: {
    baseURL: "http://localhost:5173",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "chromium",
      use: { browserName: "chromium" },
    },
  ],
  webServer: {
    command: "pnpm --filter @notrelix/app-web dev",
    port: 5173,
    reuseExistingServer: !process.env.CI,
  },
});
