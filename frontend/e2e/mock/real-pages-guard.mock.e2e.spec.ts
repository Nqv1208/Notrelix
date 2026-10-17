import { expect, test, type Page } from "@playwright/test";

/**
 * Guard for the real Home, My work and Inbox pages of the mock world: what a
 * person sees must never be seed filler, an English fallback, a missing
 * route or an absurd overdue count. Stories can match a mockup while the
 * real page binds to junk data; this fails when the world drifts back.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "/workspaces/mock-workspace-primary";
const NOW = new Date("2026-10-12T10:40:00+07:00");

const FORBIDDEN =
  /Task \d|Notification \d|You were mentioned|Page not found|Quá hạn \d{3,}/;

const PAGES = [
  { name: "Home", path: WORKSPACE, ready: /^Chào buổi sáng, / },
  { name: "My work", path: `${WORKSPACE}/my-work`, ready: "Việc của tôi" },
  { name: "Inbox", path: `${WORKSPACE}/inbox`, ready: "Hộp thư" },
] as const;

async function open(page: Page, path: string, ready: string | RegExp) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(path);
  await expect(
    page.getByRole("heading", { level: 1, name: ready }),
  ).toBeVisible();
}

test.describe("Real Home, My work and Inbox", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );

  for (const size of [
    { width: 1440, height: 900 },
    { width: 1280, height: 800 },
  ]) {
    for (const { name, path, ready } of PAGES) {
      test(`${name} shows no filler, English fallback or absurd overdue at ${size.width}px`, async ({
        page,
      }) => {
        await page.setViewportSize(size);
        await open(page, path, ready);
        // Let the lists and the sidebar settle before reading the text.
        await page.waitForTimeout(500);
        const text = await page.locator("body").innerText();
        expect(text).not.toMatch(FORBIDDEN);
      });
    }
  }

  test("a three-digit overdue label stays on one line on Home and My work", async ({
    page,
  }) => {
    // The page clock far past the due dates: ERP-142 is 630+ days overdue.
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the default locale applies.
      }
    });
    await page.clock.setFixedTime(new Date("2028-07-12T10:40:00+07:00"));
    for (const [path, cell] of [
      [WORKSPACE, '[data-f="due"]'],
      [`${WORKSPACE}/my-work`, '[data-f="du"]'],
    ] as const) {
      await page.goto(path);
      const label = page
        .locator(cell, { hasText: /Quá hạn \d{3} ngày/ })
        .first();
      await expect(label).toBeVisible();
      const lineHeight = await label.evaluate(
        (node) => parseFloat(getComputedStyle(node).lineHeight) || 18,
      );
      const lines = await label.evaluate(
        (node) =>
          Array.from(node.children).filter(
            (child) => !child.classList.contains("sr-only"),
          ).length,
      );
      const box = await label.boundingBox();
      // One line, plus the small date line under it on My work.
      expect(box!.height).toBeLessThan(lineHeight * (1 + lines) + 2);
      // And the text stays in its column (a few px of platform text-width slack).
      const spill = await label.evaluate(
        (node) => node.scrollWidth - node.clientWidth,
      );
      // The Design fixes this column at 112px and the label nearly fills it on
      // the dev machine; other platforms set the web font a few px wider.
      expect(spill).toBeLessThanOrEqual(8);
    }
  });
});
