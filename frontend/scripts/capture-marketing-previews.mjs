#!/usr/bin/env node
/**
 * Captures the product screenshots the marketing site shows.
 *
 * Starts the web app in mock mode (same env as playwright.mock.config.ts, port
 * 5174, reusing a server that is already running) and writes, at 1440x900 @2x,
 * light and dark, to apps/marketing/public/product/:
 *  - {table,kanban,timeline,calendar}-{theme}.png: the hero (the ERP board in
 *    each view; the boxes of the app's own view tabs go to
 *    hero-preview-hotspots.json);
 *  - showcase-{item,docs,automation,reports}-{theme}.png: the Showcase screens
 *    (the boxes of the regions its notes point at go to
 *    showcase-annotations.json);
 *  - story-{inbox,sharing,departments}-{theme}.png: crops of one region each
 *    (their sizes go to story-crops.json).
 * `--only=hero|showcase|story|<id>` captures a part. Run
 * `pnpm marketing:previews` after the web UI changes.
 */
import { spawn } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
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
const TAB_NAMES = {
  table: "Table",
  kanban: "Kanban",
  calendar: "Calendar",
  timeline: "Timeline",
};
const VIEWPORT = { width: 1440, height: 900 };
const hotspotsFile = resolve(
  root,
  "apps/marketing/src/sections/hero-preview-hotspots.json",
);
// view tab boxes (percent of the viewport) recorded per capture
const recorded = [];

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

