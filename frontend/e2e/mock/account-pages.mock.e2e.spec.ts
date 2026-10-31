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
  { path: "workspaces", heading: "Workspace của bạn" },
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
      page.getByRole("heading", { level: 2, name: heading }),
    ).toBeVisible();
    // The page has finished loading: no named loading region is left (an empty live region stays).
    await expect(page.getByRole("status", { name: /\S/ })).toHaveCount(0);
    const result = await overflow(page);
    expect(result.controls, `${path} has controls`).toBeGreaterThan(0);
    expect(result.page, `${path} page overflow`).toBeLessThanOrEqual(0);
    expect(result.outside, `${path} controls outside the viewport`).toEqual([]);
  }
});

test("The frame names the member and moves between the five pages", async ({
  page,
}) => {
  await page.goto(`${account}/profile`);
  await expect(
    page.getByRole("heading", { level: 1, name: "Tài khoản của bạn" }),
  ).toBeVisible();
  const nav = page.getByRole("navigation", { name: "Cài đặt tài khoản" });
  await expect(nav.getByRole("link")).toHaveCount(5);
  await expect(nav.getByRole("link", { name: "Hồ sơ" })).toHaveAttribute(
    "aria-current",
    "page",
  );
  await nav.getByRole("link", { name: "Workspace của bạn" }).click();
  await expect(page).toHaveURL(/account\/workspaces$/);
  await expect(
    page.getByRole("heading", { level: 2, name: "Workspace của bạn" }),
  ).toBeVisible();
});

test("Profile shows the save bar only after a change and reports the result in Vietnamese", async ({
  page,
}) => {
  await page.goto(`${account}/profile`);
  const bar = page.getByRole("region", { name: "Thay đổi chưa lưu" });
  await expect(bar).toHaveCount(0);
  await page.getByLabel("Số điện thoại").fill("+84 988 000 111");
  await expect(bar).toContainText("Bạn có 1 thay đổi chưa lưu");
  await expect(page.getByText("Đã sửa")).toBeVisible();
  await bar.getByRole("button", { name: "Lưu thay đổi" }).click();
  await expect(page.getByText("Đã cập nhật hồ sơ.")).toBeVisible();
  await expect(bar).toHaveCount(0);
  // The mock lives in the page: go to another page and back without a reload.
  const nav = page.getByRole("navigation", { name: "Cài đặt tài khoản" });
  await nav.getByRole("link", { name: "Bảo mật" }).click();
  await nav.getByRole("link", { name: "Hồ sơ" }).click();
  await expect(page.getByLabel("Số điện thoại")).toHaveValue("+84988000111");
});

test("Appearance radios move with the arrow keys and the sidebar switch toggles with Space", async ({
  page,
}) => {
  await page.goto(`${account}/appearance`);
  const mode = page.getByRole("radiogroup", { name: "Chế độ hiển thị" });
  const light = mode.getByRole("radio", { name: "Sáng" });
  await expect(light).toBeChecked();
  await light.focus();
  await holdKey(page, "ArrowRight");
  await expect(mode.getByRole("radio", { name: "Tối" })).toBeChecked();
  await expect(page.locator("html")).toHaveClass(/dark/);
  await expect(page.getByText("Đã lưu.")).toBeVisible();

  const sidebar = page.getByRole("switch", { name: "Thu gọn theo mặc định" });
  await expect(sidebar).toBeVisible();
  const before = await sidebar.getAttribute("aria-checked");
  await sidebar.focus();
  await page.keyboard.press("Space");
  await expect(sidebar).toHaveAttribute(
    "aria-checked",
    before === "true" ? "false" : "true",
  );
});

test("Security turns two-factor off and signs out the other devices", async ({
  page,
}) => {
  await page.goto(`${account}/security`);
  await expect(
    page.getByRole("heading", {
      level: 3,
      name: "Phiên đăng nhập đang hoạt động",
    }),
  ).toBeVisible();
  await expect(page.getByText("Đăng nhập 2 giờ trước")).toBeVisible();
  await page.getByRole("button", { name: "Đăng xuất Safari · iPad" }).click();
  await expect(page.getByText("Đã đăng xuất.")).toBeVisible();
  await expect(page.getByText("2 phiên đang hoạt động")).toBeVisible();
  // New backup codes need a proof first, then they are shown once.
  await page.getByRole("button", { name: "Tạo mã mới" }).click();
  await page.getByLabel("Mã", { exact: true }).fill("482931");
  await page.getByRole("button", { name: "Xác nhận" }).click();
  await expect(page.getByText("Lưu mã dự phòng của bạn")).toBeVisible();
  await page.getByRole("button", { name: "Tôi đã lưu" }).click();
  await page
    .getByRole("button", { name: "Đăng xuất mọi thiết bị khác" })
    .click();
  // Ending every other device spends its own proof.
  await page.getByLabel("Mã", { exact: true }).fill("482931");
  await page.getByRole("button", { name: "Xác nhận" }).click();
  await expect(page.getByText("1 phiên đang hoạt động")).toBeVisible();
  await page.getByRole("button", { name: "Tắt xác thực hai yếu tố" }).click();
  await page.getByLabel("Mã", { exact: true }).fill("482931");
  await page.getByRole("button", { name: "Xác nhận" }).click();
  // Turning it off ends every session, this one too: the person is told and offered the sign-in.
  await expect(
    page.getByRole("region", { name: "Đã tắt xác thực hai lớp" }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: "Đăng nhập" })).toBeFocused();
});

