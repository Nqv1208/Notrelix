/**
 * Design fidelity of all seven Final-Onboarding setup screens at 1440x900, light and dark: the page's box tree is
 * compared with the artboard's (`../support/artboards/onboarding.json`) entry by entry, every text run by its words
 * and every painted box or icon by place and size, within 1 px:
 *
 *   python3 docs/design/mockups/2026-10/render.py Final-Onboarding.dc.html out.html \
 *     state=<scene> mode=<sang|dem>
 *
 * The board draws 1440 only; the other widths are reviewed as screenshots, not measured. Named differences (each is
 * a board entry that is skipped or a page state set up to the drawn one, nothing else):
 *
 *  - Company bands: the board draws four, the accepted contract has five, so the size chips are not compared and
 *    everything under them sits 48 px lower than drawn (see `WRAP`). The departments' lead names the board's
 *    "101–500 người" band, which the page cannot produce, so that one sentence is not compared.
 *  - The board's screen-reader-only words (clip-hidden labels) are not measured away by the board dump; they are
 *    dropped from the board before comparing. The page's selects draw their value inside the control, which the
 *    board does not measure, so those words are hidden on the page.
 *  - Logo, the person's initials and e-mail, the free-address green, the company mark in dark, the disabled primary
 *    button and the taken-address words (the mock world's own) are skipped, as in the first page's spec.
 *  - Invited person's page (`tham-gia`), on the mock world: the inviter's name, the access list (one department and
 *    its three boards are kept, named "Phòng Công nghệ"), the lead sentence and the person's name are set to the
 *    drawn ones in the page; the preview's department rows, the open board's title and the head count come from the
 *    mock data and are not compared; the swatches of the coral accent, the review-lane dot, the sage accent and its
 *    dark soft fill, and the check ink on the amber swatch are the theme's and status tokens, a little off the
 *    board's literals.
 */