async function openContext(browser, theme) {
  const context = await browser.newContext({
    viewport: VIEWPORT,
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
  return context;
}

async function settle(page) {
  await page.waitForLoadState("networkidle");
  await page.addStyleTag({ content: HIDE_CSS });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(700);
}

async function capture(browser, theme, view) {
  const context = await openContext(browser, theme);
  const page = await context.newPage();
  await page.clock.setFixedTime(NOW);
  await page.goto(`${ORIGIN}${BOARD}?view=${view}`);
  await page
    .getByText(/ERP-\d+/)
    .first()
    .waitFor({ state: "visible", timeout: 30_000 });
  await settle(page);
  const boxes = {};
  for (const [id, name] of Object.entries(TAB_NAMES)) {
    const box = await page
      .getByRole("tablist", { name: /^views$/i })
      .getByRole("tab", { name })
      .first()
      .boundingBox();
    if (!box) throw new Error(`No ${name} view tab in ${view}-${theme}`);
    boxes[id] = {
      left: (box.x / VIEWPORT.width) * 100,
      top: (box.y / VIEWPORT.height) * 100,
      width: (box.width / VIEWPORT.width) * 100,
      height: (box.height / VIEWPORT.height) * 100,
    };
  }
  recorded.push({ key: `${view}-${theme}`, boxes });
  const file = resolve(outDir, `${view}-${theme}.png`);
  await page.screenshot({ path: file });
  await context.close();
  console.log(`wrote ${file}`);
}

/* ------------------------------------------------------------------ */
/* Showcase screens and story crops                                    */
/* ------------------------------------------------------------------ */

const W = "/workspaces/mock-workspace-primary";
const r3 = (n) => Math.round(n * 1000) / 1000;

/** The box that spans every located element, in px of the page. */
async function unionBox(locators) {
  let box = null;
  for (const locator of locators) {
    const b = await locator.first().boundingBox();
    if (!b) throw new Error("annotation target not found");
    box = box
      ? {
          x: Math.min(box.x, b.x),
          y: Math.min(box.y, b.y),
          right: Math.max(box.right, b.x + b.width),
          bottom: Math.max(box.bottom, b.y + b.height),
        }
      : { x: b.x, y: b.y, right: b.x + b.width, bottom: b.y + b.height };
  }
  return box;
}

/** Scrolls the nearest scrollable ancestor so the element sits `top` px from its top. */
async function scrollTo(locator, top) {
  await locator.first().evaluate((element, offset) => {
    let node = element.parentElement;
    while (
      node &&
      !(
        /(auto|scroll)/.test(getComputedStyle(node).overflowY) &&
        node.scrollHeight > node.clientHeight + 1
      )
    ) {
      node = node.parentElement;
    }
    if (!node) return;
    const delta =
      element.getBoundingClientRect().top - node.getBoundingClientRect().top;
    node.scrollTop += delta - offset;
  }, top);
  await locator.page().waitForTimeout(300);
}

const SHOWCASE = {
  item: {
    path: `${W}/boards/mock-board-erp/items/mock-erp-item-138`,
    async prepare(page) {
      await page
        .getByText(/^Waiting for /)
        .first()
        .waitFor();
      await page.getByText("Cần duyệt trước thứ Tư").waitFor();
    },
    async afterSettle(page) {
      await scrollTo(page.getByText("Activity", { exact: true }).first(), 20);
    },
    places: {
      approval: "above-end",
      comments: "above-end",
      properties: "below-end",
    },
    notes: (page) => ({
      approval: [
        page.getByText("Approval", { exact: true }),
        page.getByText(/^Waiting for /).first(),
      ],
      comments: [
        page.getByText("Cần duyệt trước thứ Tư"),
        page.getByPlaceholder(/Write a comment/),
      ],
      properties: [
        page.getByText("Manage fields"),
        page.getByText("Show 3 empty fields"),
      ],
    }),
  },
  docs: {
    path: `${W}/docs/mock-doc-invoice-process`,
    async prepare(page) {
      await page.getByRole("heading", { level: 1 }).waitFor();
      await page.getByText("Tasks mentioned").waitFor();
    },
    places: { chip: "right", links: "below-end", approve: "below-end" },
    notes: (page) => ({
      chip: [
        page.getByText("ERP-144", { exact: true }).first(),
        page.getByText(/Kiểm thử phân hệ công nợ với số liệu tháng 9/).first(),
      ],
      links: [
        page.getByText("Tasks mentioned"),
        page.getByText("Document · Công nghệ"),
      ],
      approve: [page.getByRole("button", { name: "Approve document" })],
    }),
  },
  automation: {
    path: `${W}/boards/mock-board-erp/settings?section=automations`,
    async prepare(page) {
      const card = page.getByRole("listitem", {
        name: "Hand a tested item to the approver",
      });
      await card.waitFor();
      await card.getByRole("button", { name: "Run history" }).click();
      await page.getByText("Stopped at action 2").waitFor();
    },
    places: {
      rule: "below-end",
      history: "right",
      failure: "below-end",
    },
    notes: (page) => {
      const card = page.getByRole("listitem", {
        name: "Hand a tested item to the approver",
      });
      return {
        rule: [
          card.getByText("Hand a tested item to the approver"),
          card.getByText("Version 2", { exact: true }),
        ],
        history: [
          card.getByText("Succeeded", { exact: true }).first(),
          card.getByText(/^10\/12\/2026/),
          card.getByText("Version 2", { exact: true }).nth(1),
          card.getByText("Action 2: Notify a person in the app").first(),
        ],
        failure: [
          card.getByText("Failed", { exact: true }).first(),
          card.getByText(/can no longer see this Board/),
        ],
      };
    },
  },
  reports: {
    path: `${W}/reports?report=mock-report-erp`,
    async prepare(page) {
      await page
        .getByRole("heading", { name: "Tổng quan triển khai ERP Q4" })
        .waitFor();
      await page.getByText("Go-live may slip.").waitFor();
    },
    places: { summary: "right", pace: "above-end", workload: "above-end" },
    notes: (page) => ({
      summary: [
        page.getByText(/5\/16 items done/),
        page.getByText(/days to go-live/).first(),
      ],
      pace: [
        page.getByRole("heading", { name: "Completion pace" }),
        page.getByText(/This compares paces/),
      ],
      workload: [
        page.getByRole("heading", { name: "Workload by assignee" }),
        page.getByText("Unassigned", { exact: true }).last(),
      ],
    }),
  },
};

/**
 * Story crops: each captures one screen and keeps only the region that proves
 * the claim (css px of the viewport; the file is that region at 2x).
 */
const STORY = {
  inbox: {
    path: `${W}/inbox`,
    viewport: { width: 1440, height: 290 },
    clip: { x: 696, y: 0, width: 744, height: 290 },
    async prepare(page) {
      await page.getByRole("button", { name: "Mark as done" }).waitFor();
    },
  },
  sharing: {
    path: `${W}/boards/mock-board-erp?view=table`,
    viewport: { width: 1440, height: 1000 },
    async clipOf(page) {
      const box = await page.getByRole("dialog").boundingBox();
      // the top of the dialog: access and the permission list
      return { x: box.x, y: box.y, width: box.width, height: 580 };
    },
    async prepare(page) {
      await page
        .getByText(/ERP-\d+/)
        .first()
        .waitFor();
      await page
        .getByRole("button", { name: /^Share/ })
        .first()
        .click();
      await page.getByRole("dialog").waitFor();
    },
  },
  departments: {
    path: `${W}/spaces/mock-space-engineering`,
    viewport: { width: 1440, height: 640 },
    clip: { x: 1100, y: 160, width: 340, height: 440 },
    async prepare(page) {
      await page.getByText("Running Boards").waitFor();
    },
  },
};

function writeJson(file, data) {
  writeFileSync(file, `${JSON.stringify(data, null, 2)}\n`);
  console.log(`wrote ${file}`);
}

async function captureScreen(browser, theme, kind, id, def, annotations) {
  const context = await openContext(browser, theme);
  const page = await context.newPage();
  await page.clock.setFixedTime(NOW);
  if (def.viewport) await page.setViewportSize(def.viewport);
  await page.goto(`${ORIGIN}${def.path}`);
  await page.waitForLoadState("domcontentloaded");
  await def.prepare(page);
  await settle(page);
  if (def.afterSettle) await def.afterSettle(page);
  if (def.notes && theme === "light") {
    const found = {};
    for (const [key, locators] of Object.entries(def.notes(page))) {
      const raw = await unionBox(locators);
      // a little air around the region, kept inside the screenshot
      const pad = 8;
      const x = Math.max(raw.x - pad, 2);
      const y = Math.max(raw.y - pad, 2);
      const right = Math.min(raw.right + pad, VIEWPORT.width - 2);
      const bottom = Math.min(raw.bottom + pad, VIEWPORT.height - 2);
      found[key] = {
        ...(def.places?.[key] ? { place: def.places[key] } : {}),
        left: r3((x / VIEWPORT.width) * 100),
        top: r3((y / VIEWPORT.height) * 100),
        width: r3(((right - x) / VIEWPORT.width) * 100),
        height: r3(((bottom - y) / VIEWPORT.height) * 100),
      };
    }
    annotations[id] = found;
  }
  const file = resolve(outDir, `${kind}-${id}-${theme}.png`);
  const clip = def.clipOf ? await def.clipOf(page) : def.clip;
  await page.screenshot({ path: file, ...(clip ? { clip } : {}) });
  if (clip && theme === "light") {
    annotations[id] = {
      width: Math.round(clip.width * 2),
      height: Math.round(clip.height * 2),
    };
  }
  await context.close();
  console.log(`wrote ${file}`);
}

function writeHotspots() {
  // The view tabs sit in the same place in every view and theme; the active
  // tab is set heavier so allow a sub-pixel-ish drift, then keep the widest box.
  const tolerance = 0.5; // percent of the viewport
  const merged = {};
  for (const id of Object.keys(TAB_NAMES)) {
    const all = recorded.map((r) => r.boxes[id]);
    for (const [i, box] of all.entries()) {
      for (const k of ["left", "top"]) {
        if (Math.abs(box[k] - all[0][k]) > tolerance) {
          throw new Error(`${id} tab ${k} differs in ${recorded[i].key}`);
        }
      }
    }
    const left = Math.min(...all.map((b) => b.left));
    const top = Math.min(...all.map((b) => b.top));
    const right = Math.max(...all.map((b) => b.left + b.width));
    const bottom = Math.max(...all.map((b) => b.top + b.height));
    const r = (n) => Math.round(n * 1000) / 1000;
    merged[id] = {
      left: r(left),
      top: r(top),
      width: r(right - left),
      height: r(bottom - top),
    };
  }
  writeFileSync(hotspotsFile, `${JSON.stringify(merged, null, 2)}\n`);
  console.log(`wrote ${hotspotsFile}`);
}

const only = process.argv.find((a) => a.startsWith("--only="))?.slice(7);
const cropsFile = resolve(root, "apps/marketing/src/sections/story-crops.json");
const annotationsFile = resolve(
  root,
  "apps/marketing/src/sections/showcase-annotations.json",
);

mkdirSync(outDir, { recursive: true });
const server = await startServer();
let failed = false;
try {
  const browser = await chromium.launch();
  try {
    if (!only || only === "hero") {
      for (const theme of THEMES) {
        for (const view of VIEWS) await capture(browser, theme, view);
      }
      writeHotspots();
    }
    const crops = {};
    for (const [id, def] of Object.entries(STORY)) {
      if (only && only !== "story" && only !== id) continue;
      for (const theme of THEMES) {
        await captureScreen(browser, theme, "story", id, def, crops);
      }
    }
    if (Object.keys(crops).length > 0) {
      const previous = existsSync(cropsFile)
        ? JSON.parse(readFileSync(cropsFile, "utf8"))
        : {};
      writeJson(cropsFile, { ...previous, ...crops });
    }
    const annotations = {};
    for (const [id, def] of Object.entries(SHOWCASE)) {
      if (only !== "showcase" && only !== id && only) continue;
      for (const theme of THEMES) {
        await captureScreen(browser, theme, "showcase", id, def, annotations);
      }
    }
    if (Object.keys(annotations).length > 0) {
      const previous = existsSync(annotationsFile)
        ? JSON.parse(readFileSync(annotationsFile, "utf8"))
        : {};
      writeJson(annotationsFile, { ...previous, ...annotations });
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