test("Notification cells are named by event and channel, toggle with Space and save per workspace", async ({
  page,
}) => {
  await page.goto(`${account}/notifications`);
  await expect(
    page.getByRole("heading", { level: 2, name: "Thông báo" }),
  ).toBeVisible();
  // Mobile and quiet hours are not offered.
  await expect(page.getByRole("switch")).toHaveCount(0);
  const cell = page.getByRole("checkbox", {
    name: "Tóm tắt hằng ngày: email",
  });
  await expect(cell).toHaveAttribute("aria-checked", "false");
  await cell.focus();
  await page.keyboard.press("Space");
  await expect(cell).toHaveAttribute("aria-checked", "true");
  await page.getByRole("button", { name: "Lưu tùy chọn" }).click();
  await expect(page.getByText("Đã lưu tùy chọn thông báo.")).toBeVisible();
  const nav = page.getByRole("navigation", { name: "Cài đặt tài khoản" });
  await nav.getByRole("link", { name: "Bảo mật" }).click();
  await nav.getByRole("link", { name: "Thông báo" }).click();
  await expect(
    page.getByRole("checkbox", { name: "Tóm tắt hằng ngày: email" }),
  ).toHaveAttribute("aria-checked", "true");
});

test("Workspaces sets the default and asks before leaving; a sole owner cannot leave", async ({
  page,
}) => {
  await page.goto(`${account}/workspaces`);
  const card = page.getByRole("region", { name: /Đà Nẵng/ });
  await expect(card).toBeVisible();
  // The producer sends no unread count, guest board or other owner.
  await expect(page.getByText(/chưa đọc/)).toHaveCount(0);
  await page.getByRole("button", { name: /Đặt Đà Nẵng làm mặc định/ }).click();
  await expect(page.getByText("Đà Nẵng sẽ là nơi mở mặc định.")).toBeVisible();
  await expect(
    page.getByRole("button", { name: /Đặt Minh An Group làm mặc định/ }),
  ).toBeVisible();
  // The only owner of a workspace cannot leave it.
  await expect(
    page.getByRole("button", { name: "Rời workspace Minh An Group…" }),
  ).toBeDisabled();
  await expect(
    page.getByRole("button", { name: "Rời workspace Đà Nẵng…" }),
  ).toBeDisabled();

  await page.getByRole("button", { name: "Rời workspace Tân Lộc…" }).click();
  const dialog = page.getByRole("alertdialog");
  await expect(dialog).toContainText("Rời Tân Lộc?");
  await dialog.getByRole("button", { name: "Hủy" }).click();
  await expect(page.getByRole("region", { name: /Tân Lộc/ })).toBeVisible();
  await page.getByRole("button", { name: "Rời workspace Tân Lộc…" }).click();
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Rời workspace" })
    .click();
  await expect(page.getByText("Bạn đã rời Tân Lộc.")).toBeVisible();
  await expect(page.getByRole("region", { name: /Tân Lộc/ })).toHaveCount(0);
});

const PNG_BYTES = Buffer.from([
  0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0,
]);

test("Profile photo: a wrong type or a big file is refused with a reason, a photo is sent and can be removed", async ({
  page,
}) => {
  await page.goto(`${account}/profile`);
  await expect(page.getByLabel("Số điện thoại")).toBeVisible();
  const input = page.locator("input[type=file]");

  await input.setInputFiles({
    name: "me.gif",
    mimeType: "image/gif",
    buffer: PNG_BYTES,
  });
  await expect(
    page.getByText(
      "Không dùng được ảnh này. Hãy chọn ảnh PNG, JPEG hoặc WebP.",
    ),
  ).toBeVisible();

  await input.setInputFiles({
    name: "big.png",
    mimeType: "image/png",
    buffer: Buffer.concat([PNG_BYTES, Buffer.alloc(1_600_000)]),
  });
  await expect(page.getByText("Ảnh quá lớn.")).toBeVisible();

  await input.setInputFiles({
    name: "me.png",
    mimeType: "image/png",
    buffer: PNG_BYTES,
  });
  const remove = page.getByRole("button", { name: "Gỡ ảnh đại diện" });
  await expect(remove).toBeVisible();
  await expect(page.getByText("Ảnh quá lớn.")).toHaveCount(0);

  await remove.click();
  await expect(remove).toHaveCount(0);
});
