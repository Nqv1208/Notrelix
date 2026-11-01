import {
  assertPortsAvailable,
  down,
  logs,
  realBackend,
  runFrontend,
  start,
  stopProcess,
  up,
  waitForUrl,
} from "./real-backend-stack.mjs";

const webEnv = {
  ...process.env,
  VITE_API_URL: realBackend.apiUrl,
  VITE_WS_URL: "ws://127.0.0.1:58000/api/v1/realtime",
  VITE_APP_URL: realBackend.appUrl,
  REAL_E2E_BASE_URL: realBackend.appUrl,
  VITE_RELEASE_SHA: "real-e2e",
  VITE_MOCK_API: "false",
};

const operatorEnv = {
  ...process.env,
  VITE_OPERATOR_API_URL: realBackend.operatorApiUrl,
  REAL_E2E_OPERATOR_URL: realBackend.operatorUrl,
};

let preview;
let operatorPreview;
let failed = false;

try {
  await down();
  await assertPortsAvailable();
  await up();
  await waitForUrl(realBackend.healthUrl);
  await runFrontend("pnpm", ["--filter", "@notrelix/app-web", "build"], {
    env: webEnv,
  });
  preview = start(
    "pnpm",
    [
      "--filter",
      "@notrelix/app-web",
      "preview",
      "--host",
      "127.0.0.1",
      "--port",
      realBackend.webPort,
    ],
    { env: webEnv },
  );
  await waitForUrl(realBackend.appUrl, 120_000);
  await runFrontend("pnpm", ["--filter", "@notrelix/app-operator", "build"], {
    env: operatorEnv,
  });
  operatorPreview = start(
    "pnpm",
    [
      "--filter",
      "@notrelix/app-operator",
      "preview",
      "--host",
      "127.0.0.1",
      "--port",
      realBackend.operatorPort,
    ],
    { env: operatorEnv },
  );
  await waitForUrl(realBackend.operatorUrl, 120_000);
  // Anything after the script name (spec paths, --grep) goes to Playwright.
  await runFrontend("pnpm", ["e2e:real:test", ...process.argv.slice(2)], {
    env: { ...webEnv, ...operatorEnv },
  });
} catch (error) {
  failed = true;
  console.error(error);
  try {
    await logs();
  } catch (logsError) {
    console.error("Unable to collect real-backend logs", logsError);
  }
} finally {
  await stopProcess(preview);
  await stopProcess(operatorPreview);
  try {
    await down();
  } catch (cleanupError) {
    failed = true;
    console.error("Unable to clean the real-backend stack", cleanupError);
  }
}

if (failed) process.exitCode = 1;
