import { test, expect } from "@playwright/test";

function collectAppErrors(page: import("@playwright/test").Page) {
  const appErrors: string[] = [];
  page.on("pageerror", (error) => {
    appErrors.push(`pageerror: ${error.message}`);
  });
  page.on("console", (msg) => {
    if (msg.type() !== "error") return;
    const text = msg.text();
    if (text.includes("favicon")) return;
    if (text.includes("Failed to load resource")) return;
    appErrors.push(text);
  });
  return appErrors;
}

test.describe("Production Shell Smoke", () => {
  test("index boots the production shell without console errors", async ({
    page,
  }) => {
    const appErrors = collectAppErrors(page);

    await page.goto("/");
    await expect(page).toHaveTitle(/Notrelix/i);
    expect(appErrors).toHaveLength(0);
  });

  test("sign-in page boots without unhandled realtime errors", async ({
    page,
  }) => {
    const appErrors = collectAppErrors(page);

    await page.goto("/sign-in");
    await expect(page).toHaveTitle(/Notrelix|Sign In/i);
    expect(appErrors).toHaveLength(0);
  });

  test("sign-in page does not leak raw backend stack traces", async ({
    page,
  }) => {
    await page.goto("/sign-in");
    const pageText = await page.textContent("body");
    expect(pageText).not.toContain("TypeError: Cannot read property");
    expect(pageText).not.toContain("at Module.evaluate");
    expect(pageText).not.toContain("System.ArgumentException");
  });
});
