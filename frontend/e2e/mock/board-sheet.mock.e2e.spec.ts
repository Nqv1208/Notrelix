import { expect, test, type Page } from "@playwright/test";

/**
 * The ERP board's Kanban header and lanes as Final-Board draws them, and the card popup of an item, on the
 * mock world: every card of a lane (six in Cần làm, five in the Done lane) with no "Xem thêm" before 20, the
 * department crumb, and the popup's sub-items, vote chip, comments with the recorded changes among them,
 * the composer, and the column picker that moves the card.
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
  // An open popup is modal: the page behind it leaves the accessibility tree, so the heading is read without it.
  if (query.includes("item="))
    await expect(page.getByRole("dialog")).toBeVisible();
  else
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

  test("the popup lists the child items with their state", async ({ page }) => {
    await open(page, "?item=mock-erp-item-142");
    const section = page
      .getByRole("dialog")
      .getByRole("region", { name: "Liên kết và phụ thuộc" });
    await expect(section.getByText("Việc con", { exact: true })).toBeVisible();
    const state = (key: string) =>
      section.getByText(key, { exact: true }).locator("..");
    for (const key of ["ERP-142.1", "ERP-142.2", "ERP-142.3"])
      await expect(state(key)).toContainText("Hoàn thành");
    for (const key of ["ERP-142.4", "ERP-142.5"])
      await expect(state(key)).toContainText("Chưa xong");
  });

  test("the popup names the card and its vote chip flips and counts", async ({
    page,
  }) => {
    await open(page, "?item=mock-erp-item-142");
    const popup = page.getByRole("dialog");
    await expect(popup.getByText("ERP-142", { exact: true })).toBeVisible();
    const vote = popup.getByRole("button", {
      name: /^(Bình chọn|Đã bình chọn)$/,
    });
    await expect(vote).toHaveAttribute("aria-pressed", "false");
    await expect(
      popup.getByRole("button", { name: "0 người đã bình chọn" }),
    ).toBeVisible();
    await vote.click();
    await expect(vote).toHaveAttribute("aria-pressed", "true");
    await expect(
      popup.getByRole("button", { name: "1 người đã bình chọn" }),
    ).toBeVisible();
    await vote.click();
    await expect(vote).toHaveAttribute("aria-pressed", "false");
    await expect(
      popup.getByRole("button", { name: "0 người đã bình chọn" }),
    ).toBeVisible();
  });

  test("comments show the recorded change among them and the composer posts", async ({
    page,
  }) => {
    await open(page, "?item=mock-erp-item-142");
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Hiện chi tiết" }).click();
    await expect(
      popup.getByText("Trần Minh Quân đổi Hạn chót").first(),
    ).toBeVisible();
    await expect(
      popup.getByText("Em lùi hạn sang 10/10", { exact: false }),
    ).toBeVisible();
    await popup.getByRole("button", { name: "Viết bình luận…" }).click();
    await popup
      .getByRole("textbox", { name: "Viết bình luận" })
      .fill("Em đã đối chiếu xong, gửi chị Linh xem lại.");
    await popup.getByRole("button", { name: "Bình luận", exact: true }).click();
    await expect(
      popup.getByText("Em đã đối chiếu xong, gửi chị Linh xem lại."),
    ).toBeVisible();
  });

  test("a Status change from the column picker is recorded in the history", async ({
    page,
  }) => {
    await open(page, "?item=mock-erp-item-142");
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Đang làm" }).first().click();
    await page.getByRole("option", { name: "Chờ duyệt" }).click();
    await expect(
      popup.getByRole("button", { name: "Chờ duyệt" }).first(),
    ).toBeVisible();
    await popup.getByRole("button", { name: "Hiện chi tiết" }).click();
    await expect(
      popup.getByText("Trần Minh Quân đổi Trạng thái").first(),
    ).toBeVisible();
    await expect(popup.getByText("từ Đang làm sang Chờ duyệt")).toBeVisible();
  });
});
