import { expect, test, type Page } from "@playwright/test";
import { toNewest } from "./support/chat-newest";

/**
 * The Chat channel view against the owner canvas `Chat-Main` (1440), `Chat-Main-T` (1024) and `Chat-Main-M` (390),
 * light and dark, tolerance 1 px. Board facts: the conversation column is centred and capped at 760 (20 px side
 * padding, so 720 of content) and grows to 1040 on a window of 1536 or more, where the canvas has no board but a
 * 2000 px window must not leave wide empty gutters; bubbles 8 12 padding, 12 px above a new group and 2 px inside
 * it; the day separator is 24 high; the header is 64 (56 on a phone) and carries the Pins and Following toggles so
 * no row floats under it; the composer is a 52 high pill as wide as the column content. The chat sidebar has no
 * account footer (the workspace sidebar owns the account). The message list shows no outline on the container
 * (focus-visible stays on the focusable items) and the motion is 150 ms on the composer, bubbles, action bar and
 * Send, with transform and opacity only for movement.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";

async function open(
  page: Page,
  width: number,
  height: number,
  mode: "light" | "dark",
) {
  await page.setViewportSize({ width, height });
  await page.addInitScript((m) => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
      window.localStorage.setItem("notrelix-theme-mode", m);
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode);
  await page.goto(CHANNEL);
  await expect(page.locator("[data-bubble]").first()).toBeVisible();
  await page.waitForTimeout(600);
}

async function box(page: Page, selector: string) {
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

const SIZES = [
  // The owner widened the column (and the composer with it): both use the room up to 1120 / 1080.
  { width: 2000, height: 1000, column: 1120, content: 1080, phone: false },
  { width: 1440, height: 900, column: 1120, content: 1080, phone: false },
  { width: 1024, height: 768, column: null, content: null, phone: false },
  { width: 390, height: 844, column: null, content: null, phone: true },
] as const;

for (const mode of ["light", "dark"] as const) {
  for (const size of SIZES) {
    test.describe(`${size.width} ${mode}`, () => {
      test.beforeEach(async ({ page }) => {
        await open(page, size.width, size.height, mode);
      });

      test("column, gutters, bubbles, separator and header follow the board", async ({
        page,
      }) => {
        const main = await box(page, "main");
        const col = await box(page, "[role=log] ol");
        const mainW = main.w;
        if (size.column !== null) {
          expect(col.w).toBeCloseTo(Math.min(size.column, mainW), 0);
        } else {
          expect(col.w).toBeLessThanOrEqual(1120 + 1);
        }
        // Centred: equal gutters on both sides of the column inside the log.
        const log = await box(page, "[role=log]");
        const left = col.x - log.x;
        const right = log.x + log.w - (col.x + col.w);
        expect(Math.abs(left - right)).toBeLessThanOrEqual(1);
        // A phone has 14 px gutters at most, never an empty margin.
        if (size.phone) expect(left).toBeLessThanOrEqual(1);

        const bubble = "[data-bubble=other]";
        expect(await css(page, bubble, "padding")).toBe("8px 12px");
        expect(await css(page, bubble, "border-top-left-radius")).toBe("14px");
        const rows = page.locator("[data-message-id]");
        const starts = await rows.evaluateAll((els) =>
          els.slice(0, 40).map((e) => ({
            top: getComputedStyle(e).marginTop,
            start: e.getAttribute("data-group-start") === "true",
          })),
        );
        for (const r of starts) {
          expect(r.top).toBe(r.start ? "12px" : "2px");
        }

        // Only the rows near the view are in the page: the date label that opens the window is drawn once the list is at its top.
        await page.getByRole("log").evaluate((el) => {
          el.scrollTop = 0;
        });
        await expect(
          page.locator("[role=log] li[data-testid=chat-day-separator]").first(),
        ).toBeVisible();
        const sep = await box(
          page,
          "[role=log] li[data-testid=chat-day-separator] > div",
        );
        expect(sep.h).toBeCloseTo(24, 0);

        const head = await box(page, "main header");
        expect(head.h).toBeCloseTo(size.phone ? 56 : 64, 0);
        // The Pins and Following toggles live in the header: no row of their own under it.
        const pins = page
          .locator("main header")
          .getByRole("button", { name: /^Pins$/ });
        await expect(pins).toHaveCount(1);
        await expect(
          page.locator("main header").getByRole("button", {
            name: /^Following$/,
          }),
        ).toHaveCount(1);
        const list = await box(page, "[data-testid=chat-message-list]");
        expect(list.y - (head.y + head.h)).toBeLessThanOrEqual(1);
      });

      test("composer is a 52 pill as wide as the column content and the chat sidebar has no account footer", async ({
        page,
      }) => {
        const form = await box(page, "form:has(textarea)");
        expect(form.h).toBeCloseTo(52, 0);
        const col = await box(page, "[role=log] ol");
        if (size.content !== null) {
          expect(form.w).toBeCloseTo(Math.min(size.content, col.w - 40), 0);
        }
        // On the Chat canvas the composer and the conversation share the column's centre line.
        expect(
          Math.abs(form.x + form.w / 2 - (col.x + col.w / 2)),
        ).toBeLessThan(1.5);
        if (size.phone) return;
        const nav = page.getByRole("navigation", { name: /^Channels$/ });
        await expect(nav).toBeVisible();
        await expect(nav.locator("footer")).toHaveCount(0);
        await expect(nav.locator("[data-slot=avatar]")).toHaveCount(0);
        await expect(nav.getByText(/Workspace owner|admin/i)).toHaveCount(0);
      });

      test("the list container shows no outline, focusable items keep focus-visible", async ({
        page,
      }) => {
        const list = page.locator("[data-testid=chat-message-list]");
        await list.focus();
        await page.keyboard.press("PageUp");
        const o = await list.evaluate((el) => {
          const s = getComputedStyle(el);
          return {
            style: s.outlineStyle,
            width: s.outlineWidth,
            shadow: s.boxShadow,
          };
        });
        expect(o.style === "none" || o.width === "0px").toBe(true);
        expect(o.shadow).toBe("none");
        // A keyboard user still sees focus on a real control.
        await page.keyboard.press("Tab");
        const ring = await page.evaluate(() => {
          const el = document.activeElement as HTMLElement | null;
          if (!el) return "";
          const s = getComputedStyle(el);
          return `${s.outlineStyle}|${s.boxShadow}`;
        });
        expect(ring).not.toBe("");
      });
    });
  }
}

test.describe("motion", () => {
  test.beforeEach(async ({ page }) => {
    await open(page, 1440, 900, "light");
  });

  test("composer, bubbles, action bar and Send carry 150 ms transitions on non-layout properties", async ({
    page,
  }) => {
    const form = "form:has(textarea)";
    expect(await css(page, form, "transition-duration")).toBe("0.15s");
    expect(await css(page, form, "transition-property")).toContain(
      "border-color",
    );
    expect(await css(page, form, "transition-property")).toContain(
      "box-shadow",
    );
    expect(await css(page, "[data-bubble=other]", "transition-duration")).toBe(
      "0.15s",
    );
    const bar = "[role=toolbar][data-visible]";
    expect(await css(page, bar, "transition-property")).toContain("opacity");
    expect(await css(page, bar, "transition-property")).toContain("transform");
    expect(await css(page, bar, "transition-duration")).toContain("0.15s");
    await page.locator("textarea").first().fill("hello");
    const send = "form:has(textarea) button[type=submit]";
    expect(await css(page, send, "transition-property")).toContain("opacity");
    expect(await css(page, send, "transition-property")).toContain("transform");
    for (const sel of [form, "[data-bubble=other]", bar, send]) {
      const props = await css(page, sel, "transition-property");
      for (const layout of ["width", "height", "margin", "padding", "top"]) {
        expect(props.split(",").map((p) => p.trim())).not.toContain(layout);
      }
    }
    // Focus eases the border and the ring on the composer.
    await page.locator("textarea").first().focus();
    await page.waitForTimeout(250);
    const ring = await css(page, form, "box-shadow");
    expect(ring).not.toBe("none");
  });

  test("the action bar fades in on hover and a sent message enters with a rise", async ({
    page,
  }) => {
    await toNewest(page);
    await page.mouse.move(5, 5);
    const row = page.locator("[data-own=false]").last();
    await row.scrollIntoViewIfNeeded();
    const bar = row.locator("[role=toolbar]").first();
    await expect(bar).toHaveCSS("opacity", "0");
    await row.hover();
    await expect(bar).toHaveCSS("opacity", "1");
    await page.mouse.move(5, 5);
    await expect(bar).toHaveCSS("opacity", "0");
    // A sent message enters with a base-duration (180 ms) fade and a small rise: the first frame of its list item carries the
    // animation (the whole item slides, so the rows next to it do not jump).
    await page.evaluate(() => {
      const seen: string[] = [];
      (window as unknown as { __enter: string[] }).__enter = seen;
      new MutationObserver((records) => {
        for (const r of records)
          for (const n of Array.from(r.addedNodes))
            if (n instanceof HTMLElement) {
              const el =
                n.matches("li") && n.querySelector("[data-own=true]")
                  ? n
                  : null;
              if (el instanceof HTMLElement)
                seen.push(
                  `${getComputedStyle(el).animationName}|${getComputedStyle(el).animationDuration}`,
                );
            }
      }).observe(document.body, { childList: true, subtree: true });
    });
    const box = page.locator("textarea").first();
    await box.fill("motion check message");
    await box.press("Enter");
    await expect(
      page
        .locator("[data-own=true]")
        .filter({ hasText: "motion check message" }),
    ).toBeVisible();
    const seen = await page.evaluate(
      () => (window as unknown as { __enter: string[] }).__enter,
    );
    expect(seen.some((v) => /enter/.test(v) && v.endsWith("|0.18s"))).toBe(
      true,
    );
  });

  test("reduced motion collapses every duration", async ({ page }) => {
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.reload();
    await expect(page.locator("[data-bubble]").first()).toBeVisible();
    const d = await css(page, "form:has(textarea)", "transition-duration");
    expect(parseFloat(d)).toBeLessThan(0.001);
  });
});

test.describe("jump to latest pill", () => {
  test("floats over the list with no layout space, 34 high, fades and rises in 160 ms, and glides to the end", async ({
    page,
  }) => {
    await open(page, 1440, 900, "light");
    const log = page.getByRole("log");
    const pill = page.getByRole("button", { name: "Jump to latest" });
    const layer = "[data-testid=chat-jump-latest-layer]";
    await log.evaluate((el) => {
      el.scrollTop = el.scrollHeight;
    });
    await expect(pill).toBeHidden();
    const hidden = await box(page, "[data-testid=chat-message-list]");
    await log.evaluate((el) => {
      el.scrollTop = 0;
    });
    await expect(pill).toBeVisible();
    // No layout space: the list keeps its box when the pill shows.
    const shown = await box(page, "[data-testid=chat-message-list]");
    expect(shown.h).toBeCloseTo(hidden.h, 0);
    expect(shown.y).toBeCloseTo(hidden.y, 0);
    expect(await css(page, layer, "position")).toBe("absolute");
    const p = await box(page, "button[data-visible=true]");
    expect(p.h).toBeCloseTo(34, 0);
    expect(p.y + p.h).toBeLessThanOrEqual(shown.y + shown.h);
    expect(p.x + p.w / 2).toBeCloseTo(shown.x + shown.w / 2, 0);
    const sel = "button[data-visible]";
    expect(await css(page, sel, "transition-duration")).toContain("0.16s");
    const props = (await css(page, sel, "transition-property"))
      .split(",")
      .map((x) => x.trim());
    expect(props).toContain("opacity");
    expect(props).toContain("transform");
    for (const l of ["width", "height", "margin", "top", "bottom"]) {
      expect(props).not.toContain(l);
    }
    await pill.click();
    await expect
      .poll(() =>
        log.evaluate((el) => el.scrollHeight - el.scrollTop - el.clientHeight),
      )
      .toBeLessThan(40);
    await expect(pill).toBeHidden();
    await expect(page.locator("textarea").first()).toBeEnabled();
  });
});
