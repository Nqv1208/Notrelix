import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * UC-WM-B33 and UC-WM-D02: the in-cell version conflict of the table's built-in Title and Due date cells. A rename
 * or a due date saved after someone else saved the item first is refused as stale (412, naming who saved first);
 * the cell then offers "Giữ giá trị của tôi" or "Dùng giá trị mới", as the custom field cells do, instead of a toast
 * that drops the typed value. The note is the one `WM-Table-Columns` slice 3 draws ("Xung đột phiên bản trong ô"),
 * measured as boxes, tolerance 1 px, light and dark: 316 px wide, 10 px radius, 12 px side padding, two 26 px
 * buttons with a 7 px radius, 8 px side padding and 12 px type, 8 px apart.
 *
 * Mock hook: `globalThis.__mockItemSavedFirst[itemId]` stores the other member's title or due date and refuses
 * this write once.
 */
const TABLE =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=table";
const ITEM = "mock-erp-item-142";
const TOL = 1;
const near = (a: number, w: number, what: string) =>
  expect(
    Math.abs(a - w),
    `${what}: ${a.toFixed(1)} vs ${w}`,
  ).toBeLessThanOrEqual(TOL);

async function open(page: Page, dark: boolean, race: Record<string, unknown>) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.addInitScript(
    ({ mode, item, race }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Private mode: the defaults apply.
      }
      (
        globalThis as { __mockItemSavedFirst?: Record<string, unknown> }
      ).__mockItemSavedFirst = { [item]: race };
    },
    { mode: dark ? "dark" : "light", item: ITEM, race },
  );
  await page.goto(TABLE);
  await page.waitForTimeout(600);
  return page.locator(`[data-row-id="${ITEM}"]`).first();
}

async function rename(page: Page, row: Locator, title: string) {
  await row.locator("button[data-row-open]").dblclick();
  const input = page.getByRole("textbox", { name: /Sửa tên/ });
  await input.fill(title);
  await input.press("Enter");
  return page.getByRole("alertdialog", { name: "Xung đột giá trị" });
}

async function rel(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

for (const dark of [false, true]) {
  test(`the title conflict note, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    const row = await open(page, dark, {
      title: "Đối soát hoá đơn quý 4",
      byName: "Hà Nguyễn",
    });
    const dialog = await rename(page, row, "Đối soát hoá đơn tháng 12");
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText(
      "Hà Nguyễn vừa đổi Tên công việc thành Đối soát hoá đơn quý 4",
    );
    await expect(dialog).toContainText(
      "Giá trị của bạn: Đối soát hoá đơn tháng 12",
    );
    await page.waitForTimeout(400);
    const note = dialog.locator("xpath=..");
    const box = (await note.boundingBox())!;
    near(box.width, 316, "note width");
    expect(
      await note.evaluate((el) => {
        const s = getComputedStyle(el);
        return [s.borderRadius, s.paddingTop, s.paddingLeft];
      }),
    ).toEqual(["10px", "10px", "12px"]);
    const mine = dialog.getByRole("button", { name: "Giữ giá trị của tôi" });
    const theirs = dialog.getByRole("button", { name: /Dùng giá trị mới/ });
    const a = await rel(mine, note);
    const b = await rel(theirs, note);
    near(a.h, 26, "keep height");
    near(b.h, 26, "use height");
    near(a.x, 13, "keep x");
    near(b.x - (a.x + a.w), 8, "gap between the buttons");
    near(a.y, b.y, "buttons on one line");
    for (const button of [mine, theirs])
      expect(
        await button.evaluate((el) => {
          const s = getComputedStyle(el);
          return [s.fontSize, s.borderRadius, s.paddingLeft];
        }),
      ).toEqual(["12px", "7px", "8px"]);
    // "Dùng giá trị mới" takes the focus, as on the custom field cells.
    await expect(theirs).toBeFocused();
  });
}

test("keep mine writes the typed title on the newest version", async ({
  page,
}) => {
  const row = await open(page, false, {
    title: "Đối soát hoá đơn quý 4",
    byName: "Hà Nguyễn",
  });
  const dialog = await rename(page, row, "Đối soát hoá đơn tháng 12");
  await expect(dialog).toBeVisible();
  // The row already shows the other member's title under the choice.
  await expect(row.locator("button[data-row-open]")).toHaveText(
    /Đối soát hoá đơn quý 4/,
  );
  await dialog.getByRole("button", { name: "Giữ giá trị của tôi" }).click();
  await expect(dialog).toBeHidden();
  await expect(row.locator("button[data-row-open]")).toHaveText(
    /Đối soát hoá đơn tháng 12/,
  );
  // The second write was accepted: no choice comes back and no failure is told.
  await page.waitForTimeout(500);
  await expect(dialog).toBeHidden();
  await expect(page.getByText(/thay đổi.*trước/)).toHaveCount(0);
});

test("use theirs drops the typed title and sends nothing", async ({ page }) => {
  const row = await open(page, false, {
    title: "Đối soát hoá đơn quý 4",
    byName: "Hà Nguyễn",
  });
  const dialog = await rename(page, row, "Đối soát hoá đơn tháng 12");
  await expect(dialog).toBeVisible();
  await dialog.getByRole("button", { name: /Dùng giá trị mới/ }).click();
  await expect(dialog).toBeHidden();
  await expect(row.locator("button[data-row-open]")).toHaveText(
    /Đối soát hoá đơn quý 4/,
  );
});

test("a due date saved after someone else's shows the same choice", async ({
  page,
}) => {
  const row = await open(page, false, {
    dueDate: "2026-12-20T10:00:00.000Z",
    byName: "Hà Nguyễn",
  });
  await row.getByRole("button", { name: /Đổi hạn/ }).click();
  const day = page.getByRole("gridcell").filter({ hasText: /^15$/ }).first();
  await day.click();
  const dialog = page.getByRole("alertdialog", { name: "Xung đột giá trị" });
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText("Hà Nguyễn vừa đổi Hạn thành");
  await expect(dialog).toContainText("Giá trị của bạn:");
  await dialog.getByRole("button", { name: /Dùng giá trị mới/ }).click();
  await expect(dialog).toBeHidden();
});
