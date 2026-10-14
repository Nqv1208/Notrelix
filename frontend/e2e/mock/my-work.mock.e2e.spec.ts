import { expect, test, type Page } from "@playwright/test";

/**
 * My work (Final-MyWork) behaviour on the mock backend: tabs, grouping,
 * filters, search, selection, completing, the item sheet, the week card and
 * the keyboard. The page clock is the mock world's "today" so the assigned
 * items fall into the overdue, today, this-week and later windows.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const PAGE = "/workspaces/mock-workspace-primary/my-work";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function openMyWork(page: Page, query = "") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${PAGE}${query}`);
  await expect(
    page.getByRole("heading", { level: 1, name: "Việc của tôi" }),
  ).toBeVisible();
  await expect(page.locator('[data-f="row"]').first()).toBeVisible();
}

const groupHeadings = (page: Page) => page.locator('[data-f="gh"]');

test.describe("My work (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("lists the member's items by due window with counts that add up", async ({
    page,
  }) => {
    await openMyWork(page);
    await expect(groupHeadings(page).first()).toContainText("Quá hạn");
    await expect(groupHeadings(page).nth(1)).toContainText("Hôm nay");
    // The Home mockup's windows: 1 overdue, 2 today, 2 this week, 2 later.
    const windows = await groupHeadings(page).allInnerTexts();
    expect(windows.map((text) => text.replace(/\s+/g, " ").trim())).toEqual([
      expect.stringMatching(/^Quá hạn 1\b/),
      expect.stringMatching(/^Hôm nay 2\b/),
      expect.stringMatching(/^Tuần này 2\b/),
      expect.stringMatching(/^Sau đó 2\b/),
    ]);
    const total = Number(
      (
        await page
          .getByRole("tab", { name: /^Tất cả/ })
          .locator('[data-f="tabn"]')
          .innerText()
      ).trim(),
    );
    const counts = await groupHeadings(page)
      .locator('[data-f="gn"]')
      .allInnerTexts();
    expect(counts.map(Number).reduce((sum, n) => sum + n, 0)).toBe(total);
    expect(total).toBe(7);
    // The department card adds up to the same total.
    const rail = page.getByRole("complementary", {
      name: "Tóm tắt việc của tôi",
    });
    await expect(rail).toContainText(`${total} việc`);
  });

  test("the week card and the department card count the real items", async ({
    page,
  }) => {
    await openMyWork(page);
    const rail = page.getByRole("complementary", {
      name: "Tóm tắt việc của tôi",
    });
    // Mon 12: 2, Wed 14: 1, Fri 16: 1, and the item overdue since Sat 10/10.
    const days = rail.locator('[data-f="day"]');
    await expect(days).toHaveCount(7);
    const counts = (await days.allInnerTexts()).map((text) =>
      text.replace(/\s+/g, " ").trim(),
    );
    expect(counts.map((text) => text.split(" ").pop())).toEqual([
      "2",
      "0",
      "1",
      "0",
      "1",
      "0",
      "0",
    ]);
    await expect(rail).toContainText("1 việc quá hạn");
    // Departments: Công nghệ 4, Kế toán 2, Marketing 1.
    await expect(rail).toContainText(/Công nghệ\s*4 việc/);
    await expect(rail).toContainText(/Kế toán\s*2 việc/);
    await expect(rail).toContainText(/Marketing\s*1 việc/);
  });

  test("a due label never wraps and the dates agree with the Board", async ({
    page,
  }) => {
    await openMyWork(page);
    for (const due of await page.locator('[data-f="du"]').all()) {
      const box = await due.boundingBox();
      const lineHeight = await due.evaluate(
        (node) => parseFloat(getComputedStyle(node).lineHeight) || 18,
      );
      const lines = await due.evaluate(
        (node) =>
          Array.from(node.children).filter(
            (child) => !child.classList.contains("sr-only"),
          ).length,
      );
      // One line for the label plus at most one small line under it.
      expect(box!.height).toBeLessThan(lineHeight * (1 + lines) + 2);
    }
    const ticket = page.locator('[data-f="row"]', {
      hasText: "Kiểm thử phân hệ công nợ",
    });
    await expect(ticket.locator('[data-f="du"]')).toContainText("Hôm nay");
    await expect(ticket.locator('[data-f="du"]')).not.toContainText("Quá hạn");
  });

  test("the tabs with no producer say why and do not switch", async ({
    page,
  }) => {
    await openMyWork(page);
    for (const name of ["Cần tôi duyệt", "Đang theo dõi", "Đã xong"]) {
      const tab = page.getByRole("tab", { name });
      await expect(tab).toHaveAttribute("aria-disabled", "true");
      await expect(tab).toHaveAttribute("title", /chưa có dữ liệu/);
      await tab.click({ force: true });
      await expect(page.getByRole("tab", { name: /^Tất cả/ })).toHaveAttribute(
        "aria-selected",
        "true",
      );
    }
    await page.getByRole("tab", { name: /^Giao cho tôi/ }).click();
    await expect(
      page.getByRole("tab", { name: /^Giao cho tôi/ }),
    ).toHaveAttribute("aria-selected", "true");
  });

  test("groups by Board and not by Status (no Status in the read)", async ({
    page,
  }) => {
    await openMyWork(page);
    await page.getByRole("button", { name: /^Nhóm/ }).click();
    const status = page.getByRole("menuitemradio", { name: "Theo trạng thái" });
    await expect(status).toHaveAttribute("aria-disabled", "true");
    await page.getByRole("menuitemradio", { name: "Theo board" }).click();
    await expect(page.getByRole("button", { name: /^Nhóm/ })).toContainText(
      "Theo board",
    );
    await expect(groupHeadings(page).first()).toContainText(
      "Triển khai ERP Q4",
    );
    // A row leaves out its Board when the list is grouped by it.
    await expect(
      page.locator('[data-f="rc"]').first().locator('[data-f="why"]'),
    ).toBeVisible();
    await page.getByRole("button", { name: /^Nhóm/ }).click();
    await page.getByRole("menuitemradio", { name: "Theo hạn" }).click();
    await expect(groupHeadings(page).first()).toContainText("Quá hạn");
  });

  test("searches and filters by department and Board", async ({ page }) => {
    await openMyWork(page);
    const rows = page.locator('[data-f="row"]');
    const all = await rows.count();
    const search = page.getByRole("searchbox", {
      name: "Tìm trong việc của tôi",
    });
    await search.fill("công nợ");
    const found = await rows.count();
    expect(found).toBeGreaterThan(0);
    expect(found).toBeLessThan(all);
    for (const title of await page.locator('[data-f="rt"]').allInnerTexts()) {
      expect(title).toContain("công nợ");
    }
    await search.fill("không có việc nào tên thế này");
    await expect(page.getByText("Không có việc nào khớp.")).toBeVisible();
    await search.fill("");
    await expect(rows).toHaveCount(all);

    await page.getByRole("button", { name: /^Board/ }).click();
    await page
      .getByRole("listbox", { name: "Board" })
      .getByRole("option", { name: "Đóng sổ tháng 10" })
      .click();
    await expect(page.getByRole("button", { name: /^Board/ })).toContainText(
      "1",
    );
    for (const board of await page.locator('[data-f="rc"]').allInnerTexts()) {
      expect(board).toContain("Đóng sổ tháng 10");
    }
    await page.keyboard.press("Escape");
  });

  test("a due day in the week card filters the list and toggles back", async ({
    page,
  }) => {
    await openMyWork(page);
    const rows = page.locator('[data-f="row"]');
    const all = await rows.count();
    const days = page
      .getByRole("complementary", { name: "Tóm tắt việc của tôi" })
      .locator('[data-f="day"]');
    await expect(days).toHaveCount(7);
    const today = days.nth(0);
    await today.click();
    await expect(today).toHaveAttribute("aria-pressed", "true");
    const filtered = await rows.count();
    expect(filtered).toBeLessThan(all);
    // Every row left is due today: its due column says so.
    for (const due of await page.locator('[data-f="du"]').allInnerTexts()) {
      expect(due).toContain("Hôm nay");
    }
    await today.click();
    await expect(rows).toHaveCount(all);
  });

  test("selects rows, shows the bulk bar and completes the selection", async ({
    page,
  }) => {
    await openMyWork(page);
    const first = page.locator('[data-f="row"]').nth(0);
    const second = page.locator('[data-f="row"]').nth(1);
    await first.hover();
    await first.getByRole("checkbox").click();
    await expect(
      page.getByRole("toolbar", { name: "Thao tác với 1 việc đã chọn" }),
    ).toBeVisible();
    await second.getByRole("checkbox").click();
    const bar = page.getByRole("toolbar", {
      name: "Thao tác với 2 việc đã chọn",
    });
    await expect(bar).toContainText("2 đã chọn");
    for (const name of ["Đổi hạn", "Đổi trạng thái", "Bỏ theo dõi"]) {
      await expect(bar.getByRole("button", { name })).toHaveAttribute(
        "aria-disabled",
        "true",
      );
    }
    const titles = await page.locator('[data-f="rt"]').allInnerTexts();
    await bar.getByRole("button", { name: "Xong" }).click();
    await expect(page.getByText(/^Đã hoàn thành /).first()).toBeVisible();
    await expect(page.getByRole("toolbar")).toHaveCount(0);
    expect(titles.length).toBeGreaterThan(2);
  });

  test("Escape clears the selection", async ({ page }) => {
    await openMyWork(page);
    const first = page.locator('[data-f="row"]').first();
    await first.hover();
    await first.getByRole("checkbox").click();
    await expect(page.getByRole("toolbar")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("toolbar")).toHaveCount(0);
  });

  test("the tick completes one item and the toast names it", async ({
    page,
  }) => {
    await openMyWork(page);
    const first = page.locator('[data-f="row"]').first();
    const title = (await first.locator('[data-f="rt"]').innerText()).trim();
    const same = page.locator('[data-f="rt"] a', { hasText: title });
    const before = await same.count();
    await first.getByRole("button", { name: /^Đánh dấu xong / }).click();
    await expect(page.getByText(`Đã hoàn thành ${title}.`)).toBeVisible();
    await expect(same).toHaveCount(before - 1);
  });

  test("J and K move between titles, X selects, E completes", async ({
    page,
  }) => {
    await openMyWork(page);
    const links = page.locator('[data-f="rt"] a');
    await page.keyboard.press("j");
    await expect(links.nth(0)).toBeFocused();
    await page.keyboard.press("j");
    await expect(links.nth(1)).toBeFocused();
    await page.keyboard.press("k");
    await expect(links.nth(0)).toBeFocused();
    await page.keyboard.press("x");
    await expect(
      page.locator('[data-f="row"]').first().getByRole("checkbox"),
    ).toHaveAttribute("aria-checked", "true");
    await page.keyboard.press("Escape");
    const title = (await links.nth(0).innerText()).trim();
    await page.keyboard.press("e");
    await expect(page.getByText(`Đã hoàn thành ${title}.`)).toBeVisible();
  });

  test("a title opens the item in the side sheet and Escape closes it", async ({
    page,
  }) => {
    await openMyWork(page);
    const first = page.locator('[data-f="row"]').first();
    const title = (await first.locator('[data-f="rt"]').innerText()).trim();
    await first.locator('[data-f="rt"] a').click();
    await expect(page).toHaveURL(/\/my-work\?item=/);
    const sheet = page.getByRole("dialog");
    await expect(sheet).toBeVisible();
    await expect(sheet).toContainText(title);
    // The list narrows: the open row is marked and the rail makes room.
    await expect(first).toHaveAttribute("aria-current", "true");
    await expect(
      page.getByRole("complementary", { name: "Tóm tắt việc của tôi" }),
    ).toHaveCount(0);
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(page).not.toHaveURL(/item=/);
    await expect(
      page.getByRole("complementary", { name: "Tóm tắt việc của tôi" }),
    ).toBeVisible();
  });

  test("an item in the URL opens its sheet on load", async ({ page }) => {
    await openMyWork(page);
    const id = await page
      .locator('[data-f="row"]')
      .first()
      .getAttribute("data-item-id");
    await page.goto(`${PAGE}?item=${id}`);
    await expect(page.getByRole("dialog")).toBeVisible();
    await expect(
      page.locator(`[data-f="row"][data-item-id="${id}"]`),
    ).toHaveAttribute("aria-current", "true");
  });

  test("the create popover lists Boards and creates nothing without a write", async ({
    page,
  }) => {
    await openMyWork(page);
    await page.getByRole("button", { name: "Thêm việc" }).click();
    const dialog = page.getByRole("dialog", { name: "Thêm việc" });
    await expect(dialog.getByRole("radio").first()).toHaveAttribute(
      "aria-checked",
      "true",
    );
    await dialog.getByLabel("Tên việc").fill("Gửi biên bản họp UAT");
    const submit = dialog.getByRole("button", { name: /^Tạo việc/ });
    await expect(submit).toHaveAttribute("aria-disabled", "true");
    await expect(submit).toHaveAttribute("title", /không|chưa/);
    await submit.click({ force: true });
    await expect(dialog).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialog).toHaveCount(0);
  });

  test("My calendar is inert and says why", async ({ page }) => {
    await openMyWork(page);
    const calendar = page.getByRole("button", { name: "Lịch của tôi" });
    await expect(calendar).toHaveAttribute("aria-disabled", "true");
    await expect(calendar).toHaveAttribute("title", /chưa có/);
  });

  test("the sidebar count and the page count agree on the loaded items", async ({
    page,
  }) => {
    await openMyWork(page);
    const total = (
      await page
        .getByRole("tab", { name: /^Tất cả/ })
        .locator('[data-f="tabn"]')
        .innerText()
    ).trim();
    const nav = page.getByRole("navigation", {
      name: "Điều hướng không gian làm việc",
    });
    await expect(
      nav.getByRole("link", { name: /^Việc của tôi/ }),
    ).toContainText(total);
  });
});
