import { expect, test, type Page } from "@playwright/test";

/**
 * The dark theme of the board table against `Final-Table` (the canvas draws every state in `sang` and `dem`).
 * The artboard's dark render has the identical box tree to its light one: rendered with
 * `render.py Final-Table.dc.html out.html state=<state> mode=sang|dem nav=an` for `sua-o` and `chon-nhieu`, all
 * 592 and 602 elements sit at the same x, y, width and height in both themes (read in Chromium at 1440x900).
 * The light geometry is proven against the artboard to 1 px by `design-fidelity-table.mock.e2e.spec.ts`, so the
 * dark page is proven by holding every box of the table surface to its light twin within 0.5 px. Colours are
 * not compared here: dark colours belong to the token set (`check:design-tokens`).
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
test.use({ viewport: { width: 1440, height: 900 } });

type Boxes = [string, number, number, number, number][];

async function boxes(
  page: Page,
  dark: boolean,
  state: "default" | "status-list" | "selected",
): Promise<Boxes> {
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
  await page.goto(`${BOARD}?view=table`);
  await expect(page.getByRole("rowgroup").first()).toBeVisible();
  if (state === "status-list") {
    await page.getByRole("button", { name: "Đổi trạng thái của ERP-144" }).click();
    await expect(page.getByRole("listbox")).toBeVisible();
  }
  if (state === "selected") {
    await page.getByRole("checkbox", { name: "Chọn ERP-142" }).click();
    await page.getByRole("checkbox", { name: "Chọn ERP-146" }).click();
    await expect(page.getByRole("button", { name: "Lưu trữ" })).toBeVisible();
  }
  await page.waitForTimeout(700);
  return page.evaluate(() =>
    [...document.querySelectorAll("main *, [role=listbox], [role=listbox] *")]
      .filter((element) => !["SCRIPT", "STYLE"].includes(element.tagName))
      .map((element): [string, number, number, number, number] => {
        const r = element.getBoundingClientRect();
        return [element.tagName, r.x, r.y, r.width, r.height];
      }),
  );
}

for (const state of ["default", "status-list", "selected"] as const) {
  test(`dark keeps every box of the table surface where light has it: ${state}`, async ({
    browser,
  }) => {
    const light = await boxes(await (await browser.newContext({ timezoneId: "Asia/Ho_Chi_Minh" })).newPage(), false, state);
    const dark = await boxes(await (await browser.newContext({ timezoneId: "Asia/Ho_Chi_Minh" })).newPage(), true, state);
    expect(dark.length, "elements").toBe(light.length);
    expect(light.length).toBeGreaterThan(100);
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
