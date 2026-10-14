import type { Page } from "@playwright/test";

/** One piece of text whose painted glyphs a clipping ancestor cuts. */
export interface ClippedText {
  /** The visible text, shortened for the report. */
  text: string;
  /** The `data-tl` of the element holding the text, when it has one. */
  host: string;
  /** What clips it: the `data-tl` / tag of the clipping ancestor, or "viewport" / "list". */
  by: string;
  /** Pixels of the text beyond the clip edge, per side. */
  left: number;
  right: number;
  top: number;
  bottom: number;
}

/**
 * Finds text whose rendered glyphs are cut by an ancestor's overflow (or
 * covered by the docked list) inside `rootSelector`. Text an ancestor shows
 * with an ellipsis is fine on that ancestor, but the painted box of the
 * ellipsis must itself lie inside every outer clip: an ellipsis a frame edge
 * hides is a cut glyph. Runs in the page.
 */
export async function findClippedText(
  page: Page,
  rootSelector: string,
  /** Text inside these is not audited (a scrolling column header). */
  exceptSelector = "",
): Promise<ClippedText[]> {
  return page.evaluate(
    ([selector, except]) => {
      const root = document.querySelector(selector);
      if (!root) throw new Error(`No element matches ${selector}`);
      const TOL = 0.75;
      type Box = { l: number; r: number; t: number; b: number };
      const out: ClippedText[] = [];
      const label = (element: Element) =>
        element.getAttribute("data-tl") ?? element.tagName.toLowerCase();
      const clips = (style: CSSStyleDeclaration) => ({
        x: style.overflowX !== "visible",
        y: style.overflowY !== "visible",
        scroll:
          style.overflowX === "auto" ||
          style.overflowX === "scroll" ||
          style.overflowY === "auto" ||
          style.overflowY === "scroll",
      });
      const padBox = (element: Element): Box => {
        const rect = element.getBoundingClientRect();
        const h = element as HTMLElement;
        return {
          l: rect.left + h.clientLeft,
          r: rect.left + h.clientLeft + h.clientWidth,
          t: rect.top + h.clientTop,
          b: rect.top + h.clientTop + h.clientHeight,
        };
      };
      const ellipsized = (element: Element, style: CSSStyleDeclaration) =>
        style.textOverflow === "ellipsis" &&
        style.overflowX !== "visible" &&
        (element as HTMLElement).scrollWidth >
          (element as HTMLElement).clientWidth + 0.5;

      // The docked list covers the chart's left edge.
      const list = root.querySelector('[data-tl="left"]');
      const listDocked =
        list !== null && getComputedStyle(list).position === "sticky";
      const listRight = list ? list.getBoundingClientRect().right : 0;
      const chart = root.querySelector('[data-tl="right"]');

      const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
      for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        const text = node.textContent ?? "";
        if (text.trim() === "") continue;
        const host = node.parentElement;
        if (!host) continue;
        if (except && host.closest(except)) continue;
        const before = out.length;
        const hostStyle = getComputedStyle(host);
        if (
          hostStyle.visibility === "hidden" ||
          hostStyle.display === "none" ||
          Number(hostStyle.opacity) === 0
        )
          continue;
        const range = document.createRange();
        range.selectNodeContents(node);
        let boxes: Box[] = Array.from(range.getClientRects())
          .filter((r) => r.width > 0 && r.height > 0)
          .map((r) => ({ l: r.left, r: r.right, t: r.top, b: r.bottom }));
        if (boxes.length === 0) continue;

        // Walk up the clipping chain.
        let current: Element | null = host;
        let replaced = false;
        while (current && out.length === before) {
          const style = getComputedStyle(current);
          if (ellipsized(current, style) && !replaced) {
            // The painted text ends at this box: what shows is the box itself.
            const pad = padBox(current);
            boxes = boxes.map((box) => ({
              l: box.l,
              r: Math.min(box.r, pad.r),
              t: box.t,
              b: box.b,
            }));
            replaced = true;
            // Its own clip is the ellipsis; outer clips still apply.
            current = current.parentElement;
            continue;
          }
          const c = clips(style);
          if (current !== host || c.x || c.y) {
            if (c.x || c.y) {
              const pad = padBox(current);
              for (const box of boxes) {
                // Wholly outside the clip is hidden, not cut.
                if (c.x && (box.r <= pad.l + TOL || box.l >= pad.r - TOL))
                  continue;
                const left = c.x ? pad.l - box.l : 0;
                const right = c.x ? box.r - pad.r : 0;
                // Vertical cuts by a scroller are scrolling, not clipping.
                const top = c.y && !c.scroll ? pad.t - box.t : 0;
                const bottom = c.y && !c.scroll ? box.b - pad.b : 0;
                if (left > TOL || right > TOL || top > TOL || bottom > TOL) {
                  out.push({
                    text: text.trim().slice(0, 40),
                    host: label(host),
                    by: label(current),
                    left: Math.max(0, Math.round(left)),
                    right: Math.max(0, Math.round(right)),
                    top: Math.max(0, Math.round(top)),
                    bottom: Math.max(0, Math.round(bottom)),
                  });
                  break;
                }
              }
            }
          }
          if (current === root) break;
          current = current.parentElement;
        }

        // The window edge, and the docked list over the chart.
        if (out.length > before) continue;
        for (const box of boxes) {
          const over =
            box.r > TOL &&
            box.l < window.innerWidth - TOL &&
            (box.r - window.innerWidth > TOL || -box.l > TOL)
              ? {
                  by: "viewport",
                  left: -box.l,
                  right: box.r - window.innerWidth,
                }
              : null;
          if (over)
            out.push({
              text: text.trim().slice(0, 40),
              host: label(host),
              by: over.by,
              left: Math.max(0, Math.round(over.left)),
              right: Math.max(0, Math.round(over.right)),
              top: 0,
              bottom: 0,
            });
          if (
            listDocked &&
            chart?.contains(host) &&
            !host.closest('[data-tl="rh"]')
          ) {
            // Text starting under the list but not wholly inside it.
            const under = listRight - box.l;
            if (under > TOL && box.r > listRight - TOL) {
              out.push({
                text: text.trim().slice(0, 40),
                host: label(host),
                by: "list",
                left: Math.round(under),
                right: 0,
                top: 0,
                bottom: 0,
              });
            }
          }
        }
      }
      return out;
    },
    [rootSelector, exceptSelector] as const,
  );
}
