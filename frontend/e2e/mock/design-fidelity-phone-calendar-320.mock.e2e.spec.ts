import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The Calendar at 320x640 against `WM-TLCal-320` (render: `render.py WM-TLCal-320.dc.html out.html mode=<sang|dem>`),
 * light and dark:
 * - frame 3 "Lịch 320: không kéo, đổi ngày bằng hộp, lỗi có Thử lại" (owner world): the day heading, the agenda row
 *   with its two lines (status mark, key, title; span and the 28 px "Đổi ngày…" button), then a date save that fails
 *   once (`__mockItemDateFailures`) shows "Chưa lưu được ngày" with Thử lại, and Thử lại saves.
 * - frame 4 "Khay 320: tấm dưới, chỉ xem" (viewer world, `VITE_MOCK_PERSONA=viewer`): the undated sheet from the
 *   bottom with its grab, title, count, the view-only note and the locked chips.
 *
 * Numbers were read from the render, relative to the frame's inner edge (the artboard's phone has a 1 px border the
 * app does not, so x is one less and right-anchored boxes sit 2 px further right in the 320 px row); tolerance 1 px,
 * fills compared in sRGB after a canvas round trip, 8 levels.
 *
 * Not compared (intended): the board shell above the calendar (`Triển khai ERP` with Bảng/Lịch/Dòng thời gian is the
 * Final-M-Board shell, not built); the period head (‹ Tháng 10, 2026 › and the range chip) that the app keeps above
 * the agenda so the period can change; the text of the rows (the mock board's items are not the artboard's sample);
 * the note's height (the artboard's note carries a designer remark in brackets the app does not print, so the chips
 * are compared to the note's bottom edge); the scrim (the shared sheet overlay blurs the page); the toast's box and
 * fill (the shared Sonner toaster draws every toast: 16 px from the edges at phone width, 288 px wide where the
 * artboard draws 296, the inverse surface where it draws the danger fill, 12 px action type), so only its words and
 * its Thử lại are checked.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  scenario !== "default" || faults !== "",
  "Needs the default world without faults",
);
test.use({ viewport: { width: 320, height: 640 } });

type Mode = "sang" | "dem";
type Box = { x: number; y: number; w: number; h: number };
type Rgb = readonly [number, number, number];

const INK: Record<
  Mode,
  {
    panel: Rgb;
    line: Rgb;
    meta: Rgb;
    line2: Rgb;
    hover: Rgb;
    hover2: Rgb;
    warnSoft: Rgb;
    warnText: Rgb;
  }
> = {
  sang: {
    panel: [255, 255, 255],
    line: [230, 233, 238],
    meta: [95, 104, 117],
    line2: [210, 215, 222],
    hover: [241, 243, 246],
    hover2: [230, 233, 238],
    warnSoft: [253, 243, 220],
    warnText: [138, 90, 0],
  },
  dem: {
    panel: [21, 26, 33],
    line: [35, 43, 53],
    meta: [143, 153, 167],
    line2: [46, 55, 67],
    hover: [28, 35, 44],
    hover2: [35, 42, 52],
    // The canvas reads the translucent fill rgba(242,193,78,.12) as its colour, as it reads the render.
    warnSoft: [239, 189, 74],
    warnText: [242, 193, 78],
  },
};

async function box(el: Locator, origin?: Locator): Promise<Box> {
  const a = await el.boundingBox();
  if (!a) throw new Error("not drawn");
  const o = origin ? await origin.boundingBox() : { x: 0, y: 0 };
  if (!o) throw new Error("origin not drawn");
  return { x: a.x - o.x, y: a.y - o.y, w: a.width, h: a.height };
}

function near(actual: Box, want: Partial<Box>, what: string) {
  for (const [k, v] of Object.entries(want) as [keyof Box, number][]) {
    expect
      .soft(
        Math.abs(actual[k] - v),
        `${what}.${k}: ${actual[k].toFixed(1)} vs ${v}`,
      )
      .toBeLessThanOrEqual(1);
  }
}

async function css(el: Locator, property: string): Promise<string> {
  return el.evaluate(
    (node, p) => getComputedStyle(node).getPropertyValue(p),
    property,
  );
}

