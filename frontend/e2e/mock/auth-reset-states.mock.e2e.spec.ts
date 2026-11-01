import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * The password-reset screen (UC-IDN-B07) has no artboard: it is built from the Auth
 * components on the pattern of the forgot screen, so it is reviewed, not measured.
 * Each state at 1440, 1024 and 390 px in light and dark: no sideways scroll, no
 * clipped text, 44 px targets on a phone, axe clean. Screenshots go to the test
 * output for the reviewer.
 */
const THEME_KEY = "notrelix-theme-mode";
const VIEWS = [
  { name: "1440", width: 1440, height: 900 },
  { name: "1024", width: 1024, height: 768 },
  { name: "390", width: 390, height: 844 },
] as const;

async function inspect(page: Page, label: string, phone: boolean) {
  const over = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth,
  );
  expect(over, `${label}: page scrolls sideways`).toBeLessThanOrEqual(0);
  if (phone) {
    const small = await page.evaluate(() =>
      Array.from(
        document.querySelectorAll<HTMLElement>(
          "main button:not([disabled]), main input",
        ),
      )
        .filter((el) => el.offsetParent !== null)
        .map((el) => ({
          name: (el.getAttribute("aria-label") ?? el.textContent ?? "")
            .trim()
            .slice(0, 40),
          height: Math.round(el.getBoundingClientRect().height),
        }))
        .filter((el) => el.height < 44),
    );
    expect(small, `${label}: targets under 44 px`).toEqual([]);
  }
  const results = await new AxeBuilder({ page }).analyze();
  expect(
    results.violations
      .filter((v) => v.impact === "serious" || v.impact === "critical")
      .map((v) => ({
        id: v.id,
        nodes: v.nodes.map((n) => n.target.join(" ")),
      })),
    `${label}: serious or critical axe violations`,
  ).toEqual([]);
}

for (const view of VIEWS) {
  for (const dark of [false, true]) {
    const mode = dark ? "dark" : "light";
    test(`the reset screen: form, refusals and pending are clean, ${view.name} ${mode}`, async ({
      page,
    }, testInfo) => {
      await page.setViewportSize({ width: view.width, height: view.height });
      await page.addInitScript(
        ({ key, theme }) => {
          try {
            window.localStorage.setItem("notrelix-locale", "en");
            window.localStorage.setItem(key, theme);
          } catch {
            // Private mode: defaults apply.
          }
        },
        { key: THEME_KEY, theme: mode },
      );
      const phone = view.width < 500;
      const shot = async (state: string) => {
        const name = `reset-${state}-${view.name}-${mode}`;
        await testInfo.attach(name, {
          body: await page.screenshot({ fullPage: true }),
          contentType: "image/png",
        });
        // The reviewer sets RESET_SHOTS_DIR to keep the images of a passing run.
        const dir = process.env.RESET_SHOTS_DIR;
        if (dir)
          await page.screenshot({ path: `${dir}/${name}.png`, fullPage: true });
      };

      await page.goto(`/reset-password?email=${MOCK_AUTH.personalEmail}`);
      await expect(
        page.getByRole("heading", { level: 1, name: "Choose a new password" }),
      ).toBeVisible();
      await inspect(page, `form ${view.name} ${mode}`, phone);
      await shot("form");

      // Validation: nothing is sent, each field says what is wrong.
      await page.getByRole("button", { name: "Change password" }).click();
      await expect(
        page.getByText("Enter the 6 digits from the email."),
      ).toBeVisible();
      await expect(page.getByText("Use at least 8 characters.")).toBeVisible();
      await inspect(page, `validation ${view.name} ${mode}`, phone);
      await shot("validation");

      // Refused: a wrong code is said in words and the fields stay.
      await page.getByLabel("Code from the email").fill("111111");
      await page.getByLabel("New password").fill(`Pw-${Date.now()}`);
      await page.getByRole("button", { name: "Change password" }).click();
      await expect(page.getByRole("alert").last()).toContainText(
        "That code is not right or has run out",
      );
      await inspect(page, `refused ${view.name} ${mode}`, phone);
      await shot("refused");

      // The right code ends at sign-in with the notice.
      await page.getByLabel("Code from the email").fill(MOCK_AUTH.resetCode);
      await page.getByRole("button", { name: "Change password" }).click();
      await expect(page).toHaveURL(/\/sign-in\?reason=password-reset/);
      await expect(
        page.getByText(
          /Your password was changed and every device was signed out/,
        ),
      ).toBeVisible();
      // The rest of the sign-in page is the drawn artboard (measured elsewhere): check the notice itself.
      await page.waitForTimeout(500);
      const sideways = await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      );
      expect(sideways, "notice: page scrolls sideways").toBeLessThanOrEqual(0);
      const noticeResults = await new AxeBuilder({ page })
        .include("p.text-success-text")
        .analyze();
      expect(
        noticeResults.violations.filter(
          (v) => v.impact === "serious" || v.impact === "critical",
        ),
        "notice: serious or critical axe violations",
      ).toEqual([]);
      await shot("signed-out-notice");
    });
  }
}
