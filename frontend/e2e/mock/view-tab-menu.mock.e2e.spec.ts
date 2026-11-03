import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The menu of the opened saved view, in the tab strip (UC-WM-H03, H04): set as default, archive, delete and restore.
 * Undrawn on the canvas, so it is reviewed instead of measured: at 1440, 1024 and 390 px in light and dark, in
 * Vietnamese, the menu and the delete confirm (with the next-default picker) have no sideways scroll, no clipped
 * dialog, 44 px targets at 390 px and no serious axe violation. `WM_SHOTS_DIR` writes a screenshot of each state.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  scenario !== "default" || persona !== "owner" || faults !== "",
  "Assertions need the default owner world",
);

const trigger = (page: Page) =>
  page.getByRole("button", { name: /^Tuỳ chọn của chế độ xem/ });

async function open(page: Page, width: number, height: number, dark: boolean) {
  await page.setViewportSize({ width, height });
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
  await expect(page.getByRole("tablist", { name: "Chế độ xem" })).toBeVisible();
  // The board's Kanban view is the default; a second, shared view is added, then the default one's tab is picked.
  for (const layout of ["Bảng"]) {
    await page.getByRole("button", { name: "Thêm chế độ xem" }).click();
    await page.getByRole("menuitem", { name: layout, exact: true }).click();
    await expect(
      page.getByRole("tab", { name: layout, selected: true }),
    ).toBeVisible();
  }
  await page.getByRole("tab", { name: "Kanban", exact: true }).click();
  await expect(trigger(page)).toBeVisible();
}

/** The open surface alone: the table behind it is measured by its own specs. */
const serious = async (page: Page, surface: string) => {
  // The surface fades in; axe reads the colours once it has settled.
  await page.waitForTimeout(400);
  return (
    await new AxeBuilder({ page }).include(surface).analyze()
  ).violations.filter((violation) =>
    ["serious", "critical"].includes(violation.impact ?? ""),
  );
};

const shot = async (page: Page, name: string, width: number, dark: boolean) => {
  if (shots)
    await page.screenshot({
      path: `${shots}/view-menu-${name}-${width}-${dark ? "dark" : "light"}.png`,
    });
};

for (const [width, height] of [
  [1440, 900],
  [1024, 768],
  [390, 844],
] as const) {
  for (const dark of [false, true]) {
    test(`${width} ${dark ? "dark" : "light"}: menu and delete confirm`, async ({
      page,
    }) => {
      await open(page, width, height, dark);
      await trigger(page).click();
      const menu = page.getByRole("menu");
      await expect(menu).toBeVisible();
      expect(await serious(page, "[role=menu]"), "menu").toEqual([]);
      await shot(page, "menu", width, dark);
      await page.getByRole("menuitem", { name: /^Xoá chế độ xem/ }).click();

      const dialog = page.getByRole("alertdialog");
      await expect(dialog).toBeVisible();
      await expect(dialog).toContainText("Xoá chế độ xem");
      await expect(
        dialog.getByRole("combobox", { name: "Chế độ xem mặc định kế tiếp" }),
      ).toBeVisible();
      await page.waitForTimeout(400); // the dialog zooms in; measure it settled
      const box = await dialog.boundingBox();
      expect(box).not.toBeNull();
      expect(box!.x).toBeGreaterThanOrEqual(0);
      expect(box!.x + box!.width).toBeLessThanOrEqual(width);
      expect(box!.y + box!.height).toBeLessThanOrEqual(height);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth - window.innerWidth,
        ),
        "sideways scroll",
      ).toBeLessThanOrEqual(0);
      if (width <= 390) {
        const small = await dialog
          .locator("button, [role=combobox]")
          .evaluateAll((els) =>
            els
              .filter((el) => el.getBoundingClientRect().height < 43.5)
              .map((el) => el.textContent),
          );
        expect(small, "controls under 44 px").toEqual([]);
      }
      expect(await serious(page, "[role=alertdialog]"), "confirm").toEqual([]);
      await shot(page, "confirm", width, dark);
    });
  }
}

test("the tab strip sets the default, archives, restores and deletes a view", async ({
  page,
}) => {
  await open(page, 1440, 900, false);
  // The default Kanban view is opened. A second view takes the default, then Kanban is archived and restored.
  await page.getByRole("tab", { name: "Bảng", exact: true }).click();
  await trigger(page).click();
  await page
    .getByRole("menuitem", { name: "Đặt làm chế độ xem mặc định" })
    .click();
  await trigger(page).click();
  await expect(
    page.getByRole("menuitem", { name: "Đặt làm chế độ xem mặc định" }),
  ).toHaveAttribute("aria-disabled", "true");
  await page.keyboard.press("Escape");

  await page.getByRole("tab", { name: "Kanban", exact: true }).click();
  await trigger(page).click();
  await page.getByRole("menuitem", { name: /^Lưu trữ chế độ xem/ }).click();
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Lưu trữ chế độ xem" })
    .click();
  await expect(
    page.getByRole("tab", { name: "Kanban", exact: true }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("tab", { name: "Bảng", exact: true, selected: true }),
  ).toBeVisible();

  await trigger(page).click();
  await page.getByRole("menuitem", { name: "Khôi phục Kanban" }).click();
  await expect(
    page.getByRole("tab", { name: "Kanban", exact: true }),
  ).toBeVisible();

  // The default view leaves only with its successor named.
  await trigger(page).click();
  await page.getByRole("menuitem", { name: /^Xoá chế độ xem/ }).click();
  const dialog = page.getByRole("alertdialog");
  await expect(
    dialog.getByRole("combobox", { name: "Chế độ xem mặc định kế tiếp" }),
  ).toContainText("Kanban");
  await dialog.getByRole("button", { name: "Xoá chế độ xem" }).click();
  // The person lands on the view that took the default, with no "gone" notice.
  await expect(
    page.getByRole("tab", { name: "Kanban", exact: true, selected: true }),
  ).toBeVisible();
  await expect(page.getByText("Chế độ xem này không còn nữa")).toHaveCount(0);
});
