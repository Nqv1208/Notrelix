import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The popup's custom fields as property pills against the owner canvas board `WM-Field-Sheet` (rendered with the
 * canvas `render.py`, `mode=sang` and `mode=dem`), measured as boxes, tolerance 1 px, font sizes and radii equal.
 * The artboard draws a pill 30 px high with a 1 px border, 10 px side padding, an 8 px radius, 6 px between its
 * parts, the name (12 px) before the value (13 px), 6 px between pills and rows 36 px apart; a calculated pill
 * carries a 23 x 18 "ƒx" mark first; the read-only pill is filled and ends with a 16 px lock; "Thêm thuộc tính" is a
 * dashed pill with a 16 px plus. The failed save hangs a 300 px note 36 px under its pill (6 px under its box), the
 * version clash a 320 px one; both pad 8 px inside a 1 px border.
 * Run three ways: the default world (rest, calculated, add), `VITE_MOCK_LATENCY=slow` (saving), and
 * `VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/values/mock-erp-col-estimate=500` (the failed note);
 * `VITE_MOCK_PERSONA=viewer` draws the lock. On a phone the pill is 44 px high (the finger rule of the product
 * wins over the artboard's 30 px; the 320 px frame's sheet editor is the popup's bottom sheet). The version clash
 * note is proved by `field-pill.component.test.tsx`, because the mock world has no second writer.
 * The 320 px frame's drawer (grab handle 36 x 4, 10 px above it, the title 12 px under it, the input and the two
 * buttons 44 px, 8 px between the buttons, 16 px top radius) is measured by the last test.
 * Not measured: the contents of the option popovers (the popup's own pickers, as before).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const CARD = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";
const TOL = 1;

test.skip(scenario !== "default", "Needs the default world");

async function open(page: Page, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 1100 });
  await page.clock.setFixedTime(new Date("2026-10-12T10:40:00+07:00"));
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
  await page.goto(BOARD);
  await page.getByRole("button", { name: CARD, exact: true }).click();
  const dialog = page.getByRole("dialog", { name: CARD });
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(600);
  return dialog;
}

