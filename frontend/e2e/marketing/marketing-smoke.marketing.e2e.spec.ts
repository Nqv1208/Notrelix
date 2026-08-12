import { test, expect } from "@playwright/test";

/**
 * Marketing site smoke suite.
 *
 * Verifies the production marketing homepage boots cleanly, renders every
 * section, keeps navigation alive, links the web app sign-up funnel, and
 * no longer exposes the legacy /v2 route.
 */

const SECTION_IDS = [
  "hero",
  "features",
  "showcase",
  "use-cases",
  "pricing",
  "testimonials",
  "final-cta",
];

const LEGAL_LINKS = [
  { href: "/contact", heading: /Contact Notrelix/ },
  { href: "/legal/privacy", heading: /Privacy Policy/ },
  { href: "/legal/terms", heading: /Terms of Service/ },
] as const;

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

test.describe("Marketing Smoke", () => {
  test("index boots without console errors", async ({ page }) => {
    const appErrors = collectAppErrors(page);

    await page.goto("/");

    await expect(page).toHaveTitle(/Notrelix/);
    await expect(
      page.getByRole("heading", { level: 1, name: /From idea to/ }),
    ).toBeVisible();
    expect(appErrors).toHaveLength(0);
  });

  test("all homepage sections are rendered", async ({ page }) => {
    await page.goto("/");

    for (const id of SECTION_IDS) {
      await expect(
        page.locator(`#${id}`),
        `section #${id} should be present`,
      ).toBeVisible();
    }

    await expect(page.locator("footer")).toBeVisible();
    await expect(
      page.getByText("All systems operational", { exact: false }),
    ).toBeVisible();
  });

  test("hero CTA links to web app sign-up", async ({ page }) => {
    await page.goto("/");

    const signUpCta = page
      .getByRole("link", { name: /Get started free/ })
      .first();
    await expect(signUpCta).toBeVisible();
    await expect(signUpCta).toHaveAttribute("href", /\/sign-up$/);
  });

  test("header nav anchors navigate to sections", async ({ page }) => {
    await page.goto("/");

    await page
      .getByRole("link", { name: /Solutions/ })
      .first()
      .click();
    await expect(page).toHaveURL(/#use-cases/);
    await expect(page.locator("#use-cases")).toBeVisible();

    await page
      .getByRole("link", { name: /Pricing/ })
      .first()
      .click();
    await expect(page).toHaveURL(/#pricing/);
    await expect(page.locator("#pricing")).toBeVisible();
  });

  test("footer contact and legal pages render", async ({ page }) => {
    await page.goto("/");

    for (const { href, heading } of LEGAL_LINKS) {
      await page.locator(`footer a[href="${href}"]`).first().click();
      await page.waitForLoadState("networkidle");
      await expect(page).toHaveURL(new RegExp(`${href.replace("/", "\\/")}$`));
      await expect(
        page.getByRole("heading", { level: 1, name: heading }).first(),
      ).toBeVisible();
      await page.goto("/");
    }
  });

  test("legacy /v2 route returns 404", async ({ page }) => {
    const response = await page.goto("/v2");
    expect(response?.status()).toBe(404);
  });
});
