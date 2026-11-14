import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The remaining tiles of the owner-canvas board `WM-Filter-States` (read live from `WM_CANVAS_DIR`; each test skips when
 * the board or its world is missing): the stale saved-filter row of the menu, the save-error box with "Thử lại" (world
 * `VITE_MOCK_FAULTS="PUT /preferences=500"`), the inline condition errors, the viewer's note (`VITE_MOCK_PERSONA=viewer`),
 * the empty swimlane note, and the restoring and restored cards. The tiles are 434 px cards in a 3-column grid, the app's
 * regions are as wide as the board, so every box is read against its own parent and widths of fluid boxes are not compared.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const FILE = "WM-Filter-States.dc.html";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const WS = "/workspaces/mock-workspace-primary/boards";
const ERP = `${WS}/mock-board-erp`;
const ROADMAP = `${WS}/mock-board-roadmap`;

type Box = readonly [number, number, number, number];
const near = (a: number, b: number, what: string) =>
  expect(Math.abs(a - b), `${what}: ${a} vs ${b}`).toBeLessThanOrEqual(1);
const nearBox = (got: Box, want: Box, what: string, skip: number[] = []) => {
  for (let i = 0; i < 4; i++) {
    if (skip.includes(i)) continue;
    near(got[i]!, want[i]!, `${what} ${["x", "y", "width", "height"][i]}`);
  }
};

/** A box read against `o` (the parent the tile and the app share). */
const REL = `const rel = (el, o) => { const b = el.getBoundingClientRect(); return [b.x - o.x, b.y - o.y, b.width, b.height]; };`;

async function artboard<T>(page: Page, fn: string): Promise<T> {
  await page.goto(pathToFileURL(`${CANVAS}/${FILE}`).href);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(300);
  return page.evaluate((src) => (0, eval)(src)(), fn) as Promise<T>;
}

async function open(page: Page, url: string, width = 1440) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", "light");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.setViewportSize({ width, height: 900 });
  await page.goto(url);
  await page.evaluate(() => document.fonts.ready);
}

const owner = persona === "owner" && scenario === "default";

