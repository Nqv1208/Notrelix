import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

/**
 * The page a person cannot open (no access, or no such page: the same answer,
 * GOV-025) in the mock world. Final-Docs-2 draws no such state, so it is
 * checked at the three widths in both themes: it fits the viewport, nothing
 * scrolls sideways, and axe finds nothing serious.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const GONE = "/workspaces/mock-workspace-primary/docs/not-a-page-of-this-world";
const THEME_KEY = "notrelix-theme-mode";

test.describe("Document page a person cannot open", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Owner checks need the default owner world",
  );
  for (const width of [1440, 1024, 390]) {
    for (const theme of ["light", "dark"]) {
      test(`${width} px, ${theme}: says the page is not there and fits`, async ({
        page,
      }, testInfo) => {
        await page.setViewportSize({ width, height: 900 });
        await page.addInitScript(
          ({ key, mode }) => {
            try {
              window.localStorage.setItem("notrelix-locale", "vi");
              window.localStorage.setItem(key, mode);
            } catch {
              // Private mode: the defaults apply.
            }
          },
          { key: THEME_KEY, mode: theme },
        );
        await page.goto(GONE);
        const note = page.getByText(
          "Không tìm thấy tài liệu hoặc bạn không có quyền xem.",
        );
        await expect(note).toBeVisible();
        await expect(
          page.getByRole("heading", { name: "Không mở được tài liệu" }),
        ).toBeVisible();
        const back = page.getByRole("button", { name: "Về trang chủ" });
        await expect(back).toBeVisible();
        const box = await note.boundingBox();
        expect(box!.x).toBeGreaterThanOrEqual(0);
        expect(box!.x + box!.width).toBeLessThanOrEqual(width);
        expect(
          await page.evaluate(
            () =>
              document.documentElement.scrollWidth -
              document.documentElement.clientWidth,
          ),
        ).toBeLessThanOrEqual(0);
        await page.screenshot({
          path: testInfo.outputPath(`no-access-${width}-${theme}.png`),
        });
        const serious = (
          await new AxeBuilder({ page }).analyze()
        ).violations.filter(
          (v) => v.impact === "serious" || v.impact === "critical",
        );
        expect(serious.map((v) => v.id)).toEqual([]);
      });
    }
  }

  test("the way back leads to the workspace home", async ({ page }) => {
    await page.goto(GONE);
    await page.getByRole("button", { name: "Về trang chủ" }).click();
    await expect(page).toHaveURL(/\/workspaces\/[^/]+\/home/);
  });
});
