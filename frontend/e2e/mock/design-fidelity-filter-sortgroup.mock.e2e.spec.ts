import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The sort and group menus against the owner-canvas board `WM-Filter-SortGroup` (owner artifact
 * 18L4jUXGfdXh7Wn9YQvA2x), read live from `WM_CANVAS_DIR` (the downloaded `project/` folder; the spec skips when it is
 * missing) so no number is copied here. Four menus, each measured against its own top-left, within 1 px: the "Sắp xếp"
 * dialog (460 px: grip, field button, the Tăng/Giảm segment, remove, "Thêm cách sắp xếp", the note), the field list
 * that opens from a field button (250 px, one 36 px row per field with its type word), the "Nhóm theo" menu (310 px:
 * headings of 29.4 px, rows of 36 px, the separator and the "Chưa nhóm được" row) and the date bucket menu (250 px).
 * The app's board has other fields than the artboard's, so the rows are compared by kind and the gap between
 * neighbours, not by their text or number.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const FILE = "WM-Filter-SortGroup.dc.html";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

type Box = readonly [number, number, number, number];
type Item = { k: string; box: Box };

/** Evaluated in the page: the items of a menu (headings, rows, separators, notes) in order, against the menu. */
const ITEMS = `(root, art) => {
  const o = root.getBoundingClientRect();
  const box = (el) => { const b = el.getBoundingClientRect(); return [b.x - o.x, b.y - o.y, b.width, b.height]; };
  const sel = art ? ".mh,.mrow,.sep" : '[data-slot="dropdown-menu-label"],[role="menuitemradio"],[role="menuitem"],[role="option"],[data-slot="dropdown-menu-separator"],[data-f="note"],[aria-disabled="true"]:not([role])';
  const out = [];
  for (const el of root.querySelectorAll(sel)) {
    let k = "row";
    if (art) {
      if (el.classList.contains("mh")) k = "head";
      else if (el.classList.contains("sep")) k = "sep";
      else if (el.classList.contains("off")) k = "off";
    } else {
      const slot = el.getAttribute("data-slot");
      if (slot === "dropdown-menu-label") k = "head";
      else if (slot === "dropdown-menu-separator") k = "sep";
      else if (el.getAttribute("data-f") === "note" || el.getAttribute("aria-disabled") === "true") k = "off";
    }
    out.push({ k, box: box(el) });
  }
  return { root: [0, 0, o.width, o.height], items: out };
}`;

const SORT = `(root, art) => {
  const o = root.getBoundingClientRect();
  const box = (el) => { if (!el) return null; const b = el.getBoundingClientRect(); return [b.x - o.x, b.y - o.y, b.width, b.height]; };
  const out = { root: [0, 0, o.width, o.height], title: box(root.querySelector("h2")) };
  const rows = art
    ? [...root.querySelectorAll(":scope > div")].filter((d) => d.querySelector(".seg"))
    : [...root.querySelectorAll('[role="group"][aria-label^="Cách sắp xếp"]')];
  rows.forEach((row, i) => {
    const seg = row.querySelector(art ? ".seg" : '[role="group"]');
    const sb = [...seg.querySelectorAll("button")];
    const cells = art ? [row.firstElementChild, row.querySelector(".sel"), seg, row.querySelector(".iconbtn")] : [...row.children];
    out["r" + i + "grip"] = box(cells[0]);
    out["r" + i + "field"] = box(cells[1]);
    out["r" + i + "seg"] = box(cells[2]);
    out["r" + i + "asc"] = box(sb[0]);
    out["r" + i + "desc"] = box(sb[1]);
    out["r" + i + "remove"] = box(cells[3]);
  });
  const add = [...root.querySelectorAll("button")].find((b) => b.textContent.trim() === "Thêm cách sắp xếp");
  out.add = box(add);
  out.note = box(root.querySelector("p"));
  return out;
}`;

async function artboard(page: Page, selector: string, fn: string) {
  await page.goto(pathToFileURL(`${CANVAS}/${FILE}`).href);
  await page.waitForTimeout(400);
  return page.evaluate(
    ([sel, source]) => {
      const root = document.querySelector(sel!);
      if (!root) throw new Error(`no ${sel} on the artboard`);
      return (0, eval)(source!)(root, true);
    },
    [selector, fn],
  );
}

async function openBoard(page: Page, dark = false) {
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
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`${BOARD}?view=table`);
  await page
    .getByRole("button", { name: /^Sắp xếp/ })
    .first()
    .waitFor();
}

function same(
  got: Box | null | undefined,
  want: Box,
  label: string,
  skipWidth = false,
) {
  expect(got, `${label} exists`).toBeTruthy();
  (["x", "y", "width", "height"] as const).forEach((n, i) => {
    if (skipWidth && i === 2) return;
    expect(got![i], `${label} ${n}`).toBeCloseTo(want[i]!, 0);
  });
}

