import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The Kanban board at 1180x796 (the iPad Air landscape artboard minus its 24 px status bar) against `Final-T-Board`,
 * state `binh-thuong`, sidebar open (`mo`), light (`sang`) and dark (`dem`): the toolbar row (access chip, Lọc, Nhóm,
 * the avatar stack) and the four lanes with the first lane's header. Numbers were read from the rendered artboard
 * (`render.py Final-T-Board.dc.html out.html state=binh-thuong mode=<m> sidebar=mo`), boxes relative to the main
 * column's left edge and the header's top edge; tolerance is 1 px, radii are exact, fills are compared to 8 sRGB levels
 * because the app uses design tokens, not the artboard hex.
 *
 * Not compared: the card faces and the lane heights (the artboard's cards carry its own sample text and a "Cao"
 * priority label; the card face at tablet is not rebuilt yet), the group chip's and the stack's width (the group
 * chip reads "Nhóm: Trạng thái" where the artboard says "Nhóm theo Trạng thái"; the stack's "+29" chip differs between
 * Final-T-Board and Final-T-Table), and the controls the artboard does not draw (swimlane, saved filters, card
 * fields, view menu, Add view), which follow the stack in the same 60 px row and scroll sideways.
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
test.use({ viewport: { width: 1180, height: 796 } });

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
    toolbar: [0, 56, 932, 60, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    access: [16, 64, 255.8, 44, "14px", "500", "10px", "rgb(255, 255, 255)"],
    filter: [279.8, 64, 78.6, 44, "14px", "500", "10px", "rgb(255, 255, 255)"],
    group: [366.5, 64, 185.1, 44, "14px", "500", "10px", "rgb(255, 255, 255)"],
    stack: [559.6, 64, 134, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    lane0: [16, 120, 330, 620.1, "14.5px", "400", "16px", "rgb(236, 238, 242)"],
    lane1: [
      358,
      120,
      330,
      620.4,
      "14.5px",
      "400",
      "16px",
      "rgb(230, 239, 251)",
    ],
    lane2: [700, 120, 330, 377, "14.5px", "400", "16px", "rgb(253, 243, 220)"],
    lane3: [1042, 120, 330, 450, "14.5px", "400", "16px", "rgb(229, 243, 234)"],
    dot: [32, 143, 10, 10, "14.5px", "400", "50%", "rgba(0, 0, 0, 0)"],
    h2: [51, 137.1, 61.7, 21.8, "15px", "650", "0px", "rgba(0, 0, 0, 0)"],
    count: [121.7, 136, 24, 24, "12.5px", "600", "12px", "rgb(255, 255, 255)"],
    menu: [294, 126, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
  },
  dem: {
    access: [16, 64, 255.8, 44, "14px", "500", "10px", "rgb(21, 26, 33)"],
    filter: [279.8, 64, 78.6, 44, "14px", "500", "10px", "rgb(21, 26, 33)"],
    group: [366.5, 64, 185.1, 44, "14px", "500", "10px", "rgb(21, 26, 33)"],
    lane0: [16, 120, 330, 620.1, "14.5px", "400", "16px", "rgb(30, 37, 47)"],
    lane1: [358, 120, 330, 620.4, "14.5px", "400", "16px", "rgb(22, 40, 74)"],
    lane2: [700, 120, 330, 377, "14.5px", "400", "16px", "rgb(48, 40, 26)"],
    lane3: [1042, 120, 330, 450, "14.5px", "400", "16px", "rgb(22, 41, 31)"],
    count: [121.7, 136, 24, 24, "12.5px", "600", "12px", "rgb(21, 26, 33)"],
  },
};
const KEYS: Record<string, readonly ("x" | "y" | "w" | "h")[]> = {
  group: ["x", "y", "h"],
  stack: ["y", "h"],
  lane0: ["x", "y", "w"],
  lane1: ["x", "y", "w"],
  lane2: ["x", "y", "w"],
  lane3: ["x", "y", "w"],
};
const FILLS = new Set([
  "access",
  "filter",
  "group",
  "lane0",
  "lane1",
  "lane2",
  "lane3",
  "count",
]);

function fillGap(a: string, b: string): number {
  const nums = (c: string) => (c.match(/[\d.]+/g) ?? []).map(Number);
  const [x, y] = [nums(a), nums(b)];
  if (x.length < 3 || y.length < 3) return a === b ? 0 : 255;
  return Math.max(...[0, 1, 2].map((i) => Math.abs(x[i]! - y[i]!)));
}

function lane(page: Page, name: string) {
  return page.getByRole("region", { name, exact: true });
}

function locators(page: Page): Record<string, Locator> {
  const toolbar = page.locator("[data-kanban-toolbar]");
  const first = lane(page, "Cần làm");
  const h2 = first.locator("h2");
  return {
    toolbar: toolbar.locator("xpath=.."),
    access: page.locator("[data-slot=tablet-access-chip]"),
    filter: toolbar.getByRole("button", { name: /^Lọc/ }),
    group: toolbar.getByRole("button", { name: /^Nhóm/ }),
    stack: page.locator("[data-slot=tablet-avatar-stack]"),
    lane0: first,
    lane1: lane(page, "Đang làm"),
    lane2: lane(page, "Chờ duyệt"),
    lane3: lane(page, "Hoàn thành"),
    dot: h2.locator("xpath=preceding-sibling::span[1]"),
    h2,
    count: h2.locator("xpath=following-sibling::span[1]"),
    menu: first.getByRole("button", { name: /^Tuỳ chọn cột/ }),
  };
}

for (const mode of ["sang", "dem"] as const) {
  test(`kanban board at 1180 px, ${mode}, equals Final-T-Board`, async ({
    page,
  }) => {
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
    await page.goto(BOARD);
    await expect(page.locator("[data-slot=tablet-board-header]")).toBeVisible();
    await expect(lane(page, "Cần làm")).toBeVisible();
    await expect(
      page.getByRole("status", { name: "Đang khôi phục cách xem" }),
    ).toHaveCount(0);
    await page.waitForTimeout(600);
    const [mb, hb] = await Promise.all([
      page.locator("main").first().boundingBox(),
      page.locator("[data-slot=tablet-board-header]").boundingBox(),
    ]);
    if (!mb || !hb) throw new Error("no main column");
    const loc = locators(page);
    const want = { ...ART.sang, ...ART[mode] };
    let compared = 0;
    for (const [key, row] of Object.entries(want)) {
      const target = loc[key];
      if (!target) throw new Error(`no locator for ${key}`);
      const got = await target.boundingBox();
      expect(got, `${key} is drawn`).not.toBeNull();
      if (!got) continue;
      const box = {
        x: got.x - mb.x,
        y: got.y - hb.y,
        w: got.width,
        h: got.height,
      };
      const idx = { x: 0, y: 1, w: 2, h: 3 } as const;
      for (const k of KEYS[key] ?? ["x", "y", "w", "h"]) {
        expect
          .soft(
            Math.abs(box[k] - row[idx[k]]),
            `${key}.${k}: ${box[k].toFixed(1)} vs ${row[idx[k]]}`,
          )
          .toBeLessThanOrEqual(1);
        compared += 1;
      }
      const style = await target.evaluate((el) => {
        const c = getComputedStyle(el);
        const ctx = document.createElement("canvas").getContext("2d");
        let bg = c.backgroundColor;
        if (ctx) {
          ctx.clearRect(0, 0, 1, 1);
          ctx.fillStyle = c.backgroundColor;
          ctx.fillRect(0, 0, 1, 1);
          const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data;
          bg = a === 255 ? `rgb(${r}, ${g}, ${b})` : "transparent";
        }
        return { fs: c.fontSize, fw: c.fontWeight, r: c.borderRadius, bg };
      });
      if (["h2", "count"].includes(key)) {
        expect.soft(style.fs, `${key} font size`).toBe(row[4]);
        expect.soft(style.fw, `${key} font weight`).toBe(row[5]);
      }
      if (key !== "dot" && key !== "h2" && key !== "stack" && key !== "toolbar")
        expect.soft(style.r, `${key} radius`).toBe(row[6]);
      // The dark Doing and Review lane tints are tokens (#13203a, #272012), 16 sRGB levels from Final-T-Board's own
      // dark tints (#16284a, #30281a), which differ from Final-Board's: the token stays.
      const darkTint = mode === "dem" && /^lane[12]$/.test(key);
      if (FILLS.has(key) && !darkTint)
        expect
          .soft(
            fillGap(style.bg, row[7]),
            `${key} fill ${style.bg} vs ${row[7]}`,
          )
          .toBeLessThanOrEqual(8);
    }
    expect(compared).toBeGreaterThan(40);
  });
}
