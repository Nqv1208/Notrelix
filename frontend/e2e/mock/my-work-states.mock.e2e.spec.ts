import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * UC-WM-H18 My Work: the loading and load-error states (undrawn: Final-MyWork, Final-T-MyWork and Final-M-MyWork
 * draw neither, so they are built from the list's own rows and the board's load-error block and reviewed instead of
 * measured) at 1440 and 390 px, light and dark, Vietnamese: announced, no sideways scroll, no serious axe
 * violation, a 44 px Thử lại on the phone, and Thử lại reads again. Mock hooks: `__mockMyWorkHold` keeps the read
 * open, `__mockMyWorkFailures` refuses that many reads. The D76.17 rule itself (not done, assigned to me, by due
 * date, undated last) is proved on the server by `MyWorkAndHomeFeedTests.MyWork_D76_17_...`; here the list shows
 * the server's order.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const PAGE = "/workspaces/mock-workspace-primary/my-work";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const shots = process.env.WM_SHOTS_DIR ?? "";

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);

async function open(
  page: Page,
  width: number,
  dark: boolean,
  hooks: { hold?: boolean; failures?: number },
) {
  await page.setViewportSize({ width, height: width < 640 ? 844 : 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ([mode, hold, failures]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode as string);
      } catch {
        // Private mode: the defaults apply.
      }
      const g = globalThis as {
        __mockMyWorkHold?: boolean;
        __mockMyWorkFailures?: number;
      };
      g.__mockMyWorkHold = hold as boolean;
      g.__mockMyWorkFailures = failures as number;
    },
    [
      dark ? "dark" : "light",
      hooks.hold ?? false,
      hooks.failures ?? 0,
    ] as const,
  );
  await page.goto(PAGE);
}

async function reviewed(page: Page, name: string) {
  await page.waitForTimeout(400);
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth,
  );
  expect(overflow, `${name}: no sideways scroll`).toBeLessThanOrEqual(0);
  const serious = (
    await new AxeBuilder({ page }).include("main").analyze()
  ).violations.filter((v) => ["serious", "critical"].includes(v.impact ?? ""));
  expect(
    serious.map((v) => `${v.id}: ${v.nodes.map((n) => n.target).join(", ")}`),
    `${name}: axe`,
  ).toEqual([]);
  if (shots) await page.screenshot({ path: `${shots}/${name}.png` });
}

for (const width of [1440, 390] as const)
  for (const dark of [false, true]) {
    const tag = `${width} ${dark ? "dark" : "light"}`;

    test(`loading at ${tag}: announced rows that fit the screen`, async ({
      page,
    }) => {
      await open(page, width, dark, { hold: true });
      const loading = page.getByRole("status", {
        name: "Đang tải việc của bạn",
      });
      await expect(loading).toBeVisible();
      await expect(loading).toHaveAttribute("aria-busy", "true");
      await expect(loading.locator('[data-f="row"]').first()).toBeVisible();
      await reviewed(page, `my-work-loading-${width}-${dark ? "dem" : "sang"}`);
      // Letting the read go shows the list.
      await page.evaluate(() => {
        (globalThis as { __mockMyWorkHold?: boolean }).__mockMyWorkHold = false;
      });
      await expect(loading).toHaveCount(0);
      await expect(page.locator("[data-item-id]").first()).toBeVisible();
    });

    test(`load error at ${tag}: the block says what failed and Thử lại reads again`, async ({
      page,
    }) => {
      await open(page, width, dark, { failures: 99 });
      const block = page.getByRole("alert").filter({
        hasText: "Không tải được việc của bạn",
      });
      await expect(block).toBeVisible({ timeout: 15_000 });
      await expect(block).toContainText("Dữ liệu của bạn không thay đổi.");
      const retry = block.getByRole("button", { name: "Thử lại" });
      await expect(retry).toBeVisible();
      if (width < 640) {
        const box = await retry.boundingBox();
        expect(
          box?.height ?? 0,
          "Thử lại is 44 px on the phone",
        ).toBeGreaterThanOrEqual(44);
      }
      await reviewed(page, `my-work-error-${width}-${dark ? "dem" : "sang"}`);
      await page.evaluate(() => {
        (globalThis as { __mockMyWorkFailures?: number }).__mockMyWorkFailures =
          0;
      });
      await retry.click();
      await expect(block).toHaveCount(0);
      await expect(page.locator("[data-item-id]").first()).toBeVisible();
    });
  }

test("D76.17: the list shows the due windows in time order", async ({
  page,
}) => {
  await open(page, 1440, false, {});
  await expect(page.locator("[data-item-id]").first()).toBeVisible();
  // Grouped by due window in time order; no row of the open list is a done item.
  const groups = await page
    .locator('[data-f="lcol"] section h2, [data-f="lcol"] section h3')
    .allInnerTexts();
  const order = ["Quá hạn", "Hôm nay", "Tuần này", "Sau", "Không có hạn"];
  const seen = groups
    .map((text) => order.findIndex((label) => text.startsWith(label)))
    .filter((index) => index >= 0);
  expect(seen.length).toBeGreaterThan(1);
  expect([...seen].sort((a, b) => a - b)).toEqual(seen);
});