async function rgb(el: Locator, property: string): Promise<number[]> {
  return el.evaluate((node, p) => {
    const ctx = document.createElement("canvas").getContext("2d")!;
    ctx.fillStyle = getComputedStyle(node).getPropertyValue(p);
    ctx.fillRect(0, 0, 1, 1);
    return [...ctx.getImageData(0, 0, 1, 1).data.slice(0, 3)];
  }, property);
}

async function colour(el: Locator, property: string, want: Rgb, what: string) {
  const got = await rgb(el, property);
  const gap = Math.max(...want.map((v, i) => Math.abs(v - got[i]!)));
  expect
    .soft(
      gap,
      `${what} ${property} rgb(${got.join(", ")}) vs rgb(${want.join(", ")})`,
    )
    .toBeLessThanOrEqual(8);
}

async function type(el: Locator, size: string, weight: string, what: string) {
  expect.soft(await css(el, "font-size"), `${what} font size`).toBe(size);
  expect.soft(await css(el, "font-weight"), `${what} font weight`).toBe(weight);
}

async function open(page: Page, mode: Mode, failures = 0) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ([dark, fail]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem(
          "notrelix-theme-mode",
          dark ? "dark" : "light",
        );
      } catch {
        // Private mode: the defaults apply.
      }
      (
        globalThis as { __mockItemDateFailures?: number }
      ).__mockItemDateFailures = fail as number;
    },
    [mode === "dem", failures] as const,
  );
  await page.goto(`${BOARD}?view=calendar`);
  const root = page.locator("[data-phone-calendar]");
  await expect(root).toBeVisible();
  await expect(
    page.getByRole("status", { name: "Đang khôi phục cách xem" }),
  ).toHaveCount(0);
  await page.waitForTimeout(500);
  return root;
}

