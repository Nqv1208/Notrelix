import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * Setting a first password on the security page (UC-IDN-K08), no artboard draws it: a walk of every
 * state at 1440, 1024 and 390 px in light and dark, in Vietnamese. Per frame: the sentence shows,
 * no sideways scroll, every control a 44 px target on a phone, axe clean. The reviewer sets
 * AUTH_SHOTS_DIR to keep the images of a passing run and opens each one.
 */
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

    test(`a person with only Google sets a first password, ${tag}`, async ({
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
      await expect(page.getByText("Chưa có mật khẩu")).toBeVisible();
      await expect(
        page.getByRole("button", { name: "Đổi mật khẩu" }),
      ).toHaveCount(0);
      await frame(page, `password-none-${tag}`, view.width);

      // The proof is the provider they signed in with; back from it the form opens.
      await page.getByRole("button", { name: "Đặt mật khẩu" }).click();
      await expect(
        page.getByRole("region", { name: "Xác nhận để đặt mật khẩu" }),
      ).toBeVisible();
      await frame(page, `password-proof-${tag}`, view.width);
      await page.getByRole("button", { name: "Xác nhận bằng Google" }).click();
      const form = page.getByRole("region", { name: "Đặt mật khẩu" });
      await expect(form).toBeVisible();
      await expect(
        form.getByLabel("Mật khẩu mới", { exact: true }),
      ).toBeFocused();
      await frame(page, `password-form-${tag}`, view.width);

      // A short password and an unmatched repeat are told before anything is sent.
      await form.getByLabel("Mật khẩu mới", { exact: true }).fill("ngan");
      await form.getByLabel("Nhập lại mật khẩu mới").fill("ngan");
      await form.getByRole("button", { name: "Đặt mật khẩu" }).click();
      await expect(form.getByRole("alert")).toContainText("ít nhất 8 ký tự");
      await frame(page, `password-short-${tag}`, view.width);
      await form
        .getByLabel("Mật khẩu mới", { exact: true })
        .fill("MatKhauMoi123!");
      await form.getByLabel("Nhập lại mật khẩu mới").fill("MatKhauKhac123!");
      await form.getByRole("button", { name: "Đặt mật khẩu" }).click();
      await expect(form.getByRole("alert")).toContainText("chưa khớp");
      await frame(page, `password-mismatch-${tag}`, view.width);

      await form.getByLabel("Nhập lại mật khẩu mới").fill("MatKhauMoi123!");
      await form.getByRole("button", { name: "Đặt mật khẩu" }).click();
      await expect(page.getByText("Đã đặt mật khẩu.")).toBeVisible();
      await expect(
        page.getByRole("button", { name: "Đổi mật khẩu" }),
      ).toBeVisible();
      // With a password, the provider is no longer the only way in.
      await expect(
        page.getByRole("button", { name: "Ngắt kết nối Google" }),
      ).toBeVisible();
      await frame(page, `password-done-${tag}`, view.width);
    });
  }
}
