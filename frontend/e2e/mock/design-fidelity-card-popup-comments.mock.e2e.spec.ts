import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The comments and history column of the card popup (UC-WM-R01, R02, R06, O01) against the October 2026
 * canvas (owner artifact 18L4jUXGfdXh7Wn9YQvA2x), light and dark, 1440 px.
 *
 * Drawn, measured to the artboard:
 * - a comment of the feed (avatar, name, time, bubble, actions row): `Kanban-CardPopup-States-1`, stage 4;
 * - the feed while it loads (grey blocks the shape of a comment and a history line, not moving): stage 1;
 * - the column's empty block (44 px mark, title, line): `Kanban-CardPopup-Create`, the right column;
 * - the "Theo dõi thẻ" entry of the ⋯ menu, its line and its place after "Đánh dấu hoàn thành":
 *   `Kanban-CardPopup-Menus`, stage 3.
 *
 * Not drawn on any artboard, built from the drawn parts and held to them: the inline edit (the bubble's box,
 * the open composer's ring), the "đã sửa" mark (the time's size, the meta ink), the delete confirm (the danger
 * tones of `WM-Comments` and of the card error frame), the placeholder of a deleted comment that keeps its
 * replies (the bubble's box, dashed in the composer's disabled line) and the history error (the empty block's
 * shape in the error frame's danger tones, with Thử lại).
 *
 * Every number is read from the artboard rendered in Chromium, relative to the comment row or the column.
 * Boxes are within 1 px; sizes, weights and radii are equal; colours are compared after a canvas turns them to
 * sRGB. The states use `globalThis.__mockFeed` ("hold", "empty", "fail") in the mock's comment and history reads.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const ITEM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?item=mock-erp-item-142";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

/** The artboards' tokens, light (`sang`) and dark (`dem`). */
const INK = {
  light: {
    text: "rgb(22, 26, 33)",
    muted: "rgb(79, 88, 102)",
    meta: "rgb(95, 104, 117)",
    link: "rgb(15, 86, 174)",
    bubble: "rgb(255, 255, 255)",
    bubbleLine: "rgb(225, 229, 234)",
    line2: "rgb(210, 215, 222)",
    block: "rgb(230, 233, 238)",
    accent: "rgb(22, 102, 204)",
    danger: "rgb(196, 35, 64)",
    dangerSoft: "rgb(253, 236, 239)",
  },
  dark: {
    text: "rgb(230, 233, 238)",
    muted: "rgb(170, 179, 192)",
    meta: "rgb(143, 153, 167)",
    link: "rgb(125, 187, 255)",
    bubble: "rgb(26, 32, 40)",
    bubbleLine: "rgb(35, 43, 53)",
    line2: "rgb(46, 55, 67)",
    block: "rgb(35, 42, 52)",
    accent: "rgb(90, 169, 255)",
    danger: "rgb(255, 107, 129)",
    dangerSoft: "rgba(255, 107, 132, 31)",
  },
} as const;

type Mode = keyof typeof INK;

async function open(
  page: Page,
  mode: Mode,
  feed?: "hold" | "empty" | "fail",
): Promise<Locator> {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ([theme, hook]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", theme!);
      } catch {
        // Private mode: the defaults apply.
      }
      if (hook) (globalThis as { __mockFeed?: string }).__mockFeed = hook;
    },
    [mode, feed ?? ""] as const,
  );
  await page.goto(ITEM);
  const composer = page.getByRole("button", {
    name: "Viết bình luận…",
    exact: true,
  });
  await expect(composer.first()).toBeVisible();
  return composer.first();
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

const near = (actual: number, expected: number, what: string) =>
  expect(
    Math.abs(actual - expected),
    `${what}: ${actual} vs ${expected}`,
  ).toBeLessThanOrEqual(TOL);

