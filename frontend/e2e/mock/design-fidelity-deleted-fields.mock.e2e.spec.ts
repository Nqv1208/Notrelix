import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * The deleted fields of a board against the owner canvas boards `WM-DeletedFields` (1440, light and dark) and
 * `WM-DeletedFields-M` (390), rendered from their artboards and measured as boxes, tolerance 1 px. Boxes are read
 * relative to the table (1440) or to the card (390). The app's content is 8 px wider than the artboard's 916 px
 * table, so the columns on the right are read from the table's right edge. The keys and texts differ from the
 * artboard's (the key is derived from the name), so a row is compared on its layout, not its words.
 *
 * Needs the overlay `deleted-fields` (four fields deleted on 20 Oct, 24 Oct, 27 Oct and 2 Oct) and the page clock
 * of 31 Oct 10:40, which is what the board's "Còn N ngày" reads. The first field shares its name with a live one,
 * so its restore is refused (the name-in-use state).
 */
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-31T10:40:00+07:00");
const TOL = 1;

interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

async function open(page: Page, dark: boolean) {
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
  await page.goto(`${BOARD}/settings?from=table&section=fields`);
  await page.getByRole("tab", { name: /Đã xoá/ }).click();
  await page.waitForTimeout(500);
}

async function rel(locator: Locator, origin: Locator): Promise<Rect> {
  const [e, o] = await Promise.all([
    locator.boundingBox(),
    origin.boundingBox(),
  ]);
  if (!e || !o) throw new Error("element is not laid out");
  return { x: e.x - o.x, y: e.y - o.y, width: e.width, height: e.height };
}

async function style(locator: Locator) {
  return locator.evaluate((element) => {
    const s = getComputedStyle(element);
    return { fs: s.fontSize, fw: s.fontWeight, r: s.borderRadius };
  });
}

function near(actual: number, want: number, label: string) {
  expect(
    Math.abs(actual - want),
    `${label}: ${actual.toFixed(1)} vs ${want}`,
  ).toBeLessThanOrEqual(TOL);
}

function box(actual: Rect, want: Partial<Rect>, label: string) {
  for (const key of ["x", "y", "width", "height"] as const) {
    const value = want[key];
    if (value !== undefined) near(actual[key], value, `${label}.${key}`);
  }
}

