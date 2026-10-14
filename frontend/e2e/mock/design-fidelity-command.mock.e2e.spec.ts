import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Command palette geometry against the October 2026 mockup. Every number
 * comes from `docs/design/mockups/2026-10/src/Final-Command.dc.html` rendered
 * at 1440x900 for the states tim-kiem, chua-go, khong-thay and dang-chay and
 * measured with the box dump (position, size, padding, font, radius, colour).
 * Boxes are read relative to the top-left of the dialog (`.pal`), so the
 * checks hold whatever the page behind it is. Tolerance is 1 px for boxes and
 * exact for font sizes, weights and colours.
 *
 * The mock world differs from the mockup's: items carry no key, status or
 * assignee, Boards and Docs are the seeded ones, and a fresh member has no
 * recents. Only boxes that do not depend on that data are compared here (the
 * query row, the filter tabs, the group header, a row's icon, text and ends,
 * the foot); the data-dependent second lines, status pills and highlighted
 * notes are measured on the Storybook stories, which are fed the mockup's own
 * data.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const HOME = "/workspaces/mock-workspace-primary/home";
const NOW = new Date("2025-01-15T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

const COLOUR = {
  accent: "rgb(22, 102, 204)",
  panel: "rgb(255, 255, 255)",
  page: "rgb(247, 248, 250)",
  selected: "rgb(238, 244, 253)",
  line: "rgb(230, 233, 238)",
  lineStrong: "rgb(210, 215, 222)",
  ink: "rgb(22, 26, 33)",
  meta: "rgb(95, 104, 117)",
  muted: "rgb(79, 88, 102)",
} as const;

async function openPalette(page: Page, query?: string) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(HOME);
  await expect(
    page.getByRole("button", { name: /Tìm hoặc chạy lệnh/ }),
  ).toBeVisible({ timeout: 20_000 });
  await page.keyboard.press("Meta+k");
  const input = page.getByRole("combobox");
  await expect(input).toBeFocused();
  if (query !== undefined) await input.fill(query);
  await expect(page.locator('[data-f="r"]').first()).toBeVisible();
  // Measure at rest: the dialog fades and scales in for 100 ms.
  await page.evaluate(() =>
    Promise.all(document.getAnimations().map((a) => a.finished.catch(() => 0))),
  );
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** A locator's box relative to the top-left of the dialog. */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [o, found] = await Promise.all([
    box(page.locator('[data-f="pal"]')),
    box(locator),
  ]);
  return {
    x: found.x - o.x,
    y: found.y - o.y,
    width: found.width,
    height: found.height,
  };
}

async function style(locator: Locator, property: string): Promise<string> {
  return locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );
}

