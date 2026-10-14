import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Board Timeline behaviour in the mock world: what a person does with the
 * scale, the window, the items and the bars. Geometry against the mockup
 * lives in `design-fidelity-timeline.mock.e2e.spec.ts`. Controls whose data
 * the board does not have (dependencies, the critical path, grouping and bar
 * span choices, adding an item from here) look like the design and are inert
 * with a reason; they are checked here as inert, never as working.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openTimeline(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=timeline${query}`);
  await expect(page.getByRole("grid")).toBeVisible();
  await expect(page.locator('[data-tl="gb"]').first()).toBeVisible();
}

const listRow = (page: Page, key: string) =>
  page.locator('[data-tl="lrow"]').filter({ hasText: key }).first();

async function trackOf(page: Page, key: string): Promise<Locator> {
  const id = await listRow(page, key).getAttribute("data-row-id");
  return page.locator(`[data-tl="trow"][data-row-id="${id}"]`);
}

const dueOf = (page: Page, key: string) =>
  listRow(page, key).locator(":scope > span").last();

const scale = (page: Page, name: "Ngày" | "Tuần" | "Tháng") =>
  page.getByRole("radiogroup", { name: "Thang thời gian" }).getByRole("radio", {
    name,
  });

test.describe("Board Timeline (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the scale switch redraws the header and the bars", async ({ page }) => {
    await openTimeline(page);
    await expect(scale(page, "Ngày")).toHaveAttribute("aria-checked", "true");
    await expect(page.locator('[data-tl="day"]')).toHaveCount(29);
    const widthDay = (
      await (
        await trackOf(page, "ERP-146")
      )
        .locator('[data-tl="gb"]')
        .boundingBox()
    )?.width;

    await scale(page, "Tuần").click();
    await expect(scale(page, "Tuần")).toHaveAttribute("aria-checked", "true");
    await expect(page.locator('[data-tl="wk"]')).toHaveCount(6);
    await expect(page.locator('[data-tl="day"]')).toHaveCount(0);
    const widthWeek = (
      await (
        await trackOf(page, "ERP-146")
      )
        .locator('[data-tl="gb"]')
        .boundingBox()
    )?.width;
    expect(widthWeek).toBeLessThan(widthDay ?? 0);

    await scale(page, "Tháng").click();
    await expect(page.locator('[data-tl="mt"]')).toHaveCount(17);
    await expect(page.locator('[data-tl="wk"]')).toHaveCount(0);

    // Arrow keys move along the radio group, like the Calendar's range.
    await scale(page, "Tháng").focus();
    await page.keyboard.press("ArrowRight");
    await expect(scale(page, "Ngày")).toHaveAttribute("aria-checked", "true");
    await expect(scale(page, "Ngày")).toBeFocused();
    await expect(page.locator('[data-tl="day"]')).toHaveCount(29);
  });

  test("back, forward and today move the window", async ({ page }) => {
    await openTimeline(page);
    const firstDay = page.locator('[data-tl="day"]').first();
    await expect(firstDay).toContainText("5");
    await page.getByRole("button", { name: "Tiến", exact: true }).click();
    await expect(firstDay).toContainText("12");
    await expect(page.locator('[data-tl="month"]').first()).toHaveText(
      "Tháng 10, 2026",
    );
    await page.getByRole("button", { name: "Lùi", exact: true }).click();
    await page.getByRole("button", { name: "Lùi", exact: true }).click();
    await expect(firstDay).toContainText("28");
    await expect(page.locator('[data-tl="month"]').first()).toHaveText(
      "Tháng 9",
    );
    await page.getByRole("button", { name: "Cuộn tới hôm nay" }).click();
    await expect(firstDay).toContainText("5");
    await expect(page.locator('[data-tl="todayline"]')).toBeVisible();
  });

  test("the key and the bar open the item; Escape closes it", async ({
    page,
  }) => {
    await openTimeline(page);
    await listRow(page, "ERP-142")
      .getByRole("link", { name: /ERP-142/ })
      .first()
      .click();
    const sheet = page.getByRole("dialog");
    await expect(sheet).toBeVisible();
    await expect(page).toHaveURL(/item=/);
    await expect(sheet).toContainText("Chuyển dữ liệu khách hàng");
    await page.keyboard.press("Escape");
    await expect(sheet).toHaveCount(0);
    await expect(page).not.toHaveURL(/item=/);

    // A click on a bar (no drag) opens the same sheet.
    await (
      await trackOf(page, "ERP-146")
    )
      .locator('[data-tl="gb"]')
      .click({ position: { x: 20, y: 10 } });
    await expect(sheet).toBeVisible();
    await expect(sheet).toContainText("Đồng bộ tồn kho");
    await page.keyboard.press("Escape");
    await expect(sheet).toHaveCount(0);

    // The sheet can also be opened straight from the address.
    await page.goto(
      `${BOARD}?view=timeline&item=${await listRow(page, "ERP-144").getAttribute("data-row-id")}`,
    );
    await expect(page.getByRole("dialog")).toContainText(
      "Kiểm thử phân hệ công nợ",
    );
  });

  test("controls without data are inert and say why", async ({ page }) => {
    await openTimeline(page);
    const links = page.getByRole("button", { name: /^Phụ thuộc/ });
    const critical = page.getByRole("button", { name: /^Đường găng/ });
    const groupBy = page.getByRole("button", { name: /^Nhóm theo/ });
    const barSpan = page.getByRole("button", { name: /^Thanh dài từ/ });
    const add = page.getByRole("button", { name: /^Thêm công việc/ });
    for (const control of [links, critical, groupBy, barSpan, add]) {
      await expect(control).toHaveAttribute("aria-disabled", "true");
      await expect(control).toHaveAttribute("title", /.+/);
    }
    await expect(links).toHaveAttribute("title", /phụ thuộc/);
    await expect(critical).toHaveAttribute("title", /phụ thuộc/);
    // Dependencies look as the design draws them (on); nothing is faked.
    await expect(links).toHaveAttribute("aria-pressed", "true");
    await expect(critical).toHaveAttribute("aria-pressed", "false");
    await critical.click({ force: true });
    await expect(critical).toHaveAttribute("aria-pressed", "false");
    await links.click({ force: true });
    await expect(links).toHaveAttribute("aria-pressed", "true");
    await add.click({ force: true });
    await expect(page.getByRole("dialog")).toHaveCount(0);
    // No arrow and no critical-path ring is drawn.
    await expect(page.locator('[data-tl="dl"]')).toHaveCount(0);
    await expect(page.locator('[data-tl="ah"]')).toHaveCount(0);
  });

  test("the undated link goes to the Calendar", async ({ page }) => {
    await openTimeline(page);
    await page.getByRole("link", { name: "1 việc chưa có ngày" }).click();
    await expect(page).toHaveURL(/view=calendar/);
    await expect(page.locator("[data-calendar-view]")).toBeVisible();
  });

  test("Alt and the arrow keys move a bar and its due day", async ({
    page,
  }) => {
    await openTimeline(page);
    const track = await trackOf(page, "ERP-146");
    const barBox = async () =>
      (await track.locator('[data-tl="gb"]').boundingBox())!;
    const before = await barBox();
    await expect(dueOf(page, "ERP-146")).toHaveText("16/10");
    const title = listRow(page, "ERP-146").getByRole("link").nth(1);
    await title.focus();
    // Alt + Right moves the whole span a day.
    await page.keyboard.press("Alt+ArrowRight");
    await expect(dueOf(page, "ERP-146")).toHaveText("17/10");
    await expect
      .poll(async () => (await barBox()).x)
      .toBeCloseTo(before.x + 26, 0);
    expect((await barBox()).width).toBeCloseTo(before.width, 0);
    // Alt + Shift + Left changes only the due day.
    await page.keyboard.press("Alt+Shift+ArrowLeft");
    await expect(dueOf(page, "ERP-146")).toHaveText("16/10");
    await expect
      .poll(async () => (await barBox()).width)
      .toBeCloseTo(before.width - 26, 0);
  });

  test("dragging the handle changes the due day, with a tip and Escape", async ({
    page,
  }) => {
    await openTimeline(page);
    const track = await trackOf(page, "ERP-146");
    const barLocator = track.locator('[data-tl="gb"]');
    await barLocator.hover();
    const handle = track.locator('[data-tl="handle"]');
    await expect(handle).toBeVisible();
    const handleBox = (await handle.boundingBox())!;
    const startX = handleBox.x + handleBox.width / 2;
    const startY = handleBox.y + handleBox.height / 2;

    await page.mouse.move(startX, startY);
    await page.mouse.down();
    await page.mouse.move(startX + 26, startY, { steps: 4 });
    await page.mouse.move(startX + 52, startY, { steps: 4 });
    const tip = track.locator('[data-tl="tip"]');
    await expect(tip).toContainText("Hạn: 16/10 → 18/10");
    await expect(track.locator('[data-tl="ghost"]')).toBeVisible();
    await expect(dueOf(page, "ERP-146")).toHaveText("18/10");

    // Escape puts the bar back.
    await page.keyboard.press("Escape");
    await page.mouse.up();
    await expect(tip).toHaveCount(0);
    await expect(dueOf(page, "ERP-146")).toHaveText("16/10");

    await barLocator.hover();
    await page.mouse.move(startX, startY);
    await page.mouse.down();
    await page.mouse.move(startX + 26, startY, { steps: 4 });
    await page.mouse.move(startX + 52, startY, { steps: 4 });
    await page.mouse.up();
    await expect(dueOf(page, "ERP-146")).toHaveText("18/10");
    await expect(track.locator('[data-tl="tip"]')).toHaveCount(0);
  });

  test("dragging the bar moves its start and due days together", async ({
    page,
  }) => {
    await openTimeline(page);
    const track = await trackOf(page, "ERP-146");
    const barLocator = track.locator('[data-tl="gb"]');
    const before = (await barLocator.boundingBox())!;
    const startX = before.x + 20;
    const startY = before.y + before.height / 2;
    await page.mouse.move(startX, startY);
    await page.mouse.down();
    await page.mouse.move(startX + 26, startY, { steps: 4 });
    await page.mouse.move(startX + 52, startY, { steps: 4 });
    await expect(track.locator('[data-tl="tip"]')).toContainText(
      "13–16/10 → 15–18/10",
    );
    await page.mouse.up();
    await expect(dueOf(page, "ERP-146")).toHaveText("18/10");
    await expect
      .poll(async () => (await barLocator.boundingBox())!.x)
      .toBeCloseTo(before.x + 52, 0);
    expect((await barLocator.boundingBox())!.width).toBeCloseTo(
      before.width,
      0,
    );
    // A drag never opens the item.
    await expect(page.getByRole("dialog")).toHaveCount(0);
  });
});

test.describe("Board Timeline as a viewer", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default" || faults !== "",
    "Viewer checks need VITE_MOCK_PERSONA=viewer",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("no add, no handle and no keyboard move; the item still opens", async ({
    page,
  }) => {
    await openTimeline(page);
    await expect(
      page.getByRole("button", { name: /^Thêm công việc/ }),
    ).toHaveCount(0);
    const track = await trackOf(page, "ERP-146");
    await track.locator('[data-tl="gb"]').hover();
    await expect(track.locator('[data-tl="handle"]')).toHaveCount(0);
    const title = listRow(page, "ERP-146").getByRole("link").nth(1);
    await title.focus();
    await page.keyboard.press("Alt+ArrowRight");
    await expect(dueOf(page, "ERP-146")).toHaveText("16/10");
    await title.click();
    await expect(page.getByRole("dialog")).toBeVisible();
  });
});