test.describe("Deleted fields (WM-DeletedFields), 1440 px", () => {
  test.skip(
    !overlays.includes("deleted-fields") ||
      persona !== "owner" ||
      scenario !== "default" ||
      faults !== "",
    "Needs the owner world with the deleted-fields overlay",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`tabs, table header, rows and the Restore button (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      await open(page, dark);
      const table = page.getByRole("table", { name: "Trường đã xoá" });
      const t = (await table.boundingBox())!;
      const right = (r: Rect) => t.width - r.x;

      // Tabs: 40 px high, 4 px apart, 16 px above the table.
      const tabs = page.getByRole("tab");
      const live = await rel(tabs.nth(0), table);
      const gone = await rel(tabs.nth(1), table);
      near(live.height, 40, "tab height");
      near(gone.x - (live.x + live.width), 4, "tab gap");
      near(-gone.y - gone.height, 16, "tabs above the table");
      expect(await style(tabs.nth(1))).toMatchObject({
        fs: "13.5px",
        fw: "600",
      });

      // Header row: 37 px (36 and its line), labels 11.5 px.
      const head = page.getByRole("row").first();
      box(await rel(head, table), { x: 1, y: 1, height: 37 }, "header");
      const label = page.getByRole("columnheader", { name: "Tên trường" });
      box(await rel(label, table), { x: 17, y: 10.7, height: 16.7 }, "label");
      expect(await style(label)).toMatchObject({ fs: "11.5px", fw: "600" });

      // Rows: 92 px for the first, 93 for the others (a 1 px line), the table 316 + 1 px border each side.
      const rows = page.getByRole("row");
      box(await rel(rows.nth(1), table), { y: 38, height: 92 }, "row 1");
      box(await rel(rows.nth(2), table), { y: 130, height: 93 }, "row 2");
      box(await rel(rows.nth(3), table), { y: 223, height: 93 }, "row 3");
      near((await table.boundingBox())!.height, 38 + 92 + 93 * 3 + 1, "table");

      // A row with a two-line sub line ("Ngày bàn giao"), offsets from the row's top.
      const row = rows.filter({ hasText: "Ngày bàn giao" });
      const top = await rel(row, table);
      const at = async (l: Locator) => {
        const r = await rel(l, table);
        return { x: r.x, y: r.y - top.y, width: r.width, height: r.height };
      };
      const icon = row.locator("span.grid").first();
      box(await at(icon), { x: 17, y: 31, width: 32, height: 32 }, "icon");
      expect((await style(icon)).r).toBe("8px");
      const name = row.locator("b");
      box(await at(name), { x: 60, y: 19.5, height: 20.3 }, "name");
      expect(await style(name)).toMatchObject({ fs: "14px", fw: "600" });
      const key = row.locator("small").first();
      box(await at(key), { x: 60, y: 39.8, height: 34.8 }, "key line");
      expect(await style(key)).toMatchObject({ fs: "12px" });
      const type = row.getByRole("cell").nth(1);
      near(right(await at(type)), 699, "type column from the right");
      near((await at(type)).y, 36.6 + 1, "type y");
      const avatar = row.locator("span[aria-hidden='true']").first();
      box(await at(avatar), { width: 22, height: 22 }, "avatar");
      near(right(await at(avatar)), 535, "who column from the right");
      const chip = row.getByText(/^Còn \d+ ngày$/);
      box(await at(chip), { y: 36, height: 22 }, "left chip");
      near(right(await at(chip)), 311, "left column from the right");
      expect(await style(chip)).toMatchObject({
        fs: "12px",
        fw: "600",
        r: "6px",
      });
      const button = row.getByRole("button", { name: /^Khôi phục/ });
      box(await at(button), { y: 33, width: 83.2, height: 28 }, "restore");
      near(right(await at(button)) - 83.2, 17, "restore from the right");
      expect(await style(button)).toMatchObject({
        fs: "12.5px",
        fw: "500",
        r: "7px",
      });
    });
  }

  test("the field with under 3 days left takes the amber chip, the name-in-use refusal sits under its row", async ({
    page,
  }) => {
    await open(page, false);
    const amber = page
      .getByRole("row")
      .filter({ hasText: "Mã hợp đồng" })
      .getByText("Còn 1 ngày");
    await expect(amber).toBeVisible();
    const bg = await amber.evaluate((e) => getComputedStyle(e).backgroundColor);
    const plain = await page
      .getByRole("row")
      .filter({ hasText: "Ngày bàn giao" })
      .getByText("Còn 23 ngày")
      .evaluate((e) => getComputedStyle(e).backgroundColor);
    expect(bg).not.toBe(plain);

    const row = page.getByRole("row").filter({ hasText: "Phòng ban yêu cầu" });
    await row.getByRole("button", { name: /^Khôi phục/ }).click();
    const note = row.getByRole("alert");
    await expect(note).toContainText("đang được một trường khác dùng");
    await expect(
      row.getByRole("button", { name: "Mở trường đang dùng" }),
    ).toBeVisible();
    const r = await rel(note, row);
    near((await rel(note.locator("svg"), row)).x, 60, "note icon x");
    const reason = await rel(note.locator("span").first(), row);
    box(reason, { x: 80, y: 79.1, height: 37.5 }, "reason");
    near(r.y + r.height + 10, (await row.boundingBox())!.height, "row bottom");
  });
});

test.describe("Deleted fields (WM-DeletedFields-M), 390 px", () => {
  test.skip(
    !overlays.includes("deleted-fields") ||
      persona !== "owner" ||
      scenario !== "default" ||
      faults !== "",
    "Needs the owner world with the deleted-fields overlay",
  );
  test.use({ viewport: { width: 390, height: 844 } });

  for (const dark of [false, true]) {
    test(`chips, cards, the 44 px Restore and the note (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      await open(page, dark);
      const chips = page.getByRole("tablist");
      const c = (await chips.boundingBox())!;
      near(c.height, 44, "chips height");
      const tabs = page.getByRole("tab");
      const second = await rel(tabs.nth(1), chips);
      const first = await rel(tabs.nth(0), chips);
      near(second.x - (first.x + first.width), 8, "chip gap");
      for (const i of [0, 1]) {
        const s = await style(tabs.nth(i));
        expect(s.r).toBe("12px");
        expect(s.fs).toBe("14px");
      }
      const selected = await tabs.nth(1).evaluate((e) => {
        const s = getComputedStyle(e);
        return { bg: s.backgroundColor, fg: s.color };
      });
      const idle = await tabs.nth(0).evaluate((e) => {
        const s = getComputedStyle(e);
        return { bg: s.backgroundColor };
      });
      expect(selected.bg).not.toBe(idle.bg);

      // The first card is 358 px wide, 14 px under the chips; cards are 14 px apart.
      const cards = page
        .getByRole("list", { name: "Trường đã xoá" })
        .getByRole("listitem");
      const one = await rel(cards.nth(0), chips);
      box(one, { x: 0, y: 58, width: 358 }, "card 1");
      const two = await rel(cards.nth(1), chips);
      near(two.y - (one.y + one.height), 14, "card gap");

      // A card with a two-line header ("Ngày bàn giao"): 163 px, offsets from the card.
      const card = cards.filter({ hasText: "Ngày bàn giao" });
      const origin = await rel(card, chips);
      const at = async (l: Locator) => {
        const r = await rel(l, chips);
        return {
          x: r.x - origin.x,
          y: r.y - origin.y,
          width: r.width,
          height: r.height,
        };
      };
      near(origin.height, 163, "card height");
      expect((await style(card)).r).toBe("14px");
      const icon = card.locator("span.grid").first();
      box(await at(icon), { x: 17, y: 18.8, width: 36, height: 36 }, "icon");
      expect((await style(icon)).r).toBe("10px");
      const name = card.locator("b");
      box(await at(name), { x: 63, y: 16 }, "name");
      expect(await style(name)).toMatchObject({ fs: "15px", fw: "650" });
      const left = card.getByText(/^Còn \d+ ngày$/);
      const l = await at(left);
      box(l, { y: 24.8, height: 24 }, "left pill");
      near(l.x + l.width, 341, "left pill right edge");
      expect(await style(left)).toMatchObject({
        fs: "12.5px",
        fw: "600",
        r: "7px",
      });
      const meta = card.locator("small").last();
      box(await at(meta), { x: 17, y: 68.5, width: 324, height: 19.5 }, "meta");
      const restore = card.getByRole("button", { name: /^Khôi phục/ });
      box(
        await at(restore),
        { x: 17, y: 98, width: 324, height: 50 },
        "restore",
      );
      expect(await style(restore)).toMatchObject({
        fs: "15px",
        fw: "500",
        r: "12px",
      });

      // The amber pill of the last field (under 3 days), and the closing note 27 px under the last card.
      const amber = cards
        .filter({ hasText: "Mã hợp đồng" })
        .getByText("Còn 1 ngày");
      await expect(amber).toBeVisible();
      const noteBox = await rel(page.getByText(/^Khôi phục đưa trường/), chips);
      const lastCard = await rel(cards.last(), chips);
      near(
        noteBox.y - (lastCard.y + lastCard.height),
        27,
        "note under the cards",
      );
    });
  }

  test("a refused restore keeps the reason and the way to the live field inside the card", async ({
    page,
  }) => {
    await open(page, false);
    const card = page
      .getByRole("listitem")
      .filter({ hasText: "Phòng ban yêu cầu" });
    await card.getByRole("button", { name: /^Khôi phục/ }).click();
    const note = card.getByRole("alert");
    await expect(note).toContainText("đang được một trường khác dùng");
    const origin = (await card.boundingBox())!;
    const n = (await note.boundingBox())!;
    near(n.x - origin.x, 17, "reason x");
    near(n.width, 324, "reason width");
    expect(await style(note)).toMatchObject({ fs: "13.5px", fw: "500" });
    const openLive = card.getByRole("button", { name: "Mở trường đang dùng" });
    const restore = card.getByRole("button", { name: /^Khôi phục/ });
    near((await openLive.boundingBox())!.height, 50, "open height");
    near(
      (await restore.boundingBox())!.y -
        ((await openLive.boundingBox())!.y +
          (await openLive.boundingBox())!.height),
      10,
      "button gap",
    );
  });
});

