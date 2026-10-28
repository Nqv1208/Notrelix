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
const NOW = new Date("2026-10-12T10:40:00+07:00");

/** Home's add form: pick the ERP board, name the item and create it. */
async function addUatItem(page: Page) {
  await page.getByRole("button", { name: "Thêm công việc" }).click();
  await page.getByRole("combobox", { name: "Thêm vào board" }).click();
  await page.getByRole("option", { name: /Triển khai ERP/ }).click();
  await page.getByLabel("Tên công việc").fill("Gửi biên bản họp UAT");
  await page.getByRole("button", { name: "Tạo công việc" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
}

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
        name: "Chào buổi sáng, Hà",
      }),
    ).toBeVisible();
    await expect(
      page.getByText(
        "Thứ Hai, 12 tháng 10 · 3 cuộc họp hôm nay, 7 việc đang mở",
      ),
    ).toBeVisible();
  });

  test("the due windows list the member's items with real counts", async ({
    page,
  }) => {
    await openHome(page);
    // The mockup's windows and counts: 1 overdue, 2 today, 2 this week, and
    // the two items due from next week behind the "Sau đó" line.
    for (const [name, count] of [
      [/^Quá hạn/, 1],
      [/^Hôm nay/, 2],
      [/^Tuần này/, 2],
    ] as const) {
      const region = page.getByRole("region", { name });
      await expect(region).toBeVisible();
      await expect(region.locator('[data-f="row"]')).toHaveCount(count);
    }
    await expect(
      page.getByText("Sau đó · 2 việc đến hạn từ tuần sau"),
    ).toBeVisible();
    const chips = page.getByRole("group", { name: "Lọc nhanh việc cần chú ý" });
    await expect(chips.getByRole("link", { name: "1 quá hạn" })).toBeVisible();
    await expect(
      chips.getByRole("link", { name: "2 đến hạn hôm nay" }),
    ).toBeVisible();
  });

  test("the same item has the same date here and on its Board", async ({
    page,
  }) => {
    await openHome(page);
    const today = page.getByRole("region", { name: /^Hôm nay/ });
    // ERP-144 is due 12/10 (a date, no time) on the Board table: today, never "Quá hạn".
    const row = today.locator('[data-f="row"]', {
      hasText: "Kiểm thử phân hệ công nợ",
    });
    await expect(row.locator('[data-f="due"]')).toHaveText("Hôm nay");
    const overdue = page.getByRole("region", { name: /^Quá hạn/ });
    await expect(overdue.locator('[data-f="due"]')).toHaveText(
      "Quá hạn 2 ngày",
    );
  });

  test("no due label wraps onto a second line", async ({ page }) => {
    await openHome(page);
    for (const due of await page.locator('[data-f="due"]').all()) {
      const box = await due.boundingBox();
      const lineHeight = await due.evaluate(
        (node) => parseFloat(getComputedStyle(node).lineHeight) || 18,
      );
      expect(box!.height).toBeLessThan(lineHeight * 1.5);
    }
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

  test("a row carries its Status, Priority, key and relationship", async ({
    page,
  }) => {
    await openHome(page);
    const row = page
      .getByRole("region", { name: /^Quá hạn/ })
      .locator('[data-f="row"]')
      .first();
    await expect(row.locator('[data-f="pill"]')).toHaveText("Đang làm");
    await expect(row.getByText("ERP-142")).toBeVisible();
    await expect(row.getByText("bạn đang theo dõi")).toBeVisible();
    await expect(row.getByLabel(/Ưu tiên/)).toBeVisible();
  });

  test("all four chips and the meetings line show the member's day", async ({
    page,
  }) => {
    await openHome(page);
    const chips = page.getByRole("group", { name: "Lọc nhanh việc cần chú ý" });
    await expect(
      chips.getByRole("link", { name: "1 chờ bạn duyệt" }),
    ).toBeVisible();
    await expect(
      chips.getByRole("link", { name: "1 nhắc tên bạn" }),
    ).toBeVisible();
    await expect(
      page.getByText(
        "Thứ Hai, 12 tháng 10 · 3 cuộc họp hôm nay, 7 việc đang mở",
      ),
    ).toBeVisible();
  });

  test("the schedule shows today's events with the now marker", async ({
    page,
  }) => {
    await openHome(page);
    const agenda = page.getByRole("region", { name: /Lịch hôm nay/ });
    await expect(agenda).toContainText("Họp đứng nhóm ERP");
    await expect(agenda).toContainText("15 phút · Phòng họp 3");
    await expect(agenda).toContainText("1 giờ · trực tuyến · 6 người");
    await expect(agenda.locator('[data-f="now"]')).toHaveText("10:40 bây giờ");
  });

  test("Duyệt approves the item and the card, chip and row follow", async ({
    page,
  }) => {
    await openHome(page);
    await page.getByRole("button", { name: "Duyệt ERP-138" }).click();
    await expect(page.getByText("Đã duyệt ERP-138.")).toBeVisible();
    await expect(
      page
        .getByRole("group", { name: "Lọc nhanh việc cần chú ý" })
        .getByText("chờ bạn duyệt"),
    ).toHaveCount(0);
    await expect(
      page.getByRole("link", {
        name: "Thiết lập đăng nhập một lần (SSO) cho toàn công ty",
        exact: true,
      }),
    ).toHaveCount(0);
  });

  test("Yêu cầu sửa returns the item to its owner and clears the card", async ({
    page,
  }) => {
    await openHome(page);
    await page.getByRole("button", { name: "Yêu cầu sửa ERP-138" }).click();
    await expect(page.getByText("Đã yêu cầu sửa ERP-138.")).toBeVisible();
    await expect(page.getByText("gửi duyệt").first()).toHaveCount(0);
    await expect(
      page.getByRole("button", { name: "Duyệt ERP-138" }),
    ).toHaveCount(0);
  });

  test("Trả lời posts a reply to the mention and clears it", async ({
    page,
  }) => {
    await openHome(page);
    await page.getByRole("button", { name: "Trả lời", exact: true }).click();
    await page
      .getByRole("textbox", { name: /Trả lời Lý Thu Trang/ })
      .fill("Em đồng ý đề xuất 3 ngày.");
    await page.getByRole("button", { name: "Gửi" }).click();
    await expect(
      page.getByText("Đã gửi trả lời cho Lý Thu Trang."),
    ).toBeVisible();
    await expect(page.getByText("@Hà chị xem giúp")).toHaveCount(0);
  });

  test("Thêm công việc creates an item assigned to the member", async ({
    page,
  }) => {
    await openHome(page);
    await addUatItem(page);
    // The form ticks the member: the item is theirs, so the open count follows.
    await expect(page.getByText(/8 việc đang mở/)).toBeVisible();
  });

  test("Tiếp tục lists the recents with their department", async ({ page }) => {
    await openHome(page);
    const recents = page.locator('[data-f="contbox"]');
    await expect(recents).toContainText("Triển khai ERP Q4");
    await expect(recents).toContainText("Board · Công nghệ · 20 phút trước");
    await expect(recents).toContainText("Tài liệu · Kế toán");
  });

  test("Lịch của tôi has no producer and says why", async ({ page }) => {
    await openHome(page);
    const calendar = page.getByRole("button", { name: "Lịch của tôi" });
    await expect(calendar).toHaveAttribute("aria-disabled", "true");
    await expect(calendar).toHaveAttribute("title", /chưa có/);
  });

  test("a plain browser (no forced clock or locale) shows the demo day in Vietnamese", async ({
    browser,
  }) => {
    const context = await browser.newContext({
      locale: "en-US",
      timezoneId: "Asia/Ho_Chi_Minh",
      viewport: { width: 1440, height: 900 },
    });
    const page = await context.newPage();
    await page.goto(HOME);
    await expect(
      page.getByRole("heading", { level: 1, name: "Chào buổi sáng, Hà" }),
    ).toBeVisible();
    await expect(
      page.getByText(
        "Thứ Hai, 12 tháng 10 · 3 cuộc họp hôm nay, 7 việc đang mở",
      ),
    ).toBeVisible();
    await expect(page.getByText("Quá hạn 2 ngày")).toBeVisible();
    await expect(page.locator('[data-f="now"]')).toContainText("10:4");
    await context.close();
  });

  test("an item added without a due date is listed under Chưa có hạn", async ({
    page,
  }) => {
    await openHome(page);
    await addUatItem(page);
    const region = page.getByRole("region", { name: /^Chưa có hạn/ });
    await expect(region).toContainText("Gửi biên bản họp UAT");
  });
});
