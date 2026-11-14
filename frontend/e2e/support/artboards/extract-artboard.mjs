#!/usr/bin/env node
/**
 * Reads the dialogs of a canvas artboard rendered by `docs/design/mockups/2026-10/render.py` and writes the box
 * of every text of each one (relative to the dialog's top-left, font size, weight), light and dark, as JSON for
 * the fidelity specs. Reference tooling: it never ships.
 *
 * Usage: node extract-artboard.mjs <rendered.html> <out.json> <width> <dialogWidth> [radius=14]
 * A frame is a block whose first row holds the frame's label (bold) and code; its dialogs are the first boxes
 * of that dialog width and radius under each backdrop.
 */
import { readFileSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const require = createRequire(join(here, "../../../package.json"));
const { chromium } = require("@playwright/test");

const [file, out, width, dialogWidths, radius = "14"] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({
  viewport: { width: Number(width), height: 1000 },
});
await page.setContent(readFileSync(file, "utf8"));
await page.waitForTimeout(500);
const frames = await page.evaluate(
  ([widths, radius]) => {
    const rect = (e) => e.getBoundingClientRect();
    const out = [];
    for (const block of document.querySelectorAll("div")) {
      const row = block.children[0];
      const pair = block.children[1];
      if (
        !row ||
        !pair ||
        row.children.length < 2 ||
        row.children[0].tagName !== "B"
      )
        continue;
      if (block.children.length !== 2) continue;
      const label = row.children[0].textContent.trim();
      const code = row.children[1].textContent.trim();
      const variants = [];
      for (const backdrop of pair.children) {
        const dialog = [...backdrop.querySelectorAll("div")].find((d) => {
          const b = rect(d);
          return (
            widths.includes(Math.round(b.width)) &&
            getComputedStyle(d).borderTopLeftRadius === `${radius}px`
          );
        });
        if (!dialog) continue;
        const o = rect(dialog);
        const luminance = (c) =>
          c
            .match(/\d+/g)
            .slice(0, 3)
            .reduce((s, v) => s + Number(v), 0);
        const items = [];
        for (const e of dialog.querySelectorAll("*")) {
          const own = [...e.childNodes]
            .filter((n) => n.nodeType === 3)
            .map((n) => n.textContent.trim())
            .join(" ");
          if (!own) continue;
          const b = rect(e);
          const cs = getComputedStyle(e);
          items.push({
            t: own,
            x: +(b.x - o.x).toFixed(1),
            y: +(b.y - o.y).toFixed(1),
            w: +b.width.toFixed(1),
            h: +b.height.toFixed(1),
            fs: cs.fontSize,
            fw: cs.fontWeight,
          });
        }
        variants.push({
          mode:
            luminance(getComputedStyle(dialog).backgroundColor) > 382
              ? "light"
              : "dark",
          w: +o.width.toFixed(1),
          h: +o.height.toFixed(1),
          items,
        });
      }
      if (variants.length) out.push({ label, code, variants });
    }
    return out;
  },
  [dialogWidths.split(",").map(Number), Number(radius)],
);
writeFileSync(out, `${JSON.stringify(frames, null, 1)}\n`);
console.log(
  frames
    .map(
      (f) =>
        `${f.label} [${f.variants.map((v) => v.mode + " " + v.w + "x" + v.h + " " + v.items.length).join("; ")}]`,
    )
    .join("\n"),
);
await browser.close();
