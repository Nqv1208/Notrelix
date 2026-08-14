import { defineConfig, devices } from "@playwright/test";

/**
 * Marketing site (Next.js) e2e gate.
 *
 * Runs a production build of @notrelix/app-marketing on port 3000 and
 * executes smoke, accessibility and visual snapshot suites against it.
 */

const marketingEnv = {
  NEXT_PUBLIC_SITE_URL:
    process.env.NEXT_PUBLIC_SITE_URL ?? "http://127.0.0.1:3100",
  NEXT_PUBLIC_WEB_APP_URL:
    process.env.NEXT_PUBLIC_WEB_APP_URL ?? "http://127.0.0.1:5173",
  NEXT_PUBLIC_API_URL:
    process.env.NEXT_PUBLIC_API_URL ?? "https://api.example.invalid",
};

const marketingUrl = "http://127.0.0.1:3100";

export default defineConfig({
  testDir: "./e2e/marketing",
  testMatch: "**/*.marketing.e2e.spec.ts",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI
    ? [
        ["list"],
        [
          "html",
          { open: "never", outputFolder: "playwright-report-marketing" },
        ],
      ]
    : "list",
  use: {
    baseURL: marketingUrl,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  webServer: {
    command: "node ./scripts/marketing-e2e-server.mjs",
    url: marketingUrl,
    env: marketingEnv,
    reuseExistingServer: false,
    timeout: 300_000,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
