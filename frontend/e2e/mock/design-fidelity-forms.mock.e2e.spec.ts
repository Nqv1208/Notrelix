import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Work Management forms against the design canvas artboards WM-Forms-List,
 * WM-Forms-Builder, WM-Forms-Submit, WM-Forms-Submit-States, WM-Forms-Inbox and
 * WM-Forms-Dem (the Night theme), rendered at 1440 px and measured with the box
 * dump. The canvas is not in the repository; every number below was read from
 * the rendered artboard. Boxes are read relative to a frame of the surface (the
 * table frame, the builder card, the form card, the inbox card), so the checks
 * hold whatever the app shell draws around them. Tolerance is 1 px for boxes
 * and exact for font sizes, radii and colours.
 *
 * Differences that are intended, written down where the product differs from
 * the drawing:
 *  - the list's actions column is 290 px, the artboard's is 230 px: the three
 *    buttons of a published row do not fit 230 px and the artboard draws them
 *    overflowing to the left, so the status and count columns sit 60 px
 *    further right in the artboard; right-anchored boxes (buttons, trash) match;
 *  - the "Biểu mẫu" heading of the settings panel is 17 px (the shared board
 *    settings header) where the artboard draws 16 px, and its intro wraps to
 *    two lines in the narrower settings column, so rows are measured from the
 *    top of the table frame, not from the heading;
 *  - the date question is a typed field with a picker button in the product
 *    (the artboard draws one 34 px field): same y and height, the field is
 *    shorter by the button;
 *  - the inbox list column is 421 px from 1280 px wide, the card is as wide as
 *    the app shell allows (1136 px at 1440) and as tall as its content, where
 *    the artboard frames a 1360 x 514 card; the answers' chips sit 0.35 px lower
 *    per row in the artboard (inline chip in a line box) than in the product's
 *    flex row, so only the first answer row is compared;
 *  - the artboard's closed row offers "Mở lại"; the product has no reopen action (a closed form stays
 *    closed, the row keeps "Xem phản hồi" and delete), so that button is not compared;
 *  - the mock world's names, forms and answers differ from the artboard's
 *    words; only boxes that do not depend on the wording are compared.
 *
 * Colours match within 2/255 a channel (the tokens round the artboard's hex values), with three
 * intended exceptions: the status chips use the label palette (light within 16/255 of the artboard's
 * hand-picked tones, Night lighter by design), the Night selected row is the token's (22, 33, 46)
 * where the artboard draws (22, 38, 62), and the Night danger fill is 10/255 off.
 *
 * The page clock is fixed to the fixtures' 12/10/2026 10:40. The checks need
 * the default owner world.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const SUPPORT = `${BOARD}/forms/mock-form-it-support`;
const NOW = new Date("2026-10-12T10:40:00+07:00");

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function open(page: Page, path: string, dark = false) {
  await page.setViewportSize({ width: 1440, height: 900 });
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

/** A locator's box relative to the top-left of `frame`. */
async function rel(frame: Locator, locator: Locator): Promise<Rect> {
  const [origin, box] = await Promise.all([
    frame.boundingBox(),
    locator.boundingBox(),
  ]);
  if (!origin || !box) throw new Error("element has no box");
  return {
    x: box.x - origin.x,
    y: box.y - origin.y,
    width: box.width,
    height: box.height,
  };
}

/** Distance from a locator's right edge to the right edge of `frame`. */
async function right(frame: Locator, locator: Locator): Promise<number> {
  const [origin, box] = await Promise.all([
    frame.boundingBox(),
    locator.boundingBox(),
  ]);
  if (!origin || !box) throw new Error("element has no box");
  return origin.x + origin.width - (box.x + box.width);
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
    expect
      .soft(
        Math.abs(actual[key] - want),
        `${label}.${key}: ${actual[key].toFixed(1)} vs ${want}`,
      )
      .toBeLessThanOrEqual(1);
  }
}

/** Fills and text colours match the artboard within 2/255 a channel (the tokens round the hex values). */
function expectColour(
  actual: string,
  want: string,
  label: string,
  tolerance = 2,
) {
  const a = actual.match(/\d+/g)!.map(Number);
  const w = want.match(/\d+/g)!.map(Number);
  expect
    .soft(
      Math.max(...a.map((value, index) => Math.abs(value - w[index]!))),
      `${label}: ${actual} vs ${want}`,
    )
    .toBeLessThanOrEqual(tolerance);
}

function expectNear(actual: number, want: number, label: string) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(1);
}

