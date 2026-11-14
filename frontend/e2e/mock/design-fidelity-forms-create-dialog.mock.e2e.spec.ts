import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The "Tạo việc từ phản hồi" dialog of the response inbox at 1440 px against `WM-Forms-Inbox` (light) and
 * `WM-Forms-Dem` (dark), rendered with `docs/design/mockups/2026-10/render.py`. Boxes are relative to the dialog's
 * top-left; tolerance 1 px.
 *
 * Measured: the 640 px card and its 16 px radius, the header (18 px title at 22/16, the "Thêm vào" chip, the 30 px
 * close button), the prefill note, the name field (48 px) and the description field (64 px) with their "Từ:" chips,
 * and the footer buttons (44 px, right edge at the 22 px padding, 10 px apart).
 *
 * Not built, so not compared (named gap): the drawn Trạng thái, Ngày bắt đầu, quick due chips, Người làm and Nhãn
 * rows; the app has a Nhóm picker instead of the drawn status select (lead decision: group picker), so the card is
 * shorter than the artboard's 817.75 px and the footer is compared from the card's bottom edge.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const RESPONSES =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/forms/mock-form-it-support/responses";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.skip(
  persona !== "owner" ||
    scenario !== "default" ||
    faults !== "" ||
    latency !== "instant",
  "Needs the default owner world without faults or latency",
);

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function within(origin: Locator, target: Locator): Promise<Rect> {
  const [o, b] = await Promise.all([
    origin.boundingBox(),
    target.boundingBox(),
  ]);
  if (!o || !b) throw new Error("element has no box");
  return { x: b.x - o.x, y: b.y - o.y, width: b.width, height: b.height };
}

function expectRect(actual: Rect, want: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const expected = want[key];
    if (expected === undefined) continue;
    expect
      .soft(
        Math.abs(actual[key] - expected),
        `${label}.${key}: ${actual[key].toFixed(1)} vs ${expected}`,
      )
      .toBeLessThanOrEqual(1);
  }
}

const style = (locator: Locator, property: string) =>
  locator.evaluate(
    (element, name) => getComputedStyle(element).getPropertyValue(name).trim(),
    property,
  );

/** A colour property as sRGB channels. */
const rgb = (locator: Locator, property: string) =>
  locator.evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d");
    if (!context) return [0, 0, 0];
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    return Array.from(context.getImageData(0, 0, 1, 1).data.slice(0, 3));
  }, property);

async function expectColour(
  locator: Locator,
  property: string,
  want: readonly number[],
  label: string,
  tolerance = 6,
) {
  const got = await rgb(locator, property);
  want.forEach((channel, index) =>
    expect
      .soft(
        Math.abs((got[index] ?? 0) - channel),
        `${label} ${got.join(",")} vs ${want.join(",")}`,
      )
      .toBeLessThanOrEqual(tolerance),
  );
}

async function openDialog(page: Page, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 1000 });
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
  await page.goto(RESPONSES);
  await page.getByRole("option").first().click();
  await page.getByRole("button", { name: "Tạo việc từ phản hồi" }).click();
  const dialog = page.getByRole("dialog", { name: "Tạo việc từ phản hồi" });
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(300);
  return dialog;
}

const PANEL = { light: [255, 255, 255], dark: [26, 32, 40] } as const;
const PRIMARY = { light: [22, 102, 204], dark: [90, 169, 255] } as const;

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test(`create-item dialog as drawn, ${mode}`, async ({ page }) => {
    const dialog = await openDialog(page, dark);
    const card = await within(dialog, dialog);
    expectRect(card, { width: 640 }, "dialog");
    expect.soft(await style(dialog, "border-top-left-radius")).toBe("16px");
    await expectColour(dialog, "background-color", PANEL[mode], "dialog fill");

    const title = dialog.getByRole("heading", { name: "Tạo việc từ phản hồi" });
    expectRect(
      await within(dialog, title),
      { x: 22, y: 16, height: 26.1 },
      "title",
    );
    expect.soft(await style(title, "font-size")).toBe("18px");
    expect.soft(await style(title, "font-weight")).toBe("650");
    const titleBox = await within(dialog, title);
    const chip = dialog.getByText("Thêm vào Triển khai ERP");
    expectRect(
      await within(dialog, chip),
      { x: titleBox.x + titleBox.width + 10, y: 16, height: 26 },
      "board chip",
    );
    const close = dialog.getByRole("button", { name: "Đóng" });
    expectRect(
      await within(dialog, close),
      { x: 598, y: 14, width: 30, height: 30 },
      "close",
    );

    const note = dialog
      .getByText(/^Điền sẵn từ phản hồi/)
      .last()
      .locator("..");
    expectRect(
      await within(dialog, note),
      { x: 22, y: 62, width: 596 },
      "note",
    );
    expect.soft(await style(note, "border-top-left-radius")).toBe("9px");

    const name = dialog.getByRole("textbox", { name: /Tên công việc/ });
    expectRect(
      await within(dialog, name),
      { x: 22, width: 596, height: 48 },
      "name field",
    );
    expect.soft(await style(name, "font-size")).toBe("16px");
    expect.soft(await style(name, "border-top-left-radius")).toBe("9px");
    const nameLabel = dialog.getByText(/^Tên công việc/).first();
    expectRect(
      await within(dialog, nameLabel),
      { x: 22, height: 20 },
      "name label",
    );
    const source = dialog.getByText("Từ: Bạn cần gì?");
    expectRect(await within(dialog, source), { height: 20 }, "source chip");
    expect.soft(await style(source, "font-size")).toBe("11.5px");

    const description = dialog.getByRole("textbox", { name: /Mô tả/ });
    expectRect(
      await within(dialog, description),
      { x: 22, width: 596, height: 64 },
      "description",
    );
    const nameBox = await within(dialog, name);
    const descBox = await within(dialog, description);
    // Label 20 px + 6 px gap + 48 px box + 16 px gap to the next label, as the artboard stacks them.
    expectRect(descBox, { y: nameBox.y + 48 + 16 + 20 + 6 }, "description y");

    const cancel = dialog.getByRole("button", { name: "Huỷ" });
    const create = dialog.getByRole("button", { name: "Tạo công việc" });
    const cancelBox = await within(dialog, cancel);
    const createBox = await within(dialog, create);
    expectRect(createBox, { height: 44 }, "create");
    expectRect(cancelBox, { height: 44 }, "cancel");
    // The buttons end at the 22 px padding and sit 10 px apart. Their widths are not compared: the label font runs
    // about 5 percent wider than the artboard's (create 137.0 vs 131.4 px), a named difference.
    expectRect(
      { x: createBox.x + createBox.width, y: 0, width: 0, height: 0 },
      { x: 618 },
      "create right edge",
    );
    expectRect(
      { x: cancelBox.x + cancelBox.width + 10, y: 0, width: 0, height: 0 },
      { x: createBox.x },
      "button gap",
    );
    // 14 px footer padding under the buttons, a 1 px rule above them.
    expectRect(
      { x: 0, y: createBox.y + createBox.height + 14, width: 0, height: 0 },
      { y: card.height },
      "footer bottom padding",
    );
    expect.soft(await style(create, "font-weight")).toBe("600");
    await expectColour(
      create,
      "background-color",
      PRIMARY[mode],
      "create fill",
      16,
    );
  });
}
