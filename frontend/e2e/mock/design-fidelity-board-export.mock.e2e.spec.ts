import { expect, test, type Locator, type Page } from "@playwright/test";
import frames from "../support/artboards/wm-export.json" with { type: "json" };
import {
  compareDialog,
  type ArtboardFrame,
} from "../support/artboards/compare-dialog";

/**
 * The board export dialog against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `WM-Export`, light and dark. `wm-export.json` holds every text of each frame's 600 px dialog as the artboard
 * renders in Chromium (`extract-artboard.mjs`: box relative to the dialog, font size, weight). Each text must
 * sit within 1 px of the app's box of the same words, with the same font size and weight.
 *
 * Frames measured: 1 (CSV, current view) and 2 (XLSX, whole board), one per mode. The queued, running and ready
 * frames (4 to 6, the mock export job moves through them by age), the empty view (3), expired (7), failed (8)
 * and the 390 px sheets are not measured yet. `KNOWN` is the count of deviations each measured frame still has
 * (the dialog is built on the shared dialog type and spacing: title 14 px, not 17; padding 24, not 20; cards
 * and footer positions follow), so a regression fails and a fix has to lower the number; it is not a pass.
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const all = frames as unknown as ArtboardFrame[];
const frame = (index: number) => all[index]!;

/** The artboard counts items of its own board and says "Triển khai ERP Q4"; the mock world has other data. */
const SKIP: string[] = [];
const KNOWN: Record<string, number> = {
  "1-light": 17,
  "1-dark": 17,
  "2-light": 17,
  "2-dark": 17,
};

async function openDialog(page: Page, dark: boolean): Promise<Locator> {
  await page.setViewportSize({ width: 1440, height: 900 });
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
  await page.getByRole("button", { name: "Thêm tuỳ chọn" }).click();
  await page.getByRole("menuitem", { name: "Xuất việc…" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(400);
  return dialog;
}

async function measure(
  dialog: Locator,
  index: number,
  dark: boolean,
  key: string,
) {
  const variant = frame(index).variants.find(
    (v) => v.mode === (dark ? "dark" : "light"),
  )!;
  const found = await compareDialog(dialog, variant.items, SKIP);
  test.info().annotations.push({
    type: key,
    description: found.map((d) => `${d.text}: ${d.what}`).join(" | "),
  });
  expect(
    found.length,
    `${key}: ${found.map((d) => `${d.text}: ${d.what}`).join("; ")}`,
  ).toBeLessThanOrEqual(KNOWN[key] ?? 0);
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test.describe(`board export ${mode}`, () => {
    test("frame 1: CSV, current view", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await measure(dialog, 0, dark, `1-${mode}`);
    });

    test("frame 2: XLSX, the whole board", async ({ page }) => {
      const dialog = await openDialog(page, dark);
      await dialog.getByRole("radio", { name: /^XLSX/ }).check();
      await dialog.getByRole("radio", { name: /^Tất cả việc/ }).check();
      await measure(dialog, 1, dark, `2-${mode}`);
    });
  });
}
