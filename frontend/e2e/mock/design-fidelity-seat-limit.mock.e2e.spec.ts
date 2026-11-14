import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The invitation dialog that stops at the seats the account owns, against the October 2026 mockup
 * (`Final-Billing.dc.html`, state `het-cho`, modes sang and dem, rendered at 1440x900 with `nav=an`). Every number
 * comes from the artboard measured with the box dump (position, size, font, radius) and from one pixel read per
 * surface; boxes are read relative to the top-left of the dialog, which sits 96 px under the top and is 560 wide.
 * Tolerance is 1 px for boxes and exact for font sizes, weights and radii; a colour is a screen pixel within 1 level
 * per channel.
 *
 * The mock world's seats are the artboard's (104 of 110, shared with Minh An Đà Nẵng), and the seven addresses are
 * the ones the artboard draws.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const MEMBERS = "/workspaces/mock-workspace-primary/members?invite=seats";
const EMAILS = [
  "lan.do@minhan.vn",
  "son.pham@minhan.vn",
  "hieu.ngo@minhan.vn",
  "trang.le@minhan.vn",
  "duc.vo@minhan.vn",
  "mai.hoang@minhan.vn",
  "nam.dinh@minhan.vn",
];

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);
test.use({ viewport: { width: 1440, height: 900 } });

async function openSeatDialog(page: Page, dark: boolean) {
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
        window.localStorage.setItem("notrelix-system-rail", "pinned");
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(MEMBERS);
  const dialog = page.getByRole("dialog", { name: /^Mời vào/ });
  await expect(dialog).toBeVisible();
  await page.locator("#inv-mail").fill(EMAILS.join(", "));
  await page.locator("#inv-mail").press("Enter");
  await expect(dialog.getByRole("alert")).toBeVisible();
  // The dialog zooms in over 100 ms and the list settles: measure at rest.
  await page.waitForTimeout(600);
  return dialog;
}

async function box(locator: Locator): Promise<Rect> {
  const found = await locator.boundingBox();
  if (!found) throw new Error("element has no box");
  return found;
}

/** A locator's box relative to the top-left of the dialog. */
async function rel(dialog: Locator, locator: Locator): Promise<Rect> {
  const [origin, found] = await Promise.all([box(dialog), box(locator)]);
  return {
    x: found.x - origin.x,
    y: found.y - origin.y,
    width: found.width,
    height: found.height,
  };
}

function expectRect(actual: Rect, expected: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const want = expected[key];
    if (want === undefined) continue;
    expect(
      Math.abs(actual[key] - want),
      `${label}.${key}: ${actual[key].toFixed(1)} vs ${want}`,
    ).toBeLessThanOrEqual(1);
  }
}

async function style(locator: Locator, property: string): Promise<string> {
  return locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );
}

/** The screen pixel at a point of the dialog, read from a 1x1 screenshot. */
async function pixel(
  page: Page,
  dialog: Locator,
  at: readonly [number, number],
): Promise<readonly [number, number, number]> {
  const origin = await box(dialog);
  const png = await page.screenshot({
    clip: { x: origin.x + at[0], y: origin.y + at[1], width: 1, height: 1 },
  });
  return page.evaluate(async (base64) => {
    const bitmap = await createImageBitmap(
      await (await fetch(`data:image/png;base64,${base64}`)).blob(),
    );
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    context.drawImage(bitmap, 0, 0);
    const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
    return [r!, g!, b!] as const;
  }, png.toString("base64"));
}

/** Pixel points of the dialog (relative to its top-left) and the artboard's colour there, per mode. */
const SURFACES = {
  panel: { at: [300, 30], sang: [255, 255, 255], dem: [26, 32, 40] },
  chip: { at: [33, 103], sang: [230, 233, 238], dem: [35, 42, 52] },
  alert: { at: [24, 330], sang: [255, 244, 214], dem: [52, 51, 44] },
  foot: { at: [300, 480], sang: [247, 248, 250], dem: [14, 17, 22] },
  send: { at: [440, 480], sang: [230, 233, 238], dem: [35, 42, 52] },
  cancel: { at: [382, 480], sang: [255, 255, 255], dem: [26, 32, 40] },
  buy: { at: [64, 410], sang: [22, 102, 204], dem: [90, 169, 255] },
  less: { at: [180, 410], sang: [255, 255, 255], dem: [26, 32, 40] },
  role: { at: [300, 292], sang: [255, 255, 255], dem: [26, 32, 40] },
  tokBorder: { at: [300, 92], sang: [210, 215, 222], dem: [46, 55, 67] },
} as const;