for (const mode of ["sang", "dem"] as const) {
  test(`agenda at 320 px, ${mode}, equals WM-TLCal-320 Lịch 320`, async ({
    page,
  }) => {
    test.skip(persona !== "owner", "Needs an editor (the owner world)");
    const root = await open(page, mode);
    const ink = INK[mode];
    // The 320 px agenda replaces the month grid and the week strip.
    await expect(root.locator("[data-calendar-week]")).toHaveCount(0);
    const day = root.locator("[data-phone-agenda-day]").first();
    const heading = day.locator("h3");
    const row = day.locator("[data-phone-row]").first();
    await expect(row).toBeVisible();

    near(await box(heading, day), { x: 0, y: 0, w: 320, h: 33.4 }, "heading");
    await type(heading, "12px", "600", "heading");
    await colour(heading, "color", ink.meta, "heading");

    const rowBox = await box(row, day);
    near(rowBox, { x: 0, y: 33.4, w: 320, h: 67.8 }, "row");
    await colour(row, "background-color", ink.panel, "row");
    await colour(row, "border-bottom-color", ink.line, "row line");

    const glyph = row.locator("[data-status-glyph]");
    near(await box(glyph, row), { x: 12, y: 9, w: 13, h: 13 }, "mark");
    const key = row.locator("button span").first();
    near(await box(key, row), { x: 33, y: 7.1, h: 16.7 }, "key");
    await type(key, "11.5px", "400", "key");
    const title = row.locator("button span").nth(1);
    const titleBox = await box(title, row);
    near(titleBox, { y: 6, h: 18.8 }, "title");
    near(
      { ...titleBox, x: titleBox.x + titleBox.w },
      { x: 308 },
      "title right edge",
    );
    await type(title, "13px", "500", "title");
    const span = row.locator("> span");
    near(await box(span, row), { x: 12, y: 38.2, h: 17.4 }, "span");
    await type(span, "12px", "400", "span");
    await colour(span, "color", ink.meta, "span");

    const change = row.getByRole("button", { name: /^Đổi ngày của/ });
    await expect(change).toHaveText("Đổi ngày…");
    const changeBox = await box(change, row);
    near(changeBox, { y: 32.9, h: 28 }, "Đổi ngày…");
    near(
      { ...changeBox, x: changeBox.x + changeBox.w },
      { x: 308 },
      "Đổi ngày… right edge",
    );
    expect
      .soft(Math.abs(changeBox.w - 86.5), `Đổi ngày… w ${changeBox.w}`)
      .toBeLessThanOrEqual(2);
    await type(change, "12.5px", "500", "Đổi ngày…");
    expect
      .soft(await css(change, "border-radius"), "button radius")
      .toBe("7px");
    await colour(change, "border-top-color", ink.line2, "button");
    await colour(change, "background-color", ink.panel, "button");
    // 28 px drawn, 44 px to the finger.
    const hit = await change.evaluate((el) => {
      const before = getComputedStyle(el, "::before");
      return el.getBoundingClientRect().height - 2 * parseFloat(before.top);
    });
    expect.soft(hit, "Đổi ngày… hit height").toBeGreaterThanOrEqual(44);

    // No sideways scroll at 320.
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - window.innerWidth,
    );
    expect(overflow, "no sideways scroll").toBeLessThanOrEqual(0);
  });

  test(`a failed date save at 320 px, ${mode}, shows Thử lại and Thử lại saves`, async ({
    page,
  }) => {
    test.skip(persona !== "owner", "Needs an editor (the owner world)");
    const root = await open(page, mode, 1);
    const row = root.locator("[data-phone-row]").first();
    const id = await row.getAttribute("data-phone-row");
    await row.getByRole("button", { name: /^Đổi ngày của/ }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    const due = dialog.locator("input[type=date]").nth(1);
    const before = await due.inputValue();
    const next = new Date(`${before}T00:00:00Z`);
    next.setUTCDate(next.getUTCDate() + 1);
    const nextDay = next.toISOString().slice(0, 10);
    await due.fill(nextDay);
    await dialog.getByRole("button", { name: "Lưu" }).click();

    const toast = page
      .locator("[data-sonner-toast]")
      .filter({ hasText: "Chưa lưu được ngày" });
    await expect(toast).toBeVisible();
    await expect(toast).toContainText("Mất kết nối. Ngày mới vẫn hiển thị");
    // The new dates stay shown while the save is pending a retry.
    const dd = nextDay.slice(8, 10);
    const mm = nextDay.slice(5, 7);
    await expect(root.locator(`[data-phone-row="${id}"]`)).toContainText(
      `${dd}/${mm}`,
    );
    const toastBox = await box(toast);
    expect
      .soft(toastBox.x, "toast inside the screen")
      .toBeGreaterThanOrEqual(12);
    expect
      .soft(toastBox.x + toastBox.w, "toast inside the screen")
      .toBeLessThanOrEqual(308);
    expect.soft(await css(toast, "border-radius"), "toast radius").toBe("10px");

    await toast.getByRole("button", { name: "Thử lại" }).click();
    await expect(
      page.locator("[data-sonner-toast]").filter({ hasText: "Đã dời" }),
    ).toBeVisible();
    await expect(root.locator(`[data-phone-row="${id}"]`)).toContainText(
      `${dd}/${mm}`,
    );
  });

  test(`undated sheet at 320 px, ${mode}, equals WM-TLCal-320 Khay 320`, async ({
    page,
  }) => {
    test.skip(
      persona !== "viewer",
      "Frame 4 is the view-only tray: run with VITE_MOCK_PERSONA=viewer",
    );
    const root = await open(page, mode);
    const ink = INK[mode];
    await root.getByRole("button", { name: /chưa có ngày/ }).click();
    const sheet = page.locator("[data-phone-tray]");
    await expect(sheet).toBeVisible();
    await page.waitForTimeout(400);

    const sheetBox = await box(sheet);
    near(sheetBox, { x: 0, w: 320 }, "sheet");
    near(
      { ...sheetBox, y: sheetBox.y + sheetBox.h },
      { y: 640 },
      "sheet bottom",
    );
    expect
      .soft(await css(sheet, "border-radius"), "sheet radius")
      .toBe("14px 14px 0px 0px");
    await colour(sheet, "background-color", ink.panel, "sheet");

    const grab = sheet.locator("[data-phone-tray-grab]");
    near(await box(grab, sheet), { x: 142, y: 11, w: 36, h: 4 }, "grab");
    await colour(grab, "background-color", ink.line2, "grab");

    const title = sheet.getByRole("heading", { name: "Chưa lên lịch" });
    near(await box(title, sheet), { x: 17, y: 25, h: 20.3 }, "title");
    await type(title, "14px", "700", "title");
    const count = sheet.locator("[data-phone-tray-count]");
    const countBox = await box(count, sheet);
    near(countBox, { y: 26.5, h: 17.4 }, "count");
    near(
      { ...countBox, x: countBox.x + countBox.w },
      { x: 303 },
      "count right",
    );
    await type(count, "12px", "600", "count");
    await colour(count, "background-color", ink.hover2, "count");

    const note = sheet.locator("[data-phone-tray-note]");
    await expect(note).toHaveText(
      "Bạn chỉ có quyền xem nên không thể đặt lịch.",
    );
    const noteBox = await box(note, sheet);
    near(noteBox, { x: 17, y: 55.3, w: 286 }, "note");
    expect.soft(await css(note, "border-radius"), "note radius").toBe("8px");
    await type(note, "12.5px", "400", "note");
    await colour(note, "background-color", ink.warnSoft, "note");
    await colour(note, "color", ink.warnText, "note");
    near(
      await box(note.locator("svg"), sheet),
      { x: 29, y: 65.3, w: 16, h: 16 },
      "note icon",
    );

    const chips = sheet.locator("[data-phone-tray-item]");
    const n = await chips.count();
    expect(n).toBeGreaterThan(0);
    const chip = chips.first();
    await expect(chip).toHaveAttribute("aria-disabled", "true");
    const chipBox = await box(chip, sheet);
    near(
      chipBox,
      { x: 17, y: noteBox.y + noteBox.h + 10, w: 286, h: 34 },
      "chip",
    );
    expect.soft(await css(chip, "border-radius"), "chip radius").toBe("8px");
    await type(chip, "12.5px", "500", "chip");
    await colour(chip, "background-color", ink.hover, "chip");
    await colour(chip, "border-top-color", ink.line2, "chip");
    near(
      await box(chip.locator("[data-status-glyph]"), chip),
      { x: 11, y: 10.5, w: 13, h: 13 },
      "chip mark",
    );
    const chipKey = chip.locator("span").first();
    near(await box(chipKey, chip), { x: 32, y: 8.7, h: 16.7 }, "chip key");
    await type(chipKey, "11.5px", "500", "chip key");
    await colour(chipKey, "color", ink.meta, "chip key");
    near(
      await box(chip.locator("svg").last(), chip),
      { x: 263, y: 11, w: 12, h: 12 },
      "lock",
    );
    if (n > 1)
      near(
        await box(chips.nth(1), sheet),
        { y: chipBox.y + 42 },
        "second chip",
      );
    const lastBox = await box(chips.nth(n - 1), sheet);
    near(
      { ...lastBox, y: lastBox.y + lastBox.h + 16 },
      { y: sheetBox.h },
      "bottom padding",
    );

    // Read only: no "Đặt ngày" anywhere in the sheet.
    await expect(sheet.getByRole("button", { name: /Đặt ngày/ })).toHaveCount(
      0,
    );
  });
}

test("an editor's undated sheet at 320 px sets the shown day", async ({
  page,
}) => {
  test.skip(persona !== "owner", "Needs an editor (the owner world)");
  const root = await open(page, "sang");
  await root.getByRole("button", { name: /chưa có ngày/ }).click();
  const sheet = page.locator("[data-phone-tray]");
  await expect(sheet).toBeVisible();
  await expect(sheet.locator("[data-phone-tray-note]")).toHaveCount(0);
  const setDue = sheet.getByRole("button", { name: /^Đặt ngày cho/ }).first();
  await expect(setDue).toBeVisible();
  await setDue.click();
  await expect(
    page.locator("[data-sonner-toast]").filter({ hasText: "Đã đặt" }),
  ).toBeVisible();
});