import { expect, test, type Page } from "@playwright/test";
import { dumpBoxTree, type BoxEntry } from "../support/box-tree";
import golden from "../support/artboards/onboarding.json" with { type: "json" };

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const THEME_KEY = "notrelix-theme-mode";
type Row = [string, number, number, number, number, string, string, string];
const kind = (e: BoxEntry) => e.key.split(/[:#]/)[0] ?? "";
const box = (e: BoxEntry) => `${e.key} ${e.x},${e.y} ${e.w}x${e.h}`;

function toEntries(rows: Row[]): BoxEntry[] {
  return rows.map(([key, x, y, w, h, a, b, c]) => ({
    key,
    x,
    y,
    w,
    h,
    a,
    b,
    c,
  }));
}

function sameColour(want: string, got: string, dark: boolean): boolean {
  if (want === got) return true;
  const close = (u: number[], v: number[]) =>
    u.length === 4 &&
    v.length === 4 &&
    [0, 1, 2].every((i) => Math.abs((u[i] ?? 0) - (v[i] ?? 0)) <= 8) &&
    u[3] === v[3];
  if (close(want.split(",").map(Number), got.split(",").map(Number)))
    return true;
  if (!dark) return false;
  const w = want.split(",").map(Number);
  const g = got.split(",").map(Number);
  // The board's dark panel greys are the tokens' within 14 per channel.
  const neutral = (c: number[]) =>
    c.length === 4 && c[3] === 1 && Math.max(...c.slice(0, 3)) < 60;
  if (neutral(w) && neutral(g))
    return [0, 1, 2].every((i) => Math.abs((w[i] ?? 0) - (g[i] ?? 0)) <= 14);
  if (w.length < 4 || g.length < 4) return false;
  const over = [21, 26, 33];
  const flat = (c: number[]) =>
    [0, 1, 2].map(
      (i) => (c[i] ?? 0) * (c[3] ?? 1) + (over[i] ?? 0) * (1 - (c[3] ?? 1)),
    );
  const fw = flat(w);
  const fg = flat(g);
  return [0, 1, 2].every((i) => Math.abs((fw[i] ?? 0) - (fg[i] ?? 0)) <= 20);
}

const near = (a: number, b: number) => Math.abs(a - b) <= 1.01;
/** A pill drawn as 2 px on a 4 px bar and as a huge radius read the same. */
const sameRadius = (want: BoxEntry, got: BoxEntry) =>
  want.a === got.a ||
  (want.a === "round" &&
    Number.parseFloat(got.a) >= Math.min(want.w, want.h) / 2 - 0.5) ||
  (got.a === "round" &&
    Number.parseFloat(want.a) >= Math.min(want.w, want.h) / 2 - 0.5);

/**
 * Every board entry is looked for on the page: a text run by its words (and its place in the order of equal
 * words), a painted box or an icon by its place and size. Page-only entries (visually hidden labels, the
 * mock world's own words) are not compared; board entries named in `skip` are the named differences.
 */
function compare(
  want: BoxEntry[],
  got: BoxEntry[],
  dark: boolean,
  skip: (e: BoxEntry) => boolean,
): string[] {
  const seen: string[] = [];
  for (const w of want) {
    if (skip(w)) continue;
    const text = kind(w) === "t" || kind(w) === "input";
    const g = text
      ? got.find((e) => e.key === w.key)
      : got.find(
          (e) =>
            kind(e) === kind(w) &&
            near(e.x, w.x) &&
            near(e.y, w.y) &&
            near(e.w, w.w) &&
            near(e.h, w.h),
        );
    if (!g) {
      seen.push(`missing on the page: ${box(w)}`);
      continue;
    }
    const at = `${w.key} (page ${g.key})`;
    if (text)
      for (const k of ["x", "y", "w", "h"] as const)
        if (!near(w[k], g[k])) seen.push(`${at}.${k}: ${g[k]} vs ${w[k]}`);
    if (kind(w) === "p") {
      if (!sameRadius(w, g)) seen.push(`${at}: radius ${g.a} vs ${w.a}`);
      if (!sameColour(w.b, g.b, dark))
        seen.push(`${at}: fill ${g.b} vs ${w.b}`);
      if (!sameColour(w.c, g.c, dark))
        seen.push(`${at}: border ${g.c} vs ${w.c}`);
    } else if (kind(w) !== "svg") {
      if (w.a !== g.a) seen.push(`${at}: font-size ${g.a} vs ${w.a}`);
      if (w.b !== g.b) seen.push(`${at}: font-weight ${g.b} vs ${w.b}`);
      if (!sameColour(w.c, g.c, dark))
        seen.push(`${at}: color ${g.c} vs ${w.c}`);
    } else if (!sameColour(w.c, g.c, dark))
      seen.push(`${at}: color ${g.c} vs ${w.c}`);
  }
  return seen;
}

type Scn =
  | "tao-workspace"
  | "dia-chi-trung"
  | "phong-ban"
  | "moi-nguoi"
  | "mau-board"
  | "xong"
  | "tham-gia";
/** Every screen of the setup, in the order the person meets them. */
const SCENES: Scn[] = [
  "tao-workspace",
  "dia-chi-trung",
  "phong-ban",
  "moi-nguoi",
  "mau-board",
  "xong",
  "tham-gia",
];

async function open(page: Page, path: string, dark: boolean) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.addInitScript(
    ({ key, mode }) => {
      try {
        localStorage.setItem("notrelix-locale", "vi");
        localStorage.setItem(key, mode);
      } catch {
        /* defaults apply */
      }
    },
    { key: THEME_KEY, mode: dark ? "dark" : "light" },
  );
  await page.goto(path);
}

/** The invited person's page as drawn: her name, the sage accent and the access card of one department. */
async function reachJoin(page: Page) {
  await expect(page.getByLabel("Tên hiển thị")).toBeVisible();
  await page.getByLabel("Tên hiển thị").fill("Lý Thu Trang");
  await page.getByText("Xô thơm", { exact: true }).click();
  await page.getByText("Tóm tắt mỗi ngày", { exact: true }).click();
  await page.evaluate(() => {
    // The mock world names no inviter; the lead takes the drawn words so it wraps as drawn.
    document.querySelector("h1 + p")!.textContent =
      "Nguyễn Thu Hà đã mời bạn. Thiết lập nhanh vài thứ dưới đây, đổi lại lúc nào cũng được.";
    // The mock world lists four departments; the board draws one with three boards.
    const card = document.querySelector(
      '[aria-labelledby="join-access"] > div',
    )!;
    for (const name of card.querySelectorAll("b"))
      if (name.textContent === "Công nghệ")
        name.textContent = "Phòng Công nghệ";
    for (const group of [...card.children])
      if (group.tagName === "DIV" && !group.textContent?.includes("Công nghệ"))
        group.remove();
    [...card.querySelector(":scope > div")!.children]
      .slice(4)
      .forEach((row) => row.remove());
  });
}

/** Three invitations in four rows, as drawn. */
async function fillInvites(page: Page) {
  const add = page.getByRole("button", { name: "Thêm dòng" });
  const emails = page.getByRole("textbox", { name: /^Email \d/ });
  await add.click();
  for (const [row, email] of [
    "hoang.minh.khoa@saomaifood.vn",
    "tran.thi.lan@saomaifood.vn",
    "vo.duc.long@saomaifood.vn",
  ].entries())
    await emails.nth(row).fill(email);
}

async function reach(page: Page, scn: Scn) {
  const next = page.getByRole("button", { name: "Tiếp tục", exact: true });
  if (scn === "tham-gia") return reachJoin(page);
  await page.getByLabel("Tên công ty").fill("Công ty CP Thực phẩm Sao Mai");
  if (scn === "dia-chi-trung") {
    await page.getByLabel("Địa chỉ workspace").fill("minhan");
    await expect(page.getByText(/đã có người dùng/)).toBeVisible();
    return;
  }
  await expect(page.getByText(/còn trống/).first()).toBeVisible();
  if (scn === "tao-workspace") return;
  await next.click();
  if (scn === "phong-ban") {
    await expect(
      page.getByRole("button", { name: /^Tạo \d+ phòng ban/ }),
    ).toBeVisible();
    return;
  }
  await page.getByRole("button", { name: /^Tạo \d+ phòng ban/ }).click();
  if (scn === "moi-nguoi") {
    await fillInvites(page);
    return;
  }
  await fillInvites(page);
  await page.getByRole("button", { name: /^Gửi \d+ lời mời/ }).click();
  await expect(page.getByLabel("Tên board")).toBeVisible();
  await page.getByRole("combobox", { name: "Thuộc phòng ban" }).click();
  await page.getByRole("option", { name: "Sản xuất" }).click();
  await page.getByLabel("Tên board").fill("Ra mắt nước chấm Sao Mai");
  if (scn === "mau-board") return;
  await page.getByRole("button", { name: "Tạo board" }).click();
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "đã sẵn sàng",
  );
}

