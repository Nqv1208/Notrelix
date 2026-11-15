/**
 * A box tree of one region of a page: every run of text (the glyph box of the
 * text node, keyed by its words) and every painted box (a fill or a border, in
 * reading order), each with its position in the viewport, size, type and
 * colours. It runs in the browser and takes only the region's selector, so the
 * same function dumps a Design board (a static render of the artboard) and the
 * shipped page, and the two dumps are compared entry by entry.
 */
export interface BoxEntry {
  key: string;
  x: number;
  y: number;
  w: number;
  h: number;
  /** Text runs: font size, weight and colour. Painted boxes: radius, fill and border colour. */
  a: string;
  b: string;
  c: string;
}

export function dumpBoxTree(selector: string): BoxEntry[] {
  const root = document.querySelector(selector);
  if (!root) return [];
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = 1;
  const ctx = canvas.getContext("2d", { willReadFrequently: true })!;
  const rgba = (value: string): string => {
    ctx.clearRect(0, 0, 1, 1);
    ctx.fillStyle = "#000";
    ctx.fillStyle = value;
    ctx.fillRect(0, 0, 1, 1);
    const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data;
    return `${r},${g},${b},${((a ?? 0) / 255).toFixed(2)}`;
  };
  const round = (n: number) => Math.round(n * 10) / 10;
  // A pill and a circle read the same whether drawn as 50% or as a huge radius.
  const radius = (v: string) =>
    v.endsWith("%") || Number.parseFloat(v) > 1000 ? "round" : v;
  const out: BoxEntry[] = [];
  const seen = new Map<string, number>();
  const painted: BoxEntry[] = [];
  const walk = (el: Element) => {
    const cs = getComputedStyle(el);
    if (cs.display === "none" || cs.visibility === "hidden") return;
    if (el.closest("svg") && el.tagName.toLowerCase() !== "svg") return;
    const rect = el.getBoundingClientRect();
    const fill = rgba(cs.backgroundColor);
    // The colour of the first side that has a border (the others may only inherit currentColor).
    const side = ["Top", "Right", "Bottom", "Left"].find(
      (s) =>
        parseFloat(cs.getPropertyValue(`border-${s.toLowerCase()}-width`)) > 0,
    );
    const border = side
      ? rgba(cs.getPropertyValue(`border-${side.toLowerCase()}-color`))
      : "";
    const isInput = el.tagName === "INPUT";
    const checkbox = isInput && (el as HTMLInputElement).type === "checkbox";
    if (
      rect.width > 0 &&
      rect.height > 0 &&
      (!fill.endsWith(",0.00") ||
        (border && !border.endsWith(",0.00")) ||
        checkbox)
    ) {
      painted.push({
        key: "",
        x: round(rect.x),
        y: round(rect.y),
        w: round(rect.width),
        h: round(rect.height),
        a: radius(cs.borderTopLeftRadius),
        // An avatar is coloured by the person it shows: not a design value.
        b:
          radius(cs.borderTopLeftRadius) === "round" &&
          rect.width === rect.height &&
          (el.textContent ?? "").trim().length <= 3 &&
          (el.textContent ?? "").trim().length > 0
            ? "person"
            : fill,
        c: border,
      });
    }
    if (el.tagName.toLowerCase() === "svg") {
      if (rect.width > 0) {
        const n = (seen.get("svg") ?? 0) + 1;
        seen.set("svg", n);
        out.push({
          key: `svg#${n}`,
          x: round(rect.x),
          y: round(rect.y),
          w: round(rect.width),
          h: round(rect.height),
          a: "",
          b: "",
          c: rgba(cs.color),
        });
      }
      return;
    }
    for (const node of Array.from(el.childNodes)) {
      if (node.nodeType === 3) {
        const text = (node.textContent ?? "").replace(/\s+/g, " ").trim();
        if (!text) continue;
        const range = document.createRange();
        // The glyph box of the words, not of the spaces around them.
        const raw = node.textContent ?? "";
        range.setStart(node, raw.length - raw.trimStart().length);
        range.setEnd(node, raw.trimEnd().length);
        const r = range.getBoundingClientRect();
        if (r.width === 0) continue;
        const n = (seen.get(text) ?? 0) + 1;
        seen.set(text, n);
        out.push({
          key: `t:${text}#${n}`,
          x: round(r.x),
          y: round(r.y),
          w: round(r.width),
          h: round(r.height),
          a: cs.fontSize,
          b: cs.fontWeight,
          c: rgba(cs.color),
        });
      } else if (node.nodeType === 1) {
        walk(node as Element);
      }
    }
    if (isInput && !checkbox) {
      const input = el as HTMLInputElement;
      const text = input.value || input.placeholder;
      if (text) {
        const n = (seen.get(`input:${text}`) ?? 0) + 1;
        seen.set(`input:${text}`, n);
        out.push({
          key: `input:${text}#${n}`,
          x: round(rect.x),
          y: round(rect.y),
          w: round(rect.width),
          h: round(rect.height),
          a: cs.fontSize,
          b: cs.fontWeight,
          // The browser draws a placeholder in its own grey: not a design value.
          c: input.value ? rgba(cs.color) : "placeholder",
        });
      }
    }
  };
  walk(root);
  painted
    .sort((p, q) => p.y - q.y || p.x - q.x || q.w - p.w)
    .forEach((p, i) => out.push({ ...p, key: `p:${i + 1}` }));
  return out;
}
