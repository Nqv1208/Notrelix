import { expect, test, type Page } from "@playwright/test";

/**
 * Deleting a department and restoring it, at the three review widths in both themes, in Vietnamese. The
 * delete confirmation is drawn in the mockup and measured in `design-fidelity-space`; the deleted list has no
 * board, so here it is checked for a page that never scrolls sideways and for its states, and the screenshots
 * of every width and theme are attached for review.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "/workspaces/mock-workspace-primary";
const EMPTY_DEPARTMENT = `${WORKSPACE}/spaces/mock-space-hr`;

const WIDTHS = [1440, 1024, 390] as const;
const THEMES = ["light", "dark"] as const;

async function noSidewaysScroll(page: Page) {
  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth -
      document.documentElement.clientWidth,
  );
  expect(overflow, "horizontal overflow of the page").toBeLessThanOrEqual(0);
}

test.describe("Deleted departments (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );

  for (const width of WIDTHS) {
    for (const theme of THEMES) {
      test(`delete, the deleted list and restore at ${width} px in the ${theme} theme`, async ({
        page,
      }, testInfo) => {
        await page.setViewportSize({ width, height: 900 });
        await page.addInitScript(
          ([mode]) => {
            try {
              window.localStorage.setItem("notrelix-locale", "vi");
              window.localStorage.setItem("notrelix-system-rail", "pinned");
              window.localStorage.setItem("notrelix-theme-mode", mode!);
            } catch {
              // Private mode: the defaults apply.
            }
          },
          [theme],
        );
        await page.goto(`${EMPTY_DEPARTMENT}?tab=settings`);
        const remove = page.getByRole("button", { name: "Xoá phòng ban" });
        await expect(remove).toBeVisible();
        await remove.click();
        const group = page.getByRole("group", {
          name: /^Xoá Phòng Nhân sự khỏi/,
        });
        await expect(group).toBeVisible();
        await noSidewaysScroll(page);
        await testInfo.attach(`confirm-${width}-${theme}`, {
          body: await page.screenshot(),
          contentType: "image/png",
        });
        await group.getByRole("textbox").fill("Phòng Nhân sự");
        await group.getByRole("button", { name: "Xoá phòng ban" }).click();

        const heading = page.getByRole("heading", {
          level: 1,
          name: "Phòng ban đã xoá",
        });
        await expect(heading).toBeVisible();
        await expect(page.getByText(/Khôi phục được đến/)).toBeVisible();
        await noSidewaysScroll(page);
        await testInfo.attach(`deleted-${width}-${theme}`, {
          body: await page.screenshot(),
          contentType: "image/png",
        });
        // The Restore button is a touch target on a phone.
        const restore = page.getByRole("button", { name: "Khôi phục" });
        const size = await restore.boundingBox();
        expect(size?.height ?? 0).toBeGreaterThanOrEqual(
          width <= 640 ? 44 : 32,
        );
        await restore.click();
        await expect(
          page.getByText(/Không có phòng ban nào bị xoá/),
        ).toBeVisible();
        await noSidewaysScroll(page);
        await testInfo.attach(`empty-${width}-${theme}`, {
          body: await page.screenshot(),
          contentType: "image/png",
        });
      });
    }
  }
});