const near = (actual: number, want: number, what: string) =>
  expect(
    Math.abs(actual - want),
    `${what}: ${actual} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);

async function at(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

const pillOf = (dialog: Locator, fieldId: string) =>
  dialog
    .locator(`[data-field-row="${fieldId}"] > span > :is(button, span)`)
    .first();

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test.describe(`field pills, ${mode}`, () => {
    test("rest: the pill, its name, its value and its rows", async ({
      page,
    }) => {
      test.skip(persona !== "owner" || faults !== "" || latency === "slow");
      const dialog = await open(page, dark);
      const pill = pillOf(dialog, "mock-erp-col-estimate");
      await expect(pill).toBeVisible();
      const box = await pill.boundingBox();
      near(box!.height, 30, "pill height");
      const style = await pill.evaluate((el) => {
        const s = getComputedStyle(el);
        return [s.fontSize, s.borderRadius, s.borderTopWidth, s.borderTopStyle];
      });
      expect(style).toEqual(["13px", "8px", "1px", "solid"]);
      const key = pill.locator("> span").first();
      const keyBox = await at(key, pill);
      near(keyBox.x, 11, "key x");
      expect(await key.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
        "12px",
      );
      const value = pill.locator("> span").nth(1);
      near(
        (await at(value, pill)).x - (keyBox.x + keyBox.w),
        6,
        "key to value",
      );
      // Pills sit 6 px apart; a second row starts 36 px under the first.
      const first = await dialog
        .locator("[data-field-pills] > *")
        .first()
        .boundingBox();
      const pitches = await dialog
        .locator("[data-field-pills] > *")
        .evaluateAll((els) => [
          ...new Set(
            els.map((el) => Math.round(el.getBoundingClientRect().top)),
          ),
        ]);
      expect(first).not.toBeNull();
      if (pitches.length > 1) near(pitches[1]! - pitches[0]!, 36, "row pitch");
    });

    test("calculated: the ƒx mark, the filled pill, the dash when empty; the add pill is dashed", async ({
      page,
    }) => {
      test.skip(persona !== "owner" || faults !== "" || latency === "slow");
      const dialog = await open(page, dark);
      const fx = pillOf(dialog, "mock-erp-col-double");
      const mark = fx.locator("> span").first();
      const m = await at(mark, fx);
      near(m.x, 11, "ƒx x");
      near(m.w, 23.2, "ƒx width");
      near(m.h, 18, "ƒx height");
      const name = await at(fx.locator("> span").nth(1), fx);
      near(name.x - (m.x + m.w), 6, "ƒx to name");
      near((await fx.boundingBox())!.height, 30, "calculated pill height");
      expect(await fx.getAttribute("data-computed-state")).toBe("empty");
      const add = dialog.getByRole("button", { name: "Thêm thuộc tính" });
      const a = await at(add, add);
      near(a.h, 30, "add pill height");
      expect(
        await add.evaluate((el) => getComputedStyle(el).borderTopStyle),
      ).toBe("dashed");
      near((await at(add.locator("svg"), add)).x, 11, "plus x");
      near((await add.locator("svg").boundingBox())!.width, 16, "plus size");
    });

    test("saving: the pill keeps its value and ends with a spinner", async ({
      page,
    }) => {
      test.skip(persona !== "owner" || faults !== "" || latency !== "slow");
      const dialog = await open(page, dark);
      const pill = pillOf(dialog, "mock-erp-col-estimate");
      await pill.click();
      const input = page.getByRole("dialog").last().getByRole("textbox");
      await input.fill("41");
      await page.keyboard.press("Enter");
      await expect(pill.locator("svg.animate-spin")).toBeVisible();
      await expect(pill).toContainText("41");
    });

    test("failed save: the 300 px note hangs 36 px under the pill with Retry", async ({
      page,
    }) => {
      test.skip(persona !== "owner" || !faults.includes("/values/"));
      const dialog = await open(page, dark);
      const pill = pillOf(dialog, "mock-erp-col-estimate");
      await pill.click();
      await page.getByRole("dialog").last().getByRole("textbox").fill("41");
      await page.keyboard.press("Enter");
      const note = dialog.getByRole("alert").first();
      await expect(note).toBeVisible();
      const n = await at(note, pill);
      near(n.x, 0, "note x");
      near(n.y, 36, "note y");
      near(n.w, 300, "note width");
      near(n.h, 109.6, "note height");
      near(
        (await at(note.getByRole("button", { name: "Thử lại" }), note)).h,
        26,
        "retry height",
      );
      expect(
        await pill.evaluate((el) => getComputedStyle(el).borderTopColor),
      ).not.toBe("rgb(210, 215, 222)");
      await note.getByRole("button", { name: "Bỏ thay đổi" }).click();
      await expect(note).toBeHidden();
    });

    test("read-only: the filled pill ends with a 16 px lock and opens nothing", async ({
      page,
    }) => {
      test.skip(persona !== "viewer");
      const dialog = await open(page, dark);
      const pill = pillOf(dialog, "mock-erp-col-estimate");
      await expect(pill).toHaveAttribute(
        "title",
        "Chỉ xem: bạn không có quyền sửa",
      );
      const lock = pill.locator("svg").last();
      near((await lock.boundingBox())!.width, 16, "lock size");
      await expect(
        dialog.getByRole("button", { name: "Thêm thuộc tính" }),
      ).toHaveCount(0);
    });
  });
}

for (const dark of [false, true]) {
  test(`320 px: the editor is a bottom drawer with 44 px controls, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(persona !== "owner" || faults !== "" || latency === "slow");
    await page.setViewportSize({ width: 320, height: 640 });
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
    await page.goto(BOARD);
    await page.getByRole("button", { name: CARD, exact: true }).click();
    const dialog = page.getByRole("dialog", { name: CARD });
    await expect(dialog).toBeVisible();
    await page.waitForTimeout(500);
    await pillOf(dialog, "mock-erp-col-estimate").click();
    await page.waitForTimeout(500);
    const drawer = page.getByRole("dialog").last();
    const d = (await drawer.boundingBox())!;
    expect(d.width).toBe(320);
    expect(
      await drawer.evaluate((el) => getComputedStyle(el).borderRadius),
    ).toBe("16px 16px 0px 0px");
    const grab = drawer.locator("div[aria-hidden=true]").first();
    const g = (await grab.boundingBox())!;
    near(g.width, 36, "grab width");
    near(g.height, 4, "grab height");
    near(g.y - d.y, 10, "grab top");
    near(g.x + g.width / 2, 160, "grab centred");
    const input = drawer.getByRole("textbox");
    near((await input.boundingBox())!.height, 44, "input height");
    const cancel = drawer.getByRole("button", { name: "Huỷ" });
    const save = drawer.getByRole("button", { name: "Lưu" });
    const [c, v] = [(await cancel.boundingBox())!, (await save.boundingBox())!];
    near(c.height, 44, "cancel height");
    near(v.height, 44, "save height");
    near(v.x - (c.x + c.width), 8, "gap between the buttons");
    near(320 - 16 - (v.x + v.width), 0, "save at the right gutter");
    near(
      (await input.boundingBox())!.y - (g.y + g.height),
      38.8,
      "the input under the grab (12, the 13 px title, 8)",
    );
  });
}
