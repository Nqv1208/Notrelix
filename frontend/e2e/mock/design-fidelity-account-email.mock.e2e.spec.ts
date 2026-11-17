import { expect, test, type Locator, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * Design fidelity of the sign-in email page at 1440x900, 1024x768 and 390x844, light and dark, within
 * 1 px of Idn-Email (1440), Idn-Email-M (390), Idn-Var-Email-Invalid and Idn-Var-Email-Sent, rendered
 * with `render.py` at the same viewport (the boards' layout does not change with the mode). No board
 * is drawn at 1024: the card is asserted from its own top, as at the other widths.
 * Where the page differs on purpose:
 *  - the board draws the password for the proof as a third field; the page buys the proof (code,
 *    password or provider) after the address, so the card has two fields and the button sits where the
 *    third field is; boxes below that point are asserted from the card's top;
 *  - the sent notice says the same words for any address (no "resend" and "cancel" buttons), so only
 *    its position and width are asserted;
 *  - at 1024 and 390 the shell sets the content column (menu beside it at 1024, stacked above at 390);
 *  - at 390 every input is a 44 px touch target (the account pages' rule, which the mock e2e of this
 *    page keeps) where the board draws 36, so each input adds 8 px to what lies below it, and the button is 44 px tall.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const EMAIL = "/workspaces/mock-workspace-primary/account/email";
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

/** A box relative to the card's top-left corner, so the shell around the card does not move it. */
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
    if (value !== undefined)
      expect(
        Math.abs(relative[key] - value),
        `${label} ${key} ${relative[key]} vs ${value}`,
      ).toBeLessThanOrEqual(1);
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
  await page.goto(EMAIL);
  await expect(
    page.getByRole("heading", { level: 2, name: "Email đăng nhập" }),
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

      test("the form is the Idn-Email board", async ({ page }) => {
        await open(page, dark);
        const wide = size.width >= 1440;
        const extra = size.width <= 390 ? 8 : 0;
        if (wide) {
          const ph = await box(page.locator("[data-f=ph]"));
          expect(Math.abs(ph.x - 496)).toBeLessThanOrEqual(1);
          expect(Math.abs(ph.y - 102.63)).toBeLessThanOrEqual(1);
          expect(Math.abs(ph.width - 916)).toBeLessThanOrEqual(1);
          expect(Math.abs(ph.height - 48.59)).toBeLessThanOrEqual(1);
          const card = await box(page.locator("[data-f=card]"));
          expect(Math.abs(card.x - 496)).toBeLessThanOrEqual(1);
          expect(Math.abs(card.y - 167.22)).toBeLessThanOrEqual(1);
          expect(Math.abs(card.width - 560)).toBeLessThanOrEqual(1);
        }
        const card = await box(page.locator("[data-f=card]"));
        // The card is 560 wide, or the column when that is narrower: 1 px border, 16 px padding.
        const inner = card.width - 34;
        const labels = page.locator("[data-f=card] label");
        const inputs = page.locator("[data-f=card] input");
        await expectInCard(
          page,
          labels.nth(0),
          { x: 17, y: 17, width: inner, height: 18.13 },
          "current label",
        );
        await expectInCard(
          page,
          inputs.nth(0),
          { x: 17, y: 41.12, width: inner, height: 36 + extra },
          "current input",
        );
        await expectInCard(
          page,
          labels.nth(1),
          { x: 17, y: 91.12 + extra, width: inner, height: 18.13 },
          "new label",
        );
        await expectInCard(
          page,
          inputs.nth(1),
          { x: 17, y: 115.25 + extra, width: inner, height: 36 + extra },
          "new input",
        );
        await expectInCard(
          page,
          page.getByRole("button", { name: "Gửi liên kết xác nhận" }),
          {
            x: 17,
            y: 165.25 + 2 * extra,
            width: 159.55,
            height: size.width <= 390 ? 44 : 32,
          },
          "button",
        );
        expect(
          Math.abs(card.height - (197.25 + 17 + 2 * extra + (extra ? 12 : 0))),
          `card height ${card.height}`,
        ).toBeLessThanOrEqual(1);
      });

      test("a malformed address is the Idn-Var-Email-Invalid board", async ({
        page,
      }) => {
        await open(page, dark);
        await page.getByLabel("Email mới").fill("hung.trinh@minhan");
        await page
          .getByRole("button", { name: "Gửi liên kết xác nhận" })
          .click();
        const error = page.getByRole("alert");
        await expect(error).toBeVisible();
        const extra = size.width <= 390 ? 8 : 0;
        const inner = (await box(page.locator("[data-f=card]"))).width - 34;
        // 6 px under the input (115.25 + 36 + 6), 13 px at 1.45.
        await expectInCard(
          page,
          error,
          { x: 17, y: 157.25 + 2 * extra, width: inner, height: 18.84 },
          "error",
        );
        await expectInCard(
          page,
          page.getByRole("button", { name: "Gửi liên kết xác nhận" }),
          { y: 190.1 + 2 * extra, height: size.width <= 390 ? 44 : 32 },
          "button below the error",
        );
      });

      test("the sent notice sits above the card as in Idn-Var-Email-Sent", async ({
        page,
      }) => {
        await open(page, dark);
        const cardBefore = await box(page.locator("[data-f=card]"));
        const headBefore = await box(page.locator("[data-f=ph]"));
        await page.getByLabel("Email mới").fill("moi@example.com");
        await page
          .getByRole("button", { name: "Gửi liên kết xác nhận" })
          .click();
        await page.getByLabel("Mã", { exact: true }).fill(MOCK_AUTH.otpCode);
        await page.keyboard.press("Enter");
        const notice = page.locator("[data-f=ban]");
        await expect(notice).toBeVisible();
        await page.waitForTimeout(300);
        // The step-up field took focus and may have scrolled the page: measure from the top again.
        await page.evaluate(() => window.scrollTo(0, 0));
        const ban = await box(notice);
        const card = await box(page.locator("[data-f=card]"));
        // The notice takes the card's old place, the card follows 16 px under it.
        const headAfter = await box(page.locator("[data-f=ph]"));
        expect(
          Math.abs(ban.y - headAfter.y - (cardBefore.y - headBefore.y)),
        ).toBeLessThanOrEqual(1);
        expect(
          Math.abs(card.y - (ban.y + ban.height) - 16),
        ).toBeLessThanOrEqual(1);
        if (size.width >= 1440) {
          expect(Math.abs(ban.x - 496)).toBeLessThanOrEqual(1);
          expect(Math.abs(ban.width - 916)).toBeLessThanOrEqual(1);
        }
      });
    });
  }