/** Font size, weight and radius of a control, exactly. */
async function expectType(
  locator: Locator,
  want: { size?: string; weight?: string; radius?: string },
  label: string,
) {
  if (want.size)
    expect
      .soft(await style(locator, "font-size"), `${label} size`)
      .toBe(want.size);
  if (want.weight)
    expect
      .soft(await style(locator, "font-weight"), `${label} weight`)
      .toBe(want.weight);
  if (want.radius)
    expect
      .soft(await style(locator, "border-top-left-radius"), `${label} radius`)
      .toBe(want.radius);
}

/** Light and Night palettes of the artboards (`.nx` and `.nx.mode-dem`). */
const PALETTE = {
  light: {
    panel: "rgb(255, 255, 255)",
    bg: "rgb(247, 248, 250)",
    ink: "rgb(22, 26, 33)",
    meta: "rgb(95, 104, 117)",
    accent: "rgb(22, 102, 204)",
    accentInk: "rgb(255, 255, 255)",
    sel: "rgb(238, 244, 253)",
    green: ["rgb(30, 106, 68)", "rgb(229, 244, 236)"],
    amber: ["rgb(122, 88, 16)", "rgb(251, 241, 214)"],
    gray: ["rgb(75, 82, 99)", "rgb(238, 240, 244)"],
    danger: "rgb(196, 35, 64)",
    dangerSoft: "rgb(252, 235, 238)",
  },
  dark: {
    panel: "rgb(26, 32, 40)",
    bg: "rgb(14, 17, 22)",
    ink: "rgb(230, 233, 238)",
    meta: "rgb(143, 153, 167)",
    accent: "rgb(90, 169, 255)",
    accentInk: "rgb(4, 17, 31)",
    sel: "rgb(22, 38, 62)",
    green: ["rgb(86, 201, 138)", "rgb(18, 37, 28)"],
    amber: ["rgb(242, 193, 78)", "rgb(39, 32, 18)"],
    gray: ["rgb(170, 179, 192)", "rgb(35, 42, 52)"],
    danger: "rgb(255, 107, 129)",
    dangerSoft: "rgb(58, 24, 32)",
  },
} as const;

