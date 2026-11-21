import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * Frame review of the provider sign-in states no artboard draws (UC-IDN-F01, F02, K06) at 1440, 1024
 * and 390 px in light and dark: each provider refusal on the sign-in page and the provider step-up
 * prompt with its refusal on the security page. Per frame: the sentence shows, no sideways scroll,
 * every control a 44 px target on a phone, axe clean. The reviewer sets AUTH_SHOTS_DIR to keep the
 * images of a passing run and opens each one.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const security = "/workspaces/mock-workspace-primary/account/security";
const THEME_KEY = "notrelix-theme-mode";
const VIEWS = [
  { name: "1440", width: 1440, height: 900 },
  { name: "1024", width: 1024, height: 768 },
  { name: "390", width: 390, height: 844 },
] as const;
const CODES: Record<string, string> = {
  "identity.oauth.failed": "chưa được",
  "identity.oauth.state-invalid": "quá lâu hoặc đã được dùng",
  "identity.oauth.denied": "dừng việc đăng nhập",
  "identity.oauth.email-in-use": "Đã có tài khoản",
  "identity.oauth.email-unverified": "chưa xác minh email",
  "identity.oauth.provider-unavailable":
    "chưa đăng nhập được bằng nhà cung cấp",
};

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

for (const view of VIEWS) {
  for (const dark of [false, true]) {
    const mode = dark ? "dark" : "light";
    test(`provider refusals on the sign-in page, ${view.name} ${mode}`, async ({
      page,
    }) => {
      test.setTimeout(60_000);
      await prepare(page, view.width, view.height, dark);
      for (const [code, words] of Object.entries(CODES)) {
        await page.goto(`/sign-in?error=${code}`);
        await expect(page.getByRole("alert")).toContainText(words);
        await expect(page.getByLabel("Email")).toBeVisible();
        await frame(
          page,
          `provider-${code.split(".").pop()}-${view.name}-${mode}`,
          view.width,
        );
      }
    });

    test(`provider step-up prompt and refusal, ${view.name} ${mode}`, async ({
      page,
    }) => {
      test.setTimeout(60_000);
      await prepare(page, view.width, view.height, dark);
      // A live session forwards a plain /sign-in; a refusal code keeps the form.
      await page.goto("/sign-in?error=identity.oauth.failed");
      await page.getByRole("button", { name: "Tiếp tục với Google" }).click();
      await expect(page).toHaveURL(
        /\/(home|workspaces\/mock-workspace-primary)/,
      );
      const goTo = (path: string) =>
        page.evaluate((target) => {
          history.pushState({}, "", target);
          window.dispatchEvent(new PopStateEvent("popstate"));
        }, path);
      await goTo(security);
      await page
        .getByRole("button", { name: "Bật xác thực hai yếu tố" })
        .click();
      await expect(
        page.getByRole("region", {
          name: "Xác nhận để bật xác thực hai yếu tố",
        }),
      ).toBeVisible();
      await frame(page, `stepup-prompt-${view.name}-${mode}`, view.width);
      await goTo(`${security}?stepUp=others&stepUpResult=failed`);
      await expect(page.getByRole("alert")).toContainText(
        "Chưa xác nhận được bạn với Google",
      );
      await frame(page, `stepup-refused-${view.name}-${mode}`, view.width);
    });
  }
}
