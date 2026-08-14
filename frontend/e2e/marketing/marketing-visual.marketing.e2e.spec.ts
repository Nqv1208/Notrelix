import { test, expect } from "@playwright/test";

/**
 * Marketing visual regression baselines.
 *
 * Snapshots for the marketing homepage and contact page at desktop,
 * tablet and mobile viewports, in light and dark modes, plus section
 * captures required by the visual redesign QA set (MKT-VIS T24).
 * Baselines are checked in; regenerate intentionally with:
 * npx playwright test --config playwright.marketing.config.ts
 * --update-snapshots
 */

const FULL_PAGE = { fullPage: true } as const;

test.describe("Marketing Visual", () => {
  test("homepage desktop snapshot", async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto("/");
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page).toHaveScreenshot("homepage-desktop.png", FULL_PAGE);
  });

  test("homepage mobile snapshot", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page).toHaveScreenshot("homepage-mobile.png", FULL_PAGE);
  });

  test("homepage tablet desktop snapshot", async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.goto("/");
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page).toHaveScreenshot(
      "homepage-tablet-desktop.png",
      FULL_PAGE,
    );
  });

  test("homepage tablet dark snapshot", async ({ browser }) => {
    const context = await browser.newContext({ colorScheme: "dark" });
    const page = await context.newPage();
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.goto("/");
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page).toHaveScreenshot("homepage-tablet-dark.png", FULL_PAGE);
    await context.close();
  });

  test("contact page desktop snapshot", async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto("/contact");
    await expect(
      page.getByRole("heading", { level: 1, name: /Contact Notrelix/ }),
    ).toBeVisible();
    await expect(page).toHaveScreenshot("contact-desktop.png", FULL_PAGE);
  });

  test("homepage desktop dark snapshot", async ({ browser }) => {
    const context = await browser.newContext({ colorScheme: "dark" });
    const page = await context.newPage();
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto("/");
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page).toHaveScreenshot("homepage-desktop-dark.png", FULL_PAGE);
    await context.close();
  });

  test("homepage mobile dark snapshot", async ({ browser }) => {
    const context = await browser.newContext({ colorScheme: "dark" });
    const page = await context.newPage();
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page).toHaveScreenshot("homepage-mobile-dark.png", FULL_PAGE);
    await context.close();
  });
});

test.describe("Marketing Visual Sections", () => {
  const sections = [
    { id: "hero", name: "hero" },
    { id: "showcase", name: "showcase" },
    { id: "metrics", name: "metrics" },
    { id: "pricing", name: "pricing" },
    { id: "final-cta", name: "final-cta" },
  ] as const;

  for (const section of sections) {
    test(`section ${section.name} desktop snapshot (light)`, async ({
      page,
    }) => {
      await page.setViewportSize({ width: 1440, height: 900 });
      await page.goto("/");
      const locator = page.locator(`#${section.id}`);
      await locator.scrollIntoViewIfNeeded();
      await expect(locator).toHaveScreenshot(
        `section-${section.name}-desktop.png`,
      );
    });

    test(`section ${section.name} desktop snapshot (dark)`, async ({
      browser,
    }) => {
      const context = await browser.newContext({ colorScheme: "dark" });
      const page = await context.newPage();
      await page.setViewportSize({ width: 1440, height: 900 });
      await page.goto("/");
      const locator = page.locator(`#${section.id}`);
      await locator.scrollIntoViewIfNeeded();
      await expect(locator).toHaveScreenshot(
        `section-${section.name}-desktop-dark.png`,
      );
      await context.close();
    });

    test(`section ${section.name} mobile snapshot (light)`, async ({
      page,
    }) => {
      await page.setViewportSize({ width: 390, height: 844 });
      await page.goto("/");
      const locator = page.locator(`#${section.id}`);
      await locator.scrollIntoViewIfNeeded();
      await expect(locator).toHaveScreenshot(
        `section-${section.name}-mobile.png`,
      );
    });

    test(`section ${section.name} mobile snapshot (dark)`, async ({
      browser,
    }) => {
      const context = await browser.newContext({ colorScheme: "dark" });
      const page = await context.newPage();
      await page.setViewportSize({ width: 390, height: 844 });
      await page.goto("/");
      const locator = page.locator(`#${section.id}`);
      await locator.scrollIntoViewIfNeeded();
      await expect(locator).toHaveScreenshot(
        `section-${section.name}-mobile-dark.png`,
      );
      await context.close();
    });
  }

  test("mobile menu open snapshot", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");
    await page.getByRole("button", { name: "Open menu" }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await expect(page.locator("header")).toHaveScreenshot(
      "mobile-menu-open.png",
    );
  });
});

