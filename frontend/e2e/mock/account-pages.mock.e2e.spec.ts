import { expect, test, type Page } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const account = "/workspaces/mock-workspace-primary/account";

test.beforeEach(async ({ page }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

/**
 * Radix checks a radio when an arrow key moves focus onto it, but only while
 * the key is still down; Playwright's `press` releases it in the same tick, so
 * hold the key for a human-scale moment like a real keyboard does.
 */
async function holdKey(page: Page, key: string) {
  await page.keyboard.down(key);
  await page.waitForTimeout(80);
  await page.keyboard.up(key);
}

const PAGES = [
  { path: "profile", heading: "Hồ sơ" },
  { path: "appearance", heading: "Giao diện" },
  { path: "security", heading: "Bảo mật" },
  { path: "notifications", heading: "Thông báo" },
] as const;

/**
 * Horizontal overflow at the page level AND per control: the page-level
 * check alone can pass when an ancestor clips its content, so every visible
 * control must also sit inside the viewport.
 */
async function overflow(page: Page) {
  return page.evaluate(() => {
    const viewport = window.innerWidth;
    const controls = Array.from(
      document.querySelectorAll<HTMLElement>(
        "main input, main button, main [role='switch'], main [role='radio'], main [role='combobox'], main a",
      ),
    );
    const outside = controls
      .map((element) => ({
        label:
          element.getAttribute("aria-label") ??
          element.textContent?.trim().slice(0, 40) ??
          element.tagName,
        box: element.getBoundingClientRect(),
      }))
      .filter(
        ({ box }) =>
          box.width > 0 && (box.right > viewport + 0.5 || box.left < -0.5),
      )
      .map(({ label, box }) => `${label} [${box.left}..${box.right}]`);
    return {
      page: document.documentElement.scrollWidth - viewport,
      controls: controls.length,
      outside,
    };
  });
}

test("Account pages stay inside a 320 px viewport in Vietnamese (WX-QUAL-001)", async ({
  page,
}) => {
  await page.setViewportSize({ width: 320, height: 800 });
  for (const { path, heading } of PAGES) {
    await page.goto(`${account}/${path}`);
    await expect(
      page.getByRole("heading", { level: 1, name: heading }),
    ).toBeVisible();
    // The page has finished loading: no skeleton is left.
    await expect(page.getByRole("status")).toHaveCount(0);
    const result = await overflow(page);
    expect(result.controls, `${path} has controls`).toBeGreaterThan(0);
    expect(result.page, `${path} page overflow`).toBeLessThanOrEqual(0);
    expect(result.outside, `${path} controls outside the viewport`).toEqual([]);
  }
});

test("Profile saves only after a change and reports the result in Vietnamese", async ({
  page,
}) => {
  await page.goto(`${account}/profile`);
  const save = page.getByRole("button", { name: "Lưu thay đổi" });
  await expect(save).toBeDisabled();
  const name = page.getByLabel("Tên");
  await name.fill("Ada Lovelace Cập Nhật");
  await expect(save).toBeEnabled();
  await save.click();
  await expect(page.getByText("Đã cập nhật hồ sơ.")).toBeVisible();
});

test("Appearance radios move with the arrow keys and the sidebar switch toggles with Space", async ({
  page,
}) => {
  await page.goto(`${account}/appearance`);
  const mode = page.getByRole("radiogroup", { name: "Chế độ hiển thị" });
  const light = mode.getByRole("radio", { name: "Sáng" });
  await expect(light).toBeChecked();
  await light.focus();
  await holdKey(page, "ArrowDown");
  await expect(mode.getByRole("radio", { name: "Tối" })).toBeChecked();
  await expect(page.locator("html")).toHaveClass(/dark/);

  const sidebar = page.getByRole("switch", { name: "Thu gọn theo mặc định" });
  await expect(sidebar).toBeVisible();
  const before = await sidebar.getAttribute("aria-checked");
  await sidebar.focus();
  await page.keyboard.press("Space");
  await expect(sidebar).toHaveAttribute(
    "aria-checked",
    before === "true" ? "false" : "true",
  );
  await expect(page.getByText("Đã lưu.")).toBeVisible();
});

test("Notification switches are named by their label and toggle with the Space key", async ({
  page,
}) => {
  await page.goto(`${account}/notifications`);
  await expect(
    page.getByRole("heading", { level: 1, name: "Thông báo" }),
  ).toBeVisible();
  const save = page.getByRole("button", { name: "Lưu tùy chọn" });
  await expect(save).toBeDisabled();

  for (const name of ["Thông báo qua email", "Thông báo đẩy trên máy tính"]) {
    const control = page.getByRole("switch", { name });
    await expect(control).toBeVisible();
    const before = await control.getAttribute("aria-checked");
    await control.focus();
    await page.keyboard.press("Space");
    await expect(control).toHaveAttribute(
      "aria-checked",
      before === "true" ? "false" : "true",
    );
  }
  await expect(save).toBeEnabled();

  // Tab order reaches both switches and the save button.
  await page.getByRole("switch", { name: "Thông báo qua email" }).focus();
  await page.keyboard.press("Tab");
  await expect(
    page.getByRole("switch", { name: "Thông báo đẩy trên máy tính" }),
  ).toBeFocused();
  await page.keyboard.press("Tab");
  await expect(save).toBeFocused();
});
