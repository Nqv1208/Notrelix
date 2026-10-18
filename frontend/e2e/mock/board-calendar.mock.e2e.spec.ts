import { expect, test, type Page } from "@playwright/test";

/**
 * The ERP board's Calendar on the mock world as Final-Calendar draws October
 * 2026: the three phases and the go-live milestone, ERP-142 as the only bar,
 * the finished items struck through on the day they were finished, and the
 * day view's 17:00 slot for ERP-144. Geometry lives in
 * `design-fidelity-calendar.mock.e2e.spec.ts`.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openCalendar(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=calendar${query}`);
  await expect(page.getByRole("grid")).toBeVisible();
}

test.describe("Board Calendar data (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the phases and the Go-live milestone are drawn", async ({ page }) => {
    await openCalendar(page);
    const phases = page.locator('[data-calendar-marker="phase"]');
    await expect(phases).toHaveCount(3);
    await expect(phases.nth(0)).toContainText(
      "UAT phân hệ bán hàng với phòng Kinh doanh",
    );
    await expect(phases.nth(1)).toContainText("Đào tạo phòng Kế toán");
    await expect(phases.nth(2)).toContainText(
      "Chạy song song hệ thống cũ và ERP",
    );
    const milestone = page.locator('[data-calendar-marker="milestone"]');
    await expect(milestone).toHaveCount(1);
    await expect(milestone).toContainText("Go-live ERP");
  });

  test("ERP-142 is the only bar; the other items are one-day chips", async ({
    page,
  }) => {
    await openCalendar(page);
    const bars = page
      .locator("[data-calendar-event]")
      .filter({ hasText: "Chuyển dữ liệu khách hàng" });
    await expect(bars).toHaveCount(2);
    for (const key of ["ERP-144", "ERP-147", "ERP-150", "ERP-151"]) {
      const chips = page.locator("[data-calendar-event]").filter({
        has: page.locator(`xpath=self::*[contains(@aria-label, "${key}")]`),
      });
      await expect(chips).toHaveCount(1);
      const box = (await chips.first().boundingBox())!;
      expect(box.width).toBeLessThan(130);
    }
  });

  test("the finished items are struck through on the day they were finished", async ({
    page,
  }) => {
    await openCalendar(page);
    const done = page
      .locator("[data-calendar-event]")
      .filter({ has: page.locator(".line-through") });
    await expect(done.first()).toBeVisible();
    await expect(
      page
        .locator("[data-calendar-event]")
        .filter({ hasText: "ERP-129" })
        .first(),
    ).toBeVisible();
  });

  test("a filter hides the phases with the items it hides", async ({
    page,
  }) => {
    await openCalendar(page, "&filter=p.low~o");
    await expect(page.locator("[data-calendar-marker]")).toHaveCount(0);
  });

  test("the day view puts ERP-144 in the 17:00 slot", async ({ page }) => {
    await openCalendar(page);
    await page.getByRole("radio", { name: "Ngày" }).click();
    await expect(page.getByText("Hạn 17:00").first()).toBeVisible();
  });

  test("the sidebar lists the department's boards as the mockup does", async ({
    page,
  }) => {
    await openCalendar(page);
    const tree = page.getByRole("tree");
    const names = await tree
      .getByRole("treeitem")
      .evaluateAll((items) =>
        items.slice(0, 8).map((item) => item.textContent?.trim() ?? ""),
      );
    const joined = names.join("|");
    expect(joined.indexOf("Triển khai ERP Q4")).toBeLessThan(
      joined.indexOf("Hạ tầng & bảo mật"),
    );
    expect(joined.indexOf("Hạ tầng & bảo mật")).toBeLessThan(
      joined.indexOf("Hỗ trợ nội bộ"),
    );
    expect(joined.indexOf("Hỗ trợ nội bộ")).toBeLessThan(
      joined.indexOf("Nhóm Hạ tầng"),
    );
  });
});
