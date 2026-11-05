import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The in-cell value conflict of the table ("keep my value" or "use the new value") against `WM-Table-Columns`,
 * slice 3 ("Xung đột phiên bản trong ô"), rendered from the owner canvas artboard (`mode=sang` and `mode=dem`) and
 * measured as boxes, tolerance 1 px. The note is 316 px wide (290 of content, 12 px side padding, 10 px top and
 * bottom, a 1 px border), a 10 px radius, 8 px between its title, its line and its buttons; the buttons are 26 px
 * high with a 7 px radius, 8 px side padding and 12 px type, 8 px apart.
 *
 * Run with `VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/values/mock-erp-col-estimate=409`: the write is refused
 * as a stale one and the refusal names who saved first.
 */
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const TABLE =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=table";
const TOL = 1;
const near = (a: number, w: number, what: string) =>
  expect(
    Math.abs(a - w),
    `${what}: ${a.toFixed(1)} vs ${w}`,
  ).toBeLessThanOrEqual(TOL);

async function refuse(page: Page, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 900 });
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
  await page.goto(TABLE);
  await page.waitForTimeout(600);
  await page.getByRole("button", { name: /Sửa Ước lượng.*ERP-142/ }).click();
  await page.getByRole("textbox").last().fill("13");
  await page.keyboard.press("Enter");
  return page.getByRole("alertdialog", { name: "Xung đột giá trị" });
}

async function rel(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

for (const dark of [false, true]) {
  test(`the value conflict note, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(persona !== "owner" || !faults.endsWith("=409"));
    const dialog = await refuse(page, dark);
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText("Hà Nguyễn vừa đổi Ước lượng");
    await page.waitForTimeout(400);
    const note = dialog.locator("xpath=..");
    const box = (await note.boundingBox())!;
    near(box.width, 316, "note width");
    near(box.height, 100.25, "note height");
    const css = await note.evaluate((el) => {
      const s = getComputedStyle(el);
      return {
        r: s.borderRadius,
        pt: s.paddingTop,
        pl: s.paddingLeft,
      };
    });
    expect(css).toEqual({ r: "10px", pt: "10px", pl: "12px" });

    const title = dialog.locator("span").nth(0);
    const mine = dialog.getByRole("button", { name: "Giữ giá trị của tôi" });
    const theirs = dialog.getByRole("button", { name: /Dùng giá trị mới/ });
    const t = await rel(title, note);
    near(t.x, 13, "title x");
    near(t.y, 11, "title y");
    near(t.h, 18.1, "title height");
    near((await rel(dialog.locator("span").nth(1), note)).y, 37.1, "line y");
    const a = await rel(mine, note);
    const b = await rel(theirs, note);
    for (const [name, r] of [
      ["keep", a],
      ["use", b],
    ] as const) {
      near(r.y, 63.25, `${name} y`);
      near(r.h, 26, `${name} height`);
    }
    near(a.x, 13, "keep x");
    near(b.x - (a.x + a.w), 8, "gap between the buttons");
    for (const button of [mine, theirs])
      expect(
        await button.evaluate((el) => {
          const s = getComputedStyle(el);
          return [s.fontSize, s.borderRadius, s.paddingLeft];
        }),
      ).toEqual(["12px", "7px", "8px"]);
    expect(
      await title.evaluate((el) => {
        const s = getComputedStyle(el);
        return [s.fontSize, s.fontWeight];
      }),
    ).toEqual(["12.5px", "600"]);
  });
}
