import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The Calendar at 390x844 against `Final-M-Calendar`, states `thang` and `tuan`, light (`sang`) and dark (`dem`): the month head,
 * the weekday row, the five 54 px week rows, the today circle and its dot, the overdue range bars, the legend, the
 * selected day's agenda heading and the undated bar. Numbers were read from the artboard's `main.cal`
 * (`render.py Final-M-Calendar.dc.html out.html state=thang mode=<m>`), boxes relative to its top-left, which is the
 * phone calendar's own top-left in the app; tolerance is 1 px, radii are exact, the chip fill is compared to 8 levels.
 *
 * Not compared: the artboard's phone shell around `main.cal` (status bar, board header, view row and tab bar are the
 * Final-M-Board shell, not built here), the other bars and dots (the mock board's items span other days than the
 * artboard's sample), the agenda rows, and the undated bar's offset from the top (the app's calendar fills the screen
 * under the shell, the artboard's is 598 px high), so the bar is compared to the bottom edge instead.
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

// x, y, width, height, font size, font weight, radius, fill
type Row = readonly [
  number,
  number,
  number,
  number,
  string,
  string,
  string,
  string,
];

const ART: Record<"sang" | "dem", Record<string, Row>> = {
  sang: {
    prev: [6, 2, 44, 44, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    title: [50, 12.4, 129.9, 23.2, "16px", "650", "0px", "rgba(0, 0, 0, 0)"],
    next: [179.9, 2, 44, 44, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    chip: [283.6, 2, 94.4, 44, "14px", "600", "12px", "rgb(22, 26, 33)"],
    wdRow: [12, 48, 366, 22, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week0: [12, 70, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week1: [12, 124, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week2: [12, 178, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week3: [12, 232, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week4: [12, 286, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    todayNum: [23.1, 181, 30, 30, "15px", "600", "50%", "rgb(22, 102, 204)"],
    todayDot: [35.6, 214, 5, 5, "15px", "400", "50%", "rgb(30, 123, 224)"],
    legend: [0, 340, 390, 46.8, "12px", "400", "0px", "rgba(0, 0, 0, 0)"],
    lg0: [16, 344, 66, 17.4, "12px", "400", "0px", "rgba(0, 0, 0, 0)"],
    lgMile: [16, 365.4, 38.4, 17.4, "12px", "400", "0px", "rgba(0, 0, 0, 0)"],
    dh: [16, 386.8, 358, 44, "15px", "650", "0px", "rgba(0, 0, 0, 0)"],
    nowTag: [
      126.1,
      397.8,
      67.8,
      22,
      "12px",
      "600",
      "6px",
      "rgb(231, 240, 252)",
    ],
    count: [201.9, 399.4, 38.9, 18.8, "13px", "500", "0px", "rgba(0, 0, 0, 0)"],
    list: [16, 430.8, 358, 166.6, "15px", "400", "14px", "rgb(255, 255, 255)"],
    ud: [0, 550, 390, 48, "14px", "400", "0px", "rgb(255, 255, 255)"],
    barOver0: [
      172.8,
      114,
      205.1,
      4,
      "15px",
      "400",
      "2px 0px 0px 2px",
      "rgb(196, 35, 64)",
    ],
    barOver1: [
      12,
      168,
      309.7,
      4,
      "15px",
      "400",
      "0px 2px 2px 0px",
      "rgb(196, 35, 64)",
    ],
  },
  dem: {
    prev: [6, 2, 44, 44, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    title: [50, 12.4, 129.9, 23.2, "16px", "650", "0px", "rgba(0, 0, 0, 0)"],
    next: [179.9, 2, 44, 44, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    chip: [283.6, 2, 94.4, 44, "14px", "600", "12px", "rgb(230, 233, 238)"],
    wdRow: [12, 48, 366, 22, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week0: [12, 70, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week1: [12, 124, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week2: [12, 178, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week3: [12, 232, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    week4: [12, 286, 366, 54, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    todayNum: [23.1, 181, 30, 30, "15px", "600", "50%", "rgb(90, 169, 255)"],
    todayDot: [35.6, 214, 5, 5, "15px", "400", "50%", "rgb(90, 169, 255)"],
    legend: [0, 340, 390, 46.8, "12px", "400", "0px", "rgba(0, 0, 0, 0)"],
    lg0: [16, 344, 66, 17.4, "12px", "400", "0px", "rgba(0, 0, 0, 0)"],
    lgMile: [16, 365.4, 38.4, 17.4, "12px", "400", "0px", "rgba(0, 0, 0, 0)"],
    dh: [16, 386.8, 358, 44, "15px", "650", "0px", "rgba(0, 0, 0, 0)"],
    nowTag: [
      126.1,
      397.8,
      67.8,
      22,
      "12px",
      "600",
      "6px",
      "rgba(90, 169, 255, 0.16)",
    ],
    count: [201.9, 399.4, 38.9, 18.8, "13px", "500", "0px", "rgba(0, 0, 0, 0)"],
    list: [16, 430.8, 358, 166.6, "15px", "400", "14px", "rgb(21, 26, 33)"],
    ud: [0, 550, 390, 48, "14px", "400", "0px", "rgb(21, 26, 33)"],
    barOver0: [
      172.8,
      114,
      205.1,
      4,
      "15px",
      "400",
      "2px 0px 0px 2px",
      "rgb(255, 107, 129)",
    ],
    barOver1: [
      12,
      168,
      309.7,
      4,
      "15px",
      "400",
      "0px 2px 2px 0px",
      "rgb(255, 107, 129)",
    ],
  },
};

const ART_TUAN: Record<"sang" | "dem", Record<string, Row>> = {
  sang: {
    prev: [6, 2, 44, 44, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    title: [50, 12.4, 136.5, 23.2, "16px", "650", "0px", "rgba(0, 0, 0, 0)"],
    next: [186.5, 2, 44, 44, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    chip: [284.1, 2, 93.9, 44, "14px", "500", "12px", "rgb(255, 255, 255)"],
    wk: [0, 48, 390, 69, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    wd0: [16, 48, 44, 63, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    pill0: [16, 48, 44, 52, "15px", "400", "12px", "rgb(22, 102, 204)"],
    small0: [30.1, 56.1, 15.8, 14.4, "12px", "500", "0px", "rgba(0, 0, 0, 0)"],
    b0: [28.7, 71.5, 18.6, 20.4, "17px", "650", "0px", "rgba(0, 0, 0, 0)"],
    dot0: [30.5, 105, 6, 6, "15px", "400", "50%", "rgb(30, 123, 224)"],
    wd6: [330, 48, 44, 63, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    ovd: [16, 119, 358, 56, "15px", "400", "12px", "rgb(253, 236, 239)"],
    dh: [16, 183, 358, 44, "15px", "650", "0px", "rgba(0, 0, 0, 0)"],
    nowTag: [126.1, 194, 67.8, 22, "12px", "600", "6px", "rgb(231, 240, 252)"],
    count: [201.9, 195.6, 38.9, 18.8, "13px", "500", "0px", "rgba(0, 0, 0, 0)"],
    list: [16, 227, 358, 166.6, "15px", "400", "14px", "rgb(255, 255, 255)"],
    ud: [0, 550, 390, 48, "14px", "400", "0px", "rgb(255, 255, 255)"],
  },
  dem: {
    prev: [6, 2, 44, 44, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    title: [50, 12.4, 136.5, 23.2, "16px", "650", "0px", "rgba(0, 0, 0, 0)"],
    next: [186.5, 2, 44, 44, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    chip: [284.1, 2, 93.9, 44, "14px", "500", "12px", "rgb(21, 26, 33)"],
    wk: [0, 48, 390, 69, "15px", "400", "0px", "rgba(0, 0, 0, 0)"],
    wd0: [16, 48, 44, 63, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    pill0: [16, 48, 44, 52, "15px", "400", "12px", "rgb(90, 169, 255)"],
    small0: [30.1, 56.1, 15.8, 14.4, "12px", "500", "0px", "rgba(0, 0, 0, 0)"],
    b0: [28.7, 71.5, 18.6, 20.4, "17px", "650", "0px", "rgba(0, 0, 0, 0)"],
    dot0: [30.5, 105, 6, 6, "15px", "400", "50%", "rgb(90, 169, 255)"],
    wd6: [330, 48, 44, 63, "15px", "400", "12px", "rgba(0, 0, 0, 0)"],
    ovd: [16, 119, 358, 56, "15px", "400", "12px", "rgba(255, 107, 129, 0.14)"],
    dh: [16, 183, 358, 44, "15px", "650", "0px", "rgba(0, 0, 0, 0)"],
    nowTag: [
      126.1,
      194,
      67.8,
      22,
      "12px",
      "600",
      "6px",
      "rgba(90, 169, 255, 0.16)",
    ],
    count: [201.9, 195.6, 38.9, 18.8, "13px", "500", "0px", "rgba(0, 0, 0, 0)"],
    list: [16, 227, 358, 166.6, "15px", "400", "14px", "rgb(21, 26, 33)"],
    ud: [0, 550, 390, 48, "14px", "400", "0px", "rgb(21, 26, 33)"],
  },
};

const KEYS: Record<string, readonly ("x" | "y" | "w" | "h")[]> = {
  count: ["x", "y", "h"],
  list: ["x", "y", "w"],
  ovd: ["x", "y", "w", "h"],
  ud: ["x", "w", "h"],
};

function fillGap(a: string, b: string): number {
  const nums = (c: string) => (c.match(/[\d.]+/g) ?? []).map(Number);
  const [x, y] = [nums(a), nums(b)];
  if (x.length < 3 || y.length < 3) return a === b ? 0 : 255;
  return Math.max(...[0, 1, 2].map((i) => Math.abs(x[i]! - y[i]!)));
}

function locators(page: Page): Record<string, Locator> {
  const root = page.locator("[data-phone-calendar]");
  const head = root.locator("> div").first();
  const weeks = root.locator("[data-calendar-week]");
  const todayCell = root
    .locator("[role=gridcell]")
    .filter({ has: page.locator("[aria-current=date]") });
  const legend = root.locator("p").first();
  const day = root.locator("h3");
  return {
    prev: head.locator("button").nth(0),
    title: head.locator("h2"),
    next: head.locator("button").nth(1),
    chip: head.locator("button").nth(2),
    wdRow: root.locator("[role=row]").first(),
    week0: weeks.nth(0),
    week1: weeks.nth(1),
    week2: weeks.nth(2),
    week3: weeks.nth(3),
    week4: weeks.nth(4),
    todayNum: root.locator("[aria-current=date] b"),
    todayDot: todayCell.locator("span > span").first(),
    legend,
    lg0: legend.locator("> span").nth(0),
    lgMile: legend.locator("> span").nth(4),
    dh: day,
    nowTag: day.locator("> span").first(),
    count: day.locator("> span").nth(1),
    list: root.locator("ul").first(),
    ud: root.getByRole("button", { name: /chưa có ngày/ }),
    barOver0: weeks.nth(0).locator('[data-phone-bar="mock-erp-item-142"]'),
    barOver1: weeks.nth(1).locator('[data-phone-bar="mock-erp-item-142"]'),
    wk: root.locator("[role=row]").first(),
    wd0: root.locator("[role=row]").first().locator("button").nth(0),
    pill0: root
      .locator("[role=row]")
      .first()
      .locator("button")
      .nth(0)
      .locator("> span")
      .first(),
    small0: root
      .locator("[role=row]")
      .first()
      .locator("button")
      .nth(0)
      .locator("small"),
    b0: root
      .locator("[role=row]")
      .first()
      .locator("button")
      .nth(0)
      .locator("b"),
    dot0: root
      .locator("[role=row]")
      .first()
      .locator("button")
      .nth(0)
      .locator("> span")
      .nth(1)
      .locator("span")
      .first(),
    wd6: root.locator("[role=row]").first().locator("button").nth(6),
    ovd: root.locator("[data-phone-overdue]"),
  };
}

async function open(page: Page, mode: "sang" | "dem", week: boolean) {
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
  // The day that is already open opens its week.
  if (week) await page.locator("[aria-current=date]").click();
  await page.waitForTimeout(500);
  return root;
}

async function compare(
  page: Page,
  root: Locator,
  art: Record<string, Row>,
  min: number,
) {
  const origin = await root.boundingBox();
  if (!origin) throw new Error("no phone calendar");
  const loc = locators(page);
  let compared = 0;
  for (const [key, row] of Object.entries(art)) {
    const target = loc[key];
    if (!target) throw new Error(`no locator for ${key}`);
    const got = await target.boundingBox();
    expect(got, `${key} is drawn`).not.toBeNull();
    if (!got) continue;
    const box = {
      x: got.x - origin.x,
      y:
        key === "ud" ? origin.y + origin.height - 48 - got.y : got.y - origin.y,
      w: got.width,
      h: got.height,
    };
    const idx = { x: 0, y: 1, w: 2, h: 3 } as const;
    for (const k of KEYS[key] ?? ["x", "y", "w", "h"]) {
      const want = k === "y" && key === "ud" ? 0 : row[idx[k]];
      expect
        .soft(
          Math.abs(box[k] - want),
          `${key}.${k}: ${box[k].toFixed(1)} vs ${want}`,
        )
        .toBeLessThanOrEqual(1);
      compared += 1;
    }
    if (
      ["prev", "next", "chip", "barOver0", "barOver1", "pill0", "ovd"].includes(
        key,
      )
    ) {
      const style = await target.evaluate((el) => {
        const c = getComputedStyle(el);
        return { r: c.borderRadius, fs: c.fontSize, fw: c.fontWeight };
      });
      expect.soft(style.r, `${key} radius`).toBe(row[6]);
      if (key === "chip") {
        expect.soft(style.fs, "chip font size").toBe(row[4]);
        expect.soft(style.fw, "chip font weight").toBe(row[5]);
      }
    }
    if (["small0", "b0"].includes(key)) {
      const style = await target.evaluate((el) => {
        const c = getComputedStyle(el);
        return { fs: c.fontSize, fw: c.fontWeight };
      });
      expect.soft(style.fs, `${key} font size`).toBe(row[4]);
      expect.soft(style.fw, `${key} font weight`).toBe(row[5]);
    }
    if (["chip", "pill0", "ovd"].includes(key)) {
      const bg = await target.evaluate((el) => {
        const ctx = document.createElement("canvas").getContext("2d")!;
        ctx.fillStyle = getComputedStyle(el).backgroundColor;
        ctx.fillRect(0, 0, 1, 1);
        const [r, g, b] = ctx.getImageData(0, 0, 1, 1).data;
        return `rgb(${r}, ${g}, ${b})`;
      });
      // The overdue link's tint is a token over the page: 8 levels is the app's usual distance from the artboard.
      expect
        .soft(fillGap(bg, row[7]), `${key} fill ${bg} vs ${row[7]}`)
        .toBeLessThanOrEqual(8);
    }
  }
  expect(compared).toBeGreaterThan(min);
}

for (const mode of ["sang", "dem"] as const) {
  test(`calendar month at 390 px, ${mode}, equals Final-M-Calendar thang`, async ({
    page,
  }) => {
    const root = await open(page, mode, false);
    await compare(page, root, ART[mode], 70);
  });
  test(`calendar week at 390 px, ${mode}, equals Final-M-Calendar tuan`, async ({
    page,
  }) => {
    const root = await open(page, mode, true);
    await compare(page, root, ART_TUAN[mode], 60);
  });
}
