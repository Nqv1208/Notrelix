import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The rest of the `BC-Bulk` frames of the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), measured
 * against the artboard rendered in Chromium (boxes relative to the table, the note, the bar or the sheet; font, radius):
 * - `bk-grp` and `bk-cap` need `VITE_MOCK_OVERLAYS=bulk-states` (the board `mock-board-it-bulk` holds 301 items in one
 *   group); `bk-gone` runs in the default owner world (another person deletes two picked items);
 * - `bk320-R`, `bk320-cap`, `bk320-sheet` and `bk320-ok` at 320 px, light and dark; the artboard frame has a 1 px
 *   border, so its 318 px content is 320 px of window here (a width drawn as 294 reads 296).
 * Tolerance is 1 px. The phone cards of the artboard are Kanban-like cards; the app keeps the phone list rows of
 * `Final-M-Board` and swaps their check circle for the 44 px pick box while a row is picked, so the rows are not
 * compared, only the pick box, the strip, the note, the sheet and the pill.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const WS = "/workspaces/mock-workspace-primary/boards";
const ERP = `${WS}/mock-board-erp`;
const BULK = `${WS}/mock-board-it-bulk`;
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

async function rel(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("no box");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

function expectBox(actual: Box, want: Partial<Box>, what: string) {
  if (process.env.BOXLOG) {
    console.log(
      "BOX",
      what,
      JSON.stringify(actual, (_k, v) =>
        typeof v === "number" ? +v.toFixed(1) : v,
      ),
      "want",
      JSON.stringify(want),
    );
    return;
  }
  for (const k of ["x", "y", "w", "h"] as const) {
    const w = want[k];
    if (w !== undefined)
      expect(
        Math.abs(actual[k] - w),
        `${what}.${k}: ${actual[k].toFixed(1)} vs ${w}`,
      ).toBeLessThanOrEqual(TOL);
  }
}

async function open(page: Page, url: string, dark: boolean, fixedClock = true) {
  // A fixed clock never goes stale, so the board is never read again on its own.
  if (fixedClock) await page.clock.setFixedTime(NOW);
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
  await page.goto(url);
  await expect(
    page.locator('[role="rowgroup"], [data-slot="table-phone-list"]').first(),
  ).toBeVisible();
}

function near(actual: number, want: number, what: string) {
  if (process.env.BOXLOG) {
    console.log("NEAR", what, actual.toFixed(1), "want", want);
    return;
  }
  expect(
    Math.abs(actual - want),
    `${what}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);
}

/** A note over the bar is as wide as the table and sits flush with it. */
async function expectUnderTable(page: Page, note: Locator, height: number) {
  const table = (await page.locator("[data-table-scroll]").boundingBox())!;
  const n = (await note.boundingBox())!;
  near(n.x - table.x, 0, "note.x");
  near(n.width, table.width, "note.w");
  near(n.height, height, "note.h");
}

const bar = (page: Page) => page.locator("[data-table-bulk-bar]");

/** A bar's boxes relative to itself: its size, the count and the buttons. */
async function expectBar(
  page: Page,
  want: { w: number; count: Partial<Box>; buttons: [string, Partial<Box>][] },
) {
  const b = bar(page);
  expectBox({ ...(await rel(b, b)), x: 0, y: 0 }, { w: want.w, h: 44 }, "bar");
  expectBox(await rel(b.locator("b").first(), b), want.count, "count");
  for (const [name, box] of want.buttons)
    expectBox(
      await rel(b.getByRole("button", { name, exact: true }), b),
      box,
      name,
    );
}

test.describe("Group pick and the limit at 1440 px", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      overlays !== "bulk-states",
    "Needs VITE_MOCK_OVERLAYS=bulk-states in the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    const mode = dark ? "dem" : "sang";

    test(`bk-grp-${mode}: 200 of the group's 301 and the note`, async ({
      page,
    }) => {
      await open(page, `${BULK}?view=table`, dark);
      await page.getByRole("button", { name: "Chọn cả nhóm" }).click();
      const note = page.locator('[data-table-bulk-notice="info"]');
      await expect(note).toBeVisible();
      await expect(note).toContainText("Đã chọn 200 trong 301 việc của nhóm");
      await expect(note).toContainText(
        "Nhiều nhất 200 việc mỗi lần. 101 việc còn lại chưa được chọn.",
      );
      await page.mouse.move(2, 2);
      await page.waitForTimeout(300);
      const header = page
        .getByRole("row")
        .filter({ hasText: "Đang làm" })
        .first();
      // Note: x 13 and height 64 against the group header; its parts against the note.
      const tb = (await page.locator("[data-table-scroll]").boundingBox())!;
      const nb0 = (await note.boundingBox())!;
      near(nb0.x - tb.x, 13, "note.x");
      near(nb0.width, tb.width - 26, "note.w");
      near(nb0.height, 64, "note.h");
      expectBox(
        await rel(note.locator("b"), note),
        { x: 40, y: 10, h: 18.2 },
        "title",
      );
      const clearBox = await rel(
        note.getByRole("button", { name: "Bỏ chọn nhóm" }),
        note,
      );
      expectBox(clearBox, { y: 10, w: 120.5, h: 44 }, "Bỏ chọn nhóm");
      near(nb0.width - clearBox.x - clearBox.w, 14, "Bỏ chọn nhóm right gap");
      const hb = await header.boundingBox();
      const nb = await note.boundingBox();
      expect(hb && nb && nb.y - (hb.y + hb.height)).toBeTruthy();
      await expectBar(page, {
        w: 811.2,
        count: { x: 14, y: 11.8, w: 88.4, h: 20.3 },
        buttons: [
          ["Trạng thái", { x: 121.4, y: 6, w: 104.9, h: 32 }],
          ["Ưu tiên", { x: 228.2, y: 6, w: 83.8, h: 32 }],
          ["Người phụ trách", { x: 314, y: 6, w: 141.2, h: 32 }],
          ["Hạn", { x: 457.3, y: 6, w: 64.4, h: 32 }],
          ["Thêm", { x: 523.7, y: 6, w: 77.8, h: 32 }],
          ["Lưu trữ", { x: 614.5, y: 6, w: 86.8, h: 32 }],
          ["Xoá", { x: 703.3, y: 6, w: 67.9, h: 32 }],
          ["Bỏ chọn (Esc)", { x: 773.2, y: 6, w: 32, h: 32 }],
        ],
      });
      // "Bỏ chọn nhóm" drops the group's pick and the note.
      await note.getByRole("button", { name: "Bỏ chọn nhóm" }).click();
      await expect(note).toHaveCount(0);
      await expect(bar(page)).toHaveCount(0);
    });

    test(`bk-cap-${mode}: 201 picked locks the bar and says so`, async ({
      page,
    }) => {
      await open(page, `${BULK}?view=table`, dark);
      await page.getByRole("button", { name: "Chọn cả nhóm" }).click();
      await expect(page.getByText("200 đã chọn")).toBeVisible();
      // One more row past the 200 the group pick took.
      const next = page.getByRole("checkbox", { name: "Chọn NL-201" });
      for (let i = 0; i < 6 && !(await next.isVisible()); i++)
        await page
          .getByRole("button", { name: /^Hiện thêm/ })
          .first()
          .click();
      await next.click();
      await page.mouse.move(2, 2);
      const note = page.locator('[data-table-bulk-notice="warning"]');
      await expect(note).toContainText(
        "Đã chọn 201 việc, nhiều nhất 200 việc mỗi lần",
      );
      await expect(note).toContainText(
        "Bỏ chọn bớt 1 việc để tiếp tục. Lựa chọn không bị cắt bớt.",
      );
      await page.waitForTimeout(300);
      await expectUnderTable(page, note, 57.4);
      expectBox(
        await rel(note.locator("b"), note),
        { x: 40, y: 10, h: 18.2 },
        "title",
      );
      const b = bar(page);
      const gap =
        (await b.boundingBox())!.y -
        ((await note.boundingBox())!.y + (await note.boundingBox())!.height);
      expect(Math.abs(gap - 24), `gap ${gap}`).toBeLessThanOrEqual(TOL);
      await expectBar(page, {
        w: 807.3,
        count: { x: 14, y: 11.8, w: 84.5, h: 20.3 },
        buttons: [
          ["Trạng thái", { x: 117.5, y: 6, w: 104.9, h: 32 }],
          ["Xoá", { x: 699.4, y: 6, w: 67.9, h: 32 }],
          ["Bỏ chọn (Esc)", { x: 769.4, y: 6, w: 32, h: 32 }],
        ],
      });
      // The actions are locked: no menu opens from them.
      await expect(
        b.getByRole("button", { name: "Trạng thái", exact: true }),
      ).toBeDisabled();
    });
  }
});

