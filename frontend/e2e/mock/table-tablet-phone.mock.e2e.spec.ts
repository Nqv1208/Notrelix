import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The tablet and phone drawings of the board table (UC-WM-D04, D05, D06, I01) at 1180, 1024, 768 and 390 px in
 * light and dark: no sideways page scroll, no serious axe violation on the grid or list, the due picker, the
 * bulk bar, the no-results note and the row-menu sheet, and every control of those surfaces is a 44 px target.
 * `WM_SHOTS_DIR` writes a screenshot of each state for review.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const TABLE =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=table";

test.skip(
  scenario !== "default" || persona !== "owner" || faults !== "",
  "Assertions need the default owner world",
);

async function open(
  page: Page,
  width: number,
  height: number,
  dark: boolean,
  query = "",
) {
  await page.setViewportSize({ width, height });
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
  await page.goto(`${TABLE}${query}`);
  await expect(
    width < 768
      ? page.locator("[data-slot=table-phone-list]")
      : page.getByRole("table"),
  ).toBeVisible();
  await page.waitForTimeout(500);
}

const serious = async (page: Page, skip: string[] = []) =>
  (
    await new AxeBuilder({ page }).disableRules(skip).analyze()
  ).violations.filter((violation) =>
    ["serious", "critical"].includes(violation.impact ?? ""),
  );

const shot = async (page: Page, name: string, width: number, dark: boolean) => {
  if (shots)
    await page.screenshot({
      path: `${shots}/${name}-${width}-${dark ? "dark" : "light"}.png`,
    });
};

/**
 * Every button, checkbox, option and select inside `scope` is a 44 px target: its box is 44 px high, or the point
 * 21 px above and below its centre still reaches it (the hit area a control extends past its drawing).
 */
async function expectTargets(page: Page, scope: string, label: string) {
  const bad = await page.evaluate((selector) => {
    const root = document.querySelector(selector);
    if (!root) return ["missing scope"];
    return [
      ...root.querySelectorAll(
        "button, [role=checkbox], [role=option], [role=menuitem], select, input[type=date]",
      ),
    ]
      .filter(
        (element) =>
          element.getClientRects().length > 0 &&
          getComputedStyle(element).visibility !== "hidden",
      )
      .filter((element) => {
        const box = element.getBoundingClientRect();
        if (box.height >= 43.5) return false;
        const x = box.x + box.width / 2;
        return [-21, 21].some((dy) => {
          const hit = document.elementFromPoint(x, box.y + box.height / 2 + dy);
          return !hit || !element.contains(hit);
        });
      })
      .map(
        (element) =>
          `${element.getAttribute("aria-label") ?? element.textContent?.trim().slice(0, 30)} ${element.getBoundingClientRect().height.toFixed(1)}`,
      );
  }, scope);
  expect(bad, `${label}: targets under 44 px`).toEqual([]);
}

for (const [width, height] of [
  [1180, 820],
  [1024, 768],
  [768, 1024],
  [390, 844],
] as const) {
  for (const dark of [false, true]) {
    const label = `${width} ${dark ? "dark" : "light"}`;
    const phone = width < 768;

    test(`the table stays inside the page with no serious axe violation, ${label}`, async ({
      page,
    }) => {
      test.setTimeout(90_000);
      await open(page, width, height, dark);
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      );
      expect(overflow, "the page scrolls sideways").toBeLessThanOrEqual(0);
      await shot(page, "table", width, dark);
      expect(await serious(page)).toEqual([]);
      // The grid's rows and the list's rows are 44 px targets.
      await expectTargets(
        page,
        phone ? "[data-slot=table-phone-list]" : "[role=table] [data-row-id]",
        label,
      );
    });

    test(`the due picker has no serious axe violation and only 44 px targets, ${label}`, async ({
      page,
    }) => {
      test.setTimeout(90_000);
      test.skip(width >= 1280, "Desktop keeps the calendar");
      await open(page, width, height, dark);
      if (phone) {
        await page
          .getByRole("button", { name: "Thao tác khác cho ERP-144" })
          .click();
        await page
          .getByRole("dialog")
          .getByRole("button", { name: /^Đổi hạn…/ })
          .click();
      } else {
        await page.getByRole("button", { name: "Đổi hạn của ERP-144" }).click();
      }
      const surface = page.locator(
        phone ? "[role=dialog]" : "[data-slot=popover-content]",
      );
      await expect(surface.getByRole("combobox")).toBeVisible();
      await page.waitForTimeout(400);
      const box = (await surface.boundingBox())!;
      expect(box.x).toBeGreaterThanOrEqual(-0.5);
      expect(box.x + box.width).toBeLessThanOrEqual(width + 0.5);
      await shot(page, "table-due", width, dark);
      await expectTargets(
        page,
        phone ? "[role=dialog]" : "[data-slot=popover-content]",
        `due picker ${label}`,
      );
      // An open modal layer hides the page behind it while its buttons stay focusable: the primitive's behaviour.
      expect(await serious(page, ["aria-hidden-focus"])).toEqual([]);
    });

    test(`the row menu sheet has no serious axe violation and only 44 px targets, ${label}`, async ({
      page,
    }) => {
      test.setTimeout(90_000);
      test.skip(!phone, "The sheet is the phone's");
      await open(page, width, height, dark);
      await page
        .getByRole("button", { name: "Thao tác khác cho ERP-144" })
        .click();
      await expect(page.getByRole("dialog")).toBeVisible();
      await page.waitForTimeout(400);
      await shot(page, "table-sheet", width, dark);
      await expectTargets(page, "[role=dialog]", `sheet ${label}`);
      expect(await serious(page, ["aria-hidden-focus"])).toEqual([]);
    });

    test(`the bulk bar has no serious axe violation and only 44 px buttons, ${label}`, async ({
      page,
    }) => {
      test.setTimeout(90_000);
      test.skip(phone, "The phone list has no multi-select");
      await open(page, width, height, dark);
      await page.getByRole("checkbox", { name: "Chọn ERP-142" }).click();
      await page.getByRole("checkbox", { name: "Chọn ERP-146" }).click();
      await expect(
        page.getByRole("toolbar", { name: /đã chọn/ }),
      ).toBeVisible();
      await page.waitForTimeout(500);
      await shot(page, "table-bulk", width, dark);
      await expectTargets(page, "[data-table-bulk-bar]", `bulk bar ${label}`);
      expect(await serious(page)).toEqual([]);
    });

    test(`the no-results note has no serious axe violation and a 44 px Bỏ lọc, ${label}`, async ({
      page,
    }) => {
      test.setTimeout(90_000);
      await open(page, width, height, dark, "&filter=p.low~o");
      const note = page
        .getByRole("status")
        .filter({ hasText: "Không có việc khớp bộ lọc" });
      await expect(note).toBeVisible();
      await shot(page, "table-no-results", width, dark);
      const clear = (await note
        .getByRole("button", { name: "Bỏ lọc" })
        .boundingBox())!;
      if (width < 1280) expect(clear.height).toBeGreaterThanOrEqual(43.5);
      expect(await serious(page)).toEqual([]);
    });
  }
}
