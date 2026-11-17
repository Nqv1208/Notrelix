import { expect, test, type Locator, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * Design fidelity of the API token page at 1440x900, light and dark, every box within 1 px of the
 * Idn-Tokens boards (Idn-Tokens, Idn-Var-Tokens-Issue, -StepUp, -Reveal, -Revoke, -Empty), measured
 * from the boards rendered with `render.py` at the same viewport. Where the page differs on purpose
 * the box is asserted by what the two share:
 *  - no scope column or picker (the vocabulary of UC-IDN-L05 is not decided), so the issue dialog is
 *    shorter and the name column wider; boxes inside it are asserted from the dialog's top;
 *  - one field for the code or password where the board draws six boxes, so the proof dialog's body is
 *    shorter; the head, the sentence and the footer are asserted.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const TOKENS = "/workspaces/mock-workspace-primary/account/tokens";
const EMPTY = "/workspaces/mock-workspace-secondary/account/tokens";
const THEME_KEY = "notrelix-theme-mode";

test.use({ viewport: { width: 1440, height: 900 } });

test.beforeEach(async ({ page, context }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await context.grantPermissions(["clipboard-read", "clipboard-write"]);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

interface Box {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function open(page: Page, path: string, dark: boolean) {
  await page.addInitScript(
    ({ key, mode }) => {
      try {
        window.localStorage.setItem(key, mode);
      } catch {
        // Private mode: defaults apply.
      }
    },
    { key: THEME_KEY, mode: dark ? "dark" : "light" },
  );
  await page.goto(path);
  await expect(
    page.getByRole("heading", { level: 2, name: "Token API" }),
  ).toBeVisible();
  await page.waitForTimeout(500);
}

async function box(locator: Locator): Promise<Box> {
  const found = await locator.first().boundingBox();
  expect(found, "the box is on the page").not.toBeNull();
  return found as Box;
}

async function expectBox(locator: Locator, expected: Partial<Box>) {
  const actual = await box(locator);
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want !== undefined)
      expect(
        Math.abs(actual[key] - want),
        `${key} ${actual[key]} vs ${want}`,
      ).toBeLessThanOrEqual(1);
  }
}

/** Boxes of a dialog relative to its own top-left corner, so a different height does not shift them. */
async function expectInside(
  page: Page,
  dialog: Locator,
  parts: Record<string, Partial<Box>>,
) {
  const outer = await box(dialog);
  for (const [selector, want] of Object.entries(parts)) {
    const actual = await box(dialog.locator(selector));
    const relative = {
      x: actual.x - outer.x,
      y: actual.y - outer.y,
      width: actual.width,
      height: actual.height,
    };
    for (const key of ["x", "y", "width", "height"] as const) {
      const value = want[key];
      if (value !== undefined)
        expect(
          Math.abs(relative[key] - value),
          `${selector} ${key} ${relative[key]} vs ${value}`,
        ).toBeLessThanOrEqual(1);
    }
  }
  void page;
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test.describe(mode, () => {
    test("the list is the Idn-Tokens board", async ({ page }) => {
      await open(page, TOKENS, dark);
      await expectBox(page.locator("[data-f=ph]"), {
        x: 496,
        y: 102.63,
        width: 916,
        height: 48.59,
      });
      await expectBox(page.locator("[data-f=ph] button"), {
        x: 1298.66,
        y: 102.63,
        width: 113.34,
        height: 32,
      });
      await expectBox(page.locator("[data-f=tokens] > [data-f=card]"), {
        x: 496,
        y: 167.22,
        width: 916,
        height: 218,
      });
      await expectBox(page.locator("[data-f=th]"), {
        x: 497,
        y: 168.22,
        width: 914,
        height: 36,
      });
      await expectBox(page.locator("[data-f=tr]").nth(0), {
        x: 497,
        y: 204.22,
        width: 914,
        height: 60,
      });
      await expectBox(page.locator("[data-f=tr]").nth(1), {
        y: 264.22,
        height: 60,
      });
      await expectBox(page.locator("[data-f=tr] [data-f=ico]").first(), {
        x: 513,
        y: 216.22,
        width: 36,
        height: 36,
      });
      await expectBox(page.locator("[data-f=tr] button").first(), {
        x: 1328.3,
        y: 220.22,
        width: 66.7,
        height: 28,
      });
      await expectBox(page.locator("[data-f=ban]"), {
        x: 496,
        y: 401.22,
        width: 916,
        height: 65,
      });
    });

    test("the empty list is the Idn-Var-Tokens-Empty board", async ({
      page,
    }) => {
      await open(page, EMPTY, dark);
      await expectBox(page.locator("[data-f=tokens] > [data-f=card]"), {
        x: 496,
        y: 167.22,
        width: 916,
        height: 250.03,
      });
      await expectBox(page.locator("[data-f=card] [data-f=ico]"), {
        x: 928,
        y: 204.22,
        width: 52,
        height: 52,
      });
      await expectBox(page.locator("[data-f=card] p"), {
        x: 760.66,
        y: 298,
        width: 386.67,
        height: 40.28,
      });
      await expectBox(page.locator("[data-f=card] button"), {
        x: 869.39,
        y: 348.25,
        width: 169.22,
        height: 32,
      });
    });

    test("the issue dialog is the Idn-Var-Tokens-Issue board, less the scope picker", async ({
      page,
    }) => {
      await open(page, TOKENS, dark);
      await page
        .getByRole("button", { name: "Tạo token", exact: true })
        .click();
      const dialog = page.getByRole("dialog", { name: "Tạo token API" });
      await page.waitForTimeout(500);
      await expectBox(dialog, { x: 460, width: 520 });
      await expectInside(page, dialog, {
        "[data-f=dh]": { x: 0, y: 0, width: 520, height: 58 },
        "[data-f=dh] [data-f=ico]": { x: 20, y: 18, width: 36, height: 36 },
        "[data-f=dh] h2": { x: 68, y: 22.96, height: 26.09 },
        "input[type=text]": { x: 20, y: 96.13, width: 480, height: 36 },
        "[data-f=df]": { x: 0, width: 520, height: 65 },
        "[data-f=df] button:first-child": { x: 20, width: 55.36, height: 36 },
      });
    });

    test("the proof dialog is the Idn-Var-Tokens-StepUp board, with one field for the answer", async ({
      page,
    }) => {
      await open(page, TOKENS, dark);
      await page
        .getByRole("button", { name: "Tạo token", exact: true })
        .click();
      await page.getByLabel("Tên token").fill("Báo cáo tồn kho");
      await page.getByRole("button", { name: "Tạo token" }).last().click();
      const dialog = page.getByRole("dialog", {
        name: "Xác nhận lại danh tính",
      });
      await page.waitForTimeout(500);
      await expectBox(dialog, { x: 500, width: 440 });
      await expectInside(page, dialog, {
        "[data-f=dh]": { x: 0, y: 0, width: 440, height: 58 },
        "[data-f=dh] h2": { x: 68, y: 22.95, width: 200.45, height: 26.09 },
        "p:first-of-type": { x: 0, y: 58, width: 440, height: 41.84 },
        "[data-f=df] button:first-child": { x: 20, width: 55.36, height: 36 },
      });
    });

    test("the one-time dialog is the Idn-Var-Tokens-Reveal board", async ({
      page,
    }) => {
      await open(page, TOKENS, dark);
      await page
        .getByRole("button", { name: "Tạo token", exact: true })
        .click();
      await page.getByLabel("Tên token").fill("Báo cáo tồn kho");
      await page.getByRole("button", { name: "Tạo token" }).last().click();
      await page
        .getByLabel("Mã 6 số từ ứng dụng xác thực")
        .fill(MOCK_AUTH.otpCode);
      await page.getByRole("button", { name: "Xác nhận" }).click();
      const dialog = page.getByRole("dialog", {
        name: "Sao chép token của bạn",
      });
      await page.waitForTimeout(500);
      await expectBox(dialog, {
        x: 460,
        y: 280.88,
        width: 520,
        height: 338.25,
      });
      await expectInside(page, dialog, {
        "[data-f=dh] h2": { x: 68, width: 217.59, height: 26.09 },
        "[data-f=db] [data-f=ban]": { x: 20, y: 72, width: 480, height: 84.5 },
        "[data-f=tok-rev]": { x: 20, y: 170.5, width: 480, height: 54 },
        "[data-f=df]": { x: 0, y: 273.25, width: 520, height: 65 },
        "[data-f=df] button": { x: 327.3, width: 172.7, height: 36 },
      });
    });

    test("the revoke dialog is the Idn-Var-Tokens-Revoke board", async ({
      page,
    }) => {
      await open(page, TOKENS, dark);
      await page
        .getByRole("button", { name: "Thu hồi CI triển khai ERP" })
        .click();
      const dialog = page.getByRole("alertdialog", {
        name: "Thu hồi token này?",
      });
      await page.waitForTimeout(500);
      await expectBox(dialog, {
        x: 500,
        y: 304.69,
        width: 440,
        height: 290.61,
      });
      await expectInside(page, dialog, {
        "[data-f=dh] h2": { x: 68, width: 170.05, height: 26.09 },
        "[data-f=df]": { x: 0, y: 225.61, width: 440, height: 65 },
        "[data-f=df] button:first-child": { x: 20, width: 92.84, height: 36 },
        "[data-f=df] button:last-child": { x: 300, width: 120, height: 36 },
      });
    });
  });
}
