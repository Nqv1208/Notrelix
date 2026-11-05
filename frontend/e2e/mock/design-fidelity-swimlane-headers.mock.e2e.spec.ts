import { existsSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { expect, test } from "@playwright/test";

/**
 * The column header (37 px) and the lane header (40 px) of the Kanban with lanes against the owner canvas board
 * `WM-Swimlanes` (`docs/design/mockups/2026-10/src/WM-Swimlanes.dc.html`), light. The artboard is read live; tolerance
 * is 1 px. Compared: heights, the header font size and weight, the count chip's size, font and colours, the lane
 * header's fill and font. Widths follow the app's main area, not the artboard's 1360 px frame.
 */
const CANVAS = process.env.WM_CANVAS_DIR ?? "/tmp/wmv-canvas/project";
const SRC = `${process.cwd()}/../docs/design/mockups/2026-10/src`;
const FILE = "WM-Swimlanes.dc.html";
const dir = existsSync(`${CANVAS}/${FILE}`) ? CANVAS : SRC;
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
test.skip(
  persona !== "owner" || scenario !== "default" || faults !== "",
  "Owner world",
);
test.use({ viewport: { width: 1440, height: 900 } });

/** Evaluated in the page: the facts of a column header, its count chip and a lane header. */
const READ = `({ col, lane }) => {
  const ctx = document.createElement("canvas").getContext("2d");
  const rgb = (v) => { ctx.fillStyle = "#fff"; ctx.fillRect(0,0,1,1); ctx.fillStyle = v; ctx.fillRect(0,0,1,1); const d = ctx.getImageData(0,0,1,1).data; return "rgb(" + d[0] + ", " + d[1] + ", " + d[2] + ")"; };
  const f = (el) => { const cs = getComputedStyle(el); const b = el.getBoundingClientRect(); return { h: b.height, fs: cs.fontSize, fw: cs.fontWeight, bg: rgb(cs.backgroundColor), color: rgb(cs.color) }; };
  return { col: f(col), chip: f(col.lastElementChild), lane: f(lane) };
}`;
type Part = { h: number; fs: string; fw: string; bg: string; color: string };
type Facts = { col: Part; chip: Part; lane: Part };

function same(a: string, b: string, label: string) {
  const x = a.match(/\d+/g)!;
  const y = b.match(/\d+/g)!;
  x.forEach((v, i) =>
    expect(
      Math.abs(Number(v) - Number(y[i]!)),
      `${label}: ${a} vs ${b}`,
    ).toBeLessThanOrEqual(2),
  );
}

test("WM-Swimlanes: column header and lane header", async ({
  page,
  browser,
}) => {
  const art = await browser.newPage();
  await art.setViewportSize({ width: 1500, height: 900 });
  await art.goto(pathToFileURL(`${dir}/${FILE}`).href);
  const want = (await art.evaluate(
    (fn) =>
      (0, eval)(fn)({
        col: document.querySelector(".kh")!,
        lane: document.querySelector(".lh")!,
      }),
    READ,
  )) as Facts;
  await art.close();

  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(
    "/workspaces/mock-workspace-primary/boards/mock-board-erp?view=kanban",
  );
  await page.getByRole("button", { name: "Làn", exact: true }).click();
  await page.getByRole("menuitemradio", { name: /Người thực hiện/ }).click();
  await page.waitForSelector("[data-swimlane-header]");
  const got = (await page.evaluate(
    (fn) =>
      (0, eval)(fn)({
        // The header row is 37 px with its own border: the artboard's `.kh` is one cell with a border.
        col: document.querySelector("[data-swimlane-header]")!
          .firstElementChild,
        lane: document.querySelector("[data-swimlane] > div > div"),
      }),
    READ,
  )) as Facts;

  // The artboard cell includes its bottom border; the app's cell sits in a bordered row of 37 px.
  expect(got.col.h + 1, "column header height").toBeCloseTo(want.col.h, 0);
  expect(got.col.fs, "column font size").toBe(want.col.fs);
  expect(got.col.fw, "column font weight").toBe(want.col.fw);
  expect(got.chip.fs, "count font size").toBe(want.chip.fs);
  expect(got.chip.h, "count height").toBeCloseTo(want.chip.h, 0);
  same(got.chip.bg, want.chip.bg, "count fill");
  same(got.chip.color, want.chip.color, "count colour");
  expect(got.lane.h, "lane header height").toBeCloseTo(want.lane.h, 0);
  expect(got.lane.fs, "lane font size").toBe(want.lane.fs);
  expect(got.lane.fw, "lane font weight").toBe(want.lane.fw);
  same(got.lane.bg, want.lane.bg, "lane fill");
});