test.describe("Marketing Visual QA", () => {
  test("360px has no horizontal overflow", async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 800 });
    await page.goto("/");
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth > window.innerWidth,
    );
    expect(overflow, "page must not overflow horizontally at 360px").toBe(
      false,
    );
  });

  test("product window is contained on mobile", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");
    const hero = await page.locator("#hero").boundingBox();
    const window = await page.locator("#hero .product-window").boundingBox();
    expect(hero).not.toBeNull();
    expect(window).not.toBeNull();
    expect(window!.x).toBeGreaterThanOrEqual(0);
    expect(window!.x + window!.width).toBeLessThanOrEqual(
      hero!.x + hero!.width + 1,
    );
  });

  test("final CTA buttons are not clipped at 360px", async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 800 });
    await page.goto("/");
    await page.locator("#final-cta").scrollIntoViewIfNeeded();
    const box = await page.locator("#final-cta .v2-cta").first().boundingBox();
    expect(box).not.toBeNull();
    expect(box!.x).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width).toBeLessThanOrEqual(360 + 1);
  });

  test("reduced motion disables CTA animation and reveal", async ({ page }) => {
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.goto("/");
    const primary = page.locator("#hero .v2-cta--primary").first();
    await primary.scrollIntoViewIfNeeded();
    const duration = await primary.evaluate(
      (element) => getComputedStyle(element).animationDuration,
    );
    expect(
      duration,
      "primary CTA must not animate under reduced motion",
    ).toMatch(/^(0\.01ms|1e-05s|0s)$/);
    const revealOpacity = await page
      .locator(".reveal")
      .first()
      .evaluate((element) => getComputedStyle(element).opacity);
    expect(revealOpacity, "reveal content must be visible immediately").toBe(
      "1",
    );
  });
});

test.describe("Marketing Theme Resolution", () => {
  test("saved dark does not flash light on hard refresh", async ({
    browser,
  }) => {
    const context = await browser.newContext({ colorScheme: "light" });
    await context.addInitScript(() => {
      localStorage.setItem("theme", "dark");
    });
    const page = await context.newPage();
    await page.goto("/");
    expect(
      await page.evaluate(() =>
        document.documentElement.classList.contains("dark"),
      ),
    ).toBe(true);
    await context.close();
  });

  test("saved light does not flash dark on hard refresh", async ({
    browser,
  }) => {
    const context = await browser.newContext({ colorScheme: "dark" });
    await context.addInitScript(() => {
      localStorage.setItem("theme", "light");
    });
    const page = await context.newPage();
    await page.goto("/");
    expect(
      await page.evaluate(() =>
        document.documentElement.classList.contains("light"),
      ),
    ).toBe(true);
    await context.close();
  });

  test("no stored theme + OS dark resolves to dark", async ({ browser }) => {
    const context = await browser.newContext({ colorScheme: "dark" });
    const page = await context.newPage();
    await page.goto("/");
    expect(
      await page.evaluate(() =>
        document.documentElement.classList.contains("dark"),
      ),
    ).toBe(true);
    await context.close();
  });

  test("no stored theme + OS light resolves to light", async ({ browser }) => {
    const context = await browser.newContext({ colorScheme: "light" });
    const page = await context.newPage();
    await page.goto("/");
    expect(
      await page.evaluate(() =>
        document.documentElement.classList.contains("light"),
      ),
    ).toBe(true);
    await context.close();
  });
});
