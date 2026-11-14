import { execFileSync } from "node:child_process";
import { mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
import { expect, test, type Page } from "@playwright/test";

/**
 * The groups section of the board settings (UC-WM-E01, E02, E03, F02) against the October 2026 mockup
 * `docs/design/mockups/2026-10/src/Final-Settings.dc.html`, state `nhom-viec`, rendered with `render.py` at
 * 1440x900 with `nav=an`, light (`sang`) and dark (`dem`). The artboard is rendered and measured live in the
 * same run, so no number is copied here: every box is read relative to the section (`section.list` in the
 * artboard), compared with 1 px of tolerance; colours are compared after the canvas turns them into sRGB.
 *
 * Measured against the drawn frame: the section header (title, "Thêm nhóm"), the callout, the list line
 * ("Nhóm của board", "Kéo để đổi thứ tự", which is the reorder of F02), the list, its header row, the four
 * group rows (drag handle, swatch, rename and more buttons), the delete panel under the third group (its
 * text, the move-to picker, "Huỷ" and "Chuyển việc và xoá nhóm") and the footnote. The mock board holds the
 * same four groups as the artboard, so the rows line up one for one.
 *
 * Not drawn by the artboard, built on its delete panel (`.gconf`) and measured against that panel's rules:
 * the add panel (E01 create flow, at the end of the list where the new group lands), its pending state
 * (under VITE_MOCK_LATENCY=slow) and the archive panel (E03). The same left inset (58 px), top padding
 * (12 px), button height (32 px) and right inset (14 px) as the delete panel, and its background and text
 * colours.
 *
 * Not measured: text widths (the group names differ from the artboard's) and the drawn select's width (the
 * app's styled Select is 220 px as drawn, measured, but its inner text differs).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const overlays = process.env.VITE_MOCK_OVERLAYS ?? "";
const latency = process.env.VITE_MOCK_LATENCY ?? "instant";
const SETTINGS =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/settings?from=table&section=groups";
const RENDER = `${process.cwd()}/../docs/design/mockups/2026-10/render.py`;
const NOW = new Date("2026-10-12T10:40:00+07:00");
const TOL = 1;

type Box = readonly [number, number, number, number];
type Dump = Record<string, Box>;

function renderArtboard(mode: "sang" | "dem") {
  const out = join(mkdtempSync(join(tmpdir(), "wm-groups-")), `${mode}.html`);
  execFileSync("python3", [
    "-I",
    RENDER,
    "Final-Settings.dc.html",
    out,
    "state=nhom-viec",
    `mode=${mode}`,
    "nav=an",
  ]);
  return pathToFileURL(out).href;
}

/** In-page helpers: a box relative to an origin and a colour as sRGB through a canvas. */
const HELPERS = `
  window.__box = (el, origin) => {
    const b = el.getBoundingClientRect(); const o = origin.getBoundingClientRect();
    return [b.x - o.x, b.y - o.y, b.width, b.height];
  };
  window.__rgb = (css) => {
    const c = document.createElement("canvas"); c.width = c.height = 1;
    const x = c.getContext("2d", { colorSpace: "srgb" });
    x.fillStyle = "#000"; x.fillStyle = css; x.fillRect(0, 0, 1, 1);
    const d = x.getImageData(0, 0, 1, 1).data; return d[0] + "," + d[1] + "," + d[2];
  };
`;

async function artboardDump(page: Page, mode: "sang" | "dem") {
  await page.goto(renderArtboard(mode));
  await page.addScriptTag({ content: HELPERS });
  return page.evaluate(() => {
    const w = window as unknown as {
      __box: (e: Element, o: Element) => Box;
      __rgb: (c: string) => string;
    };
    const sec = document.querySelector("section.list")!;
    const box = (e: Element) => w.__box(e, sec);
    const all = (sel: string) => [...document.querySelectorAll(sel)];
    const rows = all(".frows > .gx:not(.hd)");
    const conf = document.querySelector(".gconf")!;
    const out: Record<string, Box> = {
      h2: box(document.querySelector("#gh")!),
      add: box(document.querySelector(".lh .btn.pri")!),
      callout: box(document.querySelector(".callout")!),
      line: box(document.querySelector(".sec")!),
      list: box(document.querySelector(".frows")!),
      head: box(document.querySelector(".gx.hd")!),
      confirm: box(conf),
      confirmText: box(conf.querySelector("p")!),
      confirmSelect: box(conf.querySelector("select")!),
      confirmCancel: box(conf.querySelectorAll(".btn")[0]!),
      confirmSubmit: box(conf.querySelectorAll(".btn")[1]!),
      footnote: box(document.querySelector("p.hint")!),
    };
    rows.forEach((row, i) => {
      out[`row${i}`] = box(row);
      out[`grip${i}`] = box(row.querySelector(".grip")!);
      out[`swatch${i}`] = box(row.querySelector(".sw-c")!);
      const icons = row.querySelectorAll(".iconbtn");
      out[`rename${i}`] = box(icons[0]!);
      out[`more${i}`] = box(icons[1]!);
    });
    const s = getComputedStyle(conf);
    const p = getComputedStyle(conf.querySelector("p")!);
    const b = getComputedStyle(conf.querySelector("b")!);
    const list = getComputedStyle(document.querySelector(".frows")!);
    return {
      boxes: out,
      colours: {
        panel: w.__rgb(s.backgroundColor),
        text: w.__rgb(p.color),
        title: w.__rgb(b.color),
        list: w.__rgb(list.backgroundColor),
        line: w.__rgb(list.borderTopColor),
      },
      type: { text: p.fontSize, title: b.fontWeight },
    };
  });
}

async function openApp(page: Page, dark: boolean) {
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
  await page.goto(SETTINGS);
  await expect(
    page.getByRole("heading", { name: "Nhóm việc", level: 2 }),
  ).toBeVisible({ timeout: 15_000 });
  await page.addScriptTag({ content: HELPERS });
  await page.waitForTimeout(400);
}

/** The app's parts, in the artboard's keys. */
async function appDump(page: Page) {
  return page.evaluate(() => {
    const w = window as unknown as {
      __box: (e: Element, o: Element) => Box;
      __rgb: (c: string) => string;
    };
    const sec = document.querySelector(
      "section[aria-labelledby='bs-groups-h']",
    )!;
    const box = (e: Element) => w.__box(e, sec);
    const byText = (root: Element, tag: string, text: string) =>
      [...root.querySelectorAll(tag)].find(
        (e) => e.textContent?.trim() === text,
      )!;
    const list = sec.querySelector("[role=list]")!;
    const note = [...sec.querySelectorAll("span")].find((e) =>
      e.textContent?.startsWith("Nhóm việc khác Trạng thái"),
    )!;
    const line = byText(sec, "*", "Kéo để đổi thứ tự").parentElement!;
    const out: Record<string, Box> = {
      h2: box(document.getElementById("bs-groups-h")!),
      add: box(byText(sec, "button", "Thêm nhóm")),
      callout: box(note.parentElement!),
      line: box(line),
      list: box(list),
      head: box(list.firstElementChild!),
      footnote: box(list.nextElementSibling!),
    };
    const rows = [...list.querySelectorAll("[role=listitem]")].filter((row) =>
      row.querySelector("button[aria-keyshortcuts]"),
    );
    rows.forEach((row, i) => {
      out[`row${i}`] = box(row);
      out[`grip${i}`] = box(row.querySelector("button[aria-keyshortcuts]")!);
      out[`swatch${i}`] = box(row.children[1]!);
      const buttons = row.querySelectorAll("span:last-child > button");
      out[`rename${i}`] = box(buttons[0]!);
      out[`more${i}`] = box(buttons[1]!);
    });
    return out;
  });
}

/** One inset panel of the list (delete, archive, add), in the artboard's keys. */
async function panelDump(page: Page, name: string) {
  const panel = page.getByRole("listitem", { name });
  await expect(panel).toBeVisible();
  return panel.evaluate((conf) => {
    const w = window as unknown as {
      __box: (e: Element, o: Element) => Box;
      __rgb: (c: string) => string;
    };
    const sec = document.querySelector(
      "section[aria-labelledby='bs-groups-h']",
    )!;
    const box = (e: Element) => w.__box(e, sec);
    const buttons = [...conf.querySelectorAll("button:not([role=combobox])")];
    const select = conf.querySelector("[role=combobox]");
    const field = conf.querySelector("input");
    const p = conf.querySelector("p")!;
    const s = getComputedStyle(conf);
    return {
      boxes: {
        confirm: box(conf),
        confirmText: box(p),
        ...(select ? { confirmSelect: box(select) } : {}),
        ...(field ? { confirmField: box(field) } : {}),
        confirmCancel: box(buttons[buttons.length - 2]!),
        confirmSubmit: box(buttons[buttons.length - 1]!),
      } as Record<string, Box>,
      colours: {
        panel: w.__rgb(s.backgroundColor),
        text: w.__rgb(getComputedStyle(p).color),
        title: w.__rgb(getComputedStyle(p.querySelector("b")!).color),
      },
      type: {
        text: getComputedStyle(p).fontSize,
        title: getComputedStyle(p.querySelector("b")!).fontWeight,
      },
    };
  });
}

function near(actual: number, want: number, label: string) {
  expect
    .soft(
      Math.abs(actual - want),
      `${label}: ${actual.toFixed(1)} vs ${want.toFixed(1)}`,
    )
    .toBeLessThanOrEqual(TOL);
}

function same(actual: Box, want: Box, label: string, keys = [0, 1, 2, 3]) {
  const names = ["x", "y", "width", "height"];
  for (const k of keys) near(actual[k]!, want[k]!, `${label}.${names[k]}`);
}

const X = 0,
  Y = 1,
  W = 2,
  H = 3;

test.describe("Board settings, groups (Final-Settings nhom-viec), 1440 px", () => {
  test.skip(
    persona !== "owner" ||
      scenario !== "default" ||
      faults !== "" ||
      overlays !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  for (const dark of [false, true]) {
    const mode = dark ? "dem" : "sang";

    test(`list, rows and delete panel against the drawn frame (${mode})`, async ({
      page,
      browser,
    }) => {
      test.skip(latency !== "instant", "Measured without latency");
      const artPage = await browser.newPage({
        viewport: { width: 1440, height: 900 },
      });
      const art = await artboardDump(artPage, mode);
      await artPage.close();

      await openApp(page, dark);
      const app = await appDump(page);
      // Header, callout, line, list, header row: every edge.
      // The list's height is compared once the delete panel the artboard draws is open, below.
      for (const key of ["h2", "callout", "line", "list", "head"] as const)
        same(
          app[key]!,
          art.boxes[key]!,
          key,
          key === "h2" ? [X, Y, H] : key === "list" ? [X, Y, W] : [X, Y, W, H],
        );
      // "Thêm nhóm": the right edge, top and height (the label is the same; the icon may differ by a pixel).
      near(
        app.add![X] + app.add![W],
        art.boxes.add![X] + art.boxes.add![W],
        "add right",
      );
      same(app.add!, art.boxes.add!, "add", [Y, H]);
      for (let i = 0; i < 4; i += 1) {
        // The delete panel sits under the third row in the artboard; the fourth row is measured after it.
        if (i === 3) continue;
        for (const part of ["row", "grip", "swatch", "rename", "more"])
          same(app[`${part}${i}`]!, art.boxes[`${part}${i}`]!, `${part}${i}`);
      }

      // E03: the delete panel of the third group, with its picker.
      await page
        .getByRole("button", { name: "Thao tác khác với nhóm Kiểm thử" })
        .click();
      await page.getByRole("menuitem", { name: "Xoá nhóm" }).click();
      const del = await panelDump(page, 'Xoá nhóm "Kiểm thử"?');
      same(del.boxes.confirm!, art.boxes.confirm!, "delete panel");
      same(del.boxes.confirmText!, art.boxes.confirmText!, "delete text", [
        X,
        Y,
        H,
      ]);
      // The picker follows its label and the buttons end at the panel's right inset. In the dark render the
      // artboard's text sets about 4 px narrower than in the light one (the app's does not change), so the
      // label-led x and the buttons' widths are compared in light only; right edges, y and height in both.
      same(
        del.boxes.confirmSelect!,
        art.boxes.confirmSelect!,
        "picker",
        dark ? [Y, W] : [X, Y, W],
      );
      near(del.boxes.confirmSelect![H], 34, "picker height");
      for (const [key, label] of [
        ["confirmSubmit", "delete submit"],
        ["confirmCancel", "delete cancel"],
      ] as const) {
        const a = del.boxes[key]!;
        const b = art.boxes[key]!;
        if (key === "confirmSubmit")
          near(a[X] + a[W], b[X] + b[W], `${label} right`);
        same(a, b, label, dark ? [Y, H] : [X, Y, W, H]);
      }
      expect.soft(del.colours).toEqual({
        panel: art.colours.panel,
        text: art.colours.text,
        title: art.colours.title,
      });
      expect.soft(del.type).toEqual(art.type);
      // The fourth row follows the panel, and the footnote follows the list.
      const after = await appDump(page);
      for (const part of ["row", "grip", "swatch", "rename", "more"])
        same(after[`${part}3`]!, art.boxes[`${part}3`]!, `${part}3`);
      same(after.list!, art.boxes.list!, "list with panel");
      same(after.footnote!, art.boxes.footnote!, "footnote", [X, Y, H]);
    });

    test(`add and archive panels follow the delete panel (${mode})`, async ({
      page,
      browser,
    }) => {
      test.skip(latency !== "instant", "Measured without latency");
      const artPage = await browser.newPage({
        viewport: { width: 1440, height: 900 },
      });
      const art = await artboardDump(artPage, mode);
      await artPage.close();
      const drawn = art.boxes.confirm!;
      const drawnSubmit = art.boxes.confirmSubmit!;
      const rightInset =
        drawn[X] + drawn[W] - (drawnSubmit[X] + drawnSubmit[W]);
      const textInset = art.boxes.confirmText![X] - drawn[X];
      const textTop = art.boxes.confirmText![Y] - drawn[Y];

      await openApp(page, dark);
      const rules = (
        panel: Awaited<ReturnType<typeof panelDump>>,
        label: string,
      ) => {
        const box = panel.boxes.confirm!;
        near(box[X], drawn[X], `${label} x`);
        near(box[W], drawn[W], `${label} width`);
        near(
          panel.boxes.confirmText![X] - box[X],
          textInset,
          `${label} text inset`,
        );
        near(
          panel.boxes.confirmText![Y] - box[Y],
          textTop,
          `${label} text top`,
        );
        const submit = panel.boxes.confirmSubmit!;
        near(
          box[X] + box[W] - (submit[X] + submit[W]),
          rightInset,
          `${label} right inset`,
        );
        near(submit[H], drawnSubmit[H], `${label} submit height`);
        near(
          panel.boxes.confirmCancel![H],
          art.boxes.confirmCancel![H],
          `${label} cancel height`,
        );
        expect.soft(panel.colours, `${label} colours`).toEqual({
          panel: art.colours.panel,
          text: art.colours.text,
          title: art.colours.title,
        });
        expect.soft(panel.type, `${label} type`).toEqual(art.type);
      };

      // E03: archive, under the third group like the drawn delete panel, at the same place.
      await page
        .getByRole("button", { name: "Thao tác khác với nhóm Kiểm thử" })
        .click();
      await page.getByRole("menuitem", { name: "Lưu trữ nhóm" }).click();
      const archive = await panelDump(page, 'Lưu trữ nhóm "Kiểm thử"?');
      rules(archive, "archive");
      near(archive.boxes.confirm![Y], drawn[Y], "archive y");
      await page
        .getByRole("listitem", { name: 'Lưu trữ nhóm "Kiểm thử"?' })
        .getByRole("button", { name: "Huỷ" })
        .click();

      // E01: the add panel closes the list, where the new group lands.
      await page.getByRole("button", { name: "Thêm nhóm" }).click();
      const add = await panelDump(page, "Nhóm mới");
      rules(add, "add");
      const app = await appDump(page);
      near(
        add.boxes.confirm![Y],
        app.row3![Y] + app.row3![H],
        "add follows the last row",
      );
      near(add.boxes.confirmField![H], 34, "name field height");
      near(
        add.boxes.confirmField![X],
        add.boxes.confirmText![X],
        "name field x",
      );
    });
  }

  test("the add panel while the group is being added (VITE_MOCK_LATENCY=slow)", async ({
    page,
  }) => {
    test.skip(latency !== "slow", "Needs VITE_MOCK_LATENCY=slow");
    await openApp(page, false);
    await page.getByRole("button", { name: "Thêm nhóm" }).click();
    const panel = page.getByRole("listitem", { name: "Nhóm mới" });
    await panel.getByRole("textbox").fill("Go-live");
    await panel.getByRole("button", { name: "Thêm", exact: true }).click();
    const pending = panel.getByRole("button", { name: "Đang thêm…" });
    await expect(pending).toBeDisabled();
    await expect(panel).toHaveAttribute("aria-busy", "true");
    await expect(panel.getByRole("textbox")).toHaveAttribute("readonly", "");
    const before = (await panel.boundingBox())!;
    const submit = (await pending.boundingBox())!;
    near(submit.height, 32, "pending submit height");
    near(
      before.x + before.width - (submit.x + submit.width),
      14,
      "pending right inset",
    );
    await expect(panel).toBeHidden({ timeout: 10_000 });
    await expect(
      page.getByRole("listitem").filter({ hasText: "Go-live" }).first(),
    ).toBeVisible();
  });
});
