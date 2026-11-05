import { expect, test, type Locator, type Page } from "@playwright/test";
import first from "../support/artboards/wm-import-1.json" with { type: "json" };
import {
  compareDialog,
  type ArtboardFrame,
} from "../support/artboards/compare-dialog";

/**
 * The board import dialog against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `WM-Import-1`, light and dark: the file step. `wm-import-1.json` holds every text of each frame's 880 px
 * dialog as the artboard renders in Chromium (`extract-artboard.mjs`); each must sit within 1 px of the app's
 * box of the same words, with the same font size and weight.
 *
 * The mock reads no files: the uploaded file's NAME picks what the server finds (`mock-backend`
 * `board-import.handlers.ts`). Frames measured here: 1 default, 2 reading, 3 PDF refused, 4 over the row limit,
 * 5 no data rows. The mapping, check and run frames (WM-Import-1 6 to 8, WM-Import-2) are not measured yet.
 *
 * The artboard's file card carries two CSS rules that disagree (a 28 px pill, then a padded card): drawn, its
 * 40 px icon overflows a 28 px box. The app builds the intended 62 px card, so the card's own texts are not
 * compared and what sits under it is compared 34 px lower. The reading frame's "2.400 trên 5.000 dòng" is
 * rows read, which the upload does not report; the app shows the percent read.
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-31T11:00:00+07:00");
const frames = first as unknown as ArtboardFrame[];
const SHIFT = { afterY: 120, dy: 34 };
const CARD = [
  "khach-hang-q4.xlsx",
  "Đang đọc… 2.400 trên 5.000 dòng",
  "khach-hang-day-du.xlsx",
  "5.012 dòng · 12 cột · 2,1 MB",
  "mau-trong.csv",
  "0 dòng dữ liệu · 6 cột",
  "Đổi tệp",
];

async function openDialog(
  page: Page,
  dark: boolean,
  hold = false,
): Promise<Locator> {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ([mode, held]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode as string);
      } catch {
        // Private mode: the defaults apply.
      }
      if (held) (globalThis as Record<string, unknown>).__mockImportHold = true;
    },
    [dark ? "dark" : "light", hold],
  );
  await page.goto(`${BOARD}?view=table`);
  await page.getByRole("button", { name: "Thêm tuỳ chọn" }).click();
  await page.getByRole("menuitem", { name: "Nhập việc từ tệp…" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(400);
  return dialog;
}

async function choose(dialog: Locator, name: string) {
  await dialog.locator('input[type="file"]').setInputFiles({
    name,
    mimeType: "application/octet-stream",
    buffer: Buffer.from("a,b\n1,2\n"),
  });
}

async function measure(
  dialog: Locator,
  index: number,
  dark: boolean,
  key: string,
  card = false,
) {
  const variant = frames[index]!.variants.find(
    (v) => v.mode === (dark ? "dark" : "light"),
  )!;
  const found = await compareDialog(
    dialog,
    variant.items,
    card ? CARD : [],
    1,
    card ? SHIFT : undefined,
  );
  expect(
    found.length,
    `${key}: ${found.map((d) => `${d.text}: ${d.what}`).join("; ")}`,
  ).toBe(0);
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test.describe(`board import ${mode}`, () => {
    test("frame 1: drop or choose a file", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await measure(dialog, 0, dark, `1-${mode}`);
    });

    test("frame 2: reading the file", async ({ page }) => {
      const dialog = await openDialog(page, dark, true);
      await choose(dialog, "khach-hang-q4.xlsx");
      await expect(dialog.getByText("Đang đọc…")).toBeVisible();
      await measure(dialog, 1, dark, `2-${mode}`, true);
    });

    test("frame 3: a PDF is refused", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await choose(dialog, "bao-cao-q4.pdf");
      await expect(dialog.getByRole("alert")).toBeVisible();
      await measure(dialog, 2, dark, `3-${mode}`);
    });

    test("frame 4: over 5.000 rows", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await choose(dialog, "khach-hang-day-du.xlsx");
      await expect(
        dialog.getByText("Tệp có 5.012 dòng, vượt giới hạn 5.000 dòng"),
      ).toBeVisible();
      await measure(dialog, 3, dark, `4-${mode}`, true);
    });

    test("frame 5: no data rows", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await choose(dialog, "tep-rong.csv");
      await expect(dialog.getByText("Tệp chỉ có dòng tên cột")).toBeVisible();
      await measure(dialog, 4, dark, `5-${mode}`, true);
    });
  });
}
