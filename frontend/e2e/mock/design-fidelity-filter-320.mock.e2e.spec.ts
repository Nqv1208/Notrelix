import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The phone bar and its two sheets against the owner-canvas board `WM-Filter-320` (owner artifact
 * 18L4jUXGfdXh7Wn9YQvA2x), read live from `WM_CANVAS_DIR` (skips when missing), the app at 320 x 720: the bar of
 * Tìm, Lọc, Cách xem and Bộ lọc đã lưu (each box and the gaps between them), the filter sheet (title, close, the VÀ and
 * HOẶC switch, the three stacked condition cards with their lead, field, operator, value and remove, "Thêm điều
 * kiện" and the footer) and the Cách xem sheet (the personal row, sort, group and lane selects, the footer). Boxes
 * are relative to the sheet's top-left, the footer's to its bottom, within 1 px. The app's sheet is as tall as its
 * content where the artboard's card is 720 px tall, so the footer is compared from the bottom.
 *
 * Deviations, written down: the field and operator keep the browser's own select (same box, same 13.5 px text); the
 * artboard's Kanban draws a sort row the Kanban has no sort for, so the sort row is measured on the Table and the
 * lane row on the Kanban.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const FILE = "WM-Filter-320.dc.html";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const FILTER =
  "c2:or~mock-erp-col-owner.has_any.__none__~mock-erp-col-golive.between.2026-10-12.2026-10-18~$title.contains.báo cáo";

type Box = readonly [number, number, number, number];
type Boxes = Record<string, Box | null>;

/** Both pages: the named boxes of a sheet. `art` picks the artboard's markup. The bottom ones are measured from the bottom. */
const FILTER_SHEET = `(root, art) => {
  const o = root.getBoundingClientRect();
  const ox = o.x, ob = o.bottom;
  const box = (el, bottom) => { if (!el) return null; const b = el.getBoundingClientRect(); return [b.x - ox, bottom ? b.y - ob : b.y - o.y, b.width, b.height]; };
  const out = { sheet: [0, 0, o.width, 0] };
  const btn = (text) => [...root.querySelectorAll("button")].find((b) => b.textContent.trim() === text);
  out.title = box(root.querySelector("h2"));
  out.close = box(root.querySelector('button[aria-label="Đóng"]'));
  const seg = root.querySelector('[role="group"][aria-label="Cách ghép điều kiện"]');
  out.seg = box(seg);
  const sb = seg ? [...seg.querySelectorAll("button")] : [];
  out.segAll = box(sb[0]);
  out.segAny = box(sb[1]);
  const cards = art
    ? [...root.children[2].children]
    : [...root.querySelectorAll('[role="group"][aria-label^="Điều kiện"]')];
  cards.forEach((card, i) => {
    out["c" + i] = box(card);
    const row = art ? card.children[0] : card.firstElementChild;
    const kids = art ? [row.children[0], row.children[1].firstElementChild, card.children[1], card.children[2], row.children[2]] : [...row.children];
    out["c" + i + "lead"] = box(kids[0]);
    out["c" + i + "field"] = box(kids[1]);
    out["c" + i + "op"] = box(kids[2]);
    out["c" + i + "remove"] = box(art ? kids[4] : kids[4]);
    const value = kids[3];
    const inputs = [...value.querySelectorAll(art ? ".inp" : "input")];
    inputs.forEach((el, j) => (out["c" + i + "in" + j] = box(el)));
  });
  const named = [...root.querySelectorAll("span")].find((e) => e.children.length < 3 && e.textContent.trim() === "Chưa giao");
  out.chip = box(named && (art ? named : named.closest("span[class*='rounded']") ?? named));
  out.add = box(btn("Thêm điều kiện"));
  out.clear = box(btn("Xoá hết"), true);
  out.done = box(btn("Xong"), true);
  return out;
}`;

const VIEW_SHEET = `(root, art) => {
  const o = root.getBoundingClientRect();
  const ox = o.x, ob = o.bottom;
  const box = (el, bottom) => { if (!el) return null; const b = el.getBoundingClientRect(); return [b.x - ox, bottom ? b.y - ob : b.y - o.y, b.width, b.height]; };
  const out = { sheet: [0, 0, o.width, 0] };
  const btn = (text) => [...root.querySelectorAll("button")].find((b) => b.textContent.trim() === text);
  out.title = box(root.querySelector("h2"));
  out.close = box(root.querySelector('button[aria-label="Đóng"]'));
  const mine = art ? root.querySelector(".mine") : root.querySelector('[role="group"][aria-label="Cách xem riêng"]');
  out.mine = box(mine);
  out.mineReset = box(mine && mine.querySelector("button"));
  const block = (word) => art ? [...root.querySelectorAll(".join")].find((e) => e.textContent.trim() === word)?.parentElement : null;
  const pick = (word, aria) => art ? block(word)?.querySelector(".sel") : root.querySelector('select[aria-label="' + aria + '"]');
  out.sortSel = box(pick("SẮP XẾP", "Sắp xếp"));
  out.sortSeg = box(art ? block("SẮP XẾP")?.querySelector(".seg") : root.querySelector('[role="group"][aria-label="Chiều sắp xếp"]'));
  out.groupSel = box(pick("NHÓM THEO", "Nhóm theo"));
  out.laneSel = box(pick("LÀN THEO", "Làn theo"));
  out.reset = box(btn("Đặt lại"), true);
  out.done = box(btn("Xong"), true);
  return out;
}`;

async function artboard(page: Page, tile: number, fn: string): Promise<Boxes> {
  await page.goto(pathToFileURL(`${CANVAS}/${FILE}`).href);
  await page.waitForTimeout(400);
  return page.evaluate(
    ([n, source]) => {
      const card = document
        .querySelectorAll(".tile")
        [n as number]!.querySelector<HTMLElement>(".card")!;
      // The sheet has a border on its top edge only; the artboard's card has four.
      card.style.borderWidth = "1px 0 0 0";
      return (0, eval)(source as string)(card, true);
    },
    [tile, fn],
  ) as Promise<Boxes>;
}

async function open(page: Page, view: "kanban" | "table") {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.setViewportSize({ width: 320, height: 720 });
  await page.goto(`${BOARD}?view=${view}&filter=${encodeURIComponent(FILTER)}`);
  await page.getByRole("button", { name: /^Lọc/ }).first().waitFor();
  await page.waitForTimeout(500);
}

function same(
  a: Box | null | undefined,
  e: Box,
  key: string,
  skip: number[] = [],
) {
  expect(a, `${key} exists`).toBeTruthy();
  for (let i = 0; i < 4; i++) {
    if (skip.includes(i)) continue;
    expect(a![i], `${key} ${["x", "y", "width", "height"][i]}`).toBeCloseTo(
      e[i]!,
      0,
    );
  }
}

const needs = () =>
  !existsSync(`${CANVAS}/${FILE}`) ||
  scenario !== "default" ||
  persona !== "owner" ||
  faults !== "";

test.describe("WM-Filter-320 against the canvas", () => {
  test("the bar", async ({ page }) => {
    test.skip(needs(), "Needs the canvas board and the plain owner world");
    await page.setViewportSize({ width: 320, height: 720 });
    await page.goto(pathToFileURL(`${CANVAS}/${FILE}`).href);
    await page.waitForTimeout(400);
    const want = await page.evaluate(() => {
      const bar = document
        .querySelectorAll(".tile")[0]!
        .querySelector(".card")!.firstElementChild!;
      const kids = [...bar.children].filter((c) => c.tagName === "BUTTON");
      return kids.map((k) => {
        const b = k.getBoundingClientRect();
        return [b.x, b.y, b.width, b.height];
      });
    });
    await open(page, "kanban");
    const got = await page.evaluate(() => {
      const row = document.querySelector("[data-kanban-toolbar] > div")!;
      const named = (re: RegExp) =>
        [...row.querySelectorAll("button")].find(
          (b) =>
            re.test(b.getAttribute("aria-label") ?? "") ||
            re.test(b.textContent ?? ""),
        )!;
      return [
        named(/^Tìm/),
        named(/^Lọc/),
        named(/^Cách xem/),
        named(/Bộ lọc đã lưu/),
      ].map((k) => {
        const b = k.getBoundingClientRect();
        return [b.x, b.y, b.width, b.height];
      });
    });
    await page.screenshot({ path: "/tmp/h19/bar-320.png" });
    expect(got).toHaveLength(4);
    got.forEach((g, i) => {
      expect(g![2], `button ${i} width`).toBeCloseTo(want[i]![2]!, 0);
      expect(g![3], `button ${i} height`).toBeCloseTo(want[i]![3]!, 0);
      expect(g![1], `button ${i} y`).toBeCloseTo(got[0]![1]!, 0);
    });
    // The gaps between the first three boxes are the artboard's.
    for (const i of [1, 2]) {
      expect(
        got[i]![0]! - (got[i - 1]![0]! + got[i - 1]![2]!),
        `gap ${i}`,
      ).toBeCloseTo(want[i]![0]! - (want[i - 1]![0]! + want[i - 1]![2]!), 0);
    }
  });

  test("the filter sheet", async ({ page }) => {
    test.skip(needs(), "Needs the canvas board and the plain owner world");
    const want = await artboard(page, 1, FILTER_SHEET);
    await open(page, "kanban");
    await page.getByRole("button", { name: /^Lọc/ }).first().click();
    const sheet = page.getByRole("dialog", { name: "Lọc việc" });
    await expect(sheet).toBeVisible();
    await page.waitForTimeout(500);
    const got = (await sheet.evaluate(
      (el, fn) => (0, eval)(fn)(el, false),
      FILTER_SHEET,
    )) as Boxes;
    await page.screenshot({ path: "/tmp/h19/filter-sheet-320.png" });
    if (process.env.DEBUG_BOXES) console.log(JSON.stringify({ got, want }));
    for (const [key, e] of Object.entries(want)) {
      if (key === "sheet") {
        expect(got.sheet![2], "sheet width").toBeCloseTo(e![2]!, 0);
        continue;
      }
      // The lead word is as tall as its text in the artboard and the row in the app: its x is compared.
      if (key.endsWith("lead")) {
        expect(got[key]![0], `${key} x`).toBeCloseTo(e![0]!, 0);
        continue;
      }
      // The artboard's chip wrapper is the chip's own height.
      same(
        got[key],
        e!,
        key,
        key === "c0in0" ? [1, 3] : key === "title" ? [2] : [],
      );
    }
  });

  test("the view sheet on the Table and the Kanban", async ({ page }) => {
    test.skip(needs(), "Needs the canvas board and the plain owner world");
    const want = await artboard(page, 2, VIEW_SHEET);
    for (const view of ["table", "kanban"] as const) {
      await open(page, view);
      await page
        .getByRole("button", { name: /^Cách xem/ })
        .first()
        .click();
      const sheet = page.getByRole("dialog", { name: "Cách xem" });
      await expect(sheet).toBeVisible();
      await page.waitForTimeout(500);
      const got = (await sheet.evaluate(
        (el, fn) => (0, eval)(fn)(el, false),
        VIEW_SHEET,
      )) as Boxes;
      await page.screenshot({ path: `/tmp/h19/view-sheet-${view}-320.png` });
      if (process.env.DEBUG_BOXES) console.log(JSON.stringify({ got, want }));
      const keys =
        view === "table"
          ? [
              "title",
              "close",
              "mine",
              "mineReset",
              "sortSel",
              "sortSeg",
              "groupSel",
              "reset",
              "done",
            ]
          : ["title", "close", "mine", "mineReset", "reset", "done"];
      for (const key of keys)
        same(got[key], want[key]!, key, key === "title" ? [2] : []);
      if (view === "kanban") {
        // Without the sort row, the group row and the lane row keep the artboard's spacing, x, width and height.
        same(got.groupSel, want.groupSel!, "groupSel", [1]);
        same(got.laneSel, want.laneSel!, "laneSel", [1]);
        expect(
          got.laneSel![1]! - got.groupSel![1]!,
          "group to lane",
        ).toBeCloseTo(want.laneSel![1]! - want.groupSel![1]!, 0);
      }
    }
  });
});
