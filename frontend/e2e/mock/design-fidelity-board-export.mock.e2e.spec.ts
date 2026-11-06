import { expect, test, type Locator, type Page } from "@playwright/test";
import frames from "../support/artboards/wm-export.json" with { type: "json" };
import sheets from "../support/artboards/wm-export-390.json" with { type: "json" };
import {
  compareDialog,
  type ArtboardFrame,
} from "../support/artboards/compare-dialog";

/**
 * The board export dialog against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `WM-Export`, light and dark. `wm-export.json` holds every text of each frame's 600 px dialog and
 * `wm-export-390.json` the three 390 px sheets, as the artboard renders in Chromium (`extract-artboard.mjs`:
 * box relative to the dialog, font size, weight). Each text must sit within 1 px of the app's box of the same
 * words, with the same font size and weight; nothing is held back.
 *
 * The mock export job is held in the state a frame draws (`__mockExportStage`) with the artboard's file.
 * `SKIP` lists the texts the contract cannot say: the artboard's ready note counts the file's columns and the
 * failed note names the row the server stopped at; the export job carries neither.
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-31T11:00:00+07:00");
const all = frames as unknown as ArtboardFrame[];
const phone = sheets as unknown as ArtboardFrame[];

const SKIP = [
  // The mock board has no "Phân hệ ERP" filter; the view carries its own chip (a filter on Ưu tiên), so the
  // chip's words and width are the mock world's. Its row (the 22 px chip, 6 px gap) is in the layout below it.
  "Phân hệ ERP là Kế toán",
  "Có 5.000 dòng, 11 cột. Liên kết tải hết hạn sau 24 giờ.",
  "Máy chủ dừng ở dòng 3.120. Chưa có tệp nào để tải và board không đổi. Thử lại không tạo hai tệp.",
];
/**
 * The artboard's file card carries two CSS rules that disagree (a 28 px pill, then a padded card): drawn, its
 * 40 px icon overflows a 28 px box. The app builds the intended 62 px card, so the card's own texts are not
 * compared and what sits under it is compared 33 px lower.
 */
const CARD = [
  "Triển khai ERP Q4 - 31-10-2026.xlsx",
  "XLSX · 1,2 MB · tạo lúc 10:42",
  "Triển khai ERP Q4 - 30-10-2026.xlsx",
  "XLSX · 1,2 MB · hết hạn 31/10 10:42",
];
const CARD_SHIFT = { afterY: 120, dy: 33 };

type Stage = "queued" | "running" | "ready" | "expired" | "failed";

async function openDialog(
  page: Page,
  dark: boolean,
  width: number,
  options: { viewCount?: number; stage?: Stage; plain?: boolean } = {},
): Promise<Locator> {
  // The artboard's 390 px frame has a 1 px border, so its sheet is 388 px wide.
  await page.setViewportSize({ width: width < 600 ? 388 : width, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ([mode, view, stage]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode as string);
      } catch {
        // Private mode: the defaults apply.
      }
      const held = globalThis as Record<string, unknown>;
      if (typeof view === "number") held.__mockExportViewCount = view;
      if (typeof stage === "string") held.__mockExportStage = stage;
    },
    [dark ? "dark" : "light", options.viewCount ?? null, options.stage ?? null],
  );
  await page.goto(
    `${BOARD}?view=table${options.plain ? "" : "&filter=p.high"}`,
  );
  const more = page.getByRole("button", { name: "Thêm tuỳ chọn" });
  await more.click();
  await page.getByRole("menuitem", { name: "Xuất việc…" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(400);
  return dialog;
}

async function measure(
  dialog: Locator,
  set: ArtboardFrame[],
  index: number,
  dark: boolean,
  key: string,
  skip: readonly string[] = [],
  shift?: { afterY: number; dy: number },
) {
  const variant = set[index]!.variants.find(
    (v) => v.mode === (dark ? "dark" : "light"),
  )!;
  const found = await compareDialog(
    dialog,
    variant.items,
    [...SKIP, ...skip],
    1,
    shift,
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

async function everythingXlsx(dialog: Locator) {
  await dialog.getByRole("radio", { name: /^XLSX/ }).check();
  await dialog.getByRole("radio", { name: /^Tất cả việc/ }).check();
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test.describe(`board export ${mode}`, () => {
    test("frame 1: CSV, current view", async ({ page }) => {
      const dialog = await openDialog(page, dark, 1440);
      await measure(dialog, all, 0, dark, `1-${mode}`);
    });

    test("frame 2: XLSX, the whole board", async ({ page }) => {
      const dialog = await openDialog(page, dark, 1440);
      await everythingXlsx(dialog);
      await measure(dialog, all, 1, dark, `2-${mode}`);
    });

    test("frame 3: a view with no items, the button locked with its reason", async ({
      page,
    }) => {
      const dialog = await openDialog(page, dark, 1440, {
        viewCount: 0,
        plain: true,
      });
      await expect(
        dialog.getByText("Chế độ xem hiện tại không có việc nào để xuất"),
      ).toBeVisible();
      await measure(dialog, all, 2, dark, `3-${mode}`);
    });

    const stages: [number, Stage, string][] = [
      [3, "queued", "Đang xếp hàng để tạo tệp"],
      [4, "running", "Đang tạo tệp: 2.400 trên 5.000 dòng"],
      [5, "ready", "Tệp đã sẵn sàng"],
      [6, "expired", "Tệp đã hết hạn"],
      [7, "failed", "Chưa tạo được tệp"],
    ];
    for (const [index, stage, text] of stages) {
      test(`frame ${index + 1}: ${stage}`, async ({ page }) => {
        const dialog = await openDialog(page, dark, 1440, { stage });
        await everythingXlsx(dialog);
        await dialog.getByRole("button", { name: /^Xuất/ }).click();
        await expect(dialog.getByText(text, { exact: true })).toBeVisible();
        await page.waitForTimeout(300);
        await measure(
          dialog,
          all,
          index,
          dark,
          `${index + 1}-${mode}`,
          stage === "ready" || stage === "expired" ? CARD : [],
          stage === "ready" || stage === "expired" ? CARD_SHIFT : undefined,
        );
      });
    }

    test("sheet 390: the default frame", async ({ page }) => {
      const dialog = await openDialog(page, dark, 390);
      await measure(dialog, phone, 0, dark, `s1-${mode}`);
    });

    test("sheet 390: running", async ({ page }) => {
      const dialog = await openDialog(page, dark, 390, { stage: "running" });
      await everythingXlsx(dialog);
      await dialog.getByRole("button", { name: /^Xuất/ }).click();
      await expect(
        dialog.getByText("Đang tạo tệp: 2.400 trên 5.000 dòng"),
      ).toBeVisible();
      await measure(dialog, phone, 1, dark, `s2-${mode}`);
    });

    test("sheet 390: ready", async ({ page }) => {
      const dialog = await openDialog(page, dark, 390, { stage: "ready" });
      await everythingXlsx(dialog);
      await dialog.getByRole("button", { name: /^Xuất/ }).click();
      await expect(dialog.getByText("Tệp đã sẵn sàng")).toBeVisible();
      await measure(dialog, phone, 2, dark, `s3-${mode}`, CARD, {
        afterY: 150,
        dy: 19.8,
      });
    });
  });
}