test.describe("An item another person deleted (bk-gone)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    const mode = dark ? "dem" : "sang";
    test(`bk-gone-${mode}: the note names how many left the pick`, async ({
      page,
    }) => {
      await open(page, `${ERP}?view=table`, dark);
      for (const key of ["ERP-142", "ERP-144", "ERP-146"])
        await page.getByRole("checkbox", { name: `Chọn ${key}` }).click();
      await page.evaluate(() => {
        // Another person deletes two of the picked items; the next read of the board finds them gone.
        (globalThis as { __notrelixMockGone?: string[] }).__notrelixMockGone = [
          "mock-erp-item-142",
          "mock-erp-item-144",
        ];
      });
      // A change of another row reads the board again: the two deleted items are not in it.
      await page
        .getByRole("button", { name: "Đổi trạng thái của ERP-147" })
        .click();
      await page.getByRole("option", { name: /Đang làm/ }).click();
      const note = page.locator('[data-table-bulk-notice="info"]');
      await expect(note).toContainText("2 việc đã bị người khác xoá", {
        timeout: 15_000,
      });
      await expect(note).toContainText("Còn 1 việc đang chọn");
      await page.mouse.move(2, 2);
      await page.waitForTimeout(300);
      await expectUnderTable(page, note, 64);
      const closeBox = await rel(
        note.getByRole("button", { name: "Đóng" }),
        note,
      );
      expectBox(closeBox, { y: 10, w: 64.7, h: 44 }, "Đóng");
      near(
        (await note.boundingBox())!.width - closeBox.x - closeBox.w,
        14,
        "Đóng right gap",
      );
      const b = bar(page);
      const gap =
        (await b.boundingBox())!.y -
        ((await note.boundingBox())!.y + (await note.boundingBox())!.height);
      expect(Math.abs(gap - 24), `gap ${gap}`).toBeLessThanOrEqual(TOL);
      await note.getByRole("button", { name: "Đóng" }).click();
      await expect(note).toHaveCount(0);
    });
  }
});

