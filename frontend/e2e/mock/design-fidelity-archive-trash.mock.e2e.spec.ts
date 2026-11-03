import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The Archived items and Deleted items panels against the October 2026 canvas (owner artifact
 * 18L4jUXGfdXh7Wn9YQvA2x), artboard `BC-Archive-Trash`, light and dark. Every number is read from the artboard rendered
 * in Chromium (box relative to the frame's panel, then font size, weight, radius and colour). Tolerance is 1 px for
 * boxes; font sizes, weights and radii are equal; colours differ by at most 10 per channel.
 *
 * Frames measured (the mock world's ERP board has one archived item, ERP-118, and one deleted item, ERP-121 that
 * stands for a sub-item whose parent is still deleted):
 * - `ar-R`, `ar-E`, `ar-find`: the archive with a row, with nothing, and a search with no match;
 * - `ar-L` (`VITE_MOCK_LATENCY=slow`) and `ar-Er` (`VITE_MOCK_FAULTS=/boards/mock-board-erp/items/archived=500`);
 * - `tr-R`, `tr-refuse`: the Deleted items row and the refusal under it;
 * - `ar320-R`, `ar320-E`, `ar320-Er`: the same at 320 px (full screen, the button under the line, 44 px search).
 *
 * The canvas rows hold 3 items and a count of 128; the world has 1 item and a count of 1, so rows are measured on
 * their own boxes (paddings, key, title, button) and not on the list as a whole. The 320 empty body on the canvas is
 * one line ("Việc bạn lưu trữ sẽ nằm ở đây."): the app keeps one copy at every width, so only the icon and the title
 * are held there.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

const near = (actual: number, want: number, what: string) =>
  expect(
    Math.abs(actual - want),
    `${what}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);

async function box(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

function expectBox(actual: Box, want: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const)
    if (want[k] !== undefined) near(actual[k], want[k]!, `${what}.${k}`);
}

async function css(el: Locator, property: string): Promise<string> {
  return el.evaluate(
    (node, name) => getComputedStyle(node).getPropertyValue(name).trim(),
    property,
  );
}

async function rgb(el: Locator, property: string): Promise<number[]> {
  return el.evaluate((node, name) => {
    const c = document.createElement("canvas").getContext("2d", {
      willReadFrequently: true,
    })!;
    c.fillStyle = "#000";
    c.fillStyle = getComputedStyle(node).getPropertyValue(name);
    c.fillRect(0, 0, 1, 1);
    return [...c.getImageData(0, 0, 1, 1).data].slice(0, 3);
  }, property);
}

async function expectColour(
  el: Locator,
  property: string,
  want: readonly [number, number, number],
  what: string,
) {
  const got = await rgb(el, property);
  want.forEach((channel, i) =>
    expect(
      Math.abs(got[i]! - channel),
      `${what} ${property}: ${got.join(",")} vs ${want.join(",")}`,
    ).toBeLessThanOrEqual(10),
  );
}

type Ink = {
  text: readonly [number, number, number];
  key: readonly [number, number, number];
  meta: readonly [number, number, number];
  surface: readonly [number, number, number];
  danger: readonly [number, number, number];
  dangerSurface: readonly [number, number, number];
  chip: readonly [number, number, number];
  chipText: readonly [number, number, number];
  skeleton: readonly [number, number, number];
};
const INK: Record<"light" | "dark", Ink> = {
  light: {
    text: [22, 26, 33],
    key: [95, 104, 117],
    meta: [79, 88, 102],
    surface: [255, 255, 255],
    danger: [196, 35, 64],
    dangerSurface: [253, 236, 239],
    chip: [238, 240, 244],
    chipText: [62, 71, 87],
    skeleton: [230, 233, 238],
  },
  dark: {
    text: [230, 233, 238],
    key: [143, 153, 167],
    meta: [170, 179, 192],
    surface: [26, 32, 40],
    danger: [255, 107, 129],
    dangerSurface: [58, 43, 53],
    chip: [35, 43, 53],
    chipText: [195, 203, 214],
    skeleton: [35, 42, 52],
  },
};

async function open(
  page: Page,
  which: "archive" | "trash",
  dark: boolean,
): Promise<Locator> {
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
  await page.goto(`${BOARD}?view=table`);
  await page.getByRole("button", { name: "Thêm tuỳ chọn" }).click();
  await page
    .getByRole("menuitem", {
      name: which === "archive" ? "Việc đã lưu trữ" : "Việc đã xoá",
    })
    .click();
  const panel = page.getByRole("dialog", {
    name: which === "archive" ? /^Việc đã lưu trữ/ : /^Việc đã xoá/,
  });
  await expect(panel).toBeVisible();
  // The panel slides in (200 ms): measure it at rest.
  await page.waitForTimeout(500);
  await page.mouse.move(2, 2);
  return panel;
}

/** The header, which every frame shares: title 17/650, the 32 px (44 on a phone) close button. */
async function expectHeader(panel: Locator, narrow: boolean, ink: Ink) {
  const title = panel.getByRole("heading").first();
  const t = await box(title, panel);
  near(t.x, 17, "title.x");
  near(t.h, 24.6, "title.h");
  expect(await css(title, "font-size")).toBe("17px");
  expect(await css(title, "font-weight")).toBe("650");
  const close = panel.getByRole("button", { name: "Đóng" });
  const c = await box(close, panel);
  if (narrow) {
    near(c.w, 44, "close.w");
    near(c.h, 44, "close.h");
  } else {
    expectBox(c, { x: 400, y: 12, w: 32, h: 32 }, "close");
  }
  await expectColour(title, "color", ink.text, "title");
}

async function expectSearch(panel: Locator, narrow: boolean, ink: Ink) {
  const label = panel.locator("label").first();
  const s = await box(label, panel);
  if (narrow) {
    near(s.x, 16, "search.x");
    near(s.w, 286, "search.w");
    near(s.h, 44, "search.h");
  } else {
    expectBox(s, { x: 17, y: 56, w: 407, h: 36 }, "search");
  }
  expect(await css(label, "border-radius")).toBe("7px");
  expect(await css(label, "border-top-width")).toBe("1px");
  expect(await css(label, "padding-left")).toBe("10px");
  await expectColour(label, "background-color", ink.surface, "search");
}

async function expectRow(
  panel: Locator,
  row: Locator,
  button: string,
  narrow: boolean,
  ink: Ink,
  top: number | null,
) {
  const r = await box(row, panel);
  const w = narrow ? 318 : 439;
  near(r.w, w, "row.w");
  if (top !== null) near(r.y, top, "row.y");
  const key = row.locator("span.font-mono").first();
  const k = await box(key, row);
  near(k.x, 16, "key.x");
  expect(await css(key, "font-size")).toBe("11.5px");
  await expectColour(key, "color", ink.key, "key");
  const title = row.locator("span.font-medium").first();
  expect(await css(title, "font-size")).toBe("14px");
  expect(await css(title, "font-weight")).toBe("500");
  await expectColour(title, "color", ink.text, "name");
  const meta = row.locator("span.text-\\[12\\.5px\\]").first();
  expect(await css(meta, "font-size")).toBe("12.5px");
  await expectColour(meta, "color", ink.meta, "meta");
  const action = row.getByRole("button", { name: button });
  const a = await box(action, row);
  near(a.h, 44, "button.h");
  expect(await css(action, "border-radius")).toBe("7px");
  expect(await css(action, "font-size")).toBe(narrow ? "14px" : "13px");
  expect(await css(action, "font-weight")).toBe("500");
  await expectColour(action, "background-color", ink.surface, "button");
  if (narrow) {
    // Under the line: left edge on the 16 px gutter, 6 px under the meta line, 12 px of padding below.
    near(a.x, 16, "button.x");
    const m = await box(meta, row);
    near(a.y - (m.y + m.h), 6, "button gap under the line");
    near(r.h - (a.y + a.h), 12, "row padding under the button");
  } else {
    // Beside the text, on the right gutter, centred on the row.
    near(a.x + a.w, 423, "button right edge");
    near(a.y + a.h / 2, r.h / 2, "button centre");
    near(k.y, 10, "key.y");
  }
}

test.describe("Archived items and Deleted items panels at 1440 px", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      latency !== "" ||
      overlays !== "",
    "Needs the plain owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    const mode = dark ? "dem" : "sang";
    const ink = INK[dark ? "dark" : "light"];

    test(`the archive: header, search, row and button (ar-R-${mode})`, async ({
      page,
    }) => {
      const panel = await open(page, "archive", dark);
      const p = await panel.boundingBox();
      near(p!.width, 440, "panel.w");
      await expectHeader(panel, false, ink);
      const chip = panel.getByRole("heading").first().locator("span");
      near((await box(chip, panel)).h, 22, "chip.h");
      expect(await css(chip, "font-size")).toBe("12px");
      expect(await css(chip, "font-weight")).toBe("600");
      expect(await css(chip, "border-radius")).toBe("6px");
      await expectColour(chip, "background-color", ink.chip, "chip");
      await expectSearch(panel, false, ink);
      const row = panel.getByRole("list").getByRole("listitem").first();
      await expectRow(panel, row, "Bỏ lưu trữ", false, ink, 100);
      near((await box(row, panel)).h, 78.3, "row.h (one-line name)");
      const button = row.getByRole("button", { name: "Bỏ lưu trữ" });
      near((await box(button, row)).w, 112.5, "button.w");
    });

    test(`the archive with nothing in it: no search, the icon, the title and the line (ar-E-${mode})`, async ({
      page,
    }) => {
      const panel = await open(page, "archive", dark);
      await panel.getByRole("button", { name: "Bỏ lưu trữ" }).click();
      const title = panel.getByText("Chưa có việc nào được lưu trữ");
      await expect(title).toBeVisible();
      await expectHeader(panel, false, ink);
      await expect(panel.locator("label")).toHaveCount(0);
      const holder = title.locator("..");
      const h = await box(holder, panel);
      expectBox(h, { x: 1, y: 48, w: 439, h: 212.9 }, "empty");
      expect(await css(holder, "padding")).toBe("48px 24px");
      const icon = holder.locator("span").first();
      expectBox(
        await box(icon, panel),
        { x: 198.5, y: 96, w: 44, h: 44 },
        "icon",
      );
      expect(await css(icon, "border-radius")).toBe("12px");
      await expectColour(icon, "background-color", ink.skeleton, "icon");
      const t = await box(title, panel);
      near(t.y, 146, "empty title.y");
      expect(await css(title, "font-weight")).toBe("700");
      const body = holder.locator("span").nth(1);
      near((await box(body, panel)).y, 172.3, "empty body.y");
      near((await box(body, panel)).h, 40.6, "empty body.h (two lines)");
    });

    test(`a search with no match: the box stays, the empty state names the text (ar-find-${mode})`, async ({
      page,
    }) => {
      const panel = await open(page, "archive", dark);
      await panel.getByRole("searchbox").fill("kho lạnh");
      const none = panel.getByText("Không có kết quả");
      await expect(none).toBeVisible();
      await expectSearch(panel, false, ink);
      const holder = none.locator("..");
      expectBox(
        await box(holder, panel),
        { x: 1, y: 100, w: 439, h: 192.6 },
        "empty",
      );
      expectBox(
        await box(holder.locator("span").first(), panel),
        { x: 198.5, y: 148, w: 44, h: 44 },
        "icon",
      );
      near((await box(none, panel)).y, 198, "title.y");
      await expectColour(none, "color", ink.text, "title");
    });

    test(`the Deleted items row: the count chip, who and when, and Khôi phục (tr-R-${mode})`, async ({
      page,
    }) => {
      const panel = await open(page, "trash", dark);
      await expectHeader(panel, false, ink);
      await expectSearch(panel, false, ink);
      const row = panel.getByRole("list").getByRole("listitem").first();
      await expectRow(panel, row, "Khôi phục", false, ink, 100);
      const button = row.getByRole("button", { name: "Khôi phục" });
      near((await box(button, row)).w, 115.7, "button.w");
    });

    test(`a refused restore tells why under the line, in red, with a 13 px icon (tr-refuse-${mode})`, async ({
      page,
    }) => {
      const panel = await open(page, "trash", dark);
      await panel.getByRole("button", { name: "Khôi phục" }).click();
      const why = panel.getByRole("alert");
      await expect(why).toContainText("Chưa khôi phục được");
      const row = panel.getByRole("list").getByRole("listitem").first();
      const line = row
        .locator("span.text-\\[12\\.5px\\]")
        .first()
        .locator("..");
      const n = await box(line, row);
      const w = await box(why, row);
      near(w.x, 16, "refusal.x");
      near(w.y - (n.y + n.h), 4, "refusal gap under the who-and-when line");
      expect(await css(why, "font-size")).toBe("12.5px");
      expect(await css(why, "font-weight")).toBe("500");
      await expectColour(why, "color", ink.danger, "refusal");
      const icon = why.locator("svg");
      near((await box(icon, row)).w, 13, "refusal icon");
      // Lines at 1.45 like the canvas (the canvas names the parent key, so its text runs a line longer).
      expect(await css(why, "line-height")).toBe("18.125px");
    });
  }
});

test.describe("The archive failing to load at 1440 px", () => {
  test.skip(
    faults !== "/boards/mock-board-erp/items/archived=500" ||
      persona !== "owner",
    "Needs VITE_MOCK_FAULTS=/boards/mock-board-erp/items/archived=500",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`the red banner and Thử lại (ar-Er-${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const ink = INK[dark ? "dark" : "light"];
      const panel = await open(page, "archive", dark);
      const banner = panel.getByRole("alert");
      await expect(banner).toBeVisible();
      await expectSearch(panel, false, ink);
      expectBox(
        await box(banner, panel),
        { x: 17, y: 116, w: 407, h: 64 },
        "banner",
      );
      expect(await css(banner, "border-radius")).toBe("10px");
      expect(await css(banner, "padding")).toBe("10px 14px");
      expect(await css(banner, "gap")).toBe("10px");
      expect(await css(banner, "font-size")).toBe("13px");
      if (dark) {
        // A 14 % wash of the danger colour over the panel, as the canvas draws it.
        await expectColour(
          banner,
          "background-color",
          ink.danger,
          "banner wash",
        );
        const alpha = await banner.evaluate((node) => {
          const c = document
            .createElement("canvas")
            .getContext("2d", { willReadFrequently: true })!;
          c.fillStyle = "#000";
          c.fillStyle = getComputedStyle(node).backgroundColor;
          c.fillRect(0, 0, 1, 1);
          return c.getImageData(0, 0, 1, 1).data[3]! / 255;
        });
        expect(Math.abs(alpha - 0.14), `wash alpha ${alpha}`).toBeLessThan(
          0.02,
        );
      } else {
        await expectColour(
          banner,
          "background-color",
          ink.dangerSurface,
          "banner",
        );
      }
      const retry = banner.getByRole("button", { name: "Thử lại" });
      expectBox(
        await box(retry, panel),
        { x: 337.4, y: 126, w: 72.6, h: 44 },
        "retry",
      );
    });
  }
});