function sameItems(
  got: { root: Box; items: Item[] },
  want: { root: Box; items: Item[] },
  label: string,
) {
  expect(got.root[2], `${label} width`).toBeCloseTo(want.root[2], 0);
  // Each kind of item is as the artboard draws it (x, width, height), and neighbours of a kind pair keep its gap.
  const gap = new Map<string, number>();
  want.items.forEach((item, i) => {
    const prev = want.items[i - 1];
    if (prev)
      gap.set(`${prev.k}>${item.k}`, item.box[1] - (prev.box[1] + prev.box[3]));
  });
  const heights = new Map<string, number>();
  want.items.forEach((item) => {
    if (item.k !== "off") heights.set(item.k, item.box[3]);
  });
  got.items.forEach((item, i) => {
    const h = heights.get(item.k);
    if (h !== undefined)
      expect(item.box[3], `${label} ${item.k} ${i} height`).toBeCloseTo(h, 0);
    expect(item.box[0], `${label} ${item.k} ${i} x`).toBeCloseTo(
      want.items[0]!.box[0],
      0,
    );
    expect(item.box[2], `${label} ${item.k} ${i} width`).toBeCloseTo(
      want.root[2] - 2 * want.items[0]!.box[0],
      0,
    );
    const prev = got.items[i - 1];
    const g = prev ? gap.get(`${prev.k}>${item.k}`) : undefined;
    if (prev && g !== undefined)
      expect(
        item.box[1] - (prev.box[1] + prev.box[3]),
        `${label} ${prev.k}>${item.k} ${i} gap`,
      ).toBeCloseTo(g, 0);
  });
  expect(got.items[0]!.box[1], `${label} first item y`).toBeCloseTo(
    want.items[0]!.box[1],
    0,
  );
}

for (const dark of [false, true]) {
  test.describe(`Sort and group menus against the canvas, ${dark ? "dark" : "light"}`, () => {
    test.skip(
      !existsSync(`${CANVAS}/${FILE}`) ||
        persona !== "owner" ||
        scenario !== "default" ||
        faults !== "",
      "Needs the canvas boards and the plain owner world",
    );

    // The artboard is light only; the dark twin proves the same boxes with the dark tokens.
    test("the sort dialog", async ({ page }) => {
      const want = await artboard(page, '[role="dialog"]', SORT);
      await openBoard(page, dark);
      await page
        .getByRole("button", { name: /^Sắp xếp/ })
        .first()
        .click();
      const dialog = page.getByRole("dialog", { name: "Sắp xếp" });
      await dialog.getByRole("button", { name: "Thêm cách sắp xếp" }).click();
      await dialog.getByRole("button", { name: "Thêm cách sắp xếp" }).click();
      await dialog
        .getByRole("group", { name: "Cách sắp xếp 2" })
        .getByRole("button", { name: "Giảm dần" })
        .click();
      await page.waitForTimeout(300);
      const got = await dialog.evaluate(
        (el, fn) => (0, eval)(fn)(el, false),
        SORT,
      );
      for (const [key, box] of Object.entries(want as Record<string, Box>)) {
        if (key === "root") {
          same(got.root, box, "dialog");
          continue;
        }
        // The seg buttons are as wide as their text; the field button takes what the row leaves.
        same(got[key], box, key, key.endsWith("asc") || key.endsWith("desc"));
      }
    });

    test("the field list of a sort level", async ({ page }) => {
      const want = await artboard(page, '[role="listbox"]', ITEMS);
      await openBoard(page, dark);
      await page
        .getByRole("button", { name: /^Sắp xếp/ })
        .first()
        .click();
      const dialog = page.getByRole("dialog", { name: "Sắp xếp" });
      await dialog.getByRole("button", { name: "Thêm cách sắp xếp" }).click();
      await dialog.getByRole("button", { name: "Trường sắp xếp 1" }).click();
      const list = page.getByRole("listbox", { name: /Chọn trường/ });
      await expect(list).toBeVisible();
      await page.waitForTimeout(300);
      const popover = list.locator(
        "xpath=ancestor::*[@data-slot='popover-content']",
      );
      const got = await popover.evaluate(
        (el, fn) => (0, eval)(fn)(el, false),
        ITEMS,
      );
      expect(got.root[2], "list width").toBeCloseTo(want.root[2], 0);
      for (const item of got.items) {
        expect(item.box[3], "row height").toBeCloseTo(want.items[0].box[3], 0);
        expect(item.box[0], "row x").toBeCloseTo(want.items[0].box[0], 0);
      }
      expect(got.items[0].box[1], "first row y").toBeCloseTo(
        want.items[0].box[1],
        0,
      );
      expect(
        got.items[1].box[1] - got.items[0].box[1],
        "row pitch",
      ).toBeCloseTo(want.items[1].box[1] - want.items[0].box[1], 0);
    });

    test("the group menu and its date bucket menu", async ({ page }) => {
      const wantGroup = await artboard(
        page,
        '[role="menu"][aria-label="Nhóm theo"]',
        ITEMS,
      );
      const wantBucket = await artboard(
        page,
        '[aria-label="Gom ngày theo"]',
        ITEMS,
      );
      await openBoard(page, dark);
      await page.getByRole("button", { name: /^Nhóm/ }).first().click();
      const menu = page.getByRole("menu").first();
      await expect(menu).toBeVisible();
      await page.waitForTimeout(300);
      const gotGroup = await menu.evaluate(
        (el, fn) => (0, eval)(fn)(el, false),
        ITEMS,
      );
      sameItems(gotGroup, wantGroup, "group menu");
      expect(
        gotGroup.items.some((i: Item) => i.k === "off"),
        "the types that cannot be grouped",
      ).toBe(true);

      await menu.getByRole("menuitem", { name: /Ngày/ }).first().hover();
      const bucket = page.getByRole("menu").last();
      await expect(bucket).toBeVisible();
      await page.waitForTimeout(300);
      const gotBucket = await bucket.evaluate(
        (el, fn) => (0, eval)(fn)(el, false),
        ITEMS,
      );
      // The time-zone note is last in the menu and undrawn: the menu is measured without it.
      const zone = await bucket
        .locator('[data-f="tz-note"]')
        .evaluate((el) => el.getBoundingClientRect().height + 2);
      gotBucket.root[3] -= zone;
      sameItems(gotBucket, wantBucket, "bucket menu");
    });
  });
}
