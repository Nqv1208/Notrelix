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
    // The canvas draws four sections (description, fields, attachments, links) at 152, 208, 260 and 312.
    for (const [i, y] of [152, 208, 260, 312].entries())
      near((await rel(sections.nth(i), pop)).y, y, `section ${i}.y`);
    await expect(sections).toHaveCount(4);

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

  // Kanban-CardPopup-Create draws "Liên kết và phụ thuộc · Chưa có gì · Thêm liên kết" in a new card;
  // the draft holds the links and they are written once the card exists.
  test("frame 1: the links section is drawn under the attachments with its add button", async ({
    page,
  }) => {
    await page.goto(BOARD);
    await page
      .getByRole("button", { name: "Thêm công việc", exact: true })
      .first()
      .click();
    const pop = page.getByRole("dialog").first();
    await expect(pop.getByText("Liên kết và phụ thuộc")).toBeVisible();
    await expect(pop.getByText("Chưa có gì").last()).toBeVisible();
    await expect(
      pop.getByRole("button", { name: "Thêm liên kết" }),
    ).toBeVisible();
  });
});

/**
 * Frame 2, "Đã điền nháp": a draft with a title, people, labels, dates, priority and a description. The artboard's text
 * boxes are compared one by one (relative to the popup), so the summary row, the description and the comments column
 * are held to 1 px. Its custom-fields rows, the queued attachments and the links section below them are owed (the app
 * draws fields as pills where the artboard draws label and value rows).
 */
test.describe("create popup, desktop, filled draft", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner world",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("frame 2: the filled draft", async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
      } catch {
        // Private mode: the defaults apply.
      }
    });
    await page.goto(BOARD);
    await page
      .getByRole("button", { name: "Thêm công việc", exact: true })
      .first()
      .click();
    const pop = page.getByRole("dialog").first();
    await expect(pop.getByText("Thêm vào cột")).toBeVisible();
    await page.waitForTimeout(900);
    await pop
      .getByRole("textbox")
      .first()
      .fill("Chốt lịch đào tạo kế toán trưởng");
    // Labels: Đào tạo and Tài liệu.
    await pop.getByRole("button", { name: "Nhãn" }).first().click();
    await page
      .locator("[data-radix-popper-content-wrapper]")
      .last()
      .getByRole("checkbox")
      .nth(2)
      .click();
    await page
      .locator("[data-radix-popper-content-wrapper]")
      .last()
      .getByRole("checkbox")
      .nth(4)
      .click();
    await page.keyboard.press("Escape");
    // Assignee: Lê Hoàng Phúc (the row's plus).
    await pop.getByRole("button", { name: "Người làm" }).first().click();
    const phuc = page.getByText("Lê Hoàng Phúc").first();
    await phuc
      .locator("xpath=ancestor::*[.//button][1]")
      .locator("button")
      .last()
      .click()
      .catch(async () => {
        await phuc.click();
      });
    await page.keyboard.press("Escape");
    // Dates: 15/10 to 20/10.
    await pop.getByRole("button", { name: "Ngày" }).first().click();
    const dates = page.getByRole("checkbox");
    const due = page.locator("input[value$='/2026']").last();
    const start = page
      .locator("input[value$='/2026'], input[placeholder='dd/mm/yyyy']")
      .first();
    await dates.nth((await dates.count()) - 2).check();
    await due.fill("20/10/2026");
    await start.fill("15/10/2026");
    await page.getByRole("button", { name: "Lưu", exact: true }).click();
    // Priority: Cao.
    await pop
      .getByRole("button", { name: "Thêm", exact: true })
      .first()
      .click();
    await page.getByRole("option", { name: "Ưu tiên", exact: true }).click();
    await page.waitForTimeout(500);
    await page
      .getByRole("option", { name: "Cao", exact: true })
      .click({ force: true });
    // Description.
    await pop.getByText("Thêm mô tả…").first().click();
    await page.keyboard.type(
      "Làm việc với kế toán trưởng Minh An Group để chọn hai buổi đào tạo phân hệ công nợ trước ngày chạy thử.",
    );
    await page.getByRole("button", { name: "Lưu", exact: true }).click();
    await page.waitForTimeout(500);
    await page.mouse.move(2, 2);

    const texts = await pop.evaluate((root) => {
      const o = root.getBoundingClientRect();
      const res: [string, number, number, number, number][] = [];
      const w = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
      let n: Node | null;
      while ((n = w.nextNode())) {
        const t = (n.textContent ?? "").trim();
        if (!t) continue;
        const r = document.createRange();
        r.selectNodeContents(n);
        const q = r.getBoundingClientRect();
        res.push([t, q.x - o.x, q.y - o.y, q.width, q.height]);
      }
      return res;
    });
    const at = (text: string, nth = 0) => {
      const hit = texts.filter(([t]) => t === text)[nth];
      if (!hit) throw new Error(`no text "${text}"`);
      return { x: hit[1], y: hit[2], w: hit[3], h: hit[4] };
    };
    // [text, nth, x, y, w, h] read from the artboard; x is left out where the width of a chosen label moves it.
    const want: [string, number, number, number, number, number][] = [
      ["Thêm vào cột", 0, 16, 19.4, 84.3, 16],
      ["Thêm", 0, 94, 119.6, 35.8, 16],
      ["Nhãn", 0, 182.8, 119.6, 33.4, 16],
      ["Ngày", 0, 269.2, 119.6, 33.8, 16],
      ["Checklist", 0, 356, 119.6, 60.3, 16],
      ["Người làm", 0, 469.2, 119.6, 64.2, 16],
      ["Người làm", 1, 60, 165, 59.8, 15],
      ["HP", 0, 71.3, 199.1, 13.4, 13],
      ["Nhãn", 1, 135.8, 165, 31.1, 15],
      ["Đào tạo", 0, 147.8, 195.6, 53, 16],
      ["Tài liệu", 0, 230.8, 195.6, 45.7, 16],
      ["Ngày", 1, 342.5, 165, 31.5, 15],
      ["Ưu tiên", 0, 594.7, 165, 41.7, 15],
      ["Mô tả", 0, 60, 250.1, 42.7, 19],
      ["Sửa", 0, 661.4, 251.9, 23.6, 16],
      ["Bình luận và hoạt động", 0, 771, 76.1, 171.9, 19],
      ["Hiện chi tiết", 0, 1015.5, 77.9, 73.5, 16],
      ["Bình luận mở sau khi tạo thẻ", 0, 822.5, 253, 196, 18],
      ["Tạo tiếp thẻ khác", 0, 51, 732.2, 116.3, 18],
      ["Tạo thẻ", 0, 1028.8, 732.2, 52.2, 18],
    ];
    for (const [text, nth, x, y, w, h] of want) {
      const got = at(text, nth);
      near(got.x, x, `${text}#${nth}.x`);
      near(got.y, y, `${text}#${nth}.y`);
      near(got.w, w, `${text}#${nth}.w`);
      near(got.h, h, `${text}#${nth}.h`);
    }
  });
});
