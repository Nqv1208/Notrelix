import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The form builder as a view of the board settings, against the design canvas artboard WMB-Forms-Builder
 * (1440 px) and WMB-Forms-Builder-T (1024 px), rendered with docs/design/mockups/2026-10/render.py:
 * the drag frame (scn keo), the empty label frame (nhan_trong), the same-field frame (trung_truong) and the
 * preview dialog (xem_truoc), in light and dark. Tolerance is 1 px.
 *
 * The artboard frame holds the builder alone while the app adds the board shell, so positions are read
 * inside the question card, the settings card or the dialog. The mock's form "Yêu cầu hỗ trợ IT" is
 * published (the artboard's is a draft), so the published notice sits above the description; only boxes
 * inside the cards are compared. The deleted-field frame (truong_xoa) needs a board field deleted by
 * another person and is not driven here. The artboards draw no 390 px builder frame: the phone layout of
 * the builder is a screenshot review, not a measurement.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const FORM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/settings?section=forms&form=mock-form-it-support";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" ||
    scenario !== "default" ||
    faults !== "" ||
    latency !== "instant",
  "Needs the default owner world without faults or latency",
);

async function open(page: Page, dark: boolean, width: number) {
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
  await page.getByRole("textbox", { name: "Tên biểu mẫu" }).waitFor();
  await page.waitForTimeout(250);
}

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function box(locator: Locator): Promise<Rect> {
  const b = await locator.boundingBox();
  if (!b) throw new Error("element has no box");
  return b;
}

/** A box relative to the top-left of `frame`. */
async function rel(frame: Locator, locator: Locator): Promise<Rect> {
  const [o, b] = await Promise.all([box(frame), box(locator)]);
  return { x: b.x - o.x, y: b.y - o.y, width: b.width, height: b.height };
}

function near(actual: number, want: number, label: string, tol = 1) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(tol);
}

async function type(locator: Locator) {
  return locator.evaluate((el) => {
    const s = getComputedStyle(el);
    return {
      size: s.fontSize,
      weight: s.fontWeight,
      radius: s.borderTopLeftRadius,
    };
  });
}

async function expectType(
  locator: Locator,
  want: { size?: string; weight?: string; radius?: string },
  label: string,
) {
  const got = await type(locator);
  for (const key of ["size", "weight", "radius"] as const) {
    const w = want[key];
    if (w !== undefined) expect.soft(got[key], `${label} ${key}`).toBe(w);
  }
}

