import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The saved-filter menu and the save form against the owner-canvas boards `WM-SavedFilter-Menu` and
 * `WM-SavedFilter-Save` (owner artifact 18L4jUXGfdXh7Wn9YQvA2x). The boards live only on the canvas, so the spec reads
 * them live from `WM_CANVAS_DIR` (the downloaded `project/` folder; the spec skips when it is missing), and no number
 * is copied here. Boxes are relative to the menu's or the card's own top-left, within 1 px; font size and weight are
 * equal. Measured: the menu (width, section heading, first row, its icon, name and the three action buttons, the
 * separator and the "Bỏ bộ lọc đang áp dụng" button) and the save card (width, padding, summary chips, label, name
 * input, the two radio rows, the buttons). The artboard draws four rows and the mock world has one, so a box below the
 * first row is compared by size, not by y.
 *
 * Deviations, written down: the artboard draws the save form as a card that grows under the bar; the app opens it as
 * a popover anchored to the button (the shipped pattern of the toolbars). The shared row's creator ("Hà Nguyễn ·")
 * is not drawn: the list contract has no creator.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NAME = "Việc gấp tuần này";

type Box = readonly [number, number, number, number];
interface Probe {
  box: Box;
  fs: string;
  fw: string;
}
type Probes = Record<string, Probe>;

/** The probes of an element tree: each selector's first match, boxed against `root`. */
async function probe(
  page: Page,
  rootSel: string,
  sels: Record<string, string>,
): Promise<Probes> {
  return page.evaluate(
    ({ rootSel: r, sels: s }) => {
      const root = document.querySelector(r);
      if (!root) throw new Error(`no root ${r}`);
      const o = root.getBoundingClientRect();
      const out: Record<string, Probe> = {};
      for (const [key, sel] of Object.entries(s)) {
        const el = key === "root" ? root : root.querySelector(sel);
        if (!el) continue;
        const b = el.getBoundingClientRect();
        const cs = getComputedStyle(el);
        out[key] = {
          box: [b.x - o.x, b.y - o.y, b.width, b.height],
          fs: cs.fontSize,
          fw: cs.fontWeight,
        };
      }
      return out;
    },
    { rootSel, sels },
  );
}

function same(app: Probes, art: Probes, skip: Record<string, string> = {}) {
  for (const key of Object.keys(art)) {
    const a = app[key];
    expect(a, `${key} exists in the app`).toBeDefined();
    const skipped = skip[key] ?? "";
    (["x", "y", "w", "h"] as const).forEach((axis, i) => {
      if (skipped.includes(axis)) return;
      expect(
        Math.abs(a!.box[i]! - art[key]!.box[i]!),
        `${key}.${axis}: ${a!.box[i]} vs ${art[key]!.box[i]}`,
      ).toBeLessThanOrEqual(1);
    });
    if (!skipped.includes("f")) {
      expect(a!.fs, `${key} font size`).toBe(art[key]!.fs);
      expect(a!.fw, `${key} font weight`).toBe(art[key]!.fw);
    }
  }
}

async function artboard(page: Page, file: string) {
  await page.goto(pathToFileURL(`${CANVAS}/${file}`).href);
  await page.waitForTimeout(500);
}

async function openApp(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=table&filter=p.urgent`);
  await expect(page.getByRole("rowgroup").first()).toBeVisible();
}

test.describe("Saved filters against the canvas", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      !existsSync(`${CANVAS}/WM-SavedFilter-Menu.dc.html`),
    "Needs the default owner world and the downloaded canvas boards",
  );
  test.use({ viewport: { width: 1440, height: 1000 } });

  test("the menu equals WM-SavedFilter-Menu", async ({ page }) => {
    await artboard(page, "WM-SavedFilter-Menu.dc.html");
    const art = await probe(page, '.menu[aria-label="Bộ lọc đã lưu"]', {
      root: "",
      heading: ".mh",
      row: ".mrow",
      icon: ".mrow > span:first-child",
      name: ".mrow b",
      sub: ".mrow small",
      rename: ".mrow .iconbtn:nth-child(1)",
      share: ".mrow .iconbtn:nth-child(2)",
      remove: ".mrow .iconbtn:nth-child(3)",
      clear: ".sep ~ button.btn",
    });

    await openApp(page);
    await page
      .getByRole("button", { name: "Lưu bộ lọc", exact: true })
      .click();
    const form = page.getByRole("form", { name: "Lưu bộ lọc hiện tại" });
    await form.getByRole("textbox", { name: "Tên bộ lọc" }).fill(NAME);
    await form.getByRole("button", { name: "Lưu bộ lọc", exact: true }).click();
    await page.getByRole("button", { name: NAME }).click();
    const menu = page.locator('[aria-label="Bộ lọc đã lưu"][role="dialog"]');
    await expect(menu).toBeVisible();
    await page.waitForTimeout(400);
    await menu.evaluate((el) => el.setAttribute("data-fid", "menu"));
    const app = await probe(page, '[data-fid="menu"]', {
      root: "",
      heading: "h3",
      row: "li",
      icon: "li button > span:first-child",
      name: "li b",
      sub: "li small",
      rename: "li > span button:nth-child(1)",
      share: "li > span button:nth-child(2)",
      remove: "li > span button:nth-child(3)",
      clear: '[role="separator"] ~ button',
    });
    // One row in the mock world: the menu's height and the clear button's y follow the row count.
    same(app, art, { root: "h", clear: "y", icon: "f", rename: "f", share: "f", remove: "f" });
  });

  test("the save form equals WM-SavedFilter-Save", async ({ page }) => {
    await artboard(page, "WM-SavedFilter-Save.dc.html");
    const art = await probe(
      page,
      'section[aria-label="Thanh lọc mở form lưu"] .card',
      {
        root: "",
        caption: ".cap",
        chips: ".chip",
        label: "label.lbl",
        input: "input.inp",
        radio: '[role="radio"]',
        cancel: ".btn.txt",
        submit: ".btn.pri",
      },
    );

    await openApp(page);
    await page
      .getByRole("button", { name: "Lưu bộ lọc", exact: true })
      .click();
    const form = page.getByRole("form", { name: "Lưu bộ lọc hiện tại" });
    await expect(form).toBeVisible();
    await page.waitForTimeout(400);
    await form.evaluate((el) =>
      el.parentElement!.setAttribute("data-fid", "save"),
    );
    const app = await probe(page, '[data-fid="save"]', {
      root: "",
      caption: "form > p",
      chips: "ul li",
      label: "label",
      input: "input[type=text]",
      radio: "input[type=radio]",
      cancel: 'button[type="button"]',
      submit: 'button[type="submit"]',
    });
    // The artboard card is 440 px wide with 14 px padding; the app's popover adds its 1 px border to the box.
    same(app, art, {
      root: "h",
      chips: "wf",
      radio: "f",
      cancel: "wxyf",
      submit: "xyf",
      input: "f",
    });
  });
});