for (const dark of [false, true]) {
  const mode = dark ? "dem" : "sang";

  test.describe(`Final-Billing het-cho at 1440 px (${mode})`, () => {
    test("the dialog sits 96 px under the top, 560 wide, with the artboard's header and fields", async ({
      page,
    }) => {
      const dialog = await openSeatDialog(page, dark);
      const origin = await box(dialog);
      expect(Math.abs(origin.x - 440), "dialog x").toBeLessThanOrEqual(1);
      expect(Math.abs(origin.y - 96), "dialog y").toBeLessThanOrEqual(1);
      expectRect(
        { ...origin, x: 0, y: 0 },
        { width: 560, height: 523.1 },
        "dialog",
      );
      expect(await style(dialog, "border-radius")).toBe("14px");

      const title = page.locator("#inv-dlg-t");
      expectRect(
        await rel(dialog, title),
        { x: 20, y: 21, width: 207.5, height: 26.1 },
        "title",
      );
      expect(await style(title, "font-size")).toBe("18px");
      expect(await style(title, "font-weight")).toBe("650");
      const close = dialog.getByRole("button", { name: "Đóng" });
      expectRect(
        await rel(dialog, close),
        { x: 508, y: 18, width: 32, height: 32 },
        "close",
      );
      expect(await style(close, "border-radius")).toBe("7px");

      const label = page.locator('label[for="inv-mail"]');
      expectRect(
        await rel(dialog, label),
        { x: 20, y: 68, width: 520, height: 18.1 },
        "email label",
      );
      expect(await style(label, "font-size")).toBe("12.5px");
      expect(await style(label, "font-weight")).toBe("600");

      const field = page.locator("#inv-mail").locator("..");
      expectRect(
        await rel(dialog, field),
        { x: 20, y: 92.1, width: 520, height: 144 },
        "address field",
      );
      expect(await style(field, "border-radius")).toBe("8px");
      const chips = field.locator("> span");
      await expect(chips).toHaveCount(7);
      expectRect(
        await rel(dialog, chips.first()),
        { x: 29, y: 99.1, width: 154.8, height: 28 },
        "first chip",
      );
      expectRect(
        await rel(dialog, chips.nth(6)),
        { x: 29, y: 201.1, width: 173.4, height: 28 },
        "seventh chip",
      );
      expect(await style(chips.first(), "font-size")).toBe("13px");
      expect(await style(chips.first(), "font-weight")).toBe("500");
      expect(await style(chips.first(), "border-radius")).toBe("7px");
      const input = page.locator("#inv-mail");
      expectRect(
        await rel(dialog, input),
        { x: 208.4, y: 201.1, width: 322.6, height: 28 },
        "address input",
      );
      expect(await style(input, "font-size")).toBe("13.5px");
      expect(await style(input, "font-weight")).toBe("400");

      const roleLabel = page.locator('label[for="inv-role"]');
      expectRect(
        await rel(dialog, roleLabel),
        { x: 20, y: 250.1, width: 520, height: 18.1 },
        "role label",
      );
      const role = page.locator("#inv-role");
      expectRect(
        await rel(dialog, role),
        { x: 20, y: 274.3, width: 520, height: 36 },
        "role",
      );
      expect(await style(role, "font-size")).toBe("13.5px");
      expect(await style(role, "font-weight")).toBe("400");
      expect(await style(role, "border-radius")).toBe("8px");
    });

    test("the seat alert, its two actions and the footer", async ({ page }) => {
      const dialog = await openSeatDialog(page, dark);
      const alert = page.locator("#seat-msg");
      expectRect(
        await rel(dialog, alert),
        { x: 20, y: 324.3, width: 520, height: 121.8 },
        "alert",
      );
      expect(await style(alert, "border-radius")).toBe("10px");
      expect(await style(alert, "font-size")).toBe("13px");
      const strong = alert.locator("b");
      expectRect(
        await rel(dialog, strong),
        { x: 59, y: 337.3, width: 468, height: 18.8 },
        "alert title",
      );
      expect(await style(strong, "font-weight")).toBe("650");
      const small = alert.locator("small");
      expectRect(
        await rel(dialog, small),
        { x: 59, y: 359.1, width: 468, height: 36 },
        "alert detail",
      );
      expect(await style(small, "font-size")).toBe("12px");
      const buy = alert.getByRole("link", { name: "Mua thêm chỗ" });
      expectRect(
        await rel(dialog, buy),
        { x: 59, y: 405.1, width: 109.3, height: 28 },
        "buy",
      );
      expect(await style(buy, "font-size")).toBe("12.5px");
      expect(await style(buy, "font-weight")).toBe("600");
      expect(await style(buy, "border-radius")).toBe("7px");
      const less = alert.getByRole("button", { name: "Bớt người" });
      expectRect(
        await rel(dialog, less),
        { x: 176.3, y: 405.1, width: 81.2, height: 28 },
        "trim",
      );
      expect(await style(less, "font-weight")).toBe("500");

      const cancel = dialog.getByRole("button", { name: "Huỷ" });
      const send = dialog.getByRole("button", { name: "Gửi 7 lời mời" });
      const foot = cancel.locator("..");
      expectRect(
        await rel(dialog, foot),
        { x: 0, y: 462.1, width: 560, height: 61 },
        "footer",
      );
      expectRect(
        await rel(dialog, foot.locator("small")),
        { x: 20, y: 484.4, width: 113.9, height: 17.4 },
        "footer note",
      );
      expect(await style(foot.locator("small"), "font-size")).toBe("12px");
      expectRect(
        await rel(dialog, cancel),
        { x: 378.1, y: 477.1, width: 50.4, height: 32 },
        "cancel",
      );
      expectRect(
        await rel(dialog, send),
        { x: 436.5, y: 477.1, width: 103.5, height: 32 },
        "send",
      );
      expect(await style(send, "font-size")).toBe("13px");
      expect(await style(send, "font-weight")).toBe("500");
      expect(await style(send, "border-radius")).toBe("7px");
      await expect(send).toBeDisabled();
    });

    test("the surfaces carry the artboard's colours", async ({ page }) => {
      const dialog = await openSeatDialog(page, dark);
      for (const [name, surface] of Object.entries(SURFACES)) {
        const want = surface[mode];
        const tolerance = "tol" in surface ? surface.tol : 1;
        const got = await pixel(page, dialog, surface.at);
        for (const channel of [0, 1, 2] as const) {
          expect(
            Math.abs(got[channel] - want[channel]),
            `${name} ${mode}: rgb(${got.join(", ")}) vs rgb(${want.join(", ")})`,
          ).toBeLessThanOrEqual(tolerance);
        }
      }
    });
  });
}
