import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * Screenshot review of the required-field notes no artboard draws, at 1440, 1024 and 390 px in light and dark, in
 * Vietnamese: the field panel's "choose a default first" note with the default input of a Number field, the table
 * cell's refusal note (from 768 px; at 390 px the table is the phone card list and its writes are in the sheet) and
 * the item popup's refusal note under the pill. Each state keeps inside the page, axe finds nothing serious, and the
 * controls in the note are 44 px at 390 px. `WM_SHOTS_DIR` writes a screenshot of each state for review.
 * The two refusals need `VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/values/mock-erp-col-estimate=required`; the
 * panel runs in the default world.
 */
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const CARD = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";

async function boot(page: Page, width: number, height: number, dark: boolean) {
  await page.setViewportSize({ width, height });
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
}

async function check(page: Page, name: string, tall: boolean, targets: string) {
  await page.waitForTimeout(400);
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth,
  );
  expect(overflow, "the page scrolls sideways").toBeLessThanOrEqual(0);
  if (shots) await page.screenshot({ path: `${shots}/${name}.png` });
  const violations = (await new AxeBuilder({ page }).analyze()).violations;
  expect(
    violations.filter((v) => ["serious", "critical"].includes(v.impact ?? "")),
  ).toEqual([]);
  // A target is 44 px when a tap 21.5 px above and below its centre still lands on it (a pseudo-element counts).
  if (tall)
    for (const target of await page.locator(targets).all()) {
      await target.evaluate((el) => el.scrollIntoView({ block: "center" }));
      const hit = await target.evaluate((el) => {
        const r = el.getBoundingClientRect();
        const x = r.left + r.width / 2;
        return [-21.5, 21.5].map((dy) => {
          const at = document.elementFromPoint(x, r.top + r.height / 2 + dy);
          return at === el || el.contains(at)
            ? true
            : `${Math.round(r.top)}+${Math.round(r.height)} hit ${at?.tagName}.${(at?.className ?? "").toString().slice(0, 40)}`;
        });
      });
      expect(
        hit,
        `${await target.evaluate((el) => el.outerHTML.slice(0, 120))} is a 44 px target`,
      ).toEqual([true, true]);
    }
}

for (const [width, height] of [
  [1440, 900],
  [1024, 768],
  [390, 844],
] as const) {
  for (const dark of [false, true]) {
    const label = `${width} ${dark ? "dark" : "light"}`;
    const file = `${width}-${dark ? "dark" : "light"}`;

    test(`the panel asks for a default before a Number field is required, ${label}`, async ({
      page,
    }) => {
      test.skip(
        scenario !== "default" || persona !== "owner" || faults !== "",
        "Needs the default owner world",
      );
      await boot(page, width, height, dark);
      await page.goto(`${BOARD}/settings?from=table&section=fields`);
      await page
        .getByRole("button", { name: "Sửa trường Ước lượng (giờ)" })
        .click();
      const panel = page.getByRole("complementary", { name: /^Sửa trường/ });
      await expect(panel).toBeVisible();
      await panel
        .getByRole("switch", { name: "Bắt buộc khi tạo việc" })
        .click();
      const note = panel.getByText(
        "Chọn giá trị mặc định trước khi bắt buộc trường này",
      );
      await expect(note).toBeVisible();
      await expect(
        panel.getByRole("button", { name: "Lưu thay đổi" }),
      ).toBeDisabled();
      await check(
        page,
        `rule-note-panel-${file}`,
        width < 768,
        "aside input, aside [role=switch]",
      );
      await panel.getByLabel("Giá trị mặc định cho việc mới").fill("8");
      await expect(note).toBeHidden();
      await expect(
        panel.getByRole("button", { name: "Lưu thay đổi" }),
      ).toBeEnabled();
    });

    test(`the table cell refuses a clear of a required field, ${label}`, async ({
      page,
    }) => {
      test.skip(
        persona !== "owner" || !faults.endsWith("=required") || width < 768,
        "Needs the required fault; the phone card list has no cell",
      );
      await boot(page, width, height, dark);
      await page.goto(`${BOARD}?view=table`);
      await page.waitForTimeout(600);
      await page
        .getByRole("button", { name: /Sửa Ước lượng.*ERP-142/ })
        .click();
      await page.getByRole("textbox").last().fill("");
      await page.keyboard.press("Enter");
      const note = page
        .getByRole("alert")
        .filter({ hasText: "Ước lượng là trường bắt buộc" });
      await expect(note).toBeVisible();
      const box = (await note.boundingBox())!;
      expect(box.x + box.width).toBeLessThanOrEqual(width);
      await check(page, `rule-note-cell-${file}`, false, "");
    });

    test(`the item popup refuses a clear under the pill, ${label}`, async ({
      page,
    }) => {
      test.skip(
        persona !== "owner" || !faults.endsWith("=required"),
        "Needs the required fault",
      );
      await boot(page, width, height, dark);
      await page.goto(BOARD);
      await page.getByRole("button", { name: CARD, exact: true }).click();
      const dialog = page.getByRole("dialog", { name: CARD });
      await expect(dialog).toBeVisible();
      await page.waitForTimeout(600);
      await dialog
        .locator(
          '[data-field-row="mock-erp-col-estimate"] > span > :is(button, span)',
        )
        .first()
        .click();
      await page.getByRole("dialog").last().getByRole("textbox").fill("");
      await page.keyboard.press("Enter");
      const note = dialog
        .getByRole("alert")
        .filter({ hasText: "Ước lượng là trường bắt buộc" });
      await expect(note).toBeVisible();
      const box = (await note.boundingBox())!;
      expect(box.x + box.width).toBeLessThanOrEqual(width);
      expect(box.x).toBeGreaterThanOrEqual(0);
      await check(
        page,
        `rule-note-pill-${file}`,
        width < 768,
        "[role=alert] button",
      );
    });
  }
}
