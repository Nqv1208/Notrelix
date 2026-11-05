import { expect, test, type Page } from "@playwright/test";

/**
 * The member fill page in the states of the design canvas artboard WM-Forms-Submit-States: the closed,
 * not-open-yet and not-found cards at 1440 px, and the 320 px column (default, closed). Numbers were read
 * from the artboard rendered with docs/design/mockups/2026-10/render.py. Tolerance is 1 px for boxes and
 * exact for font sizes.
 *
 * Intended differences:
 *  - the artboard frames a 434.7 px card, so its text wraps to two lines; the page card is as wide as the
 *    page column, so only offsets above the text, font sizes and the button box are compared;
 *  - the artboard's 320 frame is 320 px with a 1 px border and 14 px padding (content 290 px wide at x 15),
 *    which a 320 px viewport reproduces with a 15 px gutter;
 *  - the mock world's form questions differ from the artboard's three, so the 320 form is compared by its
 *    boxes (input 44 px, text area 70 px, full-width 44 px send button), not by their order.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.beforeEach(() => {
  test.skip(
    scenario !== "default" || persona !== "owner" || faults !== "",
    "Assertions need the default owner world without faults",
  );
});

async function open(page: Page, path: string, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(path, { waitUntil: "domcontentloaded" });
}

function near(actual: number, want: number, label: string) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(1);
}

async function box(page: Page, selector: string) {
  const b = await page.locator(selector).first().boundingBox();
  if (!b) throw new Error(`no box for ${selector}`);
  return b;
}

const CARDS = [
  { name: "closed", form: "mock-form-erp-bug", button: "Về board" },
  { name: "draft", form: "mock-form-purchase", button: "Về board" },
  { name: "not found", form: "mock-form-nope", button: "Về trang chủ" },
] as const;

for (const dark of [false, true]) {
  for (const card of CARDS) {
    test(`fill ${card.name} card, 1440 px, ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      await open(page, `${BOARD}/forms/${card.form}`, 1440, dark);
      const section = page.locator("main section").first();
      await expect(section.locator("h1")).toBeVisible();
      const s = await section.boundingBox();
      const h = await box(page, "main section h1");
      const p = await box(page, "main section p");
      const b = await section
        .getByRole("button", { name: card.button })
        .boundingBox();
      if (!s || !b) throw new Error("no box");
      // Artboard: card padding 24 + border 1; heading 56.3 down, text 89.5 down, 10 px gaps.
      near(h.x - s.x, 25, "heading x");
      near(h.y - s.y, 56.3, "heading y");
      near(h.height, 23.2, "heading height");
      near(p.y - s.y, 89.5, "text y");
      near(b.height, 32, "button height");
      near(b.x - s.x, 25, "button x");
      near(s.y + s.height - (b.y + b.height), 25, "bottom padding");
      expect(
        await section
          .locator("h1")
          .evaluate((e) => getComputedStyle(e).fontSize),
      ).toBe("16px");
      expect(
        await section
          .locator("p")
          .evaluate((e) => getComputedStyle(e).fontSize),
      ).toBe("13px");
    });
  }
}

for (const dark of [false, true]) {
  test(`fill 320 px column, ${dark ? "dark" : "light"}`, async ({ page }) => {
    await open(page, `${BOARD}/forms/mock-form-it-support`, 320, dark);
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    const h = await box(page, "main form h1");
    near(h.x, 15, "title x");
    near(h.width, 290, "column width");
    expect(
      await page
        .locator("main form h1")
        .evaluate((e) => getComputedStyle(e).fontSize),
    ).toBe("16px");
    for (const input of await page
      .locator("main form input[type=text], main form input:not([type])")
      .all()) {
      near((await input.boundingBox())!.height, 44, "input height");
    }
    near(
      (await box(page, "main form textarea")).height,
      70,
      "text area height",
    );
    const send = await page
      .getByRole("button", { name: "Gửi yêu cầu" })
      .boundingBox();
    near(send!.height, 44, "send height");
    near(send!.width, 290, "send width");
    near(send!.x, 15, "send x");
  });

  test(`fill closed 320 px column, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    await open(page, `${BOARD}/forms/mock-form-erp-bug`, 320, dark);
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    const h = await box(page, "main section h1");
    const back = await page
      .locator("main section")
      .getByRole("button", { name: "Về board" })
      .boundingBox();
    near(h.x, 15, "heading x");
    near(h.height, 23.2, "heading height");
    near(back!.x, 15, "button x");
    near(back!.height, 44, "button height");
    // No card chrome: the section has no border at 320 px.
    expect(
      await page
        .locator("main section")
        .first()
        .evaluate((e) => getComputedStyle(e).borderTopWidth),
    ).toBe("0px");
  });
}