test.describe("Deleted fields, 1024 px (the merged column) and the restored toast", () => {
  test.skip(
    !overlays.includes("deleted-fields") ||
      persona !== "owner" ||
      scenario !== "default" ||
      faults !== "",
    "Needs the owner world with the deleted-fields overlay",
  );

  test("at 1024 px the Deleted-by and Type columns fold into the name; the left chip and Restore keep the artboard's place from the right", async ({
    page,
  }) => {
    // The workspace bar does not collapse at 1024 yet (shell), so the table is 516 px wide, not the artboard's 756.
    await page.setViewportSize({ width: 1024, height: 800 });
    await open(page, false);
    const table = page.getByRole("table", { name: "Trường đã xoá" });
    for (const name of [/Người xoá/, /^Loại$/])
      await expect(table.getByRole("columnheader", { name })).toBeHidden();
    const t = (await table.boundingBox())!;
    const right = (r: Rect) => t.width - r.x;
    const row = page.getByRole("row").filter({ hasText: "Ngày bàn giao" });
    await expect(row.locator("small").first()).toContainText("Ngày");
    const chip = await rel(row.getByText(/^Còn \d+ ngày$/), table);
    near(right(chip), 281, "left chip from the right");
    const button = await rel(
      row.getByRole("button", { name: /^Khôi phục/ }),
      table,
    );
    near(right(button) - 83.2, 17, "restore from the right");
    const name = await rel(row.locator("b"), table);
    expect(name.width).toBeGreaterThan(100);
  });

  for (const dark of [false, true]) {
    test(`the toast after a restore at 1440 px (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      await page.setViewportSize({ width: 1440, height: 640 });
      await open(page, dark);
      await page
        .getByRole("row")
        .filter({ hasText: "Ghi chú nội bộ" })
        .getByRole("button", { name: /^Khôi phục/ })
        .click();
      const toast = page
        .getByRole("status")
        .filter({ hasText: "Đã khôi phục" });
      await expect(toast).toBeVisible();
      await page.waitForTimeout(300);
      const b = (await toast.boundingBox())!;
      near(b.height, 61.1, "toast height");
      near(b.x + b.width / 2, 720, "toast centre");
      near(640 - (b.y + b.height), 28, "toast from the bottom");
      const s = await toast.evaluate((e) => {
        const c = getComputedStyle(e);
        return [c.borderRadius, c.paddingTop, c.paddingLeft, c.fontSize, c.gap];
      });
      expect(s).toEqual(["10px", "11px", "14px", "13.5px", "12px"]);
      const open_ = toast.getByRole("button", { name: "Mở trường" });
      near((await open_.boundingBox())!.width, 80.8, "link width");
    });
  }

  for (const dark of [false, true]) {
    test(`the toast after a restore at 390 px (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      await page.setViewportSize({ width: 390, height: 844 });
      await open(page, dark);
      await page
        .getByRole("listitem")
        .filter({ hasText: "Ghi chú nội bộ" })
        .getByRole("button", { name: /^Khôi phục/ })
        .click();
      const toast = page
        .getByRole("status")
        .filter({ hasText: "Đã khôi phục" });
      await expect(toast).toBeVisible();
      await page.waitForTimeout(300);
      const b = (await toast.boundingBox())!;
      near(b.x, 16, "toast x");
      near(b.width, 358, "toast width");
      // The artboard's title is one line; this field's longer name wraps the title (18 px more).
      near(b.height, 68 + 14.8, "toast height");
      near(844 - (b.y + b.height), 24, "toast from the bottom");
      const link = (await toast
        .getByRole("button", { name: "Mở trường" })
        .boundingBox())!;
      near(link.height, 44, "link height");
      expect(
        await toast.evaluate((e) => getComputedStyle(e).borderRadius),
      ).toBe("12px");
    });
  }
});

