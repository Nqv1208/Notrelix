import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * Screenshot review of the Archived items and Deleted items panels where the canvas draws nothing (1024 px and
 * 390 px; `BC-Archive-Trash` draws 440 px beside the board and 320 px full screen), in Vietnamese, light and dark:
 * the panel stays inside the window with no sideways scroll, nothing inside it overflows, every control is 44 px
 * high at 390 px, the search of the Deleted items finds an item by name and by key, and axe reports no serious or
 * critical violation. `WM_SHOTS_DIR` keeps a screenshot of each for the reviewer.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, which: "archive" | "trash", dark: boolean) {
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
  await page
    .getByRole("menuitem", {
      name: which === "archive" ? "Việc đã lưu trữ" : "Việc đã xoá",
    })
    .click();
  const panel = page.getByRole("dialog", {
    name: which === "archive" ? /^Việc đã lưu trữ/ : /^Việc đã xoá/,
  });
  await expect(panel).toBeVisible();
  await page.waitForTimeout(500);
  return panel;
}

test.describe("Archived and Deleted items panels at 1024 and 390 px", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      latency !== "" ||
      overlays !== "",
    "Needs the plain owner world",
  );

  for (const width of [1024, 390]) {
    for (const dark of [false, true]) {
      for (const which of ["archive", "trash"] as const) {
        test(`${which} at ${width} px, ${dark ? "dark" : "light"}: inside the window, no overflow, 44 px targets, axe clean`, async ({
          page,
        }) => {
          await page.setViewportSize({ width, height: 800 });
          const panel = await open(page, which, dark);
          const box = (await panel.boundingBox())!;
          expect(box.x + box.width).toBeLessThanOrEqual(width + 0.5);
          expect(box.width).toBeLessThanOrEqual(width === 390 ? 390 : 440.5);
          if (width === 390) expect(box.width).toBeGreaterThanOrEqual(389.5);
          // No sideways scroll of the page or of the panel's own list.
          expect(
            await page.evaluate(
              () => document.documentElement.scrollWidth <= window.innerWidth,
            ),
          ).toBe(true);
          expect(
            await panel.evaluate((node) =>
              [...node.querySelectorAll("*")].every(
                (el) => el.scrollWidth <= el.clientWidth + 1,
              ),
            ),
          ).toBe(true);
          if (width === 390) {
            for (const control of await panel.locator("button, label").all()) {
              const b = (await control.boundingBox())!;
              expect(b.height, "a control under 44 px").toBeGreaterThanOrEqual(
                43.5,
              );
            }
          }
          if (shots)
            await page.screenshot({
              path: `${shots}/${which}-${width}-${dark ? "dark" : "light"}.png`,
            });
          const results = await new AxeBuilder({ page })
            .include('[role="dialog"]')
            .analyze();
          expect(
            results.violations.filter((v) =>
              ["serious", "critical"].includes(v.impact ?? ""),
            ),
          ).toEqual([]);
        });
      }
    }
  }

  test("the Deleted items search finds an item by name and by key, and says so when nothing matches", async ({
    page,
  }) => {
    const panel = await open(page, "trash", false);
    const search = panel.getByRole("searchbox");
    await search.fill("số dư");
    await expect(panel.getByRole("listitem")).toHaveCount(1);
    await search.fill("erp-121");
    await expect(panel.getByRole("listitem")).toHaveCount(1);
    await search.fill("kho lạnh");
    await expect(panel.getByText("Không có kết quả")).toBeVisible();
    await expect(panel.getByRole("searchbox")).toBeVisible();
  });
});
