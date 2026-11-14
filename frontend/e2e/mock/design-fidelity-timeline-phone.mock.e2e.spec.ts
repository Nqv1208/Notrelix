import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The Timeline at 320 px against `WM-TLCal-320` (docs/design/mockups/2026-10/src), frames "Dòng thời gian 320: nối
 * bằng nút ở cuối hàng, toast hoàn tác" and "Hộp liên kết 320: tấm dưới, từ chối vòng", light (`sang`) and dark
 * (`dem`). Numbers were read from `render.py WM-TLCal-320.dc.html out.html mode=<m>` in Chromium: the row boxes
 * relative to the first `.mrow`, the sheet's parts relative to the `.sheet`. The artboard's phone is 320 px with a
 * 1 px frame, so its content is 318 px wide; the app fills 320, so boxes on the right edge are compared by their gap to
 * that edge and the grab handle by its centre. Tolerance 1 px; font size, weight and radius exact; colours compared
 * after the canvas turns both into sRGB (oklch tokens against the artboard's hex), within 10 levels.
 *
 * Not compared: the phone header above the rows (board title and the Bảng/Lịch/Dòng thời gian segment are the
 * `Final-M-Board` shell, not built), the texts' widths (the mock board's titles differ from the artboard's), the
 * alert's height (it wraps the mock board's chain) and the scrim (the app's shared `--modal-scrim`, 0.38 against the
 * drawn 0.42 in light).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const WIDTH = 320;

test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Needs the default owner world without faults",
);
test.use({ viewport: { width: WIDTH, height: 640 } });

type Mode = "sang" | "dem";
interface Look {
  readonly panel: string;
  readonly line: string;
  readonly line2: string;
  readonly text: string;
  readonly muted: string;
  readonly meta: string;
  readonly accentText: string;
  readonly hover2: string;
  readonly dangerSoft: string;
  readonly glyphDoing: string;
}
const LOOK: Record<Mode, Look> = {
  sang: {
    panel: "rgb(255, 255, 255)",
    line: "rgb(230, 233, 238)",
    line2: "rgb(210, 215, 222)",
    text: "rgb(22, 26, 33)",
    muted: "rgb(79, 88, 102)",
    meta: "rgb(95, 104, 117)",
    accentText: "rgb(15, 86, 174)",
    hover2: "rgb(230, 233, 238)",
    dangerSoft: "rgb(253, 236, 239)",
    glyphDoing: "rgb(30, 123, 224)",
  },
  dem: {
    panel: "rgb(21, 26, 33)",
    line: "rgb(35, 43, 53)",
    line2: "rgb(46, 55, 67)",
    text: "rgb(230, 233, 238)",
    muted: "rgb(170, 179, 192)",
    meta: "rgb(143, 153, 167)",
    accentText: "rgb(125, 187, 255)",
    hover2: "rgb(35, 42, 52)",
    dangerSoft: "rgba(255, 107, 129, 0.14)",
    glyphDoing: "rgb(90, 169, 255)",
  },
};