test.describe("Deleted fields, the row being restored (WM-DeletedFields, 'Đang khôi phục một trường')", () => {
  test.skip(
    !overlays.includes("deleted-fields") ||
      persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      process.env.VITE_MOCK_LATENCY !== "slow",
    "Needs the owner world, the deleted-fields overlay and VITE_MOCK_LATENCY=slow",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    test(`the row keeps 92 px, takes the selected fill, its button reads "Đang khôi phục…" at 127.5 px (${dark ? "dem" : "sang"})`, async ({
      page,
    }) => {
      await open(page, dark);
      const table = page.getByRole("table", { name: "Trường đã xoá" });
      const row = page.getByRole("row").filter({ hasText: "Ghi chú nội bộ" });
      const before = await rel(row, table);
      await row.getByRole("button", { name: /^Khôi phục/ }).click();
      const busy = row.getByRole("button", { name: /Khôi phục/ });
      await expect(busy).toHaveText("Đang khôi phục…");
      const r = await rel(row, table);
      near(r.height, before.height, "row height");
      const b = await rel(busy, table);
      near(b.width, 127.5, "busy button width");
      near(
        (await table.boundingBox())!.width - (b.x + b.width),
        17,
        "button right",
      );
      expect(
        await row.evaluate((e) => getComputedStyle(e).backgroundColor),
      ).not.toBe("rgba(0, 0, 0, 0)");
      // The other rows stay pressable.
      await expect(
        page
          .getByRole("row")
          .filter({ hasText: "Mã hợp đồng" })
          .getByRole("button", { name: /^Khôi phục/ }),
      ).toBeEnabled();
    });
  }
});
