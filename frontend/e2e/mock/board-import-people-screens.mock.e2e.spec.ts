import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * UC-WM-B29: the board import states that no artboard draws, reviewed on screen. The Person column matches a
 * person only by the exact e-mail of a member who can be assigned on the board, and only an Owner or Admin
 * imports people (product decision recorded in the use case). Three states:
 *
 * - the check step with person cells that matched no member (the note lists the rows, eight at most);
 * - the result with the same note;
 * - the mapping step for an importer who is not an Owner or Admin (the Person field is locked, with the reason).
 *
 * Each is checked in Vietnamese, light and dark, at 1440, 1024 and 390. At every size it must stay inside the
 * window (no sideways scroll, nothing clipped at the right edge) and report no serious axe violation. At 390
 * every control of the sheet must be at least 44 px in both directions. The words are asserted too.
 * Screenshots go to `test-results/board-import-people` to be read; they are not baselines. The uploaded file's
 * NAME picks the story in the mock (`board-import.handlers.ts`).
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOTE =
  "Có người chưa khớp: 10 dòng (3, 7, 12, 40, 41, 42, 43, 44…). Ô đó để trống. Chỉ khớp theo đúng e-mail của thành viên có thể được giao việc trên board này.";
const LOCKED =
  "Người phụ trách: Chỉ chủ sở hữu và quản trị viên nhập được người";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Owner checks need the default owner world",
);

async function openDialog(page: Page, width: number, mode: string) {
  await page.setViewportSize({ width, height: 900 });
  await page.clock.setFixedTime(new Date("2026-10-31T11:00:00+07:00"));
  await page.addInitScript((theme) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", theme);
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode);
  await page.goto(`${BOARD}?view=table`);
  await page.getByRole("button", { name: "Thêm tuỳ chọn" }).click();
  await page.getByRole("menuitem", { name: "Nhập việc từ tệp…" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  return dialog;
}

async function toMapping(dialog: Locator, name: string) {
  await dialog.locator('input[type="file"]').setInputFiles({
    name,
    mimeType: "application/octet-stream",
    buffer: Buffer.from("a,b\n1,2\n"),
  });
  await expect(
    dialog.locator('[aria-current="step"]', { hasText: "Ghép cột" }),
  ).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Tiếp tục" })).toBeVisible();
}

async function toCheck(dialog: Locator, name: string) {
  await toMapping(dialog, name);
  await dialog.getByRole("button", { name: "Tiếp tục" }).click();
  await expect(
    dialog.locator('[aria-current="step"]', { hasText: "Kiểm tra" }),
  ).toBeVisible();
}

const SCREENS = {
  "check-people-unmatched": async (dialog: Locator) => {
    await toCheck(dialog, "khach-hang-nguoi-chua-khop.xlsx");
    await expect(dialog.getByText(NOTE)).toBeVisible();
  },
  "result-people-unmatched": async (dialog: Locator) => {
    await toCheck(dialog, "khach-hang-nguoi-chua-khop.xlsx");
    await dialog.getByRole("button", { name: /^Nhập \d/ }).click();
    await expect(dialog.getByText(NOTE)).toBeVisible();
  },
  "mapping-person-locked": async (dialog: Locator) => {
    await toMapping(dialog, "khach-hang-nguoi-khoa.xlsx");
    await expect(dialog.getByText(LOCKED)).toBeVisible();
  },
} as const;

for (const [name, show] of Object.entries(SCREENS)) {
  for (const width of [1440, 1024, 390]) {
    for (const mode of ["light", "dark"]) {
      test(`${name} at ${width} ${mode}: inside the window, no serious axe violation${width === 390 ? ", 44 px controls" : ""}`, async ({
        page,
      }) => {
        const dialog = await openDialog(page, width, mode);
        await show(dialog);
        await page.waitForTimeout(400);
        await page.screenshot({
          path: `test-results/board-import-people/${name}-${width}-${mode}.png`,
        });

        expect(
          await page.evaluate(
            () => document.documentElement.scrollWidth - window.innerWidth,
          ),
          "sideways scroll",
        ).toBeLessThanOrEqual(0);
        const clipped = await page.evaluate(() => {
          const limit = window.innerWidth;
          return [...document.querySelectorAll<HTMLElement>("[role=dialog] *")]
            .filter((node) => {
              const rect = node.getBoundingClientRect();
              return (
                rect.width > 0 &&
                getComputedStyle(node).visibility !== "hidden" &&
                rect.right > limit + 1 &&
                !node.closest("[aria-hidden=true]") &&
                // A region that scrolls sideways by design (a wide table) clips on purpose.
                !(() => {
                  for (
                    let up: HTMLElement | null = node.parentElement;
                    up;
                    up = up.parentElement
                  ) {
                    const overflowX = getComputedStyle(up).overflowX;
                    if (overflowX === "auto" || overflowX === "scroll")
                      return true;
                  }
                  return false;
                })()
              );
            })
            .slice(0, 5)
            .map(
              (node) =>
                `${node.tagName.toLowerCase()} ${Math.round(node.getBoundingClientRect().right)}`,
            );
        });
        expect(clipped, "elements past the right edge").toEqual([]);

        const violations = (
          await new AxeBuilder({ page }).include("[role=dialog]").analyze()
        ).violations.filter((v) =>
          ["serious", "critical"].includes(v.impact ?? ""),
        );
        expect(violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);

        if (width === 390) {
          const small = await page.evaluate(() =>
            [
              ...document.querySelectorAll<HTMLElement>(
                "[role=dialog] :is(button, a[href], input:not([type=hidden]):not([type=file]), select, textarea)",
              ),
            ]
              .filter((node) => {
                const rect = node.getBoundingClientRect();
                return (
                  rect.width > 0 &&
                  getComputedStyle(node).visibility !== "hidden" &&
                  !node.closest("[aria-hidden=true]") &&
                  (rect.width < 44 || rect.height < 44)
                );
              })
              .slice(0, 8)
              .map((node) => {
                const rect = node.getBoundingClientRect();
                return `${node.tagName.toLowerCase()}[${(node.getAttribute("aria-label") ?? node.textContent ?? "").trim().slice(0, 24)}] ${Math.round(rect.width)}x${Math.round(rect.height)}`;
              }),
          );
          expect(small, "controls under 44 px").toEqual([]);
        }
      });
    }
  }
}
