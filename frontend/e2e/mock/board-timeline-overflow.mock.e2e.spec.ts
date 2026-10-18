import { expect, test, type Page } from "@playwright/test";
import { findClippedText } from "./support/text-clipping";

/**
 * Nothing in the Board Timeline is cut mid-glyph. For every width, theme and
 * system-rail state the chart is put at each scale and scrolled to its left
 * end, its middle and its right end; text that the chart frame, a bar, the
 * window or the docked list cuts (rather than shortens with an ellipsis, or
 * leaves out) fails the check, header included: a month, week or day cell
 * the view edge cuts moves its text inside the view or loses it. The same pass
 * checks that the cells of a list row never overlap and that the toolbar never
 * leaves one control alone on a line (the primary "Thêm công việc" button
 * wraps alone at the widths where the design's own toolbar does the same).
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const WIDTHS = [1024, 1100, 1280, 1440, 1920] as const;
const SCALES = ["Ngày", "Tuần", "Tháng"] as const;
const POSITIONS = ["left", "middle", "right"] as const;

async function openTimeline(
  page: Page,
  theme: "light" | "dark",
  rail: "hidden" | "pinned",
) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ([mode, railState]) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode ?? "light");
        window.localStorage.setItem("notrelix-system-rail", railState ?? "");
      } catch {
        // Private mode: the defaults apply and the audit still runs.
      }
    },
    [theme, rail],
  );
  await page.goto(`${BOARD}?view=timeline`);
  await expect(page.getByRole("grid")).toBeVisible();
  await expect(page.locator('[data-tl="gb"]').first()).toBeVisible();
}

/**
 * The clipped text once the view has placed its labels for the scroll: the
 * view re-places them a frame or two after a scroll, so an answer is only
 * final when it stays empty or has not changed for 400 ms.
 */
async function settledClipping(page: Page) {
  const audit = () => findClippedText(page, "[data-timeline-view]");
  let hits = await audit();
  const deadline = Date.now() + 1500;
  let stableSince = Date.now();
  while (hits.length > 0 && Date.now() < deadline) {
    await page.waitForTimeout(60);
    const next = await audit();
    if (JSON.stringify(next) !== JSON.stringify(hits)) stableSince = Date.now();
    hits = next;
    if (Date.now() - stableSince >= 400) break;
  }
  return hits;
}

async function scrollChart(page: Page, position: (typeof POSITIONS)[number]) {
  await page.evaluate((where) => {
    const wrap = document.querySelector<HTMLElement>('[data-tl="wrap"]');
    if (!wrap) return;
    wrap.scrollLeft =
      where === "left"
        ? 0
        : where === "right"
          ? wrap.scrollWidth
          : (wrap.scrollWidth - wrap.clientWidth) / 2;
  }, position);
  // The view re-places its labels on the next frame after a scroll.
  await page.evaluate(
    () =>
      new Promise<void>((done) =>
        requestAnimationFrame(() => requestAnimationFrame(() => done())),
      ),
  );
}

/** Cells of a list row whose content boxes overlap, as "ERP-1 key/title". */
async function overlappingCells(page: Page): Promise<string[]> {
  return page.evaluate(() => {
    const found: string[] = [];
    for (const row of document.querySelectorAll('[data-tl="lrow"]')) {
      const boxes = Array.from(row.querySelectorAll('[role="cell"]')).flatMap(
        (cell, index) => {
          const range = document.createRange();
          range.selectNodeContents(cell);
          const content = range.getBoundingClientRect();
          if (content.width === 0) return [];
          const own = cell.getBoundingClientRect();
          // A truncated title shows only its own cell.
          const truncated = getComputedStyle(cell).textOverflow === "ellipsis";
          return [
            {
              index,
              left: content.left,
              right: truncated
                ? Math.min(content.right, own.right)
                : content.right,
            },
          ];
        },
      );
      for (let i = 1; i < boxes.length; i += 1) {
        const before = boxes[i - 1];
        const after = boxes[i];
        if (before && after && before.right > after.left + 0.5)
          found.push(
            `${row.getAttribute("data-row-id")}: cell ${before.index} runs ${(before.right - after.left).toFixed(1)}px into cell ${after.index}`,
          );
      }
    }
    return found;
  });
}

