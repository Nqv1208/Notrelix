import { expect, test, type Locator, type Page } from "@playwright/test";
import sheets from "../support/artboards/wm-import-390.json" with { type: "json" };
import {
  compareDialog,
  type ArtboardFrame,
} from "../support/artboards/compare-dialog";

/**
 * The board import sheet at 390 px against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x),
 * artboards `WM-Import-1` and `WM-Import-2`, light and dark. `wm-import-390.json` holds every text of each
 * 388 px sheet as the artboard renders in Chromium (`extract-artboard.mjs`, the frame has a 1 px border);
 * each must sit within 1 px of the app's box of the same words. Frames: file step, over 5.000 rows, a column
 * that cannot map, 12 errors in 9 rows, running (3.200 of 5.000) and refused at row 3.480. The file NAME picks
 * what the mock server finds (`board-import.handlers.ts`).
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-31T11:00:00+07:00");
const frames = sheets as unknown as ArtboardFrame[];

async function openSheet(page: Page, dark: boolean): Promise<Locator> {
  await page.setViewportSize({ width: 388, height: 1200 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode as string);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
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

async function step(dialog: Locator, label: string) {
  await expect(
    dialog.locator('[aria-current="step"]', { hasText: label }),
  ).toBeVisible();
  await dialog.page().waitForTimeout(500);
}

// The artboard's file card carries two CSS rules that disagree (as at 1440): its own texts are not compared,
// and what sits under it is compared `dy` lower. The mock names the file by the upload.
const CARD = [
  "khach-hang-q4.xlsx",
  "khach-hang-day-du.xlsx",
  "5.000 dòng · 12 cột · 1,8 MB",
  "5.012 dòng · 12 cột · 2,1 MB",
  "Đổi tệp",
];

async function measure(
  dialog: Locator,
  index: number,
  dark: boolean,
  key: string,
  dy = 0,
) {
  const variant = frames[index]!.variants.find(
    (v) => v.mode === (dark ? "dark" : "light"),
  )!;
  const found = await compareDialog(
    dialog,
    variant.items,
    dy ? CARD : [],
    Number(process.env.TOL ?? 1),
    dy ? { afterY: 160, dy } : undefined,
  );
  test.info().annotations.push({
    type: key,
    description: found.map((d) => `${d.text}: ${d.what}`).join(" | "),
  });
  expect(
    found.length,
    `${key}: ${found.map((d) => `${d.text}: ${d.what}`).join("; ")}`,
  ).toBe(0);
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test.describe(`board import sheet 390 ${mode}`, () => {
    test("file step", async ({ page }) => {
      const dialog = await openSheet(page, dark);
      await measure(dialog, 0, dark, `s1-${mode}`);
    });

    test("over 5.000 rows", async ({ page }) => {
      const dialog = await openSheet(page, dark);
      await choose(dialog, "khach-hang-day-du.xlsx");
      await expect(
        dialog.getByText("Tệp có 5.012 dòng, vượt giới hạn 5.000 dòng"),
      ).toBeVisible();
      await measure(dialog, 1, dark, `s2-${mode}`, 38);
    });

    test("a column that cannot map", async ({ page }) => {
      const dialog = await openSheet(page, dark);
      await choose(dialog, "khach-hang-cot-loi-gon.xlsx");
      await step(dialog, "Ghép cột");
      await measure(dialog, 2, dark, `s3-${mode}`, 38);
    });

    test("12 errors in 9 rows", async ({ page }) => {
      const dialog = await openSheet(page, dark);
      await choose(dialog, "khach-hang-co-loi.xlsx");
      await step(dialog, "Ghép cột");
      await dialog.getByRole("button", { name: "Tiếp tục" }).click();
      await step(dialog, "Kiểm tra");
      await measure(dialog, 3, dark, `s4-${mode}`, 38);
    });

    test("running", async ({ page }) => {
      const dialog = await openSheet(page, dark);
      await choose(dialog, "khach-hang-dang-tao.xlsx");
      await step(dialog, "Ghép cột");
      await dialog.getByRole("button", { name: "Tiếp tục" }).click();
      await step(dialog, "Kiểm tra");
      await dialog.getByRole("button", { name: /^Nhập \d/ }).click();
      await dialog.page().waitForTimeout(1500);
      await measure(dialog, 4, dark, `s5-${mode}`, 34);
    });

    test("refused at row 3.480", async ({ page }) => {
      const dialog = await openSheet(page, dark);
      await choose(dialog, "khach-hang-bi-tu-choi.xlsx");
      await step(dialog, "Ghép cột");
      await dialog.getByRole("button", { name: "Tiếp tục" }).click();
      await step(dialog, "Kiểm tra");
      await dialog.getByRole("button", { name: /^Nhập \d/ }).click();
      await dialog.page().waitForTimeout(1500);
      await measure(dialog, 5, dark, `s6-${mode}`, 34);
    });
  });
}