test.describe("The archive loading at 1440 px", () => {
  test.skip(
    latency !== "slow" || persona !== "owner" || faults !== "",
    "Needs VITE_MOCK_LATENCY=slow",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`three skeleton rows, 73 px each (ar-L-${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const ink = INK[dark ? "dark" : "light"];
      const panel = await open(page, "archive", dark);
      const loading = panel.getByRole("status", { name: /Đang tải/ });
      await expect(loading).toBeVisible();
      await expectSearch(panel, false, ink);
      const rows = loading.locator("> div");
      await expect(rows).toHaveCount(3);
      for (const [i, top] of [100, 173, 246].entries()) {
        const r = await box(rows.nth(i), panel);
        expectBox(r, { x: 1, y: top, w: 439, h: 73 }, `skeleton row ${i}`);
        const bars = rows.nth(i).locator("span");
        const heights = [10, 12, 10];
        for (const [j, height] of heights.entries())
          near((await box(bars.nth(j), panel)).h, height, `bar ${j}`);
        near((await box(bars.nth(0), panel)).w, 60, "bar 0 width");
        near((await box(bars.nth(1), panel)).w, 284.9, "bar 1 width");
        expect(await css(bars.nth(0), "border-radius")).toBe("4px");
        await expectColour(
          bars.nth(0),
          "background-color",
          ink.skeleton,
          "bar",
        );
      }
    });
  }
});

test.describe("The archive of 128 items, read 50 at a time", () => {
  test.skip(
    overlays !== "many-cards" ||
      persona !== "owner" ||
      faults !== "" ||
      latency !== "",
    "Needs VITE_MOCK_OVERLAYS=many-cards",
  );

  for (const [width, label, narrow] of [
    [1440, "ar-R", false],
    [320, "ar320-R", true],
  ] as const) {
    for (const dark of [false, true]) {
      test(`"Hiện thêm 50 việc" and "Đã hiện 50 trên 128" under the rows at ${width} px (${label}-${dark ? "dem" : "sang"})`, async ({
        page,
      }) => {
        await page.setViewportSize({
          width,
          height: width === 320 ? 640 : 900,
        });
        const ink = INK[dark ? "dark" : "light"];
        const panel = await open(page, "archive", dark);
        await expect(panel.getByRole("heading").first()).toContainText("128");
        await expect(panel.getByRole("list").getByRole("listitem")).toHaveCount(
          50,
        );
        const more = panel.getByRole("button", { name: "Hiện thêm 50 việc" });
        await more.scrollIntoViewIfNeeded();
        const wrap = more.locator("..");
        const list = panel.getByRole("list");
        const l = await box(list, panel);
        const w = await box(wrap, panel);
        near(w.y, l.y + l.h, "pager sits under the last row");
        near(w.h, 68, "pager.h");
        expect(await css(wrap, "padding")).toBe("12px 16px");
        const b = await box(more, panel);
        near(b.w, narrow ? 153.7 : 144.9, "more.w");
        near(b.h, 44, "more.h");
        near(b.x + b.w / 2, (narrow ? 320 : 440) / 2 + 0.5, "more is centred");
        near(b.y - w.y, 12, "more.y in its box");
        expect(await css(more, "font-size")).toBe(narrow ? "14px" : "13px");
        await expectColour(more, "background-color", ink.surface, "more");
        const hint = panel.getByText("Đã hiện 50 trên 128");
        const h = await box(hint, panel);
        near(h.y, w.y + w.h, "hint under the pager");
        near(h.h, 27.4, "hint.h");
        expect(await css(hint, "font-size")).toBe("12px");
        expect(await css(hint, "text-align")).toBe("center");
        await expectColour(hint, "color", ink.key, "hint");
      });
    }
  }
});

test.describe("A row being restored at 1440 px", () => {
  test.skip(
    latency !== "slow" || persona !== "owner" || faults !== "",
    "Needs VITE_MOCK_LATENCY=slow",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`the button greys out to a 32 px "Đang xử lý…" with a spinner (ar-S-${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const panel = await open(page, "archive", dark);
      await panel.getByRole("button", { name: "Bỏ lưu trữ" }).click();
      const busy = panel.getByRole("button", { name: "Đang xử lý…" });
      await expect(busy).toBeVisible();
      const b = await box(busy, panel);
      near(b.x, 299.1, "busy.x");
      near(b.w, 124.9, "busy.w");
      near(b.h, 32, "busy.h");
      near(b.x + b.w, 424, "busy right edge");
      expect(await css(busy, "padding-left")).toBe("12px");
      expect(await busy.getAttribute("aria-disabled")).toBe("true");
      await expectColour(
        busy,
        "background-color",
        dark ? [35, 42, 52] : [230, 233, 238],
        "busy",
      );
      await expectColour(
        busy,
        "color",
        dark ? [143, 153, 167] : [95, 104, 117],
        "busy",
      );
      expect(await css(busy.locator("svg"), "width")).toBe("16px");
    });
  }
});

