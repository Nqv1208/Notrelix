import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The other five cards of the approval block against the October 2026 canvas (owner artifact
 * 18L4jUXGfdXh7Wn9YQvA2x), artboard `WM-Approval`; `design-fidelity-card-popup` measures the first card
 * (the approver's view). The block sits in the side column of the card popup, so it is opened there
 * (`?item=`), and the popup files are not edited by this spec.
 *
 * Cards measured, each rendered in the mock world by the person the card shows:
 * - 2 the approver rejecting, the reason box open (owner on ERP-138, presses "Từ chối");
 * - 3 the requester waiting with "Huỷ yêu cầu" (owner asks a person on ERP-142);
 * - 4 the approved result (owner approves ERP-138);
 * - 5 the observer, "Thành viên khác" (the owner on ERP-147, a request the mock world seeds between two
 *   other people);
 * - 6 the rejected result (owner rejects ERP-138 with a reason).
 *
 * Every number is read from the artboard rendered in Chromium, relative to the card's top-left.
 * Tolerance is 1 px for boxes; font sizes, weights, radii and colours must be equal. Wording and
 * names differ between the canvas data and the mock world, so only boxes that do not follow the
 * wording are compared, and a row that follows a wrapped line is checked as a stack.
 *
 * Kept deviation: the success and danger fills (the result banners, the filled "Từ chối yêu cầu") come
 * from the shared design tokens, which differ from the artboard's own #E5F4EC, #FCEBEE and #C42340, so
 * the spec measures their boxes, fonts and radii but not those three colours.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, item: string, dark = false, asked = true) {
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
  await page.goto(`${BOARD}?item=${item}`);
  const card = page.getByRole("region", { name: "Phê duyệt" });
  // An item nobody asked about has no block yet.
  if (asked) await expect(card).toBeVisible();
  // The popup fades in.
  await page.waitForTimeout(700);
  return card;
}

interface Box {
  x: number;
  y: number;
  w: number;
  h: number;
}

