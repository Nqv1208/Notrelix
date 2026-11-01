import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Card popup geometry against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x). An item has
 * no page of its own since f2897fe9e, so the popup is the item view and its fidelity is measured here.
 *
 * Two surfaces are measured, each against the artboard that draws it:
 * - the archive confirmation, against `BC-Actions` (frames `act-arc-sang` / `act-arc-S-sang` / `act-arc-E-sang`
 *   at 440 px and `act320-arc-sang` at 320 px);
 * - the approval block of the side column, against `WM-Approval` (the first card, the approver's view).
 *
 * Every number is read from the artboard rendered in Chromium (box relative to the dialog's or the card's own
 * top-left, then font size, weight, radius and padding). Tolerance is 1 px for boxes; font sizes, weights and
 * radii must be equal. Text differs between the canvas data and the mock world, so a box whose width follows
 * its wording lists only the keys that do not, and the rows below a wrapped line are checked as a stack
 * (each starts where the one above ends plus the artboard's gap) rather than at an absolute y.
 *
 * The busy state needs a slow write and the error state a failing one, so they run when the server was
 * started with `VITE_MOCK_LATENCY=slow` and with
 * `VITE_MOCK_FAULTS=/board-items/mock-erp-item-142/archive=network`; the ready state and the approval block
 * run in the default world.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

async function open(page: Page, path: string, dark = false) {
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
  await page.goto(path);
}

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

/** The box of `el` relative to the top-left of `origin`. */
async function box(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

const near = (actual: number, expected: number, what: string) =>
  expect(
    Math.abs(actual - expected),
    `${what}: ${actual} vs ${expected}`,
  ).toBeLessThanOrEqual(TOL);

/** The computed values of an element, as the canvas's own rendering reports them. */
async function style(el: Locator) {
  return el.evaluate((node) => {
    const cs = getComputedStyle(node);
    // The browser may print a modern colour space; draw it on a canvas to read plain sRGB.
    const rgb = (value: string) => {
      const c = document.createElement("canvas").getContext("2d")!;
      c.fillStyle = "#000";
      c.fillStyle = value;
      c.fillRect(0, 0, 1, 1);
      const [r, g, b, a] = c.getImageData(0, 0, 1, 1).data;
      return a === 255
        ? `rgb(${r}, ${g}, ${b})`
        : `rgba(${r}, ${g}, ${b}, ${a})`;
    };
    return {
      fs: cs.fontSize,
      fw: cs.fontWeight,
      r: cs.borderTopLeftRadius,
      c: rgb(cs.color),
      bg: rgb(cs.backgroundColor),
    };
  });
}

test.beforeEach(() => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
});

/** Opens the archive confirmation of ERP-142 (five sub-items, linked items). */
async function openArchiveDialog(page: Page, dark = false) {
  await open(page, `${BOARD}?item=mock-erp-item-142`, dark);
  await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
  await page.getByRole("menuitem", { name: /^Lưu trữ/ }).click();
  const dialog = page.getByRole("dialog", { name: "Lưu trữ việc này?" });
  await expect(dialog).toBeVisible();
  await page.waitForTimeout(700);
  return dialog;
}

/** The parts of the confirmation, by their slots. */
function parts(dialog: Locator) {
  const header = dialog.locator("[data-slot=dialog-header]");
  return {
    header,
    title: dialog.locator("[data-slot=dialog-title]"),
    close: header.getByRole("button"),
    body: header.locator("xpath=following-sibling::div[1]"),
    lead: dialog.locator("[data-slot=dialog-description]"),
    lines: dialog.locator("ul > li"),
    footer: dialog.locator("[data-slot=dialog-footer]"),
    buttons: dialog.locator("[data-slot=dialog-footer] button"),
  };
}

test.describe("archive confirmation (BC-Actions), 1440 px", () => {
  test.use({ viewport: { width: 1440, height: 900 } });

  test("ready, light: the box tree of act-arc-sang", async ({ page }) => {
    test.skip(
      faults !== "" || latency === "slow",
      "Ready state, default world",
    );
    const dialog = await openArchiveDialog(page);
    const p = parts(dialog);

    const d = (await dialog.boundingBox())!;
    near(d.width, 440, "dialog width");
    expect(await style(dialog)).toMatchObject({
      r: "14px",
      bg: "rgb(255, 255, 255)",
    });

    const header = await box(p.header, dialog);
    near(header.x, 1, "header x");
    near(header.y, 1, "header y");
    near(header.w, 438, "header width");
    near(header.h, 56, "header height");
    const title = await box(p.title, dialog);
    near(title.x, 21, "title x");
    near(title.y, 20.67, "title y");
    near(title.h, 24.64, "title height");
    expect(await style(p.title)).toMatchObject({ fs: "17px", fw: "650" });
    const close = await box(p.close, dialog);
    for (const [key, want] of [
      ["x", 387],
      ["y", 17],
      ["w", 32],
      ["h", 32],
    ] as const)
      near(close[key], want, `close ${key}`);
    expect(await style(p.close)).toMatchObject({
      r: "7px",
      c: "rgb(79, 88, 102)",
    });

    const body = await box(p.body, dialog);
    near(body.x, 1, "body x");
    near(body.y, 57, "body y");
    near(body.w, 438, "body width");
    const lead = await box(p.lead, dialog);
    near(lead.x, 21, "lead x");
    near(lead.y, 65, "lead y");
    near(lead.w, 398, "lead width");
    expect(await style(p.lead)).toMatchObject({ fs: "13.5px", fw: "400" });

    // Rows stack with 8 px between them; each has a 16 px icon at the left edge of the text column.
    let previousBottom = lead.y + lead.h + 14;
    const count = await p.lines.count();
    expect(count).toBeGreaterThanOrEqual(2);
    for (let i = 0; i < count; i += 1) {
      const row = await box(p.lines.nth(i), dialog);
      near(row.x, 21, `line ${i} x`);
      near(row.y, previousBottom, `line ${i} y`);
      near(row.w, 398, `line ${i} width`);
      const icon = await box(p.lines.nth(i).locator("svg"), dialog);
      near(icon.x, 21, `line ${i} icon x`);
      near(icon.w, 16, `line ${i} icon width`);
      near(icon.h, 16, `line ${i} icon height`);
      near(icon.y, row.y, `line ${i} icon y`);
      const text = await box(p.lines.nth(i).locator("span"), dialog);
      near(text.x, 47, `line ${i} text x`);
      previousBottom = row.y + row.h + 8;
    }

    const footer = await box(p.footer, dialog);
    near(footer.x, 1, "footer x");
    near(footer.w, 438, "footer width");
    near(footer.h, 61, "footer height");
    near(footer.y, body.y + body.h, "footer follows the body");
    const cancel = await box(p.buttons.nth(0), dialog);
    const confirm = await box(p.buttons.nth(1), dialog);
    for (const b of [cancel, confirm]) {
      near(b.h, 36, "button height");
      near(b.y, footer.y + 13, "button y");
    }
    near(confirm.x + confirm.w, 419, "confirm right edge");
    near(cancel.x + cancel.w, confirm.x - 8, "cancel right edge");
    near(cancel.w, 54.42, "cancel width");
    expect(await style(p.buttons.nth(0))).toMatchObject({
      fs: "13px",
      fw: "500",
      r: "7px",
      bg: "rgb(255, 255, 255)",
    });
    expect(await style(p.buttons.nth(1))).toMatchObject({
      fs: "13px",
      fw: "600",
      r: "7px",
      c: "rgb(255, 255, 255)",
      bg: "rgb(22, 102, 204)",
    });
  });

  test("busy: no close, 32 px flat buttons (act-arc-S-sang)", async ({
    page,
  }) => {
    test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    const dialog = await openArchiveDialog(page);
    const p = parts(dialog);
    await p.buttons.nth(1).click();
    await expect(p.buttons.nth(1)).toBeDisabled();
    await expect(p.close).toHaveCount(0);
    const header = await box(p.header, dialog);
    near(header.h, 48.64, "header height");
    near((await box(p.title, dialog)).y, 17, "title y");
    const footer = await box(p.footer, dialog);
    near(footer.h, 57, "footer height");
    for (const index of [0, 1]) {
      const b = await box(p.buttons.nth(index), dialog);
      near(b.h, 32, "button height");
      near(b.y, footer.y + 13, "button y");
      expect(await style(p.buttons.nth(index))).toMatchObject({
        fs: "13px",
        fw: "500",
        r: "7px",
        c: "rgb(95, 104, 117)",
        bg: "rgb(230, 233, 238)",
      });
    }
  });

  test("error: the refusal sits under the rows, Thử lại replaces the label (act-arc-E-sang)", async ({
    page,
  }) => {
    test.skip(
      !faults.includes("archive=network"),
      "Needs the archive network fault",
    );
    const dialog = await openArchiveDialog(page);
    const p = parts(dialog);
    await p.buttons.nth(1).click();
    const alert = dialog.getByRole("alert");
    await expect(alert).toBeVisible();
    const box1 = await box(alert, dialog);
    near(box1.x, 21, "alert x");
    near(box1.w, 398, "alert width");
    expect(await style(alert)).toMatchObject({
      fs: "13px",
      r: "10px",
      bg: "rgb(253, 236, 239)",
      c: "rgb(196, 35, 64)",
    });
    const last = await box(p.lines.last(), dialog);
    near(box1.y, last.y + last.h + 14, "alert y follows the rows");
    await expect(p.buttons.nth(1)).toHaveText("Thử lại");
    near((await box(p.buttons.nth(1), dialog)).w, 73.22, "Thử lại width");
  });
});

test.describe("archive confirmation (BC-Actions), 320 px sheet", () => {
  test.use({ viewport: { width: 320, height: 800 } });

  test("ready, light: the sheet of act320-arc-sang", async ({ page }) => {
    test.skip(
      faults !== "" || latency === "slow",
      "Ready state, default world",
    );
    const dialog = await openArchiveDialog(page);
    const p = parts(dialog);
    const d = (await dialog.boundingBox())!;
    // The artboard's frame adds a 1 px border either side of a 318 px sheet.
    near(d.width, 320, "sheet width");
    expect(await style(dialog)).toMatchObject({ r: "16px" });
    const close = await box(p.close, dialog);
    near(close.w, 44, "close width");
    near(close.h, 44, "close height");
    near(d.width - (close.x + close.w), 22, "close right gap");
    const title = await box(p.title, dialog);
    near(title.x, 21, "title x");
    const footer = await box(p.footer, dialog);
    near(footer.h, 121, "footer height");
    // Stacked, 44 px each, confirm above cancel, 21 px in from both edges.
    const confirm = await box(p.buttons.nth(1), dialog);
    const cancel = await box(p.buttons.nth(0), dialog);
    for (const b of [confirm, cancel]) {
      near(b.h, 44, "button height");
      near(b.x, 21, "button x");
      near(d.width - (b.x + b.w), 21, "button right gap");
    }
    near(confirm.y, footer.y + 13, "confirm y");
    near(cancel.y, footer.y + 65, "cancel y");
    expect(await style(p.buttons.nth(0))).toMatchObject({
      fs: "14px",
      fw: "500",
    });
    expect(await style(p.buttons.nth(1))).toMatchObject({
      fs: "14px",
      fw: "600",
    });
  });
});

test.describe("archive confirmation (BC-Actions), dark", () => {
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the dialog keeps its boxes and draws on the dark surface (act-arc-dem)", async ({
    page,
  }) => {
    test.skip(
      faults !== "" || latency === "slow",
      "Ready state, default world",
    );
    const dialog = await openArchiveDialog(page, true);
    const p = parts(dialog);
    const d = (await dialog.boundingBox())!;
    near(d.width, 440, "dialog width");
    const header = await box(p.header, dialog);
    near(header.h, 56, "header height");
    const footer = await box(p.footer, dialog);
    near(footer.h, 61, "footer height");
    const dialogStyle = await style(dialog);
    expect(dialogStyle.r).toBe("14px");
    // Dark surface: the sheet is not the light one.
    expect(dialogStyle.bg).not.toBe("rgb(255, 255, 255)");
    expect((await style(p.buttons.nth(1))).fw).toBe("600");
  });
});

