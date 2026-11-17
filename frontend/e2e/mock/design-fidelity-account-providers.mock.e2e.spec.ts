import { expect, test, type Locator, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * Design fidelity of the connected accounts page at 1440x900, 1024x768 and 390x844, light and dark,
 * within 1 px of Idn-Providers (1440) and Idn-Providers-M (390), rendered with `render.py` at the
 * same viewport (the boards' layout does not change with the mode); Idn-Var-Providers-LastMethod
 * is the last-method state. No board is drawn at 1024: the cards are asserted from the first
 * card's own top, as at the other widths.
 * Where the page differs on purpose:
 *  - the last-method board draws only the Google row; the page keeps the Microsoft row under it;
 *  - the board gives the linked row a line "address · linked on date" the account summary does not
 *    carry, so the row says what the link allows in one line of the same size;
 *  - at 390 every button is a 44 px touch target (the account pages' rule) where the board draws 28,
 *    so each provider row is 16 px taller than drawn;
 *  - at 1024 and 390 the shell sets the content column.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const PROVIDERS = "/workspaces/mock-workspace-primary/account/providers";
const THEME_KEY = "notrelix-theme-mode";

interface Box {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function box(locator: Locator): Promise<Box> {
  const found = (await locator.first().boundingBox()) as Box | null;
  expect(found, "the box is on the page").not.toBeNull();
  return found as Box;
}

function near(actual: number, want: number, label: string) {
  expect(
    Math.abs(actual - want),
    `${label} ${actual} vs ${want}`,
  ).toBeLessThanOrEqual(1);
}

/** A box relative to the first card's top-left corner, so the shell around it does not move it. */
async function expectInCard(
  page: Page,
  locator: Locator,
  want: Partial<Box>,
  label: string,
) {
  const card = await box(page.locator("[data-f=card]"));
  const actual = await box(locator);
  const relative = {
    x: actual.x - card.x,
    y: actual.y - card.y,
    width: actual.width,
    height: actual.height,
  };
  for (const key of ["x", "y", "width", "height"] as const) {
    const value = want[key];
    if (value !== undefined) near(relative[key], value, `${label} ${key}`);
  }
}

async function open(page: Page, dark: boolean) {
  await page.addInitScript(
    ({ key, mode }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem(key, mode);
      } catch {
        // Private mode: defaults apply.
      }
    },
    { key: THEME_KEY, mode: dark ? "dark" : "light" },
  );
  await page.goto(PROVIDERS);
  await expect(
    page.getByRole("heading", { level: 2, name: "Tài khoản liên kết" }),
  ).toBeVisible();
  // The board is drawn with Google linked: link it through the page, as a person would.
  await page.getByRole("button", { name: "Liên kết Google" }).click();
  await page.getByLabel("Mã", { exact: true }).fill(MOCK_AUTH.otpCode);
  await page.keyboard.press("Enter");
  await expect(
    page.getByRole("button", { name: "Gỡ liên kết Google" }),
  ).toBeVisible();
  await page.waitForTimeout(500);
}

const SIZES = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

for (const size of SIZES)
  for (const dark of [false, true]) {
    test.describe(`${size.width} ${dark ? "dark" : "light"}`, () => {
      test.use({ viewport: { width: size.width, height: size.height } });
      test.beforeEach(() => {
        test.skip(scenario !== "default", "Assertions need the default world");
      });

      test("the page is the Idn-Providers board", async ({ page }) => {
        await open(page, dark);
        const phone = size.width <= 390;
        // Each provider row's button is 44 px on a phone and 28 on the board, and sits on its own line.
        const extra = phone ? 16 : 0;
        const inner = (await box(page.locator("[data-f=card]"))).width - 2;
        if (size.width >= 1440) {
          const ph = await box(page.locator("[data-f=ph]"));
          near(ph.x, 496, "ph x");
          near(ph.y, 102.63, "ph y");
          near(ph.width, 916, "ph width");
          near(ph.height, 68.09, "ph height");
          const card = await box(page.locator("[data-f=card]"));
          near(card.x, 496, "card x");
          near(card.y, 186.72, "card y");
          near(card.width, 916, "card width");
        }
        const rows = page.locator("[data-f=card] [data-f=row]");
        await expectInCard(
          page,
          page.locator("[data-f=card]"),
          {
            x: 0,
            y: 0,
            width: inner + 2,
            height: (phone ? 220.42 : 144.42) + 2 * extra,
          },
          "card",
        );
        await expectInCard(
          page,
          rows.nth(0),
          { x: 1, y: 1, width: inner, height: phone ? 109.73 + extra : 71.73 },
          "google row",
        );
        await expectInCard(
          page,
          page.getByRole("button", { name: "Gỡ liên kết Google" }),
          phone
            ? { x: 17, width: inner - 32, height: 44 }
            : { y: 22.86, height: 28 },
          "unlink button",
        );
        const card1Right =
          (await box(page.locator("[data-f=card]"))).x +
          (await box(page.locator("[data-f=card]"))).width;
        if (!phone)
          near(
            (
              await box(
                page.getByRole("button", { name: "Gỡ liên kết Google" }),
              )
            ).x +
              (
                await box(
                  page.getByRole("button", { name: "Gỡ liên kết Google" }),
                )
              ).width,
            card1Right - 17,
            "unlink button right edge",
          );
        const second = page.locator("[data-f=card2]");
        const card1 = await box(page.locator("[data-f=card]"));
        const card2 = await box(second);
        near(card2.y - (card1.y + card1.height), phone ? 14 : 16, "card gap");
        near(card2.height, 186.22, "methods card height");
        near(card2.width, card1.width, "methods card width");
        const head = await box(second.locator("h3"));
        near(head.y - card2.y, 1, "methods title y");
        near(head.height, 43.84, "methods title height");
        const methods = second.locator("[data-f=row]");
        near((await box(methods.nth(0))).height, 69.69, "password row");
        near((await box(methods.nth(1))).height, 70.69, "google method row");
        const ico = await box(rows.nth(0).locator("span[aria-hidden]"));
        near(ico.width, 36, "icon width");
        near(ico.height, 36, "icon height");
      });

      test("the only way in is the Idn-Var-Providers-LastMethod board", async ({
        page,
      }) => {
        await page.addInitScript(
          ({ key, mode }) => {
            try {
              window.localStorage.setItem("notrelix-locale", "vi");
              window.localStorage.setItem(key, mode);
            } catch {
              // Private mode: defaults apply.
            }
          },
          { key: THEME_KEY, mode: dark ? "dark" : "light" },
        );
        await page.goto("/sign-in?error=identity.oauth.failed");
        await page.getByRole("button", { name: "Tiếp tục với Google" }).click();
        await expect(page).toHaveURL(
          /\/(home|workspaces\/mock-workspace-primary)/,
        );
        await page.evaluate((target) => {
          history.pushState({}, "", target);
          window.dispatchEvent(new PopStateEvent("popstate"));
        }, PROVIDERS);
        const ban = page.locator("[data-f=ban]");
        await expect(ban).toBeVisible();
        await page.waitForTimeout(500);
        const b = await box(ban);
        const card = await box(page.locator("[data-f=card]"));
        if (size.width >= 1440) {
          near(b.x, 496, "ban x");
          near(b.y, 167.22, "ban y");
          near(b.width, 916, "ban width");
          near(card.y, 279.72, "card y");
        }
        if (size.width >= 1440) near(b.height, 96.5, "ban height");
        near(
          card.y - (b.y + b.height),
          size.width <= 390 ? 14 : 16,
          "gap under the ban",
        );
        // The board draws only the Google row; the page keeps the Microsoft row under it, so the
        // person with one way in can still link a second provider.
        near(
          (await box(page.locator("[data-f=card] [data-f=row]"))).height,
          size.width <= 390 ? 109.73 + 16 : 71.73,
          "google row height",
        );
        expect(
          await page
            .getByRole("button", { name: "Gỡ liên kết Google" })
            .isDisabled(),
        ).toBe(true);
      });
    });
  }
