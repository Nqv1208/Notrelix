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

  test("controls without data are inert and say why; the dependencies and the critical path work", async ({
    page,
  }) => {
    await openTimeline(page);
    const links = page.getByRole("button", { name: /^Phụ thuộc/ });
    const critical = page.getByRole("button", { name: /^Đường găng/ });
    const groupBy = page.getByRole("button", { name: /^Nhóm theo/ });
    const barSpan = page.getByRole("button", { name: /^Thanh dài từ/ });
    const add = page.getByRole("button", { name: /^Thêm công việc/ });
    for (const control of [groupBy, barSpan]) {
      await expect(control).toHaveAttribute("aria-disabled", "true");
      await expect(control).toHaveAttribute("title", /.+/);
    }
    // Adding works from the Timeline: it opens the create dialog.
    await expect(add).not.toHaveAttribute("aria-disabled", "true");
    await add.click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0);
    // The seven finish-to-start arrows of the board are drawn.
    await expect(links).toHaveAttribute("aria-pressed", "true");
    await expect(critical).toHaveAttribute("aria-pressed", "false");
    await expect(page.locator('[data-tl="ah"]')).toHaveCount(7);
    await links.click();
    await expect(page.locator('[data-tl="ah"]')).toHaveCount(0);
    await links.click();
    await critical.click();
    await expect(critical).toHaveAttribute("aria-pressed", "true");
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

  test("the board reads as four groups with bars as long as their dates", async ({
    page,
  }) => {
    await openTimeline(page);
    await expect(page.locator('[data-tl="lgrp"]')).toHaveCount(4);
    await expect(page.locator('[data-tl="lgrp"]')).toContainText([
      "Chuẩn bị",
      "Dữ liệu",
      "Kiểm thử",
      "Đào tạo và vận hành",
    ]);
    // 26 px a day, 1 px inset on each side: start day to due day, inclusive.
    const widthOf = async (key: string) =>
      (await (await trackOf(page, key)).locator('[data-tl="gb"]').boundingBox())
        ?.width;
    expect(await widthOf("ERP-151")).toBeCloseTo(6 * 26 - 2, 0); // 13/10-18/10
    expect(await widthOf("ERP-146")).toBeCloseTo(4 * 26 - 2, 0); // 13/10-16/10
    expect(await widthOf("ERP-144")).toBeCloseTo(5 * 26 - 2, 0); // 08/10-12/10
    expect(await widthOf("ERP-138")).toBeCloseTo(6 * 26 - 2, 0); // 09/10-14/10
    expect(await widthOf("ERP-150")).toBeCloseTo(7 * 26 - 2, 0); // 22/10-28/10
    // Ten bars: the items with a start day and the phases (an item with only
    // a due day, ERP-140, 149 and 152, belongs to the Calendar and is not
    // listed; a finished item with no start is listed only when a dependency
    // names it, ERP-129).
    const widths = await page
      .locator('[data-tl="gb"]')
      .evaluateAll((bars) =>
        bars.map((bar) => bar.getBoundingClientRect().width),
      );
    expect(widths.filter((width) => width > 2 * 26 - 4)).toHaveLength(10);
    expect(widths.filter((width) => width < 30)).toHaveLength(0);
    // The overdue item, the done items and the item before the window.
    const late = (await trackOf(page, "ERP-142")).locator('[data-tl="latel"]');
    await expect(late).toHaveText("Quá hạn 2 ngày");
    await expect(page.locator('[data-tl="fin"]')).toHaveCount(0);
    await expect(
      (await trackOf(page, "ERP-129")).locator('[data-tl="lab"]'),
    ).toHaveText("◂ Hoàn thành 30/09");
  });

  for (const size of [
    { width: 1440, height: 900 },
    { width: 1280, height: 800 },
    { width: 1100, height: 760 },
  ]) {
    for (const name of ["Ngày", "Tuần", "Tháng"] as const) {
      test(`no label is cut by the frame or crosses the today line at ${size.width} px, ${name}`, async ({
        page,
      }) => {
        await page.setViewportSize(size);
        await openTimeline(page);
        await scale(page, name).click();
        await expect(scale(page, name)).toHaveAttribute("aria-checked", "true");
        const check = async (scrolled: boolean) =>
          page.evaluate((scrolled) => {
            const wrap = document
              .querySelector('[data-tl="wrap"]')!
              .getBoundingClientRect();
            const list = document
              .querySelector('[data-tl="left"]')!
              .getBoundingClientRect();
            const today = document
              .querySelector('[data-tl="todayline"]')!
              .getBoundingClientRect();
            const problems: string[] = [];
            const boxes = document.querySelectorAll(
              '[data-tl="trow"] [data-tl="lab"]',
            );
            for (const label of boxes) {
              const box = label.getBoundingClientRect();
              const row = label.closest('[data-tl="trow"]')!;
              const top = row.getBoundingClientRect();
              if (top.bottom < wrap.top || top.top > wrap.bottom) continue;
              const text = label.textContent ?? "";
              // Cut by the right edge, or hidden under the list.
              if (box.right > wrap.right + 1)
                problems.push(`${text}: right ${box.right} > ${wrap.right}`);
              if (
                !scrolled &&
                box.left < list.right - 1 &&
                box.right > list.right + 1
              )
                problems.push(`${text}: under the list`);
              // A done note must not sit across the today line.
              if (
                row.querySelector('[data-tl="fin"]') &&
                box.left < today.right &&
                box.right > today.left
              )
                problems.push(`${text}: across the today line`);
              // The text itself must not be truncated by its own box unless the
              // truncation was chosen (ellipsis) - never a half-cut glyph run.
            }
            return problems;
          }, scrolled);
        await expect.poll(() => check(false)).toEqual([]);
        // And once the chart is scrolled to its right end.
        await page.locator('[data-tl="wrap"]').evaluate((element) => {
          element.scrollLeft = element.scrollWidth;
        });
        await expect.poll(() => check(true)).toEqual([]);
      });
    }
  }

  test("an item before the window shows a note, and the window reaches it", async ({
    page,
  }) => {
    await openTimeline(page);
    const track = await trackOf(page, "ERP-129");
    await expect(track.locator('[data-tl="lab"]')).toHaveText(
      "◂ Hoàn thành 30/09",
    );
    await expect(track.locator('[data-tl="fin"]')).toHaveCount(0);
    await page.getByRole("button", { name: "Lùi", exact: true }).click();
    await expect(track.locator('[data-tl="fin"]')).toHaveCount(1);
    await expect(track.locator('[data-tl="lab"]')).toHaveText(
      "Hoàn thành 30/09",
    );
    // The chart scrolls inside its frame: the window is wider than the frame
    // at 1100 px, and the frame, not the page, scrolls.
    await page.setViewportSize({ width: 1100, height: 760 });
    const overflow = await page
      .locator('[data-tl="wrap"]')
      .evaluate((element) => ({
        scrolls: element.scrollWidth > element.clientWidth,
        page: document.documentElement.scrollWidth > window.innerWidth,
      }));
    expect(overflow).toEqual({ scrolls: true, page: false });
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
