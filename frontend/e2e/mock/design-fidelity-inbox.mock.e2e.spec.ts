import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The Inbox page against the design canvas artboard Final-Inbox (`binh-thuong`) at 1440 x 900, light and dark.
 * Numbers were read from the artboard rendered with docs/design/mockups/2026-10/render.py; tolerance is 1 px.
 * The mock list opens on the artboard's second row (a mention that needs a reply), so the row numbers are
 * that row's. Not compared: the keyboard hint strip of the artboard (a product rule drops it).
 */
const INBOX = "/workspaces/mock-workspace-primary/inbox";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(INBOX, { waitUntil: "domcontentloaded" });
  await expect(page.locator("[data-f=lbar]")).toBeVisible();
}

async function box(locator: Locator) {
  return (await locator.first().boundingBox())!;
}

function at(
  b: { x: number; y: number; width: number; height: number },
  want: [number, number, number, number],
  label: string,
) {
  const got = [b.x, b.y, b.width, b.height];
  want.forEach((w, i) =>
    expect
      .soft(
        Math.abs(got[i]! - w),
        `${label}: ${got.map((n) => n.toFixed(1))} vs ${want}`,
      )
      .toBeLessThanOrEqual(1),
  );
}

for (const dark of [false, true]) {
  test(`inbox boxes match Final-Inbox, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    await open(page, dark);
    // [x, y, width, height] in page pixels.
    at(await box(page.locator("h1")), [272, 17.5, 77.1, 29], "title");
    at(
      await box(page.locator("[data-f=lbar]")),
      [256, 150.4, 440, 45],
      "filter bar",
    );
    at(
      await box(page.locator("[data-f=tog]")),
      [268, 158.4, 101.9, 28],
      "unread toggle",
    );
    at(
      await box(page.locator("[data-f=day]")),
      [256, 195.4, 440, 33.4],
      "day header",
    );
    const row = page.locator("[data-f=day] ~ *").first();
    at(await box(row), [256, 228.8, 440, 127.1], "row");
    at(await box(row.locator("time")), [653.9, 239.8, 26.1, 104.1], "row time");
    at(
      await box(page.locator("[data-f=rbar]")),
      [697, 0, 743, 53],
      "reading bar",
    );
    at(
      await box(page.locator("[data-f=open]")),
      [1312.7, 9, 111.3, 34],
      "open button",
    );
    at(
      await box(page.locator("[data-f=event]")),
      [725, 77, 687, 43.4],
      "event line",
    );
    at(await box(page.locator("[data-f=foot]")), [697, 847, 743, 53], "foot");
    const done = await box(page.locator("[data-f=done]"));
    expect.soft(done.width, "done button width").toBeCloseTo(99, 0);
    expect.soft(done.height, "done button height").toBeCloseTo(32, 0);
  });
}
