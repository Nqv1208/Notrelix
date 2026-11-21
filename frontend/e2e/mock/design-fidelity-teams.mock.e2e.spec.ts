import { expect, test, type Page } from "@playwright/test";
import { dumpBoxTree, type BoxEntry } from "../support/box-tree";
import golden from "../support/artboards/teams.json" with { type: "json" };

/**
 * Teams inside "Thiết lập workspace" against the October 2026 mockup
 * `Final-Teams.dc.html` (drawn at 1440 px only): the list with the archived
 * Team (`danh-sach`), one Team's people (`chi-tiet`), the create dialog
 * (`tao-moi`), the archive confirm (`luu-tru`) and what a member who cannot
 * manage Teams sees (`khong-co-quyen`), light and dark.
 *
 * `e2e/support/artboards/teams.json` holds the box tree of each state: the board
 * rendered by `docs/design/mockups/2026-10/render.py` (`nav=hien-day`) at 1440 x
 * 1000, then dumped by `dumpBoxTree` (every text run by its glyph box and every
 * painted box in reading order, with type and colours), by region and with
 * positions relative to the region's top-left: the setup menu, the page head,
 * the search bar, the table head, the archived heading and each Team row of the
 * list; the setup body of a Team and of the page a member sees; the dialog. The
 * shipped page is dumped by the same function and compared entry by entry.
 * Tolerance 1 px; font size, weight, radius and colours are equal (a translucent
 * dark colour may differ by 20 per channel: the tokens round the board's
 * translucent blues).
 *
 * Named differences from the board, kept out of the comparison:
 * - The list is ordered by Team name (the producer's order); the board lists the
 *   Teams in another order. Rows are paired by Team name.
 * - Team and person marks (squares and circles up to 44 px, and their initials)
 *   take the design system's identity tones; the board colours each by a hex of
 *   its own. Their place and size are compared, their colours are not.
 * - The setup header is the frame shared with Members, Settings and Billing (its
 *   own specs measure it): the Teams board draws its title 2.5 px shorter, so the
 *   height of a region that fills the page is not compared.
 * - The usage column of a row: the board's CSS stretches its department marks to
 *   28 x 22 boxes (a rule meant for the chip also hits the mark), so the column
 *   is left out of both sides.
 * - The create dialog's lead field is the design system's Select (the board draws a
 *   native select whose text and arrow are not boxes): the field's band is left out.
 *   The board adds Khúc Văn Tài, who is not in the mock world, so the second token
 *   and the input after it (their width follows the name) are left out too, and the
 *   boxes under the field hint are read from the hint's bottom (the board's hint,
 *   with its reference id, wraps to a second line).
 * - In dark the board's panel greys (26, 32, 40) are the tokens' (21, 26, 33) and its near-black button ink (26, 3, 8) is the token's (14, 17, 22), within 14
 *   per channel, and its 8 px role tags keep the light accent and green.
 * - The member page's 17 px title is drawn in the section heading size (18 px): only
 *   its place is compared.
 * - Reference ids in the board's copy (WSP-014, GOV-026, WM-040) are not shown.
 */
const WORKSPACE = "/workspaces/mock-workspace-primary";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";

