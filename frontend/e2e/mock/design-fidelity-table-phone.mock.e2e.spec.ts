import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The board table at phone width against `Final-M-Board`, state `danh-sach` (the list view, 390x844). Every number
 * was read from the rendered artboard (`render.py Final-M-Board.dc.html out.html state=danh-sach`), boxes relative
 * to the first section's top-left so what sits above the list (the page header, the view row) does not matter.
 * Tolerance is 1 px for boxes; font sizes, weights and radii are exact.
 *
 * Not compared: the page header, the view row and the tab bar around the list (other owners), and the row menu
 * sheet, which the artboard does not draw for a row.
 *
 * Dark: the artboard's dark render holds every box within 1.5 px of its light twin (text widths only), so the
 * app's dark page is held to its own light page, box by box, within 0.5 px.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);
test.use({ viewport: { width: 390, height: 844 } });

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function openList(page: Page, dark = false, query = "") {
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
  await page.goto(`${BOARD}?view=table${query}`);
  await expect(page.locator("[data-slot=table-phone-list]")).toBeVisible();
  await page.waitForTimeout(500);
}

async function openTable(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(`${BOARD}?view=table`);
  await expect(page.getByRole("table")).toBeVisible();
}

async function within(origin: Locator, target: Locator): Promise<Rect> {
  const [o, b] = await Promise.all([
    origin.boundingBox(),
    target.boundingBox(),
  ]);
  if (!o || !b) throw new Error("element has no box");
  return { x: b.x - o.x, y: b.y - o.y, width: b.width, height: b.height };
}

function expectRect(actual: Rect, want: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const expected = want[key];
    if (expected === undefined) continue;
    expect(
      Math.abs(actual[key] - expected),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${expected}`,
    ).toBeLessThanOrEqual(1);
  }
}

const style = (locator: Locator, property: string) =>
  locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );

test("danh-sach: section heads, card lists, 44 px check and more, the row text and Xem thêm", async ({
  page,
}) => {
  await openList(page);
  const sections = page.locator("[data-slot=table-phone-list] section");
  const first = sections.first();
  expectRect(
    await within(first, first),
    { x: 0, y: 0, width: 358, height: 349.9 },
    "first section",
  );

  const head = first.locator("h2");
  expectRect(
    await within(first, head),
    { x: 0, y: 0, width: 358, height: 40 },
    "section head",
  );
  expect(await style(head, "font-size")).toBe("17px");
  expect(await style(head, "font-weight")).toBe("650");

  const list = first.locator("> ul");
  expectRect(
    await within(first, list),
    { x: 0, y: 44, width: 358, height: 305.9 },
    "card list",
  );
  expect(await style(list, "border-radius")).toBe("14px");

  const rows = list.locator("> li[data-row-id]");
  await expect(rows).toHaveCount(3);
  for (const [index, [y, height]] of [
    [45, 84.6],
    [129.6, 85.6],
    [215.3, 85.6],
  ].entries())
    expectRect(
      await within(first, rows.nth(index)),
      { x: 1, y, width: 356, height },
      `row ${index}`,
    );

  const row = rows.first();
  const tick = row.getByRole("button", { name: "Đánh dấu xong ERP-151" });
  expectRect(
    await within(first, tick),
    { x: 3, y: 51, width: 44, height: 44 },
    "check target",
  );
  expect(await style(tick, "border-radius")).toBe("12px");
  const circle = tick.locator("> span");
  expectRect(
    await within(first, circle),
    { x: 14, y: 62, width: 22, height: 22 },
    "check circle",
  );

  const more = row.getByRole("button", { name: "Thao tác khác cho ERP-151" });
  expectRect(
    await within(first, more),
    { x: 313, y: 51, width: 44, height: 44 },
    "more",
  );
  expect(await style(more, "border-radius")).toBe("12px");

  const open = row.locator("[data-row-open]");
  expectRect(
    await within(first, open),
    { x: 47, y: 51, width: 266, height: 72.6 },
    "row body",
  );
  expect(await style(open, "border-radius")).toBe("8px");
  const [title, meta] = await open.locator("> span:first-child > span").all();
  expectRect(
    await within(first, title!),
    { x: 47, y: 56, width: 186, height: 40.8 },
    "title",
  );
  expect(await style(title!, "font-size")).toBe("15px");
  expect(await style(title!, "font-weight")).toBe("500");
  expectRect(
    await within(first, meta!),
    { x: 47, y: 99.8, width: 186, height: 18.8 },
    "key and assignee",
  );
  expect(await style(meta!, "font-size")).toBe("13px");
  const end = open.locator("> span:last-child");
  expectRect(
    await within(first, end),
    { x: 243, y: 56, width: 70 },
    "due and priority",
  );
  expect(await style(end, "font-size")).toBe("13px");

  // "Xem thêm N việc": the 48 px row closing the list.
  const moreRows = list.locator("> li:last-child");
  expectRect(
    await within(first, moreRows),
    { x: 1, y: 300.9, width: 356, height: 48 },
    "Xem thêm",
  );

  // The next section follows 14 px under the list.
  const second = sections.nth(1);
  expectRect(
    await within(first, second.locator("h2")),
    { y: 363.9, height: 40 },
    "second section head",
  );
  expectRect(
    await within(first, second.locator("> ul")),
    { y: 407.9 },
    "second card list",
  );
});

test("danh-sach: a closed group is one 52 px row inside a 54 px card", async ({
  page,
}) => {
  // The groups a person closed on the grid stay closed in the list: close "Cần làm" at desktop width.
  await page.setViewportSize({ width: 1440, height: 900 });
  await openTable(page);
  await page.getByRole("button", { name: "Thu gọn nhóm Cần làm" }).click();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload();
  const closed = page.locator(
    '[data-slot=table-phone-list] section[aria-label="Cần làm"]',
  );
  await expect(closed).toBeVisible();
  const button = closed.getByRole("button", { name: /Cần làm/ });
  expectRect(
    await within(closed, button),
    { x: 0, y: 0, width: 358, height: 54 },
    "closed group",
  );
  expect(await style(button, "border-radius")).toBe("14px");
  await expect(closed.locator("h2")).toHaveCount(0);
});

for (const [label, query] of [
  ["list", ""],
  ["no results", "&filter=p.low~o"],
] as const) {
  test(`dark keeps every box of the phone list where light has it: ${label}`, async ({
    browser,
  }) => {
    const read = async (dark: boolean) => {
      const page = await (
        await browser.newContext({ timezoneId: "Asia/Ho_Chi_Minh" })
      ).newPage();
      await openList(page, dark, query);
      return page.evaluate(() =>
        [...document.querySelectorAll("[data-slot=table-phone-list] *")]
          .filter((element) => element.getClientRects().length > 0)
          .map((element): [string, number, number, number, number] => {
            const r = element.getBoundingClientRect();
            return [element.tagName, r.x, r.y, r.width, r.height];
          }),
      );
    };
    const light = await read(false);
    const dark = await read(true);
    expect(dark.length, "elements").toBe(light.length);
    expect(light.length).toBeGreaterThan(label === "list" ? 100 : 2);
    light.forEach((box, index) => {
      const twin = dark[index]!;
      expect(twin[0], `element ${index}`).toBe(box[0]);
      for (const k of [1, 2, 3, 4] as const)
        expect(
          Math.abs((twin[k] as number) - (box[k] as number)),
          `${box[0]} #${index} box ${k}`,
        ).toBeLessThanOrEqual(0.5);
    });
  });
}
