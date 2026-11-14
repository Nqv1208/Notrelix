import { expect, test, type Page } from "@playwright/test";

/**
 * The stale-save notice of the form builder in the design canvas artboard WM-FormConflict ("Xung đột: nêu
 * tên, ba lối xử lý"). Numbers were read from the artboard rendered with docs/design/mockups/2026-10/render.py;
 * tolerance is 1 px. Run with `VITE_MOCK_FAULTS="PATCH /forms/mock-form-purchase=409"`: the mock answers the
 * first save of the draft with a conflict that names Hà Nguyễn.
 *
 * Not measured: the "Xem bản mới" difference table. The mock world holds one copy of a form, so no second
 * person's change can be shown; the table's rows are covered by the component tests.
 */
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const FORM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/forms/mock-form-purchase/edit";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, dark: boolean) {
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
  await page.goto(FORM, { waitUntil: "domcontentloaded" });
}

function near(actual: number, want: number, label: string) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(1);
}

for (const dark of [false, true]) {
  test(`conflict notice names who saved and offers three ways out, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(
      !faults.includes("PATCH /forms/mock-form-purchase=409") ||
        persona !== "owner",
      "Needs the conflict fault",
    );
    await open(page, dark);
    const description = page.getByRole("textbox", { name: /^Mô tả/ });
    await description.fill("Mô tả mới của người soạn");
    await description.blur();
    const notice = page
      .getByRole("alert")
      .filter({ hasText: "Hà Nguyễn vừa lưu" });
    await expect(notice).toBeVisible();
    await page.waitForTimeout(250);
    const n = (await notice.boundingBox())!;
    const title = (await notice.locator("b").first().boundingBox())!;
    const buttons = {
      see: (await notice
        .getByRole("button", { name: "Xem bản mới" })
        .boundingBox())!,
      take: (await notice
        .getByRole("button", { name: "Dùng bản mới" })
        .boundingBox())!,
      keep: (await notice
        .getByRole("button", { name: "Giữ của tôi" })
        .boundingBox())!,
    };
    // .bn: padding 10 14, gap 10, radius 10; title 13 px / 600 then the two-line body; buttons 28 high, 6 apart, flush right.
    near(title.x - n.x, 40, "text x");
    near(title.y - n.y, 10, "title y");
    near(title.height, 18.2, "title height");
    for (const name of ["see", "take", "keep"] as const)
      near(buttons[name].height, 28, `${name} height`);
    near(buttons.see.width, 101.6, "see width");
    near(buttons.take.width, 106.2, "take width");
    near(buttons.keep.width, 89.1, "keep width");
    near(buttons.take.x - (buttons.see.x + buttons.see.width), 6, "gap 1");
    near(buttons.keep.x - (buttons.take.x + buttons.take.width), 6, "gap 2");
    near(
      n.x + n.width - (buttons.keep.x + buttons.keep.width),
      14,
      "right pad",
    );
    near(buttons.see.y - n.y, 10, "buttons y");
    expect(
      await notice.evaluate((e) => getComputedStyle(e).borderTopLeftRadius),
    ).toBe("10px");
    await expect(page.getByRole("button", { name: "Xuất bản" })).toBeDisabled();
  });
}
