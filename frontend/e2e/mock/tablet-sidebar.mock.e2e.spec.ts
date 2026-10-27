import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const board = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

test.beforeEach(async ({ page }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await page.addInitScript(() => {
    try {
      if (!window.localStorage.getItem("notrelix-locale")) {
        window.localStorage.setItem("notrelix-locale", "vi");
      }
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

const sidebarWidth = (page: import("@playwright/test").Page) =>
  page
    .locator('[data-slot="sidebar-container"]')
    .evaluate((node) => Math.round(node.getBoundingClientRect().width));

test.describe("tablet sidebar (Final-T-* shell)", () => {
  test.use({ viewport: { width: 1180, height: 820 } });

  test("opens at 248 px with 44 px rows and toggles to the 64 px icon column", async ({
    page,
  }) => {
    await page.goto(board);
    const toggle = page.getByRole("button", { name: "Thu gọn thanh bên" });
    await expect(toggle).toHaveAttribute("aria-expanded", "true");
    expect(await sidebarWidth(page)).toBe(248);
    const home = page.getByRole("link", { name: "Trang chủ" });
    expect(
      await home.evaluate((n) => Math.round(n.getBoundingClientRect().height)),
    ).toBe(44);

    await toggle.click();
    const reopen = page.getByRole("button", { name: "Mở rộng thanh bên" });
    await expect(reopen).toHaveAttribute("aria-expanded", "false");
    await expect.poll(() => sidebarWidth(page)).toBe(64);
    // Counts stay visible as badges and the links keep their names.
    await expect(page.getByRole("link", { name: /Hộp thư/ })).toBeVisible();

    await page.reload();
    await expect(
      page.getByRole("button", { name: "Mở rộng thanh bên" }),
    ).toBeVisible();
    expect(await sidebarWidth(page)).toBe(64);
  });
});

test.describe("desktop sidebar is unchanged", () => {
  test.use({ viewport: { width: 1440, height: 900 } });

  test("keeps 256 px and ignores the tablet preference", async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.setItem("notrelix-sidebar-tablet", "mini");
    });
    await page.goto(board);
    await expect(page.getByRole("link", { name: "Trang chủ" })).toBeVisible();
    expect(await sidebarWidth(page)).toBe(256);
    // The edge arrow is the only toggle, and it is open.
    await expect(page.getByRole("button", { name: /thanh bên/ })).toHaveCount(
      1,
    );
    await expect(
      page.getByRole("button", { name: "Thu gọn thanh bên" }),
    ).toHaveAttribute("aria-expanded", "true");
  });
});
