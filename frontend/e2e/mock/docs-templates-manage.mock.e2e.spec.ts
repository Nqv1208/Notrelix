import { expect, test, type Page } from "@playwright/test";

/**
 * The page templates dialog (File, Mẫu trang…) in Vietnamese at 1440, 1024 and 390, light and dark. The Doc
 * boards draw no template gallery, so the review is by measurement and by looking: the dialog stays inside
 * the viewport, the page never scrolls sideways, every state is reachable, and its text is legible. With
 * `TEMPLATE_SHOTS` set to a folder, each frame is saved there for the screenshot review.
 *
 * Loading and error need a different mock world: run with `VITE_MOCK_LATENCY=slow` for the loading frame and
 * `VITE_MOCK_FAULTS=/workspaces/mock-workspace-primary/page-templates=500` for the error frame.
 */
const DOC = "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const SHOTS = process.env.TEMPLATE_SHOTS;
const SLOW = process.env.VITE_MOCK_LATENCY === "slow";
const BROKEN = (process.env.VITE_MOCK_FAULTS ?? "").includes("page-templates");
const WIDTHS = [1440, 1024, 390] as const;

async function open(page: Page, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: width < 500 ? 844 : 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ({ dark }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        if (dark) window.localStorage.setItem("notrelix-theme-mode", "dark");
      } catch {
        // Private mode: the defaults apply.
      }
    },
    { dark },
  );
  await page.goto(DOC);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await page.evaluate(() => document.fonts.ready);
}

async function openTemplates(page: Page) {
  await page.getByRole("menuitem", { name: "Tệp", exact: true }).click();
  await page.getByRole("menuitem", { name: /Mẫu trang/ }).click();
  await expect(page.getByRole("dialog", { name: "Mẫu trang" })).toBeVisible();
}

async function saveTemplate(page: Page, name: string, description: string) {
  await page.getByRole("menuitem", { name: "Tệp", exact: true }).click();
  await page.getByRole("menuitem", { name: /Lưu thành mẫu/ }).click();
  const dialog = page.getByRole("dialog", { name: "Lưu thành mẫu" });
  await dialog.getByRole("textbox", { name: "Tên mẫu" }).fill(name);
  await dialog.getByRole("textbox", { name: /Mô tả/ }).fill(description);
  await dialog.getByRole("button", { name: "Lưu mẫu", exact: true }).click();
  await expect(dialog).toHaveCount(0);
}

async function frame(page: Page, tag: string, width: number, dark: boolean) {
  const dialog = page.getByRole("dialog", { name: "Mẫu trang" });
  const box = (await dialog.boundingBox())!;
  expect(
    box.x,
    `${tag}: dialog inside the viewport (left)`,
  ).toBeGreaterThanOrEqual(0);
  expect(
    box.x + box.width,
    `${tag}: dialog inside the viewport (right)`,
  ).toBeLessThanOrEqual(width);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
    `${tag}: no sideways scroll`,
  ).toBe(true);
  if (SHOTS) {
    await page.screenshot({
      path: `${SHOTS}/${tag}-${width}-${dark ? "dark" : "light"}.png`,
    });
  }
}

for (const dark of [false, true]) {
  for (const width of WIDTHS) {
    const label = `${width} ${dark ? "dark" : "light"}`;

    test(`templates dialog: empty, list, rename, confirm delete, confirm archive at ${label}`, async ({
      page,
    }) => {
      test.skip(SLOW || BROKEN, "the default world only");
      await open(page, width, dark);
      await openTemplates(page);
      const dialog = page.getByRole("dialog", { name: "Mẫu trang" });
      await expect(dialog.getByText("Chưa có mẫu nào")).toBeVisible();
      await frame(page, "1-empty", width, dark);
      await dialog
        .getByRole("button", { name: "Đóng hộp thoại mẫu trang" })
        .click();

      await saveTemplate(page, "Biên bản họp tuần", "Mỗi sáng thứ Hai");
      await saveTemplate(page, "Kế hoạch quý", "");
      await openTemplates(page);
      await expect(dialog.getByText("Biên bản họp tuần")).toBeVisible();
      await expect(dialog.getByText("Kế hoạch quý")).toBeVisible();
      await frame(page, "2-list", width, dark);

      await dialog
        .getByRole("button", { name: "Đổi tên mẫu Kế hoạch quý" })
        .click();
      await dialog.getByRole("textbox", { name: "Tên mẫu" }).fill("   ");
      await dialog.getByRole("button", { name: "Lưu", exact: true }).click();
      await expect(dialog.getByRole("alert")).toHaveText(
        "Nhập tên mẫu để lưu.",
      );
      await frame(page, "3-rename-invalid", width, dark);
      await dialog
        .getByRole("textbox", { name: "Tên mẫu" })
        .fill("Kế hoạch năm");
      await dialog.getByRole("button", { name: "Lưu", exact: true }).click();
      await expect(dialog.getByText("Kế hoạch năm")).toBeVisible();

      await dialog
        .getByRole("button", { name: "Xóa mẫu Kế hoạch năm" })
        .click();
      await expect(dialog.getByText(/Không thể hoàn tác/)).toBeVisible();
      await frame(page, "4-confirm-delete", width, dark);
      await dialog.getByRole("button", { name: "Hủy" }).click();
      await dialog
        .getByRole("button", { name: "Lưu trữ mẫu Kế hoạch năm" })
        .click();
      await frame(page, "5-confirm-archive", width, dark);
      await dialog
        .getByRole("button", { name: "Lưu trữ mẫu", exact: true })
        .click();
      await expect(dialog.getByText("Kế hoạch năm")).toHaveCount(0);
      await expect(dialog.getByText("Biên bản họp tuần")).toBeVisible();
      await dialog
        .getByRole("button", { name: "Xóa mẫu Biên bản họp tuần" })
        .click();
      await dialog
        .getByRole("button", { name: "Xóa mẫu", exact: true })
        .click();
      await expect(dialog.getByText("Chưa có mẫu nào")).toBeVisible();
    });

    test(`templates dialog: loading at ${label}`, async ({ page }) => {
      test.skip(!SLOW, "needs VITE_MOCK_LATENCY=slow");
      await open(page, width, dark);
      await page.getByRole("menuitem", { name: "Tệp", exact: true }).click();
      await page.getByRole("menuitem", { name: /Mẫu trang/ }).click();
      const dialog = page.getByRole("dialog", { name: "Mẫu trang" });
      await expect(dialog.getByRole("status")).toHaveText("Đang tải các mẫu…");
      await frame(page, "6-loading", width, dark);
    });

    test(`templates dialog: load error with retry at ${label}`, async ({
      page,
    }) => {
      test.skip(!BROKEN, "needs VITE_MOCK_FAULTS for the template route");
      await open(page, width, dark);
      await openTemplates(page);
      const dialog = page.getByRole("dialog", { name: "Mẫu trang" });
      await expect(dialog.getByRole("alert")).toContainText(
        "Chưa tải được các mẫu.",
      );
      await expect(
        dialog.getByRole("button", { name: "Thử lại" }),
      ).toBeVisible();
      await frame(page, "7-error", width, dark);
    });
  }
}
