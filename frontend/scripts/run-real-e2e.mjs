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
  VITE_WS_URL: "ws://127.0.0.1:58000/realtime",
  VITE_APP_URL: realBackend.appUrl,
  VITE_RELEASE_SHA: "real-e2e",
  VITE_MOCK_API: "false",
};

let preview;
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
      "5173",
    ],
    { env: webEnv },
  );
  await waitForUrl(realBackend.appUrl, 120_000);
  await runFrontend("pnpm", ["e2e:real:test"], { env: webEnv });
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
  try {
    await down();
  } catch (cleanupError) {
    failed = true;
    console.error("Unable to clean the real-backend stack", cleanupError);
  }
}

if (failed) process.exitCode = 1;
