import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The four rule groups of the field panel against the October 2026 mockup `Final-Settings`, state `truong`
 * (the panel of "Mức ưu tiên", rendered at 1440x900 with `nav=an` and measured with the box dump): the default
 * value select and its note, the required row with its switch, the guidance text area and "Đang được dùng ở".
 * Boxes are read relative to the panel body (the scroll area under the header), whose top is the artboard's y 0.
 *
 * The mock world's field is `Ưu tiên` (the artboard's `Mức ưu tiên`) with the artboard's rules saved in its
 * settings: required, the guidance sentence and Trung bình as the default. Its usage rows come from the saved
 * views the page already has; the mock world's views do not touch this field, so only the label and the empty line
 * are measured on it. The usage rows are measured on `Sẵn sàng bàn giao`, which the Kanban view shows on its card:
 * a 32 px row (radius 7, 13 px name, 9 px gap, 14 px icon at x 8, the 12 px "how" 8 px from the right edge) that
 * starts 24.1 px under the label, against `Final-Settings` `truong` rendered in Chromium.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function open(page: Page, dark: boolean, field = "Ưu tiên") {
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
  await page.getByText(field, { exact: true }).first().click();
  const panel = page.getByRole("complementary", { name: /^Sửa trường/ });
  await expect(panel).toBeVisible();
  await page.waitForTimeout(400);
  return panel.getByLabel("Tên hiển thị").locator("xpath=../..");
}

async function rel(locator: Locator, origin: Locator): Promise<Rect> {
  const [e, o] = await Promise.all([
    locator.boundingBox(),
    origin.boundingBox(),
  ]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, width: e.width, height: e.height };
}

async function style(locator: Locator) {
  return locator.evaluate((element) => {
    const s = getComputedStyle(element);
    return { fs: s.fontSize, fw: s.fontWeight, r: s.borderRadius };
  });
}

function box(actual: Rect, want: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const value = want[key];
    if (value === undefined) continue;
    expect(
      Math.abs(actual[key] - value),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${value}`,
    ).toBeLessThanOrEqual(TOL);
  }
}

test.describe("Field panel rule groups (Final-Settings truong), 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`default value, required, guidance and usage (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      const body = await open(page, dark);

      const defaultLabel = body.getByText("Giá trị mặc định cho việc mới", {
        exact: true,
      });
      box(
        await rel(defaultLabel, body),
        { x: 18, y: 565.5, width: 364, height: 18.1 },
        "default label",
      );
      expect(await style(defaultLabel)).toMatchObject({
        fs: "12.5px",
        fw: "600",
      });
      const select = body.getByRole("combobox", {
        name: "Giá trị mặc định cho việc mới",
      });
      box(
        await rel(select, body),
        { x: 18, y: 589.6, width: 364, height: 34 },
        "default select",
      );
      expect(await style(select)).toMatchObject({ fs: "13.5px", r: "7px" });
      await expect(select).toContainText("Trung bình");
      const note = body.getByText("Chỉ áp dụng cho việc tạo từ bây giờ", {
        exact: false,
      });
      const callout = note.locator("xpath=..");
      box(
        await rel(callout, body),
        { x: 18, y: 629.6, width: 364, height: 59.5 },
        "default note",
      );
      expect((await style(callout)).r).toBe("8px");

      // Required: a 36.2 px row (13 px title in an 18.8 px line, the 12 px hint in 17.4 px), the 34 x 20 switch
      // flush right and centred in the row.
      const title = body.getByText("Bắt buộc khi tạo việc", { exact: true });
      const row = title.locator("xpath=../..");
      box(
        await rel(row, body),
        { x: 18, y: 705.1, width: 364, height: 36.2 },
        "required row",
      );
      box(await rel(title, body), { x: 18, height: 18.8 }, "required title");
      const hint = row.locator("small");
      box(
        await rel(hint, body),
        { x: 18, y: 723.9, width: 320, height: 17.4 },
        "required hint",
      );
      expect(await style(hint)).toMatchObject({ fs: "12px" });
      const toggle = row.getByRole("switch");
      box(
        await rel(toggle, body),
        { x: 348, y: 713.2, width: 34, height: 20 },
        "required switch",
      );
      await expect(toggle).toBeChecked();

      const guidanceLabel = body.getByText("Hướng dẫn cho người điền", {
        exact: true,
      });
      box(
        await rel(guidanceLabel, body),
        { x: 18, y: 757.3, width: 364, height: 18.1 },
        "guidance label",
      );
      const guidance = body.getByRole("textbox", {
        name: "Hướng dẫn cho người điền",
      });
      box(
        await rel(guidance, body),
        { x: 18, y: 781.4, width: 364, height: 58 },
        "guidance",
      );
      expect(await style(guidance)).toMatchObject({ fs: "13.5px", r: "7px" });
      await expect(guidance).toHaveValue(/go-live 01\/11/);

      const usageLabel = body.getByText("Đang được dùng ở", { exact: true });
      box(
        await rel(usageLabel, body),
        { x: 18, y: 855.4, width: 364, height: 18.1 },
        "usage label",
      );
      // The mock world's views do not touch this field, so the empty line is what is drawn: 12 px in a
      // 17.4 px line right under the label (the artboard's rows start 6 px lower, at 879.6).
      const none = body.getByText("Chưa được dùng ở đâu.", { exact: true });
      box(
        await rel(none, body),
        { x: 18, y: 879.5, width: 364, height: 17.4 },
        "usage none",
      );
      expect(await style(none)).toMatchObject({ fs: "12px" });
    });
  }
  for (const dark of [false, true]) {
    test(`usage rows (${dark ? "dem" : "sang"})`, async ({ page }) => {
      const body = await open(page, dark, "Sẵn sàng bàn giao");
      const label = body.getByText("Đang được dùng ở", { exact: true });
      const l = await rel(label, body);
      const row = body.getByRole("listitem").filter({ hasText: "Kanban" });
      await expect(row).toHaveCount(1);
      const r = await rel(row, body);
      box(r, { x: 18, width: 364, height: 32 }, "usage row");
      expect(Math.abs(r.y - (l.y + 24.125))).toBeLessThanOrEqual(TOL);
      expect(await style(row)).toMatchObject({ fs: "13px", r: "7px" });
      expect(await row.evaluate((el) => getComputedStyle(el).gap)).toBe("9px");
      const how = row.getByText("hiện trên thẻ", { exact: true });
      expect(await style(how)).toMatchObject({ fs: "12px" });
      const h = await rel(how, row);
      expect(Math.abs(r.width - 8 - (h.x + h.width))).toBeLessThanOrEqual(TOL);
      const icon = await rel(row.locator("svg"), row);
      box(icon, { x: 8, width: 14 }, "usage icon");
    });
  }
});