async function colour(locator: Locator, property: string): Promise<string> {
  return locator.evaluate((element, name) => {
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

function expectRect(actual: Rect, expected: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want === undefined) continue;
    expect(
      Math.abs(actual[key] - want),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${want}`,
    ).toBeLessThanOrEqual(1);
  }
}

const f = (page: Page, name: string) => page.locator(`[data-f="${name}"]`);

test.describe("Command palette geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the dialog sits 92 px from the top, 702 px wide and centred, over a scrim", async ({
    page,
  }) => {
    await openPalette(page, "erp");
    const pal = await box(f(page, "pal"));
    expect(Math.abs(pal.x - (1440 - 702) / 2)).toBeLessThanOrEqual(1);
    expect(Math.abs(pal.y - 92)).toBeLessThanOrEqual(1);
    expect(Math.abs(pal.width - 702)).toBeLessThanOrEqual(1);
    expect(await style(f(page, "pal"), "border-radius")).toBe("14px");
    expect(await colour(f(page, "pal"), "background-color")).toBe(COLOUR.panel);
    expect(await colour(f(page, "pal"), "border-top-color")).toBe(
      COLOUR.lineStrong,
    );
    // The scrim is the modal scrim (no blur): 38% of the dark slate.
    const overlay = page.locator('[data-slot="dialog-overlay"]');
    expect(await style(overlay, "backdrop-filter")).toMatch(/none|^$/);
    expect(await style(overlay, "background-color")).toMatch(/0\.38\)?$/);
    const shadow = await style(f(page, "pal"), "box-shadow");
    expect(shadow).toContain("64px");
  });

  test("the query row: icon, text, scope chip and Esc key", async ({
    page,
  }) => {
    await openPalette(page, "erp");
    expectRect(
      await rel(page, f(page, "qrow")),
      { x: 1, y: 1, width: 700, height: 57 },
      "query row",
    );
    expect(await colour(f(page, "qrow"), "border-bottom-color")).toBe(
      COLOUR.line,
    );
    expectRect(
      await rel(page, f(page, "qicon")),
      { x: 19, y: 19.5, width: 19, height: 19 },
      "search icon",
    );
    const q = f(page, "q");
    expectRect(await rel(page, q), { x: 48, height: 24.64 }, "query");
    const input = page.getByRole("combobox");
    expect(await style(input, "font-size")).toBe("17px");
    expect(await style(input, "font-weight")).toBe("400");
    expect(await colour(input, "color")).toBe(COLOUR.ink);

    const scope = f(page, "scope");
    expectRect(await rel(page, scope), { y: 15, height: 28 }, "scope chip");
    const scopeBox = await rel(page, scope);
    // The scope chip and Esc end 14 px from the right edge.
    const esc = f(page, "esc");
    const escBox = await rel(page, esc);
    expectRect(escBox, { y: 19, height: 20 }, "Esc key");
    expect(Math.abs(escBox.x + escBox.width - 686)).toBeLessThanOrEqual(1);
    expect(
      Math.abs(escBox.x - (scopeBox.x + scopeBox.width + 10)),
    ).toBeLessThanOrEqual(1);
    expect(await style(scope, "font-size")).toBe("12.5px");
    expect(await style(scope, "font-weight")).toBe("500");
    expect(await colour(scope, "background-color")).toBe(COLOUR.page);
    expect(await style(scope, "border-radius")).toBe("7px");
    expect(await style(esc, "font-size")).toBe("11px");
    expect(await colour(esc, "color")).toBe(COLOUR.muted);
  });

  test("the filter tabs and the first group header", async ({ page }) => {
    await openPalette(page, "erp");
    expectRect(
      await rel(page, f(page, "filters")),
      { x: 1, y: 58, width: 700, height: 45 },
      "filters",
    );
    const tabs = f(page, "fl");
    expect(await tabs.count()).toBe(6);
    const first = tabs.first();
    expectRect(
      await rel(page, first),
      { x: 15, y: 66, height: 28 },
      "first tab",
    );
    expect(await style(first, "font-size")).toBe("12.5px");
    expect(await style(first, "font-weight")).toBe("600");
    expect(await colour(first, "background-color")).toBe(COLOUR.ink);
    expect(await colour(first, "color")).toBe(COLOUR.panel);
    expect(await style(first, "border-radius")).toMatch(
      /9999|999|3\.35544e\+07px/,
    );
    const second = tabs.nth(1);
    expect(await style(second, "font-weight")).toBe("500");
    expect(await colour(second, "color")).toBe(COLOUR.muted);
    // Tabs sit 4 px apart.
    const a = await rel(page, first);
    const b = await rel(page, second);
    expect(Math.abs(b.x - (a.x + a.width + 4))).toBeLessThanOrEqual(1);
    const count = f(page, "flc").first();
    expect(await style(count, "font-size")).toBe("11.5px");

    expectRect(
      await rel(page, f(page, "res")),
      { x: 1, y: 103, width: 700 },
      "results",
    );
    const header = f(page, "gh").first();
    expectRect(
      await rel(page, header),
      { x: 9, y: 109, width: 684, height: 31.39 },
      "group header",
    );
    expect(await style(header, "font-size")).toBe("12px");
    expect(await style(header, "font-weight")).toBe("600");
    expect(await colour(header, "color")).toBe(COLOUR.meta);
  });

  test("a result row: tile, text, second line and the selected tone", async ({
    page,
  }) => {
    await openPalette(page, "erp");
    const row = f(page, "r").first();
    expectRect(
      await rel(page, row),
      { x: 9, y: 140.39, width: 684, height: 60 },
      "first row",
    );
    expect(await colour(row, "background-color")).toBe(COLOUR.selected);
    expect(await style(row, "border-radius")).toBe("9px");
    expect(await style(f(page, "r").nth(1), "background-color")).toMatch(
      /rgba\(0, 0, 0, 0\)|transparent/,
    );
    const tile = row.locator('[data-f="ic"]');
    expectRect(
      await rel(page, tile),
      { x: 19, y: 154.39, width: 32, height: 32 },
      "tile",
    );
    expect(await style(tile, "border-radius")).toBe("8px");
    expect(await colour(tile, "border-top-color")).toBe(COLOUR.line);
    expect(await colour(tile, "background-color")).toBe(COLOUR.page);
    expect(await colour(tile, "color")).toBe(COLOUR.muted);
    const body = row.locator('[data-f="body"]');
    expectRect(
      await rel(page, body),
      { x: 63, y: 151.05, height: 38.69 },
      "text block",
    );
    const title = row.locator('[data-f="t"]');
    expectRect(
      await rel(page, title),
      { x: 63, y: 151.05, height: 20.3 },
      "title",
    );
    expect(await style(title, "font-size")).toBe("14px");
    expect(await style(title, "font-weight")).toBe("500");
    expect(await colour(title, "color")).toBe(COLOUR.ink);
    const ctx = row.locator('[data-f="ctx"]');
    expectRect(
      await rel(page, ctx),
      { x: 63, y: 172.34, height: 17.39 },
      "second line",
    );
    expect(await style(ctx, "font-size")).toBe("12px");
    expect(await colour(ctx, "color")).toBe(COLOUR.meta);
    // The mark in the second line is 16 px, rounded 4 px, 6 px from the text.
    const mark = ctx.locator("span.contents, [data-f='ctxdm'] > *").first();
    if (await mark.count()) {
      const m = await rel(page, mark);
      expect(Math.abs(m.width - 16)).toBeLessThanOrEqual(1);
      expect(Math.abs(m.x - 63)).toBeLessThanOrEqual(1);
      expect(await style(mark, "border-radius")).toBe("4px");
    }
    // The selected row ends with the open hint, right-aligned 10 px inside.
    const hint = row.locator('[data-f="hintsel"]');
    expect(await style(hint, "font-weight")).toBe("600");
    expect(await colour(hint, "color")).toBe("rgb(15, 86, 174)");
    const h = await rel(page, hint);
    expect(Math.abs(h.x + h.width - 683)).toBeLessThanOrEqual(1);
  });

  test("the foot lists the key hints in one row", async ({ page }) => {
    await openPalette(page, "erp");
    const foot = f(page, "foot");
    const footBox = await rel(page, foot);
    expectRect(footBox, { x: 1, width: 700, height: 39 }, "foot");
    // The foot closes the dialog: it ends one border above the bottom edge.
    const pal = await rel(page, f(page, "pal"));
    expect(
      Math.abs(footBox.y + footBox.height + 1 - pal.height),
    ).toBeLessThanOrEqual(1);
    expect(await colour(foot, "background-color")).toBe(COLOUR.page);
    expect(await colour(foot, "border-top-color")).toBe(COLOUR.line);
    expect(await style(foot, "font-size")).toBe("12px");
    const hints = f(page, "fh");
    expect(await hints.count()).toBe(7);
    const expected = [
      { x: 17, width: 76.28 },
      { x: 105.28, width: 42.17 },
      { x: 159.45, width: 140.94 },
      { x: 312.39, width: 79.59 },
      { x: 498.25, width: 47.86 },
      { x: 558.11, width: 56.11 },
      { x: 626.22, width: 58.78 },
    ];
    for (let i = 0; i < expected.length; i += 1) {
      expectRect(
        await rel(page, hints.nth(i)),
        { ...expected[i], height: 20 },
        `hint ${i}`,
      );
    }
    const key = f(page, "kbd").first();
    expect(await style(key, "font-size")).toBe("11px");
    expect(await style(key, "border-radius")).toBe("4px");
    expect(await colour(key, "background-color")).toBe("rgb(238, 240, 243)");
  });

  test("nothing typed lists the quick commands under their header", async ({
    page,
  }) => {
    await openPalette(page);
    expect(await f(page, "filters").count()).toBe(0);
    expectRect(
      await rel(page, f(page, "res")),
      { x: 1, y: 58, width: 700 },
      "results",
    );
    const header = f(page, "gh").last();
    expect(await header.textContent()).toBe("Lệnh nhanh");
    expect(await style(header, "font-size")).toBe("12px");
    const rows = f(page, "r");
    const last = rows.last();
    expectRect(
      await rel(page, last),
      { x: 9, width: 684, height: 60 },
      "last row",
    );
    // Rows are 60 px apart.
    const a = await rel(page, rows.nth(0));
    const b = await rel(page, rows.nth(1));
    expect(Math.abs(b.y - a.y - 60)).toBeLessThanOrEqual(1);
    // The foot is as tall as in the mockup whatever the list holds.
    expectRect(await rel(page, f(page, "foot")), { height: 39 }, "foot");
  });

  test("no match: the message, then inert suggestions", async ({ page }) => {
    await openPalette(page, "zzqqxx");
    expect(await f(page, "filters").count()).toBe(0);
    const empty = f(page, "empty");
    expectRect(
      await rel(page, empty),
      { x: 1, y: 58, width: 700, height: 90.05 },
      "message",
    );
    const title = f(page, "emptyt");
    expect(await style(title, "font-size")).toBe("15px");
    expect(await style(title, "font-weight")).toBe("700");
    expect(await colour(title, "color")).toBe(COLOUR.ink);
    expect(await style(empty, "text-align")).toBe("center");
    expectRect(
      await rel(page, f(page, "res")),
      { y: 148.05, width: 700 },
      "suggestions",
    );
    expectRect(
      await rel(page, f(page, "r").first()),
      { x: 9, y: 154.05, width: 684, height: 60 },
      "first suggestion",
    );
    expectRect(
      await rel(page, f(page, "foot")),
      { y: 282.05, height: 39 },
      "foot",
    );
  });

  test("the results list scrolls inside a 534 px frame and the foot stays", async ({
    page,
  }) => {
    await openPalette(page, "e");
    const res = f(page, "res");
    const box_ = await rel(page, res);
    expect(box_.height).toBeLessThanOrEqual(534.5);
    const overflow = await res.evaluate((n) => n.scrollHeight > n.clientHeight);
    if (overflow) expect(Math.abs(box_.height - 534)).toBeLessThanOrEqual(1);
    const pal = await box(f(page, "pal"));
    expect(pal.y + pal.height).toBeLessThanOrEqual(900);
  });
});

test.describe("Command palette at narrower widths (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );

  for (const size of [
    { width: 1280, height: 800 },
    { width: 1100, height: 760 },
  ]) {
    test(`stays 702 px wide and centred at ${size.width} px, page does not scroll`, async ({
      page,
    }) => {
      await page.setViewportSize(size);
      await openPalette(page, "erp");
      const pal = await box(f(page, "pal"));
      expect(Math.abs(pal.width - 702)).toBeLessThanOrEqual(1);
      expect(
        Math.abs(pal.x + pal.width / 2 - size.width / 2),
      ).toBeLessThanOrEqual(1);
      expect(Math.abs(pal.y - 92)).toBeLessThanOrEqual(1);
      expect(pal.y + pal.height).toBeLessThanOrEqual(size.height);
      const scroll = await page.evaluate(() => ({
        x:
          document.documentElement.scrollWidth -
          document.documentElement.clientWidth,
      }));
      expect(scroll.x).toBeLessThanOrEqual(0);
    });
  }
});
