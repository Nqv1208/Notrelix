import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The move-board dialog's write states against `WM-MoveBoard` (`src/WM-MoveBoard.dc.html`), frames 6 (someone else
 * moved the board first), 7 (the move is on its way), 9 (the person may not move it) and 11 (the move is done, with
 * Hoàn tác), light and dark, at 1440 px. Boxes are relative to the dialog's top-left and were read from the artboard
 * rendered with `render.py`; tolerance is 1 px. Fills are compared as sRGB within 16 per channel (the app's tokens
 * are oklch and round the artboard's tones).
 *
 * The mock world gets each state from a flag a spec's init script sets: `__mockMoveRace` (the next move is stale,
 * the board is already in Marketing and the 412 names Hà Nguyễn), `__mockMoveHold` (the move waits until the flag is
 * cleared), `__mockMoveDenied` (403) and `__mockMovePrivate` (the move impact says the board is Private).
 *
 * Not compared: the artboard's frame 7 adds three people (the mock world's Kế toán impact strands three, so the
 * wording matches, but the places above the note follow the mock world's seven Spaces, so only the note's own box
 * and the footer are measured, relative to the footer); the toast of frame 11 is the shared toast, so only its words
 * and its Hoàn tác are checked; the Night bands keep the shared brand-subtle and warning-subtle tokens where the artboard
 * composites a 16 % and a 14 % wash (as `design-fidelity-move-board` skips the dark washes); the place reads "Phòng Marketing", the app's name for a department;
 * frame 5 sits under the mock world's seven places (the artboard's four), so only its x, width and
 * type are compared; frame 10 (the board menu entry) is the shared menu.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const SETTINGS =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/settings?from=table&section=general";

test.skip(
  persona !== "owner" ||
    scenario !== "default" ||
    faults !== "" ||
    latency !== "instant",
  "Needs the default owner world without faults or latency",
);
test.use({ viewport: { width: 1440, height: 900 } });

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

type Flag =
  | "__mockMoveRace"
  | "__mockMoveHold"
  | "__mockMoveDenied"
  | "__mockMovePrivate";

async function openDialog(page: Page, dark: boolean, flag?: Flag) {
  await page.addInitScript(
    ([mode, name]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode ?? "light");
        window.localStorage.setItem("notrelix-system-rail", "pinned");
      } catch {
        // Private mode: the defaults apply.
      }
      if (name) (globalThis as Record<string, unknown>)[name] = true;
    },
    [dark ? "dark" : "light", flag ?? ""] as const,
  );
  await page.goto(SETTINGS);
  await page.getByRole("button", { name: "Chuyển sang phòng khác" }).click();
  const dialog = page.locator('[data-f="mdlg"]');
  await expect(dialog).toBeVisible();
  return dialog;
}

async function pickAndMove(page: Page, place: RegExp) {
  await page.getByRole("radio", { name: place }).click();
  await expect(page.locator('[data-f="sections"]')).toBeVisible();
  await page.waitForTimeout(300);
  await page.locator('[data-f="mgo"]').click();
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

/** A colour as sRGB over the dialog's panel (a translucent dark wash is composited first). */
const rgb = (locator: Locator, property: string) =>
  locator.evaluate((element, name) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d");
    if (!context) return [0, 0, 0];
    const panel = element.closest('[data-f="mdlg"]');
    context.fillStyle = panel
      ? getComputedStyle(panel).backgroundColor
      : "#fff";
    context.fillRect(0, 0, 1, 1);
    context.fillStyle = getComputedStyle(element).getPropertyValue(name);
    context.fillRect(0, 0, 1, 1);
    return Array.from(context.getImageData(0, 0, 1, 1).data.slice(0, 3));
  }, property);

async function expectColour(
  locator: Locator,
  property: string,
  want: readonly number[],
  label: string,
) {
  const got = await rgb(locator, property);
  want.forEach((channel, index) =>
    expect
      .soft(
        Math.abs((got[index] ?? 0) - channel),
        `${label} ${got.join(",")} vs ${want.join(",")}`,
      )
      .toBeLessThanOrEqual(16),
  );
}

