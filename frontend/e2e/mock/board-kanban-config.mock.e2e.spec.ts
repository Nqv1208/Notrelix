import { expect, test, type Page } from "@playwright/test";

/**
 * The Kanban view's own settings, on the mock world (owner persona = board
 * manager): items per lane, who sees the status controls, and Alt+Arrow card
 * move. The settings are stored on the view config; the mock keeps a saved
 * config in session storage, so a reload shows it again. (The mock world's
 * cards are rebuilt on every load, so a moved card itself does not survive a
 * reload here.)
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const ERP_149 = "Viết tài liệu quy trình nhập hoá đơn đầu vào";

async function openBoard(page: Page) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=kanban`);
  await expect(lane(page, "Cần làm")).toBeVisible();
}

function lane(page: Page, name: string) {
  return page.getByRole("region", { name, exact: true });
}

function cardButtons(page: Page, laneName: string) {
  return lane(page, laneName).locator("button[data-card-id]");
}

async function openSettings(page: Page) {
  await page.getByRole("button", { name: "Thiết lập", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "Thiết lập Kanban" });
  await expect(dialog).toBeVisible();
  return dialog;
}

async function closeSettings(page: Page) {
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
}

test.describe("Kanban view settings", () => {
  test("the lane limit decides how many cards a lane shows, and a reload keeps it", async ({
    page,
  }) => {
    await openBoard(page);
    await expect(cardButtons(page, "Cần làm")).toHaveCount(4);

    const dialog = await openSettings(page);
    await dialog
      .getByRole("spinbutton", { name: "Số việc hiện trong mỗi cột" })
      .fill("2");
    await closeSettings(page);
    await expect(cardButtons(page, "Cần làm")).toHaveCount(2);
    await expect(
      lane(page, "Cần làm").getByRole("button", { name: "Xem thêm 4 việc" }),
    ).toBeVisible();

    await page.reload();
    await expect(lane(page, "Cần làm")).toBeVisible();
    await expect(cardButtons(page, "Cần làm")).toHaveCount(2);

    // The stepper writes too.
    const again = await openSettings(page);
    await again
      .getByRole("button", { name: "Hiện nhiều việc hơn trong mỗi cột" })
      .click();
    await closeSettings(page);
    await expect(cardButtons(page, "Cần làm")).toHaveCount(3);
  });

  test("the role setting never takes the status controls from a manager", async ({
    page,
  }) => {
    await openBoard(page);
    await expect(
      page.getByRole("button", { name: "Thêm trạng thái" }),
    ).toBeVisible();

    const dialog = await openSettings(page);
    const manager = dialog.getByRole("checkbox", { name: "Quản lý (luôn có)" });
    await expect(manager).toBeChecked();
    await expect(manager).toBeDisabled();
    const editor = dialog.getByRole("checkbox", { name: "Người chỉnh sửa" });
    await expect(editor).toBeChecked();
    await editor.click();
    await expect(editor).not.toBeChecked();
    await closeSettings(page);

    // Editors are off, the manager keeps the controls.
    await expect(
      page.getByRole("button", { name: "Thêm trạng thái" }),
    ).toBeVisible();
    await page.reload();
    await expect(lane(page, "Cần làm")).toBeVisible();
    const after = await openSettings(page);
    await expect(
      after.getByRole("checkbox", { name: "Người chỉnh sửa" }),
    ).not.toBeChecked();
  });

  test("Alt+Arrow moves the focused card to the next lane, announces it and keeps focus", async ({
    page,
  }) => {
    await openBoard(page);
    const card = page.getByRole("button", { name: ERP_149, exact: true });
    await card.focus();
    await page.keyboard.press("Alt+ArrowRight");

    await expect(
      lane(page, "Đang làm").getByRole("button", {
        name: ERP_149,
        exact: true,
      }),
    ).toBeVisible();
    await expect(page.locator("[data-kanban-announcer]")).toHaveText(
      `Đã chuyển “${ERP_149}” sang Đang làm`,
    );
    await expect(
      page.getByRole("button", { name: ERP_149, exact: true }),
    ).toBeFocused();
    // The write lands and the card stays where it was moved.
    await page.waitForTimeout(1800);
    await expect(
      lane(page, "Đang làm").getByRole("button", {
        name: ERP_149,
        exact: true,
      }),
    ).toBeVisible();
    await expect(lane(page, "Cần làm")).toHaveAttribute("data-lane-count", "5");
    await expect(lane(page, "Đang làm")).toHaveAttribute(
      "data-lane-count",
      "4",
    );

    // Alt+ArrowLeft takes it back, still focused.
    await page.keyboard.press("Alt+ArrowLeft");
    await expect(
      lane(page, "Cần làm").getByRole("button", { name: ERP_149, exact: true }),
    ).toBeFocused();
  });

  test("the switch turns Alt+Arrow off, and a reload keeps it off", async ({
    page,
  }) => {
    await openBoard(page);
    const dialog = await openSettings(page);
    const keys = dialog.getByRole("switch", {
      name: "Chuyển thẻ bằng bàn phím",
    });
    await expect(keys).toBeChecked();
    await keys.click();
    await expect(keys).not.toBeChecked();
    await closeSettings(page);

    for (const reload of [false, true]) {
      if (reload) {
        await page.reload();
        await expect(lane(page, "Cần làm")).toBeVisible();
      }
      const card = page.getByRole("button", { name: ERP_149, exact: true });
      await expect(card).not.toHaveAttribute("aria-keyshortcuts");
      await card.focus();
      await page.keyboard.press("Alt+ArrowRight");
      await page.waitForTimeout(300);
      await expect(
        lane(page, "Cần làm").getByRole("button", {
          name: ERP_149,
          exact: true,
        }),
      ).toBeVisible();
      await expect(lane(page, "Cần làm")).toHaveAttribute(
        "data-lane-count",
        "6",
      );
    }
  });
});
