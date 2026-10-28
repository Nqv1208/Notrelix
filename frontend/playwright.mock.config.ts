import { readdirSync, readFileSync } from "node:fs";
import { defineConfig, devices } from "@playwright/test";

// A set MOCK_E2E_PORT (the pre-push run) gets its own dev server: the default
// port may hold another checkout's server, which must never answer for this one.
const PORT = process.env.MOCK_E2E_PORT ?? "5174";

const baseEnv = {
  VITE_API_URL: "http://127.0.0.1:59999",
  VITE_WS_URL: "ws://127.0.0.1:59998",
  VITE_APP_URL: `http://127.0.0.1:${PORT}`,
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

// The workspace shows Hanoi time, and the fixtures are written in it: pin the
// browser zone so the runner's own zone (UTC on CI) does not move clock labels.
const TIME_ZONE = "Asia/Ho_Chi_Minh";

// Outside owner/default only the specs that read VITE_MOCK_PERSONA run: they
// adapt to or skip the scenario themselves. The others assume the owner world.
const isDefaultScenario =
  (process.env.VITE_MOCK_PERSONA ?? "owner") === "owner" &&
  (process.env.VITE_MOCK_STATE ?? "default") === "default";
const scenarioAwareSpecs = isDefaultScenario
  ? []
  : readdirSync("./e2e/mock").filter(
      (name) =>
        name.endsWith(".mock.e2e.spec.ts") &&
        readFileSync(`./e2e/mock/${name}`, "utf8").includes(
          "VITE_MOCK_PERSONA",
        ),
    );

export default defineConfig({
  testDir: "./e2e/mock",
  testMatch: isDefaultScenario ? "**/*.mock.e2e.spec.ts" : scenarioAwareSpecs,
  // The design-fidelity specs measure boxes to 1px against the Design render
  // on the dev machine; the CI runner rasterizes the same web font a few
  // pixels wider, so they only run locally (pnpm e2e:mock).
  testIgnore: process.env.CI
    ? /design-fidelity-.*\.mock\.e2e\.spec\.ts$/
    : undefined,
  fullyParallel: false,
  workers: 1,
  reporter: [
    ["list"],
    ["json", { outputFile: "test-results/mock-e2e-results.json" }],
  ],
  use: {
    baseURL: baseEnv.VITE_APP_URL,
    timezoneId: TIME_ZONE,
    trace: "on-first-retry",
  },
  webServer: {
    command: `pnpm --filter @notrelix/app-web dev --host 127.0.0.1 --port ${PORT}`,
    url: baseEnv.VITE_APP_URL,
    env: serverEnv,
    reuseExistingServer: !process.env.CI && !process.env.MOCK_E2E_PORT,
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
