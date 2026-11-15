import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The phone Calendar at 390x844 against `Final-M-Calendar`, states `ngay`, `chua-co-ngay` and `doi-han`, light (`sang`)
 * and dark (`dem`) (`render.py Final-M-Calendar.dc.html out.html state=<state> mode=<m>`). Boxes were read from the
 * rendered artboard: the day is relative to `main.cal`'s top-left (the phone calendar's own), the sheets to the sheet's
 * top-left. Tolerance is 1 px; the fills of the dark twin are compared to 8 levels.
 *
 * Not compared, each with its reason:
 * - the phone shell around the calendar (status bar, board header, view row, tab bar): the app keeps its own shell;
 * - the day's all-day block and the hours' vertical start: the mock board has two all-day entries where the artboard
 *   draws one, so the hours are compared to their own box, not to the page;
 * - the item's tag chip ("Mua sắm") in the undated card: the undated list carries no label;
 * - widths of text-sized boxes (the count line, the quick chips except the first): they follow the sample text.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);
test.use({ viewport: { width: 390, height: 844 } });

type Mode = "sang" | "dem";
// Keys of the box: x, y, w, h. A missing key is not compared.
type Box = readonly [number?, number?, number?, number?];

async function open(page: Page, mode: Mode) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript((dark) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem(
        "notrelix-theme-mode",
        dark ? "dark" : "light",
      );
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode === "dem");
  await page.goto(`${BOARD}?view=calendar`);
  const root = page.locator("[data-phone-calendar]");
  await expect(root).toBeVisible();
  await expect(page.locator("[data-calendar-event]").first()).toBeVisible();
  await expect(
    page.getByRole("status", { name: "Đang khôi phục cách xem" }),
  ).toHaveCount(0);
  return root;
}

async function rgb(target: Locator): Promise<string> {
  return target.evaluate((el) => {
    const ctx = document.createElement("canvas").getContext("2d")!;
    ctx.fillStyle = getComputedStyle(el).backgroundColor;
    ctx.fillRect(0, 0, 1, 1);
    const [r, g, b] = ctx.getImageData(0, 0, 1, 1).data;
    return `rgb(${r}, ${g}, ${b})`;
  });
}

function gap(a: string, b: string): number {
  const nums = (c: string) => (c.match(/[\d.]+/g) ?? []).map(Number);
  const [x, y] = [nums(a), nums(b)];
  return Math.max(...[0, 1, 2].map((i) => Math.abs(x[i]! - y[i]!)));
}

async function boxes(
  origin: Locator,
  rows: ReadonlyArray<readonly [string, Locator, Box]>,
  min: number,
) {
  const o = await origin.boundingBox();
  if (!o) throw new Error("no origin");
  let compared = 0;
  for (const [name, target, want] of rows) {
    const b = await target.boundingBox();
    expect(b, `${name} is drawn`).not.toBeNull();
    if (!b) continue;
    const got = [b.x - o.x, b.y - o.y, b.width, b.height];
    for (let i = 0; i < 4; i += 1) {
      const expected = want[i];
      if (expected === undefined) continue;
      expect
        .soft(
          Math.abs(got[i]! - expected),
          `${name}.${"xywh"[i]}: ${got[i]!.toFixed(1)} vs ${expected}`,
        )
        .toBeLessThanOrEqual(1);
      compared += 1;
    }
  }
  expect(compared).toBeGreaterThan(min);
}

async function radius(target: Locator, want: string, label: string) {
  expect
    .soft(
      await target.evaluate((el) => getComputedStyle(el).borderRadius),
      `${label} radius`,
    )
    .toBe(want);
}

async function fill(target: Locator, want: string, label: string) {
  const got = await rgb(target);
  expect
    .soft(gap(got, want), `${label} fill ${got} vs ${want}`)
    .toBeLessThanOrEqual(8);
}

