import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Inbox geometry against the October 2026 mockup. Every number comes from
 * `docs/design/mockups/2026-10/src/Final-Inbox.dc.html` rendered at 1440x900
 * with `nav=hien-day` (rail pinned: the list column starts at x 332, the
 * reading pane at 773) and measured with the box dump (position, size,
 * padding, font, radius, colour) for the states binh-thuong, tat-ca and
 * het-viec and the modes sang and dem. Boxes are read relative to the
 * top-left of the list column, so the checks hold whatever sits left of it.
 * Tolerance is 1 px for boxes and exact for font sizes, weights, radii and
 * colours.
 *
 * The mock world differs from the mockup's: its notifications are a title and
 * a text with a read flag and a time (no person, no item, no type, no chip),
 * the read is not scoped to a workspace (so the "Chỉ workspace …" line is not
 * drawn) and four of the five tabs have no producer (drawn inert, no counts,
 * so the strip is one line instead of two). The avatar corner badge, the
 * context line, the "needs" chip, the bundle row, the crumb, the card, the
 * comment and the reply field are therefore measured on the Storybook stories
 * (all three states, light and dark, within 1 px); here only boxes that do not
 * depend on that data are compared: the title row, the tabs, the unread bar,
 * the day header, a row's dot, avatar, text and time, the reading pane's bar,
 * event and foot.
 *
 * The page clock is fixed to 15/01/2025 10:40, the mock world's own "today",
 * in Asia/Ho_Chi_Minh.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const INBOX = "/workspaces/mock-workspace-primary/inbox";
