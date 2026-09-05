import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e/real",
  testMatch: "**/*.real.e2e.spec.ts",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI
    ? [
        ["list"],
        ["html", { open: "never", outputFolder: "playwright-report-real" }],
      ]
    : "list",
  use: {
    baseURL: "http://127.0.0.1:5173",
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
