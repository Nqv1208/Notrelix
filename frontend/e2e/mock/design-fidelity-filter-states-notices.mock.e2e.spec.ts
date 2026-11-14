import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The warning of a filter on a removed field, its chip, and the "+N điều kiện" chip of a long bar against the
 * owner-canvas board `WM-Filter-States` (read live from `WM_CANVAS_DIR`; the spec skips when it is missing).
 * The tiles are 434 px cards that wrap their bar, the app's bar is the width of the board, so every box is read against
 * its own parent: the warning box and its two 28 px buttons (the gap between them, the 8 px under the box, the box's own
 * padding), the stale chip (26 px, the 16 px icon, the 18 px remove button on the right) and the "+N" chip (26 px, 10 px
 * padding). The artboard's warning box carries a literal "%s" where its icon should be, so the text's x is not compared
 * (the app draws the 16 px icon and the 8 px gap the box CSS says).
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const FILE = "WM-Filter-States.dc.html";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
// Two real conditions and one on a field the board never had.
const STALE =
  "c2:and~mock-erp-col-priority.has_any.mock-erp-priority-high~mock-erp-col-gone.gte.50";
const LONG =
  "c2:and~mock-erp-col-priority.has_any.mock-erp-priority-high~mock-erp-col-owner.has_any.__none__~mock-erp-col-estimate.gte.1~$title.contains.a~$title.contains.b~$title.contains.c";

type Box = readonly [number, number, number, number];

const NOTICE = `(root, art) => {
  const warn = art ? root.querySelector(".warnbox") : root.querySelector('[role="status"]');
  const o = warn.getBoundingClientRect();
  const box = (el) => { if (!el) return null; const b = el.getBoundingClientRect(); return [b.x - o.x, b.y - o.y, b.width, b.height]; };
  const row = warn.nextElementSibling;
  const r = row.getBoundingClientRect();
  const rbox = (el) => { const b = el.getBoundingClientRect(); return [b.x - r.x, b.y - r.y, b.width, b.height]; };
  const buttons = [...row.querySelectorAll("button")];
  const chip = art ? root.querySelector(".fchip.stale") : document.querySelector('[data-stale]');
  const c = chip.getBoundingClientRect();
  const cbox = (el) => { const b = el.getBoundingClientRect(); return [b.x - c.x, b.y - c.y, b.width, b.height]; };
  const text = warn.querySelector("span");
  return {
    textTop: text.getBoundingClientRect().y - o.y,
    rowGap: row.getBoundingClientRect().y - (o.y + o.height),
    b0: rbox(buttons[0]), b1: rbox(buttons[1]),
    chip: [0, 0, c.width, c.height],
    chipRemove: cbox(chip.querySelector(art ? "i" : "button")),
    chipIcon: cbox(chip.querySelector("svg")),
  };
}`;

async function artboard(page: Page) {
  await page.goto(pathToFileURL(`${CANVAS}/${FILE}`).href);
  await page.waitForTimeout(400);
  return page.evaluate((fn) => {
    const warn = [...document.querySelectorAll(".warnbox")].find((e) =>
      e.textContent?.includes("Việc gấp tuần này"),
    );
    const root = warn!.closest("section.card")!;
    return (0, eval)(fn)(root, true);
  }, NOTICE);
}

async function openApp(page: Page, filter: string) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", "light");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`${BOARD}?view=table&filter=${encodeURIComponent(filter)}`);
  await expect(
    page.getByRole("button", { name: /^Lọc/ }).first(),
  ).toBeVisible();
  await page.waitForTimeout(500);
}

test.describe("Filter notices against the canvas", () => {
  test.skip(
    !existsSync(`${CANVAS}/${FILE}`) ||
      persona !== "owner" ||
      scenario !== "default" ||
      faults !== "",
    "Needs the canvas boards and the plain owner world",
  );

  test("the removed-field warning, its buttons and its chip", async ({
    page,
  }) => {
    const want = (await artboard(page)) as Record<string, Box | number>;
    await openApp(page, STALE);
    const got = (await page.evaluate(
      (fn) => (0, eval)(fn)(document.body, false),
      NOTICE,
    )) as Record<string, Box | number>;
    expect(got.textTop, "text top").toBeCloseTo(want.textTop as number, 0);
    expect(got.rowGap, "gap to the buttons").toBeCloseTo(
      want.rowGap as number,
      0,
    );
    for (const key of ["b0", "b1", "chip", "chipRemove", "chipIcon"]) {
      const e = want[key] as Box;
      const a = got[key] as Box;
      // The chip's own width is its text plus the artboard's longer wording ("≥ 50 triệu · trường đã xoá").
      for (let i = 0; i < 4; i++) {
        if (key === "chip" && i === 2) continue;
        if (key === "chipRemove" && i === 0) continue;
        expect(a[i], `${key} ${["x", "y", "width", "height"][i]}`).toBeCloseTo(
          e[i]!,
          0,
        );
      }
    }
  });

  test("the +N chip of a long bar", async ({ page }) => {
    const want = await (async () => {
      await page.goto(pathToFileURL(`${CANVAS}/${FILE}`).href);
      await page.waitForTimeout(400);
      return page.evaluate(() => {
        const chip = [...document.querySelectorAll(".chip.c-gray")].find((e) =>
          e.textContent?.includes("+3 điều kiện"),
        )!;
        const b = chip.getBoundingClientRect();
        return [b.width, b.height];
      });
    })();
    await openApp(page, LONG);
    const more = page.getByRole("button", { name: /^\+\d+ điều kiện$/ });
    await expect(more).toBeVisible();
    const b = await more.boundingBox();
    expect(b!.height, "+N chip height").toBeCloseTo(want[1]!, 0);
    // "+3 điều kiện" and "+4 điều kiện" are as wide as their text: same digit width.
    expect(b!.width, "+N chip width").toBeCloseTo(want[0]!, 0);
  });
});
