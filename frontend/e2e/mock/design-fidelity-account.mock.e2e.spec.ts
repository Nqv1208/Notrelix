import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Design fidelity of the account security page at 1440x900, light and dark,
 * every box within 1 px of the mockup at the same viewport:
 *
 *   python3 docs/design/mockups/2026-10/render.py Final-Account.dc.html out.html \
 *     state=bao-mat mode=<sang|dem> nav=an
 *
 * The mock world shows an email account where the mockup shows a managed
 * single sign-on account, and it adds the recent security activity block the
 * mockup does not draw, so the password block and everything below the
 * sessions block are asserted by size, not by position. Colours follow the
 * dark tokens (the dark panel token is #151A21, not the mockup's #1A2028).
 */

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const security = "/workspaces/mock-workspace-primary/account/security";
const THEME_KEY = "notrelix-theme-mode";

test.use({ viewport: { width: 1440, height: 900 } });

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
  await page.waitForTimeout(400);
}

async function expectBox(locator: Locator, expected: Partial<Box>) {
  const actual = (await locator.first().boundingBox()) as Box | null;
  expect(actual, "element is laid out").not.toBeNull();
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want === undefined) continue;
    expect(
      Math.abs(actual![key] - want),
      `${key}: ${actual![key]} vs ${want}`,
    ).toBeLessThanOrEqual(1);
  }
}

for (const dark of [false, true]) {
  test(`security page boxes, ${dark ? "dark" : "light"}`, async ({ page }) => {
    await open(page, dark);
    // The head: 22 px title, the page heading and its line.
    await expectBox(
      page.getByRole("heading", { level: 1, name: "Tài khoản của bạn" }),
      { x: 338, y: 36.1, height: 27.5 },
    );
    expect(
      await page
        .getByRole("heading", { level: 1 })
        .evaluate((element) => getComputedStyle(element).fontSize),
    ).toBe("22px");
    await expectBox(page.getByRole("heading", { level: 2, name: "Bảo mật" }), {
      x: 496,
      y: 102.6,
      width: 319.9,
      height: 26.1,
    });
    // Two-factor block, sessions block: 16 px apart, 916 px wide.
    await expectBox(
      page.getByRole("region", { name: "Xác thực hai yếu tố", exact: true }),
      { x: 496, y: 291.5, width: 916, height: 168.2 },
    );
    await expectBox(
      page.getByRole("region", {
        name: "Phiên đăng nhập đang hoạt động",
        exact: true,
      }),
      { x: 496, y: 475.7, width: 916, height: 228.9 },
    );
    await expectBox(
      page.getByRole("button", { name: "Tắt xác thực hai yếu tố" }),
      { x: 1252.1, y: 360.2, width: 142.9, height: 18.1 },
    );
    await expectBox(page.getByRole("button", { name: "Tạo mã mới" }), {
      x: 1300.5,
      y: 415.2,
      width: 94.5,
      height: 28,
    });
    // The device rows: Sign out buttons 86 x 28 at the right edge, 60 px apart.
    const signOut = page.getByRole("button", { name: /^Đăng xuất (?!mọi)/ });
    await expectBox(signOut.nth(0), {
      x: 1308.8,
      y: 539.8,
      width: 86.2,
      height: 28,
    });
    await expectBox(signOut.nth(1), {
      x: 1308.8,
      y: 600.2,
      width: 86.2,
      height: 28,
    });
    await expectBox(signOut.nth(2), {
      x: 1308.8,
      y: 660.1,
      width: 86.2,
      height: 28,
    });
    // The row icon is 36 px (34 px content and a 1 px border), the text starts at 561.
    await expectBox(page.getByText("Ứng dụng xác thực", { exact: true }), {
      x: 561,
    });
    // Sign out of every other device: a 204 x 32 danger button on the danger fill.
    const others = page.getByRole("button", {
      name: "Đăng xuất mọi thiết bị khác",
    });
    await expectBox(others, { width: 204.5, height: 32 });
    const fill = await others.evaluate((element) => {
      const canvas = document.createElement("canvas");
      canvas.width = canvas.height = 1;
      const context = canvas.getContext("2d", { willReadFrequently: true })!;
      context.fillStyle = getComputedStyle(element).backgroundColor;
      context.fillRect(0, 0, 1, 1);
      return Array.from(context.getImageData(0, 0, 1, 1).data.slice(0, 3));
    });
    const want = dark ? [255, 107, 129] : [196, 35, 64];
    fill.forEach((channel, index) =>
      expect(Math.abs(channel - want[index]!)).toBeLessThanOrEqual(2),
    );
  });
}

