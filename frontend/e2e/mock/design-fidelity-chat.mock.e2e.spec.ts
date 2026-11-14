import { expect, test, type Page } from "@playwright/test";

/**
 * Chat at 1440 x 900 and 390 x 844 against the owner canvas boards `Chat-Main` (1440), `Chat-List-M` and
 * `Chat-Main-M` (390, scenes list and room). Numbers were read from the boards rendered in a browser at
 * the board's own width (the .dc.html rendered with docs/design/mockups/2026-10/render.py's method, `mode=sang`,
 * `scn=list|room` for the phone boards); tolerance 1 px. Only the Chat module is measured: the app shell, the
 * system rail and the workspace sidebar around it follow the Final-* boards and are not compared.
 *
 * Board facts asserted: chat sidebar rows 32 high, 8 radius, 10 side padding, 9 gap (48 high full-width rows on a
 * phone); group heading 11.5/650; header 64 (56 on a phone) with an 18 (17) title; bubbles 8 12 padding, 1.45 line
 * height. Where a board still contradicts the design lead's decision (one set of values at every width, ahead of
 * the canvas update) the decision wins: bubble radius 14 with a 6 px tail only on a group's last bubble; reaction
 * pills 26 high in normal flow 8 px under the bubble, the avatar on the bubble's bottom edge; date separator and
 * "Mới" divider 24 high with a line each side; composer a 52 high rounded single-row field, 40 px buttons, mic
 * while empty and a send icon button once typed, no "Gửi" text.
 *
 * Not compared: text colours (the app has no board "meta" ink, its muted ink is the board's "muted"), the right
 * panel, the pinned banner, the DM rows with a preview, the own-bubble @mention chip (the mock does not return
 * mentions for a sent message; its classes are pinned in the component test), the phone bottom tab bar and the
 * 72 px board rail (the shell is shared and out of scope).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const CHAT = "/workspaces/mock-workspace-primary/chat";
const CHANNEL = `${CHAT}/c1000000-0000-4000-8000-000000000001`;

async function open(page: Page, width: number, height: number, path: string) {
  await page.setViewportSize({ width, height });
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", "light");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(path);
  await page.waitForTimeout(1500);
}

type Box = { x: number; y: number; w: number; h: number };
async function box(page: Page, selector: string): Promise<Box> {
  const b = await page.locator(selector).first().boundingBox();
  if (!b) throw new Error(`no box for ${selector}`);
  return { x: b.x, y: b.y, w: b.width, h: b.height };
}
async function css(page: Page, selector: string, prop: string) {
  return page
    .locator(selector)
    .first()
    .evaluate((el, p) => getComputedStyle(el).getPropertyValue(p), prop);
}

async function react(page: Page) {
  const row = page.locator("[data-own=false]").last();
  await row.scrollIntoViewIfNeeded();
  await row.hover();
  await row.getByRole("button", { name: "Biểu cảm" }).click();
  await page
    .locator("[role=group] button, [role=menu] button, [role=dialog] button")
    .filter({ hasText: /^[^\w\s]{1,4}$/ })
    .first()
    .click();
  await page.mouse.move(5, 5);
  await expect(page.getByTestId("chat-reactions").first()).toBeVisible();
}

test.describe("1440 (Chat-Main)", () => {
  test.beforeEach(async ({ page }) => {
    await open(page, 1440, 900, CHANNEL);
  });

  test("sidebar rows, group heading and title follow the board", async ({
    page,
  }) => {
    const row = await box(page, "li button[aria-current=page]");
    expect(row.h).toBeCloseTo(32, 0);
    expect(
      await css(page, "li button[aria-current=page]", "border-radius"),
    ).toBe("8px");
    expect(
      await css(page, "li button[aria-current=page]", "padding-left"),
    ).toBe("10px");
    expect(await css(page, "li button[aria-current=page]", "column-gap")).toBe(
      "9px",
    );
    const next = await box(
      page,
      "li:has(button[aria-current=page]) + li button",
    );
    expect(next.y - row.y).toBeCloseTo(33, 0);
    const title = page.locator("h2", { hasText: /^Kênh$/ });
    expect(await title.evaluate((e) => getComputedStyle(e).fontSize)).toBe(
      "15px",
    );
    const heading = page.locator("nav h3").first();
    expect(await heading.evaluate((e) => getComputedStyle(e).fontSize)).toBe(
      "11.5px",
    );
    expect(await heading.evaluate((e) => getComputedStyle(e).paddingLeft)).toBe(
      "10px",
    );
    expect(await css(page, "nav div[aria-hidden=true]", "border-radius")).toBe(
      "10px",
    );
  });

  test("no account footer, no Ctrl K chip, no Enter hint", async ({ page }) => {
    const nav = page.getByRole("navigation", { name: /^(Channels|Kênh)$/ });
    await expect(nav.getByText(/Ctrl|⌘/)).toHaveCount(0);
    await expect(page.getByText(/Enter để gửi|Shift\+Enter/)).toHaveCount(0);
    const decorated = await page
      .locator("nav a, nav button")
      .evaluateAll(
        (els) =>
          els.filter((e) => getComputedStyle(e).textDecorationLine !== "none")
            .length,
      );
    expect(decorated).toBe(0);
    const send = page.locator("textarea").first();
    await send.fill("a");
    const btn = page.getByRole("button", { name: /^(Send|Gửi)$/ });
    await expect(btn).toHaveAttribute("title", /Enter/);
    await expect(send).toHaveAttribute("aria-keyshortcuts", /Enter/);
  });

  test("header is 64 high with an 18/650 title", async ({ page }) => {
    const h = await box(page, "main header");
    expect(h.h).toBeCloseTo(64, 0);
    expect(await css(page, "main header h1", "font-size")).toBe("18px");
    expect(await css(page, "main header h1", "font-weight")).toBe("650");
  });

  test("bubbles, date separator and Mới divider", async ({ page }) => {
    const bubble = "[data-bubble=other]";
    expect(await css(page, bubble, "padding")).toBe("8px 12px");
    expect(await css(page, bubble, "border-top-left-radius")).toBe("14px");
    expect(
      Math.abs(parseFloat(await css(page, bubble, "line-height")) - 14 * 1.45),
    ).toBeLessThan(1);
    const col = await box(page, "[role=log] ol");
    expect(col.w).toBeLessThanOrEqual(760);
    const nd = await box(page, "[data-unread-divider]");
    expect(nd.h).toBeCloseTo(24, 0);
    expect(nd.x + nd.w / 2).toBeCloseTo(col.x + col.w / 2, 0);
    const ds = await box(page, "[role=log] li[role=presentation]:has(span)");
    expect(ds.h).toBeCloseTo(24, 0);
  });

  test("a reaction pill is 26 high, 8 px under the bubble, avatar on the bubble edge", async ({
    page,
  }) => {
    await react(page);
    const row = "[data-message-id]:has([data-testid=chat-reactions])";
    const bub = await box(page, `${row} [data-bubble]`);
    const pill = await box(page, `${row} [data-testid=chat-reactions] button`);
    const av = await box(page, `${row} [data-avatar-slot] span`);
    expect(pill.h).toBeCloseTo(26, 0);
    expect(pill.y - (bub.y + bub.h)).toBeCloseTo(8, 0);
    expect(av.y + av.h).toBeCloseTo(bub.y + bub.h, 0);
    expect(
      await css(
        page,
        `${row} [data-testid=chat-reactions] button`,
        "border-top-width",
      ),
    ).toBe("1px");
  });

  test("composer is a 52 high pill with 40 px buttons", async ({ page }) => {
    const form = await box(page, "form:has(textarea)");
    expect(form.h).toBeCloseTo(52, 0);
    expect(await css(page, "form:has(textarea)", "border-radius")).toBe("26px");
    const b = await box(page, "form:has(textarea) button");
    expect(b.w).toBeCloseTo(40, 0);
    expect(b.h).toBeCloseTo(40, 0);
    expect(900 - (form.y + form.h)).toBeCloseTo(4, 0);
  });
});

test.describe("390 (Chat-List-M, Chat-Main-M)", () => {
  test("the bare route is the channel list: 56 top, 48 rows, no jump field", async ({
    page,
  }) => {
    await open(page, 390, 844, CHAT);
    await expect(page).toHaveURL(/\/chat$/);
    const top = await box(page, "nav h2 >> xpath=..");
    expect(top.h).toBeCloseTo(56, 0);
    expect(await css(page, "nav h2", "font-size")).toBe("17px");
    const row = await box(page, "li button");
    expect(row.w).toBeCloseTo(390, 0);
    expect(row.h).toBeGreaterThanOrEqual(47.5);
    expect(await css(page, "li button", "border-radius")).toBe("0px");
    expect(await css(page, "li button", "padding-left")).toBe("16px");
    await expect(page.locator("nav div[aria-hidden=true]")).toBeHidden();
  });

  test("a pushed room has a back arrow, 56 header, 26 pills 8 px under the bubble, the same pill composer", async ({
    page,
  }) => {
    await open(page, 390, 844, CHANNEL);
    await expect(
      page.getByRole("button", { name: /Quay lại danh sách kênh|Back to/ }),
    ).toBeVisible();
    expect((await box(page, "main header")).h).toBeCloseTo(56, 0);
    expect(await css(page, "main header h1", "font-size")).toBe("17px");
    await react(page);
    const row = "[data-message-id]:has([data-testid=chat-reactions])";
    const bub = await box(page, `${row} [data-bubble]`);
    const pill = await box(page, `${row} [data-testid=chat-reactions] button`);
    expect(pill.h).toBeCloseTo(26, 0);
    expect(pill.y - (bub.y + bub.h)).toBeCloseTo(8, 0);
    const av = await box(page, `${row} [data-avatar-slot] span`);
    expect(av.x).toBeCloseTo(14, 0);
    expect(av.y + av.h).toBeCloseTo(bub.y + bub.h, 0);
    const form = await box(page, "form:has(textarea)");
    expect(form.h).toBeCloseTo(52, 0);
    expect(await css(page, "form:has(textarea)", "border-radius")).toBe("26px");
    await expect(
      page.getByRole("button", { name: /^(Send|Gửi)$/ }),
    ).toHaveCount(0);
    const nd = await box(page, "[data-unread-divider]");
    expect(nd.h).toBeCloseTo(24, 0);
  });
});

/**
 * Side panels against Chat-Pins (1440), Chat-Pins-M, Chat-Thread-M and Chat-Search-M (v41 boards rendered at their own
 * width, tolerance 1 px). The thread keeps the bubbles of Chat-Main-T's thread (Chat-Thread-T and -M draw flat messages: a
 * board inconsistency) and the shared pill composer; the 1440 Chat-Search board is a command palette with kind tabs and a
 * preview pane, which the app does not have: only the field, chips and result rows are compared, on the phone.
 */
