import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * The sign-in states at 1440, 1024, 390 and 320 px in light and dark (UC-IDN-B05, B06, B07, B09,
 * I01, I02, J01): no sideways scroll, every control a 44 px target on a phone (the show-password
 * eye and the link rows included), axe clean. Screenshots go to the test output; the reviewer
 * sets AUTH_SHOTS_DIR to keep the images of a passing run and opens each one.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const THEME_KEY = "notrelix-theme-mode";
const VIEWS = [
  { name: "1440", width: 1440, height: 900 },
  { name: "1024", width: 1024, height: 768 },
  { name: "390", width: 390, height: 844 },
  { name: "320", width: 320, height: 640 },
] as const;

test.beforeEach(() => {
  test.skip(scenario !== "default", "Assertions need the default world data");
});

async function inspect(page: Page, label: string, width: number) {
  await page.waitForTimeout(500);
  const over = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth,
  );
  expect(over, `${label}: page scrolls sideways`).toBeLessThanOrEqual(0);
  if (width <= 390) {
    const small = await page.evaluate(() =>
      Array.from(
        document.querySelectorAll<HTMLElement>(
          "main button:not([disabled]), main input:not([disabled]):not([type=hidden])",
        ),
      )
        .filter((el) => el.offsetParent !== null && !el.closest("[inert]"))
        .map((el) => ({
          name: (el.getAttribute("aria-label") ?? el.textContent ?? el.id)
            .trim()
            .slice(0, 40),
          height: Math.round(el.getBoundingClientRect().height),
        }))
        // The footer's legal words are plain text, not controls (they carry no button).
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
    test(`sign-in states are clean, ${view.name} ${mode}`, async ({
      page,
    }, testInfo) => {
      await page.setViewportSize({ width: view.width, height: view.height });
      await page.addInitScript(
        ({ key, theme }) => {
          try {
            window.localStorage.setItem("notrelix-locale", "vi");
            window.localStorage.setItem(key, theme);
          } catch {
            // Private mode: defaults apply.
          }
        },
        { key: THEME_KEY, theme: mode },
      );
      const step = async (name: string) => {
        await inspect(page, `${name} ${view.name} ${mode}`, view.width);
        const file = `sign-in-${name}-${view.name}-${mode}`;
        await testInfo.attach(file, {
          body: await page.screenshot({ fullPage: true }),
          contentType: "image/png",
        });
        const dir = process.env.AUTH_SHOTS_DIR;
        if (dir)
          await page.screenshot({ path: `${dir}/${file}.png`, fullPage: true });
      };
      const submit = page.locator("form button[type=submit]");

      await page.goto("/sign-in");
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
      await step("email");

      await page.getByLabel("Email").fill(MOCK_AUTH.personalEmail);
      await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
      await page.locator("#auth-password").fill("x".repeat(12));
      await step("password");

      // The ten wrong passwords of the lock.
      const password = page.locator("#auth-password");
      for (let attempt = 1; attempt <= 10; attempt += 1) {
        await password.fill(`wrong-try-${attempt}`);
        await submit.click();
        await expect(password).toHaveValue("");
      }
      await expect(page.getByText(/Thử lại sau \d+ giây/)).toBeVisible();
      await step("locked");

      // A fresh page for the second step (the lock outlives it in the mock world).
      await page.goto("/forgot-password");
      await expect(page.locator("#auth-reset")).toBeVisible();
      await step("forgot");
      await page.locator("#auth-reset").fill(MOCK_AUTH.personalEmail);
      await submit.click();
      await expect(
        page.getByRole("heading", { level: 1, name: "Kiểm tra email của bạn" }),
      ).toBeVisible();
      await step("sent");
    });

    test(`second-step states are clean, ${view.name} ${mode}`, async ({
      page,
    }, testInfo) => {
      await page.setViewportSize({ width: view.width, height: view.height });
      await page.addInitScript(
        ({ key, theme }) => {
          try {
            window.localStorage.setItem("notrelix-locale", "vi");
            window.localStorage.setItem(key, theme);
          } catch {
            // Private mode: defaults apply.
          }
        },
        { key: THEME_KEY, theme: mode },
      );
      const step = async (name: string) => {
        await inspect(page, `${name} ${view.name} ${mode}`, view.width);
        const file = `second-step-${name}-${view.name}-${mode}`;
        await testInfo.attach(file, {
          body: await page.screenshot({ fullPage: true }),
          contentType: "image/png",
        });
        const dir = process.env.AUTH_SHOTS_DIR;
        if (dir)
          await page.screenshot({ path: `${dir}/${file}.png`, fullPage: true });
      };
      const submit = page.locator("form button[type=submit]");
      await page.goto("/sign-in");
      await page.getByLabel("Email").fill(MOCK_AUTH.personalEmail);
      await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
      await page.locator("#auth-password").fill(MOCK_AUTH.personalPassword);
      await submit.click();
      await expect(
        page.getByRole("heading", { level: 1, name: "Xác minh hai bước" }),
      ).toBeVisible();
      await step("code");

      // A wrong code, then the backup code field.
      for (const [index, digit] of "000000".split("").entries())
        await page.getByLabel(`Số ${index + 1} trên 6`).fill(digit);
      await submit.click();
      await expect(page.getByRole("alert")).toContainText("Mã chưa đúng");
      await step("wrong");
      await page.getByRole("button", { name: "Dùng mã dự phòng" }).click();
      await expect(page.getByLabel("Mã dự phòng")).toBeVisible();
      await step("backup");

      // Four more wrong codes end the challenge.
      for (let attempt = 0; attempt < 4; attempt += 1) {
        await page.getByLabel("Mã dự phòng").fill(`WRONG-CODE-${attempt}`);
        await submit.click();
        await page.waitForTimeout(150);
      }
      await expect(
        page.getByRole("button", { name: "Quay lại đăng nhập" }),
      ).toBeVisible();
      await step("ended");
    });
  }
}
