import type { Locator } from "@playwright/test";

/** One text of an artboard dialog: box relative to the dialog's top-left, font size and weight. */
export interface ArtboardText {
  t: string;
  x: number;
  y: number;
  w: number;
  h: number;
  fs: string;
  fw: string;
}

export interface ArtboardFrame {
  label: string;
  code: string;
  variants: {
    mode: "light" | "dark";
    w: number;
    h: number;
    items: ArtboardText[];
  }[];
}

export interface Deviation {
  text: string;
  what: string;
}

const squash = (s: string) => s.replace(/\s+/g, " ").trim();

/**
 * Reads every text of the dialog the same way the artboard was read (own text nodes, box relative to the
 * dialog) and compares each artboard text with the app's box of the same words: position and size within
 * `tolerance` px, font size and weight equal. `skip` lists texts the mock world words differently (data, not
 * layout). Texts the app does not have at all are reported as `missing`.
 */
export async function compareDialog(
  dialog: Locator,
  items: readonly ArtboardText[],
  skip: readonly string[] = [],
  tolerance = 1,
  /** Artboard texts below `afterY` sit `dy` lower in the app (a box the artboard draws shorter than its content). */
  shift?: { afterY: number; dy: number },
): Promise<Deviation[]> {
  const leaves = await dialog.evaluate((root) => {
    const o = root.getBoundingClientRect();
    const out: {
      t: string;
      x: number;
      y: number;
      w: number;
      h: number;
      fs: string;
      fw: string;
    }[] = [];
    for (const e of [root, ...root.querySelectorAll("*")]) {
      const own = [...e.childNodes]
        .filter((n) => n.nodeType === 3)
        .map((n) => n.textContent ?? "")
        .join(" ");
      if (!own.trim()) continue;
      const b = e.getBoundingClientRect();
      const cs = getComputedStyle(e);
      out.push({
        t: own,
        x: b.x - o.x,
        y: b.y - o.y,
        w: b.width,
        h: b.height,
        fs: cs.fontSize,
        fw: cs.fontWeight,
      });
    }
    return out;
  });
  const free = leaves.map((l) => ({ ...l, t: squash(l.t) }));
  const found: Deviation[] = [];
  for (const drawn of items) {
    if (skip.includes(drawn.t)) continue;
    const want =
      shift && drawn.y > shift.afterY
        ? { ...drawn, y: drawn.y + shift.dy }
        : drawn;
    const text = squash(want.t);
    const candidates = free.filter((l) => l.t === text);
    if (candidates.length === 0) {
      found.push({ text, what: "missing" });
      continue;
    }
    const near = candidates.reduce((a, b) =>
      Math.abs(a.y - want.y) + Math.abs(a.x - want.x) <=
      Math.abs(b.y - want.y) + Math.abs(b.x - want.x)
        ? a
        : b,
    );
    free.splice(free.indexOf(near), 1);
    const off = (["x", "y", "w", "h"] as const)
      .filter((k) => Math.abs(near[k] - want[k]) > tolerance)
      .map((k) => `${k} ${near[k].toFixed(1)} vs ${want[k]}`);
    if (near.fs !== want.fs) off.push(`font ${near.fs} vs ${want.fs}`);
    if (near.fw !== want.fw) off.push(`weight ${near.fw} vs ${want.fw}`);
    if (off.length) found.push({ text, what: off.join(", ") });
  }
  return found;
}