test.describe("The compact bar at 320 px (bk320)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 320, height: 520 } });

  async function pickTwo(page: Page) {
    for (const key of ["ERP-142", "ERP-144"]) {
      const tick = page.getByRole("checkbox", { name: `Chọn ${key}` });
      if (await tick.isVisible()) await tick.click();
      else {
        await page
          .getByRole("button", { name: `Thao tác khác cho ${key}` })
          .click();
        await page.getByRole("button", { name: "Chọn việc này" }).click();
      }
    }
    await page.mouse.move(2, 2);
  }

  for (const dark of [false, true]) {
    const mode = dark ? "dem" : "sang";

    test(`bk320-R-${mode}: pick boxes and the strip`, async ({ page }) => {
      await open(page, `${ERP}?view=table`, dark);
      await pickTwo(page);
      const b = bar(page);
      await expect(b).toBeVisible();
      await page.waitForTimeout(300);
      const vp = page.viewportSize()!;
      const bb = (await b.boundingBox())!;
      expectBox(
        { x: bb.x, y: bb.y, w: bb.width, h: bb.height },
        { x: 0, y: vp.height - 60, w: 320, h: 60 },
        "strip",
      );
      expectBox(
        await rel(b.locator("b"), b),
        { x: 16, y: 19.8, h: 20.3 },
        "count",
      );
      expectBox(
        await rel(b.getByRole("button", { name: "Hành động" }), b),
        { x: 158.1, y: 8, w: 101.9, h: 44 },
        "Hành động",
      );
      expectBox(
        await rel(b.getByRole("button", { name: "Bỏ chọn (Esc)" }), b),
        { x: 268, y: 8, w: 44, h: 44 },
        "clear",
      );
      const tick = page.getByRole("checkbox", { name: "Chọn ERP-142" });
      expectBox(await rel(tick, tick), { w: 44, h: 44 }, "pick box");
      await expect(tick).toHaveAttribute("aria-checked", "true");
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      );
      expect(overflow).toBeLessThanOrEqual(0);
    });

    test(`bk320-sheet-${mode}: every action is a 44 px row`, async ({
      page,
    }) => {
      await open(page, `${ERP}?view=table`, dark);
      await pickTwo(page);
      await bar(page).getByRole("button", { name: "Hành động" }).click();
      const sheet = page.getByRole("dialog");
      await expect(sheet).toBeVisible();
      await page.waitForTimeout(500);
      await expect(
        sheet.getByText("Hành động cho 2 việc", { exact: true }),
      ).toBeVisible();
      const rows = [
        "Đổi trạng thái",
        "Đổi ưu tiên",
        "Giao người phụ trách",
        "Đặt hạn",
        "Xoá hạn",
        "Bỏ hết người phụ trách",
        "Chuyển board…",
        "Lưu trữ",
        "Xoá",
      ];
      let index = 0;
      for (const name of rows) {
        expectBox(
          await rel(sheet.getByRole("button", { name, exact: true }), sheet),
          { x: 8, y: 62 + index * 44, w: 304, h: 44 },
          name,
        );
        index += 1;
      }
      expectBox(
        await rel(sheet.getByRole("button", { name: "Đóng" }), sheet),
        { x: 260, y: 18, w: 44, h: 44 },
        "close",
      );
      const sb = (await sheet.boundingBox())!;
      expectBox(
        { x: sb.x, y: sb.y, w: sb.width, h: sb.height },
        { x: 0, w: 320 },
        "panel",
      );
      expect(
        await sheet.evaluate((e) => getComputedStyle(e).borderTopLeftRadius),
      ).toBe("16px");
      // A status is chosen in the same sheet and one request goes out.
      await sheet
        .getByRole("button", { name: "Đổi trạng thái", exact: true })
        .click();
      await sheet.getByRole("button", { name: /Hoàn thành/ }).click();
      await expect(page.locator("[data-table-bulk-done]")).toContainText(
        "Đã đổi trạng thái cho 2 việc",
      );
    });

    test(`bk320-ok-${mode}: the pill with Hoàn tác`, async ({ page }) => {
      await open(page, `${ERP}?view=table`, dark);
      await pickTwo(page);
      await bar(page).getByRole("button", { name: "Hành động" }).click();
      const sheet = page.getByRole("dialog");
      await sheet
        .getByRole("button", { name: "Đổi trạng thái", exact: true })
        .click();
      await sheet.getByRole("button", { name: /Hoàn thành/ }).click();
      const toast = page.locator("[data-table-bulk-done]");
      await expect(toast).toContainText("Đã đổi trạng thái cho 2 việc");
      await page.waitForTimeout(500);
      const tb = (await toast.boundingBox())!;
      expectBox(
        { x: tb.x, y: tb.y, w: tb.width, h: tb.height },
        { x: 12, w: 296, h: 64 },
        "toast",
      );
      expect(
        Math.abs(520 - (tb.y + tb.height) - 16),
        "toast bottom gap",
      ).toBeLessThanOrEqual(TOL);
      expectBox(
        await rel(toast.getByRole("button", { name: "Hoàn tác" }), toast),
        { y: 10, w: 66.7, h: 44 },
        "Hoàn tác",
      );
    });
  }

  for (const dark of [false, true]) {
    test(`bk320-cap-${dark ? "dem" : "sang"}: past 200 the strip locks and the note sits over the list`, async ({
      page,
    }) => {
      test.skip(
        overlays !== "bulk-states",
        "Needs VITE_MOCK_OVERLAYS=bulk-states",
      );
      await open(page, `${BULK}?view=table`, dark);
      await page.getByRole("button", { name: "Chọn cả nhóm" }).click();
      await expect(bar(page)).toContainText("200 đã chọn");
      await page.getByRole("button", { name: /^Xem thêm/ }).click();
      await page.getByRole("checkbox", { name: "Chọn NL-201" }).click();
      const note = page.locator('[data-table-bulk-notice="warning"]');
      await expect(note).toContainText("Đã chọn 201 việc, nhiều nhất 200");
      await expect(note).toContainText("Bỏ chọn bớt 1 việc.");
      await page
        .locator("[data-table-scroll]")
        .evaluate((e) => e.scrollTo(0, 0));
      await page.waitForTimeout(300);
      const nb = (await note.boundingBox())!;
      expectBox(
        { x: nb.x, y: nb.y, w: nb.width, h: nb.height },
        { x: 12, w: 296, h: 57.4 },
        "note (the y depends on the header above it)",
      );
      const go = bar(page).getByRole("button", { name: "Hành động" });
      await expect(go).toBeDisabled();
      expectBox(await rel(go, bar(page)), { w: 101.9, h: 44 }, "Hành động");
    });
  }
});

