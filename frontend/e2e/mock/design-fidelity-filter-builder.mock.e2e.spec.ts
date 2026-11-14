import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The "Lọc việc" dialog against the owner-canvas boards `WM-Filter-Builder` (light) and `WM-Filter-Dem` (dark)
 * (owner artifact 18L4jUXGfdXh7Wn9YQvA2x). The boards live only on the canvas, so the spec reads them live from
 * `WM_CANVAS_DIR` (the downloaded `project/` folder; it skips when missing) and no number is copied here. Every box is
 * relative to the dialog's own top-left, within 1 px: the dialog (760 px wide), the title and the two "Khớp" buttons,
 * each of the three condition rows (lead word, field, operator, value cell, remove button), the chip of "Chưa giao",
 * the two date inputs and the text input, "Thêm điều kiện", and the footer buttons. The app's board has a different
 * field list than the artboard's, so a row's text differs; the boxes do not.
 *
 * Deviations, written down: the artboard draws the field and operator as button-styled selects; the app keeps the
 * browser's own select (same box, same 13.5 px text, the browser's chevron). The date presets ("Hôm nay", "Tuần này"
 * ...) are not in the artboard's dialog row: they grow in under the date row while it has focus.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const FILTER =
  "c2:or~mock-erp-col-owner.has_any.__none__~mock-erp-col-golive.between.2026-10-12.2026-10-18~$title.contains.báo cáo";

type Box = readonly [number, number, number, number];
type Boxes = Record<string, Box>;

/** Evaluated in the page: the named boxes of a dialog, against the dialog. `art` picks the artboard's markup. */
const PROBE = `(root, art) => {
  const o = root.getBoundingClientRect();
  const box = (el) => {
    if (!el) return null;
    const b = el.getBoundingClientRect();
    return [b.x - o.x, b.y - o.y, b.width, b.height];
  };
  const out = { dialog: [0, 0, o.width, o.height] };
  out.title = box(root.querySelector("h2"));
  const seg = root.querySelector('[role="group"][aria-label]');
  const segButtons = seg ? [...seg.querySelectorAll("button")] : [];
  out.matchAll = box(segButtons[0]);
  out.matchAny = box(segButtons[1]);
  const rows = [...root.querySelectorAll('[role="group"][aria-label^="Điều kiện"]')];
  rows.forEach((group, i) => {
    const grid = art ? group : group.firstElementChild;
    const cells = [...grid.children];
    out["r" + i + "lead"] = box(cells[0]);
    out["r" + i + "field"] = box(cells[1]);
    out["r" + i + "op"] = box(cells[2]);
    out["r" + i + "value"] = box(cells[3]);
    out["r" + i + "remove"] = box(cells[4]);
    const inputs = [...cells[3].querySelectorAll(art ? ".inp" : "input")];
    inputs.forEach((el, j) => (out["r" + i + "in" + j] = box(el)));
  });
  const named = [...root.querySelectorAll("span")].find((e) => e.children.length < 3 && e.textContent.trim() === "Chưa giao");
  const chip = named && (art ? named : named.closest("span[class*='rounded']") ?? named);
  out.chip = box(chip);
  const buttons = [...root.querySelectorAll("button")];
  out.add = box(buttons.find((b) => b.textContent.trim() === "Thêm điều kiện"));
  out.clear = box(buttons.find((b) => b.textContent.trim() === "Xoá hết"));
  out.save = box(buttons.find((b) => b.textContent.trim() === "Lưu bộ lọc"));
  out.done = box(buttons.find((b) => b.textContent.trim() === "Xong"));
  return out;
}`;

async function art(page: Page, file: string): Promise<Boxes> {
  await page.goto(pathToFileURL(`${CANVAS}/${file}`).href);
  await page.waitForTimeout(400);
  return page.evaluate((fn) => {
    const root = document.querySelector(
      '[role="dialog"][aria-label="Lọc việc"]',
    );
    if (!root) throw new Error("no dialog on the artboard");
    return (0, eval)(fn)(root, true);
  }, PROBE) as Promise<Boxes>;
}

async function app(page: Page, dark: boolean): Promise<Boxes> {
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
  await page.goto(`${BOARD}?view=table&filter=${encodeURIComponent(FILTER)}`);
  await page.getByRole("button", { name: /^Lọc/ }).first().click();
  const dialog = page.getByRole("dialog", { name: "Lọc việc" });
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(400);
  return dialog.evaluate(
    (el, fn) => (0, eval)(fn)(el, false),
    PROBE,
  ) as Promise<Boxes>;
}

for (const [mode, file] of [
  ["sang", "WM-Filter-Builder.dc.html"],
  ["dem", "WM-Filter-Dem.dc.html"],
] as const) {
  test(`the filter dialog equals the canvas, ${mode}`, async ({ page }) => {
    test.skip(
      !existsSync(`${CANVAS}/${file}`) ||
        scenario !== "default" ||
        persona !== "owner" ||
        faults !== "",
      "Needs the canvas boards and the plain owner world",
    );
    await page.setViewportSize({ width: 1440, height: 900 });
    const expected = await art(page, file);
    const got = await app(page, mode === "dem");
    expect(got.dialog![2], "dialog width").toBeCloseTo(expected.dialog![2], 0);
    expect(got.dialog![3], "dialog height").toBeCloseTo(expected.dialog![3], 0);
    for (const [key, e] of Object.entries(expected)) {
      if (key === "dialog") continue;
      const a = got[key];
      expect(a, `${key} exists`).toBeTruthy();
      // The lead word and a chip are as wide as their text, which the app's field names do not change.
      if (key.endsWith("lead")) {
        // The word is as tall as its text on the artboard and as tall as the row in the app: its centre is compared.
        expect(a![1] + a![3] / 2, `${key} centre`).toBeCloseTo(
          e[1]! + e[3]! / 2,
          0,
        );
        expect(a![0], `${key} x`).toBeCloseTo(e[0]!, 0);
        continue;
      }
      const skipWidth = key === "title";
      for (let i = 0; i < 4; i++) {
        if (skipWidth && i === 2) continue;
        // The artboard's chip wrapper is the chip's own height; the app's cell is the row's: the chip is compared.
        if (key === "r0value" && (i === 1 || i === 3)) continue;
        expect(a![i], `${key} ${["x", "y", "width", "height"][i]}`).toBeCloseTo(
          e[i]!,
          0,
        );
      }
    }
  });
}
