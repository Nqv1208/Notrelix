import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The Links section of the card popup against `Kanban-CardPopup-Links` (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), frame 1
 * "Liên kết và phụ thuộc" (popup of ERP-144), light and dark. Only the section is measured, not the popup: the heading
 * with "Gỡ tất cả liên kết", five groups (Chặn, Bị chặn bởi, Liên quan, Việc cha, Việc con) with their rows (key, title,
 * status with its dot, the first assignee's mark, the unlink button) and "Thêm liên kết".
 *
 * Every number was read from the artboard rendered with `render.py` (box relative to the section's top-left, then font
 * size, weight, radius and colour). Tolerance is 1 px for boxes; font sizes and weights are equal; colours differ by at
 * most 14 per channel. The mock world gets ERP-148, ERP-139, ERP-120, ERP-155 and ERP-156 only through
 * `__mockLinksSet`, so every other spec keeps its counts.
 *
 * Deliberate difference: the artboard draws an unlink button on the parent and sub-item rows. Detaching a sub-item has
 * no command (it changes the item hierarchy, a Domain decision), so those two kinds draw no button and their status and
 * mark sit 42 px further right.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}
const near = (actual: number, expected: number, what: string, tol = TOL) =>
  expect(
    Math.abs(actual - expected),
    `${what}: ${actual} vs ${expected}`,
  ).toBeLessThanOrEqual(tol);

async function box(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}
function expectBox(actual: Box, expected: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const)
    if (expected[k] !== undefined)
      near(actual[k], expected[k]!, `${what}.${k}`);
}
async function style(el: Locator) {
  return el.evaluate((node) => {
    const cs = getComputedStyle(node);
    const rgb = (value: string) => {
      const c = document.createElement("canvas").getContext("2d")!;
      c.fillStyle = "#000";
      c.fillRect(0, 0, 1, 1);
      c.fillStyle = value;
      c.fillRect(0, 0, 1, 1);
      const [r, g, b] = c.getImageData(0, 0, 1, 1).data;
      return [r, g, b];
    };
    return {
      fs: cs.fontSize,
      fw: cs.fontWeight,
      r: cs.borderTopLeftRadius,
      c: rgb(cs.color),
      bg: rgb(cs.backgroundColor),
      bd: rgb(cs.borderTopColor),
    };
  });
}
function expectColor(
  actual: number[],
  expected: readonly number[],
  what: string,
  tol = 14,
) {
  for (let i = 0; i < 3; i += 1)
    near(actual[i]!, expected[i]!, `${what} ${"rgb"[i]}`, tol);
}

const COLORS = {
  light: {
    text: [22, 26, 33],
    cap: [79, 88, 102],
    meta: [95, 104, 117],
    row: [255, 255, 255],
    rowBorder: [225, 229, 234],
    btnBorder: [210, 215, 222],
  },
  dark: {
    text: [230, 233, 238],
    cap: [170, 179, 192],
    meta: [143, 153, 167],
    row: [26, 32, 40],
    rowBorder: [35, 43, 53],
    btnBorder: [46, 55, 67],
  },
} as const;

/** One group: caption y, hint, and its rows (key, title, status, mark initials, y, x of status and mark, unlink?). */
const GROUPS = [
  {
    title: "Chặn",
    hint: "Thẻ này đang chặn",
    y: 62,
    rows: [
      {
        key: "ERP-148",
        title: "Chạy thử công nợ tháng 9",
        status: "Chờ duyệt",
        mark: "MQ",
        y: 86.1,
        sx: 538.5,
        sw: 78.5,
      },
    ],
  },
  {
    title: "Bị chặn bởi",
    hint: "Cần xong trước",
    y: 136.1,
    rows: [
      {
        key: "ERP-139",
        title: "Cấu hình quyền truy cập theo phòng ban",
        status: "Đang làm",
        mark: "NA",
        y: 160.3,
        sx: 543.5,
        sw: 73.5,
      },
    ],
  },
  {
    title: "Liên quan",
    hint: null,
    y: 210.3,
    rows: [
      {
        key: "ERP-131",
        title: "Sao lưu cơ sở dữ liệu tự động hằng đêm",
        status: "Hoàn thành",
        mark: "VT",
        y: 234.4,
        sx: 531.7,
        sw: 85.3,
      },
    ],
  },
  {
    title: "Việc cha",
    hint: null,
    y: 284.4,
    rows: [
      {
        key: "ERP-120",
        title: "Triển khai phân hệ Kế toán",
        status: "Đang làm",
        mark: "MQ",
        y: 308.5,
        sx: 585.5,
        sw: 73.5,
        bare: true,
      },
    ],
  },
  {
    title: "Việc con",
    hint: null,
    y: 358.5,
    rows: [
      {
        key: "ERP-155",
        title: "Kiểm thử công nợ phải thu",
        status: "Cần làm",
        mark: "HP",
        y: 382.6,
        sx: 593.6,
        sw: 65.4,
        bare: true,
      },
      {
        key: "ERP-156",
        title: "Kiểm thử công nợ phải trả",
        status: "Cần làm",
        mark: "KL",
        y: 426.6,
        sx: 593.6,
        sw: 65.4,
        bare: true,
      },
    ],
  },
] as const;

