import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e/ui",
  testMatch: "**/*.spec.ts",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI
    ? [
        ["list"],
        ["html", { open: "never", outputFolder: "playwright-report-ui" }],
      ]
    : "list",
  use: {
    baseURL: "http://127.0.0.1:6006",
    trace: "on-first-retry",
  },
  webServer: {
    command:
      "pnpm --filter @notrelix/storybook-web build && pnpm --filter @notrelix/storybook-web exec vite preview --host 127.0.0.1 --port 6006 --outDir storybook-static",
    url: "http://127.0.0.1:6006",
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