test.describe("Archived items and Deleted items panels at 320 px", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      latency !== "" ||
      overlays !== "",
    "Needs the plain owner world",
  );
  test.use({ viewport: { width: 320, height: 640 } });

  for (const dark of [false, true]) {
    const mode = dark ? "dem" : "sang";
    const ink = INK[dark ? "dark" : "light"];

    test(`full screen: the 44 px close and search, the button under the line (ar320-R-${mode})`, async ({
      page,
    }) => {
      const panel = await open(page, "archive", dark);
      const p = await panel.boundingBox();
      near(p!.width, 320, "panel.w");
      await expectHeader(panel, true, ink);
      await expectSearch(panel, true, ink);
      const row = panel.getByRole("list").getByRole("listitem").first();
      await expectRow(panel, row, "Bỏ lưu trữ", true, ink, 121);
      near(
        (await box(row.getByRole("button", { name: "Bỏ lưu trữ" }), row)).w,
        117.1,
        "button.w",
      );
    });

    test(`nothing archived: the icon and the title (ar320-E-${mode})`, async ({
      page,
    }) => {
      const panel = await open(page, "archive", dark);
      await panel.getByRole("button", { name: "Bỏ lưu trữ" }).click();
      const title = panel.getByText("Chưa có việc nào được lưu trữ");
      await expect(title).toBeVisible();
      const holder = title.locator("..");
      const icon = holder.locator("span").first();
      expectBox(
        await box(icon, panel),
        { x: 138, y: 109, w: 44, h: 44 },
        "icon",
      );
      near((await box(title, panel)).y, 159, "title.y");
    });

    test(`a deleted item refused: the reason under the line, then Khôi phục (tr320-refuse-${mode})`, async ({
      page,
    }) => {
      const panel = await open(page, "trash", dark);
      await panel.getByRole("button", { name: "Khôi phục" }).click();
      const why = panel.getByRole("alert");
      await expect(why).toContainText("Chưa khôi phục được");
      const row = panel.getByRole("list").getByRole("listitem").first();
      const a = await box(row.getByRole("button", { name: "Khôi phục" }), row);
      const w = await box(why, row);
      near(a.x, 16, "button.x");
      expect(a.y, "the button sits under the reason").toBeGreaterThan(
        w.y + w.h - 1,
      );
      near(a.h, 44, "button.h");
    });
  }
});

test.describe("The archive failing to load at 320 px", () => {
  test.skip(
    faults !== "/boards/mock-board-erp/items/archived=500" ||
      persona !== "owner",
    "Needs VITE_MOCK_FAULTS=/boards/mock-board-erp/items/archived=500",
  );
  test.use({ viewport: { width: 320, height: 640 } });

  for (const dark of [false, true]) {
    test(`the banner stacks and Thử lại takes the whole width (ar320-Er-${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const panel = await open(page, "archive", dark);
      const banner = panel.getByRole("alert");
      await expect(banner).toBeVisible();
      const b = await box(banner, panel);
      near(b.x, 16, "banner.x");
      near(b.w, 286, "banner.w");
      near(b.h, 111.4, "banner.h");
      const retry = banner.getByRole("button", { name: "Thử lại" });
      const r = await box(retry, panel);
      near(r.x, 30, "retry.x");
      near(r.w, 258, "retry.w");
      near(r.h, 44, "retry.h");
    });
  }
});
