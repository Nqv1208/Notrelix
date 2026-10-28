import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";

test("renders a production Documents route through mock transport", async ({
  page,
}) => {
  // The editor chrome under test is English; the mock opens in Vietnamese
  // unless a locale is stored.
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default applies.
    }
  });
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));

  if (scenario !== "default") {
    // Documents page requires default world data (mock-doc-product-spec).
    // For other scenarios, just verify home loads without mock errors.
    await page.goto("/home");
    expect(
      errors.filter((message) =>
        message.includes("MockUnhandledOperationError"),
      ),
    ).toEqual([]);
    return;
  }

  await page.goto(
    "/workspaces/mock-workspace-primary/docs/mock-doc-product-spec",
  );
  await expect(page.getByRole("heading", { level: 1 }).first()).toHaveText(
    "Đặc tả triển khai ERP",
  );
  await expect(
    page.getByRole("textbox", { name: "Type / to insert a block" }).first(),
  ).toContainText(
    "Tài liệu này mô tả phạm vi, các phân hệ và mốc triển khai ERP của Minh An Group trong quý 4/2026.",
  );
  expect(
    errors.filter((message) => message.includes("MockUnhandledOperationError")),
  ).toEqual([]);
});

test("edits and checks off a document of the Kế toán department", async ({
  page,
}) => {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default applies.
    }
  });
  test.skip(scenario !== "default", "Needs the default world data.");
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));

  await page.goto(
    "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process",
  );
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Quy trình nhập hoá đơn đầu vào",
  );

  // The department tree under Docs shows the open page as the current one.
  await expect(
    page
      .getByRole("link", { name: "Nhập hoá đơn đầu vào", exact: true })
      .first(),
  ).toHaveAttribute("aria-current", "page");

  // A heading edit is saved: the producer answers and the page shows its text.
  const heading = page.getByRole("heading", { name: "Phạm vi áp dụng" });
  await heading.click();
  await page.keyboard.press("End");
  await page.keyboard.type(" mới");
  await page.getByRole("heading", { level: 1 }).click();
  await expect(
    page.getByRole("status").filter({ hasText: "Saved" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Phạm vi áp dụng mới" }),
  ).toBeVisible();

  // A checklist item toggles through the producer.
  const item = page.getByRole("checkbox", {
    name: /Đào tạo nhóm Công nợ/,
  });
  await expect(item).toHaveAttribute("aria-checked", "false");
  await item.click();
  await expect(item).toHaveAttribute("aria-checked", "true");

  // The File menu opens and closes on Escape.
  await page.getByRole("menuitem", { name: "File", exact: true }).click();
  await expect(page.getByRole("menu", { name: "File" })).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("menu", { name: "File" })).toHaveCount(0);

  expect(
    errors.filter((message) => message.includes("MockUnhandledOperationError")),
  ).toEqual([]);
});
