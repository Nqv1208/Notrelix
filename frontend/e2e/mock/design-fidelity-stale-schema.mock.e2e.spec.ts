import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The popup's notices for a field the board changed under the person (`WM-StaleSchema`, canvas 2960 wide, rendered
 * with the canvas `render.py`), light and dark. The artboard draws the notice inside a 520 px side sheet; the app's
 * popup is 1120 px, so what is measured is the notice's own geometry, read from the render: 10 px radius, padding
 * 10 px top and 14 px at the sides, a 16 px icon at x 14, the title at x 40 on a 13 px / 600 line, the body 13 px
 * in the notice's colour, and the one button 44 px high with a 7 px radius, 14 px side padding, 14 px from the
 * right edge and 10 px from the top. The notice states are: field deleted (rose, the field's pill struck with
 * "trường đã bị xoá"), option removed (amber, "Chọn lại"), type changed (amber, "Tải lại trường"), reloading (blue),
 * and the note that says what the reload changed. The guest's "Một người khác" is the same notice with no name.
 * Run once per variant: `VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/values/mock-erp-col-estimate=<fault>` with
 * `stale`, `stale-option` or `stale-type`; `VITE_MOCK_LATENCY=slow` for the reloading notice.
 * The two amber variants have no server producer yet (only the deleted field does): the mock answers them.
 * The table's cell variant is measured in `design-fidelity-stale-schema-table`. At 390 px the popup is the full screen:
 * the notice sits in the 16 px gutter, its button is 44 px high and the field rows are 44 px.
 */
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const CARD = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";
const TOL = 1;
const near = (a: number, w: number, what: string) =>
  expect(Math.abs(a - w), `${what}: ${a} vs ${w}`).toBeLessThanOrEqual(TOL);

async function at(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

async function trigger(page: Page, dark: boolean, width = 1440) {
  await page.setViewportSize({ width, height: width < 768 ? 844 : 1100 });
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
  await dialog
    .locator('[data-field-row="mock-erp-col-estimate"] button')
    .click();
  await page.getByRole("dialog").last().getByRole("textbox").fill("41");
  await page.keyboard.press("Enter");
  return dialog;
}

const kinds = [
  {
    fault: "stale",
    title: "Hà Nguyễn vừa xoá trường Ước lượng",
    button: "Tải lại trường",
    role: "alert",
    done: "Đã tải lại trường: Ước lượng không còn ở board này.",
  },
  {
    fault: "stale-option",
    title: "Hà Nguyễn vừa sửa các lựa chọn của Ước lượng",
    button: "Chọn lại",
    role: "alert",
    done: "Đã tải lại trường: các lựa chọn của Ước lượng đã cập nhật.",
  },
  {
    fault: "stale-type",
    title: "Hà Nguyễn vừa đổi loại của Ước lượng từ Number sang text",
    button: "Tải lại trường",
    role: "alert",
    done: "Đã tải lại trường: Ước lượng có loại mới.",
  },
];

for (const dark of [false, true]) {
  for (const kind of kinds) {
    test(`${kind.fault}: the notice, its button and the reload note, ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      test.skip(
        persona !== "owner" ||
          !faults.endsWith(`=${kind.fault}`) ||
          latency === "slow",
      );
      const dialog = await trigger(page, dark);
      const notice = dialog
        .getByRole("alert")
        .filter({ hasText: kind.title.split(" ").slice(0, 3).join(" ") });
      await expect(notice).toBeVisible();
      const radius = await notice.evaluate(
        (el) => getComputedStyle(el).borderRadius,
      );
      expect(radius).toBe("10px");
      const title = notice.locator("b").first();
      const t = await at(title, notice);
      near(t.x, 40, "title x");
      near(t.y, 10, "title y");
      expect(await title.evaluate((el) => getComputedStyle(el).fontSize)).toBe(
        "13px",
      );
      const icon = await at(notice.locator("svg").first(), notice);
      near(icon.x, 14, "icon x");
      near(icon.w, 16, "icon size");
      const button = notice.getByRole("button", { name: kind.button });
      const b = await at(button, notice);
      near(b.h, 44, "button height");
      near(b.y, 10, "button top");
      near(
        (await notice.boundingBox())!.width - (b.x + b.w),
        14,
        "button from the right edge",
      );
      expect(
        await button.evaluate((el) => getComputedStyle(el).borderRadius),
      ).toBe("7px");
      if (kind.fault === "stale")
        await expect(dialog.getByText("trường đã bị xoá")).toBeVisible();
      await button.click();
      await expect(
        dialog.getByRole("status").filter({ hasText: kind.done }),
      ).toBeVisible();
    });
  }
}

test("reloading: the notice says the values are kept", async ({ page }) => {
  test.skip(latency !== "slow" || !faults.endsWith("=stale"));
  test.setTimeout(120_000);
  const dialog = await trigger(page, false);
  const reload = dialog.getByRole("button", { name: "Tải lại trường" });
  await expect(reload).toBeVisible({ timeout: 30_000 });
  await reload.click();
  await expect(
    dialog.getByText("Đang tải lại các trường của board…"),
  ).toBeVisible();
  await expect(
    dialog.getByText(
      "Các giá trị bạn chưa lưu được giữ lại; chưa gửi lại thay đổi nào.",
    ),
  ).toBeVisible();
});

for (const dark of [false, true]) {
  test(`390 px: the notice in the gutter, a 44 px button, 44 px rows, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    test.skip(
      persona !== "owner" || !faults.endsWith("=stale") || latency === "slow",
    );
    const dialog = await trigger(page, dark, 390);
    const notice = dialog
      .getByRole("alert")
      .filter({ hasText: "vừa xoá trường" });
    await expect(notice).toBeVisible();
    const n = (await notice.boundingBox())!;
    near(n.x, 0 + 16, "notice x (16 px gutter)");
    near(n.width, 390 - 32, "notice width");
    const button = notice.getByRole("button", { name: "Tải lại trường" });
    near((await button.boundingBox())!.height, 44, "button height");
    expect(await notice.evaluate((el) => getComputedStyle(el).paddingTop)).toBe(
      "10px",
    );
    const row = dialog.locator('[data-field-row="mock-erp-col-estimate"]');
    near((await row.boundingBox())!.height, 44, "field row height");
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth,
      ),
    ).toBeLessThanOrEqual(0);
  });
}
