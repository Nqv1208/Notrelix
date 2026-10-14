import { expect, test, type Page } from "@playwright/test";

/**
 * Command palette (Final-Command) behaviour on the mock backend: the shortcut,
 * focus in and back out, the focus trap, typing and the arrows, the type
 * filters, opening a result, the `>` commands, the commands that run (theme,
 * Doc creation dialog, shortcuts sheet, workspace switching) and the ones
 * drawn inert. Recents are read after opening a Board and a Doc inside the
 * app (the mock world resets on a page load).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BASE = "/workspaces/mock-workspace-primary";
const NOW = new Date("2025-01-15T10:40:00+07:00");

async function open(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(`${BASE}/home`);
  await expect(
    page.getByRole("button", { name: /Tìm hoặc chạy lệnh/ }),
  ).toBeVisible({ timeout: 20_000 });
}

const dialog = (page: Page) =>
  page.getByRole("dialog", { name: "Tìm kiếm và lệnh" });
const input = (page: Page) => page.getByRole("combobox");
const options = (page: Page) => page.getByRole("option");
const selected = (page: Page) =>
  page.locator('[role="option"][aria-selected="true"]');

test.describe("Command palette (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("opens with the shortcut, focuses the query and closes with Escape", async ({
    page,
  }) => {
    await open(page);
    await expect(dialog(page)).toHaveCount(0);
    await page.keyboard.press("Meta+k");
    await expect(dialog(page)).toBeVisible();
    await expect(input(page)).toBeFocused();
    // The shortcut toggles it.
    await page.keyboard.press("Meta+k");
    await expect(dialog(page)).toHaveCount(0);
    await page.keyboard.press("Control+k");
    await expect(dialog(page)).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialog(page)).toHaveCount(0);
  });

  test("opened from the sidebar row, Escape returns focus to the row", async ({
    page,
  }) => {
    await open(page);
    const row = page.getByRole("button", { name: /Tìm hoặc chạy lệnh/ });
    await row.focus();
    await page.keyboard.press("Enter");
    await expect(input(page)).toBeFocused();
    await page.keyboard.press("Escape");
    await expect(row).toBeFocused();
  });

  test("traps focus: Tab never leaves the dialog", async ({ page }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("erp");
    // With results, Tab is the filter key and stays in the query.
    await page.keyboard.press("Tab");
    await expect(input(page)).toBeFocused();
    // Without filters (nothing typed), Tab moves inside the dialog only.
    await input(page).fill("");
    for (let i = 0; i < 6; i += 1) {
      await page.keyboard.press("Tab");
      const inside = await page.evaluate(
        () =>
          !!document.activeElement?.closest('[role="dialog"]') ||
          document.activeElement === document.body,
      );
      expect(inside).toBe(true);
    }
    for (let i = 0; i < 4; i += 1) await page.keyboard.press("Shift+Tab");
    const stillInside = await page.evaluate(
      () => !!document.activeElement?.closest('[role="dialog"]'),
    );
    expect(stillInside).toBe(true);
  });

  test("lists the recents after a Board and a Doc were opened, newest first", async ({
    page,
  }) => {
    await open(page);
    await page
      .getByRole("link", { name: /^Tài liệu/ })
      .first()
      .click();
    await page.locator('main a[href*="/boards/"]').first().click();
    await expect(page).toHaveURL(/\/boards\//);
    await page
      .getByRole("link", { name: /^Tài liệu/ })
      .first()
      .click();
    await page.locator('main a[href*="/docs/"]').first().click();
    await expect(page).toHaveURL(/\/docs\//);
    await page.getByRole("link", { name: "Trang chủ" }).first().click();
    await page.keyboard.press("Meta+k");
    await expect(page.getByText("Mở gần đây")).toBeVisible();
    const first = options(page).first();
    await expect(first).toContainText("Tài liệu");
    await expect(first).toHaveAttribute("aria-selected", "true");
    await expect(options(page).nth(1)).toContainText("Board");
    // Enter opens the selected recent Doc.
    await page.keyboard.press("Enter");
    await expect(page).toHaveURL(/\/docs\//);
    await expect(dialog(page)).toHaveCount(0);
  });

  test("typing searches Boards, Docs and Items with counts and a highlighted term", async ({
    page,
  }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("erp");
    const tabs = page.getByRole("tab");
    await expect(tabs).toHaveCount(6);
    await expect(tabs.first()).toHaveAttribute("aria-selected", "true");
    await expect(
      page.locator('[data-f="gh"]').filter({ hasText: "Công việc" }),
    ).toBeVisible();
    await expect(
      page.locator('[data-f="gh"]').filter({ hasText: /^Board/ }),
    ).toBeVisible();
    await expect(page.locator('[data-f="t"] [data-f="hl"]').first()).toHaveText(
      /erp/i,
    );
    // Diacritics are ignored.
    await input(page).fill("khao sat quy trinh");
    await expect(options(page).first()).toContainText("Khảo sát quy trình");
  });

  test("arrows move the selection (aria-activedescendant follows) and Enter opens an item on its Board", async ({
    page,
  }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("khao sat quy trinh");
    const first = options(page).first();
    await expect(first).toHaveAttribute("aria-selected", "true");
    const id = await first.getAttribute("id");
    await expect(input(page)).toHaveAttribute("aria-activedescendant", id!);
    await page.keyboard.press("ArrowDown");
    await expect(first).toHaveAttribute("aria-selected", "false");
    const second = selected(page);
    await expect(input(page)).toHaveAttribute(
      "aria-activedescendant",
      (await second.getAttribute("id"))!,
    );
    await page.keyboard.press("ArrowUp");
    await expect(first).toHaveAttribute("aria-selected", "true");
    await page.keyboard.press("Enter");
    await expect(page).toHaveURL(/\/boards\/[^/?]+\?.*item=/);
    await expect(dialog(page)).toHaveCount(0);
  });

  test("a click opens a Board; the filter tabs narrow and Tab cycles them", async ({
    page,
  }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("erp");
    await page.getByRole("tab", { name: /^Board/ }).click();
    await expect(page.getByRole("tab", { name: /^Board/ })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(options(page)).toHaveCount(1);
    await expect(input(page)).toBeFocused();
    // Tab goes to the next filter, Shift+Tab back.
    await page.keyboard.press("Tab");
    await expect(page.getByRole("tab", { name: /^Người/ })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await page.keyboard.press("Shift+Tab");
    await expect(page.getByRole("tab", { name: /^Board/ })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await options(page).first().click();
    await expect(page).toHaveURL(/\/boards\/mock-board-erp/);
  });

  test("> lists commands only, Enter runs the first one that can run", async ({
    page,
  }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill(">");
    await expect(page.getByRole("tab")).toHaveCount(0);
    await input(page).fill("hop thu");
    await expect(
      page.getByText("Lệnh khớp “hop thu”", { exact: false }),
    ).toBeVisible();
    await expect(options(page)).toHaveCount(1);
    await expect(options(page).first()).toContainText("Chuyển tới Hộp thư");
    await page.keyboard.press("Enter");
    await expect(page).toHaveURL(/\/inbox/);
  });

  test("Backspace on an empty term removes the > chip", async ({ page }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill(">");
    await expect(page.locator('[data-f="qpre"]')).toHaveText(">");
    await page.keyboard.press("Backspace");
    await expect(page.locator('[data-f="qpre"]')).toHaveCount(0);
    await expect(input(page)).toHaveValue("");
  });

  test("@ lists people and # lists Boards", async ({ page }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("@");
    await expect(options(page).first()).toBeVisible();
    await expect(page.getByRole("tab")).toHaveCount(0);
    await options(page).first().click();
    await expect(page).toHaveURL(/\/members/);
    await page.keyboard.press("Meta+k");
    await input(page).fill("#erp");
    await expect(options(page)).toHaveCount(1);
    await expect(options(page).first()).toContainText("Triển khai ERP Q4");
  });

  test("commands without a producer are inert and say why; nothing runs", async ({
    page,
  }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    const create = options(page).filter({ hasText: "Tạo công việc" });
    await expect(create).toHaveAttribute("aria-disabled", "true");
    await expect(create).toHaveAttribute("title", /chưa khả dụng/);
    await create.click({ force: true });
    await expect(dialog(page)).toBeVisible();
    await expect(page).toHaveURL(/\/home/);
    // The scope chip is inert as well.
    await expect(
      dialog(page).getByRole("button", { name: /Phạm vi tìm kiếm/ }),
    ).toHaveAttribute("aria-disabled", "true");
    // Selection starts on the first row that can run, not the inert one.
    await expect(selected(page)).not.toHaveAttribute("aria-disabled", "true");
  });

  test("no match explains and offers only inert suggestions", async ({
    page,
  }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("zzqqxx");
    await expect(page.getByText(/Không tìm thấy “zzqqxx” trong/)).toBeVisible();
    await expect(page.getByRole("tab")).toHaveCount(0);
    await expect(options(page)).toHaveCount(2);
    await page.keyboard.press("Enter");
    await expect(dialog(page)).toBeVisible();
  });

  test("the theme command switches the theme for real", async ({ page }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("giao dien");
    await options(page).filter({ hasText: "Đổi giao diện sang Đêm" }).click();
    await expect(dialog(page)).toHaveCount(0);
    await expect
      .poll(() =>
        page.evaluate(() => window.localStorage.getItem("notrelix-theme-mode")),
      )
      .toBe("dark");
    await page.keyboard.press("Meta+k");
    await expect(
      options(page).filter({ hasText: "Đổi giao diện sang Sáng" }),
    ).toHaveCount(1);
  });

  test("the Doc command opens the real creation dialog", async ({ page }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await options(page).filter({ hasText: "Tạo tài liệu" }).click();
    await expect(dialog(page)).toHaveCount(0);
    await expect(
      page.getByRole("dialog").getByRole("textbox").first(),
    ).toBeVisible();
  });

  test("the switch-workspace command moves focus to the workspaces in the rail", async ({
    page,
  }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill(">workspace");
    await options(page).filter({ hasText: "Chuyển workspace" }).click();
    await expect(dialog(page)).toHaveCount(0);
    await expect(
      page.locator('[data-presentation="pinned"] [aria-current="page"]'),
    ).toBeFocused();
  });

  test("the shortcuts command opens the shortcut sheet", async ({ page }) => {
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("phim tat");
    await page.keyboard.press("Enter");
    await expect(page.getByRole("dialog", { name: /Phím tắt/ })).toBeVisible();
  });

  test("at 1100 px the dialog still fits the window and its list scrolls", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1100, height: 760 });
    await open(page);
    await page.keyboard.press("Meta+k");
    await input(page).fill("e");
    await expect(options(page).first()).toBeVisible();
    await page.evaluate(() =>
      Promise.all(
        document.getAnimations().map((a) => a.finished.catch(() => 0)),
      ),
    );
    const box = await page.locator('[data-f="pal"]').boundingBox();
    expect(box!.y + box!.height).toBeLessThanOrEqual(760);
    await expect(page.locator('[data-f="foot"]')).toBeVisible();
  });
});
