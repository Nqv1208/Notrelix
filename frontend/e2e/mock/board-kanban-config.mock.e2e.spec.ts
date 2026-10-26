import { expect, test, type Page } from "@playwright/test";

/**
 * The Kanban view on the mock world (owner persona = board manager). The
 * board has no producer for a view's own settings (items per lane, who sees
 * the status controls, the card-move shortcut), so the settings page offers no
 * view section and the Kanban keeps its defaults: three cards before "Xem
 * thêm", the status controls for the manager, Alt+Arrow card move on. (The
 * mock world's cards are rebuilt on every load, so a moved card itself does
 * not survive a reload here.)
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

/** The board's "Thiết lập" opens the settings page. */
async function openSettings(page: Page) {
  await page.getByRole("button", { name: "Thiết lập", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Thiết lập board" }),
  ).toBeVisible();
}

test.describe("Kanban view settings", () => {
  test("the settings page offers no view settings, and a lane keeps three cards before Xem thêm", async ({
    page,
  }) => {
    await openBoard(page);
    await expect(cardButtons(page, "Cần làm")).toHaveCount(3);
    await openSettings(page);
    await expect(page.getByRole("button", { name: /^Chế độ xem/ })).toHaveCount(
      0,
    );
    await expect(
      page.getByRole("button", { name: "Sửa chế độ xem Kanban" }),
    ).toHaveCount(0);
    await page.getByRole("link", { name: "Về board" }).click();
    await expect(lane(page, "Cần làm")).toBeVisible();
    await expect(cardButtons(page, "Cần làm")).toHaveCount(3);
    await expect(
      lane(page, "Cần làm").getByRole("button", { name: "Xem thêm 3 việc" }),
    ).toBeVisible();
  });

  test("the manager keeps the status controls after a reload", async ({
    page,
  }) => {
    await openBoard(page);
    await expect(
      page.getByRole("button", { name: "Thêm trạng thái" }),
    ).toBeVisible();
    await page.reload();
    await expect(lane(page, "Cần làm")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Thêm trạng thái" }),
    ).toBeVisible();
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

  test("Alt+Arrow card move is on for a card, before and after a reload", async ({
    page,
  }) => {
    await openBoard(page);
    for (const reload of [false, true]) {
      if (reload) {
        await page.reload();
        await expect(lane(page, "Cần làm")).toBeVisible();
      }
      await expect(
        page.getByRole("button", { name: ERP_149, exact: true }),
      ).toHaveAttribute("aria-keyshortcuts", /Alt/);
    }
  });
});