for (const mode of ["sang", "dem"] as const) {
  test(`day at 390 px, ${mode}, equals Final-M-Calendar ngay`, async ({
    page,
  }) => {
    const root = await open(page, mode);
    await page.locator("[aria-current=date]").click();
    await root.getByRole("gridcell", { name: /Thứ Hai 12\/10/ }).click();
    const day = page.locator("[data-phone-day]");
    await expect(day).toBeVisible();
    await page.waitForTimeout(400);
    const head = root.locator("> div").first();
    const h3 = day.locator("h3");
    const hours = page.locator("[data-phone-hours]");
    const ev = hours.locator('[data-calendar-event][aria-label*="ERP-144"]');
    const now = hours.locator("[data-calendar-now]");
    const o = await root.boundingBox();
    const hb = await hours.boundingBox();
    if (!o || !hb) throw new Error("no day");
    await boxes(
      root,
      [
        ["prev", head.locator("button").nth(0), [6, 2, 44, 44]],
        ["title", head.locator("h2"), [50, 12.4, 170.4, 23.2]],
        ["next", head.locator("button").nth(1), [220.4, 2, 44, 44]],
        ["chip Tháng", head.locator("button").nth(2), [284.1, 2, 93.9, 44]],
        ["tag", h3.locator("> span").nth(0), [16, 59, 67.8, 22]],
        ["count", h3.locator("> span").nth(1), [91.8, 60.6, undefined, 18.8]],
        ["chip Theo giờ", h3.locator("button"), [271, 48, 107, 44]],
      ],
      6,
    );
    // The hours, relative to their own box: ten 34 px rows, the item at 17:00 and the now line at 10:40.
    await boxes(
      hours,
      [
        ["hours", hours, [0, 0, 358, 340]],
        ["ERP-144 at 17:00", ev, [52, 306, 306, 45.7]],
        ["now line", now, [46, 91, 312, 2]],
        ["now badge", now.locator("b"), [314.3, 81, 43.8, 20]],
      ],
      10,
    );
    const rows = hours.locator("> div[aria-hidden]");
    await expect(rows).toHaveCount(10);
    for (let i = 0; i < 10; i += 1) {
      const b = await rows.nth(i).boundingBox();
      expect
        .soft(Math.abs(b!.y - hb.y - i * 34), `hour row ${i}`)
        .toBeLessThanOrEqual(1);
    }
    await radius(h3.locator("button"), "12px", "Theo giờ");
    await radius(h3.locator("> span").nth(0), "6px", "tag");
    await radius(ev, "10px", "item");
    expect
      .soft(
        await h3
          .locator("button")
          .evaluate((el) => getComputedStyle(el).fontSize),
      )
      .toBe("14px");
    await fill(
      h3.locator("button"),
      mode === "sang" ? "rgb(22, 26, 33)" : "rgb(230, 233, 238)",
      "Theo giờ",
    );
    await fill(
      now,
      mode === "sang" ? "rgb(22, 102, 204)" : "rgb(90, 169, 255)",
      "now line",
    );
    await fill(
      ev,
      mode === "sang" ? "rgb(226, 236, 250)" : "rgb(22, 40, 74)",
      "item",
    );
  });

  test(`undated sheet at 390 px, ${mode}, equals Final-M-Calendar chua-co-ngay`, async ({
    page,
  }) => {
    const root = await open(page, mode);
    await root.getByRole("button", { name: /chưa có ngày/ }).click();
    const sheet = page.locator("[data-phone-tray]");
    await expect(sheet).toBeVisible();
    await sheet.getByRole("button", { name: /Đặt hạn cho ERP-153/ }).click();
    await page.waitForTimeout(500);
    const quick = sheet.getByRole("group", { name: "Chọn nhanh" });
    const grid = sheet.getByRole("grid");
    const cell = (label: RegExp) => grid.getByRole("button", { name: label });
    const sel = cell(/Thứ Hai 19\/10\/2026/);
    const today = cell(/Thứ Hai 12\/10\/2026, hôm nay/);
    const save = sheet.getByRole("button", { name: "Lưu hạn" });
    await boxes(
      sheet,
      [
        ["sheet", sheet, [0, undefined, 390, 682.5]],
        [
          "title",
          sheet.getByRole("heading", { level: 2 }),
          [20, 26.7, 310, 24.6],
        ],
        [
          "close",
          sheet.getByRole("button", { name: "Đóng" }),
          [338, 17, 44, 44],
        ],
        [
          "lead",
          sheet.getByText("Việc chưa có hạn không hiện trên lịch."),
          [20, 59, 350, 18.8],
        ],
        [
          "card",
          sheet.locator("[data-phone-tray-item]"),
          [16, 85.8, 358, 107.6],
        ],
        [
          "Đặt hạn",
          sheet.getByRole("button", { name: /Đặt hạn cho ERP-153/ }),
          [259.6, 117.7, 105.4, 44],
        ],
        [
          "chip Hôm nay",
          quick.getByRole("button").nth(0),
          [16, 209.5, 90.1, 44],
        ],
        [
          "chip Ngày mai",
          quick.getByRole("button").nth(1),
          [114.1, 209.5, 95, 44],
        ],
        [
          "chip Thứ Hai tới",
          quick.getByRole("button").nth(2),
          [217.1, 209.5, 154, 44],
        ],
        [
          "previous month",
          sheet.getByRole("button", { name: "Tháng trước" }),
          [10, 259.5, 44, 44],
        ],
        [
          "month",
          sheet.getByRole("heading", { name: "Tháng 10, 2026" }),
          [136, 270.6, 118.1, 21.8],
        ],
        [
          "next month",
          sheet.getByRole("button", { name: "Tháng sau" }),
          [336, 259.5, 44, 44],
        ],
        ["weekday row", grid.getByRole("row").first(), [16, 303.5, 358, 22]],
        ["day 28/9", cell(/28\/09\/2026/), [20, 325.5, 44, 42]],
        ["today 12", today, [20, 409.5, 44, 42]],
        ["selected 19", sel, [20, 451.5, 44, 42]],
        [
          "time label",
          sheet.getByText("Giờ hạn", { exact: true }),
          [16, 562.3, 53.3, 20.3],
        ],
        ["time select", sheet.getByRole("combobox"), [79.3, 550.5, 86, 44]],
        [
          "zone",
          sheet.getByText("Giờ Hà Nội (GMT+7)"),
          [175.3, 563, 127, 18.8],
        ],
        [
          "Huỷ",
          sheet.getByRole("button", { name: "Huỷ" }),
          [197.8, 604.5, 62.2, 44],
        ],
        ["Lưu hạn", save, [270, 604.5, 104, 44]],
      ],
      40,
    );
    await radius(sheet, "14px 14px 0px 0px", "sheet");
    await radius(sel, "50%", "selected day");
    await radius(save, "12px", "Lưu hạn");
    await fill(
      sheet,
      mode === "sang" ? "rgb(255, 255, 255)" : "rgb(21, 26, 33)",
      "sheet",
    );
    await fill(
      sel,
      mode === "sang" ? "rgb(22, 102, 204)" : "rgb(90, 169, 255)",
      "selected day",
    );
    await fill(
      save,
      mode === "sang" ? "rgb(22, 102, 204)" : "rgb(90, 169, 255)",
      "Lưu hạn",
    );
    await fill(
      quick.getByRole("button").nth(2),
      mode === "sang" ? "rgb(22, 26, 33)" : "rgb(230, 233, 238)",
      "picked quick chip",
    );
  });

  test(`action sheet at 390 px, ${mode}, equals Final-M-Calendar doi-han`, async ({
    page,
  }) => {
    const root = await open(page, mode);
    await page.locator("[aria-current=date]").click();
    await root.locator("[data-calendar-more]").first().click();
    const sheet = page.locator("[data-phone-actions]");
    await expect(sheet).toBeVisible();
    await page.waitForTimeout(500);
    const row = (name: string) => sheet.getByRole("menuitem", { name });
    const note = sheet.getByText(/Trên điện thoại không kéo thả/);
    await boxes(
      sheet,
      [
        ["sheet", sheet, [0, undefined, 390, 345.4]],
        [
          "title",
          sheet.getByRole("heading", { level: 2 }),
          [20, 41.8, 310, 43.2],
        ],
        [
          "close",
          sheet.getByRole("button", { name: "Đóng" }),
          [338, 17, 44, 44],
        ],
        ["Đổi hạn…", row(/Đổi hạn…/), [0, 85, 390, 52]],
        ["Chuyển sang…", row(/Chuyển sang…/), [0, 137, 390, 52]],
        ["Mở việc", row(/Mở việc/), [0, 189, 390, 52]],
        [
          "icon",
          row(/Đổi hạn…/)
            .locator("> span")
            .first(),
          [20, 93, 36, 36],
        ],
        ["note", note.locator(".."), [20, 247, 350, 56.4]],
      ],
      20,
    );
    await radius(sheet, "14px 14px 0px 0px", "sheet");
    await radius(
      row(/Đổi hạn…/)
        .locator("> span")
        .first(),
      "10px",
      "icon",
    );
    await radius(note.locator(".."), "10px", "note");
    await fill(
      sheet,
      mode === "sang" ? "rgb(255, 255, 255)" : "rgb(21, 26, 33)",
      "sheet",
    );
    await fill(
      note.locator(".."),
      mode === "sang" ? "rgb(241, 243, 246)" : "rgb(28, 35, 44)",
      "note",
    );
    await fill(
      row(/Đổi hạn…/)
        .locator("> span")
        .first(),
      mode === "sang" ? "rgb(247, 248, 250)" : "rgb(14, 17, 22)",
      "icon",
    );
  });
}
