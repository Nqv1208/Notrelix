import { defineConfig, devices } from "@playwright/test";

const baseEnv = {
  VITE_API_URL: "http://127.0.0.1:59999",
  VITE_WS_URL: "ws://127.0.0.1:59998",
  VITE_APP_URL: "http://127.0.0.1:5174",
  VITE_RELEASE_SHA: "mock-e2e",
  VITE_MOCK_API: "true",
  VITE_MOCK_LATENCY: "instant",
  VITE_MOCK_SEED: "1001",
};

// Helper to create per-scenario env
function scenarioEnv(persona: string, state: string) {
  return { ...baseEnv, VITE_MOCK_PERSONA: persona, VITE_MOCK_STATE: state };
}

// The web server is started once; env is set via Playwright projects.
// All projects share the same dev server port, with env injected per run.
// NOTE: In CI each project runs in a separate invocation with VITE_MOCK_* overrides.
const serverEnv = scenarioEnv(
  process.env.VITE_MOCK_PERSONA ?? "owner",
  process.env.VITE_MOCK_STATE ?? "default",
);

export default defineConfig({
  testDir: "./e2e/mock",
  testMatch: "**/*.mock.e2e.spec.ts",
  fullyParallel: false,
  workers: 1,
  reporter: [
    ["list"],
    ["json", { outputFile: "test-results/mock-e2e-results.json" }],
  ],
  use: {
    baseURL: baseEnv.VITE_APP_URL,
    trace: "on-first-retry",
  },
  webServer: {
    command: "pnpm --filter @notrelix/app-web dev --host 127.0.0.1 --port 5174",
    url: baseEnv.VITE_APP_URL,
    env: serverEnv,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
  // One Chromium project; persona/state controlled via env variables at launch.
  // CI runs pnpm e2e:mock multiple times with different VITE_MOCK_PERSONA and VITE_MOCK_STATE.
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
