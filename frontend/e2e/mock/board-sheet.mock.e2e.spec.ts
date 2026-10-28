import { expect, test, type Page } from "@playwright/test";

/**
 * The ERP board's Kanban header, lanes and item side sheet as Final-Board draws
 * them, on the mock world: every card of a lane (six in Cần làm, five in the
 * Done lane) with no "Xem thêm" before 20, the department crumb, and the sheet's child items, follow chip,
 * comments with the recorded changes among them, and the composer.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default applies.
    }
  });
  await page.goto(`${BOARD}${query}`);
  await expect(
    page.getByRole("heading", { level: 1, name: "Triển khai ERP Q4" }),
  ).toBeVisible();
}

test.describe("Board header, lanes and item sheet (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("a lane shows all its cards: six in Cần làm, five in Hoàn thành, no Xem thêm", async ({
    page,
  }) => {
    await open(page);
    const todo = page.getByRole("region", { name: "Cần làm", exact: true });
    await expect(todo.locator("button[data-card-id]")).toHaveCount(6);
    await expect(todo.getByRole("button", { name: /^Xem thêm/ })).toHaveCount(
      0,
    );
    const done = page.getByRole("region", { name: "Hoàn thành", exact: true });
    await expect(done.locator("button[data-card-id]")).toHaveCount(5);
    await expect(done.getByRole("button", { name: /^Xem thêm/ })).toHaveCount(
      0,
    );
  });

  test("the header names the department", async ({ page }) => {
    await open(page);
    await expect(page.getByText("Phòng Công nghệ · có 1 khách")).toBeVisible();
    await expect(
      page.getByRole("link", { name: "Phòng Công nghệ" }).first(),
    ).toBeVisible();
  });

  test("the sheet shows the child progress and lists the child items", async ({
    page,
  }) => {
    await open(page, "?item=mock-erp-item-142");
    const sheet = page.getByRole("dialog");
    await expect(sheet.getByText("3/5")).toBeVisible();
    await expect(sheet.getByRole("checkbox").first()).toBeVisible();
  });

  test("the sheet offers the follow chip and flips it", async ({ page }) => {
    await open(page, "?item=mock-erp-item-142");
    const sheet = page.getByRole("dialog");
    await expect(sheet.getByText("ERP-142", { exact: true })).toBeVisible();
    const chip = sheet.locator('[data-f="follow"]');
    await expect(chip).toBeVisible();
    const before = await chip.getAttribute("aria-pressed");
    await chip.click();
    await expect(chip).not.toHaveAttribute("aria-pressed", before ?? "");
  });

  test("comments show the recorded change among them and the composer posts", async ({
    page,
  }) => {
    await open(page, "?item=mock-erp-item-142");
    const sheet = page.getByRole("dialog");
    await expect(
      sheet
        .locator('[data-f="event"]')
        .filter({ hasText: "đổi Hạn chót từ 08/10 sang 10/10" }),
    ).toContainText("Trần Minh Quân đổi Hạn chót từ 08/10 sang 10/10");
    await sheet
      .getByRole("textbox", { name: /Viết bình luận/ })
      .fill("Em đã đối chiếu xong, gửi chị Linh xem lại.");
    await page.keyboard.press("Control+Enter");
    await expect(
      sheet.getByText("Em đã đối chiếu xong, gửi chị Linh xem lại."),
    ).toBeVisible();
  });

  test("a Status change is recorded in the history", async ({ page }) => {
    await open(page, "?item=mock-erp-item-142");
    const sheet = page.getByRole("dialog");
    await sheet.getByRole("button", { name: /^Trạng thái: Đang làm/ }).click();
    await page.getByRole("menuitem", { name: "Chờ duyệt" }).click();
    await expect(sheet.locator('[data-f="event"]').last()).toContainText(
      "đổi trạng thái từ Đang làm sang Chờ duyệt",
    );
  });
});
