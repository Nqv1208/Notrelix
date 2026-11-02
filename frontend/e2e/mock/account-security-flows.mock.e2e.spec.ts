import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";
import { MOCK_AUTH } from "../../packages/dev/mock-backend/src/state/mock-auth-directory";

/**
 * Acceptance walk of the account security flows (UC-IDN-C03, H01, H02, J03, K03, K04, K05):
 * every prompt and panel state, by keyboard, with axe at 1440 and 390 px in light and dark.
 * The mock backend lives in the page, so one walk keeps its state; turning two-factor off
 * ends the session, and the walk signs back in without a reload to reach the "off" states.
 */

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const security = "/workspaces/mock-workspace-primary/account/security";
const THEME_KEY = "notrelix-theme-mode";

test.beforeEach(async ({ page }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

async function axeClean(page: Page, label: string) {
  const results = await new AxeBuilder({ page }).analyze();
  const blocking = results.violations
    .filter((v) => v.impact === "serious" || v.impact === "critical")
    .map((v) => ({
      id: v.id,
      impact: v.impact,
      nodes: v.nodes.map((n) => n.target.join(" ")),
    }));
  expect(blocking, `serious or critical axe violations in ${label}`).toEqual(
    [],
  );
}

async function noOverflow(page: Page, label: string) {
  const over = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth,
  );
  expect(over, `${label}: page scrolls sideways`).toBeLessThanOrEqual(0);
}

/** Each state: no sideways scroll, every button and field at least 44 px tall on a phone, axe clean. */
async function inspect(page: Page, label: string, phone: boolean) {
  await noOverflow(page, label);
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
  await axeClean(page, label);
}

async function signBackIn(page: Page) {
  await page.getByRole("button", { name: "Đăng nhập" }).press("Enter");
  await expect(page.getByLabel("Email")).toBeVisible();
  await page.waitForTimeout(400);
  await page.getByLabel("Email").fill(MOCK_AUTH.personalEmail);
  await page.getByRole("button", { name: "Tiếp tục", exact: true }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Nhập mật khẩu" }),
  ).toBeVisible();
  await page
    .getByLabel("Mật khẩu", { exact: true })
    .fill(MOCK_AUTH.personalPassword);
  await page.locator("form button[type=submit]").click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Xác minh hai bước" }),
  ).toBeVisible();
  for (const [index, digit] of MOCK_AUTH.otpCode.split("").entries())
    await page.getByLabel(`Số ${index + 1} trên 6`).fill(digit);
  await page.getByRole("button", { name: "Xác minh" }).click();
  await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary/);
  // A client-side move keeps the mock world (a reload would reset it).
  await page.evaluate((path) => {
    history.pushState({}, "", path);
    window.dispatchEvent(new PopStateEvent("popstate"));
  }, security);
  await expect(
    page.getByRole("heading", { level: 2, name: "Bảo mật" }),
  ).toBeVisible();
}

const code = (page: Page) => page.getByLabel("Mã", { exact: true });

async function answer(page: Page, field: Locator, text: string) {
  await expect(field).toBeFocused();
  await page.keyboard.type(text);
  await page.keyboard.press("Enter");
}

const VIEWS = [
  { name: "1440", width: 1440, height: 900 },
  { name: "390", width: 390, height: 844 },
] as const;

