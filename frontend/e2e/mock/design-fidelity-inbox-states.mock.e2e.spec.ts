import { expect, test, type Page } from "@playwright/test";

/**
 * The Inbox states of the design canvas board Col-Inbox (snoozedList, doneUndo) at 1440, 1024 and 390, light and
 * dark; tolerance 1 px. Numbers are read from the board's own CSS (doccol-base.css and Col-Inbox.dc.html):
 *  - "Quay lại" pill (`.pill.info` in the row's `small`): 22 px high, 0 8 px padding, 5 px gap, 13 px clock,
 *    12 px / 600 text, fully rounded, 3 px under the line above;
 *  - toast: Col-Inbox draws a centred 44 px toast; the app has one toast (Final-States `.toast`, bottom left, padding
 *    10 10 10 14, radius 10) that every screen uses, so the Done toast is measured against that and the board's
 *    centred position is recorded as a gap;
 *  - unread entry: the row keeps its box when it turns unread and draws the 7 px dot.
 */
const INBOX = "/workspaces/mock-workspace-primary/inbox";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const SIZES = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
];

async function open(page: Page, size: (typeof SIZES)[number], dark: boolean) {
  await page.setViewportSize(size);
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

function near(actual: number, want: number, label: string) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(1);
}

async function selectFirstRow(page: Page) {
  await page.locator("[data-f=it]").first().click();
  await expect(page.locator("[data-f=foot]")).toBeVisible();
}

for (const size of SIZES) {
  for (const dark of [false, true]) {
    const label = `${size.width}, ${dark ? "dark" : "light"}`;

    test(`snoozed row pill matches Col-Inbox snoozedList, ${label}`, async ({
      page,
    }) => {
      await open(page, size, dark);
      await selectFirstRow(page);
      await page
        .locator("[data-f=foot]")
        .getByRole("button", { name: /Nhắc lại sau/ })
        .click();
      await page.getByRole("menuitem").first().click();
      await page.locator("[data-f=tog-snoozed]").click();
      const pill = page.locator("[data-f=snoozed-when]").first();
      await expect(pill).toBeVisible();
      await expect(pill).toContainText(/^Quay lại /);
      const b = (await pill.boundingBox())!;
      near(b.height, 22, "pill height");
      const geometry = await pill.evaluate((el) => {
        const c = getComputedStyle(el);
        const svg = el.querySelector("svg")!.getBoundingClientRect();
        const r = el.getBoundingClientRect();
        const prev = el.previousElementSibling!;
        return {
          padL: parseFloat(c.paddingLeft),
          padR: parseFloat(c.paddingRight),
          gap: parseFloat(c.columnGap),
          icon: svg.width,
          iconX: svg.x - r.x,
          font: c.fontSize,
          weight: c.fontWeight,
          radius: parseFloat(c.borderTopLeftRadius),
          above: r.y - prev.getBoundingClientRect().bottom,
        };
      });
      near(geometry.padL, 8, "pill left padding");
      near(geometry.padR, 8, "pill right padding");
      near(geometry.gap, 5, "pill gap");
      near(geometry.icon, 13, "clock size");
      near(geometry.iconX, 8, "clock x");
      near(geometry.above, 3, "gap under the line above");
      expect(geometry.font).toBe("12px");
      expect(geometry.weight).toBe("600");
      expect(geometry.radius).toBeGreaterThanOrEqual(11);
    });

    test(`done toast offers undo for about 5 s on the app toast, ${label}`, async ({
      page,
    }) => {
      await open(page, size, dark);
      await selectFirstRow(page);
      await page.locator("[data-f=done]").click();
      const toast = page.locator("[data-sonner-toast]").first();
      await expect(toast).toBeVisible();
      await expect(toast).toContainText("Đã chuyển sang Đã xử lý");
      const undo = toast.getByRole("button", { name: "Hoàn tác" });
      await expect(undo).toBeVisible();
      await page.waitForTimeout(600);
      const t = (await toast.boundingBox())!;
      const u = (await undo.boundingBox())!;
      const style = await toast.evaluate((el) => {
        const c = getComputedStyle(el);
        return {
          radius: parseFloat(c.borderTopLeftRadius),
          padL: parseFloat(c.paddingLeft),
          padR: parseFloat(c.paddingRight),
        };
      });
      // The app's one toast (Final-States `.toast`), not Col-Inbox's centred 44 px variant.
      near(style.radius, 10, "toast radius");
      near(style.padL, 14, "toast left padding");
      near(style.padR, 10, "toast right padding");
      near(t.x + t.width - (u.x + u.width), 10, "undo right inset");
      expect(t.y + t.height).toBeLessThanOrEqual(size.height);
      // The undo lasts about 5 s, then the toast goes.
      await expect(toast).toBeHidden({ timeout: 8000 });
    });

    test(`mark as unread keeps the row box and draws the dot, ${label}`, async ({
      page,
    }) => {
      await open(page, size, dark);
      // The All tab draws read entries too (the Needs tab lists only what is open).
      await page.getByRole("tab", { name: /^Tất cả/ }).click();
      const rows = page.locator("[data-f=it]");
      const row = rows.first();
      await row.click();
      await page.locator("[data-f=handled]").click();
      await expect(page.locator("[data-f=mark-unread]")).toBeVisible();
      const before = (await row.boundingBox())!;
      await page.locator("[data-f=mark-unread]").click();
      await expect(row.locator("[data-f=ud]")).toBeVisible();
      const after = (await rows.first().boundingBox())!;
      near(after.height, before.height, "row height");
      near(after.width, before.width, "row width");
      const dot = (await row.locator("[data-f=ud]").boundingBox())!;
      near(dot.width, 7, "dot width");
      near(dot.x - after.x, 5, "dot x");
    });
  }
}
