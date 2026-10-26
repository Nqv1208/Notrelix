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
      page.getByRole("heading", { level: 1, name: /Work Management Made/ }),
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

  test("hero preview view tabs sit over the screenshot and switch it", async ({
    page,
  }) => {
    await page.goto("/");

    const tablist = page.locator("#hero").getByRole("tablist", {
      name: "Product views",
    });
    await expect(tablist.getByRole("tab")).toHaveText([
      "Table",
      "Kanban",
      "Calendar",
      "Timeline",
    ]);

    const visibleImage = page.locator(
      "#hero .shot-layer.is-active .shot-img--light",
    );
    await expect(visibleImage).toHaveAttribute("alt", /Table view/);
    await expect(visibleImage).toHaveAttribute("src", /table-light/);

    await tablist.getByRole("tab", { name: "Timeline" }).click();
    await expect(visibleImage).toHaveAttribute("alt", /Timeline view/);
    await expect(visibleImage).toHaveAttribute("src", /timeline-light/);

    await tablist.getByRole("tab", { name: "Timeline" }).focus();
    await page.keyboard.press("ArrowLeft");
    await expect(
      tablist.getByRole("tab", { name: "Calendar" }),
    ).toHaveAttribute("aria-selected", "true");
    await expect(visibleImage).toHaveAttribute("alt", /Calendar view/);

    await page.keyboard.press("ArrowRight");
    await expect(
      tablist.getByRole("tab", { name: "Timeline" }),
    ).toHaveAttribute("aria-selected", "true");
    await expect(visibleImage).toHaveAttribute("alt", /Timeline view/);
  });

  test("showcase tabs switch the screen and pin notes to it", async ({
    page,
  }) => {
    await page.goto("/");

    const showcase = page.locator("#showcase");
    const tablist = showcase.getByRole("tablist", {
      name: "How work gets done",
    });
    await expect(tablist.getByRole("tab")).toHaveText([
      "Item",
      "Docs",
      "Automation",
      "Reports",
    ]);
    // no browser chrome and no "live preview" claim around the screenshots
    await expect(showcase.locator(".shot-chrome")).toHaveCount(0);
    await expect(showcase).not.toContainText(/live preview/i);

    const visibleImage = showcase.locator(
      ".shot-layer.is-active .shot-img--light",
    );
    await expect(visibleImage).toHaveAttribute("src", /showcase-item-light/);
    await expect(visibleImage).toHaveAttribute("alt", /item page/i);
    await expect(showcase.locator(".shot-note")).toHaveCount(3);

    await tablist.getByRole("tab", { name: "Automation" }).click();
    await expect(visibleImage).toHaveAttribute(
      "src",
      /showcase-automation-light/,
    );
    await expect(showcase.locator(".shot-note")).toHaveCount(3);
    await expect(
      showcase.getByRole("list", { name: "Automation" }).getByRole("listitem"),
    ).toHaveText([
      /Start from something that happens on the Board/,
      /Narrow it with conditions/,
      /Up to five actions/,
    ]);

    await tablist.getByRole("tab", { name: "Automation" }).focus();
    await page.keyboard.press("ArrowRight");
    await expect(tablist.getByRole("tab", { name: "Reports" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(visibleImage).toHaveAttribute("src", /showcase-reports-light/);

    await page.keyboard.press("ArrowRight");
    await expect(tablist.getByRole("tab", { name: "Item" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
  });

  test("story rows show cropped product screens and link to real places", async ({
    page,
  }) => {
    await page.goto("/");

    const rows = page.locator("#features .story-row");
    await expect(rows).toHaveCount(3);
    await expect(page.locator("#features .eyebrow")).toHaveCount(0);
    for (const id of ["inbox", "sharing", "departments"]) {
      await expect(
        page.locator(`#features img[src*="story-${id}-light"]`),
      ).toHaveCount(1);
    }
    const hrefs = await rows
      .locator(".story-copy a")
      .evaluateAll((links) => links.map((a) => a.getAttribute("href")));
    expect(hrefs).toEqual([
      expect.stringMatching(/\/sign-up$/),
      "/legal/privacy",
      "#pricing",
    ]);
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
