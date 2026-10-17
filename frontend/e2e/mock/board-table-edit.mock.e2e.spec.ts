import { expect, test, type Page } from "@playwright/test";

/**
 * The ERP board's table edited in place on the mock world: the assignee, due
 * date and priority of a row, "Giao cho" and "Chuyển board" of the bulk bar,
 * and the row menu (duplicate, copy link, archive with its undo window). The
 * header as Final-Table draws it (round faces, no "More options") is checked
 * here too.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openTable(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=table`);
  await expect(page.getByRole("rowgroup").first()).toBeVisible();
}

const row = (page: Page, key: string) =>
  page.locator("[data-row-id]").filter({ hasText: key }).first();

test.describe("Board table, in-place edits (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the header has round faces and no More options button", async ({
    page,
  }) => {
    await openTable(page);
    await expect(
      page.getByRole("button", { name: /Thêm tuỳ chọn/ }),
    ).toHaveCount(0);
    const more = page.getByText("+29");
    const box = (await more.boundingBox())!;
    expect(Math.round(box.width)).toBe(Math.round(box.height));
    await expect(
      page.getByRole("button", { name: "Thiết lập", exact: true }),
    ).toBeVisible();
    // Only Final-Board's header has it.
    await page.goto(BOARD);
    await expect(
      page.getByRole("button", { name: /Thêm tuỳ chọn/ }),
    ).toBeVisible();
  });

  test("assignee: the list changes the person of the row", async ({ page }) => {
    await openTable(page);
    await page
      .getByRole("button", { name: "Đổi người phụ trách của ERP-153" })
      .click();
    await page.getByRole("button", { name: /Lê Hoàng Phúc/ }).click();
    await expect(row(page, "ERP-153")).toContainText("Lê Hoàng Phúc");
    await page
      .getByRole("button", { name: "Đổi người phụ trách của ERP-153" })
      .click();
    await page.getByRole("button", { name: /Chưa giao/ }).click();
    await expect(row(page, "ERP-153")).toContainText("Chưa giao");
  });

  test("priority and due date: the cells write and the row follows", async ({
    page,
  }) => {
    await openTable(page);
    await page.getByRole("button", { name: "Đổi ưu tiên của ERP-153" }).click();
    await page.getByRole("menuitem", { name: "Khẩn" }).click();
    await expect(row(page, "ERP-153")).toContainText("Khẩn");

    await page.getByRole("button", { name: "Đổi hạn của ERP-153" }).click();
    await page
      .locator("[data-radix-popper-content-wrapper] button")
      .filter({ hasText: /^20$/ })
      .first()
      .click();
    await expect(row(page, "ERP-153")).toContainText("20/10");
  });

  test("bulk: Giao cho assigns every selected row", async ({ page }) => {
    await openTable(page);
    for (const key of ["ERP-142", "ERP-146"])
      await page.getByRole("checkbox", { name: `Chọn ${key}` }).click();
    await page.getByRole("button", { name: /^Giao cho/ }).click();
    await page.getByRole("button", { name: "Phan Ngọc Anh" }).click();
    await expect(row(page, "ERP-142")).toContainText("Phan Ngọc Anh");
    await expect(row(page, "ERP-146")).toContainText("Phan Ngọc Anh");
  });

  test("bulk: Chuyển board sends the row to the other board", async ({
    page,
  }) => {
    await openTable(page);
    await page.getByRole("checkbox", { name: "Chọn ERP-147" }).click();
    await page.getByRole("button", { name: /^Chuyển board/ }).click();
    await page.getByRole("button", { name: "Hỗ trợ nội bộ" }).click();
    await expect(
      page.locator("[data-row-id]").filter({ hasText: "ERP-147" }),
    ).toHaveCount(0);
    await expect(
      page.getByText("10 công việc đang hiện · 5 ẩn bởi bộ lọc"),
    ).toBeVisible();
  });

  test("row menu: duplicate, and archive with its undo window", async ({
    page,
  }) => {
    await openTable(page);
    await row(page, "ERP-144").click({ button: "right" });
    await page.getByRole("menuitem", { name: "Nhân bản" }).click();
    await expect(
      page.locator("[data-row-id]").filter({ hasText: "(Copy)" }),
    ).toHaveCount(1);

    await row(page, "ERP-147").click({ button: "right" });
    await page.getByRole("menuitem", { name: "Lưu trữ" }).click();
    await expect(row(page, "ERP-147")).toHaveCount(0);
    await expect(page.getByText(/1 vừa lưu trữ/)).toBeVisible();
    await page.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(row(page, "ERP-147")).toBeVisible();
  });

  test("the empty note names the person the filter narrows to", async ({
    page,
  }) => {
    await openTable(page);
    await page.getByRole("button", { name: /^Lọc/ }).click();
    await page.getByText("Đặng Minh Tuấn").first().click();
    await page.keyboard.press("Escape");
    await expect(
      page.getByText(
        "Đặng Minh Tuấn chưa phụ trách việc nào chưa xong trên board này.",
      ),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: /^Thêm trạng thái/ }),
    ).toHaveCount(0);
  });
});
