import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * Changing the sign-in email on the security page (UC-IDN-K09), no artboard draws it: a walk of every
 * state at 1440, 1024 and 390 px in light and dark, in Vietnamese. Per frame: the sentence shows,
 * no sideways scroll, every control a 44 px target on a phone, axe clean. The reviewer sets
 * AUTH_SHOTS_DIR to keep the images of a passing run and opens each one.
 */
/** The email of the signed-in person of the default mock world. */
const CURRENT_EMAIL = "ui-dev@notrelix.local";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const security = "/workspaces/mock-workspace-primary/account/security";
const THEME_KEY = "notrelix-theme-mode";
const VIEWS = [
  { name: "1440", width: 1440, height: 900 },
  { name: "1024", width: 1024, height: 768 },
  { name: "390", width: 390, height: 844 },
] as const;

test.beforeEach(() => {
  test.skip(scenario !== "default", "Assertions need the default world data");
});

async function frame(page: Page, label: string, width: number) {
  await page.waitForTimeout(400);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth - window.innerWidth,
    ),
    `${label}: page scrolls sideways`,
  ).toBeLessThanOrEqual(0);
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
  const dir = process.env.AUTH_SHOTS_DIR;
  if (dir)
    await page.screenshot({ path: `${dir}/${label}.png`, fullPage: true });
}

async function prepare(
  page: Page,
  width: number,
  height: number,
  dark: boolean,
) {
  await page.setViewportSize({ width, height });
  await page.addInitScript(
    ({ key, theme }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem(key, theme);
      } catch {
        // Private mode: defaults apply.
      }
    },
    { key: THEME_KEY, theme: dark ? "dark" : "light" },
  );
}

// The mock lives in the page: a reload forgets what the walk did, so the walk moves without one.
const goTo = (page: Page, path: string) =>
  page.evaluate((target) => {
    history.pushState({}, "", target);
    window.dispatchEvent(new PopStateEvent("popstate"));
  }, path);

for (const view of VIEWS) {
  for (const dark of [false, true]) {
    const mode = dark ? "dark" : "light";
    const tag = `${view.name}-${mode}`;

    test(`a person changes the sign-in email, ${tag}`, async ({ page }) => {
      test.setTimeout(90_000);
      await prepare(page, view.width, view.height, dark);
      await page.goto(security);
      await expect(
        page.getByRole("heading", { level: 2, name: "Bảo mật" }),
      ).toBeVisible();
      await expect(
        page.getByText(CURRENT_EMAIL, { exact: true }).first(),
      ).toBeVisible();
      await frame(page, `email-row-${tag}`, view.width);

      // The proof comes first (a code, two-factor is on), then the address is asked.
      await page.getByRole("button", { name: "Đổi email" }).click();
      const proof = page.getByRole("region", {
        name: "Xác nhận để đổi email đăng nhập",
      });
      await expect(proof).toBeVisible();
      await frame(page, `email-proof-${tag}`, view.width);
      await page.getByLabel("Mã", { exact: true }).fill(MOCK_AUTH.otpCode);
      await page.keyboard.press("Enter");
      const form = page.getByRole("region", { name: "Đổi email đăng nhập" });
      await expect(form).toBeVisible();
      await expect(form.getByLabel("Email mới")).toBeFocused();
      await frame(page, `email-form-${tag}`, view.width);

      // A malformed address and the current one are told before anything is sent.
      await form.getByLabel("Email mới").fill("khong-phai-email");
      await form.getByRole("button", { name: "Gửi liên kết" }).click();
      await expect(form.getByRole("alert")).toContainText("hợp lệ");
      await frame(page, `email-invalid-${tag}`, view.width);
      await form.getByLabel("Email mới").fill(CURRENT_EMAIL);
      await form.getByRole("button", { name: "Gửi liên kết" }).click();
      await expect(form.getByRole("alert")).toContainText("đã là email");
      await frame(page, `email-same-${tag}`, view.width);

      // Any other address gets the same answer; the sign-in email shown stays the current one.
      await form.getByLabel("Email mới").fill("moi@example.com");
      await form.getByRole("button", { name: "Gửi liên kết" }).click();
      const sent = page.getByRole("region", {
        name: "Hãy kiểm tra hộp thư mới",
      });
      await expect(sent).toBeVisible();
      await expect(sent).toContainText("moi@example.com");
      await expect(
        page.getByText(CURRENT_EMAIL, { exact: true }).first(),
      ).toBeVisible();
      await frame(page, `email-sent-${tag}`, view.width);
      await sent.getByRole("button", { name: "Xong" }).click();
      await expect(sent).toHaveCount(0);
    });
  }
}