async function pinThree(page: Page) {
  for (let i = 0; i < 3; i++) {
    const row = page.locator("[data-own=false]").nth(-1 - i * 2);
    await row.scrollIntoViewIfNeeded();
    await row.hover();
    await row
      .getByRole("button", { name: "Thao tác khác" })
      .click({ force: true });
    await page.getByRole("menuitem", { name: /Ghim tin nhắn/ }).click();
    await page.waitForTimeout(400);
  }
}

async function openThread(page: Page) {
  const row = page.locator("[data-own=false]").last();
  await row.scrollIntoViewIfNeeded();
  await row.hover();
  await row.getByRole("button", { name: "Thao tác khác" }).click();
  await page.getByRole("menuitem", { name: /Trả lời trong luồng/ }).click();
  const box = page.getByTestId("chat-thread-panel").getByRole("combobox");
  await box.fill("Phản hồi đầu tiên trong luồng");
  await box.press("Enter");
  await expect(page.getByTestId("chat-thread-separator")).toBeVisible();
}

const HEADER = "[data-testid=chat-panel-header]";
const PIN = "[data-testid=chat-pin-item]";

test.describe("Pins panel (Chat-Pins 1440)", () => {
  test("88 header, 16 title, summary strip, 12 16 rows with an 18 avatar", async ({
    page,
  }) => {
    await open(page, 1440, 900, CHANNEL);
    await pinThree(page);
    await page.getByRole("button", { name: "Ghim" }).first().click();
    await expect(page.locator(PIN)).toHaveCount(3);
    expect((await box(page, "[data-testid=chat-side-panel]")).w).toBeCloseTo(
      360,
      0,
    );
    expect((await box(page, HEADER)).h).toBeCloseTo(88, 0);
    expect(await css(page, `${HEADER} h2`, "font-size")).toBe("16px");
    expect(await css(page, `${HEADER} h2`, "font-weight")).toBe("700");
    const strip = page.locator("[data-testid=chat-pins-panel] > p").first();
    expect(await strip.evaluate((e) => getComputedStyle(e).fontSize)).toBe(
      "13px",
    );
    expect(await css(page, PIN, "padding")).toBe("12px 16px");
    const av = await box(page, `${PIN} [data-slot=avatar]`);
    expect(av.w).toBeCloseTo(18, 0);
    expect(await css(page, `${PIN} span.font-bold`, "font-size")).toBe("13px");
    expect(await css(page, `${PIN} p.line-clamp-3`, "line-height")).toBe(
      "21px",
    );
    expect((await box(page, `${PIN} button`)).h).toBeCloseTo(28, 0);
    await expect(page.getByRole("tablist")).toBeHidden();
  });
});

