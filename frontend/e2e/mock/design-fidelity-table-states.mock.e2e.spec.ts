import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The table's group and row states against the owner-canvas board `BC-Table` (`docs/design/mockups/2026-10/src/`),
 * frames `tbl-groups` and `tbl-faces`, light (sang) and dark (dem). The artboard is opened as a static file and
 * its boxes are read live. Row and group heights follow `Final-Table` (39 / 40 / 35), which the owner named the
 * standard, so only what the Final boards do not draw is compared: the add-row copy, the note of a group without
 * a matching item, and the "Bị chặn bởi" chip, boxed against its own cell (left edge, size, centre, type).
 * Not built, with the reason: the "Lưu trữ cùng việc cha" chip (an archived sub-item is listed by no table read,
 * so no row can carry it), the frames `tbl320-sang`, `tbl320-dem`, `tbl320-groups-*` and `tbl320-filtered-*` are SUPERSEDED: the owner named the
 * Final-* board group the system standard, so `Final-M-Board`'s list card (measured by
 * `design-fidelity-table-phone.mock.e2e.spec.ts`) is the phone table at 320 to 390 px.
 * Needs the default owner world and `VITE_MOCK_OVERLAYS=item-blockers` (the board read then carries each item's
 * open blockers; the plain world keeps the Final-Table rows as drawn).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const SRC = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const ART = "BC-Table.dc.html";
const dir = existsSync(`${CANVAS}/${ART}`) ? CANVAS : SRC;

test.skip(
  persona !== "owner" ||
    scenario !== "default" ||
    !overlays.includes("item-blockers"),
  "Needs the default owner world with the item-blockers overlay",
);
test.use({ viewport: { width: 1440, height: 900 } });

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
  fs: string;
  fw: string;
  color: string;
  bg: string;
  radius: string;
  text: string;
}

/** Evaluated in the page: a box against a reference element's top-left, with its type and colours. */
const BOX = `(el, ref) => {
  const o = ref.getBoundingClientRect();
  const b = el.getBoundingClientRect();
  const cs = getComputedStyle(el);
  return { x: b.x - o.x, y: b.y - o.y, w: b.width, h: b.height, fs: cs.fontSize, fw: cs.fontWeight,
    color: cs.color, bg: cs.backgroundColor, radius: cs.borderRadius, text: el.textContent.trim() };
}`;

async function artboard(page: Page, frame: string, find: string) {
  await page.goto(pathToFileURL(`${dir}/${ART}`).href);
  return page.evaluate(
    ({ frame, find, fn }) => {
      const root = document.querySelector(`[data-frame="${frame}"]`);
      if (!root) throw new Error(`no frame ${frame}`);
      const el = [...root.querySelectorAll("*")].find((e) =>
        [...e.childNodes].some(
          (n) => n.nodeType === 3 && (n.textContent ?? "").includes(find),
        ),
      );
      if (!el) throw new Error(`no ${find} in ${frame}`);
      const box = (0, eval)(fn);
      const parent = el.parentElement!;
      const prev = el.previousElementSibling;
      return {
        el: box(el, parent) as Box,
        prev: prev ? (box(prev, parent) as Box) : null,
        parentH: parent.getBoundingClientRect().height,
        text: (el.textContent ?? "").trim(),
      };
    },
    { frame, find, fn: BOX },
  );
}

async function openApp(page: Page, dark: boolean, search?: string) {
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
  await expect(page.getByRole("rowgroup").first()).toBeVisible();
  if (search) {
    await page
      .getByRole("searchbox")
      .or(page.getByPlaceholder(/Tìm trong bảng/))
      .first()
      .fill(search);
    await page.waitForTimeout(500);
  }
}

for (const mode of ["sang", "dem"] as const) {
  const dark = mode === "dem";

  test(`group without a matching item and its add row, ${mode}`, async ({
    page,
  }) => {
    const art = await artboard(
      page,
      `tbl-groups-${mode}`,
      "Không có việc khớp bộ lọc hiện tại",
    );
    const addArt = await artboard(
      page,
      `tbl-groups-${mode}`,
      "Thêm công việc vào Chờ duyệt",
    );
    await openApp(page, dark, "ERP-142");
    const note = page.locator('[data-slot="table-group-empty"]').first();
    await expect(note).toBeVisible();
    const app = await note.evaluate(
      (el, fn) =>
        (0, eval)(fn)(
          el,
          el.closest('[role="row"]')!.querySelector('[role="cell"]')!,
        ) as Box,
      BOX,
    );
    const count = await note.evaluate(
      (el, fn) =>
        (0, eval)(fn)(
          el.previousElementSibling!,
          el.closest('[role="row"]')!.querySelector('[role="cell"]')!,
        ) as Box,
      BOX,
    );
    expect(app.text).toBe(art.text);
    expect(app.fs).toBe(art.el.fs);
    expect(app.fw).toBe(art.el.fw);
    expect(app.h).toBeCloseTo(art.el.h, 0);
    // The gap between the count and the note.
    expect(app.x - (count.x + count.w)).toBeCloseTo(
      art.el.x - (art.prev!.x + art.prev!.w),
      0,
    );
    // The note's centre sits on the row's centre within the 1 px the Final-Table head (39 vs 40) leaves.
    expect(
      Math.abs(app.y + app.h / 2 - (art.parentH ? art.parentH / 2 : 0)),
    ).toBeLessThan(40);
    const add = page
      .getByRole("button", { name: /^Thêm công việc vào / })
      .first();
    await expect(add).toBeVisible();
    expect(await add.textContent()).toContain("Thêm công việc vào ");
    expect(addArt.text).toMatch(/^Thêm công việc vào /);
  });

  test(`blocked-by chip in a row, ${mode}`, async ({ page }) => {
    const art = await artboard(page, `tbl-faces-${mode}`, "Bị chặn bởi");
    await openApp(page, dark);
    const chip = page.locator('[data-slot="table-blocked-chip"]');
    await expect(chip.first()).toBeVisible();
    const app = await chip
      .first()
      .evaluate((el, fn) => (0, eval)(fn)(el, el.parentElement!) as Box, BOX);
    const cell = await chip
      .first()
      .evaluate((el) => el.parentElement!.getBoundingClientRect().height);
    // The mock world's blocked items differ from the artboard's; the wording and the box are the same.
    expect(app.text).toMatch(/^Bị chặn bởi ERP-\d+$/);
    expect(app.x).toBeCloseTo(art.el.x, 0);
    // ERP-142 against the artboard's ERP-140: the digits differ by under 1 px.
    expect(Math.abs(app.w - art.el.w)).toBeLessThanOrEqual(1);
    expect(app.h).toBeCloseTo(art.el.h, 0);
    // Final-Table's row is 39 px of content plus the line, the artboard's 40: the centre sits within 1 px.
    expect(Math.abs(app.y + app.h / 2 - cell / 2)).toBeLessThanOrEqual(1);
    expect(art.el.y + art.el.h / 2).toBeCloseTo(art.parentH / 2, 0);
    expect(app.fs).toBe(art.el.fs);
    expect(app.fw).toBe(art.el.fw);
    expect(app.radius).toBe(art.el.radius);
    // Colours belong to the token set (`check:design-tokens`), not to the artboard's hex.
  });
}