test.describe("The Kanban undo pill (bk-ok on the Kanban)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`a status change of two cards leaves the pill with Hoàn tác (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
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
      await page.goto(ERP);
      const lane = page.getByRole("region", { name: "Cần làm" });
      await expect(lane).toBeVisible();
      const cards = lane.getByRole("article");
      await cards.nth(0).getByRole("checkbox").click();
      await cards.nth(1).getByRole("checkbox").click();
      await page.mouse.move(2, 2);
      await bar(page)
        .getByRole("button", { name: "Trạng thái", exact: true })
        .click();
      await page.getByRole("menuitem", { name: /Hoàn thành/ }).click();
      const pill = page.locator("[data-table-bulk-done]");
      await expect(pill).toContainText("Đã đổi trạng thái cho 2 việc");
      await page.waitForTimeout(500);
      expectBox({ ...(await rel(pill, pill)), x: 0, y: 0 }, { h: 48 }, "pill");
      expectBox(
        await rel(pill.getByRole("button", { name: "Hoàn tác" }), pill),
        { y: 10, h: 28 },
        "Hoàn tác",
      );
      await pill.getByRole("button", { name: "Hoàn tác" }).click();
      await expect(pill).toHaveCount(0);
      // Both cards are back in the lane they came from.
      await expect(lane.getByRole("article")).toHaveCount(await cards.count());
    });
  }
});