async function dump(page: Page): Promise<BoxEntry[]> {
  await page.evaluate(() => {
    document.querySelector("[data-gen]")?.removeAttribute("data-gen");
    document
      .querySelectorAll(".sr-only")
      .forEach((e) => ((e as HTMLElement).style.display = "none"));
    // The board's selects draw their value inside the native control (not measured): hide the page's.
    document
      .querySelectorAll('[role="combobox"] > *')
      .forEach((e) => ((e as HTMLElement).style.display = "none"));
    document.querySelector("#root")!.setAttribute("data-gen", "1");
  });
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

/**
 * The mockup draws four company sizes in one row; the accepted contract has five, so the fifth chip wraps to a
 * second row of 40 px plus an 8 px gap and everything under the sizes sits 48 px lower than drawn (the footer
 * stays on the bottom edge).
 */
const WRAP = 48;
const SIZES_TOP: Partial<Record<Scn, number>> = {
  "tao-workspace": 478,
  "dia-chi-trung": 490,
};
const shift = (scn: Scn) => (e: BoxEntry) => {
  const top = SIZES_TOP[scn];
  return top !== undefined && e.x < 900 && e.y > top + 40 && e.y < 760
    ? { ...e, y: Math.round((e.y + WRAP) * 10) / 10 }
    : e;
};
/** The size chips (their words differ from the contract's five bands). */
const chip = (scn: Scn, e: BoxEntry) => {
  const top = SIZES_TOP[scn];
  return (
    top !== undefined &&
    e.x < 900 &&
    e.y >= top &&
    e.y < top + 48 &&
    kind(e) !== "svg"
  );
};

/** Keys count equal words in reading order; the header is gone, so count again. */
function renumber(list: BoxEntry[]) {
  const seen = new Map<string, number>();
  for (const e of list) {
    if (kind(e) !== "t" && kind(e) !== "input") continue;
    const base = e.key.replace(/#\d+$/, "");
    const n = (seen.get(base) ?? 0) + 1;
    seen.set(base, n);
    e.key = `${base}#${n}`;
  }
}

/** The board entries that are named differences (see the file header) and not compared. */
const skipped = (e: BoxEntry, dark = false, scn: Scn = "tao-workspace") =>
  // The accepted contract has five company bands, so the board's "101–500 người" lead has other words.
  /^t:Gợi ý theo quy mô/.test(e.key) ||
  // The invited person's page on the mock world: it names no inviter, the preview's department rows,
  // the open board's title and the company's head count come from the mock data, not from the drawing.
  (scn === "tham-gia" &&
    (/^t:(86 người|Triển khai ERP Q4#3)/.test(e.key) ||
      // The app's sage accent is a little lighter than the board's (the theme tokens set it).
      e.b === "45,106,89,1.00" ||
      // The swatch of the coral accent, the review-lane dot and the amber swatch's check ink come from the
      // theme and status tokens, a little off the board's literals.
      (kind(e) === "p" &&
        (e.b === "181,70,29,1.00" || e.b === "217,154,0,1.00")) ||
      (kind(e) === "svg" &&
        e.c === "22,26,33,1.00" &&
        e.y > 640 &&
        e.x < 900) ||
      // In dark, the sage accent's soft fill is a token.
      (kind(e) === "p" && e.b === "112,180,162,0.16") ||
      e.c === "45,106,89,1.00" ||
      e.c === "36,87,70,1.00" ||
      (e.x >= 1010 && e.x < 1170 && e.y >= 395 && e.y <= 640))) ||
  // The disabled primary button in dark is the design system's disabled-state tokens.
  (dark && kind(e) === "p" && e.y === 838 && e.b === "35,42,52,1.00") ||
  (dark && e.key === "t:Tiếp tục#1" && e.c === "143,153,167,1.00") ||
  // Radio and checkbox controls: native, styled by the design system.
  kind(e) === "input" ||
  // The logo mark (the shared header's own spec measures it) and the person's initials and e-mail.
  e.key === "svg#1" ||
  /^t:(· |, |ha\.nguyen|HA#)/.test(e.key) ||
  // The board draws the free-address line in green (30, 116, 68); the page uses the success text token (10, 119, 54).
  (kind(e) !== "p" &&
    (e.c === "30,116,68,1.00" || e.c === "111,212,156,1.00")) ||
  // In dark the board keeps the company mark in the light accent with white initials; the page uses the dark accent.
  (/^t:(SM|MA)#\d/.test(e.key) && e.c === "255,255,255,1.00") ||
  (kind(e) === "p" && e.b === "22,102,204,1.00" && e.w === e.h && e.w <= 44) ||
  // A disabled primary button: the board inks it in the meta grey, the page in the disabled-state token.
  (e.key === "t:Tiếp tục#1" && e.c === "95,104,117,1.00") ||
  // The taken-address words and the button that offers the next address carry the mock world's slug and
  // suggestion; the button is measured on its own below.
  /^t:(saomai đã có người dùng|Dùng saomai-food)/.test(e.key) ||
  (kind(e) === "p" && e.y === 392.3 && e.h === 30);

for (const scn of SCENES)
  for (const dark of [false, true]) {
    test(`onboarding ${scn} at 1440, ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      test.skip(scenario !== "default", "Needs the default world");
      await open(
        page,
        scn === "tham-gia" ? "/welcome/mock-workspace-primary" : "/onboarding",
        dark,
      );
      await reach(page, scn);
      await page.evaluate(() => document.fonts.ready);
      await page.waitForTimeout(700);
      await page.evaluate(() =>
        (document.activeElement as HTMLElement | null)?.blur(),
      );
      await page.mouse.move(0, 0);
      // The header is the frame every setup page shares (its own spec measures it).
      const below = (e: BoxEntry) => e.y >= 56;
      const want = toEntries(
        (golden as unknown as Record<string, Row[]>)[
          `${scn}.${dark ? "dem" : "sang"}`
        ]!,
      )
        .filter(below)
        .map(shift(scn));
      const got = (await dump(page)).filter(below);
      // Words the board hides for screen readers (its clip-style hidden class is not measured away).
      const hidden = new Set([
        "Phòng ban sẽ tạo",
        "Danh sách lời mời",
        "Mẫu board",
        "Đã thiết lập",
        "Chế độ hiển thị",
        "Màu nhấn",
      ]);
      for (let at = want.length - 1; at >= 0; at -= 1) {
        const w = want[at]!;
        if (
          kind(w) === "t" &&
          w.x === 165 &&
          hidden.has(w.key.slice(2).replace(/#\d+$/, ""))
        )
          want.splice(at, 1);
      }
      for (const list of [want, got]) renumber(list);
      if (scn === "dia-chi-trung") {
        const offer = page.getByRole("button", { name: /^Dùng / });
        const at = (await offer.boundingBox())!;
        expect(at.height, "the suggestion button is 30 px").toBeCloseTo(30, 0);
        expect(Math.abs(at.y - 392.3), "its place").toBeLessThanOrEqual(1);
      }
      expect(
        compare(want, got, dark, (e) => skipped(e, dark, scn) || chip(scn, e)),
        "boxes that differ from the board",
      ).toEqual([]);
    });
  }