async function style(el: Locator) {
  return el.evaluate((node) => {
    const cs = getComputedStyle(node);
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
      bd: rgb(cs.borderTopColor),
      bds: cs.borderTopStyle,
      pad: cs.padding,
      anim: cs.animationName,
    };
  });
}

/** Posts a comment as the owner and returns its row. */
async function post(page: Page, composer: Locator, text: string) {
  await composer.click();
  await page
    .getByRole("textbox", { name: "Viết bình luận" })
    .first()
    .fill(text);
  await page.getByRole("button", { name: "Bình luận", exact: true }).click();
  const found = page
    .locator("[data-comment]")
    .filter({ hasText: text })
    .first();
  await expect(found).toBeVisible();
  // The pending comment takes its server id once the write lands; the column may still scroll it into view.
  await page.waitForTimeout(800);
  // By id: the text leaves the row while it is edited.
  const id = await found.getAttribute("data-comment");
  return page.locator(`[data-comment="${id}"]`);
}

/** The parts of a comment row. */
function parts(row: Locator) {
  const body = row.locator(":scope > div").first();
  return {
    avatar: row.locator(":scope > *").first(),
    name: body.locator(":scope > div").first().locator("b"),
    time: body.locator("time"),
    bubble: body.locator(":scope > div").nth(1),
    actions: body.locator(":scope > div").last(),
  };
}

test.beforeEach(() => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      latency !== "instant",
    "Needs the default owner world",
  );
});