/**
 * The toast of "Copy link" (UC-WM-A24) is the Final-States `.toast`: padding
 * 10 10 10 14, a 16 px glyph at the left padding, the text 26 px after it,
 * 13 px at 1.45, so a one-line toast is 38.8 px tall. Final-States draws it
 * on a #1B2430 fill where the app keeps the inverse surface token (#161A21).
 */
test("the link-copied toast keeps the Final-States padding and rhythm", async ({
  page,
}) => {
  await page.context().grantPermissions(["clipboard-read", "clipboard-write"]);
  await page.goto("/workspaces/mock-workspace-primary/boards/mock-board-erp");
  const card = page.locator("article").first();
  await card.hover();
  await card
    .getByRole("button", { name: /tuỳ chọn|Thao tác|Thêm/i })
    .first()
    .click();
  await page.getByRole("menuitem", { name: /Sao chép liên kết/ }).click();
  const toast = page.locator("[data-sonner-toast]").first();
  await expect(toast).toBeVisible();
  await page.waitForTimeout(600);
  const measured = await toast.evaluate((element) => {
    const box = element.getBoundingClientRect();
    const icon = element.querySelector("svg")!.getBoundingClientRect();
    const content = element
      .querySelector("[data-content]")!
      .getBoundingClientRect();
    return {
      height: box.height,
      icon: icon.x - box.x,
      text: content.x - box.x,
      radius: getComputedStyle(element).borderTopLeftRadius,
      size: getComputedStyle(element).fontSize,
    };
  });
  expect(Math.abs(measured.height - 38.85)).toBeLessThanOrEqual(1);
  expect(Math.abs(measured.icon - 14)).toBeLessThanOrEqual(1);
  expect(Math.abs(measured.text - 40)).toBeLessThanOrEqual(1);
  expect(measured.radius).toBe("10px");
  expect(measured.size).toBe("13px");
});

/**
 * At 390 px every setting row keeps its text column readable: the action moves
 * under the text, 48 px in (the 36 px icon and its 12 px gap), and is 44 px tall.
 */
test("at 390 px a setting row puts its action under the text", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await open(page, false);
  const description = page.getByText(
    "Nhập mã 6 số từ ứng dụng mỗi lần đăng nhập trên thiết bị mới",
  );
  const title = page.getByText("Ứng dụng xác thực", { exact: true });
  const button = page.getByRole("button", { name: "Tạo mã mới" });
  const column = (await description.boundingBox())!;
  const heading = (await title.boundingBox())!;
  const action = (await button.boundingBox())!;
  expect(column.width, "the description column").toBeGreaterThanOrEqual(240);
  expect(action.y, "the action is below the text").toBeGreaterThanOrEqual(
    column.y + column.height,
  );
  expect(
    Math.abs(action.x - heading.x),
    "left-aligned with the text",
  ).toBeLessThanOrEqual(1);
  expect(action.height, "a 44 px target").toBeGreaterThanOrEqual(44);
  const sessionAction = (await page
    .getByRole("button", { name: /^Đăng xuất (?!mọi)/ })
    .first()
    .boundingBox())!;
  expect(sessionAction.height).toBeGreaterThanOrEqual(44);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
    "no horizontal overflow",
  ).toBe(true);
});
