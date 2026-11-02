import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The invitation pages of the canvas, every box within 1 px of the mockup at the same viewport, light and dark:
 *
 *   python3 docs/design/mockups/2026-10/render.py Final-Auth.dc.html out.html state=<loi-moi|het-han> mode=<sang|dem>
 *   python3 docs/design/mockups/2026-10/render.py Final-M-Auth.dc.html out.html state=loi-moi mode=<sang|dem>
 *
 * Numbers are the box dump of those renders. At 1440 the form column sits at x 836.8, 400 wide, centred in the page.
 *
 *  - `loi-moi` (board invitation, new person): the page title is the 24 px product token where the artboard draws
 *    23 px, so the three-line title is 3.96 px taller and the centred column starts 1.98 px higher: boxes above the
 *    title's bottom edge are expected 1.98 px higher, boxes below 1.98 px lower (the sso state has the same offset).
 *    The words (inviter, level, password length) are the product's; the box that holds them (the level chip) is
 *    compared by height only.
 *  - `het-han` (expired): the artboard exactly.
 *  - Final-M-Auth `loi-moi` (390): a native screen drawn in a phone frame with a status bar and a bar that holds the
 *    brand mark. The page keeps the artboard's content boxes (avatar, title, card, line, two buttons), measured
 *    from the avatar's top (the frame's status bar and the page's top gutter differ); the two buttons follow the
 *    content instead of sitting at the bottom of the frame (a page scrolls), so only their x, width and height are
 *    compared. A new person types a name and a password after pressing Chấp nhận (the card shows no form).
 *    The brand mark is the 28 px mark at x 18 with the 17 px name.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const THEME_KEY = "notrelix-theme-mode";

/** The artboards' tokens, light (`sang`) and dark (`dem`). */
const PALETTE = {
  light: {
    ink: "rgb(22, 26, 33)",
    calm: "rgb(79, 88, 102)",
    hint: "rgb(95, 104, 117)",
    link: "rgb(15, 86, 174)",
    icon: "rgb(241, 243, 246)",
    line: "rgb(230, 233, 238)",
    // The artboard draws #FFFFFF; the product's light panel token is one step off white (#FEFEFF).
    panel: "rgb(254, 254, 255)",
    accent: "rgb(22, 102, 204)",
    see: "rgb(225, 229, 234)",
  },
  dark: {
    ink: "rgb(230, 233, 238)",
    calm: "rgb(170, 179, 192)",
    hint: "rgb(143, 153, 167)",
    // The artboard draws #7DBBFF; the product's dark link-text token is #90C2FD (a shared token, not an Auth
    // decision). The value asserted here is the token, so a token change shows up.
    link: "rgb(144, 194, 253)",
    icon: "rgb(28, 35, 44)",
    line: "rgb(35, 43, 53)",
    panel: "rgb(21, 26, 33)",
    accent: "rgb(90, 169, 255)",
    see: "rgb(35, 43, 53)",
  },
} as const;
let COLOUR: (typeof PALETTE)[keyof typeof PALETTE] = PALETTE.light;

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function expectBox(
  label: string,
  locator: Locator,
  expected: Partial<Rect>,
  dy = 0,
) {
  const actual = await locator.first().boundingBox();
  expect(actual, `${label}: element is laid out`).not.toBeNull();
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want === undefined) continue;
    const target = key === "y" ? want + dy : want;
    expect(
      Math.abs(actual![key] - target),
      `${label}.${key}: ${actual![key].toFixed(1)} vs ${target}`,
    ).toBeLessThanOrEqual(1);
  }
}

async function style(locator: Locator, property: string): Promise<string> {
  return locator
    .first()
    .evaluate(
      (element, name) =>
        getComputedStyle(element).getPropertyValue(name).trim(),
      property,
    );
}

async function colour(locator: Locator, property: string): Promise<string> {
  return locator.first().evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    context.fillStyle = "#000";
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
    return `rgb(${r}, ${g}, ${b})`;
  }, property);
}