const near = (actual: number, want: number, what: string, tol = 1) =>
  expect(
    Math.abs(actual - want),
    `${what}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(tol);

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}
async function rel(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}
function expectBox(actual: Box, expected: Partial<Box>, what: string) {
  for (const k of ["x", "y", "w", "h"] as const)
    if (expected[k] !== undefined)
      near(actual[k], expected[k]!, `${what}.${k}`);
}

/** Computed font, radius and colours; colours (and the wanted ones) as sRGB over black through a canvas. */
async function look(
  el: Locator,
  want: Record<string, string> = {},
  base = "#000",
) {
  return el.evaluate(
    (node, [wanted, under]) => {
      const cs = getComputedStyle(node);
      const rgb = (value: string) => {
        const c = document.createElement("canvas").getContext("2d")!;
        c.fillStyle = under;
        c.fillRect(0, 0, 1, 1);
        c.fillStyle = value;
        c.fillRect(0, 0, 1, 1);
        return [...c.getImageData(0, 0, 1, 1).data.slice(0, 3)];
      };
      return {
        fs: cs.fontSize,
        fw: cs.fontWeight,
        r: cs.borderRadius,
        family: cs.fontFamily,
        c: rgb(cs.color),
        bg: rgb(cs.backgroundColor),
        bb: rgb(cs.borderBottomColor),
        bbw: cs.borderBottomWidth,
        want: Object.fromEntries(
          Object.entries(wanted).map(([k, v]) => [k, rgb(v)]),
        ),
      };
    },
    [want, base] as const,
  );
}
function sameColor(actual: number[], expected: number[], what: string) {
  for (let i = 0; i < 3; i += 1)
    near(actual[i]!, expected[i]!, `${what} ${"rgb"[i]}`, 10);
}

async function open(page: Page, mode: Mode) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    (theme) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", theme);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    mode === "dem" ? "dark" : "light",
  );
  await page.goto(`${BOARD}?view=timeline`);
  await expect(page.locator('[data-tl="mrow"]').first()).toBeVisible();
}

for (const mode of ["sang", "dem"] as const) {
  const L = LOOK[mode];

  test(`Dòng thời gian 320: rows with the 44 px link button, ${mode}`, async ({
    page,
  }) => {
    await open(page, mode);
    // No chart and no horizontal scroll at 320.
    expect(await page.locator('[data-tl="gantt"]').count()).toBe(0);
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= window.innerWidth,
      ),
    ).toBe(true);

    const row = page
      .locator('[data-tl="mrow"]')
      .filter({ hasText: "ERP-142" })
      .first();
    const r = (await row.boundingBox())!;
    near(r.width, WIDTH, "row.w");
    near(r.height, 57, "row.h");
    const rowLook = await look(row, { panel: L.panel, line: L.line });
    sameColor(rowLook.bg, rowLook.want.panel!, "row bg");
    expect(rowLook.bbw).toBe("1px");
    sameColor(rowLook.bb, rowLook.want.line!, "row rule");

    const glyph = row.locator('[data-tl="sg"]');
    expectBox(await rel(glyph, row), { x: 12, y: 21.5, w: 13, h: 13 }, "glyph");
    const glyphLook = await look(glyph, { c: L.glyphDoing });
    sameColor(glyphLook.c, glyphLook.want.c!, "glyph (Đang làm)");

    const key = row.locator('[data-tl="mkey"]');
    expectBox(await rel(key, row), { x: 33, y: 19.7, h: 16.7 }, "key");
    const keyLook = await look(key, { c: L.meta });
    expect(keyLook.fs).toBe("11.5px");
    expect(keyLook.family).toMatch(/mono/i);
    sameColor(keyLook.c, keyLook.want.c!, "key");

    const title = row.locator('[data-tl="mtitle"]');
    const t = await rel(title, row);
    near(t.y, 18.6, "title.y");
    near(t.h, 18.8, "title.h");
    const titleLook = await look(title, { c: L.text });
    expect([titleLook.fs, titleLook.fw]).toEqual(["13px", "500"]);
    sameColor(titleLook.c, titleLook.want.c!, "title");

    const date = row.locator('[data-tl="mdate"]');
    const d = await rel(date, row);
    near(d.y, 19.3, "date.y");
    near(d.h, 17.4, "date.h");
    const dateLook = await look(date, { c: L.meta });
    expect(dateLook.fs).toBe("12px");
    sameColor(dateLook.c, dateLook.want.c!, "date");

    const link = row.locator('[data-tl="mlink"]');
    await expect(link).toHaveAccessibleName("Liên kết với… ERP-142");
    const b = await rel(link, row);
    expectBox(b, { y: 6, w: 44, h: 44 }, "link button");
    near(WIDTH - (b.x + b.w), 12, "link button right gap");
    near(d.x + d.w + 8, b.x, "date sits 8 px before the button");
    const linkLook = await look(link, { c: L.accentText });
    expect(linkLook.r).toBe("8px");
    sameColor(linkLook.c, linkLook.want.c!, "link icon");
    expectBox(
      await rel(link.locator("svg"), row),
      { x: b.x + 14, y: 20, w: 16, h: 16 },
      "link icon",
    );
  });

  test(`Hộp liên kết 320: the bottom sheet refuses a loop and names it, ${mode}`, async ({
    page,
  }) => {
    await open(page, mode);
    await page
      .locator('[data-tl="mrow"]')
      .filter({ hasText: "ERP-129" })
      .locator('[data-tl="mlink"]')
      .click();
    const sheet = page.getByRole("dialog", { name: "Liên kết với… ERP-129" });
    await expect(sheet).toBeVisible();
    await page.waitForTimeout(300);

    const s = (await sheet.boundingBox())!;
    near(s.x, 0, "sheet.x");
    near(s.width, WIDTH, "sheet.w");
    near(s.y + s.height, 640, "sheet sits on the bottom edge");
    const sheetLook = await look(sheet, { panel: L.panel, line: L.line });
    expect(sheetLook.r).toBe("14px 14px 0px 0px");
    sameColor(sheetLook.bg, sheetLook.want.panel!, "sheet bg");
    sameColor(sheetLook.bb, sheetLook.want.line!, "sheet border");

    const grab = sheet.locator('[data-tl="grab"]');
    const g = await rel(grab, sheet);
    expectBox(g, { y: 11, w: 36, h: 4 }, "grab");
    near(g.x + g.w / 2, s.width / 2, "grab centre");
    const grabLook = await look(grab, { c: L.line2 });
    expect(grabLook.r).toBe("2px");
    sameColor(grabLook.bg, grabLook.want.c!, "grab");

    const title = sheet.getByRole("heading", { name: "Liên kết với…" });
    expectBox(await rel(title, sheet), { x: 17, y: 36.8, h: 20.3 }, "title");
    const titleLook = await look(title);
    expect([titleLook.fs, titleLook.fw]).toEqual(["14px", "700"]);

    const chip = sheet.locator('[data-tl="sheet-chip"]');
    expectBox(await rel(chip, sheet), { y: 36, h: 22 }, "chip");
    const chipLook = await look(chip, { bg: L.hover2, c: L.muted });
    expect([chipLook.fs, chipLook.fw, chipLook.r]).toEqual([
      "12px",
      "600",
      "6px",
    ]);
    sameColor(chipLook.bg, chipLook.want.bg!, "chip bg");
    sameColor(chipLook.c, chipLook.want.c!, "chip text");

    const close = sheet.locator('[data-tl="sheet-close"]');
    const c = await rel(close, sheet);
    expectBox(c, { y: 25, w: 44, h: 44 }, "close");
    near(s.width - (c.x + c.w), 17, "close right gap");
    near(
      c.x - 8,
      (await rel(chip, sheet)).x + (await rel(chip, sheet)).w,
      "chip 8 px before close",
    );

    const seg = sheet.locator('[data-tl="sheet-seg"]');
    expectBox(await rel(seg, sheet), { x: 17, y: 79, h: 32 }, "segment");
    const segLook = await look(seg, { bg: L.hover2 });
    expect(segLook.r).toBe("8px");
    sameColor(segLook.bg, segLook.want.bg!, "segment bg");
    const on = seg.getByRole("radio", { checked: true });
    await expect(on).toHaveText("ERP-129 chờ…");
    expectBox(await rel(on, sheet), { x: 19, y: 81, h: 28 }, "segment on");
    const onLook = await look(on, { bg: L.panel, c: L.text });
    expect([onLook.fs, onLook.fw, onLook.r]).toEqual(["13px", "600", "6px"]);
    sameColor(onLook.bg, onLook.want.bg!, "segment on bg");
    const off = seg.getByRole("radio", { checked: false });
    await expect(off).toHaveText("chặn…");
    const offLook = await look(off, { c: L.muted });
    expect([offLook.fs, offLook.fw]).toEqual(["13px", "500"]);
    sameColor(offLook.c, offLook.want.c!, "segment off text");

    const input = sheet.locator('[data-tl="sheet-input"]');
    expectBox(
      await rel(input, sheet),
      { x: 17, y: 121, w: s.width - 34, h: 44 },
      "search",
    );
    const inputLook = await look(input, { line: L.line2 });
    expect([inputLook.fs, inputLook.r]).toEqual(["13.5px", "7px"]);
    sameColor(inputLook.bb, inputLook.want.line!, "search border");

    // Choosing a row that would close a loop: it stays disabled with its reason, the alert names the chain.
    const looped = sheet
      .getByRole("option")
      .filter({ hasText: "ERP-142" })
      .first();
    await expect(looped).toHaveAttribute("aria-disabled", "true");
    await expect(looped).toContainText("Tạo vòng: ERP-142 đã chờ ERP-129");
    await looped.click({ force: true });
    const alert = sheet.getByRole("alert");
    await expect(alert).toContainText("Không thể nối: tạo vòng phụ thuộc.");
    await expect(alert).toContainText(
      /ERP-129 → .*ERP-129\.|ERP-142 → .*ERP-142\./,
    );
    const a = await rel(alert, sheet);
    expectBox(a, { x: 17, y: 175, w: s.width - 34 }, "alert");
    // Both see-through fills are laid on the sheet, as on screen.
    const alertLook = await look(alert, { bg: L.dangerSoft }, L.panel);
    expect([alertLook.fs, alertLook.r]).toEqual(["12.5px", "8px"]);
    sameColor(alertLook.bg, alertLook.want.bg!, "alert bg");

    const first = sheet.getByRole("option").first();
    const o = await rel(first, sheet);
    expectBox(o, { x: 17, w: s.width - 34 }, "first option");
    near(o.y - (a.y + a.h), 10, "first option 10 px under the alert");
    const optLook = await look(first);
    expect([optLook.fs, optLook.r]).toEqual(["13.5px", "7px"]);
    const optKey = first.locator("span.font-mono");
    const k = await rel(optKey, sheet);
    near(k.x, 53, "option key.x");
    near(k.y - o.y, 7, "option key.y");
    expect((await look(optKey)).fs).toBe("11.5px");
    const optTitle = first.locator("b");
    near((await rel(optTitle, sheet)).y - o.y, 7, "option title.y");
    expect([(await look(optTitle)).fs, (await look(optTitle)).fw]).toEqual([
      "13.5px",
      "500",
    ]);
    const optSmall = first.locator("small");
    expect((await look(optSmall)).fs).toBe("12px");
    near((await rel(optSmall, sheet)).h, 16.2, "option note.h");

    const create = sheet.locator('[data-tl="sheet-create"]');
    await expect(create).toHaveAttribute("aria-disabled", "true");
    const cr = await rel(create, sheet);
    expectBox(cr, { x: 17, w: s.width - 34, h: 44 }, "create");
    const createLook = await look(create, {
      bg: L.hover2,
      c: L.muted,
      line: L.line2,
    });
    expect([createLook.fs, createLook.fw, createLook.r]).toEqual([
      "13px",
      "600",
      "7px",
    ]);
    sameColor(createLook.bg, createLook.want.bg!, "create (disabled) bg");
    sameColor(createLook.c, createLook.want.c!, "create (disabled) text");
    sameColor(createLook.bb, createLook.want.line!, "create (disabled) border");

    // A row that closes no loop links, and the toast offers Hoàn tác.
    const free = sheet.locator('[role="option"]:not([aria-disabled])').first();
    await free.click();
    await expect(create).not.toHaveAttribute("aria-disabled", "true");
    await create.click();
    await expect(sheet).toBeHidden();
    // The undo toast is the app's shared toaster (288 px at x 16 here, the artboard draws 296 px at x 12): only its
    // message and Hoàn tác are checked.
    const toast = page.locator("[data-sonner-toast]").first();
    await expect(toast).toContainText(/^Đã nối: ERP-129 chờ ERP-\d+/);
    await expect(toast.getByRole("button", { name: "Hoàn tác" })).toBeVisible();
  });
}