/** The artboard's fills, with the dark washes composited over the dark panel (26, 32, 40). */
const FILL = {
  light: { warn: [255, 244, 214], info: [231, 240, 252] },
  dark: { warn: [56, 54, 46], info: [36, 54, 75] },
} as const;

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";

  test(`frame 6, ${mode}: someone moved it first, named, with Xem lại`, async ({
    page,
  }) => {
    const dialog = await openDialog(page, dark, "__mockMoveRace");
    await pickAndMove(page, /Kế toán/);
    const band = dialog.locator('[data-f="mnotice"]');
    await expect(band).toBeVisible();
    // The app names a department "Phòng X", as in its list of places.
    await expect(band).toContainText(
      /Hà Nguyễn vừa chuyển board này sang (Phòng )?Marketing/,
    );
    await expect(band).toContainText(
      "Danh sách người được và mất quyền đã tính lại theo nơi đặt mới.",
    );
    await page.waitForTimeout(300);
    expectRect(
      await within(dialog, band),
      { x: 21, y: 97.3, width: 558 },
      "band",
    );
    expect.soft(await style(band, "border-top-left-radius")).toBe("10px");
    // Night keeps the shared warning-subtle token, as the move dialog spec skips the dark washes.
    if (!dark)
      await expectColour(
        band,
        "background-color",
        FILL[mode].warn,
        "band fill",
      );
    const title = band.locator("b");
    expectRect(
      await within(dialog, title),
      { x: 61, y: 107.3, height: 18.2 },
      "title",
    );
    expect.soft(await style(title, "font-weight")).toBe("600");
    const review = band.getByRole("button", { name: "Xem lại" });
    expectRect(
      await within(dialog, review),
      { x: 497.8, y: 107.3, width: 67.3, height: 28 },
      "Xem lại",
    );
    expect.soft(await style(review, "font-size")).toBe("12.5px");
    // The board is in Marketing now: the dialog says so and picking starts again.
    await expect(
      dialog.getByRole("radio", { name: /Marketing/ }),
    ).toHaveAttribute("aria-disabled", "true");
    await review.click();
    await expect(band).toHaveCount(0);
  });

  test(`frame 7, ${mode}: on its way, the note under the places and Huỷ disabled`, async ({
    page,
  }) => {
    const dialog = await openDialog(page, dark, "__mockMoveHold");
    await pickAndMove(page, /Kế toán/);
    const note = dialog.locator('[data-f="mpending"]');
    await expect(note).toBeVisible();
    await expect(note).toContainText("Đang thêm 3 người và chuyển board…");
    await expect(note).toContainText("Yêu cầu đã gửi nên không huỷ được.");
    const box = note.locator("> div").first();
    expectRect(await within(dialog, box), { x: 21, width: 558 }, "note");
    expect.soft(await style(box, "border-top-left-radius")).toBe("10px");
    const title = note.locator("b");
    expectRect(await within(box, title), { x: 43, y: 9 }, "note title");
    expect.soft(await style(title, "font-size")).toBe("13.5px");
    const cancel = dialog.locator('[data-f="mcancel"]');
    await expect(cancel).toHaveAttribute("aria-disabled", "true");
    expectRect(
      await within(dialog.locator('[data-f="mf"]'), cancel),
      { width: 50.4, height: 32 },
      "Huỷ",
    );
    await expect(dialog.locator('[data-f="mgo"]')).toHaveText("Đang chuyển…");
    await page.evaluate(() => {
      (globalThis as Record<string, unknown>).__mockMoveHold = false;
    });
  });

  test(`frame 9, ${mode}: not allowed, only Đóng`, async ({ page }) => {
    const dialog = await openDialog(page, dark, "__mockMoveDenied");
    await pickAndMove(page, /Kế toán/);
    const band = dialog.locator('[data-f="mdenied"]');
    await expect(band).toBeVisible();
    await expect(band).toContainText("Bạn chưa chuyển được board này");
    await expect(band).toContainText(
      "Cần quyền Quản lý board, và quyền Tạo board ở phòng đích.",
    );
    await page.waitForTimeout(300);
    expectRect(
      await within(dialog, band),
      { x: 21, y: 97.3, width: 558 },
      "band",
    );
    // Night keeps the shared brand-subtle token, darker than the artboard's 16 % wash.
    if (!dark)
      await expectColour(
        band,
        "background-color",
        FILL[mode].info,
        "band fill",
      );
    expectRect(
      await within(dialog, band.locator("b")),
      { x: 61, y: 107.3, height: 18.2 },
      "title",
    );
    // Only Đóng: no move, no cancel, no retry.
    await expect(dialog.locator('[data-f="mgo"]')).toHaveCount(0);
    await expect(dialog.locator('[data-f="mcancel"]')).toHaveCount(0);
    await expect(
      dialog.locator('[data-f="mf"]').getByRole("button", { name: "Đóng" }),
    ).toBeVisible();
  });
}

for (const dark of [false, true]) {
  const mode = dark ? "dark" : "light";
  test(`frame 5, ${mode}: a Private board keeps everyone's access`, async ({
    page,
  }) => {
    const dialog = await openDialog(page, dark, "__mockMovePrivate");
    await page.getByRole("radio", { name: /Kế toán/ }).click();
    const band = dialog.locator('[data-f="mprivate"]');
    await expect(band).toBeVisible();
    await expect(band).toContainText(
      "Board đang Riêng tư nên quyền của mọi người giữ nguyên",
    );
    await expect(band).toContainText(
      "Chỉ nơi đặt board đổi. Người trong Phòng Công nghệ vốn đã không vào được board này.",
    );
    await page.waitForTimeout(300);
    expectRect(await within(dialog, band), { x: 21, width: 558 }, "band");
    expect.soft(await style(band, "border-top-left-radius")).toBe("10px");
    if (!dark)
      await expectColour(band, "background-color", FILL.light.info, "fill");
    const title = band.locator("b");
    expectRect(
      await within(band, title),
      { x: 40, y: 10, height: 18.9 },
      "title",
    );
    expect.soft(await style(title, "font-size")).toBe("13.5px");
    expect.soft(await style(title, "font-weight")).toBe("600");
    // Nobody gains or loses it, and no one is left behind.
    await expect(dialog.getByText(/mất quyền vào board/)).toHaveCount(0);
    await expect(dialog.locator('[data-f="mwithout"]')).toHaveCount(0);
  });
}

test("frame 11: done, who was added and who lost it, with Hoàn tác", async ({
  page,
}) => {
  await openDialog(page, false);
  await page.getByRole("radio", { name: /Kế toán/ }).click();
  await expect(page.locator('[data-f="sections"]')).toBeVisible();
  await page.locator('[data-f="mgo"]').click();
  const toast = page.locator("[data-sonner-toast]").first();
  await expect(toast).toBeVisible();
  await expect(toast).toContainText("3 người đã được thêm vào board.");
  await expect(toast).toContainText("22 người không còn thấy board.");
  await toast.getByRole("button", { name: "Hoàn tác" }).click();
  await expect(
    page.locator("[data-sonner-toast]").filter({ hasText: "đã về lại" }),
  ).toBeVisible();
});
