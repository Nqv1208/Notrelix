import { expect, test, type Page } from "@playwright/test";

/**
 * Moving a card from its popup, on the mock world. The popup's column picker changes what the board groups
 * by: the Status on a board grouped by Status, the Select field the board is grouped by otherwise, so the
 * way to move is always in the popup's header. A person who may not edit sees the column as a label and the
 * notice under the header says why.
 *
 * The owner checks run in the default world. The pending state needs `VITE_MOCK_LATENCY=slow`, a refused
 * write `VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/status=network` (or `=409`), the viewer check
 * `VITE_MOCK_PERSONA=viewer`.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const STATUS_PATH = "/board-items/mock-erp-item-142/status";

async function open(page: Page, query: string) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default applies.
    }
  });
  await page.goto(`${BOARD}?${query}item=mock-erp-item-142`);
  await expect(page.getByRole("dialog")).toBeVisible();
}

test.use({ viewport: { width: 1440, height: 900 } });

test.describe("owner", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );

  test("grouped by Status, the picker lists the lanes, marks the current one and moves the card", async ({
    page,
  }) => {
    await open(page, "");
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Đang làm" }).first().click();
    const list = page.getByRole("listbox", { name: "Chuyển sang cột" });
    await expect(list.getByRole("option")).toHaveText([
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
      "Hoàn thành",
    ]);
    await expect(
      list.getByRole("option", { name: "Đang làm" }),
    ).toHaveAttribute("aria-selected", "true");
    await list.getByRole("option", { name: "Hoàn thành" }).click();
    await expect(
      popup.getByRole("button", { name: "Hoàn thành" }).first(),
    ).toBeVisible();

    await popup.getByRole("button", { name: "Đóng" }).click();
    await expect(popup).toHaveCount(0);
    const card = (lane: string) =>
      page
        .getByRole("region", { name: lane, exact: true })
        .locator('button[data-card-id="mock-erp-item-142"]');
    await expect(card("Hoàn thành")).toHaveCount(1);
    await expect(card("Đang làm")).toHaveCount(0);
  });

  test("grouped by another field, the picker changes that field and leaves the Status alone", async ({
    page,
  }) => {
    await open(page, "view=kanban&groupBy=mock-erp-col-module&");
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Kế toán" }).first().click();
    const list = page.getByRole("listbox", { name: "Chuyển sang cột" });
    await expect(list.getByRole("option", { name: "Kế toán" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await list.getByRole("option", { name: "Bán hàng" }).click();
    await expect(
      popup.getByRole("button", { name: "Bán hàng" }).first(),
    ).toBeVisible();
    // The Status chip is still the card's own.
    await expect(popup.getByRole("button", { name: "Đang làm" })).toHaveCount(
      0,
    );
    await expect(
      popup.getByRole("region", { name: "Trường tuỳ chỉnh" }),
    ).toContainText("Bán hàng");
  });

  test("the picker is busy while a slow move is saved", async ({ page }) => {
    test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    await open(page, "");
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Đang làm" }).first().click();
    await page.getByRole("option", { name: "Chờ duyệt" }).click();
    const picker = popup
      .getByRole("button", { name: /Chờ duyệt|Đang làm/ })
      .first();
    await expect(picker).toHaveAttribute("aria-busy", "true");
    await expect(picker).toBeDisabled();
    await expect(picker).toHaveAttribute("aria-busy", "false");
  });
});

test.describe("owner, a refused write", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      !faults.includes(STATUS_PATH),
    "Needs the owner world with the status write refused",
  );

  test("says the card was not moved and offers a retry", async ({ page }) => {
    test.skip(!faults.includes(`${STATUS_PATH}=network`), "Needs =network");
    await open(page, "");
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Đang làm" }).first().click();
    await page.getByRole("option", { name: "Chờ duyệt" }).click();
    const notice = popup
      .getByRole("alert")
      .filter({ hasText: "Không chuyển được thẻ" });
    await expect(notice).toBeVisible();
    await expect(notice.getByRole("button", { name: "Thử lại" })).toBeVisible();
  });

  test("a version refused as stale shows the card was edited by someone else", async ({
    page,
  }) => {
    test.skip(!faults.includes(`${STATUS_PATH}=409`), "Needs =409");
    await open(page, "");
    const popup = page.getByRole("dialog");
    await popup.getByRole("button", { name: "Đang làm" }).first().click();
    await page.getByRole("option", { name: "Chờ duyệt" }).click();
    await expect(
      popup.getByRole("alert").filter({ hasText: "vừa sửa thẻ này" }),
    ).toBeVisible();
  });
});

test.describe("viewer", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default",
    "Needs the default viewer scenario",
  );

  test("sees the column as a label, and the notice says the card is view-only", async ({
    page,
  }) => {
    await open(page, "");
    const popup = page.getByRole("dialog");
    await expect(popup.getByRole("button", { name: "Đang làm" })).toHaveCount(
      0,
    );
    await expect(popup.getByText("Đang làm").first()).toBeVisible();
    await expect(popup.getByRole("status").first()).toContainText(
      "chỉ có quyền xem",
    );
  });
});
