import { expect, test, type Page } from "@playwright/test";

/**
 * Home (Final-Home) behaviour on the mock backend: what the member can do
 * from the page, with the page clock at the mock world's "today" so the
 * assigned items fall into the overdue, today and this-week windows.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const HOME = "/workspaces/mock-workspace-primary";
const NOW = new Date("2025-01-15T10:40:00+07:00");

async function openHome(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(HOME);
  await expect(
    page.getByRole("heading", { level: 1, name: /^Chào buổi sáng, / }),
  ).toBeVisible();
}

test.describe("Home (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("greets by name and counts what is open", async ({ page }) => {
    await openHome(page);
    await expect(
      page.getByRole("heading", {
        level: 1,
        name: "Chào buổi sáng, Developer",
      }),
    ).toBeVisible();
    await expect(
      page.getByText(/^Thứ Tư, 15 tháng 1 · \d+\+? việc đang mở$/),
    ).toBeVisible();
  });

  test("the due windows list the member's items with real counts", async ({
    page,
  }) => {
    await openHome(page);
    for (const name of [/^Quá hạn/, /^Hôm nay/, /^Tuần này/]) {
      await expect(page.getByRole("region", { name })).toBeVisible();
    }
    const chips = page.getByRole("group", { name: "Lọc nhanh việc cần chú ý" });
    await expect(chips.getByRole("link", { name: /quá hạn$/ })).toBeVisible();
    await expect(
      chips.getByRole("link", { name: /đến hạn hôm nay$/ }),
    ).toBeVisible();
  });

  test("a quick filter and the section link open My work", async ({ page }) => {
    await openHome(page);
    await page
      .getByRole("group", { name: "Lọc nhanh việc cần chú ý" })
      .getByRole("link", { name: /quá hạn$/ })
      .click();
    await expect(page).toHaveURL(/\/my-work$/);

    await openHome(page);
    await page.getByRole("link", { name: "Xem tất cả việc của tôi" }).click();
    await expect(page).toHaveURL(/\/my-work$/);
  });

  test("a title opens the item on its Board", async ({ page }) => {
    await openHome(page);
    await page.locator('[data-f="rt"] a').first().click();
    await expect(page).toHaveURL(/\/boards\/[^/]+\?.*item=/);
  });

  test("the tick completes an item and the toast says so", async ({ page }) => {
    await openHome(page);
    const overdue = page.getByRole("region", { name: /^Quá hạn/ });
    const first = overdue.locator('[data-f="row"]').first();
    const title = (await first.locator('[data-f="rt"]').innerText()).trim();
    const sameTitle = page.getByRole("link", { name: title, exact: true });
    const before = await sameTitle.count();
    await first.getByRole("button", { name: /^Đánh dấu hoàn thành / }).click();
    await expect(page.getByText(`Đã hoàn thành ${title}.`)).toBeVisible();
    await expect(sameTitle).toHaveCount(before - 1);
  });

  test("the cards without a producer say why, never fake a result", async ({
    page,
  }) => {
    await openHome(page);
    const calendar = page.getByRole("button", { name: "Lịch của tôi" });
    await expect(calendar).toHaveAttribute("aria-disabled", "true");
    await expect(calendar).toHaveAttribute("title", /chưa có/);
    const add = page.getByRole("button", { name: "Thêm công việc" });
    await expect(add).toHaveAttribute("aria-disabled", "true");
    await add.click({ force: true });
    await expect(page).toHaveURL(new RegExp(`${HOME}$`));
    // No approvals, mentions or schedule exist: no card, no count.
    await expect(
      page.getByRole("region", { name: /Cần bạn phản hồi/ }),
    ).toHaveCount(0);
    await expect(
      page.getByRole("region", { name: /Lịch hôm nay/ }),
    ).toHaveCount(0);
  });
});
