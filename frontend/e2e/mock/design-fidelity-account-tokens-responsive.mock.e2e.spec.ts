import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Design fidelity of the API token list at 1024x768 (Idn-Tokens-T) and 390x844 (Idn-Tokens-M), light
 * and dark, within 1 px of the boards rendered with `render.py` at the same viewport (the boards'
 * layout does not change with the mode). The page draws no scope column (UC-IDN-L05 is undecided).
 *
 * Only what the token page owns is asserted. Measured and left as the account pages draw them (the
 * shared account shell and PageHead, not this page):
 *  - 1024: the shell keeps the account menu beside the content (content at x 488, 508 wide) where the
 *    board folds it away (x 210, 792 wide), so the card is asserted by height and its gaps, not by
 *    position; the intro and the notice wrap to a second line in the narrower column;
 *  - 390: the shell stacks the menu above the content (content starts at y 307), the "Tạo token"
 *    button sits at the right (113 x 44) where the board stacks it full width (358 x 32), and touch
 *    targets are 44 px where the board draws 28 and 32, so rows are 115 px, not 97.7.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const TOKENS = "/workspaces/mock-workspace-primary/account/tokens";
const THEME_KEY = "notrelix-theme-mode";

interface Box {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
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
  await page.goto(TOKENS);
  await expect(
    page.getByRole("heading", { level: 2, name: "Token API" }),
  ).toBeVisible();
  await page.waitForTimeout(500);
}

async function expectBox(locator: Locator, expected: Partial<Box>) {
  const actual = (await locator.first().boundingBox()) as Box;
  expect(actual, "the box is on the page").not.toBeNull();
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want !== undefined)
      expect(
        Math.abs(actual[key] - want),
        `${key} ${actual[key]} vs ${want}`,
      ).toBeLessThanOrEqual(1);
  }
}

/** What the page owns at 1024: sizes, the 16 px gaps between its blocks, the icon and button boxes. */
async function expectTablet(page: Page) {
  const ph = (await page.locator("[data-f=ph]").boundingBox()) as Box;
  const card = (await page
    .locator("[data-f=tokens] > [data-f=card]")
    .boundingBox()) as Box;
  const notice = (await page.locator("[data-f=ban]").boundingBox()) as Box;
  const row = (await page.locator("[data-f=tr]").first().boundingBox()) as Box;
  expect(Math.abs(card.y - (ph.y + ph.height) - 16)).toBeLessThanOrEqual(1);
  expect(Math.abs(notice.y - (card.y + card.height) - 16)).toBeLessThanOrEqual(
    1,
  );
  expect(Math.abs(notice.x - card.x)).toBeLessThanOrEqual(1);
  expect(Math.abs(notice.width - card.width)).toBeLessThanOrEqual(1);
  expect(Math.abs(card.height - 218)).toBeLessThanOrEqual(1);
  await expectBox(page.locator("[data-f=ph] button"), {
    width: 113.34,
    height: 32,
  });
  await expectBox(page.locator("[data-f=th]"), { height: 36 });
  await expectBox(page.locator("[data-f=tr]").first(), { height: 60 });
  await expectBox(page.locator("[data-f=tr] [data-f=ico]"), {
    width: 36,
    height: 36,
  });
  const icon = (await page
    .locator("[data-f=tr] [data-f=ico]")
    .first()
    .boundingBox()) as Box;
  expect(Math.abs(icon.x - row.x - 16)).toBeLessThanOrEqual(1);
  expect(Math.abs(icon.y - row.y - 12)).toBeLessThanOrEqual(1);
  await expectBox(page.locator("[data-f=tr] button"), {
    width: 66.7,
    height: 28,
  });
}

/** At 390 the column edges and the key icon: 16 px gutters, a 358 px card, the 36 px icon. */
async function expectPhone(page: Page) {
  await expectBox(page.locator("[data-f=ph]"), { x: 16, width: 358 });
  await expectBox(page.locator("[data-f=tokens] > [data-f=card]"), {
    x: 16,
    width: 358,
  });
  await expectBox(page.locator("[data-f=tr]").first(), { x: 17, width: 356 });
  await expectBox(page.locator("[data-f=tr] [data-f=ico]"), {
    width: 36,
    height: 36,
  });
  await expectBox(page.locator("[data-f=ban]"), { x: 16, width: 358 });
  await expectBox(page.locator("[data-f=tr] button"), { width: 66.7 });
}

for (const size of [1024, 390] as const)
  for (const dark of [false, true]) {
    test.describe(`${size} ${dark ? "dark" : "light"}`, () => {
      test.use({
        viewport: { width: size, height: size === 1024 ? 768 : 844 },
      });

      test("the list is the Idn-Tokens board", async ({ page }) => {
        test.skip(scenario !== "default", "Assertions need the default world");
        await open(page, dark);
        if (size === 1024) await expectTablet(page);
        else await expectPhone(page);
      });
    });
  }
