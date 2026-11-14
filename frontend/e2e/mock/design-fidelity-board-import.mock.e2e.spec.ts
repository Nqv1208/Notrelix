import { expect, test, type Locator, type Page } from "@playwright/test";
import first from "../support/artboards/wm-import-1.json" with { type: "json" };
import second from "../support/artboards/wm-import-2.json" with { type: "json" };
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
 * 5 no data rows; WM-Import-1 6 to 8 are the mapping (all mapped, a column that cannot map, no title column) and
 * WM-Import-2 1 to 8 the check (clean, 12 errors in 9 rows) and the run (archived, 3.200 and 4.900 of 5.000, done,
 * refused at row 3.480, connection lost). The file name picks each story (see the mock handlers).
 *
 * The artboard's file card carries two CSS rules that disagree (a 28 px pill, then a padded card): drawn, its
 * 40 px icon overflows a 28 px box. The app builds the intended 62 px card, so the card's own texts are not
 * compared and what sits under it is compared 34 px lower. The reading frame's "2.400 trên 5.000 dòng" is
 * rows read, which the upload does not report; the app shows the percent read.
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-31T11:00:00+07:00");
const frames = first as unknown as ArtboardFrame[];
const frames2 = second as unknown as ArtboardFrame[];
const SHIFT = { afterY: 120, dy: 34 };
const CARD = [
  "khach-hang-q4.xlsx",
  "Đang đọc… 2.400 trên 5.000 dòng",
  "khach-hang-day-du.xlsx",
  "5.012 dòng · 12 cột · 2,1 MB",
  "mau-trong.csv",
  "0 dòng dữ liệu · 6 cột",
  "Đổi tệp",
  "5.000 dòng · 12 cột · 1,8 MB",
  "“đã bàn giao”, …",
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
  set: ArtboardFrame[] = frames,
) {
  const variant = set[index]!.variants.find(
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

/** Chooses a file and waits for the mapping step. */
async function toMapping(dialog: Locator, name: string) {
  await choose(dialog, name);
  await expect(
    dialog.locator('[aria-current="step"]', { hasText: "Ghép cột" }),
  ).toBeVisible();
  await expect(dialog.getByRole("row").nth(1)).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Tiếp tục" })).toBeVisible();
  await dialog.page().waitForTimeout(500);
}

async function toCheck(dialog: Locator, name: string) {
  await toMapping(dialog, name);
  await dialog.getByRole("button", { name: "Tiếp tục" }).click();
  await expect(
    dialog.locator('[aria-current="step"]', { hasText: "Kiểm tra" }),
  ).toBeVisible();
  await expect(dialog.getByRole("row").nth(1)).toBeVisible();
  await dialog.page().waitForTimeout(500);
}

async function toRun(dialog: Locator, name: string) {
  await toCheck(dialog, name);
  await dialog.getByRole("button", { name: /^Nhập \d/ }).click();
  // The run polls the job: what it shows depends on the story, so give it time to settle.
  await dialog.page().waitForTimeout(1500);
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

    test("frame 6: every column mapped", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toMapping(dialog, "khach-hang-q4.xlsx");
      await measure(dialog, 5, dark, `6-${mode}`, true);
    });

    test("frame 7: a column that cannot map", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toMapping(dialog, "khach-hang-cot-loi.xlsx");
      await measure(dialog, 6, dark, `7-${mode}`, true);
    });

    test("frame 8: no column for the title", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toMapping(dialog, "khach-hang-thieu-ten.xlsx");
      await measure(dialog, 7, dark, `8-${mode}`, true);
    });

    test("check 1: all rows valid", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toCheck(dialog, "khach-hang-q4.xlsx");
      await measure(dialog, 0, dark, `c1-${mode}`, true, frames2);
    });

    test("check 2: 12 errors in 9 rows", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toCheck(dialog, "khach-hang-co-loi.xlsx");
      await measure(dialog, 1, dark, `c2-${mode}`, true, frames2);
    });

    test("run 3: the board was just archived", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toRun(dialog, "khach-hang-luu-tru.xlsx");
      await measure(dialog, 2, dark, `c3-${mode}`, true, frames2);
    });

    test("run 4: 3.200 of 5.000", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toRun(dialog, "khach-hang-dang-tao.xlsx");
      await measure(dialog, 3, dark, `c4-${mode}`, true, frames2);
    });

    test("run 5: 4.900 of 5.000", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toRun(dialog, "khach-hang-gan-xong.xlsx");
      await measure(dialog, 4, dark, `c5-${mode}`, true, frames2);
    });

    test("run 6: imported", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toRun(dialog, "khach-hang-q4.xlsx");
      await measure(dialog, 5, dark, `c6-${mode}`, true, frames2);
    });

    test("run 7: refused at row 3.480", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toRun(dialog, "khach-hang-bi-tu-choi.xlsx");
      await measure(dialog, 6, dark, `c7-${mode}`, true, frames2);
    });

    test("run 8: connection lost", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await toCheck(dialog, "khach-hang-mat-mang.xlsx");
      await dialog.getByRole("button", { name: /^Nhập \d/ }).click();
      await expect(dialog.getByText("Mất kết nối giữa chừng")).toBeVisible();
      await page.waitForTimeout(500);
      await measure(dialog, 7, dark, `c8-${mode}`, true, frames2);
    });
  });
}
