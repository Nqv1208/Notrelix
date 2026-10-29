import { expect, test, type Page } from "@playwright/test";

/**
 * A member's own view of the ERP board on the mock world: the filter builder
 * ("Lọc"), saving the filter, keeping it across a reload, applying a saved
 * filter again, a Kanban swimlane drop, and the viewer who can filter but not
 * save. Everything here is the member's own view; the shared view is never
 * written.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const NAME = "Việc khẩn của tôi";

async function openView(page: Page, view: "table" | "kanban") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=${view}`);
  await expect(
    view === "table"
      ? page.getByRole("rowgroup").first()
      : page.locator("[data-kanban-scroller]"),
  ).toBeVisible();
}

/** Adds one condition "Ưu tiên là một trong Khẩn" in the open builder. */
async function addUrgentPriority(page: Page) {
  await page.getByRole("button", { name: /^Lọc/ }).click();
  const dialog = page.getByRole("dialog", { name: "Lọc việc" });
  await dialog.getByRole("button", { name: "Thêm điều kiện" }).click();
  await dialog
    .getByLabel(/^Trường/)
    .first()
    .selectOption({ label: "Ưu tiên" });
  await dialog.getByRole("option", { name: "Khẩn" }).click();
  await dialog.getByRole("button", { name: "Xong" }).click();
  await expect(dialog).toBeHidden();
}

test.describe("Board view state (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("builds a filter, keeps it on reload, saves it and applies it again", async ({
    page,
  }) => {
    await openView(page, "table");
    await addUrgentPriority(page);

    // The condition is a chip, the route carries it and the personal view says so.
    await expect(page.getByText("Ưu tiên: Khẩn")).toBeVisible();
    await expect(page).toHaveURL(/filter=c2/);
    await expect(
      page.getByRole("group", { name: "Cách xem riêng" }),
    ).toBeVisible();

    await page.reload();
    await expect(page.getByRole("rowgroup").first()).toBeVisible();
    await expect(page.getByText("Ưu tiên: Khẩn")).toBeVisible();

    // Save it under a name, go back to the shared view, apply it from the menu.
    await page.getByRole("button", { name: "Lưu bộ lọc", exact: true }).click();
    const form = page.getByRole("form", { name: "Lưu bộ lọc hiện tại" });
    await form.getByRole("textbox", { name: "Tên bộ lọc" }).fill(NAME);
    await form.getByRole("button", { name: "Lưu bộ lọc", exact: true }).click();
    await expect(page.getByText(`Đã lưu bộ lọc ${NAME}`)).toBeVisible();

    await page.getByRole("button", { name: "Về view chung" }).click();
    await expect(page).not.toHaveURL(/filter=/);
    await expect(page.getByText("Ưu tiên: Khẩn")).toHaveCount(0);

    await page.getByRole("button", { name: "Bộ lọc đã lưu" }).click();
    await page.getByRole("button", { name: `Áp dụng bộ lọc ${NAME}` }).click();
    await expect(page.getByText("Ưu tiên: Khẩn")).toBeVisible();
    await expect(page).toHaveURL(/filter=c2/);
    // The layout is still the board's own: no other view was opened or made.
    await expect(page).toHaveURL(/view=table/);
  });

  test("a swimlane drop writes the lane field and offers Hoàn tác", async ({
    page,
  }) => {
    await openView(page, "kanban");
    await page.getByRole("button", { name: /^Làn/ }).click();
    await page.getByRole("menuitemradio", { name: "Ưu tiên" }).click();

    const board = page.locator("[data-kanban-swimlanes]");
    await expect(board).toBeVisible();
    const lanes = board.locator("[data-swimlane]");
    expect(await lanes.count()).toBeGreaterThan(1);

    // Alt+ArrowDown is the keyboard path of the same drop.
    const card = lanes.first().locator("[data-card-id]").first();
    await card.focus();
    await page.keyboard.press("Alt+ArrowDown");
    await expect(page.getByText(/^Đã đặt Ưu tiên của /)).toBeVisible();
    await page.getByRole("button", { name: "Hoàn tác" }).click();
  });
});

test.describe("Board view state (viewer)", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default" || faults !== "",
    "Needs the default viewer world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("a viewer can filter their own view but cannot save the filter", async ({
    page,
  }) => {
    await openView(page, "table");
    await addUrgentPriority(page);
    await expect(page.getByText("Ưu tiên: Khẩn")).toBeVisible();

    const save = page.getByRole("button", { name: "Lưu bộ lọc", exact: true });
    await expect(save).toBeDisabled();
    await expect(
      page.getByText(/Bạn chỉ xem board này/).first(),
    ).toBeAttached();
  });
});
