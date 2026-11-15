import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The 1440 px frames of `BC-Large` of the October 2026 canvas that the app draws, measured against the artboard
 * rendered in Chromium (`render.py BC-Large.dc.html`; boxes relative to the table's header row, the surface or the lane):
 * the group header with its count and the board total (`lg-more`), the filtered counts (`lg-filt`), the foot with
 * "Đã hiện 50 trên 200" and its button (`lg-more`), the load-more error row (`lg-err`), "Lên đầu" (`lg-jump`) and the
 * each in light and dark. The Kanban lane frames (`lg-kan`, `lg-kan-err`) are not measured yet.
 *
 * The world is `VITE_MOCK_OVERLAYS=large-board`: the board `mock-board-it-large` holds 5,000 items (200 in Đang làm) and
 * is read a page at a time like the real server does (`/full?pageSize`, `/items/lanes/{lane}`). The error frames need
 * `VITE_MOCK_FAULTS=/boards/mock-board-it-large/items/lanes/mock-itl-status-doing=500`. Tolerance is 1 px; the artboard
 * is 4 px narrower than the app table (frame of 1182 px against 1184), so a centred box is compared by its width and by
 * its distance from the nearer edge within 2 px.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const large = overlays.includes("large-board");
const failing = faults.includes("mock-itl-status-doing");
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-it-large";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

/** Numbers measured on the rendered artboard (light and dark are the same boxes). */
const MOCK = {
  header: { h: 36 },
  group: { y: 36, h: 40, fs: "13.5px" },
  count: { x: 141, w: 26.3, h: 18.8, y: 46.1 },
  caption: { w: 130.9, r: 12, h: 18.8, y: 46.1 },
  row: { y: 76, h: 40 },
  foot: {
    y: 236,
    h: 64,
    fs: "12.5px",
    button: { x: 559.1, w: 144.9, h: 44, y: 246 },
  },
  err: {
    row: { y: 236, h: 84 },
    banner: { x: 24, y: 246, w: 1082, h: 64 },
    retry: { w: 72.6, h: 44 },
  },
  filt: {
    group: { h: 40 },
    // From the group row's own top-left: the lane count ("63 của 200") and the filtered total on the right.
    // The name sits 11 px nearer the left in the app's shared group header (not measured here); the gap to the count is.
    count: { gap: 8, y: 10.1, w: 70.2, h: 18.8 },
    caption: { r: 12, y: 10.1, w: 250.9, h: 18.8 },
  },
  jump: { w: 103.4, h: 44, r: 24, b: 20 },
  lane: {
    foot: { h: 94.1, button: { w: 144.9, h: 44 } },
    err: { h: 93.8, retry: { w: 72.6, h: 44 } },
  },
} as const;

async function rel(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("no box");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

function near(actual: number, want: number, what: string, tol = TOL) {
  if (process.env.BOXLOG) {
    console.log("NEAR", what, actual.toFixed(1), "want", want);
    return;
  }
  expect(
    Math.abs(actual - want),
    `${what}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(tol);
}

function expectBox(actual: Box, want: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const) {
    const w = want[k];
    if (w !== undefined) near(actual[k], w, `${what}.${k}`);
  }
}

async function open(page: Page, url: string, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 900 });
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
  await page.goto(url);
}

const header = (page: Page) =>
  page.locator("[data-table-scroll] [role=row]").first();
const showMore = (page: Page) => page.locator("[data-slot=table-show-more]");

async function openTable(page: Page, dark: boolean, query = "") {
  await open(page, `${BOARD}?view=table${query}`, dark);
  await expect(showMore(page).first()).toBeVisible({ timeout: 90_000 });
  await expect(
    page.locator("[data-table-scroll]").getByText("Board có 5.000 việc"),
  ).toBeVisible();
  await showMore(page).filter({ hasText: "trên 200" }).scrollIntoViewIfNeeded();
}

/** Filters the board to one person through the toolbar's filter panel (the server counts the rest). */
async function filterByPerson(page: Page, name: string) {
  await page.getByRole("button", { name: /^Lọc/ }).first().click();
  await page.getByRole("button", { name: "Thêm điều kiện" }).click();
  await page
    .getByRole("combobox")
    .first()
    .selectOption({ label: "Người thực hiện" });
  await page.getByText("Giá trị", { exact: true }).first().click();
  await page.getByRole("option", { name: new RegExp(name) }).click();
  // The people list stays open over the panel: close it, then the panel.
  await page.keyboard.press("Escape");
  await page.getByRole("button", { name: "Xong" }).click();
}

/** The right edge of an element from the header row's right edge. */
async function fromRight(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("no box");
  return o.x + o.width - (e.x + e.width);
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test.describe(`Large board table at 1440 px, ${mode}`, () => {
    test.skip(persona !== "owner" || !large || failing, "large-board world");
    test.setTimeout(120_000);

    test(`lg-more: the group header, the first row and the foot with its button (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark);
      const head = header(page);
      await page.locator("[data-table-scroll]").evaluate((el) => {
        el.scrollTop = 0;
      });
      expectBox(await rel(head, head), { h: MOCK.header.h }, "header");

      // The board total sits on the first group; the count is read on Đang làm.
      const group = page
        .locator("[data-table-scroll] [role=row]")
        .filter({ hasText: "Board có 5.000 việc" })
        .first();
      const g = await rel(group, head);
      near(g.y, MOCK.group.y, "group.y");
      near(g.h, MOCK.group.h, "group.h");

      const caption = group.getByText("Board có 5.000 việc");
      const c = await rel(caption, head);
      // The cell carries 12 px of right padding (pr-3); the artboard measures the text.
      near(c.w - 12, MOCK.caption.w, "caption.w");
      near(
        await fromRight(caption, head),
        MOCK.caption.r - 12,
        "caption.right",
      );
      near(c.h, MOCK.caption.h, "caption.h", 1.5);
      const doing = page
        .locator("[data-table-scroll] [role=row]")
        .filter({ hasText: "Chọn cả nhóm" })
        .filter({ hasText: "Đang làm" })
        .first();
      const count = doing.getByText("200", { exact: true }).first();
      // The count is text: its width and line box differ by 1.0 px and 1.2 px (13 px type, 1.5 against 1.45 leading).
      const cb = await rel(count, head);
      near(cb.w, MOCK.count.w, "count.w", 1.1);
      near(cb.h, MOCK.count.h, "count.h", 1.5);

      const first = page
        .locator("[data-table-scroll] [role=row]")
        .filter({ hasText: "ERP-101" })
        .first();
      const r = await rel(first, head);
      near(r.h, MOCK.row.h, "row.h");

      const foot = showMore(page).filter({ hasText: "trên 200" });
      await foot.scrollIntoViewIfNeeded();
      await expect(foot).toContainText(/Đã hiện \d+ trên 200/);
      const f = await rel(foot, head);
      near(f.h, MOCK.foot.h, "foot.h");
      expect(await foot.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
        MOCK.foot.fs,
      );
      const button = foot.getByRole("button", { name: "Hiện thêm 50 việc" });
      const b = await rel(button, foot);
      near(b.w, MOCK.foot.button.w, "button.w");
      near(b.h, MOCK.foot.button.h, "button.h");
      near(b.y, MOCK.foot.button.y - MOCK.foot.y, "button.y");
      // The artboard centres "Đã hiện N trên 200" and the button as one line; the app does the same.
      near(b.x, MOCK.foot.button.x, "button.x");
    });

    test(`lg-filt: the lane count and the filtered total come from the server (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark);
      await filterByPerson(page, "Lê Hoàng Phúc");
      const doing = page
        .locator("[data-table-scroll] [role=row]")
        .filter({ hasText: "Chọn cả nhóm" })
        .filter({ hasText: "Đang làm" })
        .first();
      // 63 of the 200 items in Đang làm are Phúc's, 63 of the 5,000 of the board in the open lanes: not the loaded 10.
      const count = doing.getByText(/^63 của 200$/);
      // The board's filtered total sits on the first group of the table (Cần làm here; the artboard's first is Đang làm).
      const first = page
        .locator("[data-table-scroll] [role=row]")
        .filter({ hasText: "Đang lọc" })
        .first();
      const caption = first.getByText(
        /^Đang lọc: 63 trên 5\.000 việc của board$/,
      );
      await expect(count).toBeVisible({ timeout: 30_000 });
      await expect(caption).toBeVisible();
      await expect(page.getByText("63 công việc đang hiện")).toBeVisible();
      const g = await doing.boundingBox();
      if (!g) throw new Error("no box");
      near(g.height, MOCK.filt.group.h, "group.h");
      const c = await rel(count, doing);
      const nameRight = await doing.evaluate((row) => {
        const walker = document.createTreeWalker(row, NodeFilter.SHOW_TEXT);
        for (let n = walker.nextNode(); n; n = walker.nextNode()) {
          if (n.textContent?.trim() !== "Đang làm") continue;
          const range = document.createRange();
          range.selectNodeContents(n);
          return (
            range.getBoundingClientRect().right - row.getBoundingClientRect().x
          );
        }
        return NaN;
      });
      near(c.x - nameRight, MOCK.filt.count.gap, "count.gap");
      // The count is text: its line box differs by about 1 px (13 px type, 1.5 against 1.45 leading), as in lg-more.
      near(
        c.y + c.h / 2,
        MOCK.filt.count.y + MOCK.filt.count.h / 2,
        "count.mid",
        1,
      );
      near(c.w, MOCK.filt.count.w, "count.w", 1.1);
      near(c.h, MOCK.filt.count.h, "count.h", 1.5);
      const t = await rel(caption, first);
      near(
        t.y + t.h / 2,
        MOCK.filt.caption.y + MOCK.filt.caption.h / 2,
        "caption.mid",
        1,
      );
      near(t.w, MOCK.filt.caption.w, "caption.w");
      near(t.h, MOCK.filt.caption.h, "caption.h", 1.5);
      // The 1182 px artboard is 4 px narrower than the app's table: the caption keeps its 12 px from the right edge.
      const fb = (await first.boundingBox())!;
      near(
        fb.x + fb.width - ((await caption.boundingBox())!.x + t.w),
        MOCK.filt.caption.r,
        "caption.right",
        2,
      );
    });

    test(`lg-jump: "Lên đầu" sits 24 px from the right and 20 px above the bottom (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark);
      await showMore(page)
        .filter({ hasText: "trên 200" })
        .getByRole("button", { name: "Hiện thêm 50 việc" })
        .click();
      await page.locator("[data-table-scroll]").evaluate((el) => {
        el.scrollTop = el.scrollHeight;
      });
      const jump = page.getByRole("button", { name: "Lên đầu" });
      await expect(jump).toBeVisible();
      const surface = page.locator("[data-slot=main-table-surface]");
      const [j, s] = await Promise.all([
        jump.boundingBox(),
        surface.boundingBox(),
      ]);
      if (!j || !s) throw new Error("no box");
      near(j.width, MOCK.jump.w, "jump.w");
      near(j.height, MOCK.jump.h, "jump.h");
      near(s.x + s.width - (j.x + j.width), MOCK.jump.r, "jump.right");
      near(s.y + s.height - (j.y + j.height), MOCK.jump.b, "jump.bottom");
    });
  });

  test.describe(`Large board load-more error at 1440 px, ${mode}`, () => {
    test.skip(persona !== "owner" || !large || !failing, "faulted lane");
    test.setTimeout(120_000);

    test(`lg-err: the failed page leaves the rows and shows the banner with its retry (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark);
      await showMore(page)
        .filter({ hasText: "trên 200" })
        .getByRole("button", { name: "Hiện thêm 50 việc" })
        .click();
      const row = page.locator("[data-slot=table-load-more-error]");
      await expect(row).toBeVisible({ timeout: 30_000 });
      await expect(row).toContainText("Không tải thêm được");
      const head = header(page);
      const r = await rel(row, head);
      near(r.h, MOCK.err.row.h, "row.h");
      const banner = row.getByRole("alert");
      const b = await rel(banner, row);
      near(b.h, MOCK.err.banner.h, "banner.h");
      near(b.x, MOCK.err.banner.x, "banner.x");
      near(b.w, (await rel(row, head)).w - 2 * MOCK.err.banner.x, "banner.w");
      const retry = banner.getByRole("button", { name: "Thử lại" });
      const t = await rel(retry, banner);
      near(t.w, MOCK.err.retry.w, "retry.w");
      near(t.h, MOCK.err.retry.h, "retry.h");
    });
  });
}