async function open(page: Page, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    (mode) => {
      (globalThis as Record<string, unknown>).__mockLinksSet = true;
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(`${BOARD}?item=mock-erp-item-144`);
  const heading = page.getByRole("heading", { name: "Liên kết và phụ thuộc" });
  await expect(heading).toBeVisible();
  await expect(page.getByText("ERP-156", { exact: true })).toBeVisible();
  await page.waitForTimeout(400);
  return heading.locator("xpath=ancestor::section[1]");
}

test.beforeEach(() => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      latency === "slow",
    "Needs the default owner world",
  );
});

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test(`1440 ${mode}: the Links section as drawn, five groups with their rows`, async ({
    page,
  }) => {
    const section = await open(page, dark);
    const c = COLORS[mode];
    const sb = await section.boundingBox();
    near(sb!.width, 720, "section width");
    near(sb!.height, 506.6, "section height");

    const h3 = section.getByRole("heading", { name: "Liên kết và phụ thuộc" });
    expectBox(
      await box(h3, section),
      { x: 60, y: 25.1, w: 499.8, h: 21.8 },
      "heading",
    );
    const hs = await style(h3);
    expect([hs.fs, hs.fw]).toEqual(["15px", "650"]);
    expectColor(hs.c, c.text, "heading colour");
    const clear = section.getByRole("button", { name: "Gỡ tất cả liên kết" });
    expectBox(
      await box(clear, section),
      { x: 567.8, y: 22, w: 128.2, h: 28 },
      "clear button",
    );
    const cs = await style(clear);
    expect([cs.fs, cs.fw, cs.r]).toEqual(["12.5px", "500", "8px"]);
    expectColor(cs.bd, c.btnBorder, "clear border");

    for (const group of GROUPS) {
      const cap = section.locator(
        `xpath=.//div[normalize-space(text())='${group.title}']`,
      );
      expectBox(
        await box(cap, section),
        { x: 60, y: group.y, w: 636, h: 18.1 },
        `${group.title} caption`,
      );
      const caps = await style(cap);
      expect([caps.fs, caps.fw]).toEqual(["12.5px", "600"]);
      expectColor(caps.c, c.cap, `${group.title} colour`);
      if (group.hint) await expect(cap).toContainText(group.hint);

      for (const row of group.rows) {
        const key = section.getByText(row.key, { exact: true });
        const line = key.locator("xpath=..");
        expectBox(
          await box(line, section),
          { x: 60, y: row.y, w: 636, h: 40 },
          `${row.key} row`,
        );
        const rs = await style(line);
        expect([rs.fs, rs.r]).toEqual(["14px", "8px"]);
        expectColor(rs.bg, c.row, `${row.key} bg`);
        expectColor(rs.bd, c.rowBorder, `${row.key} border`);
        const kb = await box(key, section);
        near(kb.x, 73, `${row.key} key x`);
        near(kb.w, 50.4, `${row.key} key w`);
        const ks = await style(key);
        expect([ks.fs, ks.fw]).toEqual(["12px", "400"]);
        expectColor(ks.c, c.meta, `${row.key} key colour`);
        const title = line.getByText(row.title, { exact: true });
        near((await box(title, section)).x, 133.4, `${row.key} title x`);
        const ts = await style(title);
        expect([ts.fs, ts.fw]).toEqual(["14px", "400"]);
        expectColor(ts.c, c.text, `${row.key} title colour`);
        const status = line.getByText(row.status, { exact: true });
        const sbx = await box(status, section);
        near(sbx.x, row.sx, `${row.key} status x`);
        near(sbx.w, row.sw, `${row.key} status w`);
        const sts = await style(status);
        expect([sts.fs, sts.fw]).toEqual(["12.5px", "400"]);
        expectColor(sts.c, c.cap, `${row.key} status colour`);
        const dot = status.locator("> span").first();
        expectBox(
          await box(dot, section),
          { x: row.sx, w: 9, h: 9 },
          `${row.key} dot`,
        );
        const mark = line.getByRole("img").first();
        expect(await mark.textContent()).toBe(row.mark);
        const mx = "bare" in row ? 669 : 627;
        expectBox(
          await box(mark, section),
          { x: mx, y: row.y + 10, w: 20, h: 20 },
          `${row.key} mark`,
        );
        const unlink = line.getByRole("button", {
          name: `Gỡ liên kết ${row.key}`,
        });
        if ("bare" in row) await expect(unlink).toHaveCount(0);
        else {
          expectBox(
            await box(unlink, section),
            { x: 657, y: row.y + 4, w: 32, h: 32 },
            `${row.key} unlink`,
          );
          expect((await style(unlink)).r).toBe("8px");
        }
      }
    }

    const add = section.getByRole("button", { name: "Thêm liên kết" });
    expectBox(
      await box(add, section),
      { x: 60, y: 478.6, w: 123, h: 28 },
      "add button",
    );
    const as = await style(add);
    expect([as.fs, as.fw, as.r]).toEqual(["12.5px", "500", "8px"]);
  });

  test(`1440 ${mode}: the popup with the Links section has no axe violations`, async ({
    page,
  }) => {
    const section = await open(page, dark);
    await expect(section).toBeVisible();
    await section.evaluate((node) => node.setAttribute("data-probe", "links"));
    const results = await new AxeBuilder({ page })
      .include("[data-probe=links]")
      .analyze();
    expect(results.violations).toEqual([]);
  });
}
