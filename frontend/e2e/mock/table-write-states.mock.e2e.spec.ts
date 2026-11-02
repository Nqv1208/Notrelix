import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The board table's single-item writes (UC-WM-B04, B05, D04, D05, D06) at 1440, 1024 and 390 px in light and dark:
 * the table keeps inside the page and axe finds nothing serious, and a status write refused as a lost race tells
 * the person who won in a toast that stays inside the viewport. The refusal needs the server started with
 * `VITE_MOCK_FAULTS=/board-items/mock-erp-item-149/status=409`; the layout runs in the default world.
 * `WM_SHOTS_DIR` writes a screenshot of each state for review.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const TABLE =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=table";

test.beforeEach(() => {
  test.skip(
    scenario !== "default" || persona !== "owner",
    "Assertions need the default owner world",
  );
});

async function open(page: Page, width: number, height: number, dark: boolean) {
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
  await page.goto(TABLE);
  await expect(page.getByRole("rowgroup", { name: "Cần làm" })).toBeVisible();
  await page.waitForTimeout(500);
}

const seriousViolations = async (page: Page, skip: string[] = []) =>
  (
    await new AxeBuilder({ page }).disableRules(skip).analyze()
  ).violations.filter((violation) =>
    ["serious", "critical"].includes(violation.impact ?? ""),
  );

for (const [width, height] of [
  [1440, 900],
  [1024, 768],
  [390, 844],
] as const) {
  for (const dark of [false, true]) {
    const label = `${width} ${dark ? "dark" : "light"}`;

    test(`the table keeps inside the page and its editors have no serious axe violation, ${label}`, async ({
      page,
    }) => {
      test.skip(faults !== "", "Default world");
      // Four lists, an axe run each and the screenshots outlast the 30 s default on a loaded runner.
      test.setTimeout(90_000);
      await open(page, width, height, dark);
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      );
      expect(overflow, "the page scrolls sideways").toBeLessThanOrEqual(0);
      if (shots)
        await page.screenshot({
          path: `${shots}/table-${width}-${dark ? "dark" : "light"}.png`,
        });
      expect(await seriousViolations(page)).toEqual([]);

      // Each list a write starts from opens inside the viewport and has no serious axe violation.
      const pickers: [string, RegExp | string][] = [
        ["status-list", "Đổi trạng thái của ERP-149"],
        ["assignee-list", /Đổi người phụ trách của ERP-149/],
        ["due-calendar", /Đổi hạn của ERP-149/],
        ["priority-menu", /Đổi ưu tiên của ERP-149/],
      ];
      for (const [shot, name] of pickers) {
        const trigger = page.getByRole("button", { name });
        await trigger.scrollIntoViewIfNeeded();
        await trigger.click();
        const open = page.locator(
          "[role=listbox], [role=menu], [data-slot=popover-content]",
        );
        await expect(open.first()).toBeVisible();
        await page.waitForTimeout(300);
        const box = (await open.first().boundingBox())!;
        expect(box.x, `${shot} left edge`).toBeGreaterThanOrEqual(-0.5);
        expect(box.x + box.width, `${shot} right edge`).toBeLessThanOrEqual(
          width + 0.5,
        );
        if (width < 768) {
          // A phone target is 44 px: every day, row, option and button the list holds.
          const targets = open
            .first()
            .locator(
              "[role=option], [role=menuitem], button:not([aria-hidden])",
            );
          const count = await targets.count();
          expect(count, `${shot} targets`).toBeGreaterThan(0);
          for (let index = 0; index < count; index += 1) {
            const target = await targets.nth(index).boundingBox();
            if (target)
              expect(
                target.height,
                `${shot} target ${index}`,
              ).toBeGreaterThanOrEqual(43.5);
          }
        }
        if (shots)
          await page.screenshot({
            path: `${shots}/table-${shot}-${width}-${dark ? "dark" : "light"}.png`,
          });
        // An open Radix menu hides the page behind it with aria-hidden while the page's own buttons stay focusable
        // underneath; that is the menu primitive's modal behaviour (every menu of the app), not a table defect.
        expect(
          await seriousViolations(
            page,
            shot === "priority-menu" ? ["aria-hidden-focus"] : [],
          ),
          shot,
        ).toEqual([]);
        await page.keyboard.press("Escape");
        await expect(open.first()).toBeHidden();
      }
    });

    test(`a status write refused as a lost race names who won in a toast inside the viewport, ${label}`, async ({
      page,
    }) => {
      test.skip(
        !faults.includes("mock-erp-item-149/status=409"),
        "Needs the status 409 fault",
      );
      await open(page, width, height, dark);
      await page
        .getByRole("button", { name: "Đổi trạng thái của ERP-149" })
        .click();
      await page.getByRole("option", { name: /Đang làm/ }).click();
      const toast = page.getByText(/vừa sửa “[^”]*” trước bạn/);
      await expect(toast).toBeVisible();
      await expect(toast).toContainText("Thay đổi của bạn chưa được lưu");
      await page.waitForTimeout(800); // the toast has slid in
      const box = (await toast.boundingBox())!;
      expect(box.x).toBeGreaterThanOrEqual(0);
      expect(box.x + box.width).toBeLessThanOrEqual(width + 0.5);
      expect(box.y + box.height).toBeLessThanOrEqual(height + 0.5);
      if (shots)
        await page.screenshot({
          path: `${shots}/table-conflict-${width}-${dark ? "dark" : "light"}.png`,
        });
      expect(await seriousViolations(page)).toEqual([]);
      // The row is back in its group.
      await expect(
        page
          .getByRole("rowgroup", { name: "Cần làm" })
          .locator("[data-row-id]")
          .filter({ hasText: "ERP-149" }),
      ).toHaveCount(1);
    });
  }
}

for (const dark of [false, true]) {
  test(`at 390 px every control a write starts from is a 44 px target, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(faults !== "", "Default world");
    await open(page, 390, 844, dark);
    const row = page.locator('[data-row-id="mock-erp-item-149"]');
    await expect(row).toBeVisible();
    const rowBox = (await row.boundingBox())!;
    expect(rowBox.height).toBeGreaterThanOrEqual(43.5);

    // A control's target is its box plus its hit area: the point 21 px above and below its centre still reaches it.
    const reach = async (
      name: string | RegExp,
      role: "button" | "checkbox",
    ) => {
      const control = row.getByRole(role, { name }).first();
      await control.scrollIntoViewIfNeeded();
      const box = (await control.boundingBox())!;
      const x = box.x + box.width / 2;
      for (const dy of [-21, 21]) {
        const hit = await page.evaluate(
          ([px, py]) =>
            document.elementFromPoint(px!, py!)?.closest("button") !== null,
          [x, box.y + box.height / 2 + dy],
        );
        expect(hit, `${String(name)} at ${dy}`).toBe(true);
      }
    };
    await reach("Chọn ERP-149", "checkbox");
    await reach("Đổi trạng thái của ERP-149", "button");
    for (const name of [
      /Đổi người phụ trách của ERP-149/,
      /Đổi hạn của ERP-149/,
    ]) {
      const box = (await row.getByRole("button", { name }).boundingBox())!;
      expect(box.height, String(name)).toBeGreaterThanOrEqual(43.5);
    }
  });
}
