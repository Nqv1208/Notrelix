import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

/**
 * The board's Labels settings at 390 px, light and dark, in Vietnamese: the page keeps inside the viewport, the
 * add form, a row's rename and delete buttons, the colour choices and the delete question are 44 px targets, and axe
 * finds nothing serious. `WM_SHOTS_DIR` writes a screenshot of each state for review.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const URL =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/settings?section=labels";

for (const dark of [false, true]) {
  test(`labels at 390 px: inside the page, 44 px targets, no serious axe violation, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(persona !== "owner" || scenario !== "default" || faults !== "");
    await page.setViewportSize({ width: 390, height: 844 });
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
    await page.goto(URL);
    const list = page.getByRole("list", { name: "Nhãn của board" });
    await expect(list).toBeVisible();
    await page.waitForTimeout(500);
    const overflow = () =>
      page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      );
    expect(await overflow()).toBeLessThanOrEqual(0);
    const shot = (name: string) =>
      shots
        ? page.screenshot({
            path: `${shots}/labels-${name}-${dark ? "dark" : "light"}.png`,
          })
        : undefined;
    await shot("list");

    const small = async (names: RegExp[], where = page, role: "button" | "radio" = "button") => {
      for (const name of names) {
        const box = await where.getByRole(role, { name }).first().boundingBox();
        expect(box, String(name)).not.toBeNull();
        expect(box!.height, `${name} height`).toBeGreaterThanOrEqual(43.5);
        expect(box!.width, `${name} width`).toBeGreaterThanOrEqual(43.5);
      }
    };
    await small([/^Sửa nhãn|^Đổi tên nhãn/, /^Xoá nhãn/]);

    await page.getByRole("button", { name: /^Thêm nhãn/ }).first().click();
    await expect(page.getByRole("form")).toBeVisible();
    await small([/^Lam$/], page.getByRole("form"), "radio");
    await small([/Huỷ/], page.getByRole("form"));
    expect(await overflow()).toBeLessThanOrEqual(0);
    await shot("add");
    const serious = (
      await new AxeBuilder({ page }).analyze()
    ).violations.filter((v) => ["serious", "critical"].includes(v.impact ?? ""));
    expect(serious).toEqual([]);
  });
}