/** The toolbar's lines that hold one control, other than the primary button. */
async function loneControls(page: Page): Promise<string[]> {
  return page.evaluate(() => {
    const bar = document.querySelector('[data-tl="bar"]');
    if (!bar) return ["no toolbar"];
    const items: Array<{ centre: number; name: string; primary: boolean }> = [];
    const add = (element: Element, primary: boolean) => {
      const box = element.getBoundingClientRect();
      if (box.width === 0) return;
      items.push({
        centre: box.top + box.height / 2,
        name: (
          element.getAttribute("aria-label") ??
          element.textContent ??
          ""
        ).trim(),
        primary,
      });
    };
    const children = Array.from(bar.children);
    const visit = (child: Element, last: boolean) => {
      const kind = child.getAttribute("data-tl");
      if (kind === "toggles")
        Array.from(child.children).forEach((toggle) => add(toggle, false));
      else if (kind === "tail")
        Array.from(child.children).forEach((inner) => visit(inner, false));
      else if (!child.className.toString().includes("flex-1"))
        add(child, last && child.tagName === "BUTTON");
    };
    children.forEach((child, index) =>
      visit(child, index === children.length - 1),
    );
    items.sort((a, b) => a.centre - b.centre);
    const lines: Array<typeof items> = [];
    let last = Number.NEGATIVE_INFINITY;
    for (const item of items) {
      if (item.centre - last > 12) lines.push([]);
      lines[lines.length - 1]?.push(item);
      last = item.centre;
    }
    return lines
      .filter((line) => line.length === 1 && !line[0]?.primary)
      .map((line) => line[0]?.name ?? "");
  });
}

test.describe(`Board Timeline text is never cut mid-glyph (${persona})`, () => {
  test.skip(
    scenario !== "default" || faults !== "",
    "The audit needs the default world without faults",
  );
  test.setTimeout(240_000);

  for (const width of WIDTHS) {
    test(`at ${width}px, in both themes, with the rail pinned and hidden`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height: 800 });
      const failures: string[] = [];
      for (const theme of ["light", "dark"] as const) {
        for (const rail of ["hidden", "pinned"] as const) {
          await openTimeline(page, theme, rail);
          const state = `${width} ${theme} rail ${rail}`;
          for (const name of await loneControls(page))
            failures.push(`${state}: toolbar line holds only "${name}"`);
          for (const scale of SCALES) {
            await page
              .getByRole("radiogroup", { name: "Thang thời gian" })
              .getByRole("radio", { name: scale })
              .click();
            for (const position of POSITIONS) {
              await scrollChart(page, position);
              const clipped = await settledClipping(page);
              for (const hit of clipped)
                failures.push(
                  `${state} ${scale} ${position}: "${hit.text}" (${hit.host}) cut by ${hit.by} (L${hit.left} R${hit.right} T${hit.top} B${hit.bottom})`,
                );
              if (position === "left")
                for (const cells of await overlappingCells(page))
                  failures.push(`${state} ${scale}: ${cells}`);
            }
          }
          // A new load per theme/rail state: drop the previous page's script.
          await page.goto("about:blank");
        }
      }
      expect(failures).toEqual([]);
    });
  }

  test("the audit sees text: it finds a label cut by the frame", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await openTimeline(page, "light", "pinned");
    // Put an element in the frame whose text the frame cuts mid-glyph.
    await page.evaluate(() => {
      const frame = document.querySelector<HTMLElement>('[data-tl="wrap"]');
      if (!frame) return;
      const probe = document.createElement("span");
      probe.textContent = "Chữ bị cắt giữa chừng bởi khung";
      probe.style.cssText =
        "position:absolute;top:60px;white-space:nowrap;font-size:12px";
      probe.style.left = `${frame.getBoundingClientRect().width - 40}px`;
      frame.style.position = "relative";
      frame.appendChild(probe);
      probe.dataset.probe = "1";
    });
    const clipped = await findClippedText(page, "[data-timeline-view]");
    expect(clipped.map((hit) => hit.text)).toContain(
      "Chữ bị cắt giữa chừng bởi khung",
    );
  });
});