for (const view of VIEWS) {
  for (const dark of [false, true]) {
    test(`security flows are keyboard-complete and axe-clean, ${view.name} ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      const phone = view.width < 500;
      await page.setViewportSize({ width: view.width, height: view.height });
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
      await page.goto(security);
      await expect(
        page.getByRole("heading", { level: 2, name: "Bảo mật" }),
      ).toBeVisible();
      await expect(page.getByRole("status", { name: /\S/ })).toHaveCount(0);
      await inspect(page, "page", phone);

      // C03: ending this device asks first, with the safe answer focused.
      const signOutThis = page.getByRole("button", {
        name: /\(thiết bị này\)/,
      });
      await signOutThis.focus();
      await page.keyboard.press("Enter");
      await expect(
        page.getByRole("region", { name: "Đăng xuất khỏi thiết bị này?" }),
      ).toBeVisible();
      await expect(page.getByRole("button", { name: "Hủy" })).toBeFocused();
      await inspect(page, "sign-out-this-confirm", phone);
      await page.keyboard.press("Enter");

      // K03/K04: new backup codes ask which factor applies (a code), refuse a wrong one, accept the right one.
      await page.getByRole("button", { name: "Tạo mã mới" }).press("Enter");
      await expect(
        page.getByRole("region", { name: "Xác nhận để tạo mã dự phòng mới" }),
      ).toBeVisible();
      await inspect(page, "code-prompt", phone);
      await answer(page, code(page), "000000");
      await expect(page.getByRole("alert")).toContainText("Mã chưa đúng");
      await expect(code(page)).toHaveAttribute("aria-invalid", "true");
      await inspect(page, "wrong-code", phone);
      await code(page).focus();
      await page.keyboard.type(MOCK_AUTH.otpCode);
      await page.keyboard.press("Enter");
      // H02's codes panel: focus lands on the button that confirms they were saved.
      await expect(page.getByText("Lưu mã dự phòng của bạn")).toBeVisible();
      await expect(
        page.getByRole("button", { name: "Tôi đã lưu" }),
      ).toBeFocused();
      await inspect(page, "backup-codes", phone);
      await page.keyboard.press("Enter");

      // J03: turn off asks for a fresh code, then the session ends and the page says so.
      await page
        .getByRole("button", { name: "Tắt xác thực hai yếu tố" })
        .press("Enter");
      await expect(
        page.getByRole("region", { name: "Xác nhận để tắt xác thực hai lớp" }),
      ).toBeVisible();
      await inspect(page, "disable-code-prompt", phone);
      await answer(page, code(page), MOCK_AUTH.otpCode);
      await expect(
        page.getByRole("region", { name: "Đã tắt xác thực hai lớp" }),
      ).toBeVisible();
      await expect(
        page.getByRole("button", { name: "Đăng nhập" }),
      ).toBeFocused();
      await inspect(page, "two-factor-off", phone);
      await signBackIn(page);

      // H01/K05: with no factor, turning on asks for the password first, then shows the key.
      await page
        .getByRole("button", { name: "Bật xác thực hai yếu tố" })
        .press("Enter");
      await expect(
        page.getByRole("region", {
          name: "Xác nhận để bật xác thực hai yếu tố",
        }),
      ).toBeVisible();
      const password = page.getByLabel("Mật khẩu hiện tại", { exact: true });
      await inspect(page, "password-prompt", phone);
      await answer(page, password, "sai");
      await expect(page.getByRole("alert")).toContainText("Mật khẩu chưa đúng");
      await inspect(page, "wrong-password", phone);
      await password.focus();
      await page.keyboard.type(MOCK_AUTH.personalPassword);
      await page.keyboard.press("Enter");
      await expect(
        page.getByRole("img", { name: "Mã QR cho ứng dụng xác thực" }),
      ).toBeVisible();
      await inspect(page, "enroll-qr", phone);
      // H02: a wrong first code leaves the setup open; the right one shows the codes and the others signed out.
      await answer(page, code(page), "000000");
      await expect(page.getByRole("alert")).toContainText("Mã chưa đúng");
      await inspect(page, "enroll-wrong-code", phone);
      await code(page).focus();
      await page.keyboard.type(MOCK_AUTH.otpCode);
      await page.keyboard.press("Enter");
      await expect(page.getByText("Lưu mã dự phòng của bạn")).toBeVisible();
      // No other device was signed in after the walk's sign-in, so the panel says nobody was signed out.
      await expect(
        page
          .getByRole("region", { name: "Lưu mã dự phòng của bạn" })
          .getByRole("status"),
      ).toContainText("thiết bị");
      await inspect(page, "enroll-backup-codes", phone);
    });
  }

  test(`a throttled step waits and says how long, ${view.name} light and dark`, async ({
    page,
  }) => {
    const phone = view.width < 500;
    await page.setViewportSize({ width: view.width, height: view.height });
    for (const dark of [false, true]) {
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
      await page.goto(security);
      await expect(
        page.getByRole("heading", { level: 2, name: "Bảo mật" }),
      ).toBeVisible();
      await page.getByRole("button", { name: "Tạo mã mới" }).click();
      for (let attempt = 0; attempt < 5; attempt += 1) {
        await code(page).fill(`00000${attempt}`);
        await page.getByRole("button", { name: "Xác nhận" }).click();
        await expect(page.getByRole("alert")).toBeVisible();
      }
      await expect(page.getByRole("alert")).toContainText("Thử quá nhiều lần");
      await expect(
        page.getByRole("button", { name: "Xác nhận" }),
      ).toBeDisabled();
      await inspect(page, `rate-limited ${dark ? "dark" : "light"}`, phone);
    }
  });
}

test("Tab order reaches every control of the security page and its prompt", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(security);
  await expect(
    page.getByRole("heading", { level: 2, name: "Bảo mật" }),
  ).toBeVisible();
  const reached = new Set<string>();
  for (let step = 0; step < 60; step += 1) {
    await page.keyboard.press("Tab");
    const name = await page.evaluate(() => {
      const el = document.activeElement as HTMLElement | null;
      return el
        ? (el.getAttribute("aria-label") ?? el.textContent ?? "").trim()
        : "";
    });
    reached.add(name);
  }
  for (const name of [
    "Tạo mã mới",
    "Tắt xác thực hai yếu tố",
    "Đăng xuất mọi thiết bị khác",
  ])
    expect([...reached], `Tab reaches ${name}`).toContain(name);
  expect(
    [...reached].some((n) => /^Đăng xuất .*\(thiết bị này\)$/.test(n)),
    "Tab reaches the this-device sign-out",
  ).toBe(true);
});

/**
 * UC-IDN-K03/K06: a person who signs in with Google has no password, so step-up is the provider,
 * never a password prompt they cannot satisfy. By keyboard, with axe, at both widths in both themes.
 */
for (const view of VIEWS) {
  for (const dark of [false, true]) {
    test(`a person with no password confirms with the provider, ${view.name} ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      const phone = view.width < 500;
      await page.setViewportSize({ width: view.width, height: view.height });
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
      await page.goto("/sign-in");
      await page.getByRole("button", { name: "Tiếp tục với Google" }).click();
      await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary/);
      const goTo = (path: string) =>
        page.evaluate((target) => {
          history.pushState({}, "", target);
          window.dispatchEvent(new PopStateEvent("popstate"));
        }, path);
      // A client-side move keeps the mock world (a reload would reset it).
      await goTo(security);
      await expect(
        page.getByRole("heading", { level: 2, name: "Bảo mật" }),
      ).toBeVisible();

      await page
        .getByRole("button", { name: "Bật xác thực hai yếu tố" })
        .press("Enter");
      const prompt = page.getByRole("region", {
        name: "Xác nhận để bật xác thực hai yếu tố",
      });
      await expect(prompt).toBeVisible();
      // No field to type into: the way on is the provider, with the safe first action focused.
      await expect(prompt.getByRole("textbox")).toHaveCount(0);
      await expect(page.locator('input[type="password"]')).toHaveCount(0);
      await expect(
        prompt.getByRole("button", { name: "Xác nhận bằng Google" }),
      ).toBeFocused();
      await inspect(page, "provider-prompt", phone);

      // The provider round trip comes back to the page and goes straight on to the setup.
      await page.keyboard.press("Enter");
      await expect(
        page.getByRole("img", { name: "Mã QR cho ứng dụng xác thực" }),
      ).toBeVisible();
      await expect(page).toHaveURL(/\/account\/security$/);
      await inspect(page, "provider-enroll-qr", phone);

      // The provider refused: the page says so and offers the provider again.
      await goTo(`${security}?stepUp=others&stepUpResult=failed`);
      await expect(page.getByRole("alert")).toContainText(
        "Chưa xác nhận được bạn với Google",
      );
      await expect(
        page.getByRole("button", { name: "Xác nhận bằng Google" }),
      ).toBeVisible();
      await inspect(page, "provider-failed", phone);
    });
  }
}
