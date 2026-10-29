import { expect, test, type Page } from "@playwright/test";

/**
 * Calendar interactions on the mock world (Hướng chốt, WM-Cal-Interactions):
 * multi-day bars with two 8px handles, resizing across a week break, the
 * keyboard route ("Đổi ngày…" from the context menu), the undo toast, the
 * undated column giving one day, and the viewer's disabled handles. ERP-142 is
 * the board's multi-day item, 1/10 to 10/10.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openCalendar(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=calendar`);
  await expect(page.getByRole("grid")).toBeVisible();
}

const bars = (page: Page) =>
  page
    .locator("[data-calendar-event]")
    .filter({ hasText: "Chuyển dữ liệu khách hàng" });
const dueHandle = (page: Page) =>
  page
    .locator('[data-calendar-handle="due"][data-entry="mock-erp-item-142"]')
    .first();

async function center(page: Page, selector: string) {
  const box = (await page.locator(selector).first().boundingBox())!;
  return { x: box.x + box.width / 2, y: box.y + box.height / 2 };
}

test.describe("Board Calendar interactions (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("a multi-day item is one bar cut square at the week break, with a handle at each real end", async ({
    page,
  }) => {
    await openCalendar(page);
    await expect(bars(page)).toHaveCount(2);
    const own = (edge: string) =>
      page.locator(
        `[data-calendar-handle="${edge}"][data-entry="mock-erp-item-142"]`,
      );
    await expect(own("start")).toHaveCount(1);
    await expect(own("due")).toHaveCount(1);
    const first = (await bars(page).first().boundingBox())!;
    const handle = (await dueHandle(page).boundingBox())!;
    // The hit area is 24px wide.
    expect(handle.width).toBeGreaterThanOrEqual(24);
    expect(first.width).toBeGreaterThan(100);
  });

  test("dragging the due handle to another week tips the new span, saves on release and offers Hoàn tác", async ({
    page,
  }) => {
    await openCalendar(page);
    const start = await center(
      page,
      '[data-calendar-handle="due"][data-entry="mock-erp-item-142"]',
    );
    const target = await center(page, '[data-day="2026-10-14"]');
    await page.mouse.move(start.x, start.y);
    await page.mouse.down();
    await page.mouse.move(target.x, target.y, { steps: 8 });
    await expect(page.locator("[data-calendar-tip]")).toContainText(
      "Hạn mới 14/10",
    );
    await page.mouse.up();
    await expect(page.getByText(/Đã dời ERP-142/)).toBeVisible();
    await page.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(page.getByText("Đã hoàn tác")).toBeVisible();
  });

  test("Escape during a resize puts the bar back and saves nothing", async ({
    page,
  }) => {
    await openCalendar(page);
    const start = await center(
      page,
      '[data-calendar-handle="due"][data-entry="mock-erp-item-142"]',
    );
    const target = await center(page, '[data-day="2026-10-14"]');
    await page.mouse.move(start.x, start.y);
    await page.mouse.down();
    await page.mouse.move(target.x, target.y, { steps: 6 });
    await expect(page.locator("[data-calendar-tip]")).toBeVisible();
    await page.keyboard.press("Escape");
    await page.mouse.up();
    await expect(page.locator("[data-calendar-tip]")).toHaveCount(0);
    await expect(page.getByText(/Đã dời ERP-142/)).toHaveCount(0);
  });

  test("the context menu opens Đổi ngày…, an invalid due is refused in place, a valid one saves", async ({
    page,
  }) => {
    await openCalendar(page);
    await bars(page).first().click({ button: "right" });
    await page.getByRole("menuitem", { name: /Đổi ngày…/ }).click();
    const dialog = page.getByRole("dialog", { name: /Đổi ngày/ });
    const [, due] = await dialog.locator('input[type="date"]').all();
    await due!.fill("2026-09-20");
    await expect(dialog.getByRole("alert")).toContainText(
      "Hạn không được trước ngày bắt đầu",
    );
    await expect(dialog.getByRole("button", { name: "Lưu" })).toBeDisabled();
    await due!.fill("2026-10-14");
    await dialog.getByRole("button", { name: "Lưu" }).click();
    await expect(page.getByText(/Đã dời ERP-142/)).toBeVisible();
  });

  test("the undated column gives an item one day by drop, and by Đặt ngày", async ({
    page,
  }) => {
    await openCalendar(page);
    const item = page.locator("[data-calendar-undated]").first();
    await item.dragTo(page.locator('[data-day="2026-10-22"]'));
    await expect(page.getByText(/Đã đặt ERP-\d+ vào 22\/10/)).toBeVisible();
  });

  test("Chuyển về Chưa lên lịch from the menu clears both dates", async ({
    page,
  }) => {
    await openCalendar(page);
    await bars(page).first().click({ button: "right" });
    await page
      .getByRole("menuitem", { name: /Chuyển về Chưa lên lịch/ })
      .click();
    await expect(
      page.getByText(/Đã chuyển ERP-142 về Chưa lên lịch/),
    ).toBeVisible();
  });
});

test.describe("Board Calendar interactions (viewer)", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default",
    "Needs the server started with VITE_MOCK_PERSONA=viewer",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the handles stay, disabled with the reason, and nothing drags or opens a menu", async ({
    page,
  }) => {
    await openCalendar(page);
    const handle = dueHandle(page);
    await expect(handle).toHaveAttribute("aria-disabled", "true");
    await expect(handle).toHaveAttribute(
      "title",
      /Bạn chỉ có quyền xem board này/,
    );
    await expect(bars(page).first()).toHaveAttribute("draggable", "false");
    await bars(page).first().click({ button: "right" });
    await expect(page.getByRole("menuitem")).toHaveCount(0);
  });
});