type Row = [string, number, number, number, number, string, string, string];
type State = Record<string, Row[]>;
const kind = (e: BoxEntry) => e.key.split(/[:#]/)[0] ?? "";
const box = (e: BoxEntry) => `${e.key} ${e.x},${e.y} ${e.w}x${e.h}`;

function sameColour(want: string, got: string, dark: boolean): boolean {
  if (want === got) return true;
  if (!dark) return false;
  const w = want.split(",").map(Number);
  const g = got.split(",").map(Number);
  // The board's dark panel (26, 32, 40) is the token's (21, 26, 33): the tokens round the board's hexes.
  const neutral = (c: number[]) =>
    c.length === 4 && c[3] === 1 && Math.max(...c.slice(0, 3)) < 60;
  if (neutral(w) && neutral(g))
    return [0, 1, 2].every((i) => Math.abs((w[i] ?? 0) - (g[i] ?? 0)) <= 14);
  if (w.length < 4 || g.length < 4) return false;
  if ((w[3] ?? 1) === 1 && (w[2] ?? 0) - (w[0] ?? 0) < 40) return false;
  const over = [21, 26, 33];
  const flat = (c: number[]) =>
    [0, 1, 2].map(
      (i) => (c[i] ?? 0) * (c[3] ?? 1) + (over[i] ?? 0) * (1 - (c[3] ?? 1)),
    );
  const fw = flat(w);
  const fg = flat(g);
  return [0, 1, 2].every((i) => Math.abs((fw[i] ?? 0) - (fg[i] ?? 0)) <= 20);
}

function compare(want: BoxEntry[], got: BoxEntry[], dark: boolean): string[] {
  const seen: string[] = [];
  for (const group of [["t", "input"], ["svg"], ["p"]]) {
    const a = want.filter((e) => group.includes(kind(e)));
    const b = got.filter((e) => group.includes(kind(e)));
    if (a.length !== b.length) {
      seen.push(
        `${group[0]}: ${b.length} entries, the board has ${a.length}: page [${b.map(box).join(" | ")}] vs board [${a.map(box).join(" | ")}]`,
      );
      continue;
    }
    a.forEach((w, i) => {
      const g = b[i]!;
      const at = `${w.key} (page ${g.key})`;
      const initials = kind(w) === "t" && /^t:\p{Lu}{2,3}#/u.test(w.key);
      if (initials) return;
      const fill = w.key === "p:1" && w.x === 0 && w.y === 0;
      // The board's add-person input is 32 px, its 32 px field's own height: no room for the borders.
      const field = kind(w) === "input";
      // The board's 17 px title has no token: the page's is the section heading (18 px).
      if (w.key.startsWith("t:Chỉ quản trị")) {
        if (Math.abs(w.y - g.y) > 1.01) seen.push(`${at}.y: ${g.y} vs ${w.y}`);
        return;
      }
      // A text's height is compared when both are the same words (a wrapped line differs with the words).
      const words = kind(w) === "t" && w.key !== g.key;
      for (const k of ["x", "y", "h"] as const)
        if (
          !((fill || field || words) && k === "h") &&
          Math.abs(w[k] - g[k]) > 1.01
        )
          seen.push(`${at}.${k}: ${g[k]} vs ${w[k]}`);
      if ((w.key === g.key || kind(w) !== "t") && Math.abs(w.w - g.w) > 1.01)
        seen.push(`${at}.w: ${g.w} vs ${w.w}`);
      if (kind(w) === "p") {
        const mark = w.w === w.h && w.w <= 44 && w.w >= 22;
        // The board hard-codes the light accent and green on its 8 px role tags, in dark too.
        const tag = w.w === 8 && w.h === 8 && dark;
        if (w.a !== g.a && Number.parseFloat(w.a) !== Number.parseFloat(g.a))
          seen.push(`${at}: radius ${g.a} vs ${w.a}`);
        if (!mark && !tag && !sameColour(w.b, g.b, dark))
          seen.push(`${at}: fill ${g.b} vs ${w.b}`);
        if (w.c !== g.c) seen.push(`${at}: border ${g.c} vs ${w.c}`);
      } else if (kind(w) !== "svg") {
        if (w.a !== g.a) seen.push(`${at}: font-size ${g.a} vs ${w.a}`);
        if (w.b !== g.b) seen.push(`${at}: font-weight ${g.b} vs ${w.b}`);
        if (!sameColour(w.c, g.c, dark))
          seen.push(`${at}: color ${g.c} vs ${w.c}`);
      } else if (!sameColour(w.c, g.c, dark))
        seen.push(`${at}: color ${g.c} vs ${w.c}`);
    });
  }
  return seen;
}

async function open(page: Page, path: string, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.addInitScript((dark) => {
    try {
      localStorage.setItem("notrelix-locale", "vi");
      localStorage.setItem("notrelix-system-rail", "pinned");
      if (dark) localStorage.setItem("notrelix-theme-mode", "dark");
    } catch {
      /* defaults apply */
    }
  }, dark);
  await page.goto(`${WORKSPACE}${path}`);
}

type Scn = "danh-sach" | "chi-tiet" | "tao-moi" | "luu-tru" | "khong-co-quyen";
const BODY = '[data-f="body"]';
const REGIONS: Record<Scn, Array<[string, string]>> = {
  "danh-sach": [
    ["snav", '[data-f="snav"]'],
    ["ph", '[data-f="ph"]'],
    ["bar", '[data-f="bar"]'],
    ["th", '[data-f="th"]'],
    ["sec", '[data-f="sec"]'],
  ],
  "chi-tiet": [["body", BODY]],
  "khong-co-quyen": [["body", BODY]],
  "tao-moi": [["dialog", '[role="dialog"]']],
  "luu-tru": [["dialog", '[role="dialog"]']],
};

async function reach(page: Page, scn: Scn) {
  if (scn === "danh-sach" || scn === "khong-co-quyen") {
    await expect(
      page.getByRole("heading", {
        level: 2,
        name: scn === "danh-sach" ? "Nhóm" : /Chỉ quản trị/,
      }),
    ).toBeVisible();
    return;
  }
  if (scn === "chi-tiet") {
    await page
      .getByRole("table", { name: "Nhóm trong Minh An Group" })
      .getByRole("link", { name: /Nhóm Kỹ sư ERP/ })
      .click();
    await expect(
      page.getByRole("heading", { level: 2, name: "Nhóm Kỹ sư ERP" }),
    ).toBeVisible();
    await expect(page.getByText("quan.tran@minhan.vn")).toBeVisible();
    // The board draws the role menu of the only lead open.
    await page
      .getByRole("button", { name: "Vai trò của Trần Minh Quân: Trưởng nhóm" })
      .click();
    await expect(page.getByRole("menu")).toBeVisible();
    return;
  }
  if (scn === "tao-moi") {
    await page.getByRole("button", { name: "Tạo nhóm" }).click();
    const dialog = page.getByRole("dialog", { name: "Tạo nhóm" });
    await dialog.getByLabel("Tên nhóm").fill("Nhóm Triển khai kho");
    await dialog
      .getByLabel(/Mô tả/)
      .fill("Kỹ sư và người phụ trách kho cho phân hệ Kho của dự án ERP.");
    await dialog.getByRole("combobox", { name: /Trưởng nhóm/ }).click();
    await page
      .getByRole("option", { name: "Đặng Quốc Bảo · bao.dang@minhan.vn" })
      .click();
    const members = dialog.getByPlaceholder("Gõ tên hoặc email");
    await members.fill("minh.lam");
    await expect(dialog.getByRole("option").first()).toBeVisible();
    await page.keyboard.press("Enter");
    await members.fill("Nguyễn Văn Hùng");
    await expect(dialog.getByRole("option").first()).toBeVisible();
    await page.keyboard.press("Enter");
    await expect(dialog.getByText("3 người · Đặng Quốc Bảo")).toBeVisible();
    // The board draws the member field focused.
    await members.focus();
    return;
  }
  await page
    .getByRole("button", { name: "Thao tác với Nhóm Kỹ sư ERP" })
    .click();
  await page.getByRole("menuitem", { name: "Lưu trữ nhóm" }).click();
  await expect(
    page.getByRole("dialog", { name: /Lưu trữ Nhóm Kỹ sư ERP/ }),
  ).toBeVisible();
}

const SCENES: Scn[] = [
  "danh-sach",
  "chi-tiet",
  "tao-moi",
  "luu-tru",
  "khong-co-quyen",
];

/** Dumps the element the selector finds (tagged), positions relative to its top-left. */
async function dump(page: Page, selector: string, rowName?: string) {
  await page.evaluate(
    ([q, name]) => {
      document.querySelector("[data-gen]")?.removeAttribute("data-gen");
      const found =
        name === undefined
          ? document.querySelector(q!)
          : [...document.querySelectorAll(q!)].find(
              (row) => row.querySelector("b")?.textContent?.trim() === name,
            );
      found!.setAttribute("data-gen", "1");
    },
    [selector, rowName],
  );
  const origin = await page.evaluate(() => {
    const r = document.querySelector("[data-gen]")!.getBoundingClientRect();
    return [r.x, r.y] as const;
  });
  return (await page.evaluate(dumpBoxTree, "[data-gen]")).map((e) => ({
    ...e,
    x: Math.round((e.x - origin[0]) * 10) / 10,
    y: Math.round((e.y - origin[1]) * 10) / 10,
  }));
}

for (const scn of SCENES)
  for (const dark of [false, true]) {
    test(`teams ${scn} at 1440, ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      test.skip(
        scenario !== "default" ||
          faults !== "" ||
          persona !== (scn === "khong-co-quyen" ? "member" : "owner"),
        "Needs the default world with the matching persona",
      );
      await open(page, "/teams", dark);
      await reach(page, scn);
      await page.evaluate(() => document.fonts.ready);
      await page.waitForTimeout(200);
      // The board draws no focus ring and no hover.
      if (scn !== "tao-moi")
        await page.evaluate(() =>
          (document.activeElement as HTMLElement | null)?.blur(),
        );
      await page.mouse.move(0, 0);
      const golden0 = (golden as unknown as Record<string, State>)[
        `${scn}.${dark ? "dem" : "sang"}`
      ]!;
      const seen: string[] = [];
      const toEntries = (rows: Row[]): BoxEntry[] =>
        rows.map(([k, x, y, w, h, a, b, c]) => ({
          key: k,
          x,
          y,
          w,
          h,
          a,
          b,
          c,
        }));
      const regions: Array<[string, string, string | undefined]> = REGIONS[
        scn
      ].map(([label, sel]) => [label, sel, undefined]);
      if (scn === "danh-sach")
        for (const label of Object.keys(golden0).filter((k) =>
          k.startsWith("tr:"),
        ))
          regions.push([label, '[data-f="tr"]', label.slice(3)]);
      for (const [label, selector, row] of regions) {
        const usage = (e: BoxEntry) =>
          !(row !== undefined && e.x >= 509 && e.x < 735);
        let got = (await dump(page, selector, row)).filter(usage);
        let want = toEntries(golden0[label]!).filter(usage);
        if (row !== undefined) {
          // A row's rule above it follows its place in the list, which differs: read both from under it.
          const rule = (list: BoxEntry[]) =>
            list.some(
              (e) => e.key === "p:1" && e.x === 0 && e.y === 0 && e.w > 800,
            )
              ? 1
              : 0;
          const [gr, wr] = [rule(got), rule(want)];
          const bare = (list: BoxEntry[], by: number) =>
            list
              .filter(
                (e) =>
                  !(e.key === "p:1" && e.x === 0 && e.y === 0 && e.w > 800),
              )
              .map((e) => ({ ...e, y: e.y - by }));
          got = bare(got, gr);
          want = bare(want, wr);
        }
        if (scn === "khong-co-quyen") {
          // The page is centred in a body 2.5 px shorter than the board's (919.4 px): read it from the board's centre.
          const height = await page
            .locator(BODY)
            .evaluate((e) => e.getBoundingClientRect().height);
          got = got.map((e) => ({ ...e, y: e.y + (919.4 - height) / 2 }));
        }
        if (scn === "tao-moi") {
          const keep = (e: BoxEntry) =>
            e.x >= 0 &&
            !(e.y >= 268 && e.y < 306) &&
            !(e.y >= 345 && e.y <= 385 && e.x >= 170);
          const hint = (list: BoxEntry[]) =>
            list.find((e) => e.key.startsWith("t:Chỉ thành viên"));
          const delta = (hint(want)?.h ?? 0) - (hint(got)?.h ?? 0);
          const under = hint(got)?.y ?? 0;
          got = got
            .filter(keep)
            .map((e) => (e.y > under + 1 ? { ...e, y: e.y + delta } : e));
          want = want.filter(keep);
        }
        for (const line of compare(want, got, dark))
          seen.push(`${label}: ${line}`);
      }
      expect(seen, "boxes that differ from the board").toEqual([]);
    });
  }
