import { expect, test, type Page } from "@playwright/test";
import { toNewest } from "./support/chat-newest";

/**
 * Three chat details in a real browser, where the stylesheet applies: the typing dots rise one after another (the
 * unlayered animation shorthand once reset every delay to 0), the jump-to-latest control lifts on hover instead of
 * scaling, and a reaction is one pill over the bubble's bottom corner that hangs 10 px below it.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";

async function open(page: Page, width = 1440, height = 900, theme = "light") {
  await page.setViewportSize({ width, height });
  await page.addInitScript((mode) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", mode);
    } catch {
      // Private mode: the defaults apply.
    }
  }, theme);
  await page.goto(CHANNEL);
  await expect(page.getByRole("log")).toBeVisible({ timeout: 20_000 });
  await page.waitForTimeout(1000);
}

/** Three dots in the markup the typing bubble renders, in the page, so the real stylesheet decides their delays. */
async function dotStyles(page: Page) {
  return page.evaluate(() => {
    const host = document.createElement("span");
    for (let i = 0; i < 3; i++) {
      const dot = document.createElement("span");
      dot.className = "chat-typing-wave size-1.5";
      host.append(dot);
    }
    document.body.append(host);
    const out = Array.from(host.children).map((dot) => {
      const style = getComputedStyle(dot);
      return { delay: style.animationDelay, name: style.animationName };
    });
    host.remove();
    return out;
  });
}

test("the three typing dots start 0, 160 and 320 ms apart; reduced motion keeps no animation", async ({
  page,
}) => {
  await open(page);
  const dots = await dotStyles(page);
  expect(dots.map((dot) => dot.delay)).toEqual(["0s", "0.16s", "0.32s"]);
  for (const dot of dots) expect(dot.name).toBe("chat-typing-wave");
  await page.emulateMedia({ reducedMotion: "reduce" });
  for (const dot of await dotStyles(page)) expect(dot.name).toBe("none");
});

test("the jump-to-latest control lifts 2 px on hover, never scales, and settles on press", async ({
  page,
}) => {
  await open(page);
  const log = page.getByRole("log");
  await log.evaluate((el) => {
    el.scrollTop = 0;
  });
  const jump = page.getByRole("button", {
    name: /^(Jump to latest|Xem tin mới nhất)$/,
  });
  await expect(jump).toBeVisible();
  const transform = () =>
    jump.evaluate((el) => new DOMMatrix(getComputedStyle(el).transform));
  await jump.hover();
  await expect.poll(async () => (await transform()).m42).toBeCloseTo(-2, 0);
  const lifted = await transform();
  expect(lifted.a).toBeCloseTo(1, 5);
  expect(lifted.d).toBeCloseTo(1, 5);
  await page.mouse.down();
  await expect.poll(async () => (await transform()).m42).toBeCloseTo(0, 0);
  await page.mouse.up();
});

for (const theme of ["light", "dark"]) {
  for (const width of [1440, 1024, 390]) {
    test(`a reaction is one pill over the bubble corner, 10 px below it (${width}, ${theme})`, async ({
      page,
    }) => {
      await open(page, width, width === 390 ? 844 : 900, theme);
      await toNewest(page);
      const row = page.locator("[data-own=false]").last();
      await row.scrollIntoViewIfNeeded();
      await row.hover();
      await row.getByRole("button", { name: "Biểu cảm" }).click();
      await page
        .locator("[role=group] button")
        .filter({ hasText: /^[^\w\s]{1,4}$/ })
        .first()
        .click();
      await page.mouse.move(5, 5);
      const mine = page.locator(
        "[data-message-id]:has([data-testid=chat-reactions])",
      );
      const pill = mine.getByRole("button", { name: /^Ai đã bày tỏ: / });
      await expect(pill).toBeVisible();
      await expect(pill).toHaveAttribute("data-mine", "true");
      const bubble = (await mine.locator("[data-bubble]").boundingBox())!;
      const p = (await pill.boundingBox())!;
      expect(p.y + p.height - (bubble.y + bubble.height)).toBeCloseTo(10, 0);
      expect(p.y).toBeLessThan(bubble.y + bubble.height);
      // Someone else's message: the pill is on the bubble's end (right) corner.
      expect(p.x + p.width).toBeLessThanOrEqual(bubble.x + bubble.width + 0.5);
      expect(p.x).toBeGreaterThanOrEqual(bubble.x - 0.5);
      // The row reserves the overhang only: the next message is not pushed 34 px away.
      const rowBox = (await mine.boundingBox())!;
      expect(
        rowBox.y + rowBox.height - (bubble.y + bubble.height),
      ).toBeLessThan(24);
      await pill.click();
      await expect(
        page.getByRole("list", { name: "Ai đã bày tỏ" }),
      ).toBeVisible();
    });
  }
}
