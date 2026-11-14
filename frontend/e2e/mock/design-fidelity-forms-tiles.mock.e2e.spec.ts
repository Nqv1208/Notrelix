import { expect, test, type Page } from "@playwright/test";

/**
 * The tiles of the design canvas artboard WM-Forms-Submit-States that need a state the plain mock world
 * does not show, plus the sent card of WM-Forms-Submit and the 320 px sent and validation frames. Numbers
 * were read from the artboards rendered with docs/design/mockups/2026-10/render.py; tolerance is 1 px.
 *
 * Run once per world:
 *  - plain: sent card at 1440 and 320, validation summary at 320;
 *  - `VITE_MOCK_LATENCY=slow`: loading tile;
 *  - `VITE_MOCK_FAULTS="GET /forms/mock-form-it-support=500"`: load error tile;
 *  - `VITE_MOCK_FAULTS="POST /submissions=network"`: network error tile;
 *  - `VITE_MOCK_FAULTS="POST /submissions=409"`: closed-midway tile.
 *
 * Intended differences: the tiles are 434.7 px cards, the page card is 620 px wide, so only offsets that do
 * not depend on the width are compared (text wrap, card width); the network tile's lead sentence above the
 * alert is the tile's own caption.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const FORM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/forms/mock-form-it-support";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, width: number, dark: boolean) {
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
  await page.goto(FORM, { waitUntil: "domcontentloaded" });
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

const plain =
  persona === "owner" &&
  scenario === "default" &&
  faults === "" &&
  latency === "instant";

async function fillRequired(page: Page) {
  await page
    .getByRole("textbox", { name: /^Bạn cần gì/ })
    .fill("Làm mới tên máy");
  await page
    .getByRole("combobox", { name: /^Mức độ ưu tiên/ })
    .selectOption({ index: 1 });
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test.describe(`plain world, ${mode}`, () => {
    test.skip(!plain, "Needs the plain owner world");

    test(`sent card, 1440 px, ${mode}`, async ({ page }) => {
      await open(page, 1440, dark);
      await fillRequired(page);
      await page.getByRole("button", { name: "Gửi yêu cầu" }).click();
      const card = page.locator("main section[role=status]");
      await expect(card).toBeVisible();
      await page.waitForTimeout(250);
      const c = (await card.boundingBox())!;
      // WM-Forms-Submit: card 620 wide, 32 padding + 1 border; icon 40; title 22 px at 87; text 14 below; buttons 32.
      near(c.width, 620, "card width");
      const h1 = (await card.locator("h1").boundingBox())!;
      near(h1.x - c.x, 33, "title x");
      near(h1.y - c.y, 87, "title y");
      near(h1.height, 28.6, "title height");
      const p = (await card.locator("p").boundingBox())!;
      near(p.y - c.y, 129.6, "text y");
      const another = (await card
        .getByRole("button", { name: "Gửi yêu cầu khác" })
        .boundingBox())!;
      near(another.height, 32, "button height");
      near(another.width, 135.8, "button width");
      near(another.y - (p.y + p.height), 14, "text to buttons");
      const back = (await card
        .getByRole("button", { name: "Về board" })
        .boundingBox())!;
      near(back.width, 84.9, "back width");
      near(back.x - (another.x + another.width), 8, "button gap");
      expect(
        await card.locator("h1").evaluate((e) => getComputedStyle(e).fontSize),
      ).toBe("22px");
      const icon = (await card.locator("span").first().boundingBox())!;
      near(icon.width, 40, "icon width");
      near(icon.y - c.y, 33, "icon y");
    });

    test(`sent frame, 320 px, ${mode}`, async ({ page }) => {
      await open(page, 320, dark);
      await fillRequired(page);
      await page.getByRole("button", { name: "Gửi yêu cầu" }).click();
      const card = page.locator("main section[role=status]");
      await expect(card).toBeVisible();
      await page.waitForTimeout(250);
      // WM-Forms-Submit-States "320 · gửi xong": no card chrome; 40 px icon, 16 px title, 13 px text, 12 px gaps, full-width button.
      expect(
        await card.evaluate((e) => getComputedStyle(e).borderTopWidth),
      ).toBe("0px");
      const icon = (await card.locator("span").first().boundingBox())!;
      const h1 = (await card.locator("h1").boundingBox())!;
      const p = (await card.locator("p").boundingBox())!;
      const b = (await card
        .getByRole("button", { name: "Gửi yêu cầu khác" })
        .boundingBox())!;
      near(icon.x, 15, "icon x");
      near(icon.width, 40, "icon width");
      near(h1.y - (icon.y + icon.height), 12, "icon to title");
      near(h1.height, 23.2, "title height");
      near(p.y - (h1.y + h1.height), 12, "title to text");
      near(p.width, 290, "text width");
      near(b.y - (p.y + p.height), 12, "text to button");
      near(b.width, 290, "button width");
      near(b.height, 40, "button height");
      expect(
        await card.locator("h1").evaluate((e) => getComputedStyle(e).fontSize),
      ).toBe("16px");
      expect(
        await card.locator("p").evaluate((e) => getComputedStyle(e).fontSize),
      ).toBe("13px");
      await expect(card.getByRole("button", { name: "Về board" })).toBeHidden();
    });

    test(`validation summary frame, 320 px, ${mode}`, async ({ page }) => {
      await open(page, 320, dark);
      await page.getByRole("button", { name: "Gửi yêu cầu" }).click();
      const summary = page
        .getByRole("alert")
        .filter({ hasText: "Chưa gửi được" });
      await expect(summary).toBeVisible();
      await page.waitForTimeout(250);
      // WM-Forms-Submit-States "320 · lỗi xác thực": the summary sits under the text, 14 px gaps, 10 px padding, 12 px radius.
      const s = (await summary.boundingBox())!;
      const intro = (await page
        .locator("main form h1")
        .locator("xpath=..")
        .boundingBox())!;
      near(s.x, 15, "summary x");
      near(s.width, 290, "summary width");
      near(s.height, 59.7, "summary height");
      near(s.y - (intro.y + intro.height), 14, "intro to summary");
      expect(
        await summary.evaluate((e) => getComputedStyle(e).borderTopLeftRadius),
      ).toBe("12px");
      expect(
        await summary.evaluate((e) => getComputedStyle(e).paddingTop),
      ).toBe("10px");
      const first = (await page
        .getByRole("textbox", { name: /^Bạn cần gì/ })
        .locator("xpath=preceding-sibling::label")
        .boundingBox())!;
      near(first.y - (s.y + s.height), 14, "summary to first question");
      const message = (await page
        .getByText(/^Nhập (điều bạn cần|câu trả lời)/)
        .first()
        .boundingBox())!;
      const input = (await page
        .getByRole("textbox", { name: /^Bạn cần gì/ })
        .boundingBox())!;
      near(message.y - (input.y + input.height), 6, "message under box");
      near(input.height, 44, "input height");
    });
  });

  test(`loading tile, ${mode}`, async ({ page }) => {
    test.skip(
      latency !== "slow" || faults !== "",
      "Needs VITE_MOCK_LATENCY=slow",
    );
    await open(page, 1440, dark);
    const status = page.locator("main [role=status]").first();
    await expect(status).toBeVisible();
    const c = (await status.boundingBox())!;
    // Card 312 high, 24 padding + 1 border; rows 28, 16, 40, 40, 90 with 12 px gaps.
    near(c.height, 312, "card height");
    const rows = await status.locator("> *").all();
    expect(rows).toHaveLength(5);
    let y = 25;
    for (const [i, h] of [28, 16, 40, 40, 90].entries()) {
      const r = (await rows[i]!.boundingBox())!;
      near(r.y - c.y, y, `row ${i} y`);
      near(r.height, h, `row ${i} height`);
      near(r.x - c.x, 25, `row ${i} x`);
      y += h + 12;
    }
  });

  test(`load error tile, ${mode}`, async ({ page }) => {
    test.skip(
      !faults.includes("GET /forms/mock-form-it-support"),
      "Needs the load fault",
    );
    await open(page, 1440, dark);
    const alert = page.locator("main").getByRole("alert");
    await expect(alert).toBeVisible();
    const card = alert.locator("xpath=..");
    const c = (await card.boundingBox())!;
    const text = (await alert
      .getByText("Chưa tải được biểu mẫu")
      .boundingBox())!;
    const retry = (await alert
      .getByRole("button", { name: "Thử lại" })
      .boundingBox())!;
    // Tile "Tải lỗi": padding 24 + 1; 16 px icon then text at +24; 28 px small button 8 px below with a refresh icon.
    near(text.x - c.x, 49, "text x");
    near(text.y - c.y, 25, "text y");
    near(text.height, 18.1, "text height");
    near(retry.x - c.x, 25, "retry x");
    near(retry.y - c.y, 51.1, "retry y");
    near(retry.height, 28, "retry height");
    near(retry.width, 84.9, "retry width");
    near(c.y + c.height - (retry.y + retry.height), 25, "bottom padding");
    expect(
      await card.evaluate((e) => getComputedStyle(e).borderTopLeftRadius),
    ).toBe("12px");
  });

  test(`network error tile, ${mode}`, async ({ page }) => {
    test.skip(
      !faults.includes("POST /submissions=network"),
      "Needs the network fault",
    );
    await open(page, 1440, dark);
    await fillRequired(page);
    await page.getByRole("button", { name: "Gửi yêu cầu" }).click();
    const alert = page.locator("main form").getByRole("alert");
    await expect(alert).toBeVisible();
    const text = (await alert
      .getByText("Chưa gửi được yêu cầu")
      .boundingBox())!;
    const retry = (await alert
      .getByRole("button", { name: "Thử lại" })
      .boundingBox())!;
    const a = (await alert.boundingBox())!;
    // Tile "Gửi lỗi mạng": text 24 right of the icon, 18.1 high, the small button 8 px below it.
    near(text.x - a.x, 24, "text x");
    near(text.height, 18.1, "text height");
    near(retry.y - (text.y + text.height), 8, "retry gap");
    near(retry.height, 28, "retry height");
    near(retry.width, 84.9, "retry width");
    await expect(
      page.getByRole("textbox", { name: /^Bạn cần gì/ }),
    ).toHaveValue("Làm mới tên máy");
  });

  test(`closed midway tile, ${mode}`, async ({ page }) => {
    test.skip(
      !faults.includes("POST /submissions=409"),
      "Needs the conflict fault",
    );
    await open(page, 1440, dark);
    await fillRequired(page);
    await page.getByRole("button", { name: "Gửi yêu cầu" }).click();
    const alert = page
      .locator("main form")
      .getByRole("alert")
      .filter({ hasText: "Biểu mẫu vừa đóng" });
    await expect(alert).toBeVisible();
    const text = (await alert.getByText("Câu trả lời chưa gửi").boundingBox())!;
    const copy = (await alert
      .getByRole("button", { name: "Sao chép câu trả lời" })
      .boundingBox())!;
    const a = (await alert.boundingBox())!;
    // Tile "Đóng giữa lúc đang điền": text 24 right of the icon in the warning colour, copy button 10 px below, 28 high, 145.4 wide.
    near(text.x - a.x, 24, "text x");
    near(copy.y - (text.y + text.height), 10, "copy gap");
    near(copy.height, 28, "copy height");
    near(copy.width, 145.4, "copy width");
    expect((await text.constructor) === Object).toBe(true);
    await expect(
      page.getByRole("button", { name: "Gửi yêu cầu" }),
    ).toBeDisabled();
  });
}
