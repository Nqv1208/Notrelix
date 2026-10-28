import { expect, test, type Page } from "@playwright/test";

/**
 * Saved filters of the ERP board on the mock world: "Lưu bộ lọc" saves the
 * filter on the bar under a name, "Bộ lọc đã lưu" applies it again, shares it
 * with the board, and deletes it with "Hoàn tác" to bring it back. Applying a
 * saved filter only sets the route filter; it never writes the shared view.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const NAME = "Việc khẩn của tôi";

async function openFilteredTable(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=table&filter=p.urgent`);
  await expect(page.getByRole("rowgroup").first()).toBeVisible();
}

async function saveCurrentFilter(page: Page, share = false) {
  await page.getByRole("button", { name: "Lưu bộ lọc", exact: true }).click();
  const form = page.getByRole("form", { name: "Lưu bộ lọc hiện tại" });
  await form.getByRole("textbox", { name: "Tên bộ lọc" }).fill(NAME);
  if (share)
    await form.getByRole("radio", { name: /Chia sẻ với board/ }).check();
  await form.getByRole("button", { name: "Lưu bộ lọc", exact: true }).click();
}

test.describe("Board saved filters (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("saves the filter on the bar, then applies it again from the menu", async ({
    page,
  }) => {
    await openFilteredTable(page);
    await saveCurrentFilter(page);

    await expect(page.getByText(`Đã lưu bộ lọc ${NAME}`)).toBeVisible();
    // The saved filter is on the bar: the menu button carries its name and the
    // save button is gone.
    await expect(page.getByRole("button", { name: NAME })).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Lưu bộ lọc", exact: true }),
    ).toHaveCount(0);

    // Clear it, then bring it back from the menu.
    await page.getByRole("button", { name: NAME }).click();
    await page.getByRole("button", { name: "Bỏ bộ lọc đang áp dụng" }).click();
    await expect(page).not.toHaveURL(/filter=/);

    await page.getByRole("button", { name: "Bộ lọc đã lưu" }).click();
    await page.getByRole("button", { name: `Áp dụng bộ lọc ${NAME}` }).click();
    await expect(page).toHaveURL(/filter=p\.urgent/);
    // The layout is still the board's own: no other view was opened or made.
    await expect(page).toHaveURL(/view=table/);
  });

  test("shares a personal filter with the board and takes it back", async ({
    page,
  }) => {
    await openFilteredTable(page);
    await saveCurrentFilter(page);
    await page.getByRole("button", { name: NAME }).click();

    await expect(
      page.getByRole("region", { name: /^Của tôi/ }).getByText(NAME),
    ).toBeVisible();
    await page
      .getByRole("button", { name: `Chia sẻ ${NAME} với board` })
      .click();
    await expect(page.getByText(`${NAME} đã chia sẻ với board`)).toBeVisible();
    await expect(
      page.getByRole("region", { name: /^Chia sẻ với board/ }).getByText(NAME),
    ).toBeVisible();

    await page
      .getByRole("button", { name: `Chuyển ${NAME} về riêng tôi` })
      .click();
    await expect(
      page.getByRole("region", { name: /^Của tôi/ }).getByText(NAME),
    ).toBeVisible();
  });

  test("renames in place, deletes, and undoes the delete", async ({ page }) => {
    await openFilteredTable(page);
    await saveCurrentFilter(page);
    await page.getByRole("button", { name: NAME }).click();

    await page.getByRole("button", { name: `Đổi tên ${NAME}` }).click();
    const input = page.getByRole("textbox", { name: "Tên mới của bộ lọc" });
    await input.fill("Khẩn tuần này");
    await input.press("Enter");
    await expect(
      page.getByText("Đã đổi tên thành Khẩn tuần này"),
    ).toBeVisible();

    await page.getByRole("button", { name: "Xoá Khẩn tuần này" }).click();
    await expect(page.getByText("Đã xoá bộ lọc Khẩn tuần này")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Áp dụng bộ lọc Khẩn tuần này" }),
    ).toHaveCount(0);

    await page.getByRole("button", { name: "Hoàn tác" }).click();
    await expect(
      page.getByText("Đã khôi phục bộ lọc Khẩn tuần này"),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Áp dụng bộ lọc Khẩn tuần này" }),
    ).toBeVisible();
  });
});

test.describe("Board saved filters (viewer)", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default" || faults !== "",
    "Needs the default viewer world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("a viewer cannot save a filter", async ({ page }) => {
    await openFilteredTable(page);
    await expect(
      page.getByRole("button", { name: "Lưu bộ lọc", exact: true }),
    ).toHaveCount(0);
    await page.getByRole("button", { name: "Bộ lọc đã lưu" }).click();
    await expect(page.getByText(/Bạn chỉ xem board này/)).toBeVisible();
  });
});
