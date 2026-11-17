import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * Connected accounts page (UC-IDN-G01, G02), drawn as Idn-Providers, with the 1 px fidelity in design-fidelity-account-providers: a walk of every
 * state at 1440, 1024 and 390 px in light and dark, in Vietnamese. Per frame: the sentence shows,
 * no sideways scroll, every control a 44 px target on a phone, axe clean. The reviewer sets
 * AUTH_SHOTS_DIR to keep the images of a passing run and opens each one.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const security = "/workspaces/mock-workspace-primary/account/providers";
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

    test(`a person with a password connects and disconnects Google, ${tag}`, async ({
      page,
    }) => {
      test.setTimeout(90_000);
      await prepare(page, view.width, view.height, dark);
      await page.goto(security);
      const google = page.getByRole("button", { name: "Liên kết Google" });
      await expect(google).toBeVisible();
      await frame(page, `link-list-${tag}`, view.width);

      await google.click();
      const prompt = page.getByRole("region", {
        name: "Xác nhận để liên kết Google",
      });
      await expect(prompt).toBeVisible();
      await frame(page, `link-prompt-${tag}`, view.width);

      // G01: a wrong code is refused in words and nothing is linked.
      const code = page.getByLabel("Mã", { exact: true });
      await code.fill("sai");
      await page.keyboard.press("Enter");
      await expect(page.getByRole("alert")).toContainText("Mã chưa đúng");
      await frame(page, `link-wrong-${tag}`, view.width);
      await code.fill(MOCK_AUTH.otpCode);
      await page.keyboard.press("Enter");
      await expect(page.getByText("Đã liên kết Google.")).toBeVisible();
      await expect(
        page.getByRole("button", { name: "Gỡ liên kết Google" }),
      ).toBeVisible();
      await frame(page, `link-done-${tag}`, view.width);

      // G02: disconnecting asks again, says what it costs, and leaves the password.
      await page.getByRole("button", { name: "Gỡ liên kết Google" }).click();
      await expect(
        page.getByRole("region", { name: "Xác nhận để gỡ liên kết Google" }),
      ).toBeVisible();
      await expect(page.getByText("Bạn sẽ không còn đăng nhập")).toBeVisible();
      await frame(page, `unlink-prompt-${tag}`, view.width);
      await page.getByLabel("Mã", { exact: true }).fill(MOCK_AUTH.otpCode);
      await page.keyboard.press("Enter");
      await expect(page.getByText("Đã gỡ liên kết Google.")).toBeVisible();
      await expect(
        page.getByRole("button", { name: "Liên kết Google" }),
      ).toBeVisible();
      await frame(page, `unlink-done-${tag}`, view.width);

      // G01: a provider identity another person owns is refused, naming no one; any other failure is told too.
      await goTo(page, `${security}?link=collision&provider=Google`);
      await expect(page.getByRole("alert")).toContainText(
        "đang dùng cho một tài khoản",
      );
      await page.getByRole("alert").scrollIntoViewIfNeeded();
      await expect(page.getByRole("alert")).toBeInViewport();
      await frame(page, `link-collision-${tag}`, view.width);
      await goTo(page, `${security}?link=failed&provider=Microsoft`);
      await expect(
        page.getByText("Không liên kết được Microsoft"),
      ).toBeVisible();
      await page.getByRole("alert").scrollIntoViewIfNeeded();
      await expect(page.getByRole("alert")).toBeInViewport();
      await frame(page, `link-failed-${tag}`, view.width);
    });

    test(`a person with only Google cannot disconnect it, ${tag}`, async ({
      page,
    }) => {
      test.setTimeout(90_000);
      await prepare(page, view.width, view.height, dark);
      // A live session forwards a plain /sign-in; a refusal code keeps the form.
      await page.goto("/sign-in?error=identity.oauth.failed");
      await page.getByRole("button", { name: "Tiếp tục với Google" }).click();
      await expect(page).toHaveURL(
        /\/(home|workspaces\/mock-workspace-primary)/,
      );
      await goTo(page, security);
      await expect(
        page.getByText("là cách đăng nhập duy nhất của bạn"),
      ).toBeVisible();
      await expect(
        page.getByRole("button", { name: "Gỡ liên kết Google" }),
      ).toBeDisabled();
      await frame(page, `last-method-${tag}`, view.width);

      // The proof is the provider they signed in with; back from it the link goes on.
      await page.getByRole("button", { name: "Liên kết Microsoft" }).click();
      await expect(
        page.getByRole("region", { name: "Xác nhận để liên kết Microsoft" }),
      ).toBeVisible();
      await frame(page, `link-provider-prompt-${tag}`, view.width);
      await page.getByRole("button", { name: "Xác nhận bằng Google" }).click();
      await expect(page.getByText("Đã liên kết Microsoft.")).toBeVisible();

      // Two providers: the first can go (the proof again, by the provider).
      await page.getByRole("button", { name: "Gỡ liên kết Google" }).click();
      await page.getByRole("button", { name: "Xác nhận bằng Google" }).click();
      await expect(page.getByText("Đã gỡ liên kết Google.")).toBeVisible();
      // Microsoft is now the only way in: it cannot be disconnected.
      await expect(
        page.getByRole("button", { name: "Gỡ liên kết Microsoft" }),
      ).toBeDisabled();
      await frame(page, `unlink-last-${tag}`, view.width);
    });
  }
}
