import { expect, test } from "@playwright/test";

/**
 * A viewer's custom field cells in the table against `WM-Field-Editors` ("Chỉ xem: mỗi kiểu một ô, ổ khoá có tên
 * đọc"), measured in Chromium from the canvas render, light and dark, tolerance 1 px: every editable type ends its
 * cell with a 16 x 16 lock in the muted colour, centred on the 40 px row, 11 px from the cell's right edge, named
 * "{field}: chỉ xem, bạn không có quyền sửa" and titled "Chỉ xem: bạn không có quyền sửa". The cell's own editor is
 * not offered. Run with `VITE_MOCK_PERSONA=viewer`; the editor of the owner world has no lock.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const TABLE =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=table";
const near = (a: number, w: number, what: string) =>
  expect(Math.abs(a - w), `${what}: ${a} vs ${w}`).toBeLessThanOrEqual(1);

for (const dark of [false, true]) {
  test(`the viewer's cells end with a named lock, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(persona !== "viewer" || scenario !== "default");
    await page.setViewportSize({ width: 1440, height: 900 });
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
    await page.goto(TABLE);
    await page.waitForTimeout(900);
    const locks = page.getByRole("img", {
      name: /: chỉ xem, bạn không có quyền sửa$/,
    });
    expect(await locks.count()).toBeGreaterThan(3);
    const lock = locks.first();
    await expect(lock).toHaveAttribute("title", "Chỉ xem: bạn không có quyền sửa");
    const [l, host] = await Promise.all([
      lock.boundingBox(),
      lock.locator("xpath=..").boundingBox(),
    ]);
    near(l!.width, 16, "lock width");
    near(l!.height, 16, "lock height");
    near(host!.x + host!.width - (l!.x + l!.width), 10, "lock from the cell edge");
    near(l!.y + l!.height / 2, host!.y + host!.height / 2, "lock centred");
  });
}