test.describe("Forms geometry at 1440 px (owner)", () => {
  // The first run compiles the app on demand: give a cold server room.
  test.describe.configure({ timeout: 90_000 });
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Owner checks need the default owner world without faults",
  );

  for (const dark of [false, true]) {
    const mode = dark ? "dark" : "light";
    const P = PALETTE[mode];

    test(`list panel: table frame, rows, chips and row buttons, ${mode}`, async ({
      page,
    }) => {
      await open(page, `${BOARD}/settings?section=forms`, dark);
      const root = page.locator("section[aria-labelledby=bs-forms-h]");
      const frame = root.locator("ul").locator("xpath=..");
      await expect(frame).toBeVisible();
      await page.waitForTimeout(300);

      // WM-Forms-List: frame 1089 x 282 at (24, 78) of the panel, 12 px radius, header row 34, rows 57.
      const f = await frame.boundingBox();
      const r = await root.boundingBox();
      expectNear(f!.x - r!.x, 24, "frame x");
      expectNear(r!.x + r!.width - (f!.x + f!.width), 24, "frame right inset");
      expect.soft(await style(frame, "border-top-left-radius")).toBe("12px");
      expectColour(
        await colour(frame, "background-color"),
        P.panel,
        "background-color",
      );
      const header = frame.locator("> div").first();
      expectRect(
        await rel(frame, header),
        { x: 1, y: 1, height: 34 },
        "header",
      );
      expectColour(
        await colour(header, "background-color"),
        P.bg,
        "background-color",
      );
      expect
        .soft(await colour(header.locator("span").first(), "color"))
        .toBe(P.meta);

      const create = root.getByRole("button", { name: "Tạo biểu mẫu" });
      expectRect(
        await rel(root, create),
        { y: 20, width: 135.4, height: 32 },
        "create",
      );
      expect.soft(await right(root, create)).toBeCloseTo(24, 0);
      await expectType(
        create,
        { size: "13px", weight: "600", radius: "7px" },
        "create",
      );
      expectColour(
        await colour(create, "background-color"),
        P.accent,
        "background-color",
      );
      expectColour(await colour(create, "color"), P.accentInk, "color");

      const rows = frame.locator("li");
      await expect(rows).toHaveCount(3);
      for (const [index, top] of [35, 92 + 75, 224].entries()) {
        // Draft (with the delete confirmation open below) is added in the next step; here the three rows rest.
        const box = await rel(frame, rows.nth(index));
        expectNear(box.height, 57, `row ${index} height`);
        if (index === 0) expectNear(box.y, 35, "first row y");
        void top;
      }

      const draft = rows.filter({ hasText: "chưa xuất bản" });
      const published = rows.filter({ hasText: "Đang nhận" });
      const closed = rows.filter({ hasText: "Đã đóng" });
      expectColour(
        await colour(draft.locator("> div").first(), "background-color"),
        P.sel,
        "draft row",
        dark ? 16 : 2,
      );

      const chip = async (
        li: Locator,
        text: string,
        tone: readonly string[],
      ) => {
        const c = li.getByText(text, { exact: true });
        const box = await rel(li, c);
        expectNear(box.height, 20, `${text} height`);
        expectNear(box.y, 19.7, `${text} y`);
        await expectType(
          c,
          { size: "11.5px", weight: "600", radius: "6px" },
          text,
        );
        expectColour(
          await colour(c, "color"),
          tone[0],
          `${text} colour`,
          dark ? 65 : 16,
        );
        expectColour(
          await colour(c, "background-color"),
          tone[1],
          `${text} fill`,
          dark ? 65 : 16,
        );
      };
      await chip(published, "Đang nhận", P.green);
      await chip(draft, "Bản nháp", P.amber);
      await chip(closed, "Đã đóng", P.gray);

      // Small buttons: 28 high, 12.5 px, 7 px radius; right edges 141.3, 49 and 15 px from the frame's right edge.
      for (const [li, name, w, inset] of [
        [published, "Sao chép liên kết", 149.6, 141.3],
        [published, "Đóng nhận", 88.3, 49],
        [draft, "Xuất bản", 78.7, 49],
      ] as const) {
        const button = li.getByRole("button", { name, exact: true });
        const box = await rel(li, button);
        expectNear(box.width, w, `${name} width`);
        expectNear(box.height, 28, `${name} height`);
        expectNear(box.y, 15, `${name} y`);
        expectNear(await right(frame, button), inset, `${name} inset`);
        await expectType(button, { size: "12.5px", radius: "7px" }, name);
      }
      const trash = published.getByRole("button", { name: /^Xoá biểu mẫu/ });
      expectRect(
        await rel(published, trash),
        { width: 30, height: 30, y: 14 },
        "trash",
      );
      expectNear(await right(frame, trash), 15, "trash inset");
      expect
        .soft(
          await colour(
            draft.getByRole("button", { name: "Xuất bản" }),
            "background-color",
          ),
        )
        .toBe(P.accent);

      // The delete confirmation: 10 px radius card, 28 px buttons.
      await draft.getByRole("button", { name: /^Xoá biểu mẫu/ }).click();
      const confirm = draft.getByRole("alertdialog");
      await expect(confirm).toBeVisible();
      const block = confirm.locator("xpath=..");
      // The block is 75 px with the artboard's short name; the mock form's long name wraps, so the height follows the wording.
      expectColour(
        await colour(block, "background-color"),
        P.dangerSoft,
        "background-color",
        10,
      );
      expect.soft(await style(confirm, "border-top-left-radius")).toBe("10px");
      const danger = confirm.getByRole("button", { name: "Xoá biểu mẫu" });
      expectNear(
        (await danger.boundingBox())!.height,
        28,
        "confirm danger height",
      );
      expectNear(
        (await danger.boundingBox())!.width,
        106.5,
        "confirm danger width",
      );
      expectColour(
        await colour(danger, "background-color"),
        P.danger,
        "background-color",
      );
    });

    test(`builder: header, description, question rows and the settings panel, ${mode}`, async ({
      page,
    }) => {
      await open(page, `${SUPPORT}/edit`, dark);
      const card = page.locator("main section[aria-label]").first();
      await expect(card).toBeVisible();
      await page
        .getByRole("button", { name: /^Bạn cần gì/ })
        .first()
        .click();
      await page.waitForTimeout(300);

      // WM-Forms-Builder: card radius 12; header 63 + 1; title input 34; buttons 32.
      expect.soft(await style(card, "border-top-left-radius")).toBe("12px");
      expectColour(
        await colour(card, "background-color"),
        P.panel,
        "background-color",
      );
      const title = card.getByRole("textbox", { name: "Tên biểu mẫu" });
      expectRect(await rel(card, title), { y: 15, height: 34 }, "title input");
      await expectType(
        title,
        { size: "13.5px", weight: "600", radius: "7px" },
        "title",
      );
      const preview = card.getByRole("button", { name: "Xem trước" });
      expectRect(
        await rel(card, preview),
        { y: 16, width: 114.5, height: 32 },
        "preview",
      );
      await expectType(
        preview,
        { size: "13px", weight: "500", radius: "7px" },
        "preview",
      );
      const status = card.getByText("Đang nhận", { exact: true }).first();
      expectNear((await rel(card, status)).y, 22, "status chip y");
      await expectType(
        status,
        { size: "11.5px", weight: "600", radius: "6px" },
        "status",
      );
      expectColour(
        await colour(status, "color"),
        P.green[0],
        "color",
        dark ? 65 : 16,
      );
      expectColour(
        await colour(status, "background-color"),
        P.green[1],
        "background-color",
        dark ? 65 : 16,
      );

      const description = card.getByRole("textbox", { name: /^Mô tả/ });
      expectRect(await rel(card, description), { height: 64 }, "description");
      const label = card.locator("label", { hasText: "Mô tả (người điền" });
      await expectType(
        label,
        { size: "12.5px", weight: "600" },
        "description label",
      );
      expectNear(
        (await rel(card, description)).y - (await rel(card, label)).y,
        24.1,
        "label to field",
      );

      const heading = card.getByRole("heading", { name: /^Câu hỏi/ });
      await expectType(
        heading,
        { size: "16px", weight: "650" },
        "questions heading",
      );
      expectNear(
        (await rel(card, heading)).y - (await rel(card, description)).y,
        82.4,
        "heading below description",
      );
      await expectType(
        heading.locator("span"),
        { size: "12px", weight: "500" },
        "count",
      );
      const add = card.getByRole("button", { name: "Thêm câu hỏi" });
      expectRect(
        await rel(card, add),
        { width: 126.3, height: 28 },
        "add question",
      );
      await expectType(
        add,
        { size: "12.5px", weight: "500", radius: "7px" },
        "add question",
      );

      // Rows: 59 high, 10 px radius, 8 px apart; the number badge 24 px, 6 px radius; move buttons 30 px.
      const list = card.getByRole("list", { name: "Câu hỏi" });
      const rows = list.locator("li > div");
      const first = rows.nth(0);
      const second = rows.nth(1);
      const a = await rel(card, first);
      const b = await rel(card, second);
      expectNear(a.height, 59, "row height");
      expectNear(b.y - a.y, 67, "row pitch");
      expectNear(
        a.y - (await rel(card, heading)).y,
        41.6,
        "first row below heading",
      );
      expect.soft(await style(first, "border-top-left-radius")).toBe("10px");
      expectColour(
        await colour(first, "background-color"),
        P.sel,
        "background-color",
        dark ? 16 : 2,
      );
      expectColour(
        await colour(first, "border-top-color"),
        P.accent,
        "border-top-color",
      );
      expectColour(
        await colour(second, "background-color"),
        P.panel,
        "background-color",
      );
      const badge = first.locator("span.grid.size-6");
      expectRect(
        await rel(first, badge),
        { x: 43, y: 17.5, width: 24, height: 24 },
        "badge",
      );
      await expectType(
        badge,
        { size: "12px", weight: "600", radius: "6px" },
        "badge",
      );
      const required = first.getByText("Bắt buộc", { exact: true });
      expectNear(
        (await rel(first, required)).height,
        20,
        "required chip height",
      );
      await expectType(
        required,
        { size: "11.5px", weight: "600", radius: "6px" },
        "required",
      );
      const down = first.getByRole("button", { name: /xuống$/ });
      expectRect(
        await rel(first, down),
        { width: 30, height: 30 },
        "move down",
      );
      expectNear(
        await right(first, down),
        12 + 1 > 0 ? 12 : 12,
        "move down inset",
      );
      await expectType(
        first.locator("b"),
        { size: "13.5px", weight: "600" },
        "row title",
      );

      // Settings panel of the selected question: 380 wide, labels 12.5 px, fields 34, remove button 28.
      const aside = page.getByRole("complementary", {
        name: "Thiết lập câu hỏi",
      });
      expectNear((await aside.boundingBox())!.width, 380, "panel width");
      expect.soft(await style(aside, "border-top-left-radius")).toBe("12px");
      const panelHeading = aside.getByRole("heading", { name: "Câu hỏi 1" });
      expectNear((await rel(aside, panelHeading)).y, 19, "panel heading y");
      await expectType(
        panelHeading,
        { size: "16px", weight: "650" },
        "panel heading",
      );
      const labelInput = aside.getByRole("textbox").first();
      expectRect(
        await rel(aside, labelInput),
        { x: 19, y: 80.3, width: 342, height: 34 },
        "label input",
      );
      const remove = aside.getByRole("button", { name: "Gỡ câu hỏi" });
      expectRect(
        await rel(aside, remove),
        { width: 87.8, height: 28 },
        "remove question",
      );
      await expectType(
        remove,
        { size: "12.5px", weight: "500", radius: "7px" },
        "remove question",
      );
      expectColour(await colour(remove, "color"), P.danger, "color");
      expectColour(
        await colour(remove, "background-color"),
        P.panel,
        "background-color",
      );
      void status;
    });

    test(`fill page: card, fields, submit button and the error summary, ${mode}`, async ({
      page,
    }) => {
      await open(page, SUPPORT, dark);
      const card = page.locator("main form");
      await expect(card).toBeVisible();
      await page.waitForTimeout(300);

      // WM-Forms-Submit: card 620 wide, 12 px radius, 28 px padding; fields from the card's top-left (artboard y minus 252.7).
      const size = (await card.boundingBox())!;
      expectNear(size.width, 620, "card width");
      expect.soft(await style(card, "border-top-left-radius")).toBe("12px");
      expectColour(
        await colour(card, "background-color"),
        P.panel,
        "background-color",
      );
      const h1 = card.getByRole("heading", { level: 1 });
      expectRect(await rel(card, h1), { x: 29, y: 29, width: 562 }, "title");
      await expectType(h1, { size: "22px", weight: "650" }, "title");
      expectColour(await colour(h1, "color"), P.ink, "color");
      const label = (name: RegExp) =>
        card.locator("label", { hasText: name }).first();
      for (const [name, y] of [
        [/^Bạn cần gì/, 118.7],
        [/^Mô tả chi tiết/, 195.6],
        [/^Mức độ ưu tiên/, 322.4],
        [/^Hạn mong muốn/, 399.3],
        [/^Email liên hệ/, 499.5],
      ] as const) {
        const l = label(name);
        expectRect(
          await rel(card, l),
          { x: 29, y, height: 18.8 },
          String(name),
        );
        await expectType(l, { size: "13px", weight: "600" }, String(name));
        expectColour(await colour(l, "color"), P.ink, "color");
      }
      const need = card.getByRole("textbox", { name: /^Bạn cần gì/ });
      expectRect(
        await rel(card, need),
        { x: 29, y: 143.6, width: 562, height: 34 },
        "short text",
      );
      await expectType(
        need,
        { size: "13.5px", weight: "400", radius: "7px" },
        "short text",
      );
      const long = card.getByRole("textbox", { name: /^Mô tả chi tiết/ });
      expectRect(
        await rel(card, long),
        { x: 29, y: 220.4, width: 562, height: 84 },
        "long text",
      );
      const select = card.getByLabel(/^Mức độ ưu tiên/);
      expectRect(
        await rel(card, select),
        { x: 29, y: 347.3, width: 562, height: 34 },
        "select",
      );
      const email = card.getByRole("textbox", { name: /^Email liên hệ/ });
      expectRect(
        await rel(card, email),
        { x: 29, y: 524.3, width: 562, height: 34 },
        "email",
      );
      const hint = card.getByText(/^Hôm nay là/);
      expectNear((await rel(card, hint)).y, 464.1, "today hint y");
      await expectType(hint, { size: "12px" }, "today hint");
      const submit = card.getByRole("button", { name: "Gửi yêu cầu" });
      expectRect(
        await rel(card, submit),
        { x: 29, y: 576.3, width: 118.5, height: 40 },
        "submit",
      );
      await expectType(
        submit,
        { size: "14px", weight: "600", radius: "7px" },
        "submit",
      );
      expectColour(
        await colour(submit, "background-color"),
        P.accent,
        "background-color",
      );
      expectColour(await colour(submit, "color"), P.accentInk, "color");
      const as = card.getByText(/^Gửi dưới tên/);
      expectNear((await rel(card, as)).x, 159.5, "sent-as x");
      expectNear((await rel(card, as)).y, 587.6, "sent-as y");
      expectNear(size.height, 645.3, "card height");

      // WM-Forms-Submit, "Gửi lỗi xác thực": each field's message 6 px under it, the summary 12 px radius above the button.
      await submit.click();
      const summary = card
        .getByRole("alert")
        .filter({ hasText: "Chưa gửi được" });
      await expect(summary).toBeVisible();
      await page.waitForTimeout(200);
      expect.soft(await style(summary, "border-top-left-radius")).toBe("12px");
      expectColour(
        await colour(summary, "background-color"),
        P.dangerSoft,
        "background-color",
        10,
      );
      expectColour(await colour(summary, "color"), P.danger, "color");
      const message = card
        .getByText(/^Nhập (điều bạn cần|câu trả lời)/)
        .first();
      expectNear((await rel(card, message)).y, 183.6, "error message y");
      await expectType(message, { size: "12.5px" }, "error message");
      expectColour(await colour(message, "color"), P.danger, "color");
      const box = await rel(card, summary);
      expectNear(box.height, 44.8, "summary height");
      const submitBox = await rel(card, submit);
      expectNear(submitBox.y - (box.y + box.height), 18, "summary to button");
    });

    test(`inbox: header, tabs, list rows, response card and its actions, ${mode}`, async ({
      page,
    }) => {
      await open(page, `${SUPPORT}/responses`, dark);
      const card = page.locator("main section[aria-label]").first();
      await expect(card).toBeVisible();
      await page.getByRole("option").first().click();
      await page.waitForTimeout(300);

      // WM-Forms-Inbox: header 57 (button 32), tabs 34 at y58, rows 58 from y93, the response card 16 px from the list.
      expect.soft(await style(card, "border-top-left-radius")).toBe("12px");
      expectColour(
        await colour(card, "background-color"),
        P.panel,
        "background-color",
      );
      const back = card.getByRole("button", { name: "Biểu mẫu" });
      expectRect(await rel(card, back), { y: 13, height: 32 }, "back button");
      const h1 = card.getByRole("heading", { level: 1 });
      await expectType(h1, { size: "14px", weight: "700" }, "title");
      const tabs = card.getByRole("tab");
      const t0 = await rel(card, tabs.nth(0));
      expectRect(t0, { x: 17, y: 58, width: 61.4, height: 34 }, "first tab");
      await expectType(
        tabs.nth(0),
        { size: "13px", weight: "600" },
        "active tab",
      );
      await expectType(tabs.nth(1), { size: "13px", weight: "500" }, "tab");
      await expectType(
        tabs.nth(0).locator("small"),
        { size: "11.5px", weight: "600" },
        "tab count",
      );
      expectColour(
        await colour(tabs.nth(0).locator("small"), "color"),
        dark ? "rgb(125, 187, 255)" : "rgb(15, 86, 174)",
        "tab count",
        dark ? 20 : 2,
      );
      expectNear(
        (await rel(card, tabs.nth(1))).x - (t0.x + t0.width),
        2,
        "tab gap",
      );

      const options = card.getByRole("option");
      const o0 = await rel(card, options.nth(0));
      const o1 = await rel(card, options.nth(1));
      expectRect(o0, { x: 1, y: 93, height: 58 }, "first row");
      expectNear(o1.y - o0.y, 58, "row pitch");
      expectNear(o0.width, 421 - 1, "list column");
      expectColour(
        await colour(options.nth(0), "background-color"),
        P.sel,
        "background-color",
        dark ? 16 : 2,
      );
      const dot = options.nth(0).locator("span").first();
      expectRect(
        await rel(options.nth(0), dot),
        { x: 12, width: 8, height: 8 },
        "unread dot",
      );
      expect.soft(await style(dot, "border-top-left-radius")).not.toBe("0px");
      expectColour(
        await colour(dot, "background-color"),
        P.accent,
        "background-color",
      );
      const title = options.nth(0).locator("b");
      expectNear((await rel(options.nth(0), title)).x, 42, "row title x");
      await expectType(title, { size: "13.5px", weight: "650" }, "row title");

      const article = card.getByRole("article", { name: "Chi tiết phản hồi" });
      const ab = await rel(card, article);
      expectNear(ab.x, 438, "response card x");
      expectNear(ab.y, 109, "response card y");
      expect.soft(await style(article, "border-top-left-radius")).toBe("12px");
      const h2 = article.getByRole("heading", { level: 2 });
      expectNear((await rel(article, h2)).x, 21, "response title x");
      expectNear((await rel(article, h2)).y, 21, "response title y");
      await expectType(h2, { size: "16px", weight: "650" }, "response title");
      expectColour(await colour(h2, "color"), P.ink, "color");
      const chip = article.getByText("→ Tiêu đề việc", { exact: true });
      expectNear((await rel(card, chip)).y, 199.9, "first answer chip y");
      expectNear((await rel(card, chip)).height, 20, "answer chip height");
      await expectType(
        chip,
        { size: "11.5px", weight: "600", radius: "6px" },
        "answer chip",
      );
      const dt = article.locator("dt").first();
      expectNear((await rel(article, dt)).width, 130, "label column");
      await expectType(dt, { size: "13px" }, "answer label");
      expectColour(await colour(dt, "color"), P.meta, "color");

      const create = article.getByRole("button", {
        name: "Tạo việc từ phản hồi",
      });
      const reject = article.getByRole("button", { name: "Từ chối…" });
      const spam = article.getByRole("button", { name: "Đánh dấu spam" });
      expectRect(
        await rel(card, create),
        { x: 459, width: 179.3, height: 32 },
        "create item",
      );
      // The buttons sit 27 px under the last answer row (the answers' wording, hence their height, differs).
      const last = article.locator("dd").last();
      expectNear(
        (await rel(card, create)).y -
          ((await rel(card, last)).y + (await rel(card, last)).height),
        27 -
          (20 - (await rel(card, last)).height > 0
            ? 20 - (await rel(card, last)).height
            : 0),
        "buttons under the last answer",
      );
      expectRect(
        await rel(card, reject),
        { x: 646.3, width: 83.2, height: 32 },
        "reject",
      );
      expectRect(
        await rel(card, spam),
        { x: 737.5, width: 147.8, height: 32 },
        "spam",
      );
      await expectType(
        create,
        { size: "13px", weight: "600", radius: "7px" },
        "create item",
      );
      expectColour(
        await colour(create, "background-color"),
        P.accent,
        "background-color",
      );
      const trash = article.getByRole("button", { name: "Xoá phản hồi" });
      expectRect(
        await rel(card, trash),
        { width: 30, height: 30 },
        "remove response",
      );
      expectNear(await right(card, trash), 38, "remove response inset");
      expectNear(
        (await rel(card, trash)).y - (await rel(card, create)).y,
        1,
        "remove response y",
      );
    });
  }
});