test.describe("approval block of the side column (WM-Approval), 1440 px", () => {
  test.use({ viewport: { width: 1440, height: 900 } });

  test("the approver's card: the first card of the artboard", async ({
    page,
  }) => {
    test.skip(faults !== "" || latency === "slow", "Default world");
    // ERP-138 asks the owner to approve; the request has a note.
    await open(page, `${BOARD}?item=mock-erp-item-138`);
    const card = page.getByRole("region", { name: "Phê duyệt" });
    await expect(card).toBeVisible();
    await page.waitForTimeout(700);

    const c = (await card.boundingBox())!;
    expect(await style(card)).toMatchObject({
      r: "12px",
      bg: "rgb(255, 255, 255)",
    });
    const padding = await card.evaluate((n) => getComputedStyle(n).padding);
    expect(padding).toBe("16px");

    const heading = await box(
      card.getByRole("heading", { name: "Phê duyệt" }),
      card,
    );
    near(heading.x, 17, "heading x");
    near(heading.y, 17, "heading y");
    near(heading.h, 20.3, "heading height");
    expect(
      await style(card.getByRole("heading", { name: "Phê duyệt" })),
    ).toMatchObject({ fs: "14px", fw: "650" });

    // "Gửi …" is right-aligned on the heading row, 17 px from the card's right edge.
    const sent = card.getByText(/^Gửi /);
    const sentBox = await box(sent, card);
    near(c.width - (sentBox.x + sentBox.w), 17, "sent right gap");
    expect(await style(sent)).toMatchObject({
      fs: "12px",
      c: "rgb(95, 104, 117)",
    });

    // The requester's mark and line sit 12 px under the heading row.
    const mark = await box(card.getByRole("img").first(), card);
    near(mark.x, 17, "mark x");
    near(mark.y, 49.3, "mark y");
    near(mark.w, 26, "mark width");
    near(mark.h, 26, "mark height");
    expect(await style(card.getByRole("img").first())).toMatchObject({
      fs: "11px",
      fw: "700",
    });

    // The note: 12 px under the requester row, 8 px radius, 10 px 12 px padding, on the page tint.
    const note = card.locator("p").filter({ hasText: "“" }).first();
    const noteBox = await box(note, card);
    near(noteBox.x, 17, "note x");
    near(noteBox.y, 87.3, "note y");
    expect(await style(note)).toMatchObject({
      fs: "13px",
      r: "8px",
      c: "rgb(79, 88, 102)",
      bg: "rgb(247, 248, 250)",
    });

    // The buttons: 32 px, 12 px under the note, 8 px apart.
    const approve = card.getByRole("button", { name: "Duyệt", exact: true });
    const reject = card.getByRole("button", { name: "Từ chối", exact: true });
    const a = await box(approve, card);
    const r = await box(reject, card);
    near(a.x, 17, "approve x");
    near(a.h, 32, "approve height");
    near(r.h, 32, "reject height");
    near(a.y, noteBox.y + noteBox.h + 12, "buttons follow the note");
    near(r.x, a.x + a.w + 8, "reject x");
    near(a.w, 86.55, "approve width");
    near(r.w, 95.13, "reject width");
    expect(await style(approve)).toMatchObject({
      fs: "13px",
      fw: "600",
      r: "7px",
      c: "rgb(255, 255, 255)",
      bg: "rgb(22, 102, 204)",
    });
    expect(await style(reject)).toMatchObject({
      fs: "13px",
      fw: "500",
      r: "7px",
      bg: "rgb(255, 255, 255)",
    });
  });
});