async function box(el: Locator, origin: Locator): Promise<Box> {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

async function style(el: Locator) {
  return el.evaluate((n) => {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 1;
    const context = canvas.getContext("2d", { willReadFrequently: true })!;
    const rgb = (value: string) => {
      context.clearRect(0, 0, 1, 1);
      context.fillStyle = "#000";
      context.fillStyle = value;
      context.fillRect(0, 0, 1, 1);
      const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
      return `rgb(${r}, ${g}, ${b})`;
    };
    const s = getComputedStyle(n);
    return {
      fs: s.fontSize,
      fw: s.fontWeight,
      r: s.borderRadius,
      p: s.padding,
      g: s.gap,
      bg: rgb(s.backgroundColor),
      c: rgb(s.color),
    };
  });
}

function near(actual: number, want: number, label: string) {
  expect
    .soft(Math.abs(actual - want), `${label}: ${actual.toFixed(1)} vs ${want}`)
    .toBeLessThanOrEqual(1);
}

/** The heading row every card starts with: 17 px in, 20.3 px high, "Gửi …" 17 px from the right edge. */
async function expectHeading(card: Locator) {
  const heading = card.getByRole("heading", { name: "Phê duyệt" });
  const h = await box(heading, card);
  near(h.x, 17, "heading x");
  near(h.y, 17, "heading y");
  near(h.h, 20.3, "heading height");
  const c = (await card.boundingBox())!;
  const sent = await box(card.getByText(/^Gửi /), card);
  near(c.width - (sent.x + sent.w), 17, "sent right gap");
  expect.soft(await style(card)).toMatchObject({ r: "12px", p: "16px" });
}

test.describe("approval block, owner, 1440 px", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  async function rejecting(page: Page) {
    const card = await open(page, "mock-erp-item-138");
    await card.getByRole("button", { name: "Từ chối", exact: true }).click();
    const label = card.getByText("Lý do từ chối", { exact: false }).first();
    const field = card.getByRole("textbox");
    await field.fill(
      "Còn 3 bản ghi lệch chưa xử lý, cần làm sạch trước khi duyệt.",
    );
    // The form is shorter than the row it replaces, so the pointer rests on a button: move it off.
    await page.mouse.move(2, 2);
    return { card, label, field };
  }

  test("card 2, the approver rejecting: label, reason box, note and the two buttons as one stack", async ({
    page,
  }) => {
    const { card, label, field } = await rejecting(page);
    await expectHeading(card);

    // The stack under the label is the artboard's: 6 px gaps, a 72 px box, buttons 12 px under the note.
    const l = await box(label, card);
    near(l.x, 17, "label x");
    near(l.h, 18.1, "label height");
    expect.soft(await style(label)).toMatchObject({
      fs: "12.5px",
      fw: "600",
      c: "rgb(79, 88, 102)",
    });
    const t = await box(field, card);
    near(t.x, 17, "textarea x");
    near(t.y, l.y + l.h + 6, "textarea follows the label");
    near(t.h, 72, "textarea height");
    const note = card.getByText(/sẽ thấy lý do này/);
    const n = await box(note, card);
    near(n.y, t.y + t.h + 6, "note follows the textarea");
    near(n.h, 17.4, "note height");
    expect.soft(await style(note)).toMatchObject({ fs: "12px" });

    const back = card.getByRole("button", { name: "Quay lại", exact: true });
    const confirm = card.getByRole("button", { name: "Từ chối yêu cầu" });
    const b = await box(back, card);
    const k = await box(confirm, card);
    near(b.h, 32, "back height");
    near(k.h, 32, "confirm height");
    near(k.y, n.y + n.h + 12, "buttons follow the note");
    near(k.w, 126.7, "confirm width");
    // Right-aligned, 8 px apart, 17 px from the card's right edge.
    const c = (await card.boundingBox())!;
    near(c.width - (k.x + k.w), 17, "confirm right gap");
    near(b.x + b.w + 8, k.x, "buttons gap");
    expect.soft(await style(back)).toMatchObject({
      fs: "13px",
      fw: "500",
      c: "rgb(79, 88, 102)",
    });
  });

  test("card 2, the form replaces the row and the note, the text box is 13.5 px with a 7 px radius", async ({
    page,
  }) => {
    const { card, label, field } = await rejecting(page);
    near((await box(label, card)).y, 49.3, "label y");
    near((await box(field, card)).y, 73.4, "textarea y");
    expect(await style(field)).toMatchObject({ fs: "13.5px", r: "7px" });
    near((await card.boundingBox())!.height, 229.8, "card height");
  });

  test("card 2, Từ chối yêu cầu is the filled danger button, 12 px under the note", async ({
    page,
  }) => {
    const { card } = await rejecting(page);
    const note = await box(card.getByText(/sẽ thấy lý do này/), card);
    const confirm = card.getByRole("button", { name: "Từ chối yêu cầu" });
    near((await box(confirm, card)).y, note.y + note.h + 12, "buttons y");
    expect(await style(confirm)).toMatchObject({
      fs: "13px",
      fw: "600",
      c: "rgb(255, 255, 255)",
    });
  });

  test("card 3, the requester waiting: banner and Hủy yêu cầu", async ({
    page,
  }) => {
    await open(page, "mock-erp-item-142", false, false);
    await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
    await page
      .getByRole("menuitem", { name: "Yêu cầu phê duyệt" })
      .press("ArrowRight");
    await page.getByRole("menuitem", { name: /Alex Rivera/ }).click();
    const card = page.getByRole("region", { name: "Phê duyệt" });
    await expect(card.getByText(/Đang chờ/)).toBeVisible();
    await expectHeading(card);

    const banner = card.getByRole("status");
    const b = await box(banner, card);
    near(b.x, 17, "banner x");
    near(b.y, 49.3, "banner y");
    near(b.h, 38.8, "banner height");
    expect.soft(await style(banner)).toMatchObject({
      fs: "13px",
      fw: "500",
      r: "8px",
      p: "10px 12px",
      g: "10px",
    });
    const cancel = card.getByRole("button", { name: "Hủy yêu cầu" });
    const k = await box(cancel, card);
    near(k.x, 17, "cancel x");
    near(k.y, 100.1, "cancel y");
    near(k.h, 32, "cancel height");
    near(k.w, 103, "cancel width");
    expect.soft(await style(cancel)).toMatchObject({
      fs: "13px",
      fw: "500",
      r: "7px",
      bg: "rgb(255, 255, 255)",
    });
    near((await card.boundingBox())!.height, 149.1, "card height");
  });

  test("card 4, approved: the green result banner", async ({ page }) => {
    const card = await open(page, "mock-erp-item-138");
    await card.getByRole("button", { name: "Duyệt", exact: true }).click();
    await expect(card.getByText(/đã duyệt/)).toBeVisible();
    await expectHeading(card);
    const banner = card.getByRole("status");
    const b = await box(banner, card);
    near(b.x, 17, "banner x");
    near(b.h, 38.8, "banner height");
    expect.soft(await style(banner)).toMatchObject({
      fs: "13px",
      fw: "600",
      r: "8px",
      p: "10px 12px",
    });
  });

  test("card 6, rejected: the red result banner with the reason", async ({
    page,
  }) => {
    const card = await open(page, "mock-erp-item-138");
    await card.getByRole("button", { name: "Từ chối", exact: true }).click();
    await card
      .getByRole("textbox")
      .fill("Còn 3 bản ghi lệch chưa xử lý, cần làm sạch trước khi duyệt.");
    await card.getByRole("button", { name: "Từ chối yêu cầu" }).click();
    await expect(card.getByText(/đã từ chối/)).toBeVisible();
    await expectHeading(card);
    const banner = card.getByRole("status");
    const b = await box(banner, card);
    near(b.x, 17, "banner x");
    expect.soft(await style(banner)).toMatchObject({
      fs: "13px",
      r: "8px",
      p: "10px 12px",
    });
    const reason = banner.getByText(/Còn 3 bản ghi/);
    expect.soft(await style(reason)).toMatchObject({
      fw: "400",
      c: "rgb(22, 26, 33)",
    });
  });

  test("cards 4 and 6: the result banners sit 10 px under the heading row and use the artboard's tints", async ({
    page,
  }) => {
    let card = await open(page, "mock-erp-item-138");
    await card.getByRole("button", { name: "Duyệt", exact: true }).click();
    await expect(card.getByText(/đã duyệt/)).toBeVisible();
    near((await box(card.getByRole("status"), card)).y, 47.3, "approved y");

    card = await open(page, "mock-erp-item-138");
    await card.getByRole("button", { name: "Từ chối", exact: true }).click();
    await card.getByRole("button", { name: "Từ chối yêu cầu" }).click();
    await expect(card.getByText(/đã từ chối/)).toBeVisible();
    near((await box(card.getByRole("status"), card)).y, 47.3, "rejected y");
  });

  test("card 5, a member who is neither requester nor approver: the banner and the explanation only", async ({
    page,
  }) => {
    const card = await open(page, "mock-erp-item-147");
    await expectHeading(card);
    const banner = card.getByRole("status");
    const b = await box(banner, card);
    near(b.x, 17, "banner x");
    near(b.y, 49.3, "banner y");
    near(b.h, 38.8, "banner height");
    expect.soft(await style(banner)).toMatchObject({
      fs: "13px",
      fw: "500",
      r: "8px",
    });
    const why = card.getByText(/Chỉ người được nhờ mới/);
    const w = await box(why, card);
    near(w.x, 17, "explanation x");
    near(w.y, 100.1, "explanation y");
    // The wording wraps (the mock names are longer than the artboard's): each line is 18.1 px, 1 px tolerance per line.
    const lines = Math.round(w.h / 18.1);
    expect
      .soft(
        Math.abs(w.h / lines - 18.125),
        `explanation line height: ${(w.h / lines).toFixed(2)} vs 18.125`,
      )
      .toBeLessThanOrEqual(1);
    expect.soft(await style(why)).toMatchObject({
      fs: "12.5px",
      fw: "400",
      c: "rgb(95, 104, 117)",
    });
    // No button of any kind for an observer.
    await expect(card.getByRole("button")).toHaveCount(0);
    near(
      (await card.boundingBox())!.height,
      w.y + w.h + 17,
      "card closes 17 px under the explanation",
    );
  });
});
