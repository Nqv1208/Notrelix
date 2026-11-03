import { expect, test, type Locator } from "@playwright/test";

/**
 * The create popup against the October 2026 canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), artboard
 * `Kanban-CardPopup-Create`, frame 1 ("Thẻ mới, còn trống"). Numbers are read from the artboard rendered in Chromium,
 * relative to the popup: 1120 x 772, 16 px radius, header 56, the item column 720 and the comments column 400 (the
 * comments column is off until the item exists: a 12 px rhythm, the dashed composer, the empty note), the footer 61 with
 * "Tạo tiếp thẻ khác" and Huỷ / Tạo thẻ. Opened from "Thêm công việc" of the toolbar, the lane is chosen for the person.
 *
 * Kept deviation: the filled draft (frame 2) and the 320 px sheet (`Kanban-CardPopup-M-Create`) are measured with the
 * card popup specs, which share the layout; the states frames (creating, network error, created) need the mock to
 * hold a request open and are covered by the card popup states.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";

const near = (actual: number, want: number, what: string) =>
  expect(
    Math.abs(actual - want),
    `${what}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(1);

async function rel(el: Locator, origin: Locator) {
  const [e, o] = await Promise.all([el.boundingBox(), origin.boundingBox()]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, w: e.width, h: e.height };
}

async function expectRel(
  el: Locator,
  origin: Locator,
  want: Partial<{ x: number; y: number; w: number; h: number }>,
  what: string,
) {
  const actual = await rel(el, origin);
  for (const k of ["x", "y", "w", "h"] as const)
    if (want[k] !== undefined) near(actual[k], want[k]!, `${what}.${k}`);
}

test.describe("create popup, desktop", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("frame 1: a new card, still empty", async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the defaults apply.
      }
    });
    await page.goto(BOARD);
    await expect(page.getByRole("region", { name: "Cần làm" })).toBeVisible();
    await page
      .getByRole("button", { name: "Thêm công việc", exact: true })
      .first()
      .click();
    const pop = page.getByRole("dialog").first();
    await expect(pop.getByText("Thêm vào cột")).toBeVisible();
    await page.waitForTimeout(900);
    await page.mouse.move(2, 2);

    await expectRel(pop, pop, { w: 1120, h: 772 }, "popup");
    expect(await pop.evaluate((e) => getComputedStyle(e).borderRadius)).toBe(
      "16px",
    );

    // Header: the lane picker, the more button.
    const lane = pop.getByRole("button", { name: "Cần làm" });
    await expectRel(
      lane,
      pop,
      { x: 114.3, y: 9.5, w: 117.2, h: 36 },
      "lane picker",
    );
    expect(await lane.evaluate((e) => getComputedStyle(e).borderRadius)).toBe(
      "8px",
    );
    await expectRel(
      pop.getByText("Thêm vào cột"),
      pop,
      { x: 16, h: 18.1 },
      "header label",
    );

    // The two columns.
    const comments = pop.locator("aside");
    await expectRel(
      comments,
      pop,
      { x: 720, y: 56, w: 400, h: 655 },
      "comments column",
    );
    const kids = comments.locator("> div");
    await expectRel(
      kids.nth(0),
      pop,
      { x: 741, y: 72, w: 359, h: 28 },
      "comments header",
    );
    await expectRel(
      kids.nth(1),
      pop,
      { x: 741, y: 112, w: 359, h: 44 },
      "dashed composer",
    );
    expect(
      await kids.nth(1).evaluate((e) => getComputedStyle(e).borderRadius),
    ).toBe("10px");
    expect(
      await kids.nth(1).evaluate((e) => getComputedStyle(e).borderTopStyle),
    ).toBe("dashed");
    await expectRel(
      kids.nth(2),
      pop,
      { x: 741, y: 168, w: 359, h: 183.4 },
      "empty note",
    );
    await expect(pop.getByText("Bình luận mở sau khi tạo thẻ")).toBeVisible();
    await expect(
      pop.getByRole("button", { name: "Hiện chi tiết" }),
    ).toBeDisabled();

    // The item column: title row, quick row and the four sections.
    const left = pop.locator("section").first().locator("..");
    await expectRel(left, pop, { x: 0, y: 56, w: 720 }, "item column");
    const sections = pop.locator("section");
    // The canvas draws four sections (description, fields, attachments, links) at 152, 208, 260 and 312; the app has
    // the first three at the same y. The links section is the open gap (see the fixme below).
    for (const [i, y] of [152, 208, 260].entries())
      near((await rel(sections.nth(i), pop)).y, y, `section ${i}.y`);
    await expect(sections).toHaveCount(3);

    // Footer: keep-going tick, Huỷ, Tạo thẻ.
    const keep = pop.getByLabel("Tạo tiếp thẻ khác");
    await expectRel(keep, pop, { x: 24, y: 733, w: 18, h: 18 }, "tick");
    await expectRel(
      pop.getByText("· giữ cột, nhãn, người làm"),
      pop,
      { x: 176.3, h: 18.1 },
      "tick note",
    );
    await expectRel(
      pop.getByRole("button", { name: "Huỷ" }),
      pop,
      { x: 948.5, y: 724, w: 55.4, h: 36 },
      "Huỷ",
    );
    const ok = pop.getByRole("button", { name: "Tạo thẻ", exact: true });
    await expectRel(ok, pop, { x: 1013.8, y: 724, w: 82.2, h: 36 }, "Tạo thẻ");
    expect(await ok.evaluate((e) => getComputedStyle(e).fontWeight)).toBe(
      "600",
    );
  });

  // Kanban-CardPopup-Create draws "Liên kết và phụ thuộc · Chưa có gì · Thêm liên kết" in a new card; the draft holds
  // no dependency links yet (`LinksSection` returns null in create mode), so the section is not built.
  test.fixme("frame 1: the links section is drawn under the attachments", async ({
    page,
  }) => {
    await page.goto(BOARD);
    await expect(
      page.getByRole("dialog").getByText("Liên kết và phụ thuộc"),
    ).toBeVisible();
  });
});