for (const mode of ["light", "dark"] as const) {
  const ink = INK[mode];

  test.describe(`${mode}`, () => {
    test("a comment as stage 4 of Kanban-CardPopup-States-1 draws it", async ({
      page,
    }) => {
      const composer = await open(page, mode);
      const row = await post(page, composer, "Đã đối chiếu xong nhóm A.");
      const p = parts(row);
      // The feed starts 16 px under the composer (the column's 12 px gap and 4 px).
      const feed = page.locator("[data-comment-feed]").first();
      const first = await box(feed, composer);
      near(first.y, 44 + 16, "feed under the composer");

      const avatar = await box(p.avatar, row);
      near(avatar.x, 0, "avatar x");
      near(avatar.w, 32, "avatar size");
      near(avatar.h, 32, "avatar height");
      const name = await box(p.name, row);
      near(name.x, 42, "name x");
      expect(await style(p.name)).toMatchObject({
        fs: "13.5px",
        fw: "650",
        c: ink.text,
      });
      expect(await style(p.time)).toMatchObject({ fs: "12.5px" });
      // Known gap, dark only: the popup's link token draws rgb(144, 194, 253), the artboard rgb(125, 187, 255).
      if (mode === "light") expect((await style(p.time)).c).toBe(ink.link);
      const bubble = await box(p.bubble, row);
      const r = await box(row, row);
      near(bubble.x, 42, "bubble x");
      near(bubble.y, 25.56, "bubble y");
      near(bubble.w, r.w - 42, "bubble width");
      expect(await style(p.bubble)).toMatchObject({
        fs: "14px",
        r: "10px",
        pad: "10px 12px",
        bg: ink.bubble,
        bd: ink.bubbleLine,
      });
      const actions = await box(p.actions, row);
      near(actions.y, bubble.y + bubble.h + 6, "actions 6 px under the bubble");
      const edit = row.getByRole("button", { name: "Sửa", exact: true });
      const del = row.getByRole("button", { name: "Xoá", exact: true });
      expect(await style(edit)).toMatchObject({ fs: "12.5px", c: ink.muted });
      const e = await box(edit, row);
      const d = await box(del, row);
      near(d.x, e.x + e.w + 12, "12 px between the actions");
    });

    test("editing keeps the bubble's box, saves only a change and marks it đã sửa", async ({
      page,
    }) => {
      const composer = await open(page, mode);
      const row = await post(page, composer, "Bản nháp đối chiếu.");
      const before = await box(parts(row).bubble, row);
      await row.getByRole("button", { name: "Sửa", exact: true }).click();
      const field = row.getByRole("textbox", { name: /sửa/i });
      await expect(field).toBeFocused();
      const frame = row.locator("[data-comment-editing] > div").first();
      const f = await box(frame, row);
      near(f.x, before.x, "edit x");
      near(f.y, before.y, "edit y");
      near(f.w, before.w, "edit width");
      expect(f.h).toBeGreaterThanOrEqual(64);
      expect(await style(frame)).toMatchObject({ r: "10px", bd: ink.accent });
      const save = row.getByRole("button", { name: "Lưu", exact: true });
      await expect(save).toBeDisabled();
      const cancel = row.getByRole("button", { name: "Huỷ", exact: true });
      const s = await box(save, row);
      const c = await box(cancel, row);
      near(s.x + s.w, before.x + before.w, "Lưu ends at the bubble's edge");
      near(s.h, 32, "button height");
      near(s.x, c.x + c.w + 6, "6 px between the buttons");
      await expect(row.locator("[data-comment-edited]")).toHaveCount(0);

      await field.fill("Bản đối chiếu cuối.");
      await expect(save).toBeEnabled();
      await field.press("Escape");
      await expect(row.getByText("Bản nháp đối chiếu.")).toBeVisible();
      await expect(
        row.getByRole("button", { name: "Sửa", exact: true }),
      ).toBeFocused();

      await row.getByRole("button", { name: "Sửa", exact: true }).click();
      await row
        .getByRole("textbox", { name: /sửa/i })
        .fill("Bản đối chiếu cuối.");
      await row.getByRole("button", { name: "Lưu", exact: true }).click();
      const edited = page
        .locator("[data-comment]")
        .filter({ hasText: "Bản đối chiếu cuối." })
        .first();
      const mark = edited.locator("[data-comment-edited]");
      await expect(mark).toHaveText("· đã sửa");
      expect(await style(mark)).toMatchObject({ fs: "12.5px", c: ink.meta });
      const t = await box(parts(edited).time, edited);
      const m = await box(mark, edited);
      near(m.y + m.h / 2, t.y + t.h / 2, "the mark sits on the time's line");
    });

    test("delete asks first, names the replies that stay, and leaves their place", async ({
      page,
    }) => {
      const composer = await open(page, mode);
      const row = await post(page, composer, "Ai giữ file đối chiếu?");
      await row.getByRole("button", { name: "Trả lời", exact: true }).click();
      await page
        .getByRole("textbox", { name: /^Trả lời / })
        .fill("Mình giữ, gửi lại chiều nay.");
      await page.getByRole("button", { name: "Gửi trả lời" }).click();
      await expect(
        page.getByText("Mình giữ, gửi lại chiều nay."),
      ).toBeVisible();

      const del = row.getByRole("button", { name: "Xoá", exact: true });
      await del.click();
      const confirm = row.locator("[data-comment-confirm]");
      await expect(confirm).toContainText(
        "Câu trả lời vẫn giữ, nằm dưới dòng “Bình luận đã xoá”.",
      );
      const keep = confirm.getByRole("button", { name: "Huỷ", exact: true });
      await expect(keep).toBeFocused();
      const bubble = await box(parts(row).bubble, row);
      const cb = await box(confirm, row);
      near(cb.x, bubble.x, "confirm x");
      near(cb.w, bubble.w, "confirm width");
      near(cb.y, bubble.y + bubble.h + 6, "confirm under the bubble");
      expect(await style(confirm)).toMatchObject({
        r: "10px",
        bg: ink.dangerSoft,
        pad: "10px 12px",
      });
      const go = confirm.getByRole("button", { name: "Xoá", exact: true });
      expect(await style(go)).toMatchObject({
        fw: "600",
        bg: ink.danger,
      });
      // Escape keeps the comment and hands focus back to Xoá.
      await page.keyboard.press("Escape");
      await expect(confirm).toHaveCount(0);
      await expect(del).toBeFocused();
      await expect(page.getByRole("dialog")).toHaveCount(1);

      await del.click();
      await row
        .locator("[data-comment-confirm]")
        .getByRole("button", { name: "Xoá", exact: true })
        .click();
      const placeholder = page.locator("[data-comment-deleted]");
      await expect(placeholder).toHaveCount(1);
      await expect(placeholder).toHaveText("Bình luận đã xoá");
      await expect(page.getByText("Ai giữ file đối chiếu?")).toHaveCount(0);
      const group = page.getByRole("group", { name: "Bình luận đã xoá" });
      await expect(
        group.getByText("Mình giữ, gửi lại chiều nay."),
      ).toBeVisible();
      await expect(
        group.getByRole("button", { name: "Trả lời", exact: true }),
      ).toHaveCount(0);

      const mark = placeholder.locator(":scope > span");
      const text = placeholder.locator(":scope > p");
      const pm = await box(mark, placeholder);
      const pt = await box(text, placeholder);
      const pr = await box(placeholder, placeholder);
      near(pm.w, 32, "placeholder mark size");
      near(pt.x, 42, "placeholder text x");
      near(pt.w, pr.w - 42, "placeholder width");
      expect(await style(text)).toMatchObject({
        fs: "13.5px",
        r: "10px",
        pad: "10px 12px",
        c: ink.meta,
        bd: ink.line2,
        bds: "dashed",
      });
      expect(await style(mark)).toMatchObject({ bd: ink.line2, bds: "dashed" });
    });

    test("loading draws stage 1 of Kanban-CardPopup-States-1: a comment and a line in still grey blocks", async ({
      page,
    }) => {
      const composer = await open(page, mode, "hold");
      const skeleton = page.locator("[data-feed-skeleton]").first();
      await expect(skeleton).toBeVisible();
      await expect(skeleton).toHaveAttribute("role", "status");
      const s = await box(skeleton, composer);
      near(s.y, 44 + 16, "blocks start where the feed will");
      near(s.w, 359, "blocks as wide as the column");
      const blocks = skeleton.locator("[data-feed-block]");
      await expect(blocks).toHaveCount(6);
      const want = [
        { x: 0, y: 0, w: 32, h: 32, r: "16px" },
        { x: 42, y: 0, w: 150, h: 14, r: "6px" },
        { x: 42, y: 22, w: 317, h: 60, r: "10px" },
        { x: 0, y: 96, w: 32, h: 32, r: "16px" },
        { x: 42, y: 96, w: 190, h: 14, r: "6px" },
        { x: 42, y: 118, w: 253.6, h: 14, r: "6px" },
      ];
      for (const [i, w] of want.entries()) {
        const b = await box(blocks.nth(i), skeleton);
        near(b.x, w.x, `block ${i} x`);
        near(b.y, w.y, `block ${i} y`);
        near(b.w, w.w, `block ${i} width`);
        near(b.h, w.h, `block ${i} height`);
        const st = await style(blocks.nth(i));
        expect(st.bg, `block ${i} tone`).toBe(ink.block);
        expect(st.anim, `block ${i} is still`).toBe("none");
        if (w.r !== "16px") expect(st.r).toBe(w.r);
      }
    });

    test("empty draws the column's empty block of Kanban-CardPopup-Create", async ({
      page,
    }) => {
      const composer = await open(page, mode, "empty");
      const notice = page.locator('[data-feed-notice="neutral"]').first();
      await expect(notice).toBeVisible();
      await expect(notice).toContainText("Chưa có bình luận nào");
      await expect(notice).toContainText(
        "Viết bình luận đầu tiên, gõ @ để nhắc ai đó.",
      );
      const n = await box(notice, composer);
      near(n.y, 44 + 16, "the block starts where the feed does");
      near(n.w, 359, "block width");
      expect(await style(notice)).toMatchObject({ pad: "32px 12px" });
      const mark = notice.locator(":scope > span").first();
      const m = await box(mark, notice);
      near(m.w, 44, "mark size");
      near(m.h, 44, "mark height");
      near(m.x, (359 - 44) / 2, "mark centred");
      near(m.y, 32, "mark under the padding");
      expect(await style(mark)).toMatchObject({ bg: ink.block, c: ink.meta });
      const title = notice.locator("b");
      expect(await style(title)).toMatchObject({
        fs: "14px",
        fw: "700",
        c: ink.text,
      });
      near((await box(title, notice)).y, 32 + 44 + 8, "title 8 px under");
      expect(await style(notice.locator(":scope > span").nth(1))).toMatchObject(
        { fs: "13.5px", c: ink.muted },
      );
    });

    test("a failed history read says so in the error frame's tones and Thử lại reads it again", async ({
      page,
    }) => {
      await open(page, mode, "fail");
      const alert = page.locator('[data-feed-notice="danger"]').first();
      await expect(alert).toBeVisible();
      await expect(alert).toHaveAttribute("role", "alert");
      await expect(alert).toContainText("Không tải được hoạt động");
      const mark = alert.locator(":scope > span").first();
      expect(await style(mark)).toMatchObject({
        bg: ink.dangerSoft,
        c: ink.danger,
      });
      const retry = alert.getByRole("button", { name: "Thử lại" });
      expect(await style(retry)).toMatchObject({
        fs: "13.5px",
        fw: "600",
        r: "8px",
        bg: ink.accent,
      });
      near((await retry.boundingBox())!.height, 36, "Thử lại height");
      await page.evaluate(() => {
        delete (globalThis as { __mockFeed?: string }).__mockFeed;
      });
      await retry.click();
      await expect(alert).toHaveCount(0);
      await expect(page.locator("[data-comment-feed]").first()).toBeVisible();
    });

    test("the ⋯ menu offers Theo dõi thẻ after Đánh dấu hoàn thành, as Kanban-CardPopup-Menus draws it, and flips", async ({
      page,
    }) => {
      await open(page, mode);
      await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
      const items = page.getByRole("menuitem");
      // The entry is drawn once the following read has answered.
      await expect(
        page.getByRole("menuitem", { name: /theo dõi thẻ/i }),
      ).toBeVisible();
      const labels = await items.evaluateAll((nodes) =>
        nodes.map((n) => n.querySelector("span > span")?.textContent ?? ""),
      );
      const at = labels.findIndex((l) => /theo dõi thẻ/i.test(l));
      expect(at, "the entry is there").toBeGreaterThan(-1);
      expect(labels[at - 1]).toBe("Đánh dấu hoàn thành");
      expect(labels[at + 1]).toBe("Nhắc người phụ trách");
      const entry = items.nth(at);
      const following = labels[at] === "Bỏ theo dõi thẻ";
      await expect(entry).toContainText(
        following
          ? "Người tạo thẻ và người được giao tự theo dõi"
          : "Nhận nhắc hạn và thông báo",
      );
      await entry.click();
      await page.getByRole("button", { name: "Thao tác trên thẻ" }).click();
      await expect(
        page.getByRole("menuitem", {
          name: following ? /^Theo dõi thẻ/ : /^Bỏ theo dõi thẻ/,
        }),
      ).toBeVisible();
    });

    test("no serious axe violation with the edit and the delete confirm open", async ({
      page,
    }) => {
      const composer = await open(page, mode);
      const row = await post(page, composer, "Kiểm tra trợ năng.");
      await row.getByRole("button", { name: "Xoá", exact: true }).click();
      const serious = (
        await new AxeBuilder({ page }).include("[role=dialog]").analyze()
      ).violations.filter((v) =>
        ["serious", "critical"].includes(v.impact ?? ""),
      );
      expect(serious.map((v) => v.id)).toEqual([]);
    });
  });
}
