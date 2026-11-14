import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * My work on a tablet and a phone (UC-WM-H-series list screens): at 1180, 1024, 768 and 390 px in light and dark
 * there is no sideways page scroll, no serious axe violation on the list, the filter popover, the row menu and the
 * add popover, and every control is a 44 px target; the tabs, the group chip, the filter chip and the row menu
 * work. `WM_SHOTS_DIR` writes a screenshot of each state for review.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const PAGE = "/workspaces/mock-workspace-primary/my-work";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  scenario !== "default" || persona !== "owner" || faults !== "",
  "Assertions need the default owner world",
);

async function open(page: Page, width: number, height: number, dark: boolean) {
  await page.setViewportSize({ width, height });
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
  await page.goto(PAGE);
  await expect(
    page.locator('[data-f="row"][data-item-id]').first(),
  ).toBeVisible();
  await page.waitForTimeout(400);
}

const serious = async (page: Page) =>
  (await new AxeBuilder({ page }).analyze()).violations.filter((violation) =>
    ["serious", "critical"].includes(violation.impact ?? ""),
  );

const shot = async (page: Page, name: string, width: number, dark: boolean) => {
  if (shots)
    await page.screenshot({
      path: `${shots}/my-work-${name}-${width}-${dark ? "dark" : "light"}.png`,
    });
};

/** Every control inside the page's main region is a 44 px target (the tick, the chips, the menu, the buttons). */
async function expectTargets(page: Page, label: string) {
  const bad = await page.evaluate(() =>
    [
      ...document.querySelectorAll(
        '[data-f="main"] button, [data-f="main"] [role=option], [data-f="main"] [role=tab]',
      ),
    ]
      .filter((element) => element.getClientRects().length > 0)
      .filter((element) => element.getBoundingClientRect().height < 43.5)
      .map(
        (element) =>
          element.getAttribute("aria-label") ??
          element.textContent?.slice(0, 20),
      ),
  );
  expect(bad, `${label}: controls under 44 px`).toEqual([]);
}

for (const [width, height] of [
  [1180, 820],
  [1024, 768],
  [768, 1024],
  [390, 844],
] as const) {
  for (const dark of [false, true]) {
    test(`${width} ${dark ? "dark" : "light"}: no overflow, no serious axe, 44 px targets, popovers`, async ({
      page,
    }) => {
      await open(page, width, height, dark);
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      );
      expect(overflow, "sideways scroll").toBeLessThanOrEqual(0);
      expect(await serious(page), "list").toEqual([]);
      await expectTargets(page, "list");
      await shot(page, "list", width, dark);

      // Row menu: open the item or tick it.
      await page.getByRole("button", { name: "Thao tác cho ERP-142" }).click();
      const menu = page.getByRole("menu", { name: "Thao tác cho ERP-142" });
      await expect(menu).toBeVisible();
      expect(await serious(page), "row menu").toEqual([]);
      await expectTargets(page, "row menu");
      await shot(page, "row-menu", width, dark);
      await page.keyboard.press("Escape");
      await expect(menu).toBeHidden();

      // Filter popover: department, board, priority and the search.
      await page.getByRole("button", { name: /^Lọc/ }).click();
      const filter = page.locator('[data-f="filterpop"]');
      await expect(filter).toBeVisible();
      expect(await serious(page), "filter popover").toEqual([]);
      await expectTargets(page, "filter popover");
      await shot(page, "filter", width, dark);
      await page.keyboard.press("Escape");
      await expect(filter).toBeHidden();

      // Add popover.
      await page.getByRole("button", { name: "Thêm việc" }).click();
      await expect(page.locator('[data-f="createpop"]')).toBeVisible();
      expect(await serious(page), "add popover").toEqual([]);
      await shot(page, "add", width, dark);
    });
  }
}

test("tablet: tabs, group chip, filter and the row menu act", async ({
  page,
}) => {
  await open(page, 1180, 820, false);
  const rows = page.locator('[data-f="row"][data-item-id]');
  const total = await rows.count();

  await page.getByRole("tab", { name: /Cần tôi duyệt/ }).click();
  await expect(rows).toHaveCount(1);
  await page.getByRole("tab", { name: /Tất cả/ }).click();
  await expect(rows).toHaveCount(total);

  await page.getByRole("button", { name: /Nhóm theo/ }).click();
  await page.getByRole("menuitemradio", { name: "Theo board" }).click();
  await expect(page.locator('[data-f="gh"]').first()).toContainText(
    "Triển khai ERP Q4",
  );

  await page.getByRole("button", { name: /^Lọc/ }).click();
  await page
    .getByRole("searchbox", { name: "Tìm trong việc của tôi" })
    .fill("SSO");
  await expect(rows).toHaveCount(1);
  await page.getByRole("button", { name: "Xong" }).click();
  await expect(page.getByRole("button", { name: /^Lọc/ })).toBeVisible();
});

test("phone: chips switch the list and the filter sheet carries the group", async ({
  page,
}) => {
  await open(page, 390, 844, false);
  const rows = page.locator('[data-f="row"][data-item-id]');
  await page.getByRole("tab", { name: /Giao cho tôi/ }).click();
  expect(await rows.count()).toBeGreaterThan(0);
  await page.getByRole("tab", { name: /Tất cả/ }).click();

  await page.getByRole("button", { name: "Lọc" }).click();
  await page.getByRole("menuitemradio", { name: "Theo board" }).click();
  await expect(page.locator('[data-f="gh"]').first()).toContainText(
    "Triển khai ERP Q4",
  );
});