async function open(page: Page, path: string, dark: boolean) {
  await page.addInitScript(
    ({ key, mode }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem(key, mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    { key: THEME_KEY, mode: dark ? "dark" : "light" },
  );
  await page.goto(path);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  // The page fades in on load; measure it at rest.
  await page.waitForTimeout(900);
}

const heading = (page: Page) => page.getByRole("heading", { level: 1 });
/** The round or rounded mark above the title: the page's first decorative span. */
const mark = (page: Page) => page.locator("main span[aria-hidden=true]");

test.beforeEach(() => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
});

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test.describe(mode, () => {
    test.beforeEach(() => {
      COLOUR = dark ? PALETTE.dark : PALETTE.light;
    });

    test.describe("1440", () => {
      test.use({ viewport: { width: 1440, height: 900 } });

      test(`loi-moi: a board invitation, ${mode}`, async ({ page }) => {
        await open(page, "/invite/invite-erp-guest", dark);
        const above = -1.98;
        const below = 1.98;
        await expectBox(
          "avatar",
          mark(page),
          { x: 836.8, y: 55.1, width: 32, height: 32 },
          above,
        );
        await expectBox(
          "h1",
          heading(page),
          { x: 836.8, y: 99.1, width: 400, height: 95.06 },
          above,
        );
        expect(await style(heading(page), "font-size")).toBe("24px");
        expect(await style(heading(page), "font-weight")).toBe("650");
        expect(await colour(heading(page), "color")).toBe(COLOUR.ink);
        await expectBox(
          "level",
          page.locator("main span", { hasText: /^Khách ·/ }),
          { x: 836.8, y: 202.2, height: 28 },
          below,
        );
        const see = page.locator("section[aria-labelledby=auth-see]");
        await expectBox(
          "see",
          see,
          { x: 836.8, y: 248.2, width: 400, height: 96.8 },
          below,
        );
        await expectBox(
          "see title",
          see.locator("h2"),
          { x: 853.8, y: 263.2, width: 366, height: 20.3 },
          below,
        );
        await expectBox(
          "see body",
          see.locator("p"),
          { x: 853.8, y: 289.4, width: 366, height: 40.5 },
          below,
        );
        expect(await colour(see, "border-top-color")).toBe(COLOUR.see);
        await expectBox(
          "email label",
          page.locator("label[for=auth-invite-email]"),
          { y: 362.9, width: 400, height: 20.3 },
          below,
        );
        await expectBox(
          "email",
          page.locator("#auth-invite-email"),
          { y: 389.2, width: 400, height: 44 },
          below,
        );
        const emailHint = page.locator("#auth-invite-email + p");
        await expectBox(
          "email hint",
          emailHint,
          { y: 439.2, width: 400, height: 18.8 },
          below,
        );
        expect(await colour(emailHint, "color")).toBe(COLOUR.hint);
        await expectBox(
          "name label",
          page.locator("label[for=auth-invite-name]"),
          { y: 475.9, width: 400, height: 20.3 },
          below,
        );
        await expectBox(
          "name",
          page.locator("#auth-invite-name"),
          { y: 502.2, width: 400, height: 44 },
          below,
        );
        await expectBox(
          "password label",
          page.locator("label[for=auth-invite-password]"),
          { y: 564.2, width: 400, height: 20.3 },
          below,
        );
        await expectBox(
          "password",
          page.locator("#auth-invite-password"),
          { y: 590.4, width: 400, height: 44 },
          below,
        );
        await expectBox(
          "eye",
          page.getByRole("button", { name: "Hiện mật khẩu" }),
          { x: 1196.8, y: 594.4, width: 36, height: 36 },
          below,
        );
        await expectBox(
          "password hint",
          page.locator("#auth-invite-password-hint"),
          { y: 640.4, width: 400, height: 18.8 },
          below,
        );
        const accept = page.getByRole("button", {
          name: "Chấp nhận và tạo tài khoản",
        });
        await expectBox(
          "accept",
          accept,
          { x: 836.8, y: 677.2, width: 298.8, height: 44 },
          below,
        );
        expect(await colour(accept, "background-color")).toBe(COLOUR.accent);
        const decline = page.getByRole("button", {
          name: "Từ chối",
          exact: true,
        });
        await expectBox(
          "decline",
          decline,
          { x: 1145.6, y: 677.2, width: 91.2, height: 44 },
          below,
        );
        expect(await colour(decline, "color")).toBe(COLOUR.ink);
        const safe = page.locator("main form + p");
        await expectBox(
          "safe",
          safe,
          { x: 836.8, y: 739.2, width: 400, height: 37.5 },
          below,
        );
        expect(await colour(safe, "color")).toBe(COLOUR.hint);
        await expectBox(
          "has account",
          page.locator("main form + p + p"),
          { y: 794.7, width: 400, height: 20.3 },
          below,
        );
        const signIn = page.getByRole("button", {
          name: "Đăng nhập để chấp nhận",
        });
        await expectBox(
          "sign in",
          signIn,
          { x: 1038.8, y: 795.7, width: 165.9, height: 18 },
          below,
        );
        expect(await colour(signIn, "color")).toBe(COLOUR.link);
      });

      test(`het-han: the expired page, ${mode}`, async ({ page }) => {
        await open(page, "/invite/invite-erp-expired", dark);
        const icon = mark(page);
        await expectBox("icon", icon, {
          x: 836.8,
          y: 317.8,
          width: 52,
          height: 52,
        });
        expect(await style(icon, "border-radius")).toBe("14px");
        expect(await colour(icon, "background-color")).toBe(COLOUR.icon);
        expect(await colour(icon, "color")).toBe(COLOUR.calm);
        // The artboard's hourglass, not a clock face.
        await expect(icon.locator("svg")).toHaveClass(/lucide-hourglass/);
        await expectBox("h1", heading(page), {
          x: 836.8,
          y: 387.8,
          width: 400,
          height: 35,
        });
        expect(await style(heading(page), "font-size")).toBe("28px");
        expect(await colour(heading(page), "color")).toBe(COLOUR.ink);
        const lead = page.locator("main h1 + p");
        await expectBox("lead", lead, { y: 428.8, width: 400, height: 21.8 });
        expect(await style(lead, "font-size")).toBe("14.5px");
        expect(await colour(lead, "color")).toBe(COLOUR.calm);
        const help = page.locator("main h1 ~ p, main div + p").last();
        await expectBox("help", help, { y: 468.5, width: 400, height: 21.8 });
        expect(await colour(help, "color")).toBe(COLOUR.calm);
        const back = page.getByRole("button", { name: "Về trang đăng nhập" });
        await expectBox("back", back, {
          x: 836.8,
          y: 508.3,
          width: 400,
          height: 44,
        });
        expect(await style(back, "border-radius")).toBe("9px");
        expect(await colour(back, "color")).toBe(COLOUR.link);
      });
    });

    test.describe("390", () => {
      test.use({ viewport: { width: 390, height: 844 } });

      test(`loi-moi: the invitation card, ${mode}`, async ({ page }) => {
        await open(page, "/invite/invite-erp-guest", dark);
        // The brand mark of the top bar: 28 px at x 18, the 17 px name.
        const brand = page.locator("main > div").first();
        await expectBox("mark", brand.locator("svg"), {
          x: 18,
          width: 28,
          height: 28,
        });
        const name = brand.locator("b");
        await expectBox("name", name, { x: 54, width: 67.3, height: 24.65 });
        expect(await style(name, "font-size")).toBe("17px");
        expect(await style(name, "font-weight")).toBe("700");
        expect(await colour(name, "color")).toBe(COLOUR.ink);

        const avatar = mark(page);
        const dy = (await avatar.first().boundingBox())!.y - 113;
        await expectBox(
          "avatar",
          avatar,
          { x: 20, y: 113, width: 48, height: 48 },
          dy,
        );
        await expectBox(
          "h1",
          heading(page),
          { x: 20, y: 179, width: 350, height: 85.8 },
          dy,
        );
        expect(await style(heading(page), "font-size")).toBe("22px");
        expect(await style(heading(page), "font-weight")).toBe("700");
        expect(await colour(heading(page), "color")).toBe(COLOUR.ink);
        const card = heading(page).locator("xpath=following-sibling::div[1]");
        await expectBox(
          "card",
          card,
          { x: 20, y: 282.8, width: 350, height: 244.1 },
          dy,
        );
        expect(await style(card, "border-radius")).toBe("14px");
        expect(await colour(card, "border-top-color")).toBe(COLOUR.line);
        expect(await colour(card, "background-color")).toBe(COLOUR.panel);
        const rows = card.locator("> div");
        await expectBox(
          "card head",
          rows.nth(0),
          { x: 21, y: 283.8, width: 348, height: 70 },
          dy,
        );
        await expectBox(
          "card access",
          rows.nth(1),
          { x: 21, y: 353.8, width: 348, height: 84.8 },
          dy,
        );
        await expectBox(
          "card scope",
          rows.nth(2),
          { x: 21, y: 438.6, width: 348, height: 87.3 },
          dy,
        );
        await expectBox(
          "level",
          rows.nth(1).locator("span span"),
          { x: 69, y: 394.6, height: 32 },
          dy,
        );
        const sent = card.locator("xpath=following-sibling::p[1]");
        await expectBox(
          "sent to",
          sent,
          { x: 20, y: 542.9, width: 350, height: 39 },
          dy,
        );
        expect(await style(sent, "font-size")).toBe("13px");
        expect(await colour(sent, "color")).toBe(COLOUR.hint);
        // No form: the name and the password come after Chấp nhận.
        await expect(page.locator("#auth-invite-name")).toHaveCount(0);
        const decline = page.getByRole("button", {
          name: "Từ chối",
          exact: true,
        });
        const accept = page.getByRole("button", {
          name: "Chấp nhận",
          exact: true,
        });
        await expectBox("decline", decline, { x: 20, width: 170, height: 50 });
        await expectBox("accept", accept, { x: 200, width: 170, height: 50 });
        expect(await colour(accept, "background-color")).toBe(COLOUR.accent);
        expect(await style(accept, "border-radius")).toBe("12px");
        await accept.click();
        await expect(page.locator("#auth-invite-name")).toBeFocused();
        await expect(page.locator("#auth-invite-password")).toBeVisible();
      });
    });
  });
}
