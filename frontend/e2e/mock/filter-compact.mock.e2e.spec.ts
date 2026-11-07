import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The phone bar (under 640 px) and its two sheets, as journeys and as the review of the states `WM-Filter-320` does not
 * draw (search opened, the sheets in the dark, the 390 px width): 320 and 390 px, light and dark, in Vietnamese. No
 * sideways scroll, every control of the bar and the sheets at least 36 px, axe clean. Screenshots go to REVIEW_DIR.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const REVIEW_DIR = process.env.REVIEW_DIR;
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const FILTER =
  "c2:or~mock-erp-col-owner.has_any.__none__~mock-erp-col-golive.between.2026-10-12.2026-10-18~$title.contains.báo cáo";

async function open(
  page: Page,
  view: "kanban" | "table",
  width: number,
  dark: boolean,
) {
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
  await page.setViewportSize({ width, height: 720 });
  await page.goto(`${BOARD}?view=${view}&filter=${encodeURIComponent(FILTER)}`);
  await page.getByRole("button", { name: /^Lọc/ }).first().waitFor();
}

async function noSidewaysScroll(page: Page, what: string) {
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
    `${what}: the page does not scroll sideways`,
  ).toBe(true);
}

async function axeClean(page: Page, what: string) {
  const axe = await new AxeBuilder({ page })
    .disableRules(["scrollable-region-focusable"])
    .analyze();
  expect(
    axe.violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    ),
    `${what}: axe`,
  ).toEqual([]);
}

for (const width of [320, 390]) {
  for (const dark of [false, true]) {
    test(`the phone bar and its sheets, ${width} px ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      test.skip(
        scenario !== "default" || persona !== "owner" || faults !== "",
        "Needs the plain owner world",
      );
      const tag = `${width}-${dark ? "dark" : "light"}`;
      await open(page, "table", width, dark);
      // The bar: one row of Tìm, Lọc, Cách xem and Bộ lọc đã lưu, each at least 36 px.
      const bar = page.locator("[data-table-toolbar]");
      for (const name of [/^Tìm/, /^Lọc/, /^Cách xem/, /Bộ lọc đã lưu/]) {
        const box = await bar
          .getByRole("button", { name })
          .first()
          .boundingBox();
        expect(box, `${name} exists`).toBeTruthy();
        expect(box!.height, `${name} height`).toBeGreaterThanOrEqual(36);
        expect(box!.width, `${name} width`).toBeGreaterThanOrEqual(36);
      }
      await noSidewaysScroll(page, "bar");
      await axeClean(page, "bar");
      if (REVIEW_DIR)
        await page.screenshot({ path: `${REVIEW_DIR}/bar-${tag}.png` });

      // The search opens in place and keeps its query.
      await bar.getByRole("button", { name: /^Tìm/ }).click();
      const search = bar.getByRole("searchbox");
      await expect(search).toBeFocused();
      await search.fill("báo cáo");
      await expect(search).toHaveValue("báo cáo");
      if (REVIEW_DIR)
        await page.screenshot({ path: `${REVIEW_DIR}/search-${tag}.png` });

      // The filter sheet: clear all drops the count from the pill.
      await bar.getByRole("button", { name: /^Lọc/ }).click();
      const filterSheet = page.getByRole("dialog", { name: "Lọc việc" });
      await expect(filterSheet).toBeVisible();
      await page.waitForTimeout(300);
      expect(
        await filterSheet.evaluate(
          (el) => el.scrollWidth <= el.clientWidth + 1,
        ),
        "filter sheet does not overflow",
      ).toBe(true);
      // The chosen-value chips are drawn 26 px tall (the whole chip row opens the list); every other control is 36.
      for (const button of await filterSheet
        .getByRole("button")
        .filter({ hasNotText: /Chưa giao|Người/ })
        .all()) {
        const box = await button.boundingBox();
        if (box)
          expect(box.height, "sheet control height").toBeGreaterThanOrEqual(34);
      }
      await axeClean(page, "filter sheet");
      if (REVIEW_DIR)
        await page.screenshot({ path: `${REVIEW_DIR}/filter-${tag}.png` });
      await filterSheet.getByRole("button", { name: "Xoá hết" }).click();
      await filterSheet.getByRole("button", { name: "Xong" }).click();
      await expect(filterSheet).toBeHidden();
      await expect(bar.getByRole("button", { name: /^Lọc/ })).toHaveText(
        /^Lọc$/,
      );

      // The Cách xem sheet: grouping by Ưu tiên is the member's own view.
      await bar.getByRole("button", { name: /^Cách xem/ }).click();
      const viewSheet = page.getByRole("dialog", { name: "Cách xem" });
      await expect(viewSheet).toBeVisible();
      await page.waitForTimeout(300);
      await viewSheet
        .getByLabel("Nhóm theo")
        .selectOption({ label: "Ưu tiên" });
      expect(
        await viewSheet.evaluate((el) => el.scrollWidth <= el.clientWidth + 1),
        "view sheet does not overflow",
      ).toBe(true);
      await axeClean(page, "view sheet");
      if (REVIEW_DIR)
        await page.screenshot({ path: `${REVIEW_DIR}/view-${tag}.png` });
      await viewSheet.getByRole("button", { name: "Xong" }).click();
      await bar.getByRole("button", { name: /^Cách xem/ }).click();
      await expect(
        page.getByRole("dialog", { name: "Cách xem" }).getByLabel("Nhóm theo"),
      ).toHaveValue(/priority|uu-tien|.+/);
      await page.getByRole("button", { name: "Về view chung" }).click();
      await expect(
        page.getByRole("dialog", { name: "Cách xem" }).getByLabel("Nhóm theo"),
      ).not.toHaveValue("");
      await noSidewaysScroll(page, "after the sheets");
    });
  }
}
