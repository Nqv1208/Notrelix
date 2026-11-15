import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The 1440 px frames of `BC-Large` of the October 2026 canvas that the app draws, measured against the artboard
 * rendered in Chromium (`render.py BC-Large.dc.html`; boxes relative to the table's header row, the surface or the lane):
 * the group header with its count and the board total (`lg-more`), the filtered counts (`lg-filt`), the foot with
 * "Đã hiện 50 trên 200" and its button (`lg-more`), the load-more error row (`lg-err`), "Lên đầu" (`lg-jump`) and the
 * each in light and dark, and the Kanban lane's foot (`lg-kan`) and load error (`lg-kan-err`).
 * Also measured: the loading grid (`lg-skel`, needs `VITE_MOCK_LATENCY=slow`) and, at 768 px, the foot (`lg768-win`) and
 * the error row (`lg768-err`).
 *
 * At 320 px (phone list): the foot (`lg320-win`), the error banner with its retry under the text (`lg320-err`), "Lên đầu"
 * (`lg320-jump`) and the loading cards (`lg320-skel`). The artboard's frame is 320 px with a 1 px border, so its
 * content is 318 px and the app's 320: widths compare after that 2 px, and the offsets from the edges are exact.
 * The Kanban lane's foot and error are also measured at 768 and 320 px (`lg768-kan`, `lg320-kan`, `lg768-kan-err`,
 * `lg320-kan-err`); BC-Large draws no Kanban frame at those widths, so they hold the 1440 px lane numbers.
 *
 * Not measured, and why: `lg-win`, `lg768-win`'s windowed note and `lg320-win`'s windowed cards ("Đang hiện việc 101
 * đến 108 trên 200") belong to a windowed-rows design that the product does not use (paged load-more keeps every
 * loaded row; the foot is `lg-more`).
 *
 * The Kanban lane and card sizes are NOT taken from `BC-Large` (a 256 px lane with 232 x 101.9 cards): the product's
 * Kanban follows `Kanban-Board/Final` (a 266 px lane, 250 x 90.8 cards), which owns them for every board; `BC-Large`
 * governs only what is particular to a large board. So the lane's foot and its error are measured in height, button
 * and centring inside the current lane, and the lane and card boxes are left out.
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
const slow = process.env.VITE_MOCK_LATENCY === "slow";
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
  // 768 px frames (tablet): the foot is 64 px with the button 10 px in; the error row is 84 px with a 64 px banner.
  tablet: {
    foot: { h: 64, button: { y: 10 } },
    err: { row: { h: 84 }, banner: { h: 64 } },
  },
  // lg-skel: the title bars of the loading grid (36 px rows, 10 px bars) at 1440 px.
  skel: { row: 36, bar: 10, titles: [290.4, 384.8, 479.2, 319.4, 413.8] },
  jump: { w: 103.4, h: 44, r: 24, b: 20 },
  // 320 px frames (phone): the artboard's frame is 320 px with a 1 px border, so its content is 318 px wide and the app's 320.
  phone: {
    inner: 318,
    foot: {
      h: 64,
      fs: "12.5px",
      button: { w: 153.7, h: 44, fs: "14px", fromCentre: -10.3 },
    },
    err: {
      row: { h: 149.6 },
      banner: { x: 12, w: 294, h: 129.6 },
      retry: { x: 14, y: 75.6, w: 266, h: 44 },
    },
    jump: { w: 107.4, h: 44, r: 12, b: 12 },
    // lg320-skel: three 84 px cards 10 px apart, 12 px from the edges and 8 px under the toolbar; bars from the card's content.
    skel: {
      card: { x: 12, y: 8, w: 294, h: 84, gap: 10 },
      bars: [
        { x: 13, y: 13, w: 160.8, h: 10 },
        { x: 13, y: 31, w: 241.2, h: 12 },
        { x: 13, y: 51, w: 64, h: 20 },
        { x: 85, y: 51, w: 48, h: 20 },
      ],
    },
  },
  lane: {
    // From the artboard's lane (232 px inside): the foot wraps its text over the button; the error wraps its text in
    // a 95.4 px column beside the retry, which keeps 14 px from the banner's right edge.
    foot: { h: 94.1, button: { w: 144.9, h: 44, y: 40.1 } },
    err: { h: 93.8, wrap: 2, retry: { w: 72.6, h: 44, r: 14 } },
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

async function open(page: Page, url: string, dark: boolean, width = 1440) {
  await page.setViewportSize({ width, height: 900 });
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
const showMore = (page: Page) =>
  page.locator("[data-slot=table-show-more]:visible");

async function openTable(page: Page, dark: boolean, query = "", width = 1440) {
  await open(page, `${BOARD}?view=table${query}`, dark, width);
  await expect(showMore(page).first()).toBeVisible({ timeout: 90_000 });
  // The phone list draws no board total (the 320 px frames show none).
  if (width > 500)
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
      await filterByPerson(page, "Phan Ngọc Anh");
      const doing = page
        .locator("[data-table-scroll] [role=row]")
        .filter({ hasText: "Chọn cả nhóm" })
        .filter({ hasText: "Đang làm" })
        .first();
      // 12 of the 200 items in Đang làm are Phan Ngọc Anh's, 37 of the 5,000 of the board: not the loaded 10 (the artboard draws the same).
      const count = doing.getByText(/^12 của 200$/);
      // The board's filtered total sits on the first group of the table (Cần làm here; the artboard's first is Đang làm).
      const first = page
        .locator("[data-table-scroll] [role=row]")
        .filter({ hasText: "Đang lọc" })
        .first();
      const caption = first.getByText(
        /^Đang lọc: 37 trên 5\.000 việc của board$/,
      );
      await expect(count).toBeVisible({ timeout: 30_000 });
      await expect(caption).toBeVisible();
      await expect(page.getByText("37 công việc đang hiện")).toBeVisible();
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
      // The cell carries 12 px of right padding (pr-3), as in lg-more; the artboard measures the text.
      near(t.w - 12, MOCK.filt.caption.w, "caption.w");
      near(t.h, MOCK.filt.caption.h, "caption.h", 1.5);
      // The 1182 px artboard is 4 px narrower than the app's table: the caption keeps its 12 px from the right edge.
      const fb = (await first.boundingBox())!;
      near(
        fb.x + fb.width - ((await caption.boundingBox())!.x + t.w),
        MOCK.filt.caption.r - 12,
        "caption.right",
        2,
      );
    });

    test(`lg768-win: the foot of the group at 768 px (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark, "", 768);
      const foot = showMore(page).filter({ hasText: "trên 200" });
      await foot.scrollIntoViewIfNeeded();
      const f = await foot.boundingBox();
      if (!f) throw new Error("no box");
      near(f.height, MOCK.tablet.foot.h, "foot.h");
      const button = foot.getByRole("button", { name: "Hiện thêm 50 việc" });
      const b = await rel(button, foot);
      near(b.w, MOCK.foot.button.w, "button.w");
      near(b.h, MOCK.foot.button.h, "button.h");
      near(b.y, MOCK.tablet.foot.button.y, "button.y");
    });

    test(`lg320-win: the foot of the group at 320 px (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark, "", 320);
      const foot = showMore(page).filter({ hasText: "trên 200" });
      await foot.scrollIntoViewIfNeeded();
      await expect(foot).toContainText(/Đã hiện \d+ trên 200/);
      const f = await foot.boundingBox();
      if (!f) throw new Error("no box");
      near(f.width - (320 - MOCK.phone.inner), MOCK.phone.inner, "foot.w");
      near(f.height, MOCK.phone.foot.h, "foot.h");
      expect(await foot.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
        MOCK.phone.foot.fs,
      );
      const button = foot.getByRole("button", { name: "Hiện thêm 50 việc" });
      expect(await button.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
        MOCK.phone.foot.button.fs,
      );
      const b = await rel(button, foot);
      near(b.w, MOCK.phone.foot.button.w, "button.w");
      near(b.h, MOCK.phone.foot.button.h, "button.h");
      near(b.y, 10, "button.y");
      near(
        b.x - f.width / 2,
        MOCK.phone.foot.button.fromCentre,
        "button.fromCentre",
      );
    });

    test(`lg320-jump: "Lên đầu" sits 12 px from the right and the bottom at 320 px (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark, "", 320);
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
      near(j.width, MOCK.phone.jump.w, "jump.w");
      near(j.height, MOCK.phone.jump.h, "jump.h");
      near(s.x + s.width - (j.x + j.width), MOCK.phone.jump.r, "jump.right");
      near(s.y + s.height - (j.y + j.height), MOCK.phone.jump.b, "jump.bottom");
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

  test.describe(`Large board loading grid at 1440 px, ${mode}`, () => {
    test.skip(
      persona !== "owner" || !large || !slow,
      "Needs VITE_MOCK_LATENCY=slow",
    );

    test(`lg-skel: the rows and bars of the grid while the board loads (${mode})`, async ({
      page,
    }) => {
      await open(page, `${BOARD}?view=table`, dark);
      const grid = page.locator("[data-slot=main-table-loading]");
      await expect(grid).toBeVisible();
      const rows = grid.locator("div.grid.h-9");
      await expect(rows.nth(5)).toBeVisible();
      for (let i = 0; i < 6; i++) {
        near(
          (await rows.nth(i).boundingBox())!.height,
          MOCK.skel.row,
          `row${i}.h`,
        );
      }
      for (let i = 1; i < 6; i++) {
        const bars = rows.nth(i).locator("[data-slot=skeleton]");
        const title = (await bars.nth(1).boundingBox())!;
        // The bar is a share of its cell: the artboard's cell holds 726 px, the app's 4 px more (see the header).
        const cell = (await bars.nth(1).locator("xpath=..").boundingBox())!;
        near(
          title.width,
          (MOCK.skel.titles[i - 1]! * (cell.width - 20)) / 726,
          `title${i}.w`,
        );
        near(title.height, MOCK.skel.bar, `title${i}.h`);
      }
    });
  });

  test.describe(`Large board loading cards at 320 px, ${mode}`, () => {
    test.skip(
      persona !== "owner" || !large || !slow,
      "Needs VITE_MOCK_LATENCY=slow",
    );

    test(`lg320-skel: the cards of the list while the board loads (${mode})`, async ({
      page,
    }) => {
      await open(page, `${BOARD}?view=table`, dark, 320);
      const wrap = page.locator("[data-slot=main-table-loading-cards]");
      await expect(wrap).toBeVisible();
      // One synchronous read: the loading state is gone a moment later.
      const boxes = await wrap.evaluate((el) => {
        const r = (e: Element, o: Element) => {
          const a = e.getBoundingClientRect();
          const c = o.getBoundingClientRect();
          return { x: a.x - c.x, y: a.y - c.y, w: a.width, h: a.height };
        };
        return {
          width: el.getBoundingClientRect().width,
          cards: [...el.children].map((card) => ({
            box: r(card, el),
            bars: [...card.querySelectorAll("[data-slot=skeleton]")].map((b) =>
              r(b, card),
            ),
          })),
        };
      });
      expect(boxes.cards).toHaveLength(3);
      const k = MOCK.phone.skel;
      // The artboard's content is 318 px wide, the app's 320.
      const frame = boxes.width - MOCK.phone.inner;
      boxes.cards.forEach((card, i) => {
        near(card.box.x, k.card.x, `card${i}.x`);
        near(card.box.y, k.card.y + i * (k.card.h + k.card.gap), `card${i}.y`);
        near(card.box.w - frame, k.card.w, `card${i}.w`);
        near(card.box.h, k.card.h, `card${i}.h`);
        expect(card.bars).toHaveLength(4);
        card.bars.forEach((b, j) => {
          const want = k.bars[j]!;
          near(b.x, want.x, `card${i}.bar${j}.x`);
          near(b.y, want.y, `card${i}.bar${j}.y`);
          near(b.w, want.w, `card${i}.bar${j}.w`);
          near(b.h, want.h, `card${i}.bar${j}.h`);
        });
      });
    });
  });

  test.describe(`Large board Kanban lane at 1440 px, ${mode}`, () => {
    test.skip(persona !== "owner" || !large || failing, "large-board world");
    test.setTimeout(120_000);

    test(`lg-kan: the lane foot says how many of the lane show, with its button (${mode})`, async ({
      page,
    }) => {
      await open(page, `${BOARD}?view=kanban`, dark);
      const lane = page.locator("section[aria-label='Đang làm']");
      const foot = lane.locator("[data-lane-foot]");
      await expect(foot).toBeVisible({ timeout: 90_000 });
      await expect(foot).toContainText(/Đã hiện \d+ trên 200/);
      const f = await foot.boundingBox();
      if (!f) throw new Error("no box");
      near(f.height, MOCK.lane.foot.h, "foot.h");
      expect(await foot.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
        MOCK.foot.fs,
      );
      const button = foot.getByRole("button", { name: /^Hiện thêm \d+ việc$/ });
      const b = await rel(button, foot);
      near(b.w, MOCK.lane.foot.button.w, "button.w");
      near(b.h, MOCK.lane.foot.button.h, "button.h");
      near(b.y, MOCK.lane.foot.button.y, "button.y");
      near(b.x + b.w / 2, f.width / 2, "button.centre");
    });

    for (const width of [768, 320]) {
      test(`lg${width}-kan: the lane foot keeps its height and button at ${width} px (${mode})`, async ({
        page,
      }) => {
        await open(page, `${BOARD}?view=kanban`, dark, width);
        const lane = page.locator("section[aria-label='Đang làm']");
        const foot = lane.locator("[data-lane-foot]");
        await expect(foot).toBeVisible({ timeout: 90_000 });
        await foot.scrollIntoViewIfNeeded();
        await expect(foot).toContainText(/Đã hiện \d+ trên 200/);
        const f = await foot.boundingBox();
        if (!f) throw new Error("no box");
        near(f.height, MOCK.lane.foot.h, "foot.h");
        const button = foot.getByRole("button", {
          name: /^Hiện thêm \d+ việc$/,
        });
        const b = await rel(button, foot);
        near(b.w, MOCK.lane.foot.button.w, "button.w");
        near(b.h, MOCK.lane.foot.button.h, "button.h");
        near(b.y, MOCK.lane.foot.button.y, "button.y");
        near(b.x + b.w / 2, f.width / 2, "button.centre");
      });
    }
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
      const row = page.locator("[data-slot=table-load-more-error]:visible");
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

    test(`lg768-err: the failed page at 768 px keeps the banner and its retry (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark, "", 768);
      await showMore(page)
        .filter({ hasText: "trên 200" })
        .getByRole("button", { name: "Hiện thêm 50 việc" })
        .click();
      const row = page.locator("[data-slot=table-load-more-error]:visible");
      await expect(row).toBeVisible({ timeout: 30_000 });
      const r = await row.boundingBox();
      if (!r) throw new Error("no box");
      near(r.height, MOCK.tablet.err.row.h, "row.h");
      const banner = row.getByRole("alert");
      const b = await rel(banner, row);
      near(b.h, MOCK.tablet.err.banner.h, "banner.h");
      near(b.x, MOCK.err.banner.x, "banner.x");
      const retry = banner.getByRole("button", { name: "Thử lại" });
      const t = await rel(retry, banner);
      near(t.w, MOCK.err.retry.w, "retry.w");
      near(t.h, MOCK.err.retry.h, "retry.h");
    });

    test(`lg320-err: the failed page at 320 px keeps the rows and puts the retry under the text (${mode})`, async ({
      page,
    }) => {
      await openTable(page, dark, "", 320);
      await showMore(page)
        .filter({ hasText: "trên 200" })
        .getByRole("button", { name: "Hiện thêm 50 việc" })
        .click();
      const row = page.locator("[data-slot=table-load-more-error]:visible");
      await expect(row).toBeVisible({ timeout: 30_000 });
      await expect(row).toContainText("Không tải thêm được");
      const r = await row.boundingBox();
      if (!r) throw new Error("no box");
      // The artboard's content is 318 px wide, the app's 320: widths compare after that 2 px.
      const frame = r.width - MOCK.phone.inner;
      near(r.height, MOCK.phone.err.row.h, "row.h");
      const banner = row.getByRole("alert");
      const b = await rel(banner, row);
      near(b.x, MOCK.phone.err.banner.x, "banner.x");
      near(b.w - frame, MOCK.phone.err.banner.w, "banner.w");
      near(b.h, MOCK.phone.err.banner.h, "banner.h");
      const retry = banner.getByRole("button", { name: "Thử lại" });
      const t = await rel(retry, banner);
      near(t.x, MOCK.phone.err.retry.x, "retry.x");
      near(t.y, MOCK.phone.err.retry.y, "retry.y");
      near(t.w - frame, MOCK.phone.err.retry.w, "retry.w");
      near(t.h, MOCK.phone.err.retry.h, "retry.h");
    });

    test(`lg-kan-err: a failed lane page keeps the cards and shows the error with its retry (${mode})`, async ({
      page,
    }) => {
      await open(page, `${BOARD}?view=kanban`, dark);
      const lane = page.locator("section[aria-label='Đang làm']");
      const foot = lane.locator("[data-lane-foot]");
      await expect(foot).toBeVisible({ timeout: 90_000 });
      await foot.getByRole("button", { name: /^Hiện thêm/ }).click();
      const error = lane.locator("[data-lane-load-error]");
      await expect(error).toBeVisible({ timeout: 30_000 });
      await expect(error).toContainText("Không tải thêm được");
      const banner = error.getByRole("alert");
      const b = await banner.boundingBox();
      if (!b) throw new Error("no box");
      near(b.height, MOCK.lane.err.h, "banner.h");
      const retry = banner.getByRole("button", { name: "Thử lại" });
      const t = await rel(retry, banner);
      near(t.w, MOCK.lane.err.retry.w, "retry.w");
      near(t.h, MOCK.lane.err.retry.h, "retry.h");
      near(b.width - (t.x + t.w), MOCK.lane.err.retry.r, "retry.right");
    });

    for (const width of [768, 320]) {
      test(`lg${width}-kan-err: a failed lane page keeps the error and its retry at ${width} px (${mode})`, async ({
        page,
      }) => {
        await open(page, `${BOARD}?view=kanban`, dark, width);
        const lane = page.locator("section[aria-label='Đang làm']");
        const foot = lane.locator("[data-lane-foot]");
        await expect(foot).toBeVisible({ timeout: 90_000 });
        const error = lane.locator("[data-lane-load-error]");
        // Scrolling the foot into view may already ask for the next page (the lane reads on approach): press only if it did not.
        await expect(async () => {
          if (!(await error.isVisible()))
            await foot
              .getByRole("button", { name: /^Hiện thêm/ })
              .click({ timeout: 1_000 })
              .catch(() => undefined);
          await expect(error).toBeVisible({ timeout: 2_000 });
        }).toPass({ timeout: 30_000 });
        const banner = error.getByRole("alert");
        const b = await banner.boundingBox();
        if (!b) throw new Error("no box");
        // At 768 px the lane is wide enough for the text to sit beside the retry: the 64 px banner of the tablet table.
        near(
          b.height,
          width === 768 ? MOCK.tablet.err.banner.h : MOCK.lane.err.h,
          "banner.h",
        );
        const retry = banner.getByRole("button", { name: "Thử lại" });
        const t = await rel(retry, banner);
        near(t.w, MOCK.lane.err.retry.w, "retry.w");
        near(t.h, MOCK.lane.err.retry.h, "retry.h");
        near(b.width - (t.x + t.w), MOCK.lane.err.retry.r, "retry.right");
      });
    }
  });
}
