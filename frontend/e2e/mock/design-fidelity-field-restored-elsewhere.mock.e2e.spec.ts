import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The note under a deleted field that someone else restored first, against `WM-DeletedFields` (1440, light and
 * dark): "Hà Nguyễn vừa khôi phục trường này. Danh sách đã làm mới." The server answers a restore of an active field
 * with who restored it (the field's last writer, by the `ActorNotice` display-name rule); the row stays with the
 * amber note, 12.5 px / 500, a 13 px info icon 2 px down and 43 px in, no buttons, no red row. Numbers were read from
 * the artboard: the row is 107.8 px (the note at 79.1, 18.8 high, 10 px above the row's end), the icon at x 60, the
 * text at x 81. Tolerance 1 px.
 *
 * Needs `VITE_MOCK_OVERLAYS=field-restored-elsewhere` (the four deleted fields; restoring "Ngày bàn giao" finds it
 * already live) and the page clock of 31 Oct 10:40.
 */
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-31T10:40:00+07:00");
const TOL = 1;

test.skip(
  !overlays.includes("field-restored-elsewhere") ||
    persona !== "owner" ||
    scenario !== "default" ||
    faults !== "",
  "Needs the owner world with the field-restored-elsewhere overlay",
);
test.use({ viewport: { width: 1440, height: 900 } });

async function open(page: Page, dark: boolean) {
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
  await page.goto(`${BOARD}/settings?from=table&section=fields`);
  await page.getByRole("tab", { name: /Đã xoá/ }).click();
  await page.waitForTimeout(500);
}

async function within(origin: Locator, target: Locator) {
  const [o, b] = await Promise.all([
    origin.boundingBox(),
    target.boundingBox(),
  ]);
  if (!o || !b) throw new Error("element is not laid out");
  return { x: b.x - o.x, y: b.y - o.y, width: b.width, height: b.height };
}

function near(actual: number, want: number, label: string) {
  expect(
    Math.abs(actual - want),
    `${label}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);
}

for (const dark of [false, true]) {
  test(`the restored-by note under its row (${dark ? "dem" : "sang"})`, async ({
    page,
  }) => {
    await open(page, dark);
    const row = page.getByRole("row").filter({ hasText: "Ngày bàn giao" });
    await row.getByRole("button", { name: /^Khôi phục/ }).click();
    const note = row.getByRole("status");
    await expect(note).toHaveText(
      "Hà Nguyễn vừa khôi phục trường này. Danh sách đã làm mới.",
    );
    // The refreshed list no longer holds the field; its row stays with the note.
    await expect(row).toHaveCount(1);

    // The artboard draws it as the first row (107.8 px, no line above); here it is the second, so its 1 px line counts.
    near((await row.boundingBox())!.height, 108.8, "row height");
    const box = await within(row, note);
    near(box.x, 17, "note x");
    near(box.y, 79.1, "note y");
    near(box.height, 18.8, "note height");
    // The app's table is 8 px wider than the artboard's 916: the note keeps its 15 px from the row's right edge.
    near(
      (await row.boundingBox())!.width - (box.x + box.width),
      15,
      "note right gap",
    );
    const icon = await within(row, note.locator("svg"));
    near(icon.x, 60, "icon x");
    near(icon.y, 81.1, "icon y");
    near(icon.width, 13, "icon width");
    const text = await within(row, note.locator("span"));
    near(text.x, 81, "text x");
    expect(
      await note.evaluate((e) => {
        const s = getComputedStyle(e);
        return { fs: s.fontSize, fw: s.fontWeight };
      }),
    ).toEqual({ fs: "12.5px", fw: "500" });

    // Amber text: the design token `warning-text`, the colour of the 1-day chip (the artboard draws #8A5A00 and, in
    // the dark, #F2C14E; the token is the app's amber, so the colour is held to the token, not the hex). Not the
    // refusal's red, and the row keeps its own ground.
    const amber = page
      .getByRole("row")
      .filter({ hasText: "Mã hợp đồng" })
      .getByText("Còn 1 ngày");
    expect(await note.evaluate((e) => getComputedStyle(e).color)).toBe(
      await amber.evaluate((e) => getComputedStyle(e).color),
    );
    expect(await row.evaluate((e) => getComputedStyle(e).backgroundColor)).toBe(
      "rgba(0, 0, 0, 0)",
    );
    await expect(row.getByRole("alert")).toHaveCount(0);
  });
}