const NOW = new Date("2025-01-15T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

/** Final-Inbox binh-thuong nav=hien-day, origin: the list column. */
const MOCK = {
  list: { x: 0, y: 0, width: 441, height: 900 },
  header: { y: 0, height: 48 },
  h1: { x: 16, y: 17.5, width: 77.13, height: 29 },
  markAll: { y: 17, width: 178.23, height: 30 },
  settings: { x: 392, y: 16, width: 32, height: 32 },
  tab: { x: 12, height: 36 },
  bar: { height: 45 },
  toggle: { x: 12, height: 28, onWidth: 101.91 },
  note: { right: 428, height: 17.39 },
  day: { x: 0, width: 440, height: 33.39 },
  row: { x: 0, width: 440 },
  dot: { x: 5, y: 22, size: 7 },
  avatar: { x: 16, y: 11, size: 30 },
  body: { x: 57, y: 11 },
  timeRight: 424,
  read: { x: 441, width: 667, height: 900 },
  bar_: { y: 0, height: 53 },
  previous: { y: 10, size: 32 },
  open: { y: 9, height: 34, right: 1092 },
  event: { x: 469, y: 77 },
  eventAvatar: { x: 469, y: 77, size: 30 },
  eventText: { x: 511, y: 77, height: 23.25 },
  foot: { x: 441, y: 819, width: 667, height: 81 },
  footButton: { x: 465, y: 830, height: 32 },
  hintRight: 1084,
} as const;

const TYPE = {
  title: { size: "20px", weight: "650", letterSpacing: "-0.2px" },
  markAll: { size: "13px", weight: "500" },
  tab: { size: "13px", on: "600", off: "500" },
  toggle: { size: "12.5px", on: "600", off: "500" },
  note: { size: "12px" },
  day: { size: "12px", weight: "600" },
  what: { size: "13.5px", weight: "400" },
  time: { size: "12px" },
  event: { size: "15px", weight: "400" },
  eventWhen: { size: "12.5px" },
  button: { size: "13px", weight: "500" },
  hint: { size: "12px" },
} as const;

const COLOUR = {
  accent: "rgb(22, 102, 204)",
  accentSoft: "rgb(231, 240, 252)",
  accentText: "rgb(15, 86, 174)",
  panel: "rgb(255, 255, 255)",
  page: "rgb(247, 248, 250)",
  selected: "rgb(238, 244, 253)",
  line: "rgb(230, 233, 238)",
  lineStrong: "rgb(210, 215, 222)",
  ink: "rgb(22, 26, 33)",
  meta: "rgb(95, 104, 117)",
  muted: "rgb(79, 88, 102)",
} as const;

async function openInbox(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-system-rail", "pinned");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(INBOX);
  await expect(
    page.getByRole("heading", { level: 1, name: "Hộp thư" }),
  ).toBeVisible();
  await expect(
    page.locator('[data-f="items"] [data-f="it"]').first(),
  ).toBeVisible();
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** A locator's box relative to the top-left of the list column. */
async function rel(page: Page, locator: Locator): Promise<Rect> {
  const [o, found] = await Promise.all([
    box(page.locator('[data-f="list"]')),
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

/** A colour property as `rgb(r, g, b)`, whatever colour space the page uses. */
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

test.describe("Inbox geometry at 1440 px (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the list column and its title row", async ({ page }) => {
    await openInbox(page);
    const list = page.locator('[data-f="list"]');
    expectRect(await rel(page, list), MOCK.list, "list column");
    expect(await colour(list, "border-right-color")).toBe(COLOUR.line);
    expect(await colour(list, "background-color")).toBe(COLOUR.panel);

    const header = page.locator('[data-f="lh"]');
    expectRect(
      await rel(page, header),
      { y: MOCK.header.y, height: MOCK.header.height },
      "title row",
    );
    const h1 = page.locator('[data-f="h1"]');
    expectRect(await rel(page, h1), MOCK.h1, "title");
    expect(await style(h1, "font-size")).toBe(TYPE.title.size);
    expect(await style(h1, "font-weight")).toBe(TYPE.title.weight);
    expect(await style(h1, "letter-spacing")).toBe(TYPE.title.letterSpacing);
    expect(await colour(h1, "color")).toBe(COLOUR.ink);

    const markAll = page.getByRole("button", {
      name: "Đánh dấu tất cả đã đọc",
    });
    expectRect(await rel(page, markAll), MOCK.markAll, "mark all read");
    expect(await style(markAll, "font-size")).toBe(TYPE.markAll.size);
    expect(await style(markAll, "font-weight")).toBe(TYPE.markAll.weight);
    expect(await style(markAll, "border-radius")).toBe("7px");
    expect(await colour(markAll, "color")).toBe(COLOUR.muted);

    const settings = page.getByRole("link", { name: "Cài đặt thông báo" });
    expectRect(await rel(page, settings), MOCK.settings, "settings");
    expect(await style(settings, "border-radius")).toBe("7px");
    expect(await colour(settings, "color")).toBe(COLOUR.meta);
  });

  test("the tabs and the unread bar", async ({ page }) => {
    await openInbox(page);
    const strip = page.getByRole("tablist", { name: "Loại thông báo" });
    const stripBox = await rel(page, strip);
    // No "Chỉ workspace …" line: the strip follows the 48 px title row.
    expectRect(
      stripBox,
      { x: 0, y: MOCK.header.height, width: 440 },
      "tab strip",
    );
    expect(await colour(strip, "border-bottom-color")).toBe(COLOUR.line);
    const tabs = strip.getByRole("tab");
    await expect(tabs).toHaveCount(5);
    const all = tabs.nth(4);
    expect(await style(all, "font-size")).toBe(TYPE.tab.size);
    expect(await style(all, "font-weight")).toBe(TYPE.tab.on);
    expect(await colour(all, "color")).toBe(COLOUR.ink);
    expect(await colour(all, "border-bottom-color")).toBe(COLOUR.accent);
    expect(await style(all, "border-bottom-width")).toBe("2px");
    const first = tabs.nth(0);
    const firstBox = await rel(page, first);
    expectRect(
      firstBox,
      { x: MOCK.tab.x, height: MOCK.tab.height },
      "first tab",
    );
    // 10 px under the title row's bottom edge of the strip.
    expect(Math.abs(firstBox.y - (stripBox.y + 10))).toBeLessThanOrEqual(1);
    expect(await style(first, "font-weight")).toBe(TYPE.tab.off);
    expect(await colour(first, "color")).toBe(COLOUR.muted);
    // Tabs touch 2 px apart.
    const second = await rel(page, tabs.nth(1));
    expect(
      Math.abs(firstBox.x + firstBox.width + 2 - second.x),
    ).toBeLessThanOrEqual(1);

    const bar = page.locator('[data-f="lbar"]');
    const barBox = await rel(page, bar);
    expectRect(
      barBox,
      { x: 0, width: 440, height: MOCK.bar.height },
      "unread bar",
    );
    expect(
      Math.abs(barBox.y - (stripBox.y + stripBox.height)),
    ).toBeLessThanOrEqual(1);
    expect(await colour(bar, "border-bottom-color")).toBe(COLOUR.line);
    const toggle = page.getByRole("button", { name: "Chỉ chưa đọc" });
    const toggleBox = await rel(page, toggle);
    expectRect(
      toggleBox,
      { x: MOCK.toggle.x, height: MOCK.toggle.height },
      "unread toggle",
    );
    expect(Math.abs(toggleBox.y - (barBox.y + 8))).toBeLessThanOrEqual(1);
    // Off on the All tab: the outlined look.
    expect(await style(toggle, "font-size")).toBe(TYPE.toggle.size);
    expect(await style(toggle, "font-weight")).toBe(TYPE.toggle.off);
    expect(await colour(toggle, "border-top-color")).toBe(COLOUR.lineStrong);
    expect(await colour(toggle, "color")).toBe(COLOUR.muted);
    await toggle.click();
    // On: the accent soft look, 101.91 wide as in the mockup.
    expect(await style(toggle, "font-weight")).toBe(TYPE.toggle.on);
    expect(await colour(toggle, "background-color")).toBe(COLOUR.accentSoft);
    expect(await colour(toggle, "color")).toBe(COLOUR.accentText);
    expect(
      Math.abs((await rel(page, toggle)).width - MOCK.toggle.onWidth),
    ).toBeLessThanOrEqual(1);

    const note = bar.locator("span").last();
    const noteBox = await rel(page, note);
    expect(
      Math.abs(noteBox.x + noteBox.width - MOCK.note.right),
    ).toBeLessThanOrEqual(1);
    expect(Math.abs(noteBox.height - MOCK.note.height)).toBeLessThanOrEqual(1);
    expect(await style(note, "font-size")).toBe(TYPE.note.size);
    expect(await colour(note, "color")).toBe(COLOUR.meta);
  });

  test("the day header and a notification row", async ({ page }) => {
    await openInbox(page);
    const items = page.locator('[data-f="items"]');
    const itemsBox = await rel(page, items);
    const day = items.locator('[data-f="day"]').first();
    const dayBox = await rel(page, day);
    expectRect(dayBox, { ...MOCK.day, y: itemsBox.y }, "day header");
    expect(await style(day, "font-size")).toBe(TYPE.day.size);
    expect(await style(day, "font-weight")).toBe(TYPE.day.weight);
    expect(await colour(day, "color")).toBe(COLOUR.meta);

    const rows = items.locator('[data-f="it"]');
    const row = rows.first();
    const rowBox = await rel(page, row);
    expectRect(rowBox, MOCK.row, "row");
    expect(Math.abs(rowBox.y - (dayBox.y + dayBox.height))).toBeLessThanOrEqual(
      1,
    );
    expect(await style(row, "column-gap")).toBe("11px");
    expect(await style(row, "padding-left")).toBe("16px");
    expect(await colour(row, "border-bottom-color")).toBe(COLOUR.line);
    // The first row is the open one: the soft selected tone.
    expect(await colour(row, "background-color")).toBe(COLOUR.selected);
    expect(await colour(rows.nth(1), "background-color")).not.toBe(
      COLOUR.selected,
    );

    const avatar = row.locator('[data-f="av"]');
    expectRect(
      await rel(page, avatar),
      {
        x: MOCK.avatar.x,
        y: rowBox.y + MOCK.avatar.y,
        width: MOCK.avatar.size,
        height: MOCK.avatar.size,
      },
      "avatar",
    );
    expect(await style(avatar, "border-radius")).toMatch(
      /^(50%|[0-9.]+e\+\d+px|9999px|15px)$/,
    );

    const what = row.locator('[data-f="what"]');
    expectRect(
      await rel(page, what),
      { x: MOCK.body.x, y: rowBox.y + MOCK.body.y },
      "text",
    );
    expect(await style(what, "font-size")).toBe(TYPE.what.size);
    expect(await style(what, "font-weight")).toBe(TYPE.what.weight);
    expect(await style(what, "line-height")).toBe("19.575px");

    const time = row.locator('[data-f="time"]');
    const timeBox = await rel(page, time);
    expect(
      Math.abs(timeBox.x + timeBox.width - MOCK.timeRight),
    ).toBeLessThanOrEqual(1);
    expect(await style(time, "font-size")).toBe(TYPE.time.size);
    expect(await colour(time, "color")).toBe(COLOUR.meta);
  });

  test("an unread row: the dot and the unread-only look", async ({ page }) => {
    await openInbox(page);
    const unread = page
      .locator('[data-f="items"] [data-f="it"]')
      .filter({ has: page.locator('[data-f="ud"]') })
      .first();
    const rowBox = await rel(page, unread);
    const dot = unread.locator('[data-f="ud"]');
    expectRect(
      await rel(page, dot),
      {
        x: MOCK.dot.x,
        y: rowBox.y + MOCK.dot.y,
        width: MOCK.dot.size,
        height: MOCK.dot.size,
      },
      "unread dot",
    );
    expect(await style(dot, "border-radius")).toMatch(
      /^(50%|[0-9.]+e\+\d+px|9999px|3\.5px)$/,
    );
    expect(await colour(dot, "background-color")).toBe(COLOUR.accent);
    // An unread text is the ink, a read one the muted tone.
    expect(await colour(unread.locator('[data-f="what"]'), "color")).toBe(
      COLOUR.ink,
    );
    const read = page
      .locator('[data-f="items"] [data-f="it"]')
      .filter({ hasNot: page.locator('[data-f="ud"]') })
      .first();
    expect(await colour(read.locator('[data-f="what"]'), "color")).toBe(
      COLOUR.muted,
    );
    await expect(read.locator('[data-f="what"] b').first()).toHaveCSS(
      "font-weight",
      "600",
    );
  });

  test("the reading pane: bar, buttons, event and foot", async ({ page }) => {
    await openInbox(page);
    const pane = page.getByRole("main", { name: "Chi tiết thông báo" });
    const paneBox = await rel(page, pane);
    expectRect(paneBox, MOCK.read, "reading pane");
    expect(await colour(pane, "background-color")).toBe(COLOUR.page);

    const bar = pane.locator('[data-f="rbar"]');
    expectRect(
      await rel(page, bar),
      {
        x: MOCK.read.x,
        y: MOCK.bar_.y,
        width: MOCK.read.width,
        height: MOCK.bar_.height,
      },
      "bar",
    );
    expect(await colour(bar, "border-bottom-color")).toBe(COLOUR.line);
    expect(await colour(bar, "background-color")).toBe(COLOUR.panel);
    const previous = page.getByRole("button", { name: "Thông báo trước (K)" });
    const next = page.getByRole("button", { name: "Thông báo sau (J)" });
    const open = page.getByRole("button", { name: "Mở công việc" });
    const previousBox = await rel(page, previous);
    const nextBox = await rel(page, next);
    const openBox = await rel(page, open);
    expectRect(
      previousBox,
      {
        y: MOCK.previous.y,
        width: MOCK.previous.size,
        height: MOCK.previous.size,
      },
      "previous",
    );
    expectRect(
      nextBox,
      {
        y: MOCK.previous.y,
        width: MOCK.previous.size,
        height: MOCK.previous.size,
      },
      "next",
    );
    // 6 px between the three and the right edge 16 px from the pane's.
    expect(
      Math.abs(previousBox.x + previousBox.width + 6 - nextBox.x),
    ).toBeLessThanOrEqual(1);
    expect(
      Math.abs(nextBox.x + nextBox.width + 6 - openBox.x),
    ).toBeLessThanOrEqual(1);
    expect(
      Math.abs(openBox.x + openBox.width - MOCK.open.right),
    ).toBeLessThanOrEqual(1);
    expect(Math.abs(openBox.y - MOCK.open.y)).toBeLessThanOrEqual(1);
    expect(Math.abs(openBox.height - MOCK.open.height)).toBeLessThanOrEqual(1);
    expect(await style(open, "font-size")).toBe(TYPE.button.size);
    expect(await style(open, "border-radius")).toBe("7px");

    const event = pane.locator('[data-f="event"]');
    expectRect(await rel(page, event), MOCK.event, "event");
    expect(await style(event, "column-gap")).toBe("12px");
    const avatar = event.locator('[data-f="av"]');
    expectRect(
      await rel(page, avatar),
      { x: MOCK.eventAvatar.x, y: MOCK.eventAvatar.y, width: 30, height: 30 },
      "event avatar",
    );
    const text = event.locator("p");
    expectRect(
      await rel(page, text),
      {
        x: MOCK.eventText.x,
        y: MOCK.eventText.y,
        height: MOCK.eventText.height,
      },
      "event text",
    );
    expect(await style(text, "font-size")).toBe(TYPE.event.size);
    expect(await style(text, "font-weight")).toBe(TYPE.event.weight);
    const when = event.locator("small");
    expect(await style(when, "font-size")).toBe(TYPE.eventWhen.size);
    expect(await colour(when, "color")).toBe(COLOUR.meta);

    const foot = pane.locator('[data-f="foot"]');
    expectRect(await rel(page, foot), MOCK.foot, "foot");
    expect(await colour(foot, "border-top-color")).toBe(COLOUR.line);
    expect(await colour(foot, "background-color")).toBe(COLOUR.panel);
    const handled = page.getByRole("button", { name: "Đã xử lý" });
    const handledBox = await rel(page, handled);
    expectRect(
      handledBox,
      {
        x: MOCK.footButton.x,
        y: MOCK.footButton.y,
        height: MOCK.footButton.height,
      },
      "Đã xử lý",
    );
    expect(Math.abs(handledBox.width - 99.05)).toBeLessThanOrEqual(1);
    expect(await style(handled, "font-size")).toBe(TYPE.button.size);
    expect(await style(handled, "border-radius")).toBe("7px");
    expect(await colour(handled, "border-top-color")).toBe(COLOUR.lineStrong);
    const snooze = page.getByRole("button", { name: /^Nhắc lại sau/ });
    const snoozeBox = await rel(page, snooze);
    // 8 px between the foot's buttons.
    expect(
      Math.abs(handledBox.x + handledBox.width + 8 - snoozeBox.x),
    ).toBeLessThanOrEqual(1);
    expect(Math.abs(snoozeBox.width - 144.91)).toBeLessThanOrEqual(1);
    const mute = page.getByRole("button", { name: /^Tắt thông báo việc này/ });
    expect(await colour(mute, "color")).toBe(COLOUR.muted);
    expect(await style(mute, "border-top-color")).not.toBe("");

    const hint = pane.locator('[data-f="hint"]');
    const hintBox = await rel(page, hint);
    expect(
      Math.abs(hintBox.x + hintBox.width - MOCK.hintRight),
    ).toBeLessThanOrEqual(1);
    expect(Math.abs(hintBox.height - 20)).toBeLessThanOrEqual(1);
    expect(await style(hint, "font-size")).toBe(TYPE.hint.size);
    const key = hint.locator("span.font-mono").first();
    expect(Math.abs((await rel(page, key)).width - 18.61)).toBeLessThanOrEqual(
      1,
    );
    expect(await style(key, "font-size")).toBe("11px");
    expect(await style(key, "border-radius")).toBe("4px");
  });
});