test.describe("390 side panels (Chat-Pins-M, Chat-Thread-M, Chat-Search-M)", () => {
  test("pins: a 56 page header with a back arrow, 17/650 title, 12 px line, no time in the footer", async ({
    page,
  }) => {
    await open(page, 390, 844, CHANNEL);
    await pinThree(page);
    await page.getByRole("button", { name: "Ghim" }).first().click();
    await expect(page.locator(PIN)).toHaveCount(3);
    const head = await box(page, HEADER);
    expect(head.h).toBeCloseTo(56, 0);
    const back = await box(page, `${HEADER} button`);
    expect(back.x).toBeCloseTo(12, 0);
    expect(back.w).toBeCloseTo(32, 0);
    expect(await css(page, `${HEADER} h2`, "font-size")).toBe("17px");
    expect(await css(page, `${HEADER} h2`, "font-weight")).toBe("650");
    expect(await css(page, `${HEADER} p`, "font-size")).toBe("12px");
    expect(await css(page, PIN, "padding")).toBe("12px 16px");
    await expect(
      page.locator("[data-testid=chat-pins-panel] > p"),
    ).toBeHidden();
  });

  test("thread: a 56 page header with the follow chip, a 24 high lined reply separator, the pill composer", async ({
    page,
  }) => {
    await open(page, 390, 844, CHANNEL);
    await openThread(page);
    expect((await box(page, HEADER)).h).toBeCloseTo(56, 0);
    expect(await css(page, `${HEADER} h2`, "font-size")).toBe("17px");
    const chip = await box(page, `${HEADER} button[aria-pressed]`);
    expect(chip.h).toBeCloseTo(22, 0);
    expect(
      parseFloat(
        await css(page, `${HEADER} button[aria-pressed]`, "border-radius"),
      ),
    ).toBeGreaterThanOrEqual(11);
    expect(await css(page, `${HEADER} button[aria-pressed]`, "font-size")).toBe(
      "12px",
    );
    const sep = await box(page, "[data-testid=chat-thread-separator]");
    expect(sep.h).toBeCloseTo(24, 0);
    expect(
      await css(page, "[data-testid=chat-thread-separator]", "padding"),
    ).toBe("0px 24px");
    const form = "[data-testid=chat-thread-panel] form:has(textarea)";
    expect((await box(page, form)).h).toBeCloseTo(52, 0);
    expect(await css(page, form, "border-radius")).toBe("26px");
    await expect(page.getByRole("tablist")).toBeHidden();
  });

  test("search: a full-screen page, 56 header, 44 field radius 12, 28 filter chips, rows 12 16 with an 18 avatar", async ({
    page,
  }) => {
    await open(page, 390, 844, CHANNEL);
    await page.getByRole("button", { name: "Tìm tin nhắn" }).last().click();
    await page.keyboard.type("Tin nhắn");
    const dialog = "[data-slot=dialog-content]";
    await expect(page.locator("[role=option]").first()).toBeVisible();
    const d = await box(page, dialog);
    expect(d.w).toBeCloseTo(390, 0);
    expect(d.h).toBeCloseTo(844, 0);
    expect((await box(page, `${dialog} > div:first-child`)).h).toBeCloseTo(
      56,
      0,
    );
    expect(await css(page, `${dialog} h2`, "font-size")).toBe("17px");
    const field = `${dialog} div.h-11`;
    expect((await box(page, field)).h).toBeCloseTo(44, 0);
    expect(await css(page, field, "border-radius")).toBe("12px");
    expect((await box(page, `${dialog} select`)).h).toBeCloseTo(28, 0);
    expect(
      parseFloat(await css(page, `${dialog} select`, "border-radius")),
    ).toBeGreaterThanOrEqual(14);
    const row = "[role=option]";
    expect(await css(page, row, "padding")).toBe("12px 16px");
    expect((await box(page, `${row} [data-slot=avatar]`)).w).toBeCloseTo(18, 0);
    expect(await css(page, `${row} span.font-bold`, "font-size")).toBe("13px");
    expect(await css(page, `${row} p`, "line-height")).toBe("21px");
    expect(await css(page, `${row} mark`, "border-radius")).toBe("3px");
  });
});