function parts(page: Page) {
  const list = page.getByRole("list", { name: "Câu hỏi" });
  // The question card is the list's parent; the settings card is the aside beside it.
  const card = list.locator("xpath=..");
  const panel = page.getByRole("complementary", { name: "Thiết lập câu hỏi" });
  const rows = list.locator("li > div");
  return { list, card, panel, rows };
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test(`question card and settings card, 1440, ${mode}`, async ({ page }) => {
    await open(page, dark, 1440);
    const { card, panel, rows } = parts(page);
    await rows
      .first()
      .getByRole("button", { name: /^Bạn cần gì/ })
      .click();
    await expect(panel.getByRole("textbox", { name: /^Nhãn/ })).toBeVisible();
    await page.waitForTimeout(250);

    // Card: radius 12, padding 12, 6 px gap; header 36 high; rows 52 high, 58 apart, arrows 30 x 24.
    await expectType(card, { radius: "12px" }, "question card");
    const c = await box(card);
    const heading = card.getByRole("heading", { name: /^Câu hỏi/ });
    const h = await rel(card, heading);
    near(h.x, 17, "heading x");
    near(h.y, 18.8, "heading y");
    await expectType(heading, { size: "14px", weight: "700" }, "heading");
    const add = card.getByRole("button", { name: "Thêm câu hỏi" });
    const a = await rel(card, add);
    near(a.y, 15, "add y");
    near(a.height, 28, "add height");
    near(c.width - (a.x + a.width), 17, "add right inset");
    await expectType(
      add,
      { size: "12.5px", weight: "500", radius: "7px" },
      "add",
    );

    const r1 = await rel(card, rows.nth(0));
    const r2 = await rel(card, rows.nth(1));
    near(r1.y, 55, "row 1 y");
    near(r2.y - r1.y, 58, "row pitch");
    near(r1.x, 13, "row x");
    near(c.width - (r1.x + r1.width), 13, "row right inset");
    const row = rows.nth(0).locator("> div").first();
    const rb = await rel(card, row);
    near(rb.height, 52, "row height");
    near(rb.width, c.width - 26 - 34, "row width (card less arrows)");
    await expectType(row, { radius: "8px" }, "row");
    const name = rows.nth(0).locator("b");
    const sub = rows.nth(0).locator("small");
    await expectType(name, { size: "13.5px", weight: "600" }, "row name");
    await expectType(sub, { size: "12px", weight: "400" }, "row sub");
    near((await rel(card, name)).x, 74, "name x");
    near((await rel(card, name)).y - r1.y, 7.5, "name y");
    near((await rel(card, sub)).y - r1.y, 27.1, "sub y");
    const num = rows.nth(0).locator("span").filter({ hasText: /^1$/ });
    near((await rel(card, num)).x, 50, "number x");
    const chip = rows.nth(0).getByText("Bắt buộc", { exact: true });
    const cp = await rel(card, chip);
    near(cp.height, 22, "chip height");
    near(cp.width, 70.5, "chip width", 6);
    near(
      row.evaluate ? rb.x + rb.width - (cp.x + cp.width) : 0,
      11,
      "chip right",
    );
    await expectType(
      chip,
      { size: "12px", weight: "600", radius: "6px" },
      "chip",
    );
    const arrows = rows.nth(0).getByRole("button", { name: /^Chuyển/ });
    expect(await arrows.count()).toBe(2);
    const up = await rel(card, arrows.nth(0));
    const down = await rel(card, arrows.nth(1));
    near(up.width, 30, "arrow width");
    near(up.height, 24, "arrow height");
    near(down.y - up.y, 24, "arrow pitch");
    near(up.x - (rb.x + rb.width), 4, "arrow gap");

    // Settings card: 14 px beside the question card, 418 wide, radius 12, padding 16.
    await expectType(panel, { radius: "12px" }, "settings card");
    const p = await box(panel);
    near(p.x - (c.x + c.width), 14, "gap between cards");
    near(p.width, 417.7, "settings card width");
    const title = panel.getByRole("heading", { name: /^Câu hỏi 1/ });
    near((await rel(panel, title)).x, 17, "panel title x");
    near((await rel(panel, title)).y, 17, "panel title y");
    await expectType(title, { size: "14px", weight: "700" }, "panel title");
    const label = panel.getByRole("textbox", { name: /^Nhãn/ });
    const l = await rel(panel, label);
    near(l.y, 73.4, "label field y");
    near(l.height, 34, "label field height");
    near(l.width, p.width - 34, "label field width");
    await expectType(label, { size: "13.5px", radius: "7px" }, "label field");
    near(p.height, 529.7, "settings card height");
  });

  test(`drag line, 1440, ${mode}`, async ({ page }) => {
    await open(page, dark, 1440);
    const { card, list } = parts(page);
    // A scripted drag: the browser's own drag session needs a pointer the mock cannot always give.
    const fire = (from: number, to: number | null, type: string) =>
      list.evaluate(
        (ul, [i, kind]) => {
          const el = [...ul.children][i as number]!.firstElementChild!;
          el.dispatchEvent(
            new DragEvent(kind as string, {
              bubbles: true,
              cancelable: true,
              dataTransfer: new DataTransfer(),
            }),
          );
        },
        [to ?? from, type] as const,
      );
    await fire(0, null, "dragstart");
    await fire(0, 3, "dragover");
    const line = card.locator("li > div[aria-hidden='true']");
    await expect(line).toHaveCount(1);
    await page.waitForTimeout(150);
    const lb = await box(line);
    const w = await box(list.locator("li").nth(3).locator("> div").last());
    // 2 px bar, 28 px in from the left of the row and 40 px short of its right end.
    near(lb.height, 2, "line height");
    near(lb.x - w.x, 28, "line x");
    near(w.x + w.width - (lb.x + lb.width), 40, "line right");
    await expectType(line, { radius: "2px" }, "line");
  });

  test(`empty label error, 1440, ${mode}`, async ({ page }) => {
    await open(page, dark, 1440);
    const { card, panel, rows } = parts(page);
    await rows
      .first()
      .getByRole("button", { name: /^Bạn cần gì/ })
      .click();
    const label = panel.getByRole("textbox", { name: /^Nhãn/ });
    await label.fill("");
    await label.blur();
    await page.waitForTimeout(250);
    const error = panel.getByText(/Nhập nhãn|không được trống|Nhãn câu hỏi/, {
      exact: false,
    });
    await expect(panel.getByRole("alert")).toBeVisible();
    // The label field grows by the 12.5 px message (18.1 high) and its 6 px gap: 58.1 -> 82.3.
    const l = await rel(panel, label);
    near(l.y, 73.4, "label field y");
    const alert = panel.getByRole("alert").first();
    const al = await rel(panel, alert);
    near(al.y - (l.y + l.height), 6, "message gap");
    near(al.height, 18.1, "message height");
    await expectType(alert, { size: "12.5px" }, "message");
    void error;
    // The row of the question reads "(chưa có nhãn)" and the panel keeps its place beside the card.
    await expect(rows.first()).toContainText("(chưa có nhãn)");
    near((await box(panel)).height, 553.9, "settings card height");
    near((await box(card)).height, 352, "question card height");
  });
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test(`same-field chips, 1440, ${mode}`, async ({ page }) => {
    await open(page, dark, 1440);
    const { panel, rows } = parts(page);
    await page.getByRole("button", { name: "Thêm câu hỏi" }).click();
    await page.getByRole("button", { name: /^Chọn một/ }).click();
    await expect(rows).toHaveCount(6);
    await rows.nth(5).getByRole("button").first().click();
    await panel.locator("select").selectOption({ index: 2 });
    await page.waitForTimeout(300);
    // Both questions mapped to Ưu tiên carry the 22 px danger chip, 12 px / 600, radius 6, beside the required chip.
    for (const index of [2, 5]) {
      const chip = rows.nth(index).getByText("Trùng trường", { exact: true });
      await expect(chip).toBeVisible();
      const c = await box(chip);
      near(c.height, 22, `chip ${index} height`);
      near(c.width, 93.2, `chip ${index} width`, 6);
      await expectType(
        chip,
        { size: "12px", weight: "600", radius: "6px" },
        `chip ${index}`,
      );
    }
    const first = await box(rows.nth(2).getByText("Bắt buộc", { exact: true }));
    const second = await box(
      rows.nth(2).getByText("Trùng trường", { exact: true }),
    );
    near(second.x - (first.x + first.width), 10, "chip gap");
  });
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test(`preview dialog, 1440, ${mode}`, async ({ page }) => {
    await open(page, dark, 1440);
    await page.getByRole("button", { name: "Xem trước" }).click();
    const dialog = page.getByRole("dialog", { name: "Xem trước biểu mẫu" });
    await expect(dialog).toBeVisible();
    await page.waitForTimeout(350);
    const d = await box(dialog);
    near(d.width, 660, "dialog width");
    await expectType(dialog, { radius: "14px" }, "dialog");
    near(d.x + d.width / 2, 720, "dialog centred", 1);
    const title = dialog.getByRole("heading", { name: "Xem trước biểu mẫu" });
    const t = await rel(dialog, title);
    near(t.x, 23, "title x");
    near(t.y + t.height / 2, 37, "title centre", 1.5);
    near(t.height, 24.6, "title height");
    await expectType(title, { size: "17px", weight: "650" }, "title");
    const close = dialog.getByRole("button", { name: "Đóng xem trước" });
    const cl = await rel(dialog, close);
    near(cl.height, 28, "close height");
    near(d.width - (cl.x + cl.width), 23, "close right inset");
    const notice = dialog.getByRole("status");
    const n = await rel(dialog, notice);
    near(n.x, 23, "notice x");
    near(n.width, 614, "notice width");
    near(n.height, 38.8, "notice height");
    near(n.y, 65, "notice y");
    await expectType(notice, { size: "13px", radius: "10px" }, "notice");
    const label = dialog.locator("label").first();
    await expectType(label, { size: "13.5px", weight: "600" }, "label");
    const input = dialog.getByRole("textbox").first();
    const i = await rel(dialog, input);
    near(i.height, 34, "input height");
    near(i.width, 614, "input width");
    const submit = dialog.getByRole("button", { name: "Gửi yêu cầu" });
    const s = await rel(dialog, submit);
    near(s.height, 40, "submit height");
    near(s.x, 23, "submit x");
    await expectType(submit, { radius: "7px" }, "submit");
    const as = dialog.getByText(/^Gửi dưới tên/);
    await expect(as).toBeVisible();
    const a = await rel(dialog, as);
    near(a.x, 23, "as-user line x");
    expect(a.y).toBeGreaterThan(s.y + s.height);
    near(d.height, 679.7, "dialog height", 12);
    await page.keyboard.press("Escape");
    await expect(dialog).toBeHidden();
  });
}

test("1024: the cards stack and keep the drawn padding", async ({ page }) => {
  await open(page, false, 1024);
  const { card, panel, rows } = parts(page);
  await rows
    .first()
    .getByRole("button", { name: /^Bạn cần gì/ })
    .click();
  await page.waitForTimeout(250);
  const c = await box(card);
  const p = await box(panel);
  // Below 1280 px the settings card goes under the question card, as drawn at 1024.
  expect(p.y).toBeGreaterThan(c.y + c.height - 1);
  near(p.y - (c.y + c.height), 14, "stack gap");
  near(p.width, c.width, "cards equal width");
  const row = rows.nth(0).locator("> div").first();
  near((await box(row)).height, 52, "row height");
  await expectType(card, { radius: "12px" }, "question card");
});
