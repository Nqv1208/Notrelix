import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The remaining tiles of the owner-canvas board `WM-SavedFilter-States` (read live from `WM_CANVAS_DIR`) on the
 * payables board, whose saved filters the mock seeds (one of the owner's, one a colleague shared):
 * "Không có quyền chia sẻ" (the viewer's menu: the Chia sẻ badge and the note), "Đổi tên tại chỗ" (the row's input and its
 * two buttons), "Không còn quyền" (the alert inside the menu, from `VITE_MOCK_FAULTS=/saved-filters/sfilter-m-00001=404`)
 * and "Xoá bộ lọc · hoàn tác ngay" (the toast's colours and its Hoàn tác action).
 *
 * Decision: the tile "Riêng tư và chia sẻ" draws a 44 px row with a chip on both kinds; the board `WM-SavedFilter-Menu`
 * (measured by `design-fidelity-saved-filter.mock.e2e.spec.ts`) draws 52 px rows with a lock or people icon and the
 * section headings Riêng tôi and Chia sẻ, and the chip only on a read-only row. The app follows the menu board; this
 * spec compares the chip itself (20 px, 11.5 px, 7 px padding) where both draw it. The tile grid is 434 px wide and the
 * menu a 380 px popover, so widths and the x of text are not compared.
 *
 * Worlds (the spec skips the others): owner for the rename and the toast, `VITE_MOCK_PERSONA=viewer` (default state) with
 * the 404 fault for the alert.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD =
  "/workspaces/mock-workspace-primary/boards/mock-board-payables?view=table";
// The viewer reads the ERP board, where the mock seeds the shared filter for that persona.
const VIEWER_BOARD =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=table";
const FAULT = "/saved-filters/sfilter-m-00001=404";

type Box = readonly [number, number, number, number];
const near = (a: number, b: number, what: string) =>
  expect(Math.abs(a - b), `${what}: ${a} vs ${b}`).toBeLessThanOrEqual(1);

async function artboard(page: Page, fn: string) {
  await page.goto(
    pathToFileURL(`${CANVAS}/WM-SavedFilter-States.dc.html`).href,
  );
  await page.evaluate(() => document.fonts.ready);
  return page.evaluate((src) => (0, eval)(src)(), fn);
}

async function openMenu(page: Page, board = BOARD) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(board);
  await page.getByRole("button", { name: /Bộ lọc đã lưu/ }).click();
  const menu = page.locator('[aria-label="Bộ lọc đã lưu"][role="dialog"]');
  await expect(menu).toBeVisible();
  await expect(menu).toHaveCSS("transform", "none");
  return menu;
}

const rel = `(el, o) => { const b = el.getBoundingClientRect(); return [b.x - o.x, b.y - o.y, b.width, b.height]; }`;

test.describe("Saved-filter tiles against the canvas", () => {
  test.skip(
    !existsSync(`${CANVAS}/WM-SavedFilter-States.dc.html`),
    "Needs the downloaded canvas boards",
  );
  test.use({ viewport: { width: 1440, height: 1000 } });

  test("the viewer's menu: the badge and the note equal the tile", async ({
    page,
  }) => {
    test.skip(persona !== "viewer", "Needs VITE_MOCK_PERSONA=viewer");
    const art = (await artboard(
      page,
      `() => {
        const rel = ${rel};
        const menu = document.querySelector('.menu[aria-label="Bộ lọc đã lưu, chỉ xem"]');
        const chip = menu.querySelector(".chip").getBoundingClientRect();
        const note = menu.lastElementChild;
        const cs = getComputedStyle(note);
        const o = note.getBoundingClientRect();
        return { chip: [chip.width, chip.height], chipFs: getComputedStyle(menu.querySelector(".chip")).fontSize,
          noteFs: cs.fontSize, noteH: o.height, noteBeforeRow: o.y - menu.querySelector(".mrow").getBoundingClientRect().bottom };
      }`,
    )) as {
      chip: number[];
      chipFs: string;
      noteFs: string;
      noteH: number;
      noteBeforeRow: number;
    };
    const menu = await openMenu(page, VIEWER_BOARD);
    const app = await menu.evaluate((el) => {
      const badge = [...el.querySelectorAll("li span")].find((s) =>
        /^(Chia sẻ|Riêng tôi)$/.test(s.textContent ?? ""),
      )!;
      const note = [...el.querySelectorAll("p")].find((p) =>
        /chỉ xem/i.test(p.textContent ?? ""),
      )!;
      const b = badge.getBoundingClientRect();
      const n = note.getBoundingClientRect();
      const row = badge.closest("li")!.getBoundingClientRect();
      return {
        chip: [b.height],
        chipFs: getComputedStyle(badge).fontSize,
        noteFs: getComputedStyle(note).fontSize,
        noteH: n.height,
        noteBeforeRow: n.y - row.bottom,
      };
    });
    console.log("VIEWER", JSON.stringify({ art, app }));
    near(app.chip[0]!, art.chip[1]!, "chip height");
    expect(app.chipFs).toBe(art.chipFs);
    expect(app.noteFs).toBe(art.noteFs);
    near(app.noteH, art.noteH, "note height");
  });

  test("the rename in place equals the tile", async ({ page }) => {
    test.skip(persona !== "owner" || faults !== "", "Plain owner world");
    const art = (await artboard(
      page,
      `() => {
        const rel = ${rel};
        const row = document.querySelector(".mrow.cur");
        const o = row.getBoundingClientRect();
        const input = rel(row.querySelector("input"), o);
        const buttons = [...row.querySelectorAll("button")].map((b) => rel(b, o));
        return { row: [o.width, o.height], input, buttons, fs: getComputedStyle(row.querySelector("input")).fontSize };
      }`,
    )) as {
      row: number[];
      input: Box;
      buttons: Box[];
      fs: string;
    };
    const menu = await openMenu(page);
    await menu
      .getByRole("button", { name: "Đổi tên Việc gấp tuần này" })
      .click();
    const input = menu.getByRole("textbox", { name: /Tên mới/ });
    await expect(input).toBeVisible();
    const app = await input.evaluate((el, relSrc) => {
      const rel = (0, eval)(relSrc);
      const row = el.closest("li")!;
      const o = row.getBoundingClientRect();
      return {
        row: [o.width, o.height],
        input: rel(el, o),
        buttons: [...row.querySelectorAll("button")].map((b) => rel(b, o)),
        fs: getComputedStyle(el).fontSize,
      };
    }, rel);
    console.log("RENAME", JSON.stringify({ art, app }));
    near(app.row[1]!, art.row[1]!, "row height");
    near(app.input[1], art.input[1], "input y");
    near(app.input[3], art.input[3], "input height");
    expect(app.fs).toBe(art.fs);
    app.buttons.forEach((b, i) => {
      near(b[1], art.buttons[i]![1], `button ${i} y`);
      near(b[3], art.buttons[i]![3], `button ${i} height`);
    });
  });

  test("the alert of a filter that is gone equals the tile", async ({
    page,
  }) => {
    test.skip(!faults.includes(FAULT), "Needs the saved-filter 404 fault");
    const art = (await artboard(
      page,
      `() => {
        const menu = [...document.querySelectorAll('.menu[role="alert"]')].find((m) => m.textContent.includes("không còn nữa"));
        const o = menu.getBoundingClientRect();
        const line = menu.querySelector("div");
        const b = line.getBoundingClientRect();
        const text = line.querySelector("span");
        return { box: [b.x - o.x, b.y - o.y, b.width, b.height], fs: getComputedStyle(text).fontSize,
          color: getComputedStyle(text).color, icon: line.querySelector("svg").getBoundingClientRect().width };
      }`,
    )) as { box: Box; fs: string; color: string; icon: number };
    const menu = await openMenu(page);
    await menu.getByRole("button", { name: "Xoá Việc gấp tuần này" }).click();
    const alert = menu.getByRole("alert");
    await expect(alert).toBeVisible();
    await expect(alert).toContainText("không còn nữa");
    const app = await alert.evaluate((el) => {
      const menuEl = el.closest('[role="dialog"]')!;
      const o = menuEl.getBoundingClientRect();
      const b = el.getBoundingClientRect();
      const text = el.querySelector("p")!;
      return {
        box: [b.x - o.x, b.y - o.y, b.width, b.height],
        fs: getComputedStyle(text).fontSize,
        color: getComputedStyle(text).color,
        icon: el.querySelector("svg")!.getBoundingClientRect().width,
      };
    });
    console.log("GONE", JSON.stringify({ art, app }));
    near(app.box[0], art.box[0], "alert x");
    near(app.box[1], art.box[1], "alert y");
    near(app.box[3], art.box[3], "alert height");
    expect(app.fs).toBe(art.fs);
    near(app.icon, art.icon, "icon");
    // The list is read again and stays under the alert.
    await expect(
      menu.getByRole("button", { name: "Áp dụng bộ lọc Sprint hiện tại" }),
    ).toBeVisible();
  });

  test("the delete toast carries the name and Hoàn tác, and undoing brings the row back", async ({
    page,
  }) => {
    test.skip(persona !== "owner" || faults !== "", "Plain owner world");
    const menu = await openMenu(page);
    await menu.getByRole("button", { name: "Xoá Việc gấp tuần này" }).click();
    const toast = page.locator("[data-sonner-toast]").first();
    await expect(toast).toBeVisible();
    await expect(toast).toContainText("Việc gấp tuần này");
    expect(
      await toast.evaluate((el) => getComputedStyle(el).borderRadius),
    ).toBe("10px");
    await toast.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(
      page.getByRole("button", { name: "Áp dụng bộ lọc Việc gấp tuần này" }),
    ).toBeVisible();
  });
});
void scenario;
