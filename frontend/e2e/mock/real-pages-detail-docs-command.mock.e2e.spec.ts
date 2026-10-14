import { expect, test, type Page } from "@playwright/test";

/**
 * What a person sees on the real item page, side sheet, Docs and command
 * palette of the mock world: the item's own assignee, priority, description
 * and comments (never a made-up default), the Docs entry route, no English
 * fallback on a missing page, the mockup's own search finding documents, and
 * the palette placeholder shown whole beside the workspace chip.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const WORKSPACE = "/workspaces/mock-workspace-primary";
const BOARD = `${WORKSPACE}/boards/mock-board-erp`;
const NOW = new Date("2026-10-12T10:40:00+07:00");
const ERP_142 = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

async function open(page: Page, path: string) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(path);
}

/** Opens the palette from Home once the page has settled. */
async function openPalette(page: Page) {
  await expect(
    page.getByRole("heading", { level: 1, name: /^Chào buổi sáng, / }),
  ).toBeVisible();
  await page.keyboard.press("ControlOrMeta+k");
  const dialog = page.getByRole("dialog", { name: "Tìm kiếm và lệnh" });
  await expect(dialog).toBeVisible();
  return dialog;
}

test.describe("Real item page, Docs and command palette", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the item page shows the item's own assignee, priority, description and comments", async ({
    page,
  }) => {
    await open(page, `${BOARD}/items/mock-erp-item-142`);
    await expect(page.getByRole("heading", { level: 1 })).toHaveText(ERP_142);
    await expect(
      page.getByText("Chuyển 48.200 hồ sơ khách hàng"),
    ).toBeVisible();
    await expect(
      page.getByText("Số khách hàng trên ERP khớp danh sách đã gộp"),
    ).toBeVisible();
    await expect(page.getByText("Đỗ Khánh Linh").first()).toBeVisible();
    await expect(page.getByText("Trần Minh Quân").first()).toBeVisible();
    const text = await page.locator("body").innerText();
    expect(text).toContain("Khẩn");
    expect(text).not.toMatch(/Chưa giao|Trung bình|Chưa có ưu tiên/);
    expect(text).not.toContain("Công việc này chưa có mô tả");
  });

  test("the side sheet shows the same assignee and priority as the board", async ({
    page,
  }) => {
    await open(page, `${BOARD}?view=kanban&item=mock-erp-item-142`);
    const sheet = page.getByRole("dialog", { name: ERP_142 });
    await expect(sheet).toBeVisible();
    await expect(sheet.getByText("Trần Minh Quân").first()).toBeVisible();
    await expect(sheet.getByText("Khẩn").first()).toBeVisible();
    await expect(sheet.getByText("Chuyển 48.200 hồ sơ")).toBeVisible();
    const text = await sheet.innerText();
    expect(text).not.toMatch(/Chưa giao|Trung bình|Chưa có ưu tiên/);
  });

  test("an item with no assignee says so, and shows its own priority", async ({
    page,
  }) => {
    await open(page, `${BOARD}/items/mock-erp-item-153`);
    await expect(page.getByRole("heading", { level: 1 })).toContainText(
      "Rà soát hợp đồng bản quyền",
    );
    const text = await page.locator("body").innerText();
    expect(text).toContain("Chưa giao");
    expect(text).toContain("Thấp");
  });

  test("/docs opens the Boards and Docs directory", async ({ page }) => {
    await open(page, `${WORKSPACE}/docs`);
    await expect(page).toHaveURL(
      /\/workspaces\/mock-workspace-primary\/library/,
    );
    await expect(
      page.getByRole("heading", { level: 1, name: "Bảng & Tài liệu" }),
    ).toBeVisible();
    await expect(page.getByText("Hướng dẫn đối chiếu ngân hàng")).toBeVisible();
    await expect(page.getByText("Đặc tả triển khai ERP")).toBeVisible();
    const text = await page.locator("body").innerText();
    expect(text).not.toMatch(/Product specification|Page \d|Board \d/);
  });

  test("a missing page speaks Vietnamese", async ({ page }) => {
    await open(page, `${WORKSPACE}/khong-co-trang-nay`);
    await expect(page.getByText("Không tìm thấy trang")).toBeVisible();
    const text = await page.locator("body").innerText();
    expect(text).not.toMatch(/Page not found|requested route|Route error/);
  });

  test("the palette finds the documents for the mockup's own query", async ({
    page,
  }) => {
    await open(page, WORKSPACE);
    const dialog = await openPalette(page);
    await page.keyboard.type("đối chiếu", { delay: 15 });
    await expect(dialog.getByText("Không tìm thấy")).toHaveCount(0);
    await expect(
      dialog.getByRole("option", { name: /Hướng dẫn đối chiếu ngân hàng/ }),
    ).toBeVisible();
    await expect(dialog).toContainText("Accounting / Quy trình");
  });

  for (const width of [1440, 1280]) {
    test(`the palette placeholder is shown whole beside the workspace chip at ${width}px`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height: 800 });
      await open(page, WORKSPACE);
      const dialog = await openPalette(page);
      const placeholder = dialog.locator('[data-f="ph"]');
      await expect(placeholder).toHaveText(
        "Tìm việc, board, tài liệu, người… hoặc gõ > để chạy lệnh",
      );
      const box = await placeholder.boundingBox();
      const chip = await dialog.locator('[data-f="scope"]').boundingBox();
      const row = await dialog.locator('[data-f="qrow"]').boundingBox();
      expect(box && chip && row).toBeTruthy();
      // Wrapped onto lines inside the row, and clear of the workspace chip.
      expect(box!.x + box!.width).toBeLessThanOrEqual(chip!.x);
      expect(box!.y).toBeGreaterThanOrEqual(row!.y);
      expect(box!.y + box!.height).toBeLessThanOrEqual(row!.y + row!.height);
      // Nothing is cut off: the text needs no more width than it has.
      const clipped = await placeholder.evaluate(
        (node) => node.scrollWidth > node.clientWidth,
      );
      expect(clipped).toBe(false);
    });
  }
});
