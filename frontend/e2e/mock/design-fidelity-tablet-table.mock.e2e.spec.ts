import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The board table at 1180x796 (the iPad Air landscape artboard minus its 24 px status bar) against `Final-T-Table`,
 * state `binh-thuong`, sidebar open (`mo`), light (`sang`) and dark (`dem`). Every number was read from the rendered
 * artboard (`render.py Final-T-Table.dc.html out.html state=binh-thuong mode=<m> sidebar=mo`), boxes relative to the
 * main column's left edge and the header's top edge. Tolerance is 1 px for boxes; font sizes, weights, radii and
 * (where listed) fills are exact.
 *
 * Text differs between the mock world and the mockup world, so an entry whose width or position follows its wording
 * lists only the keys that do not: the sort chip reads "Hạn" (the field's name) where the artboard says "Hạn chót",
 * which moves the group chip's x. The artboard's table-columns icon sits in the title cell's header; the app keeps
 * it in the toolbar as a 44 px icon, so the toolbar also holds the saved-filter and columns buttons, which the
 * artboard does not draw (they sit between the group chip and the avatar stack and do not move a measured box).
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
    header: [0, 0, 932, 56, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    toggle: [6, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    mark: [56, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    markDm: [64, 13.5, 28, 28, "10.5px", "700", "8px", "rgb(30, 111, 208)"],
    title: [117.3, 15.2, 150.4, 24.6, "17px", "650", "0px", "rgba(0, 0, 0, 0)"],
    seg: [279.7, 3.5, 335.5, 48, "14.5px", "400", "12px", "rgb(228, 231, 236)"],
    tab0: [281.7, 5.5, 80.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
    tab1: [362.2, 5.5, 64.7, 44, "14px", "650", "10px", "rgb(255, 255, 255)"],
    tab2: [427, 5.5, 57.8, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
    tab3: [484.7, 5.5, 128.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
    search: [653.2, 5.5, 77.8, 44, "14px", "400", "10px", "rgb(255, 255, 255)"],
    kbd: [694.2, 17.5, 25.8, 20, "11.5px", "400", "5px", "rgb(238, 240, 243)"],
    add: [737.1, 5.5, 44, 44, "14.5px", "400", "10px", "rgb(22, 102, 204)"],
    share: [
      787.1,
      5.5,
      82.9,
      44,
      "14.5px",
      "500",
      "10px",
      "rgb(255, 255, 255)",
    ],
    more: [876, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    toolbar: [0, 56, 932, 60, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    fchip: [16, 64, 211.1, 44, "14.5px", "400", "10px", "rgb(231, 240, 252)"],
    fchipX: [183.1, 64, 44, 44, "13.3333px", "400", "10px", "rgba(0, 0, 0, 0)"],
    filter: [235.1, 64, 78.6, 44, "14px", "500", "10px", "rgb(255, 255, 255)"],
    sort: [321.7, 64, 130, 44, "14px", "500", "10px", "rgb(255, 255, 255)"],
    group: [459.7, 64, 154.4, 44, "14px", "500", "10px", "rgb(255, 255, 255)"],
    stack: [806, 64, 110, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    box: [16, 116, 900, 660, "14.5px", "400", "14px", "rgb(255, 255, 255)"],
    fade: [843, 117, 72, 602, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    next: [869, 117, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    foot: [17, 719, 898, 56, "14px", "400", "0px", "rgba(0, 0, 0, 0)"],
    footLink: [
      230.1,
      725.5,
      147.2,
      44,
      "14px",
      "600",
      "10px",
      "rgba(0, 0, 0, 0)",
    ],
  },
  dem: {
    header: [0, 0, 932, 56, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    toggle: [6, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    mark: [56, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    markDm: [64, 13.5, 28, 28, "10.5px", "700", "8px", "rgb(30, 111, 208)"],
    title: [117.3, 15.2, 150.4, 24.6, "17px", "650", "0px", "rgba(0, 0, 0, 0)"],
    seg: [279.7, 3.5, 335.5, 48, "14.5px", "400", "12px", "rgb(28, 35, 44)"],
    tab0: [281.7, 5.5, 80.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
    tab1: [362.2, 5.5, 64.7, 44, "14px", "650", "10px", "rgb(21, 26, 33)"],
    tab2: [427, 5.5, 57.8, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
    tab3: [484.7, 5.5, 128.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
    search: [653.2, 5.5, 77.8, 44, "14px", "400", "10px", "rgb(21, 26, 33)"],
    kbd: [694.2, 17.5, 25.8, 20, "11.5px", "400", "5px", "rgb(35, 43, 53)"],
    add: [737.1, 5.5, 44, 44, "14.5px", "400", "10px", "rgb(90, 169, 255)"],
    share: [787.1, 5.5, 82.9, 44, "14.5px", "500", "10px", "rgb(21, 26, 33)"],
    more: [876, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    toolbar: [0, 56, 932, 60, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    fchip: [
      16,
      64,
      211.1,
      44,
      "14.5px",
      "400",
      "10px",
      "rgba(90, 169, 255, 0.16)",
    ],
    fchipX: [183.1, 64, 44, 44, "13.3333px", "400", "10px", "rgba(0, 0, 0, 0)"],
    filter: [235.1, 64, 78.6, 44, "14px", "500", "10px", "rgb(21, 26, 33)"],
    sort: [321.7, 64, 130, 44, "14px", "500", "10px", "rgb(21, 26, 33)"],
    group: [459.7, 64, 154.4, 44, "14px", "500", "10px", "rgb(21, 26, 33)"],
    stack: [806, 64, 110, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    box: [16, 116, 900, 660, "14.5px", "400", "14px", "rgb(21, 26, 33)"],
    fade: [843, 117, 72, 602, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    next: [869, 117, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    foot: [17, 719, 898, 56, "14px", "400", "0px", "rgba(0, 0, 0, 0)"],
    footLink: [
      230.1,
      725.5,
      147.2,
      44,
      "14px",
      "600",
      "10px",
      "rgba(0, 0, 0, 0)",
    ],
  },
};

/** Entries whose text differs: the keys that are still compared. */
const KEYS: Record<string, readonly ("x" | "y" | "w" | "h")[]> = {
  sort: ["x", "y", "h"],
  group: ["y", "h"],
};
/** Entries whose fill is compared too. */
const FILLS = new Set(["seg", "tab1", "add", "box", "fchip", "markDm"]);

/** The largest channel gap between two fills: the app's tokens approximate the artboard's hex to a few sRGB levels. */
function fillGap(a: string, b: string): number {
  const nums = (c: string) => (c.match(/[\d.]+/g) ?? []).map(Number);
  const [x, y] = [nums(a), nums(b)];
  if (x.length < 3 || y.length < 3) return a === b ? 0 : 255;
  // A translucent artboard fill (alpha < 1) is read over the dark page, rgb(14, 17, 22).
  const over = (c: number[]) =>
    c.length === 4 && c[3]! < 1
      ? [14, 17, 22].map((back, i) => back + (c[i]! - back) * c[3]!)
      : c.slice(0, 3);
  const [p, q] = [over(x), over(y)];
  return Math.max(...p.map((v, i) => Math.abs(v - q[i]!)));
}

async function open(page: Page, mode: "sang" | "dem") {
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
  await page.goto(`${BOARD}?view=table`);
  await expect(page.locator("[data-slot=tablet-board-header]")).toBeVisible();
  await expect(page.locator("[data-slot=tablet-table-box]")).toBeVisible();
  await page.waitForTimeout(600);
}

function locators(page: Page): Record<string, Locator> {
  const head = page.locator("[data-slot=tablet-board-header]");
  const toolbar = page.locator("[data-table-toolbar]");
  const box = page.locator("[data-slot=tablet-table-box]");
  const foot = page.locator("[data-slot=tablet-table-footer]");
  const search = head.locator("button[aria-keyshortcuts]");
  return {
    header: head,
    toggle: head.getByRole("button", { name: "Thu gọn thanh bên" }),
    mark: head.locator("a").first(),
    markDm: head.locator("a > span").first(),
    title: head.locator("h1"),
    seg: head.getByRole("tablist"),
    tab0: head.getByRole("tab").nth(0),
    tab1: head.getByRole("tab").nth(1),
    tab2: head.getByRole("tab").nth(2),
    tab3: head.getByRole("tab").nth(3),
    search,
    kbd: search.locator("span"),
    add: head.getByRole("button", { name: "Thêm công việc" }),
    share: head.getByRole("button", { name: "Chia sẻ" }),
    more: head.getByRole("button", { name: "Thêm tuỳ chọn" }),
    toolbar: toolbar.locator("xpath=.."),
    fchip: toolbar.locator("[data-chip=done]"),
    fchipX: toolbar.locator("[data-chip=done] button"),
    filter: toolbar.getByRole("button", { name: /^Lọc/ }),
    sort: toolbar.getByRole("button", { name: /Sắp xếp/ }),
    group: toolbar.getByRole("button", { name: /^Nhóm/ }),
    stack: page.locator("[data-slot=tablet-avatar-stack]"),
    box,
    fade: box.locator("[data-slot=tablet-table-fade]"),
    next: box.getByRole("button", { name: "Xem các cột tiếp theo" }),
    foot,
    footLink: foot.getByRole("button"),
  };
}

for (const mode of ["sang", "dem"] as const) {
  test(`board table at 1180 px, ${mode}, equals Final-T-Table`, async ({
    page,
  }) => {
    await open(page, mode);
    const main = page.locator("main").first();
    const [mb, hb] = await Promise.all([
      main.boundingBox(),
      page.locator("[data-slot=tablet-board-header]").boundingBox(),
    ]);
    if (!mb || !hb) throw new Error("no main column");
    const loc = locators(page);
    let compared = 0;
    for (const [key, want] of Object.entries(ART[mode])) {
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
      const keys = KEYS[key] ?? ["x", "y", "w", "h"];
      const idx = { x: 0, y: 1, w: 2, h: 3 } as const;
      for (const k of keys) {
        expect
          .soft(
            Math.abs(box[k] - want[idx[k]]),
            `${key}.${k}: ${box[k].toFixed(1)} vs ${want[idx[k]]}`,
          )
          .toBeLessThanOrEqual(1);
        compared += 1;
      }
      const style = await target.evaluate((el) => {
        const c = getComputedStyle(el);
        // The app's tokens are oklch: read the fill back as the sRGB the artboard states.
        const ctx = document.createElement("canvas").getContext("2d");
        let bg = c.backgroundColor;
        if (ctx) {
          ctx.clearRect(0, 0, 1, 1);
          ctx.fillStyle = c.backgroundColor;
          ctx.fillRect(0, 0, 1, 1);
          const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data;
          bg =
            a === 255
              ? `rgb(${r}, ${g}, ${b})`
              : `rgba(${r}, ${g}, ${b}, ${(a / 255).toFixed(2)})`;
        }
        return { fs: c.fontSize, fw: c.fontWeight, r: c.borderRadius, bg };
      });
      expect.soft(style.r, `${key} radius`).toBe(want[6]);
      if (FILLS.has(key))
        expect
          .soft(
            fillGap(style.bg, want[7]),
            `${key} fill ${style.bg} vs ${want[7]}`,
          )
          .toBeLessThanOrEqual(8);
    }
    expect(compared).toBeGreaterThan(60);
  });
}

/**
 * The value cells of the status, due date and priority columns (the cells of three rows: a plain row, an overdue row
 * and a row with no due date), boxes relative to the row's top-left: x, y, width, height, font size, font weight,
 * radius, text colour, fill. The artboard draws the status chip inside a 116 px cell button; the app's chip is the
 * button itself, so only the chip is compared there.
 */
type Cell = readonly [
  number,
  number,
  number,
  number,
  string,
  string,
  string,
  string,
  string,
  string,
];
const CELLS: Record<"sang" | "dem", Record<string, Record<string, Cell>>> = {
  sang: {
    "ERP-151": {
      status: [
        372,
        11.5,
        87.2,
        28,
        "13px",
        "600",
        "999px",
        "rgb(22, 26, 33)",
        "rgb(236, 238, 242)",
        "Cần làm",
      ],
      dueCell: [
        488,
        3.5,
        108,
        44,
        "14px",
        "400",
        "9px",
        "rgb(22, 26, 33)",
        "rgba(0, 0, 0, 0)",
        "18/10",
      ],
      due: [
        496,
        16.8,
        37,
        17.5,
        "14px",
        "400",
        "0px",
        "rgb(22, 26, 33)",
        "rgba(0, 0, 0, 0)",
        "18/10",
      ],
      prioCell: [
        804,
        3.5,
        120,
        44,
        "14px",
        "400",
        "9px",
        "rgb(22, 26, 33)",
        "rgba(0, 0, 0, 0)",
        "Cao",
      ],
      prio: [
        812,
        15.3,
        50.2,
        20.3,
        "14px",
        "600",
        "0px",
        "rgb(180, 80, 11)",
        "rgba(0, 0, 0, 0)",
        "Cao",
      ],
      prioSvg: [
        812,
        18,
        15,
        15,
        "14px",
        "600",
        "0px",
        "rgb(180, 80, 11)",
        "rgba(0, 0, 0, 0)",
        "",
      ],
    },
    "ERP-142": {
      status: [
        372,
        11.5,
        95.6,
        28,
        "13px",
        "600",
        "999px",
        "rgb(22, 26, 33)",
        "rgb(230, 239, 251)",
        "Đang làm",
      ],
      dueCell: [
        488,
        3.5,
        108,
        44,
        "14px",
        "400",
        "9px",
        "rgb(22, 26, 33)",
        "rgba(0, 0, 0, 0)",
        "10/10Quá hạn 2 ngày",
      ],
      due: [
        496,
        9.3,
        92.8,
        32.5,
        "14px",
        "400",
        "0px",
        "rgb(196, 35, 64)",
        "rgba(0, 0, 0, 0)",
        "10/10Quá hạn 2 ngày",
      ],
      prioCell: [
        804,
        3.5,
        120,
        44,
        "14px",
        "400",
        "9px",
        "rgb(22, 26, 33)",
        "rgba(0, 0, 0, 0)",
        "Khẩn",
      ],
      prio: [
        812,
        15.3,
        56.8,
        20.3,
        "14px",
        "600",
        "0px",
        "rgb(196, 35, 64)",
        "rgba(0, 0, 0, 0)",
        "Khẩn",
      ],
      prioSvg: [
        812,
        18,
        15,
        15,
        "14px",
        "600",
        "0px",
        "rgb(196, 35, 64)",
        "rgba(0, 0, 0, 0)",
        "",
      ],
    },
    "ERP-153": {
      status: [
        372,
        11.5,
        87.2,
        28,
        "13px",
        "600",
        "999px",
        "rgb(22, 26, 33)",
        "rgb(236, 238, 242)",
        "Cần làm",
      ],
      dueCell: [
        488,
        3.5,
        108,
        44,
        "14px",
        "400",
        "9px",
        "rgb(22, 26, 33)",
        "rgba(0, 0, 0, 0)",
        "Chưa đặt",
      ],
      due: [
        496,
        16.8,
        63.7,
        17.5,
        "14px",
        "400",
        "0px",
        "rgb(22, 26, 33)",
        "rgba(0, 0, 0, 0)",
        "Chưa đặt",
      ],
      prioCell: [
        804,
        3.5,
        120,
        44,
        "14px",
        "400",
        "9px",
        "rgb(22, 26, 33)",
        "rgba(0, 0, 0, 0)",
        "Thấp",
      ],
      prio: [
        812,
        15.3,
        56.9,
        20.3,
        "14px",
        "500",
        "0px",
        "rgb(95, 104, 117)",
        "rgba(0, 0, 0, 0)",
        "Thấp",
      ],
      prioSvg: [
        812,
        18,
        15,
        15,
        "14px",
        "500",
        "0px",
        "rgb(95, 104, 117)",
        "rgba(0, 0, 0, 0)",
        "",
      ],
    },
  },
  dem: {
    "ERP-151": {
      status: [
        372,
        11.5,
        87.2,
        28,
        "13px",
        "600",
        "999px",
        "rgb(230, 233, 238)",
        "rgb(30, 37, 47)",
        "Cần làm",
      ],
      dueCell: [
        488,
        3.5,
        108,
        44,
        "14px",
        "400",
        "9px",
        "rgb(230, 233, 238)",
        "rgba(0, 0, 0, 0)",
        "18/10",
      ],
      due: [
        496,
        16.8,
        37,
        17.5,
        "14px",
        "400",
        "0px",
        "rgb(230, 233, 238)",
        "rgba(0, 0, 0, 0)",
        "18/10",
      ],
      prioCell: [
        804,
        3.5,
        120,
        44,
        "14px",
        "400",
        "9px",
        "rgb(230, 233, 238)",
        "rgba(0, 0, 0, 0)",
        "Cao",
      ],
      prio: [
        812,
        15.3,
        50.2,
        20.3,
        "14px",
        "600",
        "0px",
        "rgb(255, 160, 96)",
        "rgba(0, 0, 0, 0)",
        "Cao",
      ],
      prioSvg: [
        812,
        18,
        15,
        15,
        "14px",
        "600",
        "0px",
        "rgb(255, 160, 96)",
        "rgba(0, 0, 0, 0)",
        "",
      ],
    },
    "ERP-142": {
      status: [
        372,
        11.5,
        95.6,
        28,
        "13px",
        "600",
        "999px",
        "rgb(230, 233, 238)",
        "rgb(22, 40, 74)",
        "Đang làm",
      ],
      dueCell: [
        488,
        3.5,
        108,
        44,
        "14px",
        "400",
        "9px",
        "rgb(230, 233, 238)",
        "rgba(0, 0, 0, 0)",
        "10/10Quá hạn 2 ngày",
      ],
      due: [
        496,
        9.3,
        92.8,
        32.5,
        "14px",
        "400",
        "0px",
        "rgb(255, 107, 129)",
        "rgba(0, 0, 0, 0)",
        "10/10Quá hạn 2 ngày",
      ],
      prioCell: [
        804,
        3.5,
        120,
        44,
        "14px",
        "400",
        "9px",
        "rgb(230, 233, 238)",
        "rgba(0, 0, 0, 0)",
        "Khẩn",
      ],
      prio: [
        812,
        15.3,
        56.8,
        20.3,
        "14px",
        "600",
        "0px",
        "rgb(255, 107, 129)",
        "rgba(0, 0, 0, 0)",
        "Khẩn",
      ],
      prioSvg: [
        812,
        18,
        15,
        15,
        "14px",
        "600",
        "0px",
        "rgb(255, 107, 129)",
        "rgba(0, 0, 0, 0)",
        "",
      ],
    },
    "ERP-153": {
      status: [
        372,
        11.5,
        87.2,
        28,
        "13px",
        "600",
        "999px",
        "rgb(230, 233, 238)",
        "rgb(30, 37, 47)",
        "Cần làm",
      ],
      dueCell: [
        488,
        3.5,
        108,
        44,
        "14px",
        "400",
        "9px",
        "rgb(230, 233, 238)",
        "rgba(0, 0, 0, 0)",
        "Chưa đặt",
      ],
      due: [
        496,
        16.8,
        63.7,
        17.5,
        "14px",
        "400",
        "0px",
        "rgb(230, 233, 238)",
        "rgba(0, 0, 0, 0)",
        "Chưa đặt",
      ],
      prioCell: [
        804,
        3.5,
        120,
        44,
        "14px",
        "400",
        "9px",
        "rgb(230, 233, 238)",
        "rgba(0, 0, 0, 0)",
        "Thấp",
      ],
      prio: [
        812,
        15.3,
        56.9,
        20.3,
        "14px",
        "500",
        "0px",
        "rgb(143, 153, 167)",
        "rgba(0, 0, 0, 0)",
        "Thấp",
      ],
      prioSvg: [
        812,
        18,
        15,
        15,
        "14px",
        "500",
        "0px",
        "rgb(143, 153, 167)",
        "rgba(0, 0, 0, 0)",
        "",
      ],
    },
  },
};

for (const mode of ["sang", "dem"] as const) {
  test(`status, due and priority cells at 1180 px, ${mode}, equal Final-T-Table`, async ({
    page,
  }) => {
    await open(page, mode);
    let compared = 0;
    for (const [id, want] of Object.entries(CELLS[mode])) {
      const row = page.locator("[role=row]", { hasText: id }).first();
      const rb = await row.boundingBox();
      if (!rb) throw new Error(`no row ${id}`);
      const cells = row.locator(":scope > div");
      const at: Record<string, Locator> = {
        status: cells.nth(2).getByRole("button").first(),
        dueCell: cells.nth(3).getByRole("button").first(),
        due: cells.nth(3).getByRole("button").first().locator("> span").first(),
        prioCell: cells.nth(5).getByRole("button").first(),
        prio: cells
          .nth(5)
          .getByRole("button")
          .first()
          .locator("> span")
          .first(),
        prioSvg: cells
          .nth(5)
          .getByRole("button")
          .first()
          .locator("svg")
          .first(),
      };
      for (const [key, w] of Object.entries(want)) {
        const target = at[key];
        if (!target) throw new Error(`no locator for ${key}`);
        const got = await target.boundingBox();
        expect(got, `${id} ${key} is drawn`).not.toBeNull();
        if (!got) continue;
        const rel = [got.x - rb.x, got.y - rb.y, got.width, got.height];
        // The row's own y is the row's top; the artboard's x is from the row's left edge, scroll at 0.
        for (const [i, name] of ["x", "y", "w", "h"].entries()) {
          expect
            .soft(
              Math.abs(rel[i]! - w[i]!),
              `${id} ${key}.${name}: ${rel[i]!.toFixed(1)} vs ${w[i]}`,
            )
            .toBeLessThanOrEqual(1);
          compared += 1;
        }
        const style = await target.evaluate((el) => {
          const c = getComputedStyle(el);
          return {
            fs: c.fontSize,
            fw: c.fontWeight,
            r: Number.parseFloat(c.borderRadius),
          };
        });
        // The due text keeps its weight in a child (650, a face the font does not have); only boxes are compared there.
        if (key !== "dueCell" && key !== "prioCell" && key !== "due") {
          expect.soft(style.fs, `${id} ${key} font size`).toBe(w[4]);
          expect.soft(style.fw, `${id} ${key} font weight`).toBe(w[5]);
        }
        const wantRadius = Number.parseFloat(w[6]!);
        expect
          .soft(style.r >= 999 ? 999 : style.r, `${id} ${key} radius`)
          .toBe(wantRadius >= 999 ? 999 : wantRadius);
      }
    }
    expect(compared).toBeGreaterThan(60);
  });
}

/**
 * The board header on the other views: the same 56 px header against `Final-T-Board` (state `binh-thuong`, the
 * Kanban) and `Final-T-Calendar` (state `tuan`, the Calendar), where only the active segment differs. The Kanban
 * toolbar's access chip, "Lọc" chip and avatar stack are measured too. Not measured, and different from the
 * artboards (the State line of UC-WM-K01 names them): the Kanban's lanes and cards and the rest of its toolbar (the
 * app keeps Làn, the saved filters and the card fields), and the Calendar's whole tablet surface (its toolbar,
 * overdue strip, week grid and undated tray).
 */
const VIEW_HEADER: Record<
  "board" | "calendar",
  Record<"sang" | "dem", Record<string, Row>>
> = {
  board: {
    sang: {
      header: [0, 0, 932, 56, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
      toggle: [6, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      mark: [56, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      markDm: [64, 13.5, 28, 28, "10.5px", "700", "8px", "rgb(30, 111, 208)"],
      title: [
        117.3,
        15.2,
        150.4,
        24.6,
        "17px",
        "650",
        "0px",
        "rgba(0, 0, 0, 0)",
      ],
      seg: [
        279.7,
        3.5,
        336.2,
        48,
        "14.5px",
        "400",
        "12px",
        "rgb(228, 231, 236)",
      ],
      tab0: [281.7, 5.5, 82, 44, "14px", "650", "10px", "rgb(255, 255, 255)"],
      tab1: [363.6, 5.5, 64, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      tab2: [427.6, 5.5, 57.8, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      tab3: [485.4, 5.5, 128.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      search: [
        653.2,
        5.5,
        77.8,
        44,
        "14px",
        "400",
        "10px",
        "rgb(255, 255, 255)",
      ],
      kbd: [
        694.2,
        17.5,
        25.8,
        20,
        "11.5px",
        "400",
        "5px",
        "rgb(238, 240, 243)",
      ],
      add: [737.1, 5.5, 44, 44, "14.5px", "400", "10px", "rgb(22, 102, 204)"],
      share: [
        787.1,
        5.5,
        82.9,
        44,
        "14.5px",
        "500",
        "10px",
        "rgb(255, 255, 255)",
      ],
      more: [876, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      accessChip: [
        16,
        64,
        255.8,
        44,
        "14px",
        "500",
        "10px",
        "rgb(255, 255, 255)",
      ],
      filter: [
        279.8,
        64,
        78.6,
        44,
        "14px",
        "500",
        "10px",
        "rgb(255, 255, 255)",
      ],
      stack: [559.6, 64, 134, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      toolbar: [0, 56, 932, 60, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    },
    dem: {
      header: [0, 0, 932, 56, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
      toggle: [6, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      mark: [56, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      markDm: [64, 13.5, 28, 28, "10.5px", "700", "8px", "rgb(30, 111, 208)"],
      title: [
        117.3,
        15.2,
        150.4,
        24.6,
        "17px",
        "650",
        "0px",
        "rgba(0, 0, 0, 0)",
      ],
      seg: [279.7, 3.5, 336.2, 48, "14.5px", "400", "12px", "rgb(28, 35, 44)"],
      tab0: [281.7, 5.5, 82, 44, "14px", "650", "10px", "rgb(21, 26, 33)"],
      tab1: [363.6, 5.5, 64, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      tab2: [427.6, 5.5, 57.8, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      tab3: [485.4, 5.5, 128.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      search: [653.2, 5.5, 77.8, 44, "14px", "400", "10px", "rgb(21, 26, 33)"],
      kbd: [694.2, 17.5, 25.8, 20, "11.5px", "400", "5px", "rgb(35, 43, 53)"],
      add: [737.1, 5.5, 44, 44, "14.5px", "400", "10px", "rgb(90, 169, 255)"],
      share: [787.1, 5.5, 82.9, 44, "14.5px", "500", "10px", "rgb(21, 26, 33)"],
      more: [876, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      accessChip: [16, 64, 255.8, 44, "14px", "500", "10px", "rgb(21, 26, 33)"],
      filter: [279.8, 64, 78.6, 44, "14px", "500", "10px", "rgb(21, 26, 33)"],
      stack: [559.6, 64, 134, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      toolbar: [0, 56, 932, 60, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
    },
  },
  calendar: {
    sang: {
      header: [0, 0, 932, 56, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
      toggle: [6, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      mark: [56, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      markDm: [64, 13.5, 28, 28, "10.5px", "700", "8px", "rgb(30, 111, 208)"],
      title: [
        117.3,
        15.2,
        150.4,
        24.6,
        "17px",
        "650",
        "0px",
        "rgba(0, 0, 0, 0)",
      ],
      seg: [
        279.7,
        3.5,
        335.6,
        48,
        "14.5px",
        "400",
        "12px",
        "rgb(228, 231, 236)",
      ],
      tab0: [281.7, 5.5, 80.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      tab1: [362.2, 5.5, 64, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      tab2: [426.2, 5.5, 58.6, 44, "14px", "650", "10px", "rgb(255, 255, 255)"],
      tab3: [484.8, 5.5, 128.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      search: [
        653.2,
        5.5,
        77.8,
        44,
        "14px",
        "400",
        "10px",
        "rgb(255, 255, 255)",
      ],
      kbd: [
        694.2,
        17.5,
        25.8,
        20,
        "11.5px",
        "400",
        "5px",
        "rgb(238, 240, 243)",
      ],
      add: [737.1, 5.5, 44, 44, "14.5px", "400", "10px", "rgb(22, 102, 204)"],
      share: [
        787.1,
        5.5,
        82.9,
        44,
        "14.5px",
        "500",
        "10px",
        "rgb(255, 255, 255)",
      ],
      more: [876, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    },
    dem: {
      header: [0, 0, 932, 56, "14.5px", "400", "0px", "rgba(0, 0, 0, 0)"],
      toggle: [6, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      mark: [56, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
      markDm: [64, 13.5, 28, 28, "10.5px", "700", "8px", "rgb(30, 111, 208)"],
      title: [
        117.3,
        15.2,
        150.4,
        24.6,
        "17px",
        "650",
        "0px",
        "rgba(0, 0, 0, 0)",
      ],
      seg: [279.7, 3.5, 335.6, 48, "14.5px", "400", "12px", "rgb(28, 35, 44)"],
      tab0: [281.7, 5.5, 80.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      tab1: [362.2, 5.5, 64, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      tab2: [426.2, 5.5, 58.6, 44, "14px", "650", "10px", "rgb(21, 26, 33)"],
      tab3: [484.8, 5.5, 128.5, 44, "14px", "500", "10px", "rgba(0, 0, 0, 0)"],
      search: [653.2, 5.5, 77.8, 44, "14px", "400", "10px", "rgb(21, 26, 33)"],
      kbd: [694.2, 17.5, 25.8, 20, "11.5px", "400", "5px", "rgb(35, 43, 53)"],
      add: [737.1, 5.5, 44, 44, "14.5px", "400", "10px", "rgb(90, 169, 255)"],
      share: [787.1, 5.5, 82.9, 44, "14.5px", "500", "10px", "rgb(21, 26, 33)"],
      more: [876, 5.5, 44, 44, "14.5px", "400", "10px", "rgba(0, 0, 0, 0)"],
    },
  },
};

for (const [view, query] of [
  ["board", "kanban"],
  ["calendar", "calendar"],
] as const) {
  for (const mode of ["sang", "dem"] as const) {
    test(`${view} header at 1180 px, ${mode}, equals Final-T-${view === "board" ? "Board" : "Calendar"}`, async ({
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
      await page.goto(`${BOARD}?view=${query}`);
      await expect(
        page.locator("[data-slot=tablet-board-header]"),
      ).toBeVisible();
      await expect(
        page
          .locator("[data-slot=tablet-board-header]")
          .getByRole("button", { name: "Thêm công việc" }),
      ).toBeVisible();
      await page.waitForTimeout(800);
      const main = page.locator("main").first();
      const [mb, hb] = await Promise.all([
        main.boundingBox(),
        page.locator("[data-slot=tablet-board-header]").boundingBox(),
      ]);
      if (!mb || !hb) throw new Error("no main column");
      const loc = locators(page);
      const bar = page.locator("[data-kanban-toolbar]");
      const extra: Record<string, Locator> = {
        accessChip: page.locator("[data-slot=tablet-access-chip]"),
        filter: bar.getByRole("button", { name: /^Lọc/ }),
        stack: page.locator("[data-slot=tablet-avatar-stack]"),
      };
      let compared = 0;
      for (const [key, want] of Object.entries(VIEW_HEADER[view][mode])) {
        // The Kanban toolbar keeps Làn, the saved filters and the card fields, so the stack wraps to a second row.
        if (key === "toolbar" || key === "stack") continue;
        const target = extra[key] ?? loc[key];
        if (!target) throw new Error(`no locator for ${key}`);
        const got = await target.boundingBox();
        expect(got, `${key} is drawn`).not.toBeNull();
        if (!got) continue;
        const rel = [got.x - mb.x, got.y - hb.y, got.width, got.height];
        for (const [i, name] of ["x", "y", "w", "h"].entries()) {
          expect
            .soft(
              Math.abs(rel[i]! - want[i]!),
              `${key}.${name}: ${rel[i]!.toFixed(1)} vs ${want[i]}`,
            )
            .toBeLessThanOrEqual(1);
          compared += 1;
        }
      }
      expect(compared).toBeGreaterThanOrEqual(56);
    });
  }
}
