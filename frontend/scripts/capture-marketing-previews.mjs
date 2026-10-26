#!/usr/bin/env node
/**
 * Captures the product screenshots the marketing hero shows.
 *
 * Starts the web app in mock mode (same env as playwright.mock.config.ts, port
 * 5174, reusing a server that is already running), opens the ERP board in each
 * view and each theme, and writes
 * apps/marketing/public/product/{table,kanban,timeline,calendar}-{light,dark}.png
 * at 1440x900 @2x. Run `pnpm marketing:previews` after the web UI changes.
 */
import { spawn } from "node:child_process";
import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "@playwright/test";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const outDir = resolve(root, "apps/marketing/public/product");
const PORT = 5174;
const ORIGIN = `http://127.0.0.1:${PORT}`;
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const VIEWS = ["table", "kanban", "timeline", "calendar"];
const THEMES = ["light", "dark"];

const env = {
  ...process.env,
  VITE_API_URL: "http://127.0.0.1:59999",
  VITE_WS_URL: "ws://127.0.0.1:59998",
  VITE_APP_URL: ORIGIN,
  VITE_RELEASE_SHA: "mock-e2e",
  VITE_MOCK_API: "true",
  VITE_MOCK_LATENCY: "instant",
  VITE_MOCK_SEED: "1001",
  VITE_MOCK_PERSONA: "owner",
  VITE_MOCK_STATE: "default",
};

async function isUp() {
  try {
    const res = await fetch(ORIGIN);
    return res.ok;
  } catch {
    return false;
  }
}

async function startServer() {
  if (await isUp()) return null;
  const child = spawn(
    "pnpm",
    [
      "--filter",
      "@notrelix/app-web",
      "dev",
      "--host",
      "127.0.0.1",
      "--port",
      String(PORT),
    ],
    { cwd: root, env, stdio: "ignore" },
  );
  const deadline = Date.now() + 120_000;
  while (Date.now() < deadline) {
    if (await isUp()) return child;
    await new Promise((r) => setTimeout(r, 500));
  }
  child.kill();
  throw new Error("The web app did not start in time");
}

const HIDE_CSS = `
  *, *::before, *::after { animation: none !important; transition: none !important; caret-color: transparent !important; }
  *:focus, *:focus-visible { outline: none !important; box-shadow: none !important; }
  [data-sonner-toaster], [data-sonner-toast], [role="status"][data-radix-toast-viewport], ol[data-radix-toast-viewport],
  [data-mock-badge], [data-testid="mock-badge"], [data-testid*="mock-banner"], [data-tanstack-router-devtools],
  .tsqd-parent-container, [class*="ReactQueryDevtools"] { display: none !important; }
`;

async function capture(browser, theme, view) {
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 2,
    timezoneId: "Asia/Ho_Chi_Minh",
    colorScheme: theme,
    locale: "en-US",
  });
  await context.addInitScript(
    ([mode]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "en");
        window.localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Storage unavailable: the defaults apply.
      }
    },
    [theme],
  );
  const page = await context.newPage();
  await page.clock.setFixedTime(NOW);
  await page.goto(`${ORIGIN}${BOARD}?view=${view}`);
  await page
    .getByText(/ERP-\d+/)
    .first()
    .waitFor({ state: "visible", timeout: 30_000 });
  await page.waitForLoadState("networkidle");
  await page.addStyleTag({ content: HIDE_CSS });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(600);
  const file = resolve(outDir, `${view}-${theme}.png`);
  await page.screenshot({ path: file });
  await context.close();
  console.log(`wrote ${file}`);
}

mkdirSync(outDir, { recursive: true });
const server = await startServer();
let failed = false;
try {
  const browser = await chromium.launch();
  try {
    for (const theme of THEMES) {
      for (const view of VIEWS) await capture(browser, theme, view);
    }
  } finally {
    await browser.close();
  }
} catch (error) {
  failed = true;
  console.error(error);
} finally {
  if (server) server.kill();
}
process.exit(failed ? 1 : 0);