test.describe("Filter states tiles against the canvas", () => {
  test.skip(!existsSync(`${CANVAS}/${FILE}`), "Needs the canvas boards");

  test("the stale saved-filter row in the menu", async ({ page }) => {
    test.skip(!owner || faults !== "", "Needs the plain owner world");
    const want = await artboard<Record<string, Box>>(
      page,
      `() => { ${REL}
        const menu = [...document.querySelectorAll('.menu')].find(m => m.textContent.includes('Việc gấp tuần này'));
        const row = menu.querySelector('.mrow');
        return {
          row: [0, 0, row.getBoundingClientRect().width, row.getBoundingClientRect().height],
          icon: rel(row.querySelector('svg'), row.getBoundingClientRect()),
          name: rel(row.querySelector('b'), row.getBoundingClientRect()),
          hint: rel(row.querySelector('b').nextElementSibling, row.getBoundingClientRect()),
          count: rel(row.querySelector('small'), row.getBoundingClientRect()),
        };
      }`,
    );
    await open(page, `${ROADMAP}?view=table`);
    await page.getByRole("button", { name: /Bộ lọc đã lưu/ }).click();
    const menu = page.locator('[aria-label="Bộ lọc đã lưu"][role="dialog"]');
    await expect(menu).toBeVisible();
    await expect(menu).toHaveCSS("transform", "none");
    const row = menu
      .locator("li")
      .filter({ hasText: "Việc gấp tuần này" })
      .first();
    await expect(row).toBeVisible();
    const got = await row.evaluate((li) => {
      const r = li.getBoundingClientRect();
      const rel = (el: Element) => {
        const b = el.getBoundingClientRect();
        return [b.x - r.x, b.y - r.y, b.width, b.height];
      };
      const name = li.querySelector("b")!;
      return {
        row: [0, 0, r.width, r.height],
        icon: rel(li.querySelector("svg")!),
        name: rel(name),
        hint: rel(name.nextElementSibling!),
        count: rel(
          [...li.querySelectorAll("small")].find((e) =>
            /^\d+ bị bỏ qua$/.test(e.textContent ?? ""),
          )!,
        ),
      };
    });
    nearBox(got.row as Box, want.row!, "row", [0, 2]);
    nearBox(got.icon as Box, want.icon!, "icon", [0]);
    nearBox(got.name as Box, want.name!, "name", [0, 2]);
    nearBox(got.hint as Box, want.hint!, "hint", [0, 2]);
    near(got.count![1]!, want.count![1]!, "count y");
    near(got.count![3]!, want.count![3]!, "count height");
  });

  test("the save-error box and its Thử lại", async ({ page }) => {
    test.skip(
      !owner || !faults.includes("PUT /preferences"),
      "Needs VITE_MOCK_FAULTS=PUT /preferences=500",
    );
    const want = await artboard<Record<string, Box>>(
      page,
      `() => { ${REL}
        const box = document.querySelector('.errbox[role="alert"]');
        const o = box.getBoundingClientRect();
        const btn = box.querySelector('button');
        return {
          box: [0, 0, o.width, o.height],
          icon: rel(box.querySelector('svg'), o),
          text: rel(box.querySelector('span'), o),
          btn: rel(btn, o),
          btnIcon: rel(btn.querySelector('svg'), btn.getBoundingClientRect()),
        };
      }`,
    );
    await open(page, `${ROADMAP}?view=table`);
    await page.getByRole("button", { name: /^Nhóm/ }).first().click();
    await page
      .getByRole("menuitemradio", { name: /Ưu tiên|Priority/ })
      .first()
      .click();
    const alert = page
      .getByRole("alert")
      .filter({ hasText: "Chưa lưu được cách xem" });
    await expect(alert).toBeVisible({ timeout: 5000 });
    const got: Record<string, number[]> = await alert.evaluate((box) => {
      const o = box.getBoundingClientRect();
      const rel = (el: Element, p: DOMRect) => {
        const b = el.getBoundingClientRect();
        return [b.x - p.x, b.y - p.y, b.width, b.height];
      };
      const btn = box.querySelector("button")!;
      return {
        box: [0, 0, o.width, o.height],
        icon: rel(box.querySelector("svg")!, o),
        text: rel(box.querySelector("span")!, o),
        btn: rel(btn, o),
        btnIcon: rel(btn.querySelector("svg")!, btn.getBoundingClientRect()),
      };
    });
    nearBox(got.icon as Box, want.icon!, "icon");
    nearBox(got.text as Box, want.text!, "text", [2, 3]);
    // The tile wraps the sentence on two lines; the padding around the text is what carries over.
    const inner = (v: Record<string, number[]>) =>
      v.box![3]! - Math.max(v.text![3]!, v.btn![3]!);
    near(
      inner(got),
      inner(want),
      "padding around the tallest of text and button",
    );
    nearBox(got.btn as Box, want.btn!, "button", [0]);
    nearBox(got.btnIcon as Box, want.btnIcon!, "button icon");
  });

  test("the restoring skeleton", async ({ page }) => {
    test.skip(!owner || latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    const want = await artboard<Record<string, Box>>(
      page,
      `() => { ${REL}
        const card = document.querySelector('[aria-label="Đang khôi phục cách xem"]');
        const o = card.getBoundingClientRect();
        const bars = [...card.children].map((c) => rel(c, o));
        return { card: [0, 0, o.width, o.height], bar0: bars[0], bar1: bars[1], bar2: bars[2] };
      }`,
    );
    await open(page, `${ROADMAP}?view=table`);
    const card = page.getByRole("status", { name: "Đang khôi phục cách xem" });
    await expect(card).toBeVisible();
    const got = await card.evaluate((el) => {
      const o = el.getBoundingClientRect();
      const bars = [...el.children].map((c) => {
        const b = c.getBoundingClientRect();
        return [b.x - o.x, b.y - o.y, b.width, b.height];
      });
      return {
        card: [0, 0, o.width, o.height],
        bar0: bars[0],
        bar1: bars[1],
        bar2: bars[2],
      };
    });
    near(got.card![3]!, want.card![3]!, "card height");
    nearBox(got.bar0 as Box, want.bar0!, "bar0", [2]);
    nearBox(got.bar1 as Box, want.bar1!, "bar1", [2]);
    nearBox(got.bar2 as Box, want.bar2!, "bar2", [2]);
    // The first bar is 60 % of the card's inner width, the others fill it.
    near(got.bar0![2]! / got.bar1![2]!, 0.6, "bar0 share");
  });

  test("the restored card and its note", async ({ page }) => {
    test.skip(!owner || faults !== "", "Needs the plain owner world");
    const want = await artboard<Record<string, number[]>>(
      page,
      `() => { ${REL}
        const line = [...document.querySelectorAll('.tile')].find(t => t.textContent.includes('Đã khôi phục cách xem của bạn'));
        const card = line.querySelector('.card');
        const o = card.getBoundingClientRect();
        const row = card.firstElementChild;
        const note = line.querySelector('p.note');
        return {
          card: [0, 0, o.width, o.height],
          row: rel(row, o),
          icon: rel(row.querySelector('svg'), o),
          btn: rel(row.querySelector('button'), o),
          noteY: [0, note.getBoundingClientRect().y - (o.y + o.height), 0, note.getBoundingClientRect().height],
        };
      }`,
    );
    await open(page, `${ROADMAP}?view=table`);
    await page.getByRole("button", { name: /^Nhóm/ }).first().click();
    await page
      .getByRole("menuitemradio", { name: /Ưu tiên|Priority/ })
      .first()
      .click();
    await page.waitForTimeout(900);
    // The mock world lives in the page, so leave and come back inside the app: the saved view returns as it was.
    await page.getByRole("link", { name: "Trang chủ" }).first().click();
    await page.waitForURL(/mock-workspace-primary\/?$/);
    await page.goBack();
    const status = page
      .getByRole("status")
      .filter({ hasText: "Đã khôi phục cách xem của bạn" });
    await expect(status).toBeVisible({ timeout: 5000 });
    const got = await status.evaluate((s) => {
      const card = s.parentElement!;
      const o = card.getBoundingClientRect();
      const rel = (el: Element) => {
        const b = el.getBoundingClientRect();
        return [b.x - o.x, b.y - o.y, b.width, b.height];
      };
      const note = card.nextElementSibling!.getBoundingClientRect();
      return {
        card: [0, 0, o.width, o.height],
        row: rel(s),
        icon: rel(s.querySelector("svg")!),
        btn: rel(s.querySelector("button")!),
        noteY: [0, note.y - (o.y + o.height), 0, note.height],
      };
    });
    // The tile wraps its text on two lines and the app's card is as wide as the board: compare what does not depend on
    // the wrap (the padding around the row, the button, the icon centred on the row, the note's line height and gap).
    const pad = (v: Record<string, number[]>) => [
      v.row![1]!,
      v.card![3]! - v.row![1]! - v.row![3]!,
    ];
    near(pad(got)[0]!, 12 + 0, "padding above");
    near(pad(got)[0]!, pad(want)[0]!, "padding above vs canvas");
    near(pad(got)[1]!, pad(want)[1]!, "padding below vs canvas");
    nearBox(
      got.btn as unknown as Box,
      want.btn as unknown as Box,
      "button",
      [0, 1],
    );
    near(
      got.icon![1]! + 8 - got.row![3]! / 2 - got.row![1]!,
      want.icon![1]! + 8 - want.row![3]! / 2 - want.row![1]!,
      "icon centred",
    );
    near(got.noteY![1]!, want.noteY![1]!, "gap to note");
    const line = (h: number) => h / Math.max(1, Math.round(h / 18));
    near(line(got.noteY![3]!), line(want.noteY![3]!), "note line height");
  });
  test("the inline errors of a condition row", async ({ page }) => {
    test.skip(!owner || faults !== "", "Needs the plain owner world");
    const want = await artboard<Record<string, number[] | string>>(
      page,
      `() => { ${REL}
        const card = [...document.querySelectorAll('.tile')].find(t => t.textContent.includes('Lỗi nhập ngay trong hàng')).querySelector('.card');
        const inputs = [...card.querySelectorAll('.inp[style*="danger"]')];
        const msgs = [...card.querySelectorAll('span[style*="font-size:12.5px"]')];
        const cs = (el) => getComputedStyle(el);
        const gap = (i, m) => m.getBoundingClientRect().y - (i.getBoundingClientRect().y + i.getBoundingClientRect().height);
        return {
          numberBorder: cs(inputs[0]).borderTopColor, numberHeight: [inputs[0].getBoundingClientRect().height],
          numberMsgColor: cs(msgs[0]).color, numberMsgSize: cs(msgs[0]).fontSize, numberGap: [gap(inputs[0], msgs[0])],
          rangeBorder: cs(inputs[1]).borderTopColor, rangeHeight: [inputs[1].getBoundingClientRect().height],
          rangeMsgColor: cs(msgs[1]).color, rangeGap: [gap(inputs[2], msgs[1])],
        };
      }`,
    );
    await open(
      page,
      `${ERP}?view=table&filter=${encodeURIComponent(
        "c2:and~mock-erp-col-estimate.gte.~mock-erp-col-golive.between.2026-10-20.2026-10-12",
      )}`,
    );
    await page.getByRole("button", { name: /^Lọc/ }).first().click();
    const dialog = page.getByRole("dialog", { name: "Lọc việc" });
    await expect(dialog).toBeVisible();
    const alerts = dialog.getByRole("alert");
    await expect(alerts).toHaveCount(2);
    const got = await dialog.evaluate((root) => {
      const alertEls = [...root.querySelectorAll('[role="alert"]')];
      const cs = (el: Element) => getComputedStyle(el);
      const inputOf = (a: Element, sel: string) =>
        a.previousElementSibling!.querySelector(sel) as HTMLElement;
      const gap = (i: Element, m: Element) =>
        m.getBoundingClientRect().y -
        (i.getBoundingClientRect().y + i.getBoundingClientRect().height);
      const num = inputOf(alertEls[0]!, 'input[type="number"]');
      const date = inputOf(alertEls[1]!, 'input[type="date"]');
      return {
        numberBorder: cs(num).borderTopColor,
        numberHeight: [num.getBoundingClientRect().height],
        numberMsgColor: cs(alertEls[0]!).color,
        numberMsgSize: cs(alertEls[0]!).fontSize,
        numberGap: [gap(num.closest("div.min-w-0") ?? num, alertEls[0]!)],
        rangeBorder: cs(date).borderTopColor,
        rangeHeight: [date.getBoundingClientRect().height],
        rangeMsgColor: cs(alertEls[1]!).color,
      };
    });
    // Colours are the design system's tokens (the danger red is oklch there, #C42340 in the artboard): not compared.
    near(
      got.numberGap[0]!,
      (want.numberGap as number[])[0]!,
      "gap input to message",
    );
    expect(got.numberMsgSize).toBe(want.numberMsgSize);
    near(
      got.numberHeight[0]!,
      (want.numberHeight as number[])[0]!,
      "input height",
    );
    near(
      got.rangeHeight[0]!,
      (want.rangeHeight as number[])[0]!,
      "date input height",
    );
  });
  test("the viewer's disabled save and its note", async ({ page }) => {
    test.skip(
      persona !== "viewer" || scenario !== "default" || faults !== "",
      "Needs VITE_MOCK_PERSONA=viewer",
    );
    const want = await artboard<Record<string, number[]>>(
      page,
      `() => { ${REL}
        const tile = [...document.querySelectorAll('.tile')].find(t => t.textContent.includes('Người chỉ xem · lọc được'));
        const tip = tile.querySelector('.tip');
        const o = tip.getBoundingClientRect();
        const pill = tile.querySelector('button[disabled]');
        return {
          tip: [0, 0, o.width, o.height],
          icon: rel(tip.querySelector('svg'), o),
          pill: [0, 0, pill.getBoundingClientRect().width, pill.getBoundingClientRect().height],
          pillIcon: rel(pill.querySelector('svg'), pill.getBoundingClientRect()),
        };
      }`,
    );
    await open(
      page,
      `${ERP}?view=table&filter=${encodeURIComponent("c2:and~mock-erp-col-priority.has_any.mock-erp-priority-high")}`,
    );
    const pill = page.getByRole("button", { name: "Lưu bộ lọc" });
    await expect(pill).toBeDisabled();
    await pill.locator("xpath=..").focus();
    const tip = page.locator('[data-slot="tooltip-content"]');
    await expect(tip).toBeVisible();
    await page.waitForTimeout(300);
    const got = await tip.evaluate((el) => {
      const o = el.getBoundingClientRect();
      const i = el.querySelector("svg")!.getBoundingClientRect();
      return {
        tip: [0, 0, o.width, o.height],
        icon: [i.x - o.x, i.y - o.y, i.width, i.height],
      };
    });
    const pb = await pill.boundingBox();
    nearBox(got.tip as Box, want.tip as unknown as Box, "tooltip", [0, 1]);
    nearBox(got.icon as Box, want.icon as unknown as Box, "lock");
    // The tile draws a 30 px round pill; the bar of the table and kanban boards draws 32 px (measured by their own specs): only the note is compared.
    expect(pb!.height).toBeGreaterThan(0);
  });
  test("the empty swimlane note", async ({ page }) => {
    test.skip(!owner || faults !== "", "Needs the plain owner world");
    const want = await artboard<Record<string, number[] | string>>(
      page,
      `() => { ${REL}
        const tile = [...document.querySelectorAll('.tile')].find(t => t.textContent.includes('Làn không có việc khớp'));
        const cell = tile.querySelector('.cell');
        const o = cell.getBoundingClientRect();
        const note = cell.querySelector('.note');
        const cs = getComputedStyle(note);
        return {
          cell: [0, 0, o.width, o.height],
          note: rel(note, o),
          border: cs.borderTopWidth + ' ' + cs.borderTopStyle,
          radius: cs.borderTopLeftRadius,
          size: cs.fontSize,
          lane: [tile.querySelector('.lh').getBoundingClientRect().height],
        };
      }`,
    );
    await open(
      page,
      `${ERP}?view=kanban&filter=${encodeURIComponent("c2:and~mock-erp-col-priority.has_any.mock-erp-priority-high")}`,
    );
    await page.getByRole("button", { name: /^Làn/ }).first().click();
    await page
      .getByRole("menuitemradio", { name: /Phân hệ ERP/ })
      .first()
      .click();
    const note = page.locator("[data-swimlane-empty]").first();
    await expect(note).toBeVisible({ timeout: 5000 });
    const got = await note.evaluate((cell) => {
      const o = cell.getBoundingClientRect();
      const span = cell.firstElementChild!;
      const b = span.getBoundingClientRect();
      const cs = getComputedStyle(span);
      return {
        cell: [0, 0, o.width, o.height],
        note: [b.x - o.x, b.y - o.y, b.width, b.height],
        border: cs.borderTopWidth + " " + cs.borderTopStyle,
        radius: cs.borderTopLeftRadius,
        size: cs.fontSize,
      };
    });
    expect(got.border).toBe(want.border);
    expect(got.radius).toBe(want.radius);
    expect(got.size).toBe(want.size);
    near(got.cell[3]!, (want.cell as number[])[3]!, "cell height");
    // The tile's note is as wide as its cell: the same 8 px inset on both sides.
    near(got.note[1]!, (want.note as number[])[1]!, "note y");
    near(got.note[3]!, (want.note as number[])[3]!, "note height");
    near(got.note[0]!, (want.note as number[])[0]!, "note x");
  });
});
